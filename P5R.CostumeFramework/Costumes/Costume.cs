using P5R.CostumeFramework.Models;

namespace P5R.CostumeFramework.Costumes;

internal class Costume
{
    public Costume(Character character, int itemId)
    {
        this.Character = character;
        this.ItemId = itemId;
    }

    public Character Character { get; }

    public int ItemId { get; }

    public string? Name { get; set; }

    public CostumeConfig Config { get; set; } = new();

    public string? OwnerModId { get; set; }

    public string? GmdFilePath { get; set; }

    public string? GmdBindPath { get; set; }

    public string? MusicScriptFile { get; set; }

    public string? BattleThemeFile { get; set; }

    public byte[]? DescriptionMessageBinary { get; set; }

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

    /// <summary>
    /// Character weapon model.
    /// </summary>
    public string? WeaponBindPath { get; set; }
}
