using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using CriFs.V2.Hook.Interfaces;
using P5R.CostumeFramework.Models;

namespace P5R.CostumeFramework.Costumes;

internal class CostumeFactory
{
    private readonly ICriFsRedirectorApi criFsApi;
    private readonly MessageScriptCompiler compiler;
    private readonly GameCostumes costumes;

    public CostumeFactory(
        ICriFsRedirectorApi criFsApi,
        MessageScriptCompiler compiler,
        GameCostumes costumes)
    {
        this.criFsApi = criFsApi;
        this.compiler = compiler;
        this.costumes = costumes;
    }

    public Costume? Create(string modId, string modDir, Character character, string gmdFile)
    {
        var modCostume = this.GetAvailableModCostume(character);
        if (modCostume == null)
        {
            Log.Warning($"No available costume slots for: {character}");
            return null;
        }

        modCostume.Name = Path.GetFileNameWithoutExtension(gmdFile);
        modCostume.OwnerModId = modId;
        this.AddGmdFile(modCostume, gmdFile, modDir);
        this.AddDescription(modCostume);
        this.AddMusic(modCostume);
        this.AddGoodbye(modCostume, modDir);

        Log.Information($"Costume created: {modCostume.Character} || Item ID: {modCostume.ItemId} || Bind: {modCostume.GmdBindPath}");
        return modCostume;
    }

    public Costume? CreateFromExisting(Character character, string name, string bindPath)
    {
        var modCostume = this.GetAvailableModCostume(character);
        if (modCostume == null)
        {
            Log.Warning($"No available costume slots for: {character}");
            return null;
        }

        modCostume.Name = name;
        modCostume.GmdFilePath = string.Empty;
        modCostume.GmdBindPath = bindPath;
        Log.Information($"Costume created: {modCostume.Character} || Item ID: {modCostume.ItemId} || Bind: {modCostume.GmdBindPath}");
        return modCostume;
    }

    public Costume? CreateFromExisting(Character character, string name, int modelId)
    {
        var bindPath = $@"MODEL\CHARACTER\{(int)character:D4}\C{(int)character:D4}_{modelId:D3}_00.GMD";
        return this.CreateFromExisting(character, name, bindPath);
    }

    private void AddGmdFile(Costume costume, string gmdFile, string modDir)
    {
        costume.GmdFilePath = gmdFile;
        costume.GmdBindPath = Path.GetRelativePath(modDir, gmdFile);
        this.criFsApi.AddBind(costume.GmdFilePath, costume.GmdBindPath, "Costume Framework");
    }

    private void AddDescription(Costume costume)
    {
        var descriptionFile = Path.ChangeExtension(costume.GmdFilePath, ".msg");
        if (File.Exists(descriptionFile))
        {
            if (compiler.TryCompile(File.ReadAllText(descriptionFile), out var messageScript))
            {
                using var ms = new MemoryStream();
                messageScript.ToStream(ms, true);
                costume.DescriptionMessageBinary = ms.ToArray();
            }
            else
            {
                Log.Warning($"Failed to compile costume description.\nFile: {descriptionFile}");
            }
        }
    }

    private void AddMusic(Costume costume)
    {
        var musicFile = Path.ChangeExtension(costume.GmdFilePath, ".pme");
        if (File.Exists(musicFile))
        {
            costume.MusicScriptFile = musicFile;
        }

        var battleThemeFile = Path.ChangeExtension(costume.GmdFilePath, ".theme.pme");
        if (File.Exists(battleThemeFile))
        {
            costume.BattleThemeFile = battleThemeFile;
        }
    }

    private void AddGoodbye(Costume costume, string modDir)
    {
        var goodbyeFile = Path.ChangeExtension(costume.GmdFilePath, ".bcd");
        if (File.Exists(goodbyeFile))
        {
            costume.GoodbyeBindPath = Path.GetRelativePath(modDir, goodbyeFile);
            this.criFsApi.AddBind(goodbyeFile, costume.GoodbyeBindPath, "Costume Framework");
        }
    }

    private Costume? GetAvailableModCostume(Character character)
        => this.costumes
        .FirstOrDefault(x =>
        x.Character == character
        && VirtualOutfitsSection.IsModOutfit(x.ItemId)
        && x.GmdFilePath == null);
}
