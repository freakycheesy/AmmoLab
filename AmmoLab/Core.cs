using AmmoLab.Mods;
using AmmoLab.Utils;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Data;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(AmmoLab.Core), "AmmoLab", "6.6.6", "freakycheesy", "https://github.com/freakycheesy/AmmoLab/")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
namespace AmmoLab {
    public class Core : MelonMod {
        public static Color red {
            get {
                Color c = Color.HSVToRGB(0, 0.7f, 1);
                return c;
            }
        }
        public static DefaultMod mod;
        public static MelonPreferences_Category PrefsCategory;

        public override void OnInitializeMelon() {
            LoggerInstance.Msg("Initialized.");

            HarmonyInstance.PatchAll(typeof(Utils.Patches));
            mod = new();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            MagazineUtils.magazines.Clear();
        }
    }

    namespace Utils {

        public static class MagazineUtils {
            public static List<Magazine> magazines = new();
            public static Action<Magazine> OnSpawn;
            public static Action<Magazine> OnDespawn;
            public static Action<Magazine> OnInsert;
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
            public static Action OnAmmoUpdate;

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
            [HarmonyPatch(typeof(AmmoInventory))]
            internal static class AmmoInventoryPatches {
                [HarmonyPatch(nameof(AmmoInventory.RemoveCartridge))]
                [HarmonyPostfix]
                static void RemoveCartridge(CartridgeData cartridge, int count) {
                    AmmoInventoryUtils.OnAmmoUpdate?.Invoke();
                }
            }

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

                [HarmonyPatch(nameof(Magazine.OnInsert))]
                [HarmonyPostfix]
                static void _OnInsert(Magazine __instance) {
                    MagazineUtils.OnInsert?.Invoke(__instance);
                }

                [HarmonyPatch(nameof(Magazine.OnEject))]
                [HarmonyPostfix]
                static void _OnEject(Magazine __instance) {
                    MagazineUtils.OnEject?.Invoke(__instance);
                }
            }
        }
    }
}