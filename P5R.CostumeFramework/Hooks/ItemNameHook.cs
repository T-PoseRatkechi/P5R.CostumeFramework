using P5R.CostumeFramework.Costumes;
using P5R.CostumeFramework.Models;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;

namespace P5R.CostumeFramework.Hooks;

internal unsafe class ItemNameHook : IGameHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate nint GetItemNameFunction(int itemId);
    private IHook<GetItemNameFunction>? getItemNameHook;

    private readonly CostumeRegistry costumes;

    private readonly nint fallbackNameStrPtr;

    public ItemNameHook(CostumeRegistry costumes)
    {
        this.costumes = costumes;
        this.fallbackNameStrPtr = StringsCache.GetStringPtr("UNUSED (Equipping will break game!)");
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan("Get Item Name Function", "B8 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC 4C 8B DC 48 83 EC 78", result =>
        {
            this.getItemNameHook = hooks.CreateHook<GetItemNameFunction>(this.GetItemName, result + 15).Activate();
        });
    }

    private nint GetItemName(int itemId)
    {
        if (VirtualOutfitsSection.IsOutfit(itemId))
        {
            if (this.costumes.TryGetCostume(itemId, out var costume))
            {
                if (costume.Config.Name != null)
                {
                    return StringsCache.GetStringPtr(costume.Config.Name);
                }

                if (costume.Name != null)
                {
                    return StringsCache.GetStringPtr(costume.Name);
                }
            }

            return this.fallbackNameStrPtr;
        }

        return this.getItemNameHook!.OriginalFunction(itemId);
    }
}
