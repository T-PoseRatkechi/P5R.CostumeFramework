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

    public CostumeRegistry(IModLoader modLoader)
    {
        this.modLoader = modLoader;
        this.modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out criFsApi!);
        this.modLoader.ModLoading += this.OnModLoading;

        AtlusEncoding.SetCharsetDirectory(Path.Join(modLoader.GetDirectoryForModId("P5R.CostumeFramework"), "Charsets"));
        LibraryLookup.SetLibraryPath(Path.Join(modLoader.GetDirectoryForModId("P5R.CostumeFramework"), "Libraries"));
        var compiler = new MessageScriptCompiler(FormatVersion.Version1BigEndian, AtlusEncoding.Persona5RoyalEFIGS)
        {
            Library = LibraryLookup.GetLibrary("p5r")
        };

        this.costumeFactory = new(criFsApi, compiler, this.costumes);

        this.costumeFactory.CreateCostume(Character.Akechi, "Messy Hair Akechi", @"MODEL\CHARACTER\0009\C0009_073_00.GMD");
        this.costumeFactory.CreateCostume(Character.Akechi, "Ratkechi", @"MODEL\CHARACTER\0009\C0009_099_00.GMD");
    }

    public bool TryGetModCostume(int itemId, out Costume costume)
    {
        costume = this.costumes.FirstOrDefault(x => x.ItemId == itemId && x.GmdFilePath != null)!;
        return costume != null;
    }

    public bool IsActiveModCostume(int itemId)
        => VirtualOutfitsSection.IsModOutfit(itemId)
        && this.costumes.FirstOrDefault(x => x.ItemId == itemId)?.GmdFilePath != null;

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
                this.costumeFactory.CreateCostume(modDir, character, file);
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
