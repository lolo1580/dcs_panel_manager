using System.Globalization;
using DCSPanel.Core.Mapping;
using DCSPanel.DCSBIOS.Abstractions;

namespace DCSPanel.App.ViewModels;

public sealed class DcsBiosControlViewModel
{
    private readonly string _searchText;

    public DcsBiosControlViewModel(DcsBiosControlMetadata metadata)
    {
        Metadata = metadata;
        Identifier = metadata.Identifier;
        Category = metadata.Category;
        Description = string.IsNullOrWhiteSpace(metadata.Description) ? "No description provided" : metadata.Description;
        ControlType = metadata.ControlType;
        InputSummary = metadata.Inputs.Count == 0
            ? "Read-only"
            : string.Join(", ", metadata.Inputs.Select(input => input.Interface).Distinct(StringComparer.OrdinalIgnoreCase));
        OutputSummary = metadata.Outputs.Count == 0
            ? "-"
            : string.Join(", ", metadata.Outputs.Select(output => output.Type).Distinct(StringComparer.OrdinalIgnoreCase));
        _searchText = string.Join(' ',
            metadata.Identifier,
            metadata.Category,
            metadata.Description,
            metadata.ControlType,
            string.Join(' ', metadata.Inputs.Select(input => input.Interface)),
            string.Join(' ', metadata.Outputs.Select(output => output.Type)));
    }

    public string Identifier { get; }
    public DcsBiosControlMetadata Metadata { get; }
    public string Category { get; }
    public string Description { get; }
    public string ControlType { get; }
    public string InputSummary { get; }
    public string OutputSummary { get; }
    public bool CanReceiveCommands => Metadata.Inputs.Count > 0;

    public bool Matches(string searchText) =>
        string.IsNullOrWhiteSpace(searchText) ||
        _searchText.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase);

    public string SuggestArgument(MappingTriggerOption? trigger)
    {
        if (trigger is null)
        {
            return string.Empty;
        }

        var interfaces = Metadata.Inputs;
        if (trigger.Kind is PhysicalInputKind.EncoderClockwise or PhysicalInputKind.EncoderCounterClockwise)
        {
            if (interfaces.Any(input => input.Interface.Equals("variable_step", StringComparison.OrdinalIgnoreCase)))
            {
                var step = interfaces.First(input => input.Interface.Equals("variable_step", StringComparison.OrdinalIgnoreCase))
                    .SuggestedStep ?? 1;
                return trigger.Kind == PhysicalInputKind.EncoderClockwise
                    ? "+" + step.ToString(CultureInfo.InvariantCulture)
                    : (-step).ToString(CultureInfo.InvariantCulture);
            }

            if (interfaces.Any(input => input.Interface.Equals("fixed_step", StringComparison.OrdinalIgnoreCase)))
            {
                return trigger.Kind == PhysicalInputKind.EncoderClockwise ? "INC" : "DEC";
            }
        }

        if (interfaces.Any(input => input.Interface.Equals("set_state", StringComparison.OrdinalIgnoreCase)))
        {
            return trigger.IsActive ? "1" : "0";
        }

        if (interfaces.Any(input => input.Interface.Equals("action", StringComparison.OrdinalIgnoreCase)))
        {
            return "TOGGLE";
        }

        if (interfaces.Any(input => input.Interface.Equals("fixed_step", StringComparison.OrdinalIgnoreCase)))
        {
            return trigger.IsActive ? "INC" : "DEC";
        }

        return string.Empty;
    }

    public bool IsValidArgument(string argument)
    {
        foreach (var input in Metadata.Inputs)
        {
            switch (input.Interface.ToLowerInvariant())
            {
                case "action" when argument.Equals("TOGGLE", StringComparison.OrdinalIgnoreCase):
                case "fixed_step" when argument.Equals("INC", StringComparison.OrdinalIgnoreCase) ||
                                            argument.Equals("DEC", StringComparison.OrdinalIgnoreCase):
                case "set_string":
                    return true;
                case "set_state" when int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var state) && state >= 0 &&
                                           (input.MaxValue is null || state <= input.MaxValue):
                case "variable_step" when int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var step) && step != 0:
                    return true;
            }
        }

        return false;
    }
}
