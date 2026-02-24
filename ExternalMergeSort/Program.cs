using ExternalMergeSort;

var sorter = new LoggedSorter(new ExternalMergeSorter { MaxMemoryBytes = 512L * 1024 * 1024 });

var sw = System.Diagnostics.Stopwatch.StartNew();
sorter.Sort("test_input.txt", "test_output.txt");
sw.Stop();

Console.WriteLine($"\nSort completed in {sw.Elapsed.TotalSeconds:F2}s");
