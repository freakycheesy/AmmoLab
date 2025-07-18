using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Data;
using MelonLoader;


namespace AmmoLab.Utils {
    public static class MagazineUtils {
        public static List<Magazine> magazines = new();
        public static Action<Magazine> OnSpawn;
        public static Action<Magazine> OnDespawn;
        public static Action<Magazine> OnEject;

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
        public static Action<CartridgeData, int> OnAmmoChanged;

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
                MagazineUtils.magazines.Add(__instance);
                MagazineUtils.OnSpawn?.Invoke(__instance);
            }

            [HarmonyPatch(nameof(Magazine.OnPoolDeInitialize))]
            [HarmonyPrefix]
            static void _Despawn(Magazine __instance) {
                MagazineUtils.magazines.Remove(__instance);
                MagazineUtils.OnDespawn?.Invoke(__instance);
            }

            [HarmonyPatch(nameof(Magazine.OnEject))]
            [HarmonyPrefix]
            static void _OnEject(Magazine __instance) {
                MagazineUtils.OnEject?.Invoke(__instance);
            }
        }

        [HarmonyPatch(typeof(AmmoInventory))]
        internal static class AmmoInvPatches {
            [HarmonyPatch(nameof(AmmoInventory.Awake))]
            [HarmonyPostfix]
            static void Awake() {
                AmmoInventoryUtils.OnAmmoChanged?.Invoke(null, 0);
            }
            [HarmonyPatch(nameof(AmmoInventory.RemoveCartridge))]
            [HarmonyPostfix]
            static void RemoveCartridge(CartridgeData cartridge, int count) {
                AmmoInventoryUtils.OnAmmoChanged?.Invoke(cartridge, count);
            }
        }
    }
}
