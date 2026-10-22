# SKATER SEOUL — iOS-First Unity 6 MVP Source Pack

**Scope:** self-contained prototype source **for an existing Unity 6 URP project**, with iOS as the first target and Android compatibility as a follow-up. It is not a compiled `.ipa`/`.apk`, an Editor installation, or a production game. All C# and placeholder scene art are original; no copyrighted Skate City/Skate 3 code or third-party art is embedded.

## Run (macOS / Windows Unity Editor)

1. Create or open a **Unity 6 Universal 3D / URP** project. For iOS builds, install the matching Unity iOS Build Support module.
2. `Window > Package Manager`: install **Input System** (`com.unity.inputsystem`). In `Project Settings > Player > Active Input Handling`, choose **Input System Package (New)** or **Both**, then restart when prompted.
3. In `Project Settings > Player > Resolution and Presentation`, set **Default Orientation** to a landscape orientation and disable portrait auto-rotation for this game.
4. Copy the included `Assets/SkaterSeoul` folder into your Unity project's `Assets` directory (do not nest `Assets/Assets`).
5. Let Unity import and compile. Save the current scene, then run **Tools > SKATER SEOUL > 1 - Create Playable MVP Scene**. The generator saves to a new, unused path under `Assets/SkaterSeoul/Scenes/` and does not overwrite an existing MVP scene.
6. Click **Play**. On the left circular pad, flick up for Ollie, up-left for Kickflip, or up-right for Tre Flip. Tap or hold the right circular **PUSH** control to push off and build speed. The two controls accept separate fingers simultaneously.
7. Successful and failed landings change the matching trick's mastery. Switch between **PRACTICE** and **BATTLE** in the top-right HUD. Play through the text-driven bot S.K.A.T.E. match. **R** or **RESET** restarts the match; mastery persists locally with PlayerPrefs.
8. Run **Tools > SKATER SEOUL > 2 - Validate Game Rules** to check gesture classification, monotonic probability and letter rules.

## Current fidelity (important)

| Feature | This prototype | Next production step |
|--|--|--|
| Ride, gravity, collision | Rigidbody, forward cruise, flat ground | ramps, ground normals, downhill momentum |
| Touch controls | Left safe-area trick pad, right push-off button, simultaneous two-finger control, top-right reset/mode taps | device tuning, richer curved gestures, hand QA of touch targets |
| Trick visuals | Primitive board shows Ollie, deck flip, Tre Flip shuv; failure types use different bail poses | humanoid retargeting, independent feet, polished pop/catch/landing beats |
| Skill/odds | Logistic probability, quality gate, sanitized/saved per-trick mastery | seeded production RNG, attempt telemetry, balance tuning |
| Bot S.K.A.T.E. | Player/bot set and defense, letter scoring, practice toggle, restart and win/loss | animated rival, polished mobile match UI |
| Park | Stylized generic test lane | Kenney Mini Skate; Seoul-specific original landmark layout |
| Sound/UI | Basic Unity OnGUI HUD, silent | uGUI/UI Toolkit, rolling / pop / grind sounds, haptic feedback |
| Validation | Unity 6.3 batch compile, rule fixtures, Play Mode touch-routing smoke, unsigned iOS arm64 build, and signed iPhone install/launch | physical touch, safe-area review, and device performance QA |

The source pack was compiled and exercised in a disposable Unity 6.3 project. Xcode installed and launched its iOS build on a connected iPhone. Automated Play Mode smoke checks exercised touch routing with simulated pointer IDs, drove 10 attempts, and checked bail recovery, mastery persistence, and battle transitions. Physical finger input and safe-area appearance still need a hands-on device check. The source pack itself is not a full Unity project: this does not verify import into a separate URP project or iPhone performance. The MVP is deliberately asset-free so mechanics can be reviewed before committing to an art pipeline.

## Controls, platform, and known limits

World X is forward on screen, Y is vertical, Z is depth (frozen for a side-view root). Board local X is deck length; kickflip rolls about board X; Tre Flip adds a board Y spin. The board's visual pivot is separate from the rigidbody. **This is a visual approximation**, not realistic independent skateboard trucks/wheels physics.

The first target is iOS in landscape. The left circular pad collects up to 64 points and maps up/up-left/up-right flicks to Ollie/Kickflip/Tre Flip. A separate right button pushes the rider forward, and two fingers can operate both controls together. Canceled, out-of-area, too-short, or strongly curved trick input is rejected. Probability governs landing success; input quality below 0.27 guarantees failure. Under-rotation, missed catch, and balance loss use different placeholder poses. The primitive rider cannot yet show independent foot catch or a full-body fall.

`SkaterSeoulPrototype` includes regular/goofy stance and forward/backward travel values. The default scene uses Regular + ForwardX. Alternate stance/direction behavior has not been runtime-checked and should be reviewed on-screen before the control map is extended.

The `OnGUI` HUD uses safe-area-anchored touch pads, but remains a prototype with basic labels, no final localization, sound, or haptics. The Seoul skyline is made from generic placeholder blocks, not a recreation of a real park or skyline.

## Licensing

Files created in this pack are original demonstration code and released for the recipient's use, modification, and commercial incorporation. No third-party source code or asset bundles have been copied into the ZIP. Download other assets separately under their publisher's actual current license. See `RESEARCH.md`.
