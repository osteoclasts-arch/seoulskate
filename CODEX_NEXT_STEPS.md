# SKATER SEOUL — next work after the source-pack MVP

The source pack now contains the first playable gameplay slice. See `docs/SKATER_SEOUL_IMPLEMENTATION_STATUS.md` and `docs/SKATER_SEOUL_TEST_REPORT.md` for completed work and evidence.

## Current scope

- Unity 6 URP source pack; iOS is the first target, Android compatibility follows.
- Player starts from rest with the right push-off button, chooses Ollie / Kickflip / Tre Flip from the left circular trick pad, lands or bails, and grows per-trick mastery.
- Practice and a text-driven bot S.K.A.T.E. match are available.
- Source is validated in a disposable Unity 6.3.10f1 project; no other Unity project or scene is part of this source pack.

## Highest-value next steps

1. Finish the manual landscape iPhone pass: try each trick tap/flick, hold PUSH, combine PUSH with a trick finger, reset/mode buttons, and safe-area spacing.
2. Import `Assets/SkaterSeoul` into the intended iOS Unity 6 URP project and run the editor menu checks plus a manual Play Mode input pass.
3. Refine the OnGUI HUD visuals and touch target sizing after device feedback; preserve independent left trick and right PUSH ownership.
4. Use profiling before claiming the midrange iPhone 60 FPS target. Record device, resolution, Unity build, frame time, and memory.
5. Add licensed park art, a rigged rider, independent feet/catch animation, audio, and haptics only after the core loop is checked. Record every imported asset in `docs/THIRD_PARTY_ASSETS.md`.

## Preservation rules

- Keep work within this source pack until the user selects a Unity project to receive it.
- Scene generation chooses an unused filename; do not replace existing scene assets.
- Do not remove or change other projects, `SkateSim`, `FlowSim`, or user-created scenes.
- Report iOS device and performance checks as `NOT RUN` until they are actually performed.
