using AmmoLab.Utils;
using BoneLib.BoneMenu;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Data;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace AmmoLab.Mods {
    public class Mod {
        MelonMod melon;

        public Mod(MelonMod melon) {
            this.melon = melon;
        }
    }
    public class DefaultMod : Mod {
        // Pages
        public static Page mainPage;

        // Settings
        public static MelonPreferences_Entry<bool> ActivateMod;
        public static MelonPreferences_Entry<bool> EmptyRefill;
        public static MelonPreferences_Entry<bool> AutoMagazineRefill;
        public static MelonPreferences_Entry<bool> EnableStaticAmmo;
        public static MelonPreferences_Entry<bool> MakeMagazinesGold;
        private static MelonPreferences_Entry<int> StaticAmmo;

        // Colors
        private static MelonPreferences_Entry<Color> gold;

        public DefaultMod(MelonMod melon) : base(melon) {
            OnStart();
        }

        public static void AmmoInventoryUpdate() {
            if (!ActivateMod.Value)
                return;
            if (AutoMagazineRefill.Value)
                MagazineUtils.RefillAllMagazines();
            if (EnableStaticAmmo.Value)
                AmmoInventoryUtils.AddAmmoToInventory(StaticAmmo.Value);
        }

        private void OnStart() {
            MelonLogger.Msg("Initialized Ammo Lab BoneMenu");

            Core.PrefsCategory = MelonPreferences.CreateCategory("AmmoLab");

            Menu.Initialize();
            InitSettings();
            CreateBonemenu();

            AmmoInventoryUtils.OnAmmoUpdate += (_, _) => AmmoInventoryUpdate();
            MagazineUtils.OnEject += RefillMagazine;
            MagazineUtils.OnSpawn += MakeMagsGold;
        }

        private static void InitSettings() {
            ActivateMod = Core.PrefsCategory.CreateEntry<bool>(nameof(ActivateMod), true);

            EmptyRefill = Core.PrefsCategory.CreateEntry<bool>(nameof(EmptyRefill), false);
            AutoMagazineRefill = Core.PrefsCategory.CreateEntry<bool>(nameof(AutoMagazineRefill), false);

            EnableStaticAmmo = Core.PrefsCategory.CreateEntry<bool>(nameof(EnableStaticAmmo), true);
            StaticAmmo = Core.PrefsCategory.CreateEntry<int>(nameof(StaticAmmo), 2000);

            MakeMagazinesGold = Core.PrefsCategory.CreateEntry<bool>(nameof(MakeMagazinesGold), false);
            gold = Core.PrefsCategory.CreateEntry<Color>(nameof(gold), new(218, 165, 32));
        }

        private static void RefillMagazine(Magazine magazine) {
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
            Page refillPage = mainPage.CreatePage("Refill Settings", Color.cyan);
            Page ammoPage = mainPage.CreatePage("Ammo Settings", Core.red);
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

    public class GamblingLabMod : Mod {
        public static int gambleAmount = 0;
        public static AmmoGroup ammoGroup;
        public enum AmmoGroupEnum : int {
            light = 0,
            medium,
            heavy,
        }
        public AmmoGroupEnum ammoEnum = AmmoGroupEnum.light;
        public static Page page;

        private static FunctionElement gambleinfo;
        public GamblingLabMod(MelonMod melon) : base(melon) {
            ChangeAmmoGroup(ammoEnum);
            page = Page.Root.CreatePage("Gambling Lab", Core.red);
            page.Name = "Gambling <color=white>Lab";
            page.CreateEnum("Change Ammo Group", Color.yellow, ammoEnum, ChangeAmmoGroup);
            page.CreateInt("Amount", Color.blue, 10, 10, 10, 200, ChangeAmount);
            gambleinfo = page.CreateFunction("the gamble info", Color.white, null);
            page.CreateFunction("Gamble", Core.red, Gamble);
        }

        public static void ChangeAmount(int amount) {
            gambleAmount = amount;
        }

        public static void Gamble() {
            if (!ammoGroup) {
                Log("No ammo in your dumb pouch");
                return;
            }
            if (AmmoInventory.Instance.GetCartridgeCount(ammoGroup.cartridges[0]) < gambleAmount) {
                Log("Not enough Ammo");
                return;
            }
            AmmoInventoryUtils.RemoveCartridgeToInventory(ammoGroup, gambleAmount);
            int x = Random.Range(0, gambleAmount);
            if (x <= Mathf.RoundToInt(x / 2)) {
                int y = gambleAmount * Random.Range(0, 10);
                Log($"You won {y} ammo\nwoohoo");
                AmmoInventoryUtils.AddAmmoToInventory(y);
            }
            else {
                Log($"You lost {gambleAmount} ammo lol");
            }
        }
        public static void Log(string context) {
            gambleinfo.ElementName = "the gamble info:\n";
            gambleinfo.ElementName += context;
        }
        public static void ChangeAmmoGroup(Enum i) {
            i ??= AmmoGroupEnum.light;
            switch (i) {
                case AmmoGroupEnum.light:
                    ammoGroup = AmmoInventory.Instance.lightAmmoGroup;
                    break;
                case AmmoGroupEnum.medium:
                    ammoGroup = AmmoInventory.Instance.mediumAmmoGroup;
                    break;
                case AmmoGroupEnum.heavy:
                    ammoGroup = AmmoInventory.Instance.mediumAmmoGroup;
                    break;
            }
        }
    }
}
