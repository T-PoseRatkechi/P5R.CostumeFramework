using P5R.CostumeFramework.Template.Configuration;
using System.ComponentModel;

namespace P5R.CostumeFramework.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Log Level")]
    [DefaultValue(LogLevel.Information)]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [DisplayName("Randomize Costumes")]
    [Description("Costume will randomize when moving between areas.")]
    [DefaultValue(false)]
    public bool RandomizeCostumes { get; set; } = false;

    [DisplayName("Overworld Costumes")]
    [Description("Costumes will apply in the overworld too.\nThis is just a for fun feature, expect some non-game breaking visual bugs.")]
    [DefaultValue(false)]
    public bool OverworldCostumes { get; set; } = false;

    [DisplayName("(Debug) Unlock All Items")]
    [DefaultValue(false)]
    public bool UnlockAllItems { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}