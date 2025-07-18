using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Data;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmmoLab.Utils {
    public static class MagazineUtils {
        public static List<Magazine> magazines = new();
        public static Action<Magazine> OnSpawn;
        public static Action<Magazine> OnEject;

        public static void TryAction(Magazine mag, Action<Magazine> action) {

            if (action == null)
                return;
            if (mag == null) {
                MelonLogger.Error($"Magazine is {mag != null} + Action is {action != null}, cannot perform action.");
                return;
            }
            if (action == OnSpawn) {
                magazines.Add(mag);
            }
            if (magazines.Contains(mag))
                action?.Invoke(mag);
        }

        public static void RefillAllMagazines() {
            foreach (var mag in magazines) {
                mag.magazineState.Refill();
            }
        }

        public static void RefillMagazine(Magazine mag) {
            mag.magazineState.Refill();
        }
    }

    public static class AmmoInventoryUtils {
        public static AmmoInventory AmmoInventory => AmmoInventory.Instance;
        public static int defaultammo = 2000;

        public static void TryAction(AmmoInventory ammoInventory, Action<AmmoInventory> action) {
            if (action == null)
                return;
            if (ammoInventory == null) {
                MelonLogger.Error($"AmmoInventory is {ammoInventory != null} + Action is {action != null}, cannot perform action.");
                return;
            }
            action?.Invoke(ammoInventory);
        }

        public static void AddAmmoToInventory() {
            AmmoInventory.ClearAmmo();
            AmmoInventory.AddCartridge(AmmoInventory.lightAmmoGroup, defaultammo);
            AmmoInventory.AddCartridge(AmmoInventory.mediumAmmoGroup, defaultammo);
            AmmoInventory.AddCartridge(AmmoInventory.heavyAmmoGroup, defaultammo);
        }

        public static void AddAmmoToInventory(int ammo) {
            AmmoInventory.ClearAmmo();
            AmmoInventory.AddCartridge(AmmoInventory.lightAmmoGroup, ammo);
            AmmoInventory.AddCartridge(AmmoInventory.mediumAmmoGroup, ammo);
            AmmoInventory.AddCartridge(AmmoInventory.heavyAmmoGroup, ammo);
        }

        public static void AddCartridgeToInventory(AmmoGroup ammoGroup, int ammo) {
            AmmoInventory.AddCartridge(ammoGroup, ammo);
        }

        public static void RemoveCartridgeToInventory(AmmoGroup ammoGroup, int ammo) {
            foreach (var cartridge in ammoGroup.cartridges) {
                AmmoInventory.RemoveCartridge(cartridge, ammo);
            }
        }
    }

    internal static class Patches {
        [HarmonyPatch(typeof(Magazine))]
        internal static class MagazinePatches {
            [HarmonyPatch(nameof(Magazine.OnPoolInitialize))]
            [HarmonyPrefix]
            static void _Spawn(Magazine __instance) {
                if (MagazineUtils.OnSpawn == null)
                    return;
                MagazineUtils.OnSpawn?.Invoke(__instance);
            }

            [HarmonyPatch(nameof(Magazine.OnEject))]
            [HarmonyPrefix]
            static void _OnEject(Magazine __instance) {
                if (MagazineUtils.OnEject == null)
                    return;
                MagazineUtils.OnEject?.Invoke(__instance);
            }
        }
    }
}
