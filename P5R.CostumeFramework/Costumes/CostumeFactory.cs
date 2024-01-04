using CriFs.V2.Hook.Interfaces;
using P5R.CostumeFramework.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace P5R.CostumeFramework.Costumes;

internal class CostumeFactory
{
    private readonly ICriFsRedirectorApi criFsApi;
    private readonly GameCostumes costumes;

    private readonly IDeserializer deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public CostumeFactory(
        ICriFsRedirectorApi criFsApi,
        GameCostumes costumes)
    {
        this.criFsApi = criFsApi;
        this.costumes = costumes;
    }

    public Costume? Create(string modId, string modDir, Character character, string gmdFile)
    {
        var costume = this.GetAvailableModCostume(character);
        if (costume == null)
        {
            Log.Warning($"No available costume slots for: {character}");
            return null;
        }

        costume.Name = Path.GetFileNameWithoutExtension(gmdFile);
        costume.IsEnabled = true;
        this.AddGmdFile(costume, gmdFile, modDir);
        this.AddCostumeFiles(costume, modDir, modId);

        Log.Information($"Costume created: {costume.Character} || Item ID: {costume.ItemId} || Bind: {costume.GmdBindPath}");
        return costume;
    }

    public Costume? CreateFromExisting(Character character, string name, string bindPath)
    {
        var costume = this.GetAvailableModCostume(character);
        if (costume == null)
        {
            Log.Warning($"No available costume slots for: {character}");
            return null;
        }

        costume.Name = name;
        costume.IsEnabled = true;
        costume.GmdBindPath = bindPath;
        //this.AddCostumeFiles(costume, modDir);
        Log.Information($"Costume created: {costume.Character} || Item ID: {costume.ItemId} || Bind: {costume.GmdBindPath}");
        return costume;
    }

    public Costume? CreateFromExisting(Character character, string name, int modelId)
    {
        var bindPath = $@"MODEL\CHARACTER\{(int)character:D4}\C{(int)character:D4}_{modelId:D3}_00.GMD";
        return this.CreateFromExisting(character, name, bindPath);
    }

    public void AddCostumeFiles(Costume costume, string modDir, string modId)
    {
        this.LoadConfig(costume, modDir);
        this.AddGmdFile(costume, modDir);
        this.AddDescription(costume, modDir);
        this.AddMusic(costume, modDir, modId);
        this.AddCostumeCharAssets(costume, modDir);
        this.AddCutin(costume, modDir);
        this.AddGui(costume, modDir);
        this.AddWeapons(costume, modDir);
    }

    private void AddGmdFile(Costume costume, string gmdFile, string modDir)
    {
        costume.GmdFilePath = gmdFile;
        costume.GmdBindPath = Path.GetRelativePath(modDir, gmdFile);
        costume.IsEnabled = true;
        this.criFsApi.AddBind(costume.GmdFilePath, costume.GmdBindPath, "Costume Framework");
    }

    /// <summary>
    /// Adds a costume GMD from the costume folder.
    /// Mostly useful for overriding and enabling existing costumes.
    /// </summary>
    private void AddGmdFile(Costume costume, string modDir)
    {
        var costumeFile = Path.Join(this.GetCostumeDir(costume, modDir), "costume.gmd");
        if (File.Exists(costumeFile))
        {
            this.AddGmdFile(costume, costumeFile, modDir);
        }
    }

    private void LoadConfig(Costume costume, string modDir)
    {
        var configFile = Path.Join(this.GetCostumeDir(costume, modDir), "config.yaml");
        if (File.Exists(configFile))
        {
            try
            {
                var config = this.deserializer.Deserialize<CostumeConfig>(File.ReadAllText(configFile)) ?? new();
                costume.Config = config;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load costume config for: {costume.Name}\nFile: {configFile}");
            }
        }
    }

    private void AddDescription(Costume costume, string modDir)
    {
        var descriptionFile = Path.Join(this.GetCostumeDir(costume, modDir), "description.msg");
        if (File.Exists(descriptionFile))
        {
            costume.DescriptionMsg = File.ReadAllText(descriptionFile);
        }
    }

    private void AddMusic(Costume costume, string modDir, string modId)
    {
        var musicFile = Path.Join(this.GetCostumeDir(costume, modDir), "music.pme");
        if (File.Exists(musicFile))
        {
            costume.MusicScriptFile = musicFile;
        }

        var battleThemeFile = Path.Join(this.GetCostumeDir(costume, modDir), "battle.theme.pme");
        if (File.Exists(battleThemeFile))
        {
            costume.OwnerModId = modId;
            costume.BattleThemeFile = battleThemeFile;
        }
    }

    private void AddCostumeCharAssets(Costume costume, string modDir)
    {
        var costumeDir = this.GetCostumeDir(costume, modDir);
        var baseBindPath = Path.Join(costume.Character.ToString(), costume.Name);

        // Bind costume files in the bind dir.
        var costumeBindDir = Path.Join(costumeDir, "bind");
        if (Directory.Exists(costumeBindDir))
        {
            foreach (var file in Directory.EnumerateFiles(costumeBindDir, "*", SearchOption.AllDirectories))
            {
                this.criFsApi.AddBind(file, Path.GetRelativePath(costumeBindDir, file), "Costume Framework");
            }
        }

        var goodbyeFile = Path.Join(costumeDir, "aoa_goodbye.bcd");
        if (File.Exists(goodbyeFile))
        {
            costume.GoodbyeBindPath = Path.Join(baseBindPath, Path.GetFileName(goodbyeFile));
            this.criFsApi.AddBind(goodbyeFile, costume.GoodbyeBindPath, "Costume Framework");
        }

        var futabaSkillFile = Path.Join(costumeDir, "futaba_skill.bcd");
        if (File.Exists(futabaSkillFile))
        {
            costume.FutabaSkillBind = Path.Join(baseBindPath, Path.GetFileName(futabaSkillFile));
            this.criFsApi.AddBind(futabaSkillFile, costume.FutabaSkillBind, "Costume Framework");
        }
    }

    private void AddCutin(Costume costume, string modDir)
    {
        var cutinFile = Path.Join(this.GetCostumeDir(costume, modDir), "battle_cutin.dds");
        if (File.Exists(cutinFile))
        {
            costume.CutinBindPath = Path.GetRelativePath(modDir, cutinFile);
            this.criFsApi.AddBind(cutinFile, costume.CutinBindPath, "Costume Framework");
        }
    }

    private void AddGui(Costume costume, string modDir)
    {
        var guiFile = Path.Join(this.GetCostumeDir(costume, modDir), "aoa_portrait.dds");
        if (File.Exists(guiFile))
        {
            costume.GuiBindFile = Path.GetRelativePath(modDir, guiFile);
            this.criFsApi.AddBind(guiFile, costume.GuiBindFile, "Costume Framework");
        }
    }

    private void AddWeapons(Costume costume, string modDir)
    {
        var weaponFile = Path.Join(this.GetCostumeDir(costume, modDir), "melee_weapon.gmd");
        if (File.Exists(weaponFile))
        {
            costume.WeaponBindPath = Path.GetRelativePath(modDir, weaponFile);
            this.criFsApi.AddBind(weaponFile, costume.WeaponBindPath, "Costume Framework");
        }
    }

    private string GetCostumeDir(Costume costume, string modDir)
        => Path.Join(modDir, "costumes", costume.Character.ToString(), costume.Name);

    private Costume? GetAvailableModCostume(Character character)
        => this.costumes
        .FirstOrDefault(x =>
        x.Character == character
        && VirtualOutfitsSection.IsModOutfit(x.ItemId)
        && x.GmdBindPath == null);
}
