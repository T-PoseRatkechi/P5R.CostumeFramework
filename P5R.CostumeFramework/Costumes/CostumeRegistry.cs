using AtlusScriptLibrary.Common.Libraries;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using CriFs.V2.Hook.Interfaces;
using P5R.CostumeFramework.Models;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

namespace P5R.CostumeFramework.Costumes;

internal class CostumeRegistry
{
    private readonly IModLoader modLoader;
    private readonly ICriFsRedirectorApi criFsApi;
    private readonly CostumeFactory costumeFactory;

    private readonly GameCostumes costumes = new();
    private readonly Dictionary<Character, Costume> randomizedCostumes;

    public CostumeRegistry(IModLoader modLoader)
    {
        this.modLoader = modLoader;
        this.modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out criFsApi!);
        this.modLoader.ModLoading += this.OnModLoading;

        var modDir = modLoader.GetDirectoryForModId("P5R.CostumeFramework");
        AtlusEncoding.SetCharsetDirectory(Path.Join(modDir, "Charsets"));
        LibraryLookup.SetLibraryPath(Path.Join(modDir, "Libraries"));
        var compiler = new MessageScriptCompiler(FormatVersion.Version1BigEndian, AtlusEncoding.Persona5RoyalEFIGS)
        {
            Library = LibraryLookup.GetLibrary("p5r")
        };

        this.costumeFactory = new(criFsApi, compiler, this.costumes);

        this.randomizedCostumes = CostumeRegistryUtils.AddRandomizedCostumes(this.costumeFactory)
            .GroupBy(x => x.Character)
            .ToDictionary(x => x.Key, x => x.First());

        CostumeRegistryUtils.AddExistingCostumes(this.costumeFactory);
    }

    public Costume? GetCostumeById(int itemId)
        => this.costumes.FirstOrDefault(x => x.ItemId == itemId);

    public bool TryGetModCostume(int itemId, out Costume? costume)
    {
        costume = this.costumes.FirstOrDefault(x => x.ItemId == itemId && x.GmdFilePath != null)!;
        return costume != null;
    }

    public bool IsActiveModCostume(int itemId)
        => VirtualOutfitsSection.IsModOutfit(itemId)
        && this.costumes.FirstOrDefault(x => x.ItemId == itemId)?.GmdFilePath != null;

    public Costume? GetRandomCostume(Character character)
    {
        var costumes = this.costumes
            .Where(x => x.Character == character)
            .Where(x => x.GmdBindPath != null).ToArray();
        if (costumes.Length < 1)
        {
            return null;
        }

        return costumes[Random.Shared.Next(0, costumes.Length)];
    }

    private void OnModLoading(IModV1 mod, IModConfigV1 config)
    {
        if (!config.ModDependencies.Contains("P5R.CostumeFramework"))
        {
            return;
        }

        var modDir = this.modLoader.GetDirectoryForModId(config.ModId);
        this.AddBindFiles(modDir);

        // Register mod costumes.
        foreach (var character in Enum.GetValues<Character>())
        {
            var characterDir = Path.Join(modDir, "costumes", character.ToString());
            if (!Directory.Exists(characterDir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(characterDir, "*.gmd", SearchOption.TopDirectoryOnly))
            {
                this.costumeFactory.Create(config.ModId, modDir, character, file);
            }
        }
    }

    private void AddBindFiles(string modDir)
    {
        var bindDir = Path.Join(modDir, "costumes", "bind");
        if (Directory.Exists(bindDir))
        {
            foreach (var file in Directory.EnumerateFiles(bindDir, "*", SearchOption.AllDirectories))
            {
                var relativeFilePath = Path.GetRelativePath(bindDir, file);
                this.criFsApi.AddBind(file, relativeFilePath, "Costume Framework");
                Log.Debug($"Costume file binded: {relativeFilePath}");
            }
        }
    }
}
