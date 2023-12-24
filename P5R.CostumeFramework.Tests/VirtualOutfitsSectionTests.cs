using P5R.CostumeFramework.Models;

namespace P5R.CostumeFramework.Tests;

public class VirtualOutfitsSectionTests
{
    [Fact]
    public void OutfitsSection_Works()
    {
        var outfits = new VirtualOutfitsSection();
        using var file = File.OpenWrite("./test-outfits.tbl");
        using var writer = new BinaryWriter(file);
        writer.Write(outfits.size);
        //writer.Write(outfits.outfitsBuffer);
    }
}
