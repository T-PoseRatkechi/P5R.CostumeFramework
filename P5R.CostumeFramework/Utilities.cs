using System.Diagnostics;

namespace P5R.CostumeFramework;

internal static class Utilities
{
    public static string PushCallerRegisters = "push rcx\npush rdx\npush r8\npush r9";
    public static string PopCallerRegisters = "pop r9\npop r8\npop rdx\npop rcx";
    public static nint BaseAddress = Process.GetCurrentProcess().MainModule?.BaseAddress ?? 0;

    public static ushort ToBigEndian(this ushort value)
    {
        var bigEndianValue = BitConverter.ToUInt16(BitConverter.GetBytes(value).Reverse().ToArray());
        return bigEndianValue;
    }
}