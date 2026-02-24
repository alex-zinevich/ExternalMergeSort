namespace ExternalMergeSort;

public class ParallelMergeSorter
{
    private const int SequentialThreshold = 2048;
    
    public void Sort<T>(T[] arr) where T : IComparable<T>
    {
        MergeSort(arr, 0, arr.Length - 1);
    }

    private void MergeSort<T>(T[] arr, int left, int right) where T : IComparable<T>
    {
        if (left >= right) 
            return;
        
        int mid = left + (right - left) / 2;
        
        if (right - left <= SequentialThreshold) 
        {
            MergeSort(arr, left, mid);
            MergeSort(arr, mid + 1, right);
        }
        else
        {
            Parallel.Invoke(
                () => MergeSort(arr, left, mid),
                () => MergeSort(arr, mid + 1, right)
            );
        }
        
        Merge(arr, left, mid, right);
    }

    private void Merge<T>(T[] arr, int left, int mid, int right) where T : IComparable<T>
    {
        var sizeX = mid - left + 1;
        var sizeY = right - mid;
        
        T[] x = new T[sizeX];
        T[] y = new T[sizeY];
        int i, j;
        
        for (i = 0; i < sizeX; ++i)
            x[i] = arr[left + i];
        for (j = 0; j < sizeY; ++j)
            y[j] = arr[mid + 1 + j];
        
        i = 0;
        j = 0;

        int k = left;
        while (i < sizeX && j < sizeY) {
            if (x[i].CompareTo(y[j]) <= 0) {
                arr[k] = x[i];
                i++;
            }
            else {
                arr[k] = y[j];
                j++;
            }
            k++;
        }

        // copy remaining elements of x
        while (i < sizeX) {
            arr[k] = x[i];
            i++;
            k++;
        }

        // copy remaining elements of y
        while (j < sizeY) {
            arr[k] = y[j];
            j++;
            k++;
        }
    }
}