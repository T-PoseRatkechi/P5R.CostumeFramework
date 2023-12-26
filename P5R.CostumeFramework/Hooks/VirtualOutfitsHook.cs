using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using P5R.CostumeFramework.Models;
using System.Runtime.InteropServices;

namespace P5R.CostumeFramework.Hooks;

internal unsafe class VirtualOutfitsHook
{
    private IAsmHook? virtualOutfitsHook;
    private readonly nint virtualOutfitsPtr;

    private IAsmHook? nextSectionHook;
    private readonly nint nextSectionPtr;

    public VirtualOutfitsHook(IStartupScanner scanner, IReloadedHooks hooks)
    {
        this.virtualOutfitsPtr = Marshal.AllocHGlobal(sizeof(VirtualOutfitsSection));
        Marshal.StructureToPtr(new VirtualOutfitsSection(), virtualOutfitsPtr, false);
        this.nextSectionPtr = Marshal.AllocHGlobal(sizeof(nint));

        scanner.Scan(
            "Use Virtual Outfit Section",
            "8B 37 48 83 C7 04 0F CE 48 63 EE 48 8B CD E8 ?? ?? ?? ?? 4C 8B F0 48 8B 05 ?? ?? ?? ?? 48 85 C0 74 ?? 48 8B D5 49 8B CE FF D0 48 8B CD 4C 89 35 ?? ?? ?? ?? 48 C1 E9 05",
            result =>
            {
                var patch = new string[]
                {
                    "use64",

                    // Calculate and save pointer to next item section.
                    "xor rax, rax",
                    "mov eax, [rdi]",
                    "bswap eax",
                    "lea rdi, [rdi + rax + 16]",        // Assumes outfit section always has 12 bytes of padding.
                                                        // which *should* be true, even if a mod adds new entries.
                    $"mov rax, {this.nextSectionPtr}",
                    "mov [rax], rdi",

                    // Set pointer to virtual outfit data.
                    $"mov rdi, {virtualOutfitsPtr}"
                };

                this.virtualOutfitsHook = hooks.CreateAsmHook(patch, result, AsmHookBehaviour.ExecuteFirst).Activate();

                // Fix pointer so it points to the next section
                // in the original item TBL.
                var nextSectionAddress = result + 0x82;
                var nextSectionPatch = new string[]
                {
                    "use64",
                    $"mov rdi, {this.nextSectionPtr}",
                    "mov rdi, [rdi]",
                };

                this.nextSectionHook = hooks.CreateAsmHook(nextSectionPatch, nextSectionAddress, AsmHookBehaviour.ExecuteFirst).Activate();
            });
    }
}
