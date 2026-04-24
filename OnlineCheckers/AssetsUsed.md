# Assets Used — Online Checkers

This document lists all third-party and first-party assets used in the Online Checkers project.

---

## Engine & Frameworks

| Asset                      | Source                  | License                    | Notes                              |
|----------------------------|-------------------------|----------------------------|------------------------------------|
| Unity 6 (6000.0.55f1 LTS) | Unity Technologies      | Unity License              | Game engine                        |
| Photon PUN 2 (Free)        | Exit Games              | Photon Engine License      | Networking (free tier, 20 CCU)     |
| TextMeshPro                | Unity Package Manager   | MIT License                | Advanced text rendering            |

---

## Art Assets

| Asset           | Type       | Source         | License     | Notes                                       |
|-----------------|------------|----------------|-------------|---------------------------------------------|
| Board Cells     | Procedural | Code-generated | N/A         | Simple colored quads via SpriteRenderer      |
| Piece Sprites   | Procedural | Code-generated | N/A         | Colored sprites, can be replaced with custom |
| Highlight Overlays | Procedural | Code-generated | N/A      | Runtime-generated 4×4 white textures         |
| UI Elements     | Unity Built-in | Unity      | Unity License | Standard UI (Canvas, Button, Slider, etc.)  |

> **Note:** No paid or third-party art assets are used. All visuals are procedurally generated or use Unity's built-in primitives and UI elements. Custom sprites can be assigned via the `PieceConfig` ScriptableObject.

---

## Fonts

| Font         | Source        | License     | Notes                          |
|--------------|---------------|-------------|--------------------------------|
| TMP Default  | TextMeshPro   | MIT         | Default font included with TMP |

> Custom fonts can be added by creating TMP Font Assets and assigning them to UI text components.

---

## Audio Assets

| Asset | Source | License | Notes |
|-------|--------|---------|-------|
| None  | —      | —       | No audio assets currently used. Sound effects can be added in future iterations. |

---

## Plugins & Packages

| Package                                    | Version  | Source                   | License              |
|--------------------------------------------|----------|--------------------------|----------------------|
| Photon Unity Networking (PUN 2)            | Latest   | Unity Asset Store        | Photon License       |
| Photon Realtime                            | Included | PUN 2 package            | Photon License       |
| ExitGames.Client.Photon                    | Included | PUN 2 package            | Photon License       |
| TextMeshPro                                | Latest   | Unity Package Manager    | MIT                  |

---

## Notes

1. **Free Tier Limitations:** Photon PUN 2 free tier supports up to 20 concurrent users (CCU). For production deployment, a Photon plan upgrade may be required.

2. **No External Dependencies:** The project intentionally avoids external art packages to keep the APK size minimal and eliminate licensing concerns.

3. **Customization:** All visuals can be customized by:
   - Assigning custom sprites to `PieceConfig.normalPieceSprite` and `PieceConfig.kingPieceSprite`
   - Adjusting colors in `PieceConfig.playerColors` and `BoardConfig` cell colors
   - Replacing the procedural cell generation with custom prefabs

4. **Attribution Not Required:** All used assets are either first-party (Unity built-in), MIT-licensed (no attribution required in binary), or under Photon's commercial license.
