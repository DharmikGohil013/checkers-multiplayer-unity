# Performance Report — Online Checkers

## Target Metrics

| Metric                  | Target                | Status     |
|-------------------------|-----------------------|------------|
| Minimum Frame Rate      | ≥ 30 FPS              | ⬜ Pending |
| Target Device RAM       | 3–4 GB                | ⬜ Pending |
| APK Size                | ≤ 80 MB               | ⬜ Pending |
| Peak Runtime Memory     | < 500 MB              | ⬜ Pending |
| Per-Frame GC Spikes     | 0 B                   | ⬜ Pending |
| Network Latency Impact  | < 100ms per RPC       | ⬜ Pending |

---

## Device Test Template

Fill in after building and testing on target devices.

### Test Device 1

| Field            | Value       |
|------------------|-------------|
| Device Name      |             |
| OS Version       |             |
| RAM              |             |
| CPU              |             |
| GPU              |             |
| Avg FPS          |             |
| Min FPS          |             |
| Peak Memory      |             |
| APK Size         |             |
| GC Spikes        |             |
| Notes            |             |

### Test Device 2

| Field            | Value       |
|------------------|-------------|
| Device Name      |             |
| OS Version       |             |
| RAM              |             |
| CPU              |             |
| GPU              |             |
| Avg FPS          |             |
| Min FPS          |             |
| Peak Memory      |             |
| APK Size         |             |
| GC Spikes        |             |
| Notes            |             |

### Test Device 3

| Field            | Value       |
|------------------|-------------|
| Device Name      |             |
| OS Version       |             |
| RAM              |             |
| CPU              |             |
| GPU              |             |
| Avg FPS          |             |
| Min FPS          |             |
| Peak Memory      |             |
| APK Size         |             |
| GC Spikes        |             |
| Notes            |             |

---

## Profiling Checklist

Use this checklist when profiling with Unity Profiler:

### Setup
- [ ] Connect device via USB for remote profiling
- [ ] Enable Development Build in Build Settings
- [ ] Enable Autoconnect Profiler in Build Settings
- [ ] Set target to Android IL2CPP ARM64

### CPU Profiling
- [ ] Open Window → Analysis → Profiler
- [ ] Record 60 seconds of gameplay
- [ ] Check for frame spikes > 33ms (below 30 FPS threshold)
- [ ] Identify top CPU consumers per frame
- [ ] Verify no `Update()` methods show unexpected load
- [ ] Confirm no `FindObjectOfType` calls in hot paths

### Memory Profiling
- [ ] Switch to Memory module in Profiler
- [ ] Take a snapshot at: Menu, Lobby, Mid-Game, Game Over
- [ ] Compare snapshots for unexpected growth
- [ ] Check for texture/mesh leaks
- [ ] Verify object pool counts match expectations

### GC Allocation Tracking
- [ ] Enable "GC.Alloc" in CPU Profiler timeline
- [ ] Play through a full game
- [ ] Verify zero GC allocations per frame during:
  - [ ] Idle gameplay (waiting for turn)
  - [ ] Piece selection
  - [ ] Move execution
  - [ ] Timer countdown
- [ ] Acceptable GC allocations (one-time):
  - [ ] Board initialization
  - [ ] Network RPC sending/receiving
  - [ ] Scene transitions

### Network Profiling
- [ ] Monitor Photon traffic via PhotonStatsGui
- [ ] Check RPC frequency (should be 1-2 per move, not per frame)
- [ ] Verify room property updates only on move completion
- [ ] Test reconnection flow (kill app, restart)

---

## Known Optimizations Implemented

### Object Pooling
- **CheckersPiece pool:** Pre-warmed with 24 instances (covers 8×8 two-player maximum)
- **Highlight overlay pool:** Pre-warmed with 64 instances (covers entire 8×8 board)
- Pool auto-expands if exhausted (with warning log)
- All pool operations are O(1) Queue-based

### Zero Per-Frame Allocations
- `List<MoveData>` pre-allocated with capacity 20 (single piece) and 64 (all moves)
- No string concatenation in Update loops
- GameLogger compiled out via `[Conditional]` attributes in release builds
- No `foreach` over non-struct IEnumerable in hot paths

### No FindObjectOfType in Hot Paths
- All manager references cached in `Awake()` / `Start()`
- Singleton pattern with static `_instance` field
- Event-driven architecture (no polling)

### Rendering Optimizations
- 2D SpriteRenderer-based (no 3D mesh overhead)
- Simple quad primitives for cells (minimal draw calls)
- Sprites should be atlased for optimal batching (recommendation)
- Sorting orders organized:
  - Cells: Order 0
  - Pieces: Order 1
  - Moving pieces: Order 3 (temporary during animation)

### Network Optimizations
- RPCs sent only on player action (not per-frame)
- Room properties updated only after move completion (not during animation)
- Board state serialized as compact int[] (not full PieceData objects)
- Encoding: `playerOwner * 10 + isKing` → single int per cell

### Build Size Optimizations
- **IL2CPP scripting backend** for smaller code size
- **Strip unused Engine code** enabled
- No third-party art packages (all procedural)
- Photon PUN 2 demo scenes should be removed before final build

---

## Recommendations for Further Optimization

1. **Sprite Atlasing:** Create a Sprite Atlas for all piece and cell sprites to reduce draw calls
2. **Addressables:** Consider Unity Addressables for lazy-loading assets if APK size becomes an issue
3. **Audio Compression:** When audio is added, use Vorbis compression and load-on-demand
4. **Shader Stripping:** Strip unused shader variants in Player Settings
5. **Profiler Markers:** Add custom `Profiler.BeginSample()`/`EndSample()` to key methods for easier profiling
6. **Frame Rate Cap:** Consider capping to 30 FPS on low-end devices to reduce thermal throttling

---

## Test Results Summary

| Test Run | Date | Device      | Avg FPS | Min FPS | Peak Mem | GC Spikes | Pass |
|----------|------|-------------|---------|---------|----------|-----------|------|
| 1        |      |             |         |         |          |           |      |
| 2        |      |             |         |         |          |           |      |
| 3        |      |             |         |         |          |           |      |

> Fill in after each testing session.
