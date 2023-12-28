using P5R.CostumeFramework.Costumes;
using P5R.CostumeFramework.Models;
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

    [Function(Register.rsi, Register.rax, true)]
    private delegate nint GetDescriptionDelegate(nint originalPtr);
    private IReverseWrapper<GetDescriptionDelegate>? getDescriptionWrapper;
    private IAsmHook getDescriptionHook;

    [Function(CallingConventions.Microsoft)]
    private delegate int InitializeBmdFunction(nint bmdPtr);
    private IFunction<InitializeBmdFunction> initializeBmd;
    private IHook<InitializeBmdFunction> initalizeBmdHook;

    private readonly CostumeRegistry costumes;
    private readonly Dictionary<string, nint> namesCache = new();
    private readonly Dictionary<int, nint> descriptionsCache = new();
    private int displayCostumeId;

    private readonly nint fallbackNameStrPtr;

    public ItemNameDescriptionHook(
        IStartupScanner scanner,
        IReloadedHooks hooks,
        CostumeRegistry costumes)
    {
        this.costumes = costumes;
        this.fallbackNameStrPtr = Marshal.StringToHGlobalAnsi("UNUSED (Equipping will break game!)");

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

        scanner.Scan("Overwrite MSG Pointer", "48 63 84 24 ?? ?? ?? ?? 85 C0 79 ?? 4C 89 F0", result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.GetDescriptionPointer, out this.getDescriptionWrapper),
                Utilities.PopCallerRegisters,
                "test rax, rax",
                "jz original",
                "mov rsi, rax",
                "original:"
            };

            this.setDescriptionHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });

        scanner.Scan("Initialize BMD Function", "48 83 EC 28 66 83 79 ?? 00", result =>
        {
            this.initalizeBmdHook = hooks.CreateHook<InitializeBmdFunction>(this.InitializeBmdImpl, result).Activate();
        });
    }

    private int InitializeBmdImpl(nint bmdPtr)
    {
        return this.initalizeBmdHook.OriginalFunction(bmdPtr);
    }

    private nint GetItemName(int itemId)
    {
        if (VirtualOutfitsSection.IsModOutfit(itemId))
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

            return this.fallbackNameStrPtr;
        }

        return this.getItemNameHook.OriginalFunction(itemId);
    }

    private nint GetDescriptionPointer(nint originalPtr)
    {
        if (this.displayCostumeId != -1)
        {
            //Log.Debug($"Overwriting MSG: {originalPtr:X}");
            if (this.descriptionsCache.TryGetValue(this.displayCostumeId, out var descriptionPtr))
            {
                return descriptionPtr;
            }
        }

        //Log.Debug($"BMD: {Marshal.PtrToStringAnsi(originalPtr)} || {originalPtr:X}");
        return IntPtr.Zero;
    }

    private int SetDescriptionItemId(int itemId)
    {
        // Catch and handle descriptions for any mod outfit.
        if (VirtualOutfitsSection.IsModOutfit(itemId))
        {
            this.displayCostumeId = itemId;

            // Initialize and cache the costume description if exists.
            if (this.costumes.TryGetModCostume(itemId, out var costume)
                && costume.DescriptionMessageBinary != null)
            {
                if (!this.descriptionsCache.ContainsKey(itemId))
                {
                    var ptr = Marshal.AllocHGlobal(costume.DescriptionMessageBinary.Length);
                    Marshal.Copy(costume.DescriptionMessageBinary, 0, ptr, costume.DescriptionMessageBinary.Length);
                    this.InitializeBmdImpl(ptr);
                    this.descriptionsCache[itemId] = ptr;
                }
            }

            //Log.Debug("Defaulting to Item ID 0x7000 for for costume description.");
            return 0x7000;
        }

        this.displayCostumeId = -1;
        return itemId;
    }
}
