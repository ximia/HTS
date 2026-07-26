using System.Collections.Generic;
using System.Linq;

namespace HtsTuner.Core.Tuning;

/// <summary>A cell coordinate in a table.</summary>
public readonly record struct Cell(int Row, int Col);

/// <summary>
/// The map-editing math tuners expect: nudge, scale by percent, set, interpolate
/// and smooth over a selection. All operate through <see cref="Table"/> so edits
/// land in the ROM image (with the definition's scale/clamp applied).
/// </summary>
public static class TableOps
{
    /// <summary>Adds <paramref name="delta"/> to every selected cell.</summary>
    public static void Adjust(Table table, IEnumerable<Cell> cells, double delta)
    {
        foreach (var c in cells)
            table.Set(c.Row, c.Col, table.Get(c.Row, c.Col) + delta);
    }

    /// <summary>Multiplies every selected cell by (1 + percent/100).</summary>
    public static void Scale(Table table, IEnumerable<Cell> cells, double percent)
    {
        var factor = 1.0 + percent / 100.0;
        foreach (var c in cells)
            table.Set(c.Row, c.Col, table.Get(c.Row, c.Col) * factor);
    }

    /// <summary>Sets every selected cell to an absolute value.</summary>
    public static void SetValue(Table table, IEnumerable<Cell> cells, double value)
    {
        foreach (var c in cells)
            table.Set(c.Row, c.Col, value);
    }

    /// <summary>
    /// Linear interpolation across each selected row, between the leftmost and
    /// rightmost selected columns of that row.
    /// </summary>
    public static void InterpolateHorizontal(Table table, IReadOnlyCollection<Cell> cells)
    {
        foreach (var rowGroup in cells.GroupBy(c => c.Row))
        {
            var cols = rowGroup.Select(c => c.Col).OrderBy(x => x).ToList();
            if (cols.Count < 3) continue;
            int lo = cols.First(), hi = cols.Last();
            double a = table.Get(rowGroup.Key, lo), b = table.Get(rowGroup.Key, hi);
            for (var col = lo + 1; col < hi; col++)
            {
                var t = (double)(col - lo) / (hi - lo);
                table.Set(rowGroup.Key, col, a + (b - a) * t);
            }
        }
    }

    /// <summary>Linear interpolation down each selected column.</summary>
    public static void InterpolateVertical(Table table, IReadOnlyCollection<Cell> cells)
    {
        foreach (var colGroup in cells.GroupBy(c => c.Col))
        {
            var rows = colGroup.Select(c => c.Row).OrderBy(x => x).ToList();
            if (rows.Count < 3) continue;
            int lo = rows.First(), hi = rows.Last();
            double a = table.Get(lo, colGroup.Key), b = table.Get(hi, colGroup.Key);
            for (var row = lo + 1; row < hi; row++)
            {
                var t = (double)(row - lo) / (hi - lo);
                table.Set(row, colGroup.Key, a + (b - a) * t);
            }
        }
    }

    /// <summary>
    /// Replaces each selected cell with the average of its selected neighbours
    /// (including itself) — a light 4-neighbour smoothing pass.
    /// </summary>
    public static void Smooth(Table table, IReadOnlyCollection<Cell> cells)
    {
        var set = cells.ToHashSet();
        var updated = new Dictionary<Cell, double>();
        foreach (var c in cells)
        {
            double sum = table.Get(c.Row, c.Col);
            var n = 1;
            foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
            {
                var nb = new Cell(c.Row + dr, c.Col + dc);
                if (set.Contains(nb)) { sum += table.Get(nb.Row, nb.Col); n++; }
            }
            updated[c] = sum / n;
        }
        foreach (var (c, v) in updated)
            table.Set(c.Row, c.Col, v);
    }
}
