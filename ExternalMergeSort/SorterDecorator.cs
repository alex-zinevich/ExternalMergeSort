using System.Collections.Concurrent;

namespace ExternalMergeSort;

public abstract class SorterDecorator : ExternalMergeSorter
{
    protected readonly ExternalMergeSorter Sorter;

    public SorterDecorator(ExternalMergeSorter sorter)
    {
        Sorter = sorter;
    }
    
    internal override void SortRun(ParsedLine[] chunkToSort, long index, BlockingCollection<(ParsedLine[] Lines, long Index)> sortedChunks)
    {
        Sorter.SortRun(chunkToSort, index, sortedChunks);
    }

    internal override string WriteRun(ParsedLine[] lines, long index)
    {
        return Sorter.WriteRun(lines, index);
    }

    internal override void MergeRuns(IList<string> runs, string outputPath)
    {
        Sorter.MergeRuns(runs, outputPath);
    }
}