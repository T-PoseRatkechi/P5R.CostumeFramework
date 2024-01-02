using BGME.BattleThemes.Interfaces;
using BGME.Framework.Interfaces;
using P5R.CostumeFramework.Configuration;
using P5R.CostumeFramework.Costumes;
using P5R.CostumeFramework.Hooks;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace P5R.CostumeFramework;

internal unsafe class CostumeService
{
    private readonly IModLoader modLoader;
    private readonly IBgmeApi bgme;
    private readonly IP5RLib p5rLib;
    private readonly IBattleThemesApi battleThemes;

    private readonly CostumeGmdHook costumeGmdHook;
    private readonly VirtualOutfitsHook outfitsHook;
    private readonly ItemNameDescriptionHook nameDescriptionHook;
    private readonly ItemCountHook itemCountHook;
    private readonly GoodbyeHook goodbyeHook;
    private readonly EquippedItemHook equippedItemHook;
    private readonly EmtGapHook emtGapHook;
    private readonly FieldChangeHook fieldChangeHook;
    private readonly CostumeMusicService costumeMusic;

    private readonly List<IGameHook> gameHooks = new();

    public CostumeService(IModLoader modLoader, IReloadedHooks hooks, Config config)
    {
        this.modLoader = modLoader;

        IStartupScanner scanner;
        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out scanner!);
        this.modLoader.GetController<IBgmeApi>().TryGetTarget(out this.bgme!);
        this.modLoader.GetController<IP5RLib>().TryGetTarget(out this.p5rLib!);
        this.modLoader.GetController<IBattleThemesApi>().TryGetTarget(out this.battleThemes!);

        var costumes = new CostumeRegistry(modLoader);
        this.gameHooks.Add(new CostumeTexturesHook(p5rLib, costumes));
        foreach (var hook in this.gameHooks)
        {
            hook.Initialize(scanner, hooks);
        }

        this.outfitsHook = new(scanner, hooks);
        this.nameDescriptionHook = new(scanner, hooks, costumes);
        this.itemCountHook = new(scanner, hooks, config, costumes);
        this.goodbyeHook = new(scanner, hooks, p5rLib, costumes);
        this.emtGapHook = new(scanner, hooks, p5rLib);
        this.costumeMusic = new(bgme, battleThemes, p5rLib, costumes);
        this.fieldChangeHook = new(scanner, hooks, p5rLib, config, costumes, this.costumeMusic);
        this.equippedItemHook = new(scanner, hooks, p5rLib, costumes, this.costumeMusic);
        this.costumeGmdHook = new(scanner, hooks, bgme, p5rLib, config, costumes, equippedItemHook);
    }
}
