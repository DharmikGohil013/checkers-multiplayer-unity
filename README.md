# Checkers Multiplayer — Online Turn-Based Game

## Unity Version
6000.0.55f1 LTS (Unity 6)

## Overview
Online multiplayer Checkers built with Unity and Photon PUN 2.
Supports 2 players, 8×8 board, real-time turn-based gameplay.

## How to Build
1. Open project in Unity 6000.0.55f1 LTS
2. Import Photon PUN 2 from Asset Store
3. Enter your Photon App ID in PhotonServerSettings
4. File → Build Settings → Switch to Android
5. Player Settings → Bundle ID: com.dharmikgohil.checkers
6. Click Build → CheckersMultiplayer_v1.0.apk

## How to Play
1. Player 1: Open app → Enter room name → Create Room
2. Player 2: Open app → Enter same room name → Join Room
3. Player 1: Press Start Game
4. Take turns moving pieces diagonally
5. Capture all opponent pieces to win

## Features
- Online 2-player multiplayer via Photon PUN 2
- 8×8 checkers board with standard rules
- Force capture rule enforced
- King promotion on back row
- Turn timer (30 seconds per turn)
- Reconnection support with state recovery
- Object pooling for zero GC allocations
- ScriptableObject configuration system
- GameLogger with log levels (INFO/WARN/ERROR/DESYNC)

## Networking
- Photon PUN 2 (free tier)
- State sync via RPC + Room Properties
- Reconnect: ReconnectAndRejoin() with 3 retry attempts

## Performance
- Min 30 FPS on 3-4 GB RAM devices
- APK size ≤ 80 MB
- Peak memory < 500 MB
- No per-frame GC spikes (object pooling)