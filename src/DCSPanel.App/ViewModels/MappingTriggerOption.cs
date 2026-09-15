using DCSPanel.Core.Mapping;

namespace DCSPanel.App.ViewModels;

public sealed record MappingTriggerOption(string Label, PhysicalInputKind Kind, bool IsActive);
