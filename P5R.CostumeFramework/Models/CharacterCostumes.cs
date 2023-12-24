namespace P5R.CostumeFramework.Models;

internal class CharacterCostumes
{
    private readonly List<Character> costumeSets;

    public CharacterCostumes(Character character, List<Character> costumeSets)
    {
        this.Character = character;
        this.costumeSets = costumeSets;
    }

    public Character Character { get; }

    public List<ReplacementCostume> Costumes { get; } = new();

    public int AddCostume(string replacementFile, string replacementBindPath)
    {
        // Find existing set where character has an open slot.
        var setId = this.costumeSets.FindIndex(x => !x.HasFlag(this.Character));
        if (setId == -1)
        {
            this.costumeSets.Add(0);
            setId = this.costumeSets.Count - 1;
        }

        var name = Path.GetFileNameWithoutExtension(replacementFile);
        var itemId = 28957 + (setId * 10) + (int)this.Character;

        var descriptionFile = Path.ChangeExtension(replacementFile, ".txt");
        var description = File.Exists(descriptionFile) ? File.ReadAllText(descriptionFile) : null;

        this.Costumes.Add(new(name, itemId)
        {
            CostumeId = (Costume)(27 + setId),
            ReplacementFilePath = replacementFile,
            ReplacementBindPath = replacementBindPath,
            Description = description,
        });

        this.costumeSets[setId] |= this.Character;
        Log.Information($"{this.Character}: Added costume to Set {setId} with Item ID: {itemId} and Outfit ID: {285 + (setId * 10) + (int)this.Character}. Bind: {replacementBindPath}");
        return itemId;
    }

    public void AddReplacementCostume(string replacementFile, string replacementBindPath)
    {
        if (this.Costumes.FirstOrDefault(x => x.ReplacementBindPath == null) is ReplacementCostume availableCostume)
        {
            availableCostume.ReplacementFilePath = replacementFile;
            availableCostume.ReplacementBindPath = replacementBindPath;
            Log.Information($"{this.Character}: Replacing costume {availableCostume.CostumeId}. Bind: {replacementBindPath}");
        }
        else
        {
            Log.Warning($"{this.Character}: No remaining costumes to replace. Bind: {replacementBindPath}");
        }
    }
}

internal class ReplacementCostume
{
    public ReplacementCostume(string name, Costume original)
    {
        this.Name = name;
        this.CostumeId = original;
    }

    public ReplacementCostume(string name, int itemId)
    {
        this.Name = name;
        this.ItemId = itemId;
    }

    public string Name { get; set; }

    public int ItemId { get; set; }

    public Costume CostumeId { get; set; }

    public string? ReplacementFilePath { get; set; }

    public string? ReplacementBindPath { get; set; }

    public string? MusicScriptFile { get; set; }

    public string? Description { get; set; }
}
