# Design Document — Online Checkers

## 1. Game Overview

**Online Checkers** is a multiplayer turn-based board game implementing the classic Checkers (Draughts) rule set. Players compete over the internet using Photon PUN 2 for real-time networking.

### Core Rules

- Played on an 8×8 board (default), with only dark-colored diagonal cells used
- Each player starts with pieces placed on their side of the board
- Pieces move diagonally forward one cell at a time
- **Captures:** Jump diagonally over an adjacent opponent piece into an empty cell behind it
- **Multi-Jump:** If a capture leads to another available capture, the player must continue jumping
- **Force Capture:** When enabled, if any capture is available, the player must capture (cannot make a simple move)
- **King Promotion:** A piece reaching the opponent's back row is promoted to King
- **King Movement:** Kings can move and capture diagonally in all 4 directions
- **Win Condition:** A player wins when the opponent has no pieces left or no valid moves

---

## 2. Board Configurations

The game supports three board sizes, configurable via ScriptableObject:

| Size | Cells | Playable Cells | Pieces Per Player (2P) | Pieces Per Player (4P) |
|------|-------|----------------|------------------------|------------------------|
| 4×4  | 16    | 8              | 2                      | 1                      |
| 6×6  | 36    | 18             | 6                      | 3                      |
| 8×8  | 64    | 32             | 12                     | 6                      |

### Board Layout

```
  0 1 2 3 4 5 6 7     (columns)
7 . ● . ● . ● . ●     ← Player 2 pieces
6 ● . ● . ● . ● .
5 . ● . ● . ● . ●
4 . . . . . . . .     ← Empty rows
3 . . . . . . . .
2 ○ . ○ . ○ . ○ .     ← Player 1 pieces
1 . ○ . ○ . ○ . ○
0 ○ . ○ . ○ . ○ .
(rows)
```

Legend: `●` = Player 2, `○` = Player 1, `.` = empty

---

## 3. Player Count

### 2-Player Mode (Default)
- Player 1: Bottom rows, moves upward (+row direction)
- Player 2: Top rows, moves downward (-row direction)

### 4-Player Mode (Extended)
- Player 1: Bottom-left quadrant, moves upward
- Player 2: Top-right quadrant, moves downward
- Player 3: Top-left quadrant, moves downward
- Player 4: Bottom-right quadrant, moves upward
- Players are eliminated when they lose all pieces or have no moves
- Last player standing wins

---

## 4. Turn System Design

### Turn Order
- Players take turns in order of their Photon Actor Numbers (sorted ascending)
- Turn index wraps around the player array

### Turn Timer
- Configurable via `GameSettings.turnTimeLimit`
- When timer expires, the turn automatically advances (MasterClient triggers)
- Timer UI shows countdown slider + seconds remaining
- Color-codes: White (>10s), Orange (5-10s), Red (<5s)
- Setting to 0 disables the timer

### Turn State Machine (InputHandler)
```
IDLE
  └─ [Piece Clicked] → PIECE_SELECTED
        ├─ [Valid Cell Clicked] → WAITING_FOR_NETWORK → IDLE
        ├─ [Same Piece Clicked] → IDLE (deselect)
        ├─ [Other Own Piece Clicked] → PIECE_SELECTED (re-select)
        └─ [Invalid Cell Clicked] → IDLE (deselect)
```

---

## 5. Win/Loss Conditions

A player **loses** when:
1. They have **no pieces** remaining on the board
2. They have pieces but **no valid moves** available

A player **wins** when:
- All opponents have been eliminated (2P: the other player; 4P: last standing)

A **draw** occurs when:
- No player can make a valid move (extremely rare)

---

## 6. Network Architecture

### Technology
- **Photon PUN 2** (Photon Unity Networking)
- Room-based matchmaking with custom properties
- Deterministic state via RPCs + room property backup

### Connection Flow

```
[App Start]
    ↓
[Connect to Photon Master Server]
    ↓
[Join Lobby]
    ↓
[Create Room / Join Room / Join Random]
    ↓
[Wait for Players]
    ↓
[MasterClient: Load Game Scene]
    ↓
[RPC: Initialize Game (player order)]
    ↓
[Game Loop: RPCs for moves]
    ↓
[Game Over → Return to Lobby]
```

### RPC Methods (NetworkGameManager)

| RPC Name              | Target    | Purpose                              |
|-----------------------|-----------|--------------------------------------|
| `RPC_InitializeGame`  | All       | Set player order, start game         |
| `RPC_ExecuteMove`     | All       | Execute a piece move on all clients  |
| `RPC_EndTurn`         | All       | Advance to next player's turn        |
| `RPC_GameOver`        | All       | Declare game winner                  |
| `RPC_SyncBoardState`  | All/One   | Full state sync (reconnect/desync)   |

### Room Properties

| Key            | Type   | Purpose                           |
|----------------|--------|-----------------------------------|
| `BOARD_SIZE`   | int    | Lobby filtering                   |
| `PLAYER_COUNT` | int    | Lobby filtering                   |
| `BOARD_STATE`  | string | Serialized board (reconnect)      |
| `TURN_INDEX`   | int    | Current turn index                |
| `GAME_STATE`   | string | Full serialized snapshot           |

---

## 7. State Sync Strategy

### Deterministic Execution
- All game logic is deterministic (same input → same output)
- Moves are sent as RPCs with exact coordinates
- Each client independently executes the move locally

### Room Property Backup
- After each move, MasterClient serializes the board to room properties
- If a client disconnects and reconnects, it reads the room property to restore state

### Desync Detection
- `StateSerializer.AreBoardsEqual()` can compare two board states
- `GameLogger.LogDesync()` logs mismatches with both expected and actual values

---

## 8. Reconnection Flow

```
[Disconnect Detected]
    ↓
[Show "Reconnecting..." UI]
    ↓
[Wait 2 seconds]
    ↓
[Attempt ReconnectAndRejoin()]
    ├─ Success → [Read Room Properties] → [Restore Board State] → [Resume Game]
    └─ Fail → [Wait 5 seconds]
              ↓
         [Retry (up to 3 times)]
              ├─ Success → [Restore State]
              └─ Fail (3 attempts exhausted) → [Show "Connection Failed"] → [Return to Lobby]
```

---

## 9. ScriptableObject Config System

All game configuration is externalized into ScriptableObjects loaded from `Resources/`:

### BoardConfig
- Board size, player count, cell colors, cell size
- `GetPiecesPerPlayer()` auto-calculates based on board size

### PieceConfig
- Player colors (4 slots), piece sprites, animation duration
- `GetPlayerColor(int index)` with bounds clamping

### GameSettings
- Turn time limit, force capture rule, king promotion, Photon app version

**Benefits:**
- No code changes needed to tweak game balance
- Designer-friendly Inspector UI
- Multiple configs can be created for different game modes

---

## 10. Object Pooling Strategy

### Why Pool
- Avoid `Instantiate()`/`Destroy()` GC spikes during gameplay
- Keep per-frame allocations at zero

### What's Pooled

| Object Type      | Pool Size | Reason                            |
|------------------|-----------|-----------------------------------|
| CheckersPiece    | 24        | Max pieces on 8×8 board           |
| Highlight Overlay| 64        | Max cells on 8×8 board            |

### Pool Behavior
- **Pre-warm:** All instances created in `Awake()`
- **Get():** Dequeue + SetActive(true)
- **Return():** SetActive(false) + Enqueue
- **Exhaustion:** Auto-expand with a warning log
- **Generic:** `ObjectPool<T>` works for any MonoBehaviour

---

## 11. Key Design Decisions

| Decision                         | Rationale                                              |
|----------------------------------|--------------------------------------------------------|
| Index-based board state          | Deterministic, no floating-point drift                 |
| Static CheckersRules class       | Pure functions, testable, no side effects               |
| Room Properties for state backup | Survives MasterClient migration                        |
| Conditional compilation logging  | Zero overhead in release builds                        |
| Pre-allocated Lists              | No per-frame allocations from move queries             |
| Sorted actor numbers for order   | Deterministic player order across all clients          |
| SpriteRenderer-based visuals     | Lightweight, good for 2D, easy to pool                 |
