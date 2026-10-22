# SKATER SEOUL: curated research and source/license ledger (2026-10-23)

No external source code/assets are bundled. Check the actual license text when downloading; some projects have repository-specific terms, even if GitHub says 'public'.

## Reference repositories

| Resource | URL | Use case | License / caveat |
|--|--|--|--|
| StarKnightt Golden Hour Skatepark | https://github.com/StarKnightt/skate-park | Browser demo for board/world physics, procedural rider, grinds, touch, camera | **No repo license** per README. Research / gameplay observation only; obtain permission to copy. |
| SK8 ENGINE Skate 3 Rust | https://github.com/SK8-ENGINE/skate-3-rust-engine | Deep trick/physics/reverse engineering research | **GPL-3.0-only** for project-owned original code; source obligations if incorporated. Requires lawfully owned original EA game assets to play. |
| SK8 ENGINE Custom Layer | https://github.com/SK8-ENGINE/SK8-Engine | Study world authoring, collision and rail design | Original layer MIT but upstream and EA assets have separate rights. Not a Unity mobile base. |
| Shahzaib52 Unity Skateboard Physics | https://github.com/Shahzaib52/Skateboard-Physics-Controller-Unity | Simple Unity physics experiments | Author grants broad permission in README; **no SPDX LICENSE found**. Ask for clarification before commercial copying. |
| nahkranoth/Skateboard | https://github.com/nahkranoth/Skateboard | Historical touch-based Unity skateboard jam prototype | **AGPL-3.0**, old Unity version. Avoid as source for proprietary release. |
| XifengAlpha Slidingboard Unity | https://github.com/XifengAlpha/Slidingboard-Unity | Modular controller design, speed and ground motion | No confirmed license from repo page. Not a skateboard tricks controller. |
| burning-laboratory/unity-swipe-input | https://github.com/burning-laboratory/unity-swipe-input | Quick Unity swipe start/end/direction events | **MIT** per repo. Useful for simple swipes; for advanced tricks, record entire point trajectory. |
| SteBeeGizmo/DollarUnity | https://github.com/SteBeeGizmo/DollarUnity | Template matching of 2D unistroke gestures | Original $1 BSD 3-clause + port MIT; retain notices. README cautions on straight-line swipes and always-match issue. |
| Official Unity InputSystem docs | https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/touch-polling.md | Correct EnhancedTouch activation, activeTouches lifecycle | Official documentation, not source needed. |
| Unity Animation Rigging | https://docs.unity3d.com/Manual/com.unity.animation.rigging.html | RigBuilder and Two Bone IK of skater's feet | Official Unity package via Package Manager. |
| REA - original | https://github.com/morluto/rea and https://rea.tools | Investigate legally inspectable executables/apps, plus open-source implementation comparisons | REA-authored code MIT; separate Hopper/IDA licenses; REA is not a 'clone any game' button. |

## Asset sources

| Resource | URL | License / gotchas |
|--|--|--|
| Kenney Mini Skate | https://kenney.nl/assets/mini-skate | CC0. **Page checked 2026-10-23 shows 40 files v2.0** (older search results show 20). Useful for ramps, skatepark staging and rig experiments. |
| Kenney City Roads | https://kenney.nl/assets/city-kit-roads | CC0; Seoul-esque placeholder street infrastructure. |
| Kenney City Suburban | https://kenney.nl/assets/city-kit-suburban | CC0; placeholder buildings. |
| Kenney City Commercial | https://opengameart.org/content/city-kit-commercial | CC0; denser city background. |
| Quaternius Universal Animation Library | https://quaternius.itch.io/universal-animation-library | CC0; 120+ advertised library, **free Standard subset**; not guaranteed skate trick clips. Good for crouch, balance, general biped animation/retargeting. |
| Quaternius UAL 2 | https://quaternius.itch.io/universal-animation-library-2 | CC0; supplemental parkour/locomotion clips, Standard subset. |
| Adobe Mixamo | https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html | Free with Adobe ID; royalty-free use within game, but do NOT redistribute standalone/raw character + animation packages. No guarantee of authentic ollie/kickflip clips. |
| OpenGameArt skateboard | https://opengameart.org/content/skateboard | CC0 plain static OBJ; no rig animation. |
| Poly Haven HDRI Skate Park | https://polyhaven.com/a/skate_park | CC0; environment lighting sky texture; not a playable geometry model. |
| Poly Haven textures/models | https://polyhaven.com/license | CC0. |
| ambientCG PBR surfaces | https://ambientcg.com/ | CC0; concrete, asphalt, brushed metal. |
| Freesound CC0 sample credits in Golden Hour Skatepark | https://github.com/StarKnightt/skate-park | Site README attributes rolling-concrete, wheel, grinding sounds CC0; verify the ORIGINAL sound page before adding. |

## REA workflow for a Mac user with Codex

1. Read official `https://rea.tools` and `https://github.com/morluto/rea/blob/main/docs/installation.md`.
2. Install a supported Node version per project instructions.
3. Run `npx rea-agents@latest setup --client codex` and **review/approve** suggested MCP and skills changes. Restart Codex to load tools.
4. Ask it to `doctor --client codex` per installed REA CLI help / docs to verify actual version and provider readiness; native binaries additionally require a licensed compatible native-analysis provider (Hopper / Ghidra / IDA). Do not assume JS or Unity-native binary analysis is identical.
5. Investigate only software you're authorized to inspect. Prefer open projects. Require each conclusion to reference a file/function/observation. Avoid extracting/redistributing copyrighted code, animations, maps, textures or circumvention of copy protection.
6. For Skate City, prefer **black-box gameplay study with recorded control trajectories and videos** rather than assuming internal binaries are available/authorized.

## About S.K.A.T.E. rules

Real SKATE rule variants differ (whether tricks may repeat, whether last-letter defense grants two tries, how setter switches). The prototype selects one explicit single-player variant: repeat tricks are allowed, the final E has no extra defense, and a successful defender leaves the setter unchanged. The bot's response is text-led and has no animated rival. Battle transition smoke tests cover setter failures, matching, mismatch, letters, reset, and match end. This is not a claim that the prototype represents every local ruleset.

Rules sources:
- https://skateboarderschool.com/battle-at-the-berrics-rules-of-skate/
- https://www.surfertoday.com/skateboarding/game-of-skate

## Outstanding quality gates before a public build

- Import/compile in a real Unity 6 URP project; resolve any actual input/build errors.
- iOS first: 16:9, 19.5:9, 20:9 touch ownership, notch/safe-area layout, UI button taps, accidental swipe cancellation, and gesture quality. Android compatibility is a separate follow-up.
- Camera horizon, curb contact, manual/grind collision classification, foot IK, axle and shoe sync, bailed board/rider separation.
- Log attempt state: input points, touch duration, trick ID, mastery, predicted probability, RNG outcome, failure cause, stance, approach direction; measure frustration and success calibration.
- Performance profile on a midrange iPhone; check SRP Batcher, memory/GC and device GPU frame time before claiming 60 FPS. Android profiling is follow-up work.
- No brand/trademark logos or third-party maps extracted from games; Seoul location art should be original / licensed.
