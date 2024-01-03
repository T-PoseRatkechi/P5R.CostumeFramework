using CriFs.V2.Hook.Interfaces;
using P5R.CostumeFramework.Models;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;

namespace P5R.CostumeFramework.Characters;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
internal static class CharacterAssetsLoader
{
    private static IModLoader modLoader;
    private static ICriFsRedirectorApi criFsApi;
    private static CharacterAssetsSettings assetSettings;

    public static void Init(IModLoader modLoader, CharacterAssetsSettings assetSettings)
    {
        CharacterAssetsLoader.modLoader = modLoader;
        CharacterAssetsLoader.assetSettings = assetSettings;
        modLoader.GetController<ICriFsRedirectorApi>().TryGetTarget(out criFsApi!);
        modLoader.ModLoading += OnModLoading;
    }

    private static void OnModLoading(IModV1 mod, IModConfigV1 config)
    {
        foreach (var setting in assetSettings)
        {
            if (setting.Value != CharacterAssets.Default)
            {
                AddCharacterAssets(config, setting.Key, setting.Value);
            }
        }
    }

    private static void AddCharacterAssets(IModConfigV1 modConfig, Character character, CharacterAssets assets)
    {
        var modDir = modLoader.GetDirectoryForModId(modConfig.ModId);
        var assetsDir = GetCharacteAssetsDir(character, assets, modDir);

        if (!Directory.Exists(assetsDir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(assetsDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(assetsDir, file);
            criFsApi.AddBind(file, relativePath, "Costume Framework");
        }

        Log.Debug($"Added character assets: {character} || {assets} || {modConfig.ModName}");
    }

    private static string GetCharacteAssetsDir(Character character, CharacterAssets assets, string modDir)
        => Path.Join(modDir, "characters", character.ToString(), assets.ToString("d"));
}
