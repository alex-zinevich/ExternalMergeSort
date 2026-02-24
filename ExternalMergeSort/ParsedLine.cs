namespace ExternalMergeSort;

public readonly struct ParsedLine : IComparable<ParsedLine>
{
    public readonly int Number;
    public readonly string String;
    public readonly string Original;

    public ParsedLine(string line)
    {
        Original = line;
        var dot = line.IndexOf('.');
        Number = int.Parse(line.AsSpan(0, dot));
        String = line.Substring(dot + 1);
    }

    public int CompareTo(ParsedLine other)
    {
        if (String == other.String)
            return Number.CompareTo(other.Number);
            
        return string.CompareOrdinal(String, other.String);
    }

    public override string ToString()
    {
        return Original;
    }
}