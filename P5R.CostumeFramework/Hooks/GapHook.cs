using P5R.CostumeFramework.Costumes;
using P5R.CostumeFramework.Models;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace P5R.CostumeFramework.Hooks;

internal class GapHook
{
    [Function(Register.r8, Register.rax, true)]
    private delegate int GapGetOutfitItemId(ushort currentOutfitItemId);
    private IReverseWrapper<GapGetOutfitItemId>? getOutfitItemIdWrapper;
    private IAsmHook? getOutfitItemIdHook;

    private readonly CostumeRegistry costumes;
    private readonly Dictionary<Character, FakeOutfitItemId> previousOutfitIds = new();

    public GapHook(
        IStartupScanner scanner,
        IReloadedHooks hooks,
        CostumeRegistry costumes)
    {
        this.costumes = costumes;
        scanner.Scan("GAP Get Outfit Item ID Hook", "B8 67 66 66 66 41 8D 90", result =>
        {
            var patch = new string[]
            {
                "use64",
                Utilities.PushCallerRegisters,
                hooks.Utilities.GetAbsoluteCallMnemonics(this.GetOutfitItemIdImpl, out this.getOutfitItemIdWrapper),
                Utilities.PopCallerRegisters,
                "test rax, rax",
                "jz original",
                "mov r8, rax",
                "original:"
            };

            this.getOutfitItemIdHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteFirst).Activate();
        });
    }

    /// <summary>
    /// Fix issues caused by mod outfits using item IDs too large which
    /// break some math somewhere, likely in determining/formatting the GAP file path.
    /// </summary>
    private int GetOutfitItemIdImpl(ushort currentOutfitItemId)
    {
        if (this.costumes.TryGetModCostume(currentOutfitItemId, out var costume))
        {
            if (this.previousOutfitIds.TryGetValue(costume.Character, out var fakeOutfitId))
            {
                if (fakeOutfitId.OriginalId == costume.ItemId)
                {
                    Log.Debug($"GAP Get Outfit Item ID overwritten (previous): {costume.Character} || Original: {fakeOutfitId.OriginalId} || New: {fakeOutfitId.NewId}");
                    return fakeOutfitId.NewId;
                }
            }

            var setId = VirtualOutfitsSection.GetOutfitSetId(costume.ItemId);
            var newSetId = GetNewSetId(setId);
            var prevSetId = fakeOutfitId != null ? VirtualOutfitsSection.GetOutfitSetId(fakeOutfitId.NewId) : -1;

            // Increment new set ID if same as previous causing same
            // item ID to be calculated, causing the outfit to not update.
            if (newSetId == prevSetId)
            {
                newSetId = GetNewSetId(newSetId + 1);
            }

            var equipId = (int)costume.Character - 1;
            var newOutfitItemId = 0x7010 + newSetId * 10 + equipId;
            this.previousOutfitIds[costume.Character] = new(costume.ItemId, newOutfitItemId);

            Log.Debug($"GAP Get Outfit Item ID overwritten: {costume.Character} || Equip ID: {equipId} || Original: {currentOutfitItemId} || New: {newOutfitItemId}");
            Log.Debug($"Original Set ID: {setId} || New Set ID: {newSetId}");
            return newOutfitItemId;
        }

        return 0;
    }

    private static int GetNewSetId(int currentSetId)
        => (currentSetId % 4) + VirtualOutfitsSection.GAME_OUTFIT_SETS + 1;

    private record FakeOutfitItemId(int OriginalId, int NewId);
}
