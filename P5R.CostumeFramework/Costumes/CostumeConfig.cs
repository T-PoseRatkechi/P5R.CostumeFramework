using P5R.CostumeFramework.Characters;

namespace P5R.CostumeFramework.Costumes;

internal class CostumeConfig
{
    /// <summary>
    /// Should costume be used as a default when needed.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Assets setting this costume should be used with.
    /// </summary>
    public CharacterAssets CharacterAssets { get; set; }
}
