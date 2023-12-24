using System.Runtime.InteropServices;

namespace P5R.CostumeFramework.Models;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VirtualOutfitsSection
{
    private const int NUM_OUTFIT_SETS = 32;
    private const int NUM_OUTFITS = 286 + (NUM_OUTFIT_SETS * 10);

    public VirtualOutfitsSection()
    {
        var outfits = new OutfitEntry[NUM_OUTFITS];
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

                if (i == 13)
                {
                    outfit.equippableFlags = EquippableUsers.Akechi;
                }
                else if (i < 16)
                {
                    outfit.equippableFlags = EquippableUsers.Joker;
                }
                else
                {
                    outfit.equippableFlags = ItemTbl.OrderedEquippable[(i - 16) % 10];
                }

                Marshal.StructureToPtr(
                    outfit,
                    (nint)(ptr + (sizeof(OutfitEntry) * i)),
                    false);
            }
        }
    }

    public uint size;
    public fixed byte outfitsBuffer[NUM_OUTFITS * 32];
}
