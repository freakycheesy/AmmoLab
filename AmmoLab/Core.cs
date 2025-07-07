using AmmoLab.Utils;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using MelonLoader;
using System.Reflection;
using UnityEngine;

[assembly: MelonInfo(typeof(AmmoLab.Core), "AmmoLab", "6.6.6", "freakycheesy", "https://github.com/freakycheesy/AmmoLab/")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
namespace AmmoLab
{
    public class Core : MelonMod
    {
        public static Color red {
            get {
                Color c = Color.HSVToRGB(0, 0.7f, 1);
                return c;
            }
        }
        public static DefaultMod mod = new();
        public static MelonPreferences_Category PrefsCategory;

        public override void OnInitializeMelon()
        { 
            LoggerInstance.Msg("Initialized.");
            
            HarmonyInstance.PatchAll(typeof(Utils.Patches));
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            MagazineUtils.magazines.Clear();
        }
    }

    public class DefaultMod {
        // Pages
        public static Page mainPage;
        public static Page refillPage;
        public static Page ammoPage;

        // Settings

        public static MelonPreferences_Entry<bool> ActivateMod;
        public static MelonPreferences_Entry<bool> EmptyRefill;
        public static MelonPreferences_Entry<bool> AutoMagazineRefill;
        public static MelonPreferences_Entry<bool> EnableStaticAmmo;
        public static MelonPreferences_Entry<bool> MakeMagazinesGold;
        private static MelonPreferences_Entry<int> StaticAmmo;

        // Colors
        private static MelonPreferences_Entry<Color> gold;

        public void AmmoInventoryUpdate() {
                if (AutoMagazineRefill.Value)
                    MagazineUtils.RefillAllMagazines();
                if (EnableStaticAmmo.Value)
                    AmmoInventoryUtils.AddAmmoToInventory(StaticAmmo.Value);
        }

        public DefaultMod() {
            MelonLogger.Msg("Initialized Ammo Lab BoneMenu");

            Core.PrefsCategory = MelonPreferences.CreateCategory("AmmoLab");
            Menu.Initialize();
            InitSettings();
            CreateBonemenu();

            AmmoInventoryUtils.OnAmmoUpdate += AmmoInventoryUpdate;
            MagazineUtils.OnEject += RefillMagazine;
            MagazineUtils.OnSpawn += MakeMagsGold;
        }


        private static void InitSettings() {
            ActivateMod = Core.PrefsCategory.CreateEntry<bool>(nameof(ActivateMod), true);

            EmptyRefill = Core.PrefsCategory.CreateEntry<bool>(nameof(EmptyRefill), false);
            AutoMagazineRefill = Core.PrefsCategory.CreateEntry<bool>(nameof(AutoMagazineRefill), false);

            EnableStaticAmmo = Core.PrefsCategory.CreateEntry<bool>(nameof(EnableStaticAmmo), true);
            StaticAmmo = Core.PrefsCategory.CreateEntry<int>(nameof(StaticAmmo), 2000);
            MakeMagazinesGold = Core.PrefsCategory.CreateEntry<bool>(nameof(MakeMagazinesGold), true);
            gold = Core.PrefsCategory.CreateEntry<Color>(nameof(gold), new(218, 165, 32));
        }

        private void RefillMagazine(Magazine magazine) {
            if (!ActivateMod.Value)
                return;
            if (!EmptyRefill.Value)
                return;
            MagazineUtils.RefillMagazine(magazine);
        }

        private static void MakeMagsGold(Magazine __instance) {
            if (!ActivateMod.Value)
                return;
            if (!MakeMagazinesGold.Value)
                return;
            List<Renderer> renderers = __instance.GetComponentsInParent<Renderer>().ToList();
            renderers.AddRange(__instance.GetComponentsInChildren<Renderer>());
            foreach (Renderer renderer in renderers) {
                var material = new Material(renderer.material.shader);
                material.color = gold.Value;
                material.SetFloat("_Metallic", 1);
                material.SetFloat("_Smoothness", 1);
                for (int i = 0; i < renderer.materials.Length; i++) {
                    var matList = renderer.materials.ToArray();
                    matList.SetValue(material, i);
                    renderer.materials = matList;
                }
            }
        }

        public static void CreateBonemenu() {
            mainPage = Page.Root.CreatePage("Ammo Lab", Core.red);
            #region Pages
            refillPage = mainPage.CreatePage("Refill Settings", Color.cyan);
            ammoPage = mainPage.CreatePage("Ammo Settings", Core.red);
            #endregion
            #region Elements
            mainPage.CreateBool("Activate Mod", Core.red, ActivateMod.Value, (a) => { ActivateMod.Value = a; Save(); });

            refillPage.CreateBool("Refill on Empty", Color.white, EmptyRefill.Value, (a) => { EmptyRefill.Value = a; Save(); });
            refillPage.CreateBool("Auto Refill", Color.white, AutoMagazineRefill.Value, (a) => { AutoMagazineRefill.Value = a; Save(); });

            ammoPage.CreateBool("Enable Static Ammo", Color.white, EnableStaticAmmo.Value, (a) => { EnableStaticAmmo.Value = a; Save(); });
            ammoPage.CreateInt("Static Ammo Count", Color.white, StaticAmmo.Value, 10, 0, 2000, (a) => { StaticAmmo.Value = a; Save(); });
            ammoPage.CreateBool("Gold Magazines", Color.yellow, MakeMagazinesGold.Value, (a) => { MakeMagazinesGold.Value = a; Save(); });
            #endregion
        }

        private static void Save() {
            MelonPreferences.Save();
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
                    AmmoInventoryUtils.OnAmmoUpdate.Invoke();
                }
            }

            [HarmonyPatch(typeof(Magazine))]
            internal static class MagazinePatches {
                [HarmonyPatch(nameof(Magazine.OnPoolInitialize))]
                [HarmonyPrefix]
                static void _Spawn(Magazine __instance) {
                    MagazineUtils.magazines.Add(__instance);
                    MagazineUtils.OnSpawn.Invoke(__instance);
                }

                [HarmonyPatch(nameof(Magazine.OnPoolDeInitialize))]
                [HarmonyPrefix]
                static void _Despawn(Magazine __instance) {
                    MagazineUtils.magazines.Remove(__instance);
                    MagazineUtils.OnDespawn.Invoke(__instance);
                }

                [HarmonyPatch(nameof(Magazine.OnInsert))]
                [HarmonyPostfix]
                static void _OnInsert(Magazine __instance) {
                    MagazineUtils.OnInsert.Invoke(__instance);
                }

                [HarmonyPatch(nameof(Magazine.OnEject))]
                [HarmonyPostfix]
                static void _OnEject(Magazine __instance) {
                    MagazineUtils.OnEject.Invoke(__instance);
                }
            }
        }
    }
}