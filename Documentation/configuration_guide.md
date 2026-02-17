# PKCore Configuration Guide

This document outlines all available configuration options for PKCore layout in `faospark.pkcore.cfg`.

## [01 Project Kyaro Sprites]

**Enhanced Sprite & Visual Settings**


| Setting                         |  Type  | Default | Description                                                                                |
| :-------------------------------- | :------: | :-------: | :------------------------------------------------------------------------------------------- |
| **EnableProjectKyaroSprites**   |  bool  | `true` | Enable Project Kyaro HD sprite textures. Set to`false` for original pixel-based sprites.   |
| **DisableSpritePostProcessing** |  bool  | `true` | Prevent post-processing (bloom, vignette, etc.) on sprites. Prevents white outlines/seams. |
| **SpriteFilteringEnabled**      |  bool  | `true` | Enable texture filtering (Bilinear + 2x Aniso). Set to`false` for original pixelated look. |
| **SMAAQuality**                 | string |  `Low`  | Anti-aliasing quality for sprites (`Off`, `Low`, `Medium`, `High`). `Low` is recommended.  |

## [02 User Interface]

**UI Customization**


| Setting                       |  Type  |    Default    | Description                                                                   |
| :------------------------------ | :------: | :-------------: | :------------------------------------------------------------------------------ |
| **ControllerPromptType**      | string | `PlayStation` | Controller type:`PlayStation`, `Xbox`, `Switch`, `Generic`. (Always enabled). |
| **LoadLauncherUITextures**    |  bool  |    `true`    | Load custom launcher UI textures based on unused game assets.                 |
| **MinimalUI**                 |  bool  |    `true`    | Load minimal UI textures.                                                     |
| **ClassicSaveWindow**         |  bool  |    `true`    | Use PSX-style Save/Load window for a nostalgic feel.                          |
| **DisablePortraitDialogMaskPortraitDialog** |  bool  |    `false`    | Remove the`Face_Mask_01` overlay on portraits in dialog windows.              |
| **ScaleDownDialogBox**        |  bool  |    `true`    | Compact dialog box (80% size) with adjusted position.                         |
| **ScaledDownMenu**            | string |    `true`    | Main menu layout scaled down to 80% with adjusted position.                   |

## [03 General]

**General Game Settings**


| Setting                    |  Type  |  Default  | Description                                                      |
| :--------------------------- | :------: | :---------: | :----------------------------------------------------------------- |
| **SavePointColor**         | string | `default` | Save point orb color (`blue`, `red`, `green`, `default`, etc.).  |
| **DisableSavePointGlow**   |  bool  |  `true`  | Disable glow effect on save point orbs to prevent color washout. |
| **DisableWorldMapClouds**  |  bool  |  `true`  | Disable cloud effects on the world map.                          |
| **DisableWorldMapSunrays** |  bool  |  `true`  | Disable sunray glow effects on the world map.                    |

## [04 Game : Suikoden 1]

**Suikoden 1 Specific**


| Setting                  |  Type  |  Default  | Description                                                     |
| :------------------------- | :------: | :---------: | :---------------------------------------------------------------- |
| **S1ScaledDownWorldMap** |  bool  |  `true`  | Scale down S1 world map UI to 80% for better visibility.        |
| **TirRunTexture**        | string | `default` | Texture variant for Tir's running animation (`default`, `alt`). |

## [05 Game : Suikoden 2]

**Suikoden 2 Specific**


| Setting                       |  Type  |  Default  | Description                                                           |
| :------------------------------ | :------: | :---------: | :---------------------------------------------------------------------- |
| **EnablePortraitSystem**        |  bool  |  `true`  | Enable custom NPC portrait injection system.                          |
| **MercFortFence**             | string | `default` | Mercenary Fortress fence texture variant (`default`, `bamboo`, etc.). |
| **ColoredIntroAndFlashbacks** |  bool  |  `true`  | Restore color to intro and flashback sequences.                       |
| **EnableWarAbilityMod**       |  bool  |  `true`  | Enable war battle ability customization via`S2WarAbilities.json`.      |

## [Performance]

**Optimization Settings**


| Setting                        | Type | Default | Description                                                                     |
| :------------------------------- | :-----: | :-------: | :-------------------------------------------------------------------------------- |
| **EnableTextureManifestCache** | bool | `true` | Cache texture index for faster startup. Disable when adding/removing textures.  |
| **EnableMemoryCaching**        | bool | `true` | Intelligent texture memory management.**Recommended: Keep Enabled.**            |
| **EnableResolutionScaling**    | bool | `false` | Enable the resolution scaling system.                                           |
| **ResolutionScale**            | float |  `1.0`  | Resolution multiplier (`0.5` - `2.0`). Requires `EnableResolutionScaling=true`. |

## [Utility]

**Tools & Window Management**


| Setting                    | Type | Default | Description                                             |
| :--------------------------- | :----: | :-------: | :-------------------------------------------------------- |
| **EnableBorderlessWindow** | bool | `false` | Enable borderless fullscreen window mode.               |
| **ShowMouseCursor**        | bool | `false` | Show mouse cursor in game window. Useful for debugging. |

## [zz - Diagnostics]

**Debugging Options**


| Setting          | Type | Default | Description                                                            |
| :----------------- | :----: | :-------: | :----------------------------------------------------------------------- |
| **LogTextIDs**   | bool | `false` | Log text lookups to console. Useful for finding dialog IDs. (Verbose!) |
| **DetailedLogs** | bool | `false` | Enable detailed logging for texture replacements.                      |

## [zz - Experimental]

**Work In Progress**


| Setting              | Type | Default | Description                                             |
| :--------------------- | :----: | :-------: | :-------------------------------------------------------- |
| **EnableDebugMenu2** | bool | `false` | [EXPERIMENTAL] Enable the developer`DebugMenu2` object. |
