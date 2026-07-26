using System;
using System.IO;

namespace HtsTuner.Core.Tuning;

/// <summary>
/// A ROM image for an OBD1 Honda ECU (e.g. P28). The original HTS stored the
/// image as a base ECU region plus an extended datalogging/config region; here
/// it's a single flat buffer addressed by absolute offset, which is equivalent
/// and simpler. A stock P28 base ROM is 32 KiB (0x8000).
/// </summary>
public sealed class RomImage
{
    /// <summary>Size of a stock OBD1 Honda base ROM.</summary>
    public const int BaseRomSize = 0x8000; // 32 KiB

    private readonly byte[] _data;

    public RomImage(byte[] data) => _data = data ?? throw new ArgumentNullException(nameof(data));

    public int Length => _data.Length;

    /// <summary>True when the buffer is exactly the stock 32 KiB base size.</summary>
    public bool IsStockSize => _data.Length == BaseRomSize;

    public static RomImage Load(string path) => new(File.ReadAllBytes(path));

    public void Save(string path) => File.WriteAllBytes(path, _data);

    /// <summary>Copies bytes into the existing buffer (used for undo/redo).</summary>
    public void CopyFrom(byte[] snapshot)
    {
        if (snapshot.Length != _data.Length)
            throw new ArgumentException("Snapshot size does not match ROM size.", nameof(snapshot));
        Array.Copy(snapshot, _data, _data.Length);
    }

    public byte GetByte(int offset) => _data[offset];

    public void SetByte(int offset, byte value) => _data[offset] = value;

    public ushort GetWord(int offset, bool bigEndian) => bigEndian
        ? (ushort)((_data[offset] << 8) | _data[offset + 1])
        : (ushort)((_data[offset + 1] << 8) | _data[offset]);

    public void SetWord(int offset, ushort value, bool bigEndian)
    {
        if (bigEndian)
        {
            _data[offset] = (byte)(value >> 8);
            _data[offset + 1] = (byte)(value & 0xFF);
        }
        else
        {
            _data[offset] = (byte)(value & 0xFF);
            _data[offset + 1] = (byte)(value >> 8);
        }
    }

    /// <summary>Returns a defensive copy of the raw bytes.</summary>
    public byte[] ToArray() => (byte[])_data.Clone();
}
