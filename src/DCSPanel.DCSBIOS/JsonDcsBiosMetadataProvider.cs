using System.Text.Json;
using DCSPanel.DCSBIOS.Abstractions;

namespace DCSPanel.DCSBIOS;

public sealed class JsonDcsBiosMetadataProvider : IDcsBiosMetadataProvider
{
    private const string AliasesFileName = "AircraftAliases.json";

    public JsonDcsBiosMetadataProvider() : this(DiscoverMetadataDirectories())
    {
    }

    public JsonDcsBiosMetadataProvider(IEnumerable<string> candidateDirectories)
    {
        ArgumentNullException.ThrowIfNull(candidateDirectories);
        MetadataDirectory = candidateDirectories.FirstOrDefault(directory =>
            Directory.Exists(directory) && File.Exists(Path.Combine(directory, AliasesFileName)));
    }

    public string? MetadataDirectory { get; }

    public async Task<IReadOnlyList<DcsBiosControlMetadata>> GetControlsAsync(
        string aircraft,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aircraft);
        if (MetadataDirectory is null)
        {
            return [];
        }

        var aliasesPath = Path.Combine(MetadataDirectory, AliasesFileName);
        await using var aliasesStream = File.OpenRead(aliasesPath);
        using var aliases = await JsonDocument.ParseAsync(aliasesStream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!aliases.RootElement.TryGetProperty(aircraft, out var moduleNames) ||
            moduleNames.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var controls = new Dictionary<string, DcsBiosControlMetadata>(StringComparer.Ordinal);
        foreach (var moduleNameElement in moduleNames.EnumerateArray())
        {
            var moduleName = moduleNameElement.GetString();
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                continue;
            }

            var modulePath = Path.Combine(MetadataDirectory, $"{moduleName}.json");
            if (!File.Exists(modulePath))
            {
                continue;
            }

            await ReadModuleAsync(modulePath, controls, cancellationToken).ConfigureAwait(false);
        }

        return controls.Values
            .OrderBy(control => control.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(control => control.Identifier, StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task ReadModuleAsync(
        string path,
        Dictionary<string, DcsBiosControlMetadata> controls,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var categoryProperty in document.RootElement.EnumerateObject())
        {
            if (categoryProperty.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var controlProperty in categoryProperty.Value.EnumerateObject())
                {
                    var control = ParseControl(categoryProperty.Name, controlProperty.Value);
                    if (control is not null)
                    {
                        controls[control.Identifier] = control;
                    }
                }

                continue;
            }

            if (categoryProperty.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var controlElement in categoryProperty.Value.EnumerateArray())
                {
                    var control = ParseControl(categoryProperty.Name, controlElement);
                    if (control is not null)
                    {
                        controls[control.Identifier] = control;
                    }
                }
            }
        }
    }

    private static DcsBiosControlMetadata? ParseControl(string category, JsonElement element)
    {
        var identifier = GetString(element, "identifier");
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var inputs = new List<DcsBiosInputMetadata>();
        if (element.TryGetProperty("inputs", out var inputElements) && inputElements.ValueKind == JsonValueKind.Array)
        {
            foreach (var input in inputElements.EnumerateArray())
            {
                inputs.Add(new DcsBiosInputMetadata(
                    GetString(input, "interface") ?? "unknown",
                    GetInt32(input, "max_value"),
                    GetString(input, "description")));
            }
        }

        var outputs = new List<DcsBiosOutputMetadata>();
        if (element.TryGetProperty("outputs", out var outputElements) && outputElements.ValueKind == JsonValueKind.Array)
        {
            foreach (var output in outputElements.EnumerateArray())
            {
                outputs.Add(new DcsBiosOutputMetadata(
                    GetString(output, "type") ?? "unknown",
                    GetUInt16(output, "address"),
                    GetUInt16(output, "mask"),
                    GetInt32(output, "shift_by"),
                    GetInt32(output, "max_value"),
                    GetInt32(output, "max_length"),
                    GetString(output, "description")));
            }
        }

        return new DcsBiosControlMetadata(
            identifier,
            GetString(element, "description") ?? string.Empty,
            category,
            GetString(element, "control_type") ?? "unknown",
            inputs,
            outputs);
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int? GetInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;

    private static ushort? GetUInt16(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetUInt16(out var value)
            ? value
            : null;

    private static string[] DiscoverMetadataDirectories()
    {
        var savedGames = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Saved Games");
        if (!Directory.Exists(savedGames))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateDirectories(savedGames, "DCS*", SearchOption.TopDirectoryOnly)
                .Select(directory => Path.Combine(directory, "Scripts", "DCS-BIOS", "doc", "json"))
                .ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }
}
