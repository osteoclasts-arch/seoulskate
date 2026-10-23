# SKATER SEOUL — Realism Visual Pipeline PRD

**Status:** Research complete; implementation in progress; visual approval is pending.  
**Updated:** 2026-10-23 (KST)  
**Scope:** One polished, playable Seoul/Hangang skateboarding vertical slice in the existing Unity project.  
**Art target:** Grounded, high-realism skate visuals inspired by the public look of *skate.* / “Skate 4,” while retaining the project’s Seoul identity and mobile touch HUD. The target is a visual direction, not a claim of parity with an AAA production or access to EA’s internal technology.

> Reference images are visual references only. They communicate low, sunset-lit camera placement, readable airborne rider/board separation, board visibility and a layered plaza, skyline and river. They do not provide technical instructions or measurements.

## 1. Executive decision

The current image is not a “needs more Blender detail” problem. It is a pipeline problem with several visible causes at once: the hero scene reads as a flat illustrated panorama behind a narrow play strip; the rider and board have weak material response and small screen presence; the foreground obstacles lack surface breakup and edge treatment; and the output is currently an orthographic, non-HDR camera. The scene builder also replaces imported board materials with a small set of generic colors. Increasing mesh detail before fixing these inputs would spend time on detail the runtime image cannot show.

Use the existing **URP** project as the playable baseline. The repository already has Android/iOS and PC quality profiles; Unity documents iOS and Android as supported in URP and not supported in HDRP. Do not migrate the main project to HDRP merely to make the screenshot prettier. Keep HDRP as an optional, separately measured desktop experiment only if the product target is later changed to desktop/console only. The current URP project can gain substantial realism from authored geometry, correctly imported PBR maps, scene lighting, camera composition, static baked lighting, probes and post-processing without changing pipelines. [Unity render-pipeline feature comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines-feature-comparison.html)

The fastest believable route is a **single hero slice**, not a whole city: one high-quality skater, one dimensionally credible board, one skateable Hangang plaza lane with a few detailed obstacles, one art-directed sunset lighting setup, and four clearly coordinated tricks. Preserve the existing gameplay outcome/physics owner while replacing the visible motion source where procedural poses are inadequate. Use the device profiles only after measurement; do not infer performance from the editor or a desktop CLI capture.

The scope boundary is honest: the present procedural Blender scripts are useful for dimensioned hard-surface board parts, modular props, collision proxies and repeatable exports. They are not a realistic human sculpting, clothing simulation, or production mocap system. The rider is the highest-risk asset and will need either a properly licensed, suitably realistic rigged hero model plus authored clothing/retarget cleanup, or substantial artist-led sculpt/retopo/texture/rig work. A pile of procedural spheres and capsules cannot reach the requested target.

## 2. Evidence and current-project audit

### 2.1 Observed baseline

R63 remains archived as the historical comparison at [`captures/r63/01_runtime_ride.png`](./captures/r63/01_runtime_ride.png). R64 is the untouched visual baseline at [`captures/r64/01_runtime_ride.png`](./captures/r64/01_runtime_ride.png). The corrected R64 frame includes the regenerated `SeoulBoard.fbx`. R65 was the first implementation packet: [`captures/r65/01_runtime_ride.png`](./captures/r65/01_runtime_ride.png) plus the board-only [`captures/r65/board/04_board_3q.png`](./captures/r65/board/04_board_3q.png). R66 adds a same-build board underside image at [`captures/r66/board/05_board_underside.png`](./captures/r66/board/05_board_underside.png), making the Hangang print inspectable alongside the 3/4 view and ride frame. Both CLI packets log active settings and every board material assignment. R66's visual-slice static validation passes; unknown board material names fail the scene build. The ride frame is visually almost unchanged from R64/R65, as intended for the isolated board-material gate. The full game remains prototype quality: the rider is stylized, the foreground is flat, and the HUD is unfinished.

The earlier design review scored R63: rider 4/10, board 5.5/10, location/panorama 7/10, foreground/lighting 3.5/10, HUD 4.5/10, overall about 5/10. The R64 review scored overall art 5.2/10 and returned **Visual FAIL**: rider, board, foreground and camera/light fail; Hangang background partially passes; HUD partially passes. The reviewer estimated the rider+board span at about 250px high and reported only 1.83% of R63/R64 pixels changed, concentrated around rider/board while the HUD and sky were identical. These are review observations, not objective product metrics. Their decisive recommendation was one isolated board Material Integrity slice before any camera, lighting, rider or environment changes. The ChatGPT design reviewer returned an explicit **R66 P0 PASS** for material integrity. Their image review confirmed the grip/maple edge split, underside Hangang print, trucks, and wheels; they kept P1 board finish and overall-game approval open. Their first packet did not include `validation.log`; the Unity static-validation log is now attached in the follow-up for evidence completeness.

### 2.2 Verified implementation facts

| Area | Verified fact | Consequence |
|---|---|---|
| Engine/pipeline | Unity `6000.3.10f1`, URP package `17.3.0` | Keep the existing URP authoring pipeline while Android/iOS remain in scope. |
| Quality presets | `Mobile` and `PC` quality profiles exist. Mobile URP asset uses render scale `0.8`, one shadow cascade and no soft shadows; PC uses render scale `1`, four cascades and soft shadows. Both support HDR at the pipeline-asset level. R65 capture log records `StandaloneOSX`, `PC`, `PC_RPAsset`, AA 2, shadow distance 48, Orthographic camera and camera HDR disabled. | R64/R65 comparisons are desktop evidence, not mobile performance evidence. The new log makes the chosen R65 capture configuration reproducible. |
| Camera | `SkaterSeoulSceneBuilder.CreateCamera()` creates an orthographic camera, sets `allowHDR=false`, and uses a solid-color clear plus an unlit panorama quad. | Compare a controlled low-FOV perspective camera against the orthographic camera; retain the winner based on trick readability and gameplay framing, not preference alone. Enable scene HDR only when the selected volume/color pipeline is verified. |
| Material override paths | `CreateBoardVisuals()` now maps source-name categories to specific generated board materials, throws on an unknown slot instead of silently assigning maple, and logs renderer/source/output/shader/base-map. It covers print, grip, wheel, bearing, bushing, hardware and maple/deck. The board print is `URP/Lit`. `ApplySkaterMaterials()` still assigns flat hoodie/denim/shoe colors by renderer/material name. `CreateBlenderEnvironment()` remaps environment materials to flat generated colors. The plaza floor has one albedo image and no normal or roughness/AO map. | The first implementation is a board-only Material Integrity gate. Inspect runtime logs and same-build captures before adding texture maps, changing the rider, adjusting camera/lighting, or modifying geometry. |
| Board source material data | `generate_skateboard.py` exports named Principled materials: `Seoul_Board_Maple`, `Hangang_Board_Print`, `Seoul_Board_Grip`, `Seoul_Board_Hardware`, `Seoul_Board_Wheel`, `Seoul_Board_Bearing`, and `Seoul_Board_Bushing`. It authors solid base colors with scalar roughness/metallic values, not normal/AO/roughness image maps. | Preserve each part's intended material category, but do not claim the FBX contains image-based PBR detail. Future textures require separate authoring and a separate gate. |
| Wheel calibration | Blender source sets `WHEEL_RADIUS = 0.027` (54mm diameter), while `SkaterSeoulSceneBuilder` sets runtime `SkaterSeoulBoardRig.wheelRadius = 0.039`. R66 validation now confirms the configured 39mm gameplay radius, not equality with the 27mm visual mesh radius. | Treat the 12mm radius difference as an unresolved board contact/rotation calibration risk. Do not change it in P0; after the visual gate, compare actual imported wheel bounds and wheel-ground contact before deciding whether physics or mesh is wrong. |
| Generated assets | Blender scripts generate the skater, board and riverside environment FBXs under `UnityProject/Assets/SkaterSeoul/Art/Blender/`. | Keep script sources as rebuildable truth; runtime exports, Unity import settings and actual material assignments must be checked together. |
| Animation | Runtime trick poses use C# plus Humanoid Animator IK. R63 actual skinned-mesh measurement reports kickflip flick shoe center at `d_nose=-0.025m`, `d_toe=+0.007m`; current target is `+0.037m`, `+0.044m`. | The measured shoe does not clear the empirical flick gate (`d_nose≥+0.05m`, `d_toe≥+0.08m`) and the frame does not read as a completed outward flick. IK target arrival alone is not an animation pass. |
| CLI capture | Batch `StartCapture()` now rebuilds the saved visual slice from current Blender exports before entering Play Mode, then logs active build target, quality preset, render pipeline, AA, shadow distance, camera projection and camera HDR. | This prevents future screenshots from silently using a stale saved scene and makes R64/R65 capture conditions inspectable. |
| Device performance | Project settings indicate Android, iOS and desktop profiles, but no exact supported phone/model, screen resolution, graphics API, or target frame rate is documented. | Performance acceptance remains provisional until an actual reference device is named and profiled. Do not prescribe universal triangle/texture budgets. |

Relevant files: [`SkaterSeoulSceneBuilder.cs`](../../UnityProject/Assets/SkaterSeoul/Editor/SkaterSeoulSceneBuilder.cs), [`SkaterSeoulPlayModeValidation.cs`](../../UnityProject/Assets/SkaterSeoul/Editor/SkaterSeoulPlayModeValidation.cs), [`Mobile_RPAsset.asset`](../../UnityProject/Assets/Settings/Mobile_RPAsset.asset), [`PC_RPAsset.asset`](../../UnityProject/Assets/Settings/PC_RPAsset.asset), [`generate_skateboard.py`](../../UnityProject/Tools/generate_skateboard.py), [`generate_skater_character.py`](../../UnityProject/Tools/generate_skater_character.py), [`generate_hangang_environment.py`](../../UnityProject/Tools/generate_hangang_environment.py).

## 3. Research findings and limits

### Facts supported by primary sources

- EA publicly says *skate.* is powered by Frostbite and describes it as an evolution of the franchise. EA has publicly discussed the return/improvement of Flick-It, expanded animation, and game systems blending together. This supports using *skate.* as an observable quality reference for connected rider/board motion and a living city. It does **not** reveal EA’s shaders, texture resolution, budgets, source code, capture pipeline, rig or proprietary production process. [EA FAQ](https://www.ea.com/games/skate/skate/faq), [EA “Get Control!”](https://www.ea.com/games/skate/skate/news/get-control)
- Unity’s feature matrix lists mobile support for URP and not HDRP. URP includes HDR rendering, baked GI, probes and post-processing. Unity’s URP post-processing guidance warns that effects can consume significant mobile frame time; enable only effects that improve the actual play image. [Unity pipeline comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines-feature-comparison.html), [URP post-processing](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/add-post-processing.html)
- Unity’s lighting workflow supports baked/mixed lighting, Reflection Probes and Light Probes. Bake static plaza illumination where lighting is fixed, use probes for the moving rider, and reserve real-time direct light/shadow for the key light and gameplay-relevant contact. [Unity lighting workflow](https://docs.unity3d.com/jp/current/Manual/lighting-configuration-workflow.html), [URP Reflection Probes](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lighting/reflection-probes.html)
- Blender documents UV-backed texture baking, normal/roughness/AO outputs and padding around UV islands to avoid mip/seam artifacts. Adobe’s mesh-map baking guidance likewise calls for inspecting cage intersections and the bake log. These steps are required for high-detail source meshes to transfer cleanly to a lighter runtime mesh. [Blender Cycles baking](https://docs.blender.org/manual/en/latest/render/cycles/baking.html), [Adobe mesh-map baking](https://experienceleague.adobe.com/en/docs/substance-3d-painter/using/baking/how-to-bake-mesh-maps)
- A small biomechanical study measured experienced skaters performing Ollies, and a recent review finds a limited and uneven research base across skateboarding tricks. Treat the published work as support for observing pop/board/rider sequence, not as a validated universal clock for a game Kickflip or Tre Flip. [Fredrick et al., Ollie kinematics](https://pubmed.ncbi.nlm.nih.gov/16760565/), [2024 scoping review](https://link.springer.com/article/10.1007/s40279-024-02032-1)
- Reference captures from official skateboarding competition footage can guide body/board shape and timing; any used footage, model, texture scan, font, logo or sound must be licensed for this game and recorded in the asset ledger. [World Skate video library](https://www.skateboarding.worldskate.org/videos.html), [World Skate regulations](https://www.worldskate.org/skateboarding/about/regulations.html)
- Real-world manufacturer dimensions are available for a useful reference setup: Santa Cruz 8.25 × 31.8 in deck with 14.1875 in wheelbase; Independent 144 truck at 8.25 in axle width; Spitfire Formula Four 54 mm wheel at 31 mm overall width. Use these as modelling calibration, not a requirement to reproduce any brand marks. [Santa Cruz deck specification](https://nhsskatedirect.com/products/8-25in-x-31-8in-classic-dot-santa-cruz-skateboard-deck), [Independent truck sizing](https://independenttrucks.com/trucks), [Spitfire wheel sizing](https://www.spitfirewheels.com/formulafour/)

### Decisions based on those facts

1. **URP stays the one source of truth.** Improve materials, geometry, lighting, camera, and post-processing first. Keep a separate High desktop preset for captures; keep Mobile presets visually coherent but remove avoidable quality loss (the current `0.8` render scale is an explicit A/B item). A later HDRP comparison requires a written platform change and a separate build, because it drops native Android/iOS support.
2. **Unlit illustration is only for far distance.** Keep a distant skyline panorama if it preserves Seoul readability and saves time. Replace the hero lane and near/midground with real 3D parallax, textured/roughness-varied materials, real contact shadows and skateable geometry. The horizon, river, bridge, foreground and rider must share a consistent horizon and light direction.
3. **Spend detail where the camera sees it.** Prioritize rider face/head/hair silhouette, hoodie folds/cuffs/zip seam, denim knee/ankle breakup, footwear sole and laces, and board side profile/truck/wheel silhouette. Use higher detail for the hero and board; LOD or lower-resolution material variants for distant props.
4. **Author animation performance, solve contact procedurally.** A hand-authored or licensed motion clip supplies the whole-body performance. Existing IK remains for grounded feet, board contact, stance and small device/camera corrections. Do not use IK to invent the primary flick arc or to hide bad retargeting.
5. **A captured image is not proof of continuous gameplay.** Use both fixed CLI keyframes and a continuous sequence; report static pose gates and motion/gameplay gates separately.
6. **PBR texture candidates exist, but are not part of P0.** Poly Haven's `concrete_pavement` (warm-light pavers) and `denim_fabric_05` (fabric grain) are CC0 candidates for a later controlled texture pass. Do not infer that downloaded maps are already used by this build. [Concrete Pavement](https://polyhaven.com/a/concrete_pavement), [Denim Fabric 05](https://polyhaven.com/a/denim_fabric_05), [Poly Haven license](https://polyhaven.com/license).

## 4. Product and art requirements

### 4.1 Intended player experience

Within the first seconds, the player should read a believable East Asian young-adult street skater in a dark zip hoodie, relaxed denim, and real skate shoes, standing on a recognizable, physically dimensioned board. During a trick, the body and board should form one readable action while the board has its own credible motion. The location should read as a Hangang riverside skate spot through water, bridge, Seoul signage and skyline layers, with a usable plaza in the foreground. The HUD should feel like a considered mobile skate game, not development telemetry.

### 4.2 Character asset

**Must have:** adult human proportions; readable East Asian face at the play camera; separated hair/head/neck silhouette; broad but natural hoodie volume; a visible zip seam, cuffs, hem and hood that sits on the back; relaxed denim with distinct left/right legs and believable knee/ankle folds; skater shoes with separate upper, sole, heel, laces and outsole. Use material roughness/normal detail to distinguish cloth, denim, skin, rubber and metal under the same sunset.

**Production path:** inspect the current rigged model and weights in Blender first. If its face/body topology cannot support this target, replace the hero mesh with either an artist-quality licensed rigged adult human, or an artist-led high-poly sculpt → retopology → UV → normal/AO bake → texture → existing Humanoid retarget workflow. Do not claim a procedurally generated capsule body is realistic. Preserve a stable gameplay skeleton or add an explicit bone retarget map; keep clothing/face rigging and hand/foot bones validated in Unity. Runtime cloth simulation is optional and off by default; sculpt/baked cloth folds and lightweight secondary motion are the default mobile path.

**License gate:** do not download, purchase or incorporate a model until source, license, attribution, redistribution rights and platform use are recorded in `docs/THIRD_PARTY_ASSETS.md`. No unlicensed rip from *Skate City* or *skate.*.

### 4.3 Skateboard

Build one board with separate deck, grip, graphic, truck baseplate, kingpin/nut, hanger, axle, bushings, bearings and wheels. Use a hierarchy that permits deck pop/flip/yaw independently from the rider, with wheel pivots retained for rolling. Keep physically plausible clearances and the reference dimensions below:

| Part | Calibration value |
|---|---:|
| Deck length × width | 0.808 × 0.210 m |
| Wheelbase | 0.360 m |
| Truck axle width | 0.210 m |
| Wheel diameter × width | 0.054 × 0.031 m |

The current Blender script already contains a first dimensional shell pass (8–10 mm deck side, approximately 6 mm concave, 34 mm kicks). Preserve it only if the runtime silhouette confirms it. Use correct smoothing/hard edges and a small, consistent edge radius; do not use texture alone to imply deck thickness. Preserve the distinct board-art graphic on the underside and dark grip on top. These dimensions are calibrated from public manufacturer products; branded shapes, trademarks or graphics are not required.

### 4.4 Hangang environment and lighting

Deliver one 15–25 m camera-visible skateable plaza lane, with a 3D hero foreground, a riverside midground, and a skyline/bridge background. The camera must be able to move enough to create parallax; the background may remain a panorama outside the gameplay area. Include 2–3 obstacles only: a detailed curb/ledge, a rail with consistent diameters and welded/bolted joints, and a bench or stair/planter. Each object has a deliberate silhouette, bevels, a PBR material, wear at contact edges, and collision that agrees with the render mesh. Add Korean signage in original, legible text. Avoid decorative assets that block the rider or the next line.

Lighting: one low-angle, warm directional sunset key; calibrated neutral environment fill; baked indirect light for static plaza objects; light probes for the moving skater; limited reflection probes for the board/metal; one consistent fog/haze distance. Keep cast shadows on the rider/board/obstacles and soft contact. Tune the Rider/board exposure against the actual sky so they are not silhouetted beyond readability. Use a small-volume post stack (color grade/tonemapping first; bloom only if highlights demand it; avoid heavy motion blur, chromatic aberration, lens distortion and fake depth-of-field during play).

### 4.5 Camera and HUD

Compare the current orthographic follow camera with a low-angle, narrow-FOV perspective camera. Lock follow lag, lead and vertical response for both tests; judge world depth against board geometry, foot readability, obstacle visibility and screen occupancy. Avoid cinematic lens tricks that obscure the board or cause mobile nausea. Retain a replay/capture camera only as a separate optional view, not as the gameplay camera.

Keep mobile touch controls, but redesign their visual and information hierarchy: small battle state at the top, current trick/feedback in a single compact center/top card, transparent thumb control zones at lower corners, pause/retry in the safe area. Use production typography/icons and spacing; remove developer values and large opaque blocks. Test 16:9 and tall-phone landscape safe areas with Korean and English strings. Visual touch target and actual hit area are separate acceptance items.

## 5. Animation and board/body coordination

Keep gameplay results, score and failure ownership in the existing gameplay controller. Replace/upgrade the visual source independently, then connect it through a small event contract (attempt, phase, board pose, contact state, success/failure). Existing animation code may continue to drive grounded foot IK and stance; authored clip/root pose drives the main body performance. The board has an independent visual rig or transform hierarchy and must not inherit a full-body yaw unintentionally.

| Trick | Required readable beats (reference/author the full arc; no universal timing claim) | Objective image/motion checks |
|---|---|---|
| Push | Roll/weight transfer → support foot stays planted on board → push foot leaves deck → ground contact → propulsion/leg extension → toe-off → return and settle into riding stance. | Contact foot stays on ground through the propulsion interval; planted foot stays on deck; push foot visibly clears deck and returns; rider moves forward with board rather than sliding in place. |
| Ollie | Compression → tail pop → nose rises → lead foot slides/levels deck → peak height → descent → both feet recover board → landing compression/recovery. | Tail strike and nose pitch read independently; rider and board rise coherently; feet do not pass through deck; all four wheels return to ground without a frozen straight-leg landing. |
| Kickflip | Compression/pop → front foot flicks outward beyond toe-side/front edge → board rotates one longitudinal flip → body stays balanced above it → board completes rotation → catch → landing compression. | At the chosen three-quarter review frame, actual shoe mesh center passes `d_nose≥0.05m` and `d_toe≥0.08m` with visible gap; board rotation is visibly distinct; catch returns both feet to deck before impact. These are current project review gates, not published biomechanics limits. |
| Tre Flip | Compression/pop → combined board flip and shove-it rotation → feet free of the deck during rotation → board reaches its catch orientation under the rider → catch and compressed landing. | Board’s flip and yaw are measured separately in its local axes; body yaw remains independently controlled; no 360° body spin unless explicitly authored as a separate trick. Review front/side and three-quarter frames at rotation landmarks. |

For Ollie and other complex tricks, build reference pose boards from lawful real-footage capture or licensed motion capture. Author/retarget movement in Blender, preserve root scale/orientation, and export the rig/mesh/animation with Unity-compatible units/axes. Inspect every clip at the actual game camera and a diagnostic side camera. Pin a versioned pose sheet with approach, compression, pop, flick/air, catch, impact and recovery; do not compare screenshots from different builds or different timing samples as one sequence. Use actual post-Animator skinned-mesh measurements, not only IK target coordinates or pre-skin renderer bounds. Do not invent research-backed exact Kickflip/Tre Flip timing.

## 6. Material, bake and runtime content contract

Each imported material name must remain stable across Blender export and Unity import. The scene builder may point those names to reusable Unity materials but must not collapse unknown parts to maple. Require these map semantics:

| Map | Import meaning |
|---|---|
| Base Color / Albedo | sRGB color; no baked lighting or AO in the color map. |
| Normal | Linear tangent-space normal; check green-channel convention and Unity import type. |
| Metallic + Smoothness | Metallic only on real metal; roughness/smoothness uses one documented channel convention. |
| Ambient Occlusion | Linear mask; subtle local contact/material cavity only, not a fake full shadow. |
| Detail | Optional tiling fabric/concrete grain or decals; it must survive at camera distance without obvious repetition. |

Use a detailed source mesh and a camera/target-appropriate runtime mesh where needed. UV-check the runtime mesh, bake normal/AO with a tuned cage, inspect seams and skew, and extend bake margins for filtering/mipmaps. Export non-destructive editable Blender sources and document the FBX axis/unit/export preset. Every material must be previewed in the actual Unity URP scene, under the final key light and color management; Blender viewport screenshots are not acceptance evidence.

## 7. Staged implementation plan and deliverables

| Stage | Work | Deliverable / gate |
|---|---|---|
| **P0 — Board Material Integrity (complete)** | Freeze R64; explicitly map all FBX board slots, fail unknown names, log the runtime shader/texture assignments, and hold geometry, rider, camera, lighting and gameplay fixed. | R66 ride + underside + board-only 3/4 captures; 49 slots, seven source categories, unknown mappings zero; static visual validation and ChatGPT design gate pass. |
| **P1 — Board visual quality (in review)** | Address the reviewer-visible dark/undersized underside print, broad pale border and uniform maple edge. Keep board dimensions, pivots, camera, rider, scene lighting and gameplay fixed. | R67 baseline + R68 same-build ride, underside and board 3/4; compare print coverage/readability and grip/wood/metal/urethane separation; no collision, scale, wheel-pivot or unintended UV distortion. P1 remains unapproved until all three images pass. |
| **P2 — Render foundation** | Separately verify the camera HDR path and post stack; A/B orthographic against narrow-FOV perspective; address mobile render scale only as a measured A/B. | Same scene, camera framing and light for each experiment; report the active quality/render path and device performance. |
| **P3 — Hero rider** | Audit current skeleton and asset license; improve the existing head, hoodie, denim, hands and footwear without undoing the approved pelvis silhouette; retarget and validate weights. | Ride silhouette plus rider 3/4 and tuck captures; no unexplained geometry protrusions, knee collapse, foot penetration or bone pops. |
| **P4 — Environment slice** | Improve the gameplay lane and 2–3 near props with detailed skateable 3D geometry and materials; retain distant Seoul skyline image/meshes only where they add depth. | Low gameplay camera image reads as one riverside place; obstacle render/collision align; player remains unobscured at approach and trick peak. |
| **P5 — Motion production** | Create/retarget push, Ollie, Kickflip and Tre Flip primary clips; keep limited IK for contact and board catch; measure actual mesh centers and board rotations. | One continuous CLI recording per trick plus same-build phase stills; visual and gameplay judgments reported separately. |
| **P6 — HUD & device profiles** | Replace prototype HUD styling, verify safe area, contrast and touch hit zones; tune mobile and PC quality levels. | Gameplay HUD image plus HUD-off art review; phone interaction not marked passed until tested on device. |
| **P7 — Review loop** | Send each current-build art evidence to the existing design review and motion evidence to the existing gameplay review. Apply highest-impact actionable feedback and repeat. | Record reviewer verdict, unresolved gaps and exact build/capture identifier. **No automatic approval based on code compilation.** |

### First execution slice after PRD review

The R64 review approved one first implementation slice: in `CreateBoardVisuals()`, explicitly map the existing FBX material names to maple, print, grip, truck, urethane wheel, bearing and bushing materials; fail on any unknown slot; keep the current board mesh/UV/scale, rider, environment, camera, light and gameplay unchanged. Emit a compact source-to-output material log. R65 captured ride and board 3/4 views; R66 adds the missing board underside image. The R66 Unity CLI capture and `SkaterSeoulValidation.ValidateVisualSlice()` pass, including checks for four mapped bushings, the underside artwork, and the lit board-print shader. The R66 design reviewer explicitly marked P0 PASS. Their first packet did not include the separate static-validation log; that log was attached in the follow-up. R67 changed only the Kickflip front-foot position; R68 is the separately tagged constrained board-surfacing pass. Neither closes full-game visual approval. Camera/HDR, rider, environment, HUD and trick-animation work stay on separate gates.

## 8. Acceptance gates

These are project review gates, not claims about industry standard or Skate. internals.

### Visual gate — design reviewer

1. Same-build 1280×720 CLI captures: neutral ride, push contact, Ollie peak/landing, Kickflip flick/catch, Tre Flip rotation/catch, HUD-on gameplay, and HUD-off art. Keep camera and exposure fixed between versions; attach the capture ID and branch/commit with each packet.
2. Rider is clearly human with requested hoodie + denim + skate-shoe outfit and readable pose at native size; head/face silhouette, left/right leg separation and clothes are visible in one standard ride and one trick frame.
3. Board has a convincing side profile, readable truck/wheel assembly, believable surfaces and graphic; deck/grip and wheel contacts remain visible under rotation. Model scale matches the table above within modelling tolerance.
4. Foreground materials distinguish concrete, cloth/denim, rubber and metal by both surface response and detail. Lighting direction is shared by rider/board/props; near environment is spatial 3D and casts/receives useful contact shadow.
5. HUD is legible, deliberately styled and does not hide the board/feet/obstacle; no debug text in player build.
6. Design review may still mark the goal NOT APPROVED. “Skate. target parity” is not accepted as a measurable pass/fail criterion; specific visual gates must be agreed and recorded.

### First-slice gate — board material integrity

1. The exact current Blender source slots are explicitly mapped: `Seoul_Board_Maple`, `Hangang_Board_Print`, `Seoul_Board_Grip`, `Seoul_Board_Hardware`, `Seoul_Board_Wheel`, `Seoul_Board_Bearing`, and `Seoul_Board_Bushing`.
2. Unknown or newly introduced material names stop the scene build; they are never silently rendered as maple.
3. R66 includes same-camera ride, underside and board-only three-quarter captures. `SKATER BOARD MATERIAL` logs show source name, assigned material, shader and base map; CLI settings log records build target, active quality, pipeline, AA, shadow distance and camera projection/HDR.
4. The board mesh, UVs, dimensions, wheel pivots, rider, camera, lighting, collider and gameplay code remain identical to R64.
5. R66 Unity CLI capture and `SkaterSeoulValidation.ValidateVisualSlice()` pass; its material log covers 49 renderer slots with zero unknown mappings. The validation assertions use current generated asset names and the scene's configured wheel-contact radius.
6. The ChatGPT design reviewer marked R66 **P0 PASS** after inspecting the same-build ride, underside and board-only 3/4 images plus the 49-slot assignment evidence. Their review separates that material-integrity result from P1 board finish and keeps full-game visual approval open. Their first packet did not include the separate static-validation log; it is now attached for evidence completeness.

### Motion gate — gameplay reviewer

1. Push contact, Ollie pop/level/landing, Kickflip actual-mesh flick and catch, Tre Flip flip+yaw+catch are reviewed as separate gates.
2. The game’s mechanical outcome, captured board/body state and on-screen result agree on every attempt; a visually successful land cannot be called a fail and vice versa.
3. No foot-deck penetration, foot sliding during a held contact, board rotation discontinuity, catch snapping, one-frame frozen pose, or impossible landing compression in the continuous CLI sequence.
4. Kickflip flick gate requires the actual skinned shoe geometry to meet the project distances in §5 and a visible gap in the same capture. IK target satisfaction alone is fail.
5. Complex trick pose timing is tuned from licensed/reference footage and reviewer observation; only Ollie-specific measured research is labeled as such.

R67 gameplay review: actual BakeMesh shoe-center clearances at Kickflip phase 0.35 measured +0.068 m from the nose bolts and +0.099 m from the toe-side edge, with 0.000 m IK target error. This passes the numeric gate, but the separate visual reviewer verdict is **FAIL**: the shoe remains too parallel to the deck and reads more like a lifted front foot than a toe-side flick. Preserve the measured position change; the next animation iteration should tune the front-foot/ankle orientation rather than keep extending the leg. R67 evidence is archived under `captures/r67-motion/`.

### Performance and stability gate

- Record CPU/GPU frame time, frame rate, render scale, resolution, shadows, material/shader variants, texture memory and device/OS for each preset. Use Unity Profiler and an actual representative phone. Never infer mobile performance from the desktop CLI image.
- Keep three measured presets: `Mobile Low`, `Mobile High`, and `Desktop High`. Initial quality choices are candidates; select render scale, shadows, MSAA, reflection and post effects from device results. A mobile shipping frame-rate target and named reference handset are **open product decisions**, so do not report this gate as approved before they are agreed and measured.
- Preserve current gameplay/physics and saved progression. Art asset and camera changes must not change collision dimensions, trick outcomes or touch zones without explicit new evidence.

## 9. Risks, non-goals and stop conditions

| Risk | Mitigation / decision |
|---|---|
| The game has no named target phone or frame-rate requirement. | Produce scalable URP tiers; capture current desktop truth now; keep mobile performance “NOT MEASURED” until a real phone is profiled. |
| Current model base may be too stylized/low-resolution to refine into realism economically. | Audit geometry and license before adding detail. If the face/body/clothing proportions fail the hero gate, replace the base or commission an artist rather than writing more procedural patch code. |
| Orthographic-to-perspective camera change may reduce gameplay readability or complicate board/obstacle framing. | Use a controlled A/B and keep the existing camera available until motion and obstacle gates pass. |
| More resolution/shadows/post effects may break mobile battery/thermal budget. | Tune each profile independently; benchmark the actual lower supported phone, not only editor. |
| Imported PBR work is lost by runtime remapping or an incorrect smoothness/normal convention. | Keep a material-name manifest; inspect the model and Unity frame after import; use a neutral calibration sphere/board under the final lighting. |
| Current procedural IK cannot supply polished human motion or cloth deformation. | Keep IK for contact only; author or license performance clips. Do not “solve” anatomy with more offset constants. |
| License ambiguity for reference or purchased assets. | Do not import until the asset ledger records provider, license, attribution and permitted platforms. |

**Non-goals for this slice:** recreate EA’s proprietary Frostbite implementation; clone *Skate City*/*skate.* characters, logo, city or map; build a full open world; add online multiplayer; implement simulated cloth/hair or physically simulated four-wheel suspension; purchase assets; represent a particular real Hangang park as survey-accurate; claim native mobile device performance without a phone run.

## 10. References

1. EA, [skate. FAQ](https://www.ea.com/games/skate/skate/faq) — public platform/engine statements.
2. EA, [Get Control!](https://www.ea.com/games/skate/skate/news/get-control) — public Flick-It, animation and system-blending statements.
3. Unity, [Render pipeline feature comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines-feature-comparison.html).
4. Unity, [Add post-processing in URP](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/add-post-processing.html).
5. Unity, [Lighting configuration workflow](https://docs.unity3d.com/6000.0/Documentation/Manual/lighting-configuration-workflow.html) and [Reflections in URP](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lighting/reflection-probes.html).
6. Blender, [Cycles baking](https://docs.blender.org/manual/en/latest/render/cycles/baking.html), [Retopology](https://docs.blender.org/manual/en/latest/modeling/meshes/retopology.html), [Armature modifier](https://docs.blender.org/manual/en/latest/modeling/modifiers/deform/armature.html), and [FBX export](https://docs.blender.org/manual/en/latest/files/import_export/fbx.html).
7. Adobe, [Bake mesh maps in Substance 3D Painter](https://experienceleague.adobe.com/en/docs/substance-3d-painter/using/baking/how-to-bake-mesh-maps), [The PBR Guide, Part 2](https://www.adobe.com/learn/substance-3d-designer/web/the-pbr-guide-part-2).
8. Fredrick et al., [Kinetic and kinematic patterns of the Ollie](https://pubmed.ncbi.nlm.nih.gov/16760565/); 2024 [scoping review of skateboarding biomechanics](https://link.springer.com/article/10.1007/s40279-024-02032-1).
9. Santa Cruz, [8.25 × 31.8 in deck specification](https://nhsskatedirect.com/products/8-25in-x-31-8in-classic-dot-santa-cruz-skateboard-deck); Independent, [truck sizing](https://independenttrucks.com/trucks); Spitfire, [Formula Four sizing](https://www.spitfirewheels.com/formulafour/).
10. World Skate, [official video library](https://www.skateboarding.worldskate.org/videos.html), [rules and regulations](https://www.worldskate.org/skateboarding/about/regulations.html).
11. Poly Haven, [CC0 license](https://polyhaven.com/license), [Concrete Pavement](https://polyhaven.com/a/concrete_pavement), and [Denim Fabric 05](https://polyhaven.com/a/denim_fabric_05) — researched candidates for a later PBR pass; not yet integrated.

**Research boundary:** aside from official public statements and cited papers, recommendations in this document are engineering/art-direction judgments to verify in the running project. No third-party source establishes that this project can match *skate.* quality or hit mobile performance without asset, device, and profiler evidence.

## 11. Iteration log

### R68 — board surfacing, visual review pending

The design review selected a constrained art/UV/material pass before adding image-based PBR maps. It proposed approximately 95–98% print coverage across the straight underside, 2–5 mm side margins, 5–10 mm at the rounded nose/tail, and a 15–25% perceived lift to dark graphic midtones. These are project art-direction targets, not product specifications. It proposed a muted maple color near sRGB `#947255`, maple smoothness 0.25–0.40, and print smoothness 0.35–0.48. The implementation chose the middle of those ranges for the first A/B.

R68 changes only `Tools/generate_skateboard.py` print V coordinates (to fit the existing image silhouette while preserving deck vertices), the existing `seoul_deck_artwork.png` midtone treatment, and the maple/print material values in `SkaterSeoulSceneBuilder.CreateBoardVisuals()`. The R66 texture was preserved at `captures/r66/source/seoul_deck_artwork.png`; the R68 FBX, texture, generator and scene builder are archived in `captures/r68-board/source/`. The 0.808 × 0.210 m deck, topology, pivots, 49 source material slots, seven material categories, gameplay, camera, rider, environment, HUD and light were not intentionally changed. Blender CLI exported `SeoulBoard.fbx`; Unity CLI captured the three required 1280×720 images. Runtime capture has 29 QA PASS and zero QA FAIL; `ValidateVisualSlice()` independently passes. These checks establish asset/build integrity only. **R68 P1 visual approval is still pending** until the design review accepts ride, underside and board 3/4 images.

### R69 — Kickflip front-foot orientation, gameplay review pending

After the R67 numeric-pass/visual-fail verdict, `SkaterSeoulRiderMotion.ApplyFeetIk()` keeps R67's front-foot position offsets and adds a 24° toe-side ankle yaw to the Kickflip front foot only. The pulse uses the initial nose/toe-side axes and regular/goofy sign, peaks at normalized phase 0.35 and fades before catch; rear-foot rotation, board rotation, timing and physics remain fixed. An assert checks the pulse and axis sign. R69's 1280×720 three-quarter capture logs phase 0.35, IK error 0.000 m, front-foot bone yaw −34.8° and actual BakeMesh center clearances of +0.088 m nose and +0.077 m toe-side. The nose and separation criteria pass; toe-side clearance misses its +0.080 m threshold by 3 mm. Runtime capture has 29 QA PASS and zero QA FAIL; static visual-slice validation passes. The game reviewer has not yet judged R69's visible flick or resolved the small toe-side regression. Evidence is archived under `captures/r69-kickflip-yaw/`.

### R70 — Kickflip toe-side extension, gameplay review pending

Following the R69 FAIL verdict, only the Kickflip front-foot target receives an additional +0.030 m along the stored initial toe-side axis, pulsed with the existing 24° flick yaw. Nose offset, ankle yaw, board rotation, catch timing, physics and other tricks are unchanged. At normalized phase 0.35, the actual BakeMesh center measures +0.088 m nose and +0.107 m toe-side; IK target error is 0.000 m and front-leg reach is 0.791/0.822 m. This clears the reviewer's R70 numeric thresholds (+0.050 m nose, +0.100 m toe-side, ≤0.010 m IK error) with 29 runtime QA PASS, zero FAIL and static validation PASS. Visible flick readability remains for the gameplay reviewer to judge. Evidence is archived under `captures/r70-kickflip-lateral/`.

### Overall status

R66 P0 board material integrity: **PASS**. R67 Kickflip: **numeric PASS / visual FAIL**. R68 board surfacing: **design FAIL** (ride and 3/4 pass; underside print-to-deck correspondence fails). R69 Kickflip: **numeric FAIL / visual FAIL**. R70 Kickflip position: **numeric PASS / visual review pending**. Push contact, Ollie pose/landing, the full Kickflip flick-to-catch sequence, Tre Flip pose sequence, hero-rider finish, environment slice, HUD and phone performance remain unapproved or unmeasured. The game is **not approved as Skate City / skate. quality**.
