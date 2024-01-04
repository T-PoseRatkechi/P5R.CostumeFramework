using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using System.Text;

namespace P5R.CostumeFramework.Costumes;

internal static class CostumeDescriptions
{
    public static byte[] Build(GameCostumes costumes, MessageScriptCompiler compiler)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < 16; i++)
        {
            sb.AppendLine($"[msg Item_{i:D3}]");
            sb.AppendLine("[f 0 5 65278][f 2 1]UNUSED[n][e]");
        }

        for (int i = 0; i < costumes.Count; i++)
        {
            sb.AppendLine($"[msg Item_{i:D3}]");
            sb.AppendLine(costumes[i].DescriptionMsg);
        }

        var descriptions = sb.ToString();
        if (compiler.TryCompile(descriptions, out var messageScript))
        {
            using var ms = new MemoryStream();
            messageScript.ToStream(ms, true);
            return ms.ToArray();
        }
        else
        {
            Log.Error(descriptions);
            throw new Exception("Failed to compile costume descriptions.");
        }
    }
}
