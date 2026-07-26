using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HtsTuner.Core.Tuning;

/// <summary>
/// A set of table definitions for a particular ROM revision, loaded from JSON
/// (comparable to a TunerPro XDF). Keeping these as external, reviewable data —
/// rather than constants baked into the app — means table addresses can be
/// validated against known-good references before anyone edits a live tune.
/// </summary>
public sealed class RomDefinition
{
    public string Name { get; set; } = "";
    public string EcuType { get; set; } = "P28";
    public List<TableDefinition> Tables { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static RomDefinition Load(string path) =>
        JsonSerializer.Deserialize<RomDefinition>(File.ReadAllText(path), Options)
        ?? new RomDefinition();

    public void Save(string path) =>
        File.WriteAllText(path, JsonSerializer.Serialize(this, Options));

    /// <summary>Binds every table definition to a ROM image for editing.</summary>
    public IEnumerable<Table> Bind(RomImage rom)
    {
        foreach (var def in Tables)
            yield return new Table(rom, def);
    }
}
