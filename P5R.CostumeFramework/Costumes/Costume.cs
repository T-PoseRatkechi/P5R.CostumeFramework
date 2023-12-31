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

    public string Name { get; set; } = "MISSING NAME";

    public string? OwnerModId { get; set; }

    public string? GmdFilePath { get; set; }

    public string? GmdBindPath { get; set; }

    public string? MusicScriptFile { get; set; }

    public string? BattleThemeFile { get; set; }

    public byte[]? DescriptionMessageBinary { get; set; }

    public string? GoodbyeBindPath { get; set; }
}
