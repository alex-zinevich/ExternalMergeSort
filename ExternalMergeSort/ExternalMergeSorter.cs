using System.Collections.Concurrent;
using System.Text;

namespace ExternalMergeSort;

/// <summary>
/// Sorts a large file using external merge sort.
///
/// The algorithm has two phases:
///
/// Phase 1 - Run Generation (pipelined across 3 stages):
/// <code>
/// Main (read):  [read 1]         [read 2]         [read 3]         [read 4]
/// CPU  (sort):           [sort 1]         [sort 2]         [sort 3]
/// Disk (write):                   [write 1]        [write 2]        [write 3]
/// </code>
/// Reading and sorting are overlapped — while the main thread reads the next chunk,
/// the background thread sorts the previous one. Writes are sequential and never
/// overlap with reads to avoid SSD contention.
///
/// Phase 2 - Merge:
/// All sorted runs are merged in a single pass using a min-heap, writing the
/// final sorted output sequentially.
/// </summary>
public class ExternalMergeSorter
{
    private const int WriteBufferSize = 65536;
    private const int ReadBufferSize = 65536;
    
    public long MaxMemoryBytes { get; set; } = 512L * 1024 * 1024;
    public const int LineEndSize = 2;
    public Encoding FileEncoding { get; set; } = Encoding.UTF8;
    
    private ParallelMergeSorter _chunkSorter = new();
    
    public void Sort(string inputPath, string outputPath)
    {
        IList<string> runs = new List<string>();
        try
        {
            runs = CreateSortedRuns(inputPath);
            
            MergeRuns(runs, outputPath);
        }
        finally
        {
            foreach (var run in runs)
                if (File.Exists(run))
                    File.Delete(run);
        }
    }

    private IList<string> CreateSortedRuns(string inputPath)
    {
        var sortedChunks = new BlockingCollection<(ParsedLine[] Lines, long Index)>(boundedCapacity: 1);
        var runFiles = new List<string>();
        
        var writeThread = Task.Run(() => {
            foreach (var (lines, index) in sortedChunks.GetConsumingEnumerable())
                runFiles.Add(WriteRun(lines, index));
        });
        
        var buffer = new List<ParsedLine>();
        long bufferSize = 0;
        
        using var reader = new StreamReader(inputPath, FileEncoding, detectEncodingFromByteOrderMarks: false, bufferSize: ReadBufferSize);
        long runIndex = 0;
        Task? pendingSort = null;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            buffer.Add(new ParsedLine(line));
            bufferSize += line.Length * 2 + LineEndSize;

            if (bufferSize >= MaxMemoryBytes)
            {
                pendingSort?.Wait(); // wait for previous sort to finish (memory constraint)
                
                ParsedLine[] chunkToSort = [..buffer];
                long index = runIndex++;
                pendingSort = Task.Run(() => { // sort in background while main thread reads next chunk
                    SortRun(chunkToSort, index, sortedChunks);
                });
                
                buffer = new List<ParsedLine>();
                bufferSize = 0;
            }
        }
        
        pendingSort?.Wait();
        if (buffer.Count > 0)
        {
            ParsedLine[] rest = [..buffer];
            _chunkSorter.Sort(rest);
            sortedChunks.Add((rest, runIndex));
        }

        sortedChunks.CompleteAdding();
        writeThread.Wait();

        return runFiles;
    }

    internal virtual void SortRun(ParsedLine[] chunkToSort, long index, BlockingCollection<(ParsedLine[] Lines, long Index)> sortedChunks)
    {
        _chunkSorter.Sort(chunkToSort);
        sortedChunks.Add((chunkToSort, index)); // blocks if writer is busy (bounded = 1)
    }

    internal virtual string WriteRun(ParsedLine[] lines, long index)
    {
        var path = $"run_{index}_{Guid.NewGuid():N}.tmp";
        using var writer = new StreamWriter(path, append: false, FileEncoding, bufferSize: WriteBufferSize);
        foreach (var line in lines)
            writer.WriteLine(line.ToString());

        return path;
    }

    internal virtual void MergeRuns(IList<string> runs, string outputPath)
    {
        var readers = runs.Select(f => new StreamReader(f, FileEncoding)).ToList();
        
        var heap = new PriorityQueue<int, (ParsedLine Line, long Seq)>(new RunEntryComparer(new LineComparer()));
        
        long seq = 0; 
        for (var i = 0; i < readers.Count; i++)
        {
            var firstLine = readers[i].ReadLine();
            if (firstLine != null)
                heap.Enqueue(i, (new ParsedLine(firstLine), seq++));
        }
        
        using var writer = new StreamWriter(outputPath, append: false, FileEncoding, bufferSize: WriteBufferSize);

        while (heap.Count > 0)
        {
            heap.TryDequeue(out var readerIndex, out var min);
            
            writer.WriteLine(min.Line.ToString());
            var next = readers[readerIndex].ReadLine();
            if(next != null)
                heap.Enqueue(readerIndex, (new ParsedLine(next), seq++));
        }
        
        foreach (var r in readers)
            r.Dispose();
    }
    
    private class LineComparer : IComparer<ParsedLine>
    {
        public int Compare(ParsedLine x, ParsedLine y)
        {
            if (x.String == y.String)
                return x.Number.CompareTo(y.Number);
            
            return string.CompareOrdinal(x.String, y.String);
        }
    }
    
    private class RunEntryComparer : IComparer<(ParsedLine Line, long Seq)>
    {
        private readonly IComparer<ParsedLine> _lineComparer;

        public RunEntryComparer(IComparer<ParsedLine> lineComparer) => _lineComparer = lineComparer;

        public int Compare(
            (ParsedLine Line, long Seq) x,
            (ParsedLine Line, long Seq) y)
        {
            int cmp = _lineComparer.Compare(x.Line, y.Line);
            if (cmp != 0) return cmp;

            // Use sequence number as a stable tie-break so no two entries
            // are considered equal (PriorityQueue requires unique keys).
            return x.Seq.CompareTo(y.Seq);
        }
    }
}