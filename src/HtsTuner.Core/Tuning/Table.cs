using System;

namespace HtsTuner.Core.Tuning;

/// <summary>
/// A live view over a region of a <see cref="RomImage"/> described by a
/// <see cref="TableDefinition"/>. Reads and writes convert between the ROM's
/// raw bytes and engineering values, and writes go straight back into the ROM
/// buffer so the image can be re-flashed / streamed to the Ostrich.
/// </summary>
public sealed class Table
{
    private readonly RomImage _rom;

    public Table(RomImage rom, TableDefinition definition)
    {
        _rom = rom;
        Definition = definition;
    }

    public TableDefinition Definition { get; }

    public int Columns => Definition.Columns;
    public int Rows => Definition.Rows;

    private int OffsetOf(int row, int col)
    {
        if ((uint)row >= (uint)Rows) throw new ArgumentOutOfRangeException(nameof(row));
        if ((uint)col >= (uint)Columns) throw new ArgumentOutOfRangeException(nameof(col));
        var index = row * Columns + col;
        return Definition.Address + index * (int)Definition.CellSize;
    }

    private int RawAt(int offset) => Definition.CellSize == CellSize.Word
        ? _rom.GetWord(offset, Definition.BigEndian)
        : _rom.GetByte(offset);

    /// <summary>Engineering value of a cell.</summary>
    public double Get(int row, int col)
    {
        var raw = RawAt(OffsetOf(row, col));
        return raw * Definition.Scale + Definition.Offset;
    }

    /// <summary>Sets a cell from an engineering value, clamping to the storable range.</summary>
    public void Set(int row, int col, double value)
    {
        var raw = (int)Math.Round((value - Definition.Offset) / Definition.Scale);
        var offset = OffsetOf(row, col);

        if (Definition.CellSize == CellSize.Word)
        {
            raw = Math.Clamp(raw, 0, ushort.MaxValue);
            _rom.SetWord(offset, (ushort)raw, Definition.BigEndian);
        }
        else
        {
            raw = Math.Clamp(raw, 0, byte.MaxValue);
            _rom.SetByte(offset, (byte)raw);
        }
    }

    /// <summary>Reads the whole table as a [row, col] grid of engineering values.</summary>
    public double[,] ToGrid()
    {
        var grid = new double[Rows, Columns];
        for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Columns; c++)
                grid[r, c] = Get(r, c);
        return grid;
    }
}
