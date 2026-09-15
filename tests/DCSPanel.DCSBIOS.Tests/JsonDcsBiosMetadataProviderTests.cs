namespace DCSPanel.DCSBIOS.Tests;

public sealed class JsonDcsBiosMetadataProviderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dcs-panel-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task GetAircraftAsyncReturnsSortedNonEmptyAliases()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "AircraftAliases.json"),
            """{"UH-1H":["CommonData","UH-1H"],"":["MetadataStart"],"F-16C_50":["CommonData","F-16C_50"]}""");
        var provider = new JsonDcsBiosMetadataProvider([_directory]);

        var aircraft = await provider.GetAircraftAsync();

        Assert.Equal(["F-16C_50", "UH-1H"], aircraft);
    }

    [Fact]
    public async Task GetControlsAsyncLoadsAllModulesDeclaredByAircraftAlias()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "AircraftAliases.json"),
            """{"F-16C_50":["CommonData","F-16C_50"]}""");
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "CommonData.json"),
            """
            {
              "Metadata": {
                "ALT_MSL_FT": {
                  "identifier": "ALT_MSL_FT",
                  "description": "Altitude MSL",
                  "control_type": "metadata",
                  "inputs": [],
                  "outputs": [{"type":"integer","address":1024,"mask":65535,"shift_by":0,"max_value":65535}]
                }
              }
            }
            """);
        await File.WriteAllTextAsync(
            Path.Combine(_directory, "F-16C_50.json"),
            """
            {
              "Electrical power panel": {
                "MAIN_PWR_SW": {
                  "identifier": "MAIN_PWR_SW",
                  "description": "Main power switch",
                  "control_type": "selector",
                  "inputs": [{"interface":"set_state","max_value":2},{"interface":"variable_step","max_value":2,"suggested_step":1}],
                  "outputs": [{"type":"integer","address":4096,"mask":3,"shift_by":0,"max_value":2}]
                }
              }
            }
            """);
        var provider = new JsonDcsBiosMetadataProvider([_directory]);

        var controls = await provider.GetControlsAsync("F-16C_50");

        Assert.Equal(2, controls.Count);
        var mainPower = Assert.Single(controls, control => control.Identifier == "MAIN_PWR_SW");
        Assert.Contains(mainPower.Inputs, input => input.Interface == "set_state");
        Assert.Equal(1, mainPower.Inputs.Single(input => input.Interface == "variable_step").SuggestedStep);
        Assert.Equal((ushort)0x1000, Assert.Single(mainPower.Outputs).Address);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
