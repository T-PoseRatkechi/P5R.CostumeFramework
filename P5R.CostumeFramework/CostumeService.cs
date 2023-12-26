using BGME.Framework.Interfaces;
using P5R.CostumeFramework.Configuration;
using P5R.CostumeFramework.Hooks;
using P5R.CostumeFramework.Models;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace P5R.CostumeFramework;

internal unsafe class CostumeService
{
    private readonly IModLoader modLoader;
    private readonly Config config;
    private readonly IBgmeApi bgme;
    private readonly IP5RLib p5rLib;
    private readonly CostumeManager costumes;

    [Function(CallingConventions.Microsoft)]
    private delegate void LoadCostumeGmdFunction(nint param1, Character character, nint gmdId, nint param4, nint param5);
    private IHook<LoadCostumeGmdFunction>? loadCostumeGmdHook;
    private MultiAsmHook? redirectGmdHook;

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

    private readonly nint* gmdFileStrPtr;
    private nint tempGmdStrPtr;

    private string? currentMusicFile;

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

        this.outfitsHook = new(scanner, hooks);
        this.nameDescriptionHook = new(scanner, hooks, this.costumes);

        gmdFileStrPtr = (nint*)Marshal.AllocHGlobal(sizeof(nint));
        scanner.Scan("Load Costume GMD Function", "48 83 EC 38 8B 44 24 ?? 44 8B D2", result =>
        {
            this.loadCostumeGmdHook = hooks.CreateHook<LoadCostumeGmdFunction>(this.LoadCostumeGmd, result).Activate();

            var patch = new string[]
            {
                "use64",
                $"mov rdx, {(nint)this.gmdFileStrPtr}",
                "mov rdx, [rdx]"
            };

            var redirectCostumeGmd1 = hooks.CreateAsmHook(
                patch,
                result + 0x4A,
                AsmHookBehaviour.DoNotExecuteOriginal);

            var redirectCostumeGmd2 = hooks.CreateAsmHook(
                patch,
                result + 0xDF,
                AsmHookBehaviour.DoNotExecuteOriginal);

            this.redirectGmdHook = new(redirectCostumeGmd1, redirectCostumeGmd2);
            this.redirectGmdHook.Activate().Disable();
        });

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

    private void LoadCostumeGmd(nint param1, Character character, nint gmdId, nint param4, nint param5)
    {
        var costumeEquipId = this.GetEquipmentId(character, EquipSlot.Costume);
        var costumeId = this.GetCostumeId(costumeEquipId);
        var costume = (Costume)costumeId;

        if (Enum.IsDefined(character))
        {
            Log.Verbose($"GMD: {param1} || {character} || {gmdId} || {param4} || {param5}");
            Log.Debug($"{character} || Constume Item ID: {costumeEquipId} || Costume ID: {costumeId} || Costume: {costume}");
        }
        else
        {
            Log.Verbose($"GMD: {param1} || {character} || {gmdId} || {param4} || {param5}");
        }

        if (this.costumes.TryGetReplacementCostume(character, costume, out var replacementCostume))
        {
            if (this.config.RandomizeCostumes)
            {
                var costumes = this.costumes.GetAvailableCostumes(character);
                var randomIndex = Random.Shared.Next(0, costumes.Length);
                replacementCostume = costumes[randomIndex];
                Log.Information($"Randomized costume for: {character}");
            }

            this.tempGmdStrPtr = Marshal.StringToHGlobalAnsi(replacementCostume!.ReplacementBindPath);
            *this.gmdFileStrPtr = this.tempGmdStrPtr;

            Log.Debug($"{character}: redirected {costume} GMD to {replacementCostume.ReplacementBindPath}");
            this.redirectGmdHook?.Enable();

            if (character == Character.Joker)
            {
                if (this.currentMusicFile != null)
                {
                    this.bgme.RemovePath(this.currentMusicFile);
                    this.currentMusicFile = null;
                }

                var costumeMusicFile = $"{replacementCostume.ReplacementFilePath}.pme";
                if (File.Exists(costumeMusicFile))
                {
                    this.currentMusicFile = costumeMusicFile;
                    this.bgme.AddPath(this.currentMusicFile);
                    Log.Information("Added music script for costume.");
                }
            }
        }
        else
        {
            this.redirectGmdHook?.Disable();
            if (this.tempGmdStrPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.tempGmdStrPtr);
            }

            this.tempGmdStrPtr = 0;

            if (character == Character.Joker && this.currentMusicFile != null)
            {
                this.bgme.RemovePath(this.currentMusicFile);
                this.currentMusicFile = null;
            }
        }

        this.loadCostumeGmdHook?.OriginalFunction(param1, character, gmdId, param4, param5);
    }

    private int GetEquipmentId(Character character, EquipSlot equipSlot)
    {
        return this.p5rLib.FlowCaller.GET_EQUIP((int)character, (int)equipSlot);
    }

    private int GetCostumeId(int equipmentId)
        => (equipmentId - 0x7010) / 10;

    private int GetCostumeModelId(int equipmentId)
        => this.GetCostumeId(equipmentId) + 150;
}
