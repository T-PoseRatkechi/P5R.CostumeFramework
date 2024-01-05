using P5R.CostumeFramework.Models;

namespace P5R.CostumeFramework.Costumes;

internal class Costume
{
    public const string DEFAULT_DESCRIPTION = "[f 0 5 65278][f 2 1]Outfit added with Costume Framework.[n][e]";

    public Costume(Character character, int itemId)
    {
        this.Character = character;
        this.ItemId = itemId;
    }

    public Character Character { get; }

    public int ItemId { get; }

    public bool IsEnabled { get; set; }

    public string? Name { get; set; }

    public CostumeConfig Config { get; set; } = new();

    public string? OwnerModId { get; set; }

    public string? GmdFilePath { get; set; }

    public string? GmdBindPath { get; set; }

    public string? MusicScriptFile { get; set; }

    public string? BattleThemeFile { get; set; }

    public string DescriptionMsg { get; set; } = DEFAULT_DESCRIPTION;

    /// <summary>
    /// AOA character animation ending.
    /// </summary>
    public string? GoodbyeBindPath { get; set; }

    /// <summary>
    /// Crit/weakness cutin image.
    /// </summary>
    public string? CutinBindPath { get; set; }

    /// <summary>
    /// AOA portrait.
    /// </summary>
    public string? GuiBindFile { get; set; }

    public string? WeaponBindPath { get; set; }

    public string? WeaponRBindPath { get; set; }

    public string? WeaponLBindPath { get; set; }

    public string? RangedBindPath { get; set; }

    public string? RangedRBindPath { get; set; }

    public string? RangedLBindPath { get; set; }

    /// <summary>
    /// Futaba skill BCD.
    /// </summary>
    public string? FutabaSkillBind { get; set; }
}
