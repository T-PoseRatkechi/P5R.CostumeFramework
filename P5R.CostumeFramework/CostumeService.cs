using BGME.Framework.Interfaces;
using P5R.CostumeFramework.Configuration;
using P5R.CostumeFramework.Hooks;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;

namespace P5R.CostumeFramework;

internal unsafe class CostumeService
{
    private readonly IModLoader modLoader;
    private readonly Config config;
    private readonly IBgmeApi bgme;
    private readonly IP5RLib p5rLib;
    private readonly CostumeManager costumes;

    private readonly CostumeGmdHook costumeGmdHook;
    private readonly VirtualOutfitsHook outfitsHook;
    private readonly ItemNameDescriptionHook nameDescriptionHook;
    private readonly ItemCountHook itemCountHook;

    public CostumeService(IModLoader modLoader, IReloadedHooks hooks, Config config)
    {
        this.modLoader = modLoader;
        this.config = config;
        this.costumes = new(modLoader);

        IStartupScanner scanner;
        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out scanner!);
        this.modLoader.GetController<IBgmeApi>().TryGetTarget(out this.bgme!);
        this.modLoader.GetController<IP5RLib>().TryGetTarget(out this.p5rLib!);

        this.costumeGmdHook = new(scanner, hooks, bgme, p5rLib, config, this.costumes);
        this.outfitsHook = new(scanner, hooks);
        this.nameDescriptionHook = new(scanner, hooks, this.costumes);
        this.itemCountHook = new(scanner, hooks, this.costumes);
    }
}
