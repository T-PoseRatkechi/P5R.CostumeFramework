using BGME.Framework.Interfaces;
using P5R.CostumeFramework.Configuration;
using P5R.CostumeFramework.Costumes;
using P5R.CostumeFramework.Models;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;

namespace P5R.CostumeFramework.Hooks;

internal unsafe class CostumeGmdHook
{
    [Function(CallingConventions.Microsoft)]
    private delegate void LoadCostumeGmdFunction(nint param1, Character character, nint gmdId, nint param4, nint param5);
    private IHook<LoadCostumeGmdFunction>? loadCostumeGmdHook;
    private MultiAsmHook? redirectGmdHook;

    private readonly nint* gmdFileStrPtr;
    private nint tempGmdStrPtr;

    private readonly IBgmeApi bgme;
    private readonly IP5RLib p5rLib;
    private readonly Config config;
    private readonly CostumeRegistry costumes;
    private string? currentMusicFile;

    public Action? CostumeChanged;

    public CostumeGmdHook(
        IStartupScanner scanner,
        IReloadedHooks hooks,
        IBgmeApi bgme,
        IP5RLib p5RLib,
        Config config,
        CostumeRegistry costumes)
    {
        this.bgme = bgme;
        this.p5rLib = p5RLib;
        this.config = config;
        this.costumes = costumes;

        this.gmdFileStrPtr = (nint*)Marshal.AllocHGlobal(sizeof(nint));
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
    }

    private void LoadCostumeGmd(nint param1, Character character, nint gmdId, nint param4, nint param5)
    {
        var outfitItemId = this.p5rLib.GET_EQUIP(character, EquipSlot.Costume);
        var outfitId = this.GetOutfitId(outfitItemId);
        var outfitSet = (CostumeSet)VirtualOutfitsSection.GetOutfitSetId(outfitItemId);

        if (Enum.IsDefined(character))
        {
            Log.Verbose($"GMD: {param1} || {character} || {gmdId} || {param4} || {param5}");
            Log.Debug($"{character} || Item ID: {outfitItemId} || Outfit ID: {outfitId} || Outfit Set: {outfitSet}");
        }
        else
        {
            Log.Verbose($"GMD: {param1} || {character} || {gmdId} || {param4} || {param5}");
        }

        if (this.costumes.TryGetModCostume(outfitItemId, out var costume))
        {
            this.tempGmdStrPtr = Marshal.StringToHGlobalAnsi(costume.GmdBindPath);
            *this.gmdFileStrPtr = this.tempGmdStrPtr;

            Log.Debug($"{character}: redirected {outfitSet} GMD to {costume.GmdBindPath}");
            this.redirectGmdHook?.Enable();
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

        this.CostumeChanged?.Invoke();
        this.loadCostumeGmdHook?.OriginalFunction(param1, character, gmdId, param4, param5);
    }

    private int GetOutfitId(int itemId) => itemId - 0x7000;

    private int GetCostumeModelId(int equipmentId)
        => VirtualOutfitsSection.GetOutfitSetId(equipmentId) + 150;
}
