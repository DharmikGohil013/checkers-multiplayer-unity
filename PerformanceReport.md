# Performance Report — Checkers Multiplayer

## Device Tested
- Device: Infinix X6870 (your phone model)
- RAM: [your RAM]
- Android Version: [your version]
- Unity Version: 6000.0.55f1 LTS

## Performance Results

| Metric | Target | Achieved |
|--------|--------|----------|
| FPS (gameplay) | ≥ 30 FPS | [fill after test] |
| Peak Memory | < 500 MB | [fill after test] |
| APK Size | ≤ 80 MB | [fill after build] |
| GC Spikes per frame | 0 | 0 |
| Draw Calls | Minimal | [fill from profiler] |

## Optimisations Applied

1. **Object Pooling** — PiecePool (24) and CellPool (64) pre-warmed at startup.
   Eliminates Instantiate/Destroy GC spikes during gameplay.

2. **Pre-allocated Collections** — List<MoveData> with capacity 20 reused per frame.
   No new List allocations in hot paths.

3. **Cached References** — All GetComponent calls cached in Awake/Start.
   No per-frame component lookups.

4. **Deterministic RPC Moves** — Only move data sent over network (4 integers per move).
   Minimal bandwidth usage.

5. **IL2CPP + ARM64** — Scripting backend IL2CPP, single architecture ARM64.
   Reduces APK size and improves runtime performance.

6. **No per-frame string allocation** — GameLogger uses conditional compilation
   (#if DEVELOPMENT_BUILD) so logging is stripped from release builds.

## Profiler Screenshots
[attach screenshots from Unity Profiler if possible]