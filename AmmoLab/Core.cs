using AmmoLab.Mods;
using AmmoLab.Utils;
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
            mod = new();
            gamblingMod = new();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName) {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            MagazineUtils.magazines.Clear();
        }
    }
}