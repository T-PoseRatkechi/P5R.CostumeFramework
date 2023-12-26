using CriFs.V2.Hook.Interfaces;
using P5R.CostumeFramework.Models;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

namespace P5R.CostumeFramework.Costumes;

internal class CostumeRegistry
{
    private readonly IModLoader modLoader;
    private readonly ICriFsRedirectorApi criFsApi;
    private readonly List<Costume> costumes = new();

    public CostumeRegistry(IModLoader modLoader)
    {
        this.modLoader = modLoader;
        this.modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out criFsApi!);
        this.modLoader.ModLoading += this.OnModLoading;

        var characters = Enum.GetValues<Character>();
        for (int currentSet = 0; currentSet < VirtualOutfitsSection.GAME_OUTFIT_SETS + VirtualOutfitsSection.MOD_OUTFIT_SETS; currentSet++)
        {
            foreach (var character in characters)
            {
                var itemId = 0x7010 + (currentSet * 10) + (int)character - 1;
                this.costumes.Add(new(character, itemId));
            }
        }
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
                var modCostume = this.GetAvailableModCostume(character);
                if (modCostume == null)
                {
                    Log.Warning($"No available costume slots for: {character}");
                    continue;
                }

                modCostume.Name = Path.GetFileNameWithoutExtension(file);
                modCostume.ReplacementFilePath = file;
                modCostume.ReplacementBindPath = relativePath;

                Log.Information($"Assigned Costume || Character: {modCostume.Character} || Item ID: {modCostume.ItemId} || {modCostume.ReplacementBindPath}");
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

    public bool TryGetModCostume(int itemId, out Costume? costume)
    {
        costume = this.costumes.FirstOrDefault(x => x.ItemId == itemId && x.ReplacementFilePath != null);
        return costume != null;
    }

    public bool IsActiveModCostume(int itemId)
        => VirtualOutfitsSection.IsModOutfit(itemId)
        && this.costumes.FirstOrDefault(x => x.ItemId == itemId)?.ReplacementFilePath != null;

    private Costume? GetAvailableModCostume(Character character)
        => this.costumes
        .FirstOrDefault(x =>
        x.Character == character
        && VirtualOutfitsSection.IsModOutfit(x.ItemId)
        && x.ReplacementFilePath == null);
}
