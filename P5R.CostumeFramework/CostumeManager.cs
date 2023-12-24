using CriFs.V2.Hook.Interfaces;
using P5R.CostumeFramework.Models;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

namespace P5R.CostumeFramework;

internal class CostumeManager
{
    private readonly IModLoader modLoader;
    private readonly ICriFsRedirectorApi criFsApi;
    private readonly Dictionary<Character, CharacterCostumes> characters = new();
    private readonly string modDir;

    private readonly List<Character> costumeSets = new();

    public CostumeManager(IModLoader modLoader)
    {
        this.modLoader = modLoader;
        this.modDir = this.modLoader.GetDirectoryForModId("P5R.CostumeFramework");

        this.modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out criFsApi!);

        foreach (var character in Enum.GetValues<Character>())
        {
            this.characters[character] = new(character, this.costumeSets);
        }

        this.modLoader.ModLoading += this.OnModLoading;
    }

    public ReplacementCostume[] GetAvailableCostumes(Character character)
        => this.characters[character].Costumes.Where(x => x.ReplacementFilePath != null).ToArray();

    public bool TryGetReplacementCostume(Character character, Costume costume, out ReplacementCostume? replacementCostume)
    {
        replacementCostume = null;
        if (!this.characters.ContainsKey(character))
        {
            return false;
        }

        if (this.characters[character].Costumes.FirstOrDefault(x => x.CostumeId == costume) is ReplacementCostume replacement)
        {
            if (replacement.ReplacementFilePath != null)
            {
                replacementCostume = replacement;
                return true;
            }
        }

        return false;
    }

    private Dictionary<int, string> costumeItemNames = new();

    public bool GetCostumeName(int itemId, out string? name)
        => this.costumeItemNames.TryGetValue(itemId, out name);

    public bool IsCostumeItemId(int itemId) => this.costumeItemNames.ContainsKey(itemId);

    private Dictionary<int, string?> costumeDescriptions = new();

    public string? GetCostumeDescription(int itemId)
    {
        if (this.costumeDescriptions.TryGetValue(itemId, out var description))
        {
            return description;
        }

        return null;
    }

    private void OnModLoading(IModV1 mod, IModConfigV1 config)
    {
        if (!config.ModDependencies.Contains("P5R.CostumeFramework"))
        {
            return;
        }

        // Scan for replacement files for each character.
        var modDir = this.modLoader.GetDirectoryForModId(config.ModId);
        foreach (var character in Enum.GetValues<Character>())
        {
            var costumesDir = Path.Join(modDir, "costumes", character.ToString());
            if (!Directory.Exists(costumesDir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(costumesDir, "*.gmd", SearchOption.TopDirectoryOnly))
            {
                var relativePath = Path.GetRelativePath(modDir, file);
                var itemId = this.characters[character].AddCostume(file, relativePath);
                this.costumeItemNames[itemId] = Path.GetFileNameWithoutExtension(file);
                this.costumeDescriptions[itemId] = this.characters[character].Costumes.Last().Description;

                this.criFsApi.AddBindCallback(context =>
                {
                    context.RelativePathToFileMap[$@"R2\{relativePath}"] = new()
                    {
                        new()
                        {
                            FullPath = file,
                            LastWriteTime = DateTime.UtcNow,
                            ModId = "Costume Framework",
                        },
                    };
                });
            }
        }
    }
}
