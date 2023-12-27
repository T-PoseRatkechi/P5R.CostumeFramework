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

    public string? ReplacementFilePath { get; set; }

    public string? ReplacementBindPath { get; set; }

    public string? MusicScriptFile { get; set; }

    public byte[]? DescriptionMessageBinary { get; set; }
}
