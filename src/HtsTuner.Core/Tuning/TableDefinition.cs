namespace HtsTuner.Core.Tuning;

/// <summary>Storage size of one table cell in the ROM.</summary>
public enum CellSize
{
    Byte = 1,
    Word = 2,
}

/// <summary>
/// Describes where a tuning table lives in the ROM and how to interpret its
/// raw bytes: <c>value = raw * Scale + Offset</c>.
///
/// Definitions are data, supplied and validated per ROM revision — they are
/// deliberately NOT hardcoded from the decompiled binary, because a wrong
/// address or scale on a fuel/ignition table produces a dangerous tune.
/// </summary>
public sealed class TableDefinition
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";  // e.g. "Fuel", "Ignition"

    /// <summary>Absolute byte offset of the first cell in the ROM.</summary>
    public int Address { get; set; }

    public int Columns { get; set; } = 1;
    public int Rows { get; set; } = 1;

    public CellSize CellSize { get; set; } = CellSize.Byte;
    public bool BigEndian { get; set; } = true;

    public double Scale { get; set; } = 1.0;
    public double Offset { get; set; }

    public string Unit { get; set; } = "";

    /// <summary>Optional axis labels; UI-only, may be empty.</summary>
    public double[] ColumnAxis { get; set; } = System.Array.Empty<double>();
    public double[] RowAxis { get; set; } = System.Array.Empty<double>();

    public int CellCount => Columns * Rows;
}
