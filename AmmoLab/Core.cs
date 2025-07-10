using AmmoLab.Mods;
using AmmoLab.Utils;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Data;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(AmmoLab.Core), "AmmoLab", "6.7.0", "freakycheesy", "https://github.com/freakycheesy/AmmoLab/")]
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
        public static GamblingLabMod gamblingMod;
        public static MelonPreferences_Category PrefsCategory;

        public override void OnInitializeMelon() {
            LoggerInstance.Msg("Initialized.");
            MagazineUtils.DummyNise();
            AmmoInventoryUtils.DummyNise();
            HarmonyInstance.PatchAll();
            mod = new(this);
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

            public static void DummyNise() {
                OnSpawn += DummyAction;
                OnDespawn += DummyAction;
                OnInsert += DummyAction;
                OnEject += DummyAction;
            }

            public static void DummyAction(Magazine mag) {
            }

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
                else if (action == OnDespawn) {
                    magazines.Remove(mag);
                }
                if(magazines.Contains(mag)) action?.Invoke(mag);   
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
            public static Action<CartridgeData, int> OnAmmoUpdate;
            public static void DummyNise() {
                OnAmmoUpdate += (_,_) => DummyAction();
            }

            private static void DummyAction() {
            }

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
            [HarmonyPatch(typeof(AmmoInventory))]
            internal static class AmmoInventoryPatches {
                [HarmonyPatch(nameof(AmmoInventory.RemoveCartridge))]
                [HarmonyPostfix]
                static void RemoveCartridge(CartridgeData cartridge, int count) {
                    if (cartridge)
                        AmmoInventoryUtils.OnAmmoUpdate?.Invoke(cartridge, count);
                }
            }

            [HarmonyPatch(typeof(Magazine))]
            internal static class MagazinePatches {
                [HarmonyPatch(nameof(Magazine.OnPoolInitialize))]
                [HarmonyPrefix]
                static void _Spawn(Magazine __instance) {
                    if (MagazineUtils.OnSpawn == null)
                        return;
                    MagazineUtils.TryAction(__instance, MagazineUtils.OnSpawn);
                }

                [HarmonyPatch(nameof(Magazine.OnPoolDeInitialize))]
                [HarmonyPrefix]
                static void _Despawn(Magazine __instance) {
                    if (MagazineUtils.OnDespawn == null)
                        return;
                    MagazineUtils.TryAction(__instance, MagazineUtils.OnDespawn);
                }

                [HarmonyPatch(nameof(Magazine.OnInsert))]
                [HarmonyPrefix]
                static void _OnInsert(Magazine __instance) {
                    if (MagazineUtils.OnInsert == null)
                        return;
                    MagazineUtils.TryAction(__instance, MagazineUtils.OnInsert);
                }

                [HarmonyPatch(nameof(Magazine.OnEject))]
                [HarmonyPrefix]
                static void _OnEject(Magazine __instance) {
                    if (MagazineUtils.OnEject == null)
                        return;
                    MagazineUtils.TryAction(__instance, MagazineUtils.OnEject);
                }
            }
        }
    }
}