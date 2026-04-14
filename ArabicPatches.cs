using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OuterWildsArabic
{
    [HarmonyPatch]
    public static class ArabicPatches
    {
        private static bool IsArabic()
        {
            try { return TextTranslation.Get()?.GetLanguage().ToString() == "Arabic"; }
            catch { return false; }
        }

        private static TextAnchor SwapLR(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAnchor.UpperRight;
                case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight;
                case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
                case TextAnchor.UpperRight: return TextAnchor.UpperLeft;
                case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft;
                case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
                default: return a;
            }
        }

        // --- FIX 1: Right-align Arabic text BUT keep typewriter LTR in buffer ---
        // Our text is pre-reversed at build time. Buffer pos 0 = visual right (sentence start).
        // Typing buffer 0->N = visually right-to-left = CORRECT for Arabic.
        // But _bTypeFromRightSide=true would type from buffer END = bottom-to-top = WRONG.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TypeEffectText), "SetTextAlignment")]
        static void TypeEffect_Fix(TypeEffectText __instance)
        {
            if (!IsArabic()) return;
            var textComp = (Text)AccessTools.Field(typeof(TypeEffectText), "_textComponent").GetValue(__instance);
            if (textComp == null) return;
            TextAnchor swapped = SwapLR(textComp.alignment);
            textComp.alignment = swapped;
            AccessTools.Field(typeof(TypeEffectText), "_textAnchor").SetValue(__instance, swapped);
            // CRITICAL: keep _bTypeFromRightSide = false so typewriter types from buffer start
            // (buffer start = visual right = Arabic sentence start)
            AccessTools.Field(typeof(TypeEffectText), "_bTypeFromRightSide").SetValue(__instance, false);
        }

        // --- FIX 2: Right-align dialogue box text fields ---
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DialogueBoxVer2), "InitializeFont")]
        static void Dialogue_Align(DialogueBoxVer2 __instance)
        {
            if (!IsArabic()) return;
            var main = (Text)AccessTools.Field(typeof(DialogueBoxVer2), "_mainTextField").GetValue(__instance);
            var name = (Text)AccessTools.Field(typeof(DialogueBoxVer2), "_nameTextField").GetValue(__instance);
            if (main != null) main.alignment = TextAnchor.MiddleRight;
            if (name != null) name.alignment = TextAnchor.MiddleRight;
        }

        // --- FIX 3: Right-align Nomai translator text ---
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NomaiTranslatorProp), "InitializeFont")]
        static void Nomai_Align(NomaiTranslatorProp __instance)
        {
            if (!IsArabic()) return;
            var tf = (Text)AccessTools.Field(typeof(NomaiTranslatorProp), "_textField").GetValue(__instance);
            if (tf != null) tf.alignment = TextAnchor.UpperRight;
        }

        // --- FIX 4: Prompt word order ---
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SingleInteractionVolume), "SetPromptText",
            new Type[] { typeof(UITextType), typeof(string) })]
        static bool Prompt_Order(SingleInteractionVolume __instance, UITextType promptID, string _characterName)
        {
            if (!IsArabic()) return true;
            var sp = (ScreenPrompt)AccessTools.Field(typeof(SingleInteractionVolume), "_screenPrompt").GetValue(__instance);
            var nip = (ScreenPrompt)AccessTools.Field(typeof(SingleInteractionVolume), "_noCommandIconPrompt").GetValue(__instance);
            string action = UITextLibrary.GetString(promptID);
            sp.SetText(_characterName + " " + action + " <CMD>");
            nip.SetText(_characterName + " " + action);
            return false;
        }

        // --- FIX 5: GetGameOverFont IndexOutOfRange ---
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TextTranslation), "GetGameOverFont")]
        static bool GameOverFont_Fix(ref Font __result)
        {
            var t = TextTranslation.Get();
            if (t == null) { __result = null; return false; }
            int idx = (int)t.GetLanguage();
            if (idx < 0) idx = 0;
            if (idx >= t.m_gameOverFonts.Length)
            {
                __result = TextTranslation.GetFont();
                return false;
            }
            return true;
        }
    }
}
