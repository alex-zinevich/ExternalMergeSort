using System.Diagnostics;
using System.Text;

namespace FileGenerator;

public sealed class FileGenerator
{
    private static readonly string[] StringPool =
    [
        "Apple", "Banana is yellow", "Cherry is the best", "Dragonfruit",
        "Elderberry", "Fig is sweet", "Grape", "Honeydew melon",
        "Kiwi is green and fuzzy", "Lemon", "Mango is tropical",
        "Nectarine", "Orange is both a fruit and a color", "Papaya",
        "Quince", "Raspberry", "Strawberry shortcake", "Tangerine",
        "Ugli fruit", "Vanilla bean", "Watermelon is mostly water",
        "Ximenia", "Yellow passion fruit", "Zucchini is technically a fruit",
        "The quick brown fox jumps over the lazy dog",
        "Something something something",
        "To be or not to be that is the question",
        "Pack my box with five dozen liquor jugs",
        "How vexingly quick daft zebras jump",
        "The five boxing wizards jump quickly",
        "Sphinx of black quartz judge my vow",
        "Bright copper kettles and warm woolen mittens",
        "Supercalifragilisticexpialidocious",
        "A stitch in time saves nine",
        "All that glitters is not gold",
        "The road not taken makes all the difference",
        "Elementary my dear Watson",
        "May the force be with you",
        "Houston we have a problem",
        "I am the very model of a modern major general",
    ];

    // -----------------------------------------------------------------------
    //  Configuration
    // -----------------------------------------------------------------------

    /// <summary>Total target output size in bytes.</summary>
    public long TargetSizeBytes { get; set; } = 100L * 1024 * 1024;

    /// <summary>FileStream internal write buffer 1 MB.</summary>
    public int WriteBufferBytes { get; set; } = 1024 * 1024;

    /// <summary>Fraction of lines that reuse a "duplicate" string (0.0–1.0).</summary>
    public double DuplicateRatio { get; set; } = 0.30;

    /// <summary>How many distinct strings are in the "duplicate" pool.</summary>
    public int DuplicatePoolSize { get; set; } = 10;

    /// <summary>Max value for the numeric prefix.</summary>
    public int MaxNumber { get; set; } = 999_999;

    /// <summary>Number of pre-rendered lines in the cache.</summary>
    public int LineCacheSize { get; set; } = 200_000;

    /// <summary>Random seed for reproducibility.</summary>
    public int Seed { get; set; } = 42;

    /// <summary>Final output file path.</summary>
    public string OutputPath { get; set; } = "test_input.txt";

    // -----------------------------------------------------------------------
    //  Generate
    // -----------------------------------------------------------------------

    public void Generate()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║     Test File Generator                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝");
        Console.WriteLine($"  Target      : {FormatSize(TargetSizeBytes)}");
        Console.WriteLine($"  Write buffer: {FormatSize(WriteBufferBytes)}");
        Console.WriteLine($"  Cache size  : {LineCacheSize:N0} pre-built lines");
        Console.WriteLine($"  Dup ratio   : {DuplicateRatio * 100:F0}%");
        Console.WriteLine($"  Output      : {Path.GetFullPath(OutputPath)}");
        Console.WriteLine();

        // Phase 1 ── build the line cache
        Console.Write("Building line cache... ");
        var cacheSw = Stopwatch.StartNew();
        var lineCache = BuildLineCache();
        cacheSw.Stop();
        Console.WriteLine($"done in {cacheSw.ElapsedMilliseconds} ms");
        Console.WriteLine();

        // Phase 2 ── single sequential write
        WriteFile(lineCache);
    }

    // -----------------------------------------------------------------------
    //  Phase 1 — Pre-render lines as UTF-8 byte arrays
    // -----------------------------------------------------------------------

    private byte[][] BuildLineCache()
    {
        var rng = new Random(Seed);

        // Pick the duplicate pool
        var dupeStrings = StringPool
            .OrderBy(_ => rng.Next())
            .Take(Math.Min(DuplicatePoolSize, StringPool.Length))
            .ToArray();

        Console.WriteLine("  Duplicate strings (will appear most often):");
        foreach (var s in dupeStrings)
            Console.WriteLine($"    \"{s}\"");

        var cache = new byte[LineCacheSize][];
        for (int i = 0; i < LineCacheSize; i++)
        {
            string text = rng.NextDouble() < DuplicateRatio
                ? dupeStrings[rng.Next(dupeStrings.Length)]
                : StringPool[rng.Next(StringPool.Length)];

            // Format: "<number>. <text>\n"
            cache[i] = Encoding.UTF8.GetBytes($"{rng.Next(1, MaxNumber + 1)}. {text}\n");
        }

        return cache;
    }

    // -----------------------------------------------------------------------
    //  Phase 2 — Sequential write with timer-based progress
    // -----------------------------------------------------------------------

    private void WriteFile(byte[][] lineCache)
    {
        var rng = new Random(Seed + 1); // different seed so sequence differs from cache build
        int cacheLen = lineCache.Length;
        long written = 0;
        var sw = Stopwatch.StartNew();

        // Timer fires every second to print progress — never touches the write loop
        using var progressTimer = new Timer(_ =>
        {
            long w = Interlocked.Read(ref written);
            double pct = (double)w / TargetSizeBytes * 100.0;
            double mbps = w / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
            double etaSec = mbps > 0 ? (TargetSizeBytes - w) / 1024.0 / 1024.0 / mbps : 0;

            Console.Write($"\r  {pct,5:F1}%   {FormatSize(w),10}   {mbps,6:F0} MB/s   " +
                          $"ETA {TimeSpan.FromSeconds(etaSec):mm\\:ss}   ");
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        using (var fs = new FileStream(
            OutputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: WriteBufferBytes,
            useAsync: false)) 
        {
            // Pick a cached line, write it.
            while (written < TargetSizeBytes)
            {
                byte[] line = lineCache[rng.Next(cacheLen)];
                fs.Write(line, 0, line.Length);
                Interlocked.Add(ref written, line.Length);
            }
        }

        sw.Stop();

        // Stop the timer and print the final 100% line
        progressTimer.Change(Timeout.Infinite, Timeout.Infinite);

        double totalMbps = written / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"\r  100.0%   {FormatSize(written),10}   {totalMbps,6:F0} MB/s   " +
                          $"Done in {sw.Elapsed:mm\\:ss}        ");
        Console.WriteLine();
        Console.WriteLine($"✓ {Path.GetFullPath(OutputPath)}  ({FormatSize(new FileInfo(OutputPath).Length)})");
        Console.WriteLine();

        // Preview
        Console.WriteLine("Preview (first 10 lines):");
        Console.WriteLine(new string('─', 55));
        int n = 0;
        foreach (var line in File.ReadLines(OutputPath))
        {
            Console.WriteLine(line);
            if (++n >= 10) break;
        }
        Console.WriteLine(new string('─', 55));
    }
    
    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024                => $"{bytes} B",
        < 1024 * 1024         => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0:F1} MB",
        _                     => $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB",
    };
}