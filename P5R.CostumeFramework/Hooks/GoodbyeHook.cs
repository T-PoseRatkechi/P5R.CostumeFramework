using P5R.CostumeFramework.Costumes;
using P5R.CostumeFramework.Models;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace P5R.CostumeFramework.Hooks;

internal class GoodbyeHook
{
    [Function(Register.rbx, Register.rax, true)]
    private delegate nint GoodbyeHookFunction(int characterId);
    private IReverseWrapper<GoodbyeHookFunction>? goodbyeWrapper;
    private IAsmHook? goodbyeHook;

    private readonly IP5RLib p5rLib;
    private readonly CostumeRegistry costumes;

    private readonly Dictionary<int, nint> goodbyeCache = new();

    public GoodbyeHook(
        IStartupScanner scanner,
        IReloadedHooks hooks,
        IP5RLib p5rLib,
        CostumeRegistry costumes)
    {
        this.p5rLib = p5rLib;
        this.costumes = costumes;
        scanner.Scan("Goodbye BCD Hook", "E8 ?? ?? ?? ?? 80 BD ?? ?? ?? ?? 00 0F 84 ?? ?? ?? ?? 4C 8D 8D", result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.GoodbyeHookImpl, out this.goodbyeWrapper),
                Utilities.PopCallerRegisters,
                "cmp rax, 0",
                "jz original",
                "mov rdx, rax",
                "original:",
            };

            this.goodbyeHook = hooks.CreateAsmHook(patch, result).Activate();
        });
    }

    private nint GoodbyeHookImpl(int characterId)
    {
        var character =
            characterId == 61969 ? Character.Futaba
            : (Character)(characterId + 1);
        Log.Debug($"Getting Goodbye BMD for: {character}");

        var outfitItemId = this.p5rLib.FlowCaller.GET_EQUIP((int)character, (int)EquipSlot.Costume);
        if (this.costumes.TryGetCostume(outfitItemId, out var costume)
            && costume.GoodbyeBindPath != null)
        {
            if (this.goodbyeCache.TryGetValue(outfitItemId, out var cachedPtr))
            {
                return cachedPtr;
            }
            else
            {
                var ptr = Marshal.StringToHGlobalAnsi(costume.GoodbyeBindPath);
                this.goodbyeCache[outfitItemId] = ptr;
                return ptr;
            }
        }

        return IntPtr.Zero;
    }
}
