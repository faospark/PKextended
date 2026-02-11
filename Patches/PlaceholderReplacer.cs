using System;

namespace PKCore.Patches
{
    /// <summary>
    /// Replaces text placeholders with actual protagonist and HQ names from save data.
    /// </summary>
    public static class PlaceholderReplacer
    {
        // Placeholder constants from Suikoden 2 dialog source
        private const string S2_PROTAGONIST = "♂㈱";  // Suikoden 2 protagonist name
        private const string S1_PROTAGONIST = "♂⑩";  // Suikoden 1 protagonist name (save transfer)
        private const string S2_HQ_1 = "♂①";         // Suikoden 2 HQ name (variant 1)
        private const string S2_HQ_2 = "♂②";         // Suikoden 2 HQ name (variant 2)
        private const string S1_HQ = "♂■";           // Suikoden 1 HQ name (save transfer)

        /// <summary>
        /// Replace all placeholders in the given text with actual names from save data.
        /// </summary>
        /// <param name="text">The text containing placeholders</param>
        /// <returns>Text with placeholders replaced by actual names</returns>
        public static string ReplacePlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Quick check: if text doesn't contain the special character ♂, skip processing
            if (!text.Contains("♂"))
                return text;

            string result = text;
            bool replacementOccurred = false;

            // Replace S2 protagonist name (♂㈱)
            if (result.Contains(S2_PROTAGONIST))
            {
                string name = SaveDataProcessor.GetS2ProtagonistName();
                result = result.Replace(S2_PROTAGONIST, name);
                replacementOccurred = true;
            }

            // Replace S1 protagonist name (♂⑩)
            if (result.Contains(S1_PROTAGONIST))
            {
                string name = SaveDataProcessor.GetS1ProtagonistName();
                result = result.Replace(S1_PROTAGONIST, name);
                replacementOccurred = true;
            }

            // Replace S2 HQ name - variant 1 (♂①)
            if (result.Contains(S2_HQ_1))
            {
                string name = SaveDataProcessor.GetS2HQName();
                result = result.Replace(S2_HQ_1, name);
                replacementOccurred = true;
            }

            // Replace S2 HQ name - variant 2 (♂②)
            if (result.Contains(S2_HQ_2))
            {
                string name = SaveDataProcessor.GetS2HQName();
                result = result.Replace(S2_HQ_2, name);
                replacementOccurred = true;
            }

            // Replace S1 HQ name (♂■)
            if (result.Contains(S1_HQ))
            {
                string name = SaveDataProcessor.GetS1HQName();
                result = result.Replace(S1_HQ, name);
                replacementOccurred = true;
            }

            // Log replacement if debug logging is enabled
            if (replacementOccurred && Plugin.Config.LogTextIDs.Value)
            {
                Plugin.Log.LogDebug($"[PlaceholderReplacer] Replaced placeholders: '{text}' -> '{result}'");
            }

            return result;
        }

        /// <summary>
        /// Check if text contains any placeholders.
        /// </summary>
        public static bool ContainsPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Contains(S2_PROTAGONIST) ||
                   text.Contains(S1_PROTAGONIST) ||
                   text.Contains(S2_HQ_1) ||
                   text.Contains(S2_HQ_2) ||
                   text.Contains(S1_HQ);
        }
    }
}
