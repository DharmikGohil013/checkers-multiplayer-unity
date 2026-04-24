# Online Checkers — Multiplayer Turn-Based Checkers Game

A **production-ready multiplayer Checkers game** built in **Unity 6 (6000.0.55f1 LTS)** using **Photon PUN 2** for real-time networking. Supports 2–4 players on configurable 4×4, 6×6, or 8×8 boards.

---

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Getting Started](#getting-started)
- [Photon PUN 2 Setup](#photon-pun-2-setup)
- [Scene Setup](#scene-setup)
- [How to Build APK](#how-to-build-apk)
- [Running Locally (Editor Testing)](#running-locally-editor-testing)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Performance Targets](#performance-targets)

---

## Features

- **Multiplayer:** 2 or 4 players over the internet via Photon PUN 2
- **Configurable Boards:** 4×4, 6×6, 8×8 via ScriptableObject
- **Full Checkers Rules:** Diagonal movement, captures, mandatory multi-jump, king promotion
- **Force Capture Rule:** Configurable mandatory capture
- **Turn Timer:** Configurable per-turn time limit with visual countdown
- **Reconnection Support:** Automatic reconnect with board state restoration
- **Object Pooling:** Zero per-frame GC allocations for pieces and highlights
- **Custom Logger:** Color-coded log levels, compiled out in release builds
- **Clean Architecture:** MVC-like separation (GameManager, BoardManager, NetworkManager, UIManager)

---

## Requirements

| Requirement       | Version / Detail                   |
|-------------------|------------------------------------|
| Unity             | 6000.0.55f1 LTS (Unity 6)         |
| Photon PUN 2      | Latest from Asset Store            |
| TextMeshPro       | Via Unity Package Manager          |
| Platform Target   | Android (ARM64)                    |
| Min Android API   | 24 (Android 7.0)                   |

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/checkers-multiplayer-unity.git
cd checkers-multiplayer-unity
```

### 2. Open in Unity

1. Open **Unity Hub**
2. Click **Add** → navigate to the `OnlineCheckers` folder
3. Ensure Unity version **6000.0.55f1** is selected
4. Open the project

### 3. Install Dependencies

- **Photon PUN 2:** Import from Unity Asset Store (see [Photon Setup](#photon-pun-2-setup))
- **TextMeshPro:** Window → Package Manager → TextMeshPro → Install

### 4. Create ScriptableObject Instances

1. Right-click in `Assets/Resources/` → **Create → Checkers → Board Config** → Name it `BoardConfig`
2. Right-click in `Assets/Resources/` → **Create → Checkers → Piece Config** → Name it `PieceConfig`
3. Right-click in `Assets/Resources/` → **Create → Checkers → Game Settings** → Name it `GameSettings`
4. Configure fields as desired in the Inspector

---

## Photon PUN 2 Setup

1. **Import PUN 2** from the Unity Asset Store:
   - Window → Asset Store → Search "PUN 2 - FREE"
   - Import all files

2. **Enter your App ID:**
   - Go to [Photon Dashboard](https://dashboard.photonengine.com/)
   - Create a new app (Type: Photon PUN)
   - Copy the **App ID**
   - In Unity: Window → Photon Unity Networking → PUN Wizard → Enter App ID
   - Or edit: `Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`

3. **Verify Connection:**
   - Enter Play Mode → Check console for "Connected to Photon Master Server"

---

## Scene Setup

The project uses **two scenes** that must be added to Build Settings in this order:

| Index | Scene Name    | Purpose                           |
|-------|---------------|-----------------------------------|
| 0     | `LobbyScene`  | Main menu, room creation/joining  |
| 1     | `GameScene`    | Actual gameplay                   |

### Build Settings

1. File → Build Settings
2. Add Scenes:
   - `Assets/Scenes/LobbyScene.unity` (Index 0)
   - `Assets/Scenes/GameScene.unity` (Index 1)

### LobbyScene Setup

Create a scene with:
- **Canvas** with LobbyUIController component
- **NetworkLobbyManager** GameObject with NetworkLobbyManager component
- **UIManager** singleton (DontDestroyOnLoad)

### GameScene Setup

Create a scene with:
- **GameManager** GameObject with: GameManager, BoardManager, TurnManager, NetworkGameManager (+ PhotonView), InputHandler, ReconnectHandler
- **Canvas** with GameUIController component
- **Camera** looking at origin (Orthographic recommended)

---

## How to Build APK

1. **File → Build Settings**
2. **Platform:** Android → Switch Platform
3. **Player Settings:**
   - Company Name & Product Name as desired
   - **Other Settings:**
     - Minimum API Level: 24
     - Target API Level: Latest
     - Scripting Backend: IL2CPP
     - Target Architectures: ARM64
   - **Publishing Settings:** Create/assign keystore
4. **Build** → Choose output directory → `OnlineCheckers.apk`

**Target:** APK ≤ 80 MB

---

## Running Locally (Editor Testing)

To test multiplayer in the Editor:

### Method 1: ParrelSync (Recommended)

1. Install [ParrelSync](https://github.com/VeriorPies/ParrelSync)
2. Open ParrelSync → Create Clone
3. Open the clone in a second Unity Editor
4. Enter Play Mode in both editors

### Method 2: Build + Editor

1. Build a standalone player (Windows/Mac)
2. Run the built player
3. Enter Play Mode in the Editor
4. Both instances connect to the same Photon room

### Method 3: Two Builds

1. Build two standalone players
2. Run both and connect to the same room

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── ScriptableObjects/
│   │   ├── BoardConfig.cs        # Board size, colors, cell size
│   │   ├── PieceConfig.cs        # Player colors, sprites, animation
│   │   └── GameSettings.cs       # Turn timer, rules, Photon version
│   ├── Core/
│   │   ├── GameManager.cs        # Singleton orchestrator
│   │   ├── BoardManager.cs       # Board state & piece management
│   │   ├── CheckersRules.cs      # Static pure-function rules
│   │   ├── TurnManager.cs        # Turn order & timer
│   │   └── WinConditionChecker.cs# Win/loss evaluation
│   ├── Network/
│   │   ├── NetworkLobbyManager.cs# Photon lobby & room ops
│   │   ├── NetworkGameManager.cs # In-game RPCs & state sync
│   │   ├── ReconnectHandler.cs   # Auto-reconnection logic
│   │   └── StateSerializer.cs    # Board JSON serialization
│   ├── Gameplay/
│   │   ├── CheckersPiece.cs      # Piece visuals & interaction
│   │   ├── BoardCell.cs          # Cell visuals & highlighting
│   │   ├── InputHandler.cs       # Input state machine
│   │   └── ObjectPool.cs         # Generic object pool
│   ├── UI/
│   │   ├── UIManager.cs          # Singleton UI panel controller
│   │   ├── LobbyUIController.cs  # Lobby-specific UI
│   │   └── GameUIController.cs   # In-game UI
│   └── Utilities/
│       ├── GameLogger.cs         # Custom color-coded logger
│       └── Constants.cs          # All string/numeric constants
├── Resources/
│   ├── BoardConfig.asset         # (Create via menu)
│   ├── PieceConfig.asset         # (Create via menu)
│   └── GameSettings.asset        # (Create via menu)
└── Scenes/
    ├── LobbyScene.unity
    └── GameScene.unity
```

---

## Configuration

All game configuration is done via **ScriptableObjects** in the `Assets/Resources/` folder:

### BoardConfig
- `boardSize`: 4, 6, or 8
- `playerCount`: 2 or 4
- `lightCellColor` / `darkCellColor`: Board appearance
- `cellSize`: World-space size of each cell

### PieceConfig
- `playerColors`: Array of 4 colors for P1–P4
- `normalPieceSprite` / `kingPieceSprite`: Visuals
- `moveAnimationDuration`: Movement lerp speed

### GameSettings
- `turnTimeLimit`: Seconds per turn (0 = unlimited)
- `forceCapture`: Mandatory capture rule
- `kingPromotion`: King promotion on reaching back row
- `photonAppVersion`: Matchmaking version string

---

## Performance Targets

| Metric                | Target              |
|-----------------------|---------------------|
| Min FPS               | 30 FPS              |
| Target Devices        | 3–4 GB RAM Android  |
| APK Size              | ≤ 80 MB             |
| Peak Runtime Memory   | < 500 MB            |
| Per-frame GC          | 0 B allocations     |

See [PerformanceReport.md](PerformanceReport.md) for profiling details.

---

## License

This project uses:
- **Photon PUN 2** — Photon Engine License (free tier)
- **TextMeshPro** — MIT License (Unity Package)
- All game art is procedural/code-generated

See [AssetsUsed.md](AssetsUsed.md) for full asset attributions.
