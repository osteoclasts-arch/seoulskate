# Controls — SKATER SEOUL prototype

The first platform target is **iOS**, in landscape. Android compatibility is not device-tested.

## Touch controls

| Control | Action |
|---|---|
| Left circular pad, flick up | Ollie |
| Left circular pad, flick up-left | Kickflip |
| Left circular pad, flick up-right | Tre Flip |
| Right circular button, tap or hold | Push off; holding builds speed, then the board coasts |
| Top-right `RESET` | Reset the S.K.A.T.E. match |
| Top-right `PRACTICE` / `BATTLE` | Change mode; mastery is preserved |

The trick pad and push button accept separate fingers at the same time. The trick flick is sampled at up to 64 points, must move upward, lasts 0.035–0.9 seconds, and rejects strongly curved paths. The input maps are anchored to `Screen.safeArea`; the gameplay remains landscape.

## Editor controls

| Action | Input |
|---|---|
| Ollie / Kickflip / Tre Flip | `1` / `2` / `3` |
| Push off | Hold `Space` |
| Reset match | `R` |
| Change mode | Click top-right button |
| Touch-equivalent mouse input | Drag from left circular pad; press/hold right circular button |

Gesture quality uses distance, duration, direction, and path straightness. It is clamped to `[0,1]`; quality below `0.27` gives zero landing probability. The HUD reports the recognized trick, quality, and current success chance.

## Stance and travel direction

The prototype stores `SkateStance.Regular/Goofy` and `TravelDirection.ForwardX/BackwardX`. The generated scene uses Regular + ForwardX. Goofy reverses flip/shuv direction; BackwardX reverses travel and shuv direction. Alternate combinations have not been checked on an iPhone.

## Input limits

- The touch controls are custom `OnGUI` visuals with direct EnhancedTouch hit testing. No UGUI package or event-system dependency is required.
- iPhone build, install, and app process launch were verified. The left pad, simultaneous push/trick input, and safe-area spacing still need a hands-on check.
- Tre Flip uses the prototype's up-right flick; curved and multi-part trick gestures are not implemented.
