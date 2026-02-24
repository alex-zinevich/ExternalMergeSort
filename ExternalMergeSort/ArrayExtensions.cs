namespace ExternalMergeSort;

public static class ArrayExtensions
{
    public static void Deconstruct<T>(this T?[] array, out T? first, out T? second)
    {
        if(array is null) throw new ArgumentNullException(nameof(array));
        
        first = array.Length > 0 ? array[0] : default;
        second = array.Length > 1 ? array[1] : default;
    }
}