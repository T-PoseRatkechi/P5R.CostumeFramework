namespace P5R.CostumeFramework.Models;

internal class CharacterCostumes
{
    public CharacterCostumes(Character character)
    {
        this.Character = character;

        foreach (var costume in Enum.GetValues<Costume>())
        {
            if (costume == Costume.Default)
            {
                continue;
            }

            if (this.Character == Character.Mona && costume < Costume.Gekkoukan_High)
            {
                continue;
            }

            this.Costumes.Add(new(costume));
        }
    }

    public Character Character { get; }

    public List<ReplacementCostume> Costumes { get; } = new();

    public void AddReplacementCostume(string replacementFile, string replacementBindPath)
    {
        if (this.Costumes.FirstOrDefault(x => x.ReplacementBindPath == null) is ReplacementCostume availableCostume)
        {
            availableCostume.ReplacementFilePath = replacementFile;
            availableCostume.ReplacementBindPath = replacementBindPath;
            Log.Information($"{this.Character}: Replacing costume {availableCostume.OriginalCostume}. Bind: {replacementBindPath}");
        }
        else
        {
            Log.Warning($"{this.Character}: No remaining costumes to replace. Bind: {replacementBindPath}");
        }
    }
}

internal class ReplacementCostume
{
    public ReplacementCostume(Costume original)
    {
        this.OriginalCostume = original;
    }

    public Costume OriginalCostume { get; set; }

    public string? ReplacementFilePath { get; set; }

    public string? ReplacementBindPath { get; set; }
}