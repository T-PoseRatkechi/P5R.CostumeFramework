using P5R.CostumeFramework.Models;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using System.Text;

namespace P5R.CostumeFramework;

internal unsafe class CostumeService
{
    [Function(CallingConventions.Microsoft)]
    private delegate void LoadCostumeGmdFunction(nint param1, Character character, nint gmdId, nint param4, nint param5);
    private IHook<LoadCostumeGmdFunction>? loadCostumeGmdHook;

    private readonly byte* gmdStringBuffer;
    private IAsmHook? redirectCostumeGmd;

    public CostumeService(IReloadedHooks hooks, IStartupScanner scanner)
    {
        this.gmdStringBuffer = (byte*)NativeMemory.AllocZeroed(64);
        var staticGmdString = "costumes/ren.gmd\0";
        var asciiBytes = Encoding.ASCII.GetBytes(staticGmdString);
        var handle = GCHandle.Alloc(asciiBytes, GCHandleType.Pinned);
        NativeMemory.Copy((void*)handle.AddrOfPinnedObject(), this.gmdStringBuffer, (nuint)staticGmdString.Length);
        handle.Free();

        scanner.Scan("Load Costume GMD Function", "48 83 EC 38 8B 44 24 ?? 44 8B D2", result =>
        {
            this.loadCostumeGmdHook = hooks.CreateHook<LoadCostumeGmdFunction>(this.LoadCostumeGmd, result).Activate();

            var patch = new string[]
            {
                "use64",
                $"mov rdx, {(nint)this.gmdStringBuffer}"
            };

            this.redirectCostumeGmd = hooks.CreateAsmHook(
                patch,
                result + 0x4A,
                Reloaded.Hooks.Definitions.Enums.AsmHookBehaviour.DoNotExecuteOriginal)
            .Activate();

           this.redirectCostumeGmd.Disable();
        });
    }

    private void LoadCostumeGmd(nint param1, Character character, nint gmdId, nint param4, nint param5)
    {
        Log.Verbose($"GMD Info: {param1} || {character} || {gmdId} || {param4} || {param5}");
        if (character == Character.Ren && param5 == 1 && gmdId == 51)
        {
            this.redirectCostumeGmd?.Enable();
        }
        else
        {
            this.redirectCostumeGmd?.Disable();
        }

        this.loadCostumeGmdHook?.OriginalFunction(param1, character, gmdId, param4, param5);
    }
}
