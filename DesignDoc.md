# Design Document — Checkers Multiplayer

## Game Overview
Online turn-based Checkers for 2 players on Android.

## Rules
- 8×8 board, pieces on dark cells only
- Normal pieces move diagonally forward only
- Kings move diagonally in all 4 directions
- Capture by jumping over opponent piece into empty square
- Multi-jump: after a capture, if another capture is available, must continue
- Force capture: if capture is available, player must take it
- King promotion: piece reaching opponent's back row becomes a king
- Win: opponent has no pieces left OR opponent has no valid moves

## System Design

### Architecture
- GameManager (Singleton) — game state, events
- BoardManager — board state, piece placement, move logic
- TurnManager — turn order, timer, player list
- NetworkGameManager — Photon RPCs, state sync
- InputHandler — touch/click detection, move processing
- UIManager — all UI panels and updates

### Networking
- Photon PUN 2 room-based multiplayer
- Moves sent via RPC to all players (deterministic)
- Full board state stored in Room Properties on every move
- On reconnect: board restored from Room Properties

### ScriptableObjects
- BoardConfig: board size, cell size, colors, player count
- PieceConfig: player colors, sprites, animation duration
- GameSettings: turn timer, force capture, king promotion

### Object Pooling
- PiecePool: 24 pre-warmed CheckersPiece objects
- CellPool: 64 pre-warmed BoardCell objects
- Zero per-frame GC allocations during gameplay

## Coin Logic
Not applicable — this is a pure multiplayer game with no economy system.