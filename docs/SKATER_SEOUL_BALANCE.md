# Prototype balance values

These are tunable starting values, not measured real-skater statistics.

## Trick skill and success

| Trick | Difficulty `D` | Initial player mastery `M` | Success gain | Failure gain |
|---|---:|---:|---:|---:|
| Ollie | 15 | 75 | +2.0 | +0.65 |
| Kickflip | 48 | 45 | +2.0 | +0.65 |
| Tre Flip | 65 | 38 | +2.0 | +0.65 |

Input quality is `Q ∈ [0,1]`. For `Q < 0.27`, landing probability is 0. Otherwise:

```text
z = -0.55 + 0.045 × (clamp(M, 0, 100) - D) + 2.5 × (clamp(Q, 0, 1) - 0.7)
P(land) = clamp(1 / (1 + exp(-z)), 0.01, 0.985)
```

The HUD's example display uses quality `0.86`. Practice and Battle use the same player mastery. Battle reset clears match state only. Invalid saved mastery (`NaN`, infinity, below 0, or above 100) is repaired to a safe value on load.

## Bot

| Trick | Bot mastery | Bot input quality |
|---|---:|---:|
| Ollie | 90 | 0.82 |
| Kickflip | 64 | 0.82 |
| Tre Flip | 51 | 0.82 |

The bot picks Ollie with 17% probability, Kickflip with 46%, and Tre Flip with 37%. It uses the same success function as the player, with those fixed mastery values. It has no hidden win adjustment.

## Limits

- Production success draws use Unity's global `Random`; the formula itself is deterministic, but a per-attempt injectable RNG is not implemented.
- Runtime smoke scenarios used `Random.InitState` only in the temporary test harness to drive bot outcomes. No seed setting appears in the game UI.
- The reported HUD probability is model output. A 10-attempt smoke test is not a statistical calibration study; no 10,000-trial calibration run was performed.
- Failure growth is currently a fixed +0.65 per resolved bail; no fatigue, surface, speed, or stance modifier is included.
