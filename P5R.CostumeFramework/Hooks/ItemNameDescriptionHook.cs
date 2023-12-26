using P5R.CostumeFramework.Costumes;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace P5R.CostumeFramework.Hooks;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
internal unsafe class ItemNameDescriptionHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate nint GetItemNameFunction(int itemId);
    private IHook<GetItemNameFunction>? getItemNameHook;

    [Function(Register.rax, Register.rax, true)]
    private delegate int SetDescriptionItemIdFunction(int itemId);
    private IReverseWrapper<SetDescriptionItemIdFunction>? setDescriptionWrapper;
    private IAsmHook setDescriptionHook;

    [Function(Register.rax, Register.rax, true)]
    private delegate nint GetDescriptionDelegate(nint originalPtr);
    private IReverseWrapper<GetDescriptionDelegate>? getDescriptionWrapper;
    private IAsmHook getDescriptionHook;

    private readonly CostumeRegistry costumes;
    private readonly Dictionary<string, nint> namesCache = new();
    private readonly Dictionary<int, nint> descriptionsCache = new();
    private int displayCostumeId;

    public ItemNameDescriptionHook(
        IStartupScanner scanner,
        IReloadedHooks hooks,
        CostumeRegistry costumes)
    {
        this.costumes = costumes;

        scanner.Scan("Get Item Name Function", "B8 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC 4C 8B DC 48 83 EC 78", result =>
        {
            this.getItemNameHook = hooks.CreateHook<GetItemNameFunction>(this.GetItemName, result + 15).Activate();
        });

        scanner.Scan("Get Item ID for Description Hook", "8B 85 ?? ?? ?? ?? 41 0F 28 CB F3 0F 58 0D", result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.SetDescriptionItemId, out this.setDescriptionWrapper),
                Utilities.PopCallerRegisters,
            };

            this.setDescriptionHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteAfter).Activate();
        });

        scanner.Scan("Get Item Description Pointer", "0F B6 3C ?? 85 FF", result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.GetDescriptionPointer, out this.getDescriptionWrapper),
                Utilities.PopCallerRegisters,
            };

            this.setDescriptionHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });
    }

    private nint GetItemName(int itemId)
    {
        if (this.costumes.TryGetModCostume(itemId, out var costume))
        {
            if (this.namesCache.TryGetValue(costume.Name, out var strPtr))
            {
                return strPtr;
            }
            else
            {
                this.namesCache[costume.Name] = Marshal.StringToHGlobalAnsi(costume.Name);
                return this.namesCache[costume.Name];
            }
        }

        return this.getItemNameHook.OriginalFunction(itemId);
    }

    private nint GetDescriptionPointer(nint originalPtr)
    {
        //if (this.displayCostumeId != -1)
        //{
        //    if (this.costumes.GetCostumeDescription(this.displayCostumeId) is string description)
        //    {
        //        if (this.descriptionsCache.TryGetValue(this.displayCostumeId, out var strPtr))
        //        {
        //            return strPtr;
        //        }
        //        else
        //        {
        //            this.descriptionsCache[displayCostumeId] = Marshal.StringToHGlobalAnsi(description);
        //            return this.descriptionsCache[displayCostumeId];
        //        }
        //    }
        //}

        return originalPtr;
    }

    private int SetDescriptionItemId(int itemId)
    {
        //if (this.costumes.IsCostumeItemId(itemId))
        //{
        //    //Log.Debug("Defaulting to Item ID 0x7000 for for costume description.");
        //    this.displayCostumeId = itemId;
        //    return 0x7000;
        //}

        //this.displayCostumeId = -1;
        return itemId;
    }
}
