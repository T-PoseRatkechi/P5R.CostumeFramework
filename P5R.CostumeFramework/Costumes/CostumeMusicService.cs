using BGME.Framework.Interfaces;
using P5R.CostumeFramework.Models;
using p5rpc.lib.interfaces;

namespace P5R.CostumeFramework.Costumes;

internal class CostumeMusicService
{
    private readonly IBgmeApi bgme;
    private readonly IP5RLib p5rLib;
    private readonly CostumeRegistry costumes;

    private readonly Dictionary<Character, string?> costumeMusicFiles = new();

    public CostumeMusicService(IBgmeApi bgme, IP5RLib p5rLib, CostumeRegistry costumes)
    {
        this.bgme = bgme;
        this.p5rLib = p5rLib;
        this.costumes = costumes;

        foreach (var character in Enum.GetValues<Character>())
        {
            this.costumeMusicFiles[character] = null;
        }
    }

    public void Refresh(int outfitItemId)
    {
        var costume = this.costumes.GetCostumeById(outfitItemId);
        if (costume == null)
        {
            return;
        }

        var character = costume.Character;
        var currentMusicFile = this.costumeMusicFiles[character];
        var newMusicFile = costume?.MusicScriptFile;

        // Costume music has changed.
        if (currentMusicFile != newMusicFile)
        {
            // Remove previous music, if any.
            if (currentMusicFile != null)
            {
                this.bgme.RemovePath(currentMusicFile);
                Log.Debug($"Costume music removed: {character}");
            }

            // Add new costume music, if any.
            if (costume?.MusicScriptFile != null)
            {
                this.bgme.AddPath(costume.MusicScriptFile);
                Log.Debug($"Costume music added: {character} || {costume.GmdBindPath}");
            }
        }

        this.costumeMusicFiles[character] = costume?.MusicScriptFile;
    }

    public void Refresh()
    {
        foreach (var character in Enum.GetValues<Character>())
        {
            var outfitItemId = this.p5rLib.GET_EQUIP(character, EquipSlot.Costume);
            this.Refresh(outfitItemId);
        }
    }
}
