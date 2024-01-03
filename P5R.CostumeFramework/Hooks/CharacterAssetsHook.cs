using P5R.CostumeFramework.Costumes;
using P5R.CostumeFramework.Models;
using p5rpc.lib.interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace P5R.CostumeFramework.Hooks;

internal class CharacterAssetsHook : IGameHook
{
    [Function(Register.r8, Register.rax, true)]
    private delegate nint RedirectCharAsset(Character character);

    private IReverseWrapper<RedirectCharAsset>? setGuiWrapper;
    private IAsmHook? setGuiHook;

    private IReverseWrapper<RedirectCharAsset>? setCutinWrapper;
    private IAsmHook? setCutinHook;

    private readonly IP5RLib p5rLib;
    private readonly CostumeRegistry costumes;

    public CharacterAssetsHook(IP5RLib p5rLib, CostumeRegistry costumes)
    {
        this.p5rLib = p5rLib;
        this.costumes = costumes;
    }

    public void Initialize(IStartupScanner scanner, IReloadedHooks hooks)
    {
        scanner.Scan(
            "Costume GUI Hook",
            "48 8D 8D ?? ?? ?? ?? E8 ?? ?? ?? ?? BA 10 00 00 00 8D 4A ?? E8 ?? ?? ?? ?? 48 8B D8 48 8B 05 ?? ?? ?? ?? 48 85 C0 74 ?? BA 50 00 00 00",
            result =>
            {
                var patch = AssembleRedirectPatch(hooks, character => this.RedirectCharAssetFile(character, AssetType.Gui), out this.setGuiWrapper);
                this.setGuiHook = hooks.CreateAsmHook(patch, result).Activate();
            });

        scanner.Scan(
            "Costume Cutin Hook",
            "E8 ?? ?? ?? ?? 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 89 47 ?? C7 07 01 00 00 00",
            result =>
            {
                var patch = AssembleRedirectPatch(hooks, character => this.RedirectCharAssetFile(character, AssetType.Cutin), out this.setCutinWrapper);
                this.setCutinHook = hooks.CreateAsmHook(patch, result).Activate();
            });
    }

    private static string[] AssembleRedirectPatch(IReloadedHooks hooks, RedirectCharAsset func, out IReverseWrapper<RedirectCharAsset> wrapper)
    {
        return new string[]
        {
            "use64",
            Utilities.PushCallerRegisters,
            hooks.Utilities.GetAbsoluteCallMnemonics(func, out wrapper),
            Utilities.PopCallerRegisters,
            "test rax, rax",
            "jz original",
            "mov rdx, rax",
            "original:",
        };
    }

    private nint RedirectCharAssetFile(Character character, AssetType type)
    {
        if (!Enum.IsDefined(character))
        {
            return IntPtr.Zero;
        }

        var currentOutfitId = this.p5rLib.GET_EQUIP(character, EquipSlot.Costume);
        string? redirectPath = null;

        if (this.costumes.TryGetCostume(currentOutfitId, out var costume))
        {
            switch (type)
            {
                case AssetType.Gui:
                    if (costume.GuiBindFile != null)
                    {
                        redirectPath = costume.GuiBindFile;
                    }
                    break;
                case AssetType.Cutin:
                    if (costume.CutinBindPath != null)
                    {
                        redirectPath = costume.CutinBindPath;
                    }
                    break;
                default:
                    Log.Error($"Unknown asset redirection value: {type}");
                    break;
            }
        }

        if (redirectPath != null)
        {
            Log.Debug($"Character asset redirected: {character} || {type} || {redirectPath}");
            return StringsCache.GetStringPtr(redirectPath);
        }

        return IntPtr.Zero;
    }

    private enum AssetType
    {
        Gui,
        Cutin,
    }
}
