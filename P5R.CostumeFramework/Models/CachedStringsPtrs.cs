using System.Runtime.InteropServices;

namespace P5R.CostumeFramework.Models;

internal class CachedStringsPtrs
{
    private readonly Dictionary<string, nint> cachedStrPtrs = new();

    public nint GetStringPtr(string str)
    {
        if (this.cachedStrPtrs.TryGetValue(str, out var ptr))
        {
            return ptr;
        }

        var newPtr = Marshal.StringToHGlobalAnsi(str);
        this.cachedStrPtrs[str] = newPtr;
        return newPtr;
    }
}
