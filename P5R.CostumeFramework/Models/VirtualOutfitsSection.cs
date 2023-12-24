using System.Runtime.InteropServices;

namespace P5R.CostumeFramework.Models;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VirtualOutfitsSection
{
    private const int MAX_OUTFITS = 286;

    public VirtualOutfitsSection()
    {
        var outfits = new OutfitEntry[MAX_OUTFITS];
        var size = sizeof(OutfitEntry) * outfits.Length;
        this.size = ((uint)size).ToBigEndian();

        fixed (byte* ptr = outfitsBuffer)
        {
            for (int i = 0; i < outfits.Length; i++)
            {
                var outfit = outfits[i];

                outfit.icon = 4;
                outfit.unknown9 = 100;
                outfit.unknown11 = 20;
                outfit.unknown12 = 799;
                outfit.equippableFlags = EquippableUsers.Joker;

                Marshal.StructureToPtr(
                    outfit,
                    (nint)(ptr + (sizeof(OutfitEntry) * i)),
                    false);
            }
        }
    }

    public uint size;
    public fixed byte outfitsBuffer[MAX_OUTFITS * 32];
}
