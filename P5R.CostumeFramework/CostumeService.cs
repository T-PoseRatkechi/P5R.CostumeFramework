using P5R.CostumeFramework.Hooks;
using P5R.CostumeFramework.Models;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Runtime.InteropServices;

namespace P5R.CostumeFramework;

internal unsafe class CostumeService
{
    private readonly IModLoader modLoader;
    private readonly IP5RLib p5rLib;
    private readonly CostumeManager costumes;

    [Function(CallingConventions.Microsoft)]
    private delegate void LoadCostumeGmdFunction(nint param1, Character character, nint gmdId, nint param4, nint param5);
    private IHook<LoadCostumeGmdFunction>? loadCostumeGmdHook;

    private readonly byte* gmdStringBuffer;
    private MultiAsmHook? redirectGmdHook;

    private readonly nint* gmdFileStrPtr;
    private nint tempGmdStrPtr;

    public CostumeService(IModLoader modLoader, IReloadedHooks hooks)
    {
        this.modLoader = modLoader;
        this.costumes = new(modLoader);

        IStartupScanner scanner;
        this.modLoader.GetController<IStartupScanner>().TryGetTarget(out scanner!);
        this.modLoader.GetController<IP5RLib>().TryGetTarget(out p5rLib!);

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
                Reloaded.Hooks.Definitions.Enums.AsmHookBehaviour.DoNotExecuteOriginal);

            var redirectCostumeGmd2 = hooks.CreateAsmHook(
                patch,
                result + 0xDF,
                Reloaded.Hooks.Definitions.Enums.AsmHookBehaviour.DoNotExecuteOriginal);

            this.redirectGmdHook = new(redirectCostumeGmd1, redirectCostumeGmd2);
            this.redirectGmdHook.Activate().Disable();
        });
    }

    private void LoadCostumeGmd(nint param1, Character character, nint gmdId, nint param4, nint param5)
    {
        var costumeEquipId = this.GetEquipmentId(character, EquipSlot.Costume);
        var costumeId = this.GetCostumeId(costumeEquipId);
        var costume = (Costume)costumeId;

        if (Enum.IsDefined(character))
        {
            Log.Debug($"GMD: {param1} || {character} || {gmdId} || {param4} || {param5}");
            Log.Debug($"{character} || Constume Item ID: {costumeEquipId} || Costume ID: {costumeId} || Costume: {costume}");
        }
        else
        {
            Log.Verbose($"GMD Info: {param1} || {character} || {gmdId} || {param4} || {param5}");
        }

        if (this.costumes.TryGetCostumeBind(character, costume, out var replacementBindPath))
        {
            this.tempGmdStrPtr = Marshal.StringToHGlobalAnsi(replacementBindPath);
            *this.gmdFileStrPtr = this.tempGmdStrPtr;

            Log.Debug($"{character}: redirected {costume} GMD to {replacementBindPath}");
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
