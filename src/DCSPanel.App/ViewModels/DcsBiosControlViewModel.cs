using DCSPanel.DCSBIOS.Abstractions;

namespace DCSPanel.App.ViewModels;

public sealed class DcsBiosControlViewModel(DcsBiosControlMetadata metadata)
{
    private readonly string _searchText = string.Join(' ',
        metadata.Identifier,
        metadata.Category,
        metadata.Description,
        metadata.ControlType,
        string.Join(' ', metadata.Inputs.Select(input => input.Interface)),
        string.Join(' ', metadata.Outputs.Select(output => output.Type)));

    public string Identifier { get; } = metadata.Identifier;
    public string Category { get; } = metadata.Category;
    public string Description { get; } = string.IsNullOrWhiteSpace(metadata.Description)
        ? "No description provided"
        : metadata.Description;
    public string ControlType { get; } = metadata.ControlType;
    public string InputSummary { get; } = metadata.Inputs.Count == 0
        ? "Read-only"
        : string.Join(", ", metadata.Inputs
            .Select(input => input.Interface)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    public string OutputSummary { get; } = metadata.Outputs.Count == 0
        ? "-"
        : string.Join(", ", metadata.Outputs
            .Select(output => output.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase));

    public bool Matches(string searchText) =>
        string.IsNullOrWhiteSpace(searchText) ||
        _searchText.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase);
}
