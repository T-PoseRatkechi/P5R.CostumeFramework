using BGME.Framework.Interfaces;
using P5R.CostumeFramework.Configuration;
using P5R.CostumeFramework.Hooks;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace P5R.CostumeFramework;

internal unsafe class CostumeService
{
    private readonly IModLoader modLoader;
    private readonly Config config;
    private readonly IBgmeApi bgme;
    private readonly IP5RLib p5rLib;
    private readonly CostumeManager costumes;

    [Function(new[] { Register.rbx, Register.rax }, Register.rax, true)]
    private delegate int GetItemCountFunction(int itemId, int itemCount);
    private IReverseWrapper<GetItemCountFunction>? itemCountWrapper;
    private IAsmHook? itemCountHook;

    /// <summary>
    /// Sets an item's count.
    /// </summary>
    /// <param name="itemId">Item ID.</param>
    /// <param name="itemCount">Item count.</param>
    /// <param name="param3">Related to whether an item is labeled "New" when swapping costumes.</param>
    [Function(CallingConventions.Microsoft)]
    private delegate void SetItemCountFunction(int itemId, int itemCount, nint param3);
    private IHook<SetItemCountFunction>? setItemCountHook;

    private readonly CostumeGmdHook costumeGmdHook;
    private readonly VirtualOutfitsHook outfitsHook;
    private readonly ItemNameDescriptionHook nameDescriptionHook;

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

        scanner.Scan("Get Item Count Hook", "84 C0 0F 84 ?? ?? ?? ?? 0F B7 FB C1 EF 0C", result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.GetItemCount, out this.itemCountWrapper),
                Utilities.PopCallerRegisters,
            };

            this.itemCountHook = hooks.CreateAsmHook(patch, result).Activate();
        });

        scanner.Scan("Set Item Count Function", "4C 8B DC 49 89 5B ?? 57 48 83 EC 70 48 8D 05", result =>
        {
            this.setItemCountHook = hooks.CreateHook<SetItemCountFunction>(this.SetItemCount, result).Activate();
        });
    }

    private void SetItemCount(int itemId, int itemCount, nint param3)
    {
        if (this.costumes.IsCostumeItemId(itemId))
        {
            Log.Debug("Ignoring SetItemCount for Custom Costume.");
        }
        else
        {
            Log.Debug($"SetItemCount || Item ID: {itemId} || Count: {itemCount} || param3: {param3}");
            this.setItemCountHook.OriginalFunction(itemId, itemCount, param3);
        }
    }

    private int GetItemCount(int itemId, int itemCount)
    {
        Log.Debug($"GetItemCount || Item ID: {itemId} || Count: {itemCount}");
        if (this.costumes.IsCostumeItemId(itemId))
        {
            Log.Debug($"Overwriting costume count with 1.");
            return 1;
        }

        return itemCount;
    }
}
