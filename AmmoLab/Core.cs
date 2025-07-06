using AmmoLab.Patches;
using BoneLib.BoneMenu;
using HarmonyLib;
using Il2CppSLZ.Marrow;
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
            HarmonyInstance.PatchAll();

            PrefsCategory = MelonPreferences.CreateCategory("AmmoLab");

            mod.OnInitializeMelon();
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

        public void OnInitializeMelon() {
            MelonLogger.Msg("Init Ammo Lab BoneMenu");

            Menu.Initialize();
            InitSettings();
            CreateBonemenu();

            AmmoInventoryUtils.OnAddCartridge += (a, b, c) => {
                AmmoInventoryUpdate();
            };
            AmmoInventoryUtils.OnRemoveCartridge += (a, b, c) => {
                AmmoInventoryUpdate();
            };
            MagazineUtils.onEject += RefillMagazine;
            MagazineUtils.onSpawn += MakeMagsGold;
        }

        private static void AmmoInventoryUpdate() {
            if (AutoMagazineRefill.Value)
                MagazineUtils.RefillAllMagazines();
            if (EnableStaticAmmo.Value)
                AmmoInventoryUtils.AddAmmoToInventory(StaticAmmo.Value);
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

    namespace Patches {
        [HarmonyPatch(typeof(Magazine))]
        public class MagazineUtils {
            public static List<Magazine> magazines = new();
            public static Action<Magazine> onSpawn;
            public static Action<Magazine> onDespawn;
            public static Action<Magazine> onInsert;
            public static Action<Magazine> onEject;

            [HarmonyPatch(nameof(Magazine.Awake))]
            [HarmonyPostfix]
            public static void Awake(Magazine __instance) {
                magazines.Add(__instance.TryCast<Magazine>());
                onSpawn.Invoke(__instance);
            }

            [HarmonyPatch(nameof(Magazine.Destroy))]
            [HarmonyPostfix]
            public static void Destroy(Magazine __instance) {
                magazines.Remove(__instance.TryCast<Magazine>());
                onDespawn.Invoke(__instance);
            }

            [HarmonyPatch(nameof(Magazine.OnInsert))]
            [HarmonyPostfix]
            public static void OnInsert(Magazine __instance) {
                onInsert.Invoke(__instance);
            }

            [HarmonyPatch(nameof(Magazine.OnEject))]
            [HarmonyPostfix]
            public static void OnEject(Magazine __instance) {
                onEject.Invoke(__instance);
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

        [HarmonyPatch(typeof(AmmoInventory))]
        public class AmmoInventoryUtils {
            public static AmmoInventory AmmoInventory => AmmoInventory.Instance;
            public static int defaultammo = 2000;

            public static Action<AmmoInventory> OnAwake;
            public static Action<AmmoInventory> OnDestroy;

            public static Action<AmmoInventory, AmmoGroup, int> OnAddCartridge;

            public static Action<AmmoInventory, AmmoGroup, int> OnRemoveCartridge;

            [HarmonyPatch(nameof(AmmoInventory.Awake))]
            [HarmonyPostfix]
            public static void Awake(AmmoInventory __instance) {
                OnAwake.Invoke(__instance);
            }

            [HarmonyPatch(nameof(AmmoInventory.Destroy))]
            [HarmonyPostfix]
            public static void Destroy(AmmoInventory __instance) {
                OnDestroy.Invoke(__instance);
            }
            [HarmonyPatch(nameof(AmmoInventory.AddCartridge))]
            [HarmonyPostfix]
            public static void AddCartridge(AmmoInventory __instance, AmmoGroup ammoGroup, int count) {
                OnAddCartridge.Invoke(__instance, ammoGroup, count);
            }
            [HarmonyPatch(nameof(AmmoInventory.RemoveCartridge))]
            [HarmonyPostfix]
            public static void RemoveCartridge(AmmoInventory __instance, AmmoGroup ammoGroup, int count) {
                OnRemoveCartridge.Invoke(__instance, ammoGroup, count);
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
    }
}