using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using I2.Loc;
using HarmonyLib;
using HMLLibrary;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Aquarium.Patches
{
    [HarmonyPatch(typeof(LanguageSourceData), "GetLanguageIndex")]
    static class Patch_GetLanguageIndex
    {
        static void Postfix(LanguageSourceData __instance, ref int __result)
        {
            if (__result == -1 && __instance == Main.language)
                __result = 0;
        }
    }

    [HarmonyPatch(typeof(ModManagerPage), "ShowModInfo")]
    static class Patch_ShowModInfo
    {
        static void Postfix(ModData md)
        {
            if (md.modinfo.mainClass && md.modinfo.mainClass.GetType() == typeof(Main))
                ModManagerPage.modInfoObj.transform.Find("MakePermanent").gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(Storage_Small),"Close")]
    static class Patch_CloseStorage
    {
        static void Postfix(Storage_Small __instance, Network_Player player)
        {
            if (__instance is AquariumBlock aquarium)
                aquarium.OnClose(player);
        }
    }

    [HarmonyPatch(typeof(RGD_Storage), "RestoreInventory")]
    static class Patch_RestoreStorageInventory
    {
        static void Postfix(RGD_Storage __instance, Inventory inventory)
        {
            if (inventory is AquariumInventory aquariumInventory)
                aquariumInventory.aquarium.OnClose(null);
        }
    }

    [HarmonyPatch(typeof(RemovePlaceables), "ReturnItemsFromBlock")]
    static class Patch_ReturnItemsFromBlock
    {
        static void Prefix(Block block, Network_Player player, bool giveItems)
        {
            if (giveItems && player && player.IsLocalPlayer)
            {
                if (block is AquariumBlock a)
                {
                    var inv = a.GetInventoryReference();
                    for (var i = 0; i < inv.GetSlotCount(); i++)
                    {
                        var s = inv.GetSlot(i);
                        if (s && s.HasValidItemInstance())
                            player.Inventory.AddItem(s.itemInstance, true);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StorageManager),"Update")]
    static class Patch_UpdateStorageManager
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(
                    code.FindLastIndex(
                        code.FindIndex(
                            x => x.operand is MethodInfo m && m.Name == "Distance"
                        ),
                        x => x.opcode == OpCodes.Ldfld && x.operand is FieldInfo f && f.Name == "currentStorage"
                    ),
                    x => x.operand is MethodInfo m && m.Name == "get_position"
                );
            code.InsertRange(ind + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageManager),"currentStorage")),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StorageManager),"playerNetwork")),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_UpdateStorageManager),nameof(OverrideBlockPosition)))
            });
            return code;
        }

        public static Vector3 OverrideBlockPosition(Vector3 position, Storage_Small currentStorage, Network_Player player)
        {
            if (currentStorage is AquariumBlock a)
            {
                var playerPosition = player.transform.position;
                var pos = Vector3.zero;
                var dis = float.PositiveInfinity;
                foreach (var c in a.onoffColliders)
                {
                    var p = c.ClosestPoint(playerPosition);
                    var d = (p - playerPosition).sqrMagnitude;
                    if (d < dis)
                    {
                        pos = p;
                        dis = d;
                    }
                }
                if (float.IsFinite(dis))
                    return pos;
            }
            return position;
        }
    }

    [HarmonyPatch(typeof(Storage_Small), "OnIsRayed")]
    static class Patch_OnStorageRayed
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindLastIndex(
                    code.FindIndex(
                        x => x.operand is MethodInfo m && m.Name == "LocalPlayerIsWithinDistance"
                    ),
                    x => x.operand is MethodInfo m && m.Name == "get_position"
                );
            code.InsertRange(ind + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_OnStorageRayed),nameof(OverrideBlockPosition)))
            });
            return code;
        }

        static Vector3 OverrideBlockPosition(Vector3 position, Storage_Small currentStorage) => Patch_UpdateStorageManager.OverrideBlockPosition(position, currentStorage, ComponentManager<Network_Player>.Value);
    }

    [HarmonyPatch(typeof(ReflectionProbeUpdater), "Update")]
    static class Patch_ReflectionProbeUpdate
    {
        static void Prefix(ReflectionProbe ___probe)
        {
            if (float.IsFinite(___probe.center.x))
                ___probe.center = Vector3.one * float.PositiveInfinity;
        }
    }

    [HarmonyPatch(typeof(GameMenu),"Initialize")]
    static class Patch_InitializeGameMenu
    {
        static void Postfix(GameMenu __instance)
        {
            if (__instance.menuType == MenuType.FishingBait)
                Main.instance.InsertFishingOptions();
        }
    }

    [HarmonyPatch(typeof(FishingBaitHandler), "GetRandomItemFromCurrentBaitPool")]
    static class Patch_RandomItemFromBaitHandler
    {
        static void Postfix(Item_Base ___currentBait, ref Item_Base __result)
        {
            if (___currentBait && ___currentBait.UniqueName == "FishingBait_Decor")
                __result = Main.itemSettings.GetRandom(x => x.UniqueName != "FishingBait_Decor").Item;
        }
    }

    [HarmonyPatch(typeof(ItemManager),"GetItemByIndex")]
    static class Patch_GetItemByIndex
    {
        public static bool ignore = false;
        static void Postfix(int uniqueIndex, ref Item_Base __result)
        {
            if (!ignore && uniqueIndex >= 6996 && uniqueIndex <= 7007 && !__result)
                __result = ItemManager.GetItemByIndex(uniqueIndex + 1000);
        }
    }

    [HarmonyPatch(typeof(RAPI), "RegisterItem")]
    static class Patch_RegisterItem
    {
        static void Prefix() => Patch_GetItemByIndex.ignore = true;
        static void Finalizer() => Patch_GetItemByIndex.ignore = false;
    }
}
