# SKATER SEOUL implementation status

**Updated:** 2026-10-23
**Active spec:** [SKATER_SEOUL_VISUAL_PHYSICS_OVERHAUL_PRD_v2.0.md](SKATER_SEOUL_VISUAL_PHYSICS_OVERHAUL_PRD_v2.0.md)
**Unity project:** [`UnityProject/`](../UnityProject/) — Unity 6000.3.10f1, URP 17.3.0
**QA report:** [VISUAL_OVERHAUL_QA.md](VISUAL_OVERHAUL_QA.md)

Latest Editor Play Mode capture probe: **34 PASS / 1 FAIL**. This is a custom QA log, not a Unity Test Runner suite. The failing assertion is Kickflip landing knee-flex gain (17°/17° measured vs 20–35° required). The wheel-roll metric also used a 0.039m radius against a 0.027m wheel mesh, so its distance match does not prove visual roll calibration. Details and evidence are in the QA report.

The earlier source-pack status below describes the pre-overhaul starting point only. Current implementation and evidence live in `UnityProject/` and the QA report. The scene is an executable visual slice, not a finished mobile release.

| PRD phase | Status | Evidence / remaining work |
|---|---|---|
| P0 — Baseline & capture | **PASS, partial comparison** | Existing project and reference recording inventoried; baseline frames preserved; new Unity 6 URP project, scene and runtime capture created. Before/after viewports differ. |
| P1 — Board movement | **PARTIAL; wheel roll FAIL** | 10-second push/coast covers 47.98m. The old 47.93m wheel-arc match used configured radius 0.039m, while the generated wheel mesh radius is 0.027m; at that mesh radius the same rotation represents about 33.18m. Four flat-ground probes see the surface; reverse, brake, slopes, and device frame-rate parity remain unverified. |
| P2 — Rider & tricks | **PARTIAL; 1 assertion FAIL** | Customized Quaternius humanoid mesh, Kenney clips, procedural foot IK, push/landing poses and three tricks. Kickflip landing knee-flex test measured 17°/17° against a 20–35° target; authored full-body skate animation remains incomplete. |
| P3 — Environment & camera | **PARTIAL** | Original Hangang panorama, custom Blender FBX skater/board/riverside set, Poly Haven concrete PBR, plaza, props, lighting and chase framing. The rider remains visibly stylized and distant scene detail needs polish. |
| P4 — HUD & feel | **PARTIAL** | Runtime Canvas, practice/battle indicators, trick feedback, gesture and PUSH controls. Some saved screenshots show a mode different from their filename. SFX/haptics and safe-area/device input review remain unverified. |
| P5 — QA & delivery | **PARTIAL; custom QA 34/35** | The custom Editor Play Mode capture probe has 34 PASS and the Kickflip knee-flex assertion FAIL; visual asset validation passed. No project-authored Unity Test Runner suite, iOS build/device profile, or 10-second gameplay video was found. Full details and `NOT RUN` gates are in the QA report. |

## Scope wired from the legacy game

The new Unity project includes the stable 39-trick catalog, v13 progress model, stats, missions/career data, deterministic mechanical Judge, success model and five-letter/15-second local `SkateMatch`. The live slice records practice growth and advances a player-set trick into the bot-match phase. Only Ollie, Kickflip and Tre Flip are exposed in the current 3D controls and bot pool. Full career/challenge screens, 8-way trick input, remote/friend play and complete match end-to-end play are not wired.

The old source-pack Unity files were left in place. For detailed migration boundaries and save risks, see [LEGACY_MIGRATION_AUDIT.md](LEGACY_MIGRATION_AUDIT.md) and [LEGACY_MIGRATION_PLAN.md](LEGACY_MIGRATION_PLAN.md).
