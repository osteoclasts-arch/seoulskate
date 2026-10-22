# SKATER SEOUL implementation status

**Updated:** 2026-10-23  
**Source of truth:** `docs/SKATER_SEOUL_PRD_v1.0.md`, corrected to make iOS the first platform.  
**Scope:** source pack only. This folder is not a full Unity project; no `ProjectSettings`, `Packages`, or project scenes were added here.

## Phase status

| Phase | Status | Implemented and checked | Remaining |
|---|---|---|---|
| P0 — Compile & Play | **PARTIAL** | Unity 6.3 batch compile; unique, non-overwriting scene generation in a temporary project; 10 physics attempts, landings, a forced bail, recovery, retry, and simulated touch routing | Hands-on touch play on iPhone and manual Editor input were not exercised |
| P1 — Controls & State | **PARTIAL** | iOS left circular trick pad (tap Ollie/Kickflip/Tre Flip or flick), separate right push-off button, simultaneous two-finger routing, safe-area anchoring, gesture fixtures, stance/travel fields | Physical finger input and alternate stance/direction still need device review |
| P2 — Motion & Feedback | **PARTIAL** | Ollie/Kickflip/Tre Flip visuals; UnderRotation, MissedCatch, and LostBalance bail poses; runtime smoke checked pose differences and recovery | Primitive rider has no independent feet, catch animation, sound, or authored full-body fall |
| P3 — Battle & Progression | **PARTIAL** | Practice/Battle toggle, S.K.A.T.E. letters, setter/defender flow, mismatch, game end, reset, mastery persistence and invalid-save repair; temporary runtime assertions for B01–B10 | Full match has not been played through using real touch/UI; bot has no animated rival; production RNG is not injectable |
| P4 — Art & Device Polish | **PARTIAL** | Unity generated an iOS Xcode project; Xcode installed and launched the app on a connected iPhone 15 Pro | Manual touch/safe-area review, FPS/memory profile, audio/haptics, and licensed art |
| P5 — Expansion | **NOT STARTED** | — | Out of current scope |

## Implemented source

- [SkaterSeoulPrototype.cs](../Assets/SkaterSeoul/Scripts/SkaterSeoulPrototype.cs): left trick-pad tap/flick input, independent right push touch, touch-ID routing for simultaneous fingers, safe-area HUD/pads, keyboard debug input, movement direction and stance, visual trick and failure states, mastery, practice and S.K.A.T.E. flow, duplicate landing guard, battle reset/game-over lock.
- [SkaterSeoulRules.cs](../Assets/SkaterSeoul/Scripts/SkaterSeoulRules.cs): three trick definitions, gesture path and pad-tap classification, circular control gates, probability model, mastery defaults, and letter display.
- [SkaterSeoulSceneBuilder.cs](../Assets/SkaterSeoul/Editor/SkaterSeoulSceneBuilder.cs): new-scene creation, current-scene save prompt, unique output paths, materials and build-settings registration.
- [SkaterSeoulValidation.cs](../Assets/SkaterSeoul/Editor/SkaterSeoulValidation.cs): Editor rule checks for probability, 180 synthetic gesture replays across three aspect ratios, HUD/start-area gates, rejected paths, and letter bounds.
- `README.md`, `CODEX_NEXT_STEPS.md`, `RESEARCH.md`, and docs in this folder describe controls, balance, test evidence, platform scope, and asset status.

## Data and preservation

- Source pack had no Git metadata. A pre-change archive is at `.backups/skater-seoul-prechange-20261023.tar.gz`.
- No existing scene, project, or third-party asset was removed or overwritten. Scene generation was exercised in `/tmp/skater-seoul-unity-validation-20261023`; it created both the base and `_2` scene path without replacing the first.
- The separate Unity project was not used as the implementation target.
- No third-party assets were imported; see `THIRD_PARTY_ASSETS.md`.

## Known risks

1. `OnGUI` remains a basic development HUD. Controls now use safe-area positions, but real device sizing and touch comfort still need a hands-on pass.
2. The temporary validation project used Unity 6.3.10f1 and Input System 1.14.2, but did not include URP. The target URP project must still import and verify the source.
3. Play Mode exercised the touch-routing methods with simulated pointer IDs; hand-operated iOS touch and manual Editor input were not tested.
4. The scene art is generic primitive geometry. It is Seoul-inspired placeholder dressing, not a recognizable Seoul location.
5. Xcode output has no App Store icon. The device app launched, but Xcode's debugger was still resolving device symbols; this is not an App Store-ready build.
6. Global Unity RNG and a single gameplay component make deeper deterministic testing and future animation work harder.
