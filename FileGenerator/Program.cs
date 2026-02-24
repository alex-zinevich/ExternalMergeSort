namespace FileGenerator;
    
class Program
{
    static void Main(string[] args)
    {
        var g = new FileGenerator();

        // CLI:  generator.exe [size] [output] [dupRatio] [seed]
        if (args.Length >= 1) g.TargetSizeBytes = ParseSize(args[0]);
        if (args.Length >= 2) g.OutputPath      = args[1];
        if (args.Length >= 3 && double.TryParse(args[2], out double d)) g.DuplicateRatio = d;
        if (args.Length >= 4 && int.TryParse(args[3], out int s))       g.Seed           = s;

        if (args.Length == 0)
        {
            g.TargetSizeBytes = AskSize();
            g.OutputPath      = AskString("Output file path", g.OutputPath);
            g.DuplicateRatio  = AskDouble("Duplicate ratio (0.0–1.0)", g.DuplicateRatio);
            g.Seed            = AskInt("Random seed", g.Seed);
            Console.WriteLine();
        }

        g.Generate();
    }

    private static long ParseSize(string input)
    {
        input = input.Trim().ToUpperInvariant();
        if (input.EndsWith("GB") && double.TryParse(input[..^2], out double gb))
            return (long)(gb * 1024 * 1024 * 1024);
        if (input.EndsWith("MB") && double.TryParse(input[..^2], out double mb))
            return (long)(mb * 1024 * 1024);
        if (input.EndsWith("KB") && double.TryParse(input[..^2], out double kb))
            return (long)(kb * 1024);
        if (double.TryParse(input, out double raw))
            return (long)(raw * 1024 * 1024);
        return 100L * 1024 * 1024;
    }

    private static long AskSize()
    {
        Console.Write("Target file size (e.g. 80GB, 500MB) [100MB]: ");
        return ParseSize(Console.ReadLine()?.Trim() ?? "100MB");
    }

    private static string AskString(string prompt, string def)
    {
        Console.Write($"{prompt} [{def}]: ");
        var v = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(v) ? def : v;
    }

    private static double AskDouble(string prompt, double def)
    {
        Console.Write($"{prompt} [{def}]: ");
        return double.TryParse(Console.ReadLine()?.Trim(), out double v)
            ? Math.Clamp(v, 0, 1) : def;
    }

    private static int AskInt(string prompt, int def)
    {
        Console.Write($"{prompt} [{def}]: ");
        return int.TryParse(Console.ReadLine()?.Trim(), out int v) ? v : def;
    }
}