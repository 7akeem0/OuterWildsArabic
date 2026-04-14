using HarmonyLib;
using OWML.ModHelper;
using System.Reflection;

namespace OuterWildsArabic
{
    public class OuterWildsArabic : ModBehaviour
    {
        private void Start()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            var api = ModHelper.Interaction.TryGetModApi<ILocalizationAPI>("xen.LocalizationUtility");
            api.RegisterLanguage(this, "Arabic", "assets/Translation.xml");
            api.AddLanguageFont(this, "Arabic", "assets/arabic-font", "NotoKufiArabic-Regular");
            api.AddLanguageFixer("Arabic", ArabicFixer.Fix);
            ModHelper.Console.WriteLine("Arabic loaded", OWML.Common.MessageType.Success);
        }
    }
}
