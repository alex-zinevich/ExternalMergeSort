using System.Collections.Concurrent;
using System.Diagnostics;

namespace ExternalMergeSort;

public class LoggedSorter : SorterDecorator
{
    private readonly Action<string> _log;
    
    public LoggedSorter(ExternalMergeSorter sorter, Action<string>? log = null) : base(sorter)
    {
        _log = log ?? Console.WriteLine;
    }
    
    internal override void SortRun(ParsedLine[] chunkToSort, long index, BlockingCollection<(ParsedLine[] Lines, long Index)> sortedChunks)
    {
        var sw = Stopwatch.StartNew();
        Sorter.SortRun(chunkToSort, index, sortedChunks);
        sw.Stop();
        Log($"Sort run {index} took {sw.Elapsed.TotalSeconds:F0}s");
    }
    
    internal override string WriteRun(ParsedLine[] lines, long index)
    {
        var sw = Stopwatch.StartNew();
        var path = Sorter.WriteRun(lines, index);
        sw.Stop();
        Log($"Write run {index} took {sw.Elapsed.TotalSeconds:F0}s");
        return path;
    }
    
    internal override void MergeRuns(IList<string> runs, string outputPath)
    {
        Log($"Merging {runs.Count} run(s)");
        var sw = Stopwatch.StartNew();
        Sorter.MergeRuns(runs, outputPath);
        Log($"Merge complete in {sw.Elapsed.TotalSeconds:F2}s");
    }
    
    private void Log(string message) => _log($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
}