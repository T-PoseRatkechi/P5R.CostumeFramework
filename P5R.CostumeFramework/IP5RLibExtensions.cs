using P5R.CostumeFramework.Models;
using p5rpc.lib.interfaces;

namespace P5R.CostumeFramework;

internal static class IP5RLibExtensions
{
    public static int GET_EQUIP(this IP5RLib lib, Character character, EquipSlot slot)
        => lib.FlowCaller.GET_EQUIP((int)character, (int)slot);
}
