using ExternalMergeSort;

namespace Tests;

public class MergeSortTests
{
    [Test]
    public void OneElementWorks()
    {
        var sorter = new ParallelMergeSorter();
        int[] arr = [1];
        sorter.Sort(arr);
        
        Assert.That(arr, Is.EqualTo(new[] {1}));
    }

    [Test]
    public void EmptyArrayIsSorted()
    {
        var sorter = new ParallelMergeSorter();
        int[] arr = [];
        sorter.Sort(arr);
        
        Assert.That(arr, Is.EqualTo(Array.Empty<int>()));
    }
    
    [Test]
    public void SmallArrayWorks()
    {
        var sorter = new ParallelMergeSorter();
        int[] arr = [1, 2];
        sorter.Sort(arr);
        Assert.That(arr, Is.EqualTo(new[] {1, 2}));

        arr = [2, 1];
        sorter.Sort(arr);
        Assert.That(arr, Is.EqualTo(new[] {1, 2}));

        arr = [1, 3, 2];
        sorter.Sort(arr);
        Assert.That(arr, Is.EqualTo(new[] {1, 2, 3}));
        
        arr = [2, 1, 2];
        sorter.Sort(arr);
        Assert.That(arr, Is.EqualTo(new[] {1, 2, 2}));
    }

    [Test]
    [TestCase(100)]
    [TestCase(1000)]
    public void SortOfXSizeWorks(int x)
    {
        var sorter = new ParallelMergeSorter();

        var arr = GenerateRandomArray(x, 1, 100);
        sorter.Sort(arr);

        AssertSorted(arr);
    }

    [Test]
    public void SortForDifferentTypesWorks()
    {
        string[] arr = { "a", "c", "b" };
        var sorter = new ParallelMergeSorter();
        sorter.Sort(arr);
        
        AssertSorted(arr);

        ParsedLine[] lines = [
            new("1. Apple"),
            new("2. Orange"),
            new("3. Apple")
        ];
        sorter.Sort(lines);
        
        AssertSorted(lines);
    }

    [Test]
    public void SortLongArraysWorks()
    {
        var arr = GenerateRandomArray(50_000000, 1, 1000000);
        var sorter = new ParallelMergeSorter();
        sorter.Sort(arr);
        
        AssertSorted(arr);
    }
    
    private void AssertSorted<T>(T[] arr) where T : IComparable<T>
    {
        for (int i = 1; i < arr.Length; i++)
        {
            if(arr[i].CompareTo(arr[i - 1]) < 0)
                Assert.Fail();
        }
    }
    
    private static int[] GenerateRandomArray(int size, int minValue, int maxValue)
    {
        Random randNum = new Random(); 
        int[] randomArray = new int[size];

        for (int i = 0; i < size; i++)
            randomArray[i] = randNum.Next(minValue, maxValue); 
        
        return randomArray;
    }
}