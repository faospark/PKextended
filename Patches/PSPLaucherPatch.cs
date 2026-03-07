using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PKCore.Patches;

public static class PSPLauncherPatch
{
    private static bool _bgCreated = false;
    private static bool _soundsBgCreated = false;
    private static string _currentMoviesBgName = null;

    // How long (seconds) it takes the overlay to travel across the screen
    private const float OverlayScrollDuration = 12f;

    public static void Update()
    {
        if (SceneManager.GetActiveScene().name != "Main")
        {
            _bgCreated = false;
            _soundsBgCreated = false;
            _currentMoviesBgName = null;
            return;
        }

        var waterObject = GameObject.Find("Launcher_Root_Variant(Clone)/Launcher_Root_3d_bg/model_water");
        if (waterObject != null && waterObject.activeSelf)
        {
            waterObject.SetActive(false);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Successfully disabled model_water on Launcher.");
        }

        if (!_bgCreated)
        {
            var launcherUI = GameObject.Find("Launcher_Root_Variant(Clone)/Launcher_Root_UI/UI_Canvas");
            if (launcherUI != null)
                TryInsertBackground(launcherUI);
        }

        if (!_soundsBgCreated)
        {
            var soundList = GameObject.Find("UI_Root/UI_Canvas_Root/GalleryParent/UI_SoundList_01(Clone)/Window01");
            if (soundList != null)
                TryInsertGalleryBg(soundList, "PSPGallerySoundsBg", ref _soundsBgCreated, new Vector2(1920, 1080), new Vector2(0, 54));
        }

        {
            var galleryMovies = GameObject.Find("UI_Root/UI_Canvas_Root/GalleryParent/UI_Gallery_01(Clone)/Window01");
            if (galleryMovies != null)
            {
                var imgSelectMovies = GameObject.Find("UI_Root/UI_Canvas_Root/GalleryParent/UI_Gallery_Top01(Clone)/Scroll View/Viewport/Content/UI_Gallery_Button_Set (1)/Img_Select");
                var imgSelectEvents = GameObject.Find("UI_Root/UI_Canvas_Root/GalleryParent/UI_Gallery_Top01(Clone)/Scroll View/Viewport/Content/UI_Gallery_Button_Set (2)/Img_Select");

                string desired = null;
                if (imgSelectMovies != null && imgSelectMovies.activeSelf)
                    desired = "PSPGalleryMoviesBg";
                else if (imgSelectEvents != null && imgSelectEvents.activeSelf)
                    desired = "PSPGalleryEventsBg";

                if (desired != null && desired != _currentMoviesBgName)
                {
                    if (_currentMoviesBgName != null)
                    {
                        var old = galleryMovies.transform.Find(_currentMoviesBgName);
                        if (old != null) UnityEngine.Object.Destroy(old.gameObject);
                    }
                    bool dummy = false;
                    TryInsertGalleryBg(galleryMovies, desired, ref dummy, new Vector2(1920, 1080), new Vector2(0, 11f));
                    _currentMoviesBgName = desired;
                }
            }
        }
    }

    private static void TryInsertBackground(GameObject launcherRoot)
    {
        if (launcherRoot.transform.Find("PSPBg") != null)
        {
            _bgCreated = true;
            return;
        }

        Texture2D tex = CustomTexturePatch.LoadCustomTexture("PSPLauncherbg");
        if (tex == null)
        {
            Plugin.Log.LogWarning("[PSPLauncherPatch] PSPLauncherbg texture not found. Place PSPLauncherbg.png in PKCore/Textures/.");
            _bgCreated = true;
            return;
        }

        // --- Background (index 0, behind everything) ---
        GameObject bgGO = new GameObject("PSPBg");
        bgGO.transform.SetParent(launcherRoot.transform, false);
        bgGO.transform.SetSiblingIndex(0);

        RectTransform bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;

        Plugin.Log.LogInfo("[PSPLauncherPatch] PSPBg fullscreen background inserted.");

        // Disable screen_gradation overlay
        Transform screenGradation = launcherRoot.transform.Find("screen/screen_gradation");
        if (screenGradation != null)
        {
            screenGradation.gameObject.SetActive(false);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled screen_gradation.");
        }

        // Disable screen/frame
        Transform screenFrame = launcherRoot.transform.Find("screen/frame");
        if (screenFrame != null)
        {
            screenFrame.gameObject.SetActive(false);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled screen/frame.");
        }

        // Disable screen/title
        Transform screenTitle = launcherRoot.transform.Find("screen/title");
        if (screenTitle != null)
        {
            screenTitle.gameObject.SetActive(false);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled screen/title.");
        }

        // Insert new UI_Top_Title02_003
        Transform screenGroup = launcherRoot.transform.Find("screen");
        if (screenGroup != null && screenGroup.Find("UI_Top_Title02_003") == null)
        {
            Texture2D titleTex = CustomTexturePatch.LoadCustomTexture("UI_Top_Title02_003");
            if (titleTex != null)
            {
                GameObject titleGO = new GameObject("UI_Top_Title02_003");
                titleGO.transform.SetParent(screenGroup, false);

                RectTransform titleRt = titleGO.AddComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.5f, 0.5f);
                titleRt.anchorMax = new Vector2(0.5f, 0.5f);
                titleRt.pivot     = new Vector2(0.5f, 0.5f);
                titleRt.sizeDelta = new Vector2(titleTex.width, titleTex.height);
                
                // Start with the same location/scale as the original title override
                titleRt.localScale = new Vector3(0.55f, 0.55f, 1f);
                titleRt.localPosition = new Vector3(0, 354.713f, 0f);

                Image titleImg = titleGO.AddComponent<Image>();
                titleImg.sprite = Sprite.Create(titleTex, new Rect(0, 0, titleTex.width, titleTex.height), new Vector2(0.5f, 0.5f), 100f);
                titleImg.color = Color.white;
                titleImg.raycastTarget = false;

                Plugin.Log.LogInfo("[PSPLauncherPatch] UI_Top_Title02_003 inserted into screen.");
            }
            else
            {
                Plugin.Log.LogWarning("[PSPLauncherPatch] UI_Top_Title02_003 texture not found. Place UI_Top_Title02_003.png in PKCore/Textures/.");
            }
        }

        // Disable header_nuetral
        Transform headerNeutral = launcherRoot.transform.Find("header_group/header_nuetral");
        if (headerNeutral != null)
        {
            headerNeutral.gameObject.SetActive(false);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled header_nuetral.");
        }

        Transform headerGs1 = launcherRoot.transform.Find("header_group/header_gs1");
        if (headerGs1 != null)
        {
            headerGs1.gameObject.SetActive(false);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled header_gs1.");
        }

        Transform headerGs2 = launcherRoot.transform.Find("header_group/header_gs2");
        if (headerGs2 != null)
        {
            headerGs2.gameObject.SetActive(false);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled header_gs2.");
        }

        // --- Scrolling overlay (index 1, just above bg) ---
        Texture2D overlayTex = CustomTexturePatch.LoadCustomTexture("PSPLauncherOverlay");
        if (overlayTex != null)
        {
            CreateScrollingOverlay(launcherRoot, overlayTex);
        }
        else
        {
            Plugin.Log.LogInfo("[PSPLauncherPatch] No PSPLauncherOverlay texture found — skipping overlay.");
        }

        // Reposition menu_gs1/all and disable its reflect child
        Transform gs1All = launcherRoot.transform.Find("menu_group/menu_title/menu_gs1/all");
        if (gs1All != null)
        {
            gs1All.localPosition = new Vector3(-0.0004f, -73.1621f, 0f);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Applied position to menu_gs1/all.");

            Transform gs1Reflect = gs1All.Find("reflect");
            if (gs1Reflect != null)
            {
                gs1Reflect.gameObject.SetActive(false);
                Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled reflect on menu_gs1/all.");
            }

            Transform gs1Body = gs1All.Find("body");
            if (gs1Body != null)
            {
                Transform gs1Title = gs1Body.Find("title");
                if (gs1Title != null)
                {
                    gs1Title.gameObject.SetActive(false);
                    Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled title on menu_gs1/all/body.");
                }

                if (gs1Body.Find("PSPSuikoden1Logo") == null)
                {
                    Texture2D logoTex = CustomTexturePatch.LoadCustomTexture("PSPSuikoden1Logo");
                    if (logoTex != null)
                    {
                        GameObject logoGO = new GameObject("PSPSuikoden1Logo");
                        logoGO.transform.SetParent(gs1Body, false);

                        RectTransform logoRt = logoGO.AddComponent<RectTransform>();
                        logoRt.anchorMin = new Vector2(0.5f, 0.5f);
                        logoRt.anchorMax = new Vector2(0.5f, 0.5f);
                        logoRt.pivot     = new Vector2(0.5f, 0.5f);
                        logoRt.sizeDelta = new Vector2(logoTex.width, logoTex.height);
                        logoRt.localScale = new Vector3(0.18f, 0.18f, 1f);
                        logoGO.transform.localPosition = new Vector3(2.6419f, 437.0217f, 0f);

                        Image logoImg = logoGO.AddComponent<Image>();
                        logoImg.sprite = Sprite.Create(logoTex, new Rect(0, 0, logoTex.width, logoTex.height), new Vector2(0.5f, 0.5f), 100f);
                        logoImg.color = Color.white;
                        logoImg.raycastTarget = false;

                        Plugin.Log.LogInfo("[PSPLauncherPatch] PSPSuikoden1Logo inserted into menu_gs1/all/body.");
                    }
                    else
                    {
                        Plugin.Log.LogWarning("[PSPLauncherPatch] PSPSuikoden1Logo texture not found. Place PSPSuikoden1Logo.png in PKCore/Textures/.");
                    }
                }
            }
        }

        // Reposition menu_gs2/all and disable its reflect child
        Transform gs2All = launcherRoot.transform.Find("menu_group/menu_title/menu_gs2/all");
        if (gs2All != null)
        {
            gs2All.localPosition = new Vector3(0.0004f, -78.1504f, 0f);
            Plugin.Log.LogInfo("[PSPLauncherPatch] Applied position to menu_gs2/all.");

            Transform gs2Reflect = gs2All.Find("reflect");
            if (gs2Reflect != null)
            {
                gs2Reflect.gameObject.SetActive(false);
                Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled reflect on menu_gs2/all.");
            }

            Transform gs2Body = gs2All.Find("body");
            if (gs2Body != null)
            {
                Transform gs2Title = gs2Body.Find("title");
                if (gs2Title != null)
                {
                    gs2Title.gameObject.SetActive(false);
                    Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled title on menu_gs2/all/body.");
                }

                if (gs2Body.Find("PSPSuikoden2Logo") == null)
                {
                    Texture2D logo2Tex = CustomTexturePatch.LoadCustomTexture("PSPSuikoden2Logo");
                    if (logo2Tex != null)
                    {
                        GameObject logo2GO = new GameObject("PSPSuikoden2Logo");
                        logo2GO.transform.SetParent(gs2Body, false);

                        RectTransform logo2Rt = logo2GO.AddComponent<RectTransform>();
                        logo2Rt.anchorMin = new Vector2(0.5f, 0.5f);
                        logo2Rt.anchorMax = new Vector2(0.5f, 0.5f);
                        logo2Rt.pivot     = new Vector2(0.5f, 0.5f);
                        logo2Rt.sizeDelta = new Vector2(logo2Tex.width, logo2Tex.height);
                        logo2Rt.localScale = new Vector3(0.3f, 0.3f, 1f);
                        logo2GO.transform.localPosition = new Vector3(-5.1141f, 393.6655f, 0f);

                        Image logo2Img = logo2GO.AddComponent<Image>();
                        logo2Img.sprite = Sprite.Create(logo2Tex, new Rect(0, 0, logo2Tex.width, logo2Tex.height), new Vector2(0.5f, 0.5f), 100f);
                        logo2Img.color = Color.white;
                        logo2Img.raycastTarget = false;

                        Plugin.Log.LogInfo("[PSPLauncherPatch] PSPSuikoden2Logo inserted into menu_gs2/all/body.");
                    }
                    else
                    {
                        Plugin.Log.LogWarning("[PSPLauncherPatch] PSPSuikoden2Logo texture not found. Place PSPSuikoden2Logo.png in PKCore/Textures/.");
                    }
                }
            }
        }

        // UI_Bg_02(Clone): disable Line and bg_gradation, recolor bg
        GameObject uiBg02 = GameObject.Find("UI_Root/UI_Canvas_Root/UI_Bg_02(Clone)");
        if (uiBg02 != null)
        {
            Transform line = uiBg02.transform.Find("Line");
            if (line != null)
            {
                line.gameObject.SetActive(false);
                Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled Line on UI_Bg_02(Clone).");
            }

            Transform bgGradation = uiBg02.transform.Find("bg_gradation");
            if (bgGradation != null)
            {
                bgGradation.gameObject.SetActive(false);
                Plugin.Log.LogInfo("[PSPLauncherPatch] Disabled bg_gradation on UI_Bg_02(Clone).");
            }

            Transform bg = uiBg02.transform.Find("bg");
            if (bg != null)
            {
                Image bgImage = bg.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = new Color(0.1f, 0.345f, 0.922f, 1f);
                    Plugin.Log.LogInfo("[PSPLauncherPatch] Set bg color on UI_Bg_02(Clone)/bg.");
                }
            }
        }

        _bgCreated = true;
    }

    private static void TryInsertGalleryBg(GameObject parent, string textureName, ref bool createdFlag, Vector2 fixedSize = default, Vector2 anchoredPos = default)
    {
        if (parent.transform.Find(textureName) != null)
        {
            createdFlag = true;
            return;
        }

        Texture2D tex = CustomTexturePatch.LoadCustomTexture(textureName);
        if (tex == null)
        {
            Plugin.Log.LogWarning($"[PSPLauncherPatch] {textureName} texture not found. Place {textureName}.png in PKCore/Textures/.");
            createdFlag = true;
            return;
        }

        GameObject bgGO = new GameObject(textureName);
        bgGO.transform.SetParent(parent.transform, false);
        bgGO.transform.SetSiblingIndex(0);

        RectTransform bgRt = bgGO.AddComponent<RectTransform>();
        if (fixedSize != default)
        {
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot     = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = fixedSize;
            bgRt.anchoredPosition = anchoredPos;
        }
        else
        {
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
        }

        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;

        Plugin.Log.LogInfo($"[PSPLauncherPatch] {textureName} background inserted into {parent.name}.");
        createdFlag = true;
    }

    private static void CreateScrollingOverlay(GameObject parent, Texture2D tex)
    {
        // Two-tile seamless scroll:
        // A container holding two side-by-side copies of the texture is animated
        // one tile-width to the left, then restarts. At the restart point tile B
        // is exactly where tile A started, so the loop is invisible.

        const float tileWidth = 1920f;

        // Container: left-anchored, full height, 2× tile width
        GameObject containerGO = new GameObject("PSPOverlay");
        containerGO.transform.SetParent(parent.transform, false);
        containerGO.transform.SetSiblingIndex(1); // just above PSPBg

        RectTransform containerRt = containerGO.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0f, 0f);
        containerRt.anchorMax = new Vector2(0f, 1f);
        containerRt.pivot     = new Vector2(0f, 0.5f);
        containerRt.sizeDelta = new Vector2(tileWidth * 2f, 0f);
        containerRt.anchoredPosition = Vector2.zero; // flush with left edge of parent

        // Tile A (left) and Tile B (right)
        for (int i = 0; i < 2; i++)
        {
            GameObject tileGO = new GameObject($"PSPOverlayTile{i}");
            tileGO.transform.SetParent(containerGO.transform, false);

            RectTransform tileRt = tileGO.AddComponent<RectTransform>();
            tileRt.anchorMin = new Vector2(0f, 0f);
            tileRt.anchorMax = new Vector2(0f, 1f);
            tileRt.pivot     = new Vector2(0f, 0.5f);
            tileRt.sizeDelta = new Vector2(tileWidth, 0f);
            tileRt.anchoredPosition = new Vector2(tileWidth * i, 0f);

            Image img = tileGO.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            img.color = Color.white;
            img.raycastTarget = false;
        }

        // Animate container from x=0 → x=-tileWidth, then Restart (seamless)
        containerRt.DOAnchorPosX(-tileWidth, OverlayScrollDuration)
          .SetEase(Ease.Linear)
          .SetLoops(-1, LoopType.Restart)
          .SetUpdate(UpdateType.Normal, true);

        Plugin.Log.LogInfo($"[PSPLauncherPatch] PSPOverlay seamless scroll started ({OverlayScrollDuration}s loop).");
    }
}

