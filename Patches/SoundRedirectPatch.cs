using System.IO;
using CriWare;
using HarmonyLib;

namespace PKCore.Patches;

/// <summary>
/// Redirects CriWare ACB/AWB file loads through user override folders.
/// 
/// BASE OVERRIDES (low priority) — mirror the original Sound folder structure:
///   PKCore/Sound/BGM2/BATTLE1.acb
///   PKCore/Sound/BGM2/BATTLE1.awb
///   PKCore/Sound/SEHD1/SE1.acb
/// 
/// MOD OVERRIDES (highest priority) — place Sound files inside any mod subfolder:
///   PKCore/00-Mods/MyMod/Sound/BGM2/BATTLE1.acb
///   PKCore/00-Mods/MyMod/Sound/BGM2/BATTLE1.awb
/// 
/// Priority order (highest wins):
///   1. PKCore/00-Mods/&lt;ModName&gt;/Sound/ — mod-specific overrides (alphabetically last mod wins)
///   2. PKCore/Sound/                      — base overrides
///   3. StreamingAssets/Sound/             — original game files (fallback)
/// </summary>
public static class SoundRedirectPatch
{
    private static string _overrideRoot;
    private static string _modsRoot;
    private static string _streamingSoundRoot;

    public static void Initialize()
    {
        // GameRoot = parent of Application.dataPath (the _Data folder)
        string gameRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath);
        _overrideRoot = Path.Combine(gameRoot, "PKCore", "Sound");
        _modsRoot = Path.Combine(gameRoot, "PKCore", "00-Mods");
        _streamingSoundRoot = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Sound");

        Plugin.Log.LogInfo($"[SoundRedirect] Base override folder: {_overrideRoot}");
        Plugin.Log.LogInfo($"[SoundRedirect] Mods folder: {_modsRoot}");

        if (!Directory.Exists(_overrideRoot))
        {
            Directory.CreateDirectory(_overrideRoot);
            Plugin.Log.LogInfo("[SoundRedirect] Created base override folder (empty - add .acb/.awb files to override sounds).");
        }
        else
        {
            var files = Directory.GetFiles(_overrideRoot, "*.acb", SearchOption.AllDirectories);
            Plugin.Log.LogInfo($"[SoundRedirect] Found {files.Length} .acb override file(s) in base folder.");
        }

        // Count mod sound overrides across all mod subfolders
        int modCount = 0;
        if (Directory.Exists(_modsRoot))
        {
            foreach (string modDir in Directory.GetDirectories(_modsRoot))
            {
                string modSoundDir = Path.Combine(modDir, "Sound");
                if (Directory.Exists(modSoundDir))
                {
                    var modFiles = Directory.GetFiles(modSoundDir, "*.acb", SearchOption.AllDirectories);
                    modCount += modFiles.Length;
                    Plugin.Log.LogInfo($"[SoundRedirect] Mod '{Path.GetFileName(modDir)}': {modFiles.Length} .acb file(s).");
                }
            }
        }
        if (modCount > 0)
            Plugin.Log.LogInfo($"[SoundRedirect] Total mod .acb override(s): {modCount}");
    }

    /// <summary>
    /// If an override file exists for the given CriWare path, returns the highest-priority override.
    /// Otherwise returns the original path unchanged.
    /// 
    /// Handles three path formats CriWare may use:
    ///   1. Absolute:   D:\...\Sound\BGM2\BATTLE1.acb
    ///   2. SA-relative: Sound/BGM2/BATTLE1 or Sound/BGM2/BATTLE1.acb
    ///   3. Sound-relative: BGM2/BATTLE1 or BGM2/BATTLE1.acb
    /// 
    /// Priority: 00-Mods > PKCore/Sound > original
    /// </summary>
    private static string TryRedirect(string originalPath)
    {
        if (string.IsNullOrEmpty(originalPath))
            return originalPath;

        // -- Normalise to a path relative to the Sound folder --
        string rel = originalPath.Replace('/', Path.DirectorySeparatorChar)
                                 .Replace('\\', Path.DirectorySeparatorChar);

        // Strip the streaming assets Sound prefix if present
        string soundPrefix = _streamingSoundRoot + Path.DirectorySeparatorChar;
        if (rel.StartsWith(soundPrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            rel = rel.Substring(soundPrefix.Length);
        }
        else if (rel.Contains($"{Path.DirectorySeparatorChar}Sound{Path.DirectorySeparatorChar}"))
        {
            // Absolute path containing \Sound\ somewhere
            int idx = rel.LastIndexOf($"{Path.DirectorySeparatorChar}Sound{Path.DirectorySeparatorChar}",
                                      System.StringComparison.OrdinalIgnoreCase);
            rel = rel.Substring(idx + 7); // skip \Sound\
        }
        else if (rel.StartsWith("Sound" + Path.DirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase))
        {
            rel = rel.Substring(6); // skip "Sound\"
        }
        // else: already relative to Sound folder (e.g. "BGM2\BATTLE1.acb")

        // Layer 1: base PKCore/Sound/ override
        string result = originalPath;
        string baseOverride = Path.Combine(_overrideRoot, rel);
        if (File.Exists(baseOverride))
        {
            Plugin.Log.LogDebug($"[SoundRedirect] '{rel}' matched base override");
            result = baseOverride;
        }

        // Layer 2: 00-Mods/<ModName>/Sound/ — highest priority, alphabetically last mod wins
        if (Directory.Exists(_modsRoot))
        {
            string[] modDirs = Directory.GetDirectories(_modsRoot);
            System.Array.Sort(modDirs, System.StringComparer.OrdinalIgnoreCase);
            foreach (string modDir in modDirs)
            {
                // Skip Better-Launcher-BGM-Mod if the option is disabled
                if (Path.GetFileName(modDir).Equals("Better-Launcher-BGM-Mod", System.StringComparison.OrdinalIgnoreCase)
                    && !Plugin.Config.BetterLauncherBGM.Value)
                    continue;

                string modOverride = Path.Combine(modDir, "Sound", rel);
                if (File.Exists(modOverride))
                {
                    Plugin.Log.LogDebug($"[SoundRedirect] '{rel}' matched mod '{Path.GetFileName(modDir)}'");
                    result = modOverride;
                }
            }
        }

        if (!ReferenceEquals(result, originalPath))
            Plugin.Log.LogInfo($"[SoundRedirect] → '{rel}' overridden");

        return result;
    }

    // -------------------------------------------------------------------------
    // Harmony patch — CriAtomExAcb.LoadAcbFile(awb, acbPath, awbPath)
    //
    // This is the primary CriWare API for loading an ACB+AWB pair from disk.
    // By redirecting the path arguments here we capture all BGM and SE loads.
    // -------------------------------------------------------------------------
    [HarmonyPatch(typeof(CriAtomExAcb), nameof(CriAtomExAcb.LoadAcbFile))]
    [HarmonyPrefix]
    public static void LoadAcbFile_Prefix(ref string acbPath, ref string awbPath)
    {
        acbPath = TryRedirect(acbPath);
        if (!string.IsNullOrEmpty(awbPath))
            awbPath = TryRedirect(awbPath);
    }
}
