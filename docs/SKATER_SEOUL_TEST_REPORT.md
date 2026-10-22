# SKATER SEOUL test report

**Date:** 2026-10-23  
**Unity:** 6000.3.10f1 (`e35f0c77bd8e`)  
**Host tools:** macOS, Xcode 26.3, iOS 26.2 SDK  
**Test project:** `/tmp/skater-seoul-unity-validation-20261023` (temporary Unity project; not part of the source pack)  
**Input System:** `com.unity.inputsystem` 1.14.2; Active Input Handling = Both  
**Render pipeline:** temporary validation project did not include URP. URP import/compile remains unverified.

## Results

| Check | Result | Evidence |
|---|---|---|
| Unity C# compile + Editor rule check | **PASS** | `/tmp/skater-seoul-controls-rules-final.log`; no C# compile errors; rule check PASS |
| Probability and gesture fixtures | **PASS** | 180 synthetic multi-point paths across 16:9, 19.5:9, and 20:9; three tricks per ratio; quality, minimum duration, curved-path, start-area, HUD-area and letter-bound checks |
| Play Mode touch-routing + physics/state smoke | **PASS** | `/tmp/skater-seoul-controls-play-smoke-final.log`; tap launches Ollie, simultaneous left flick/right push works, push release stops acceleration; 10/10 attempts resolved; three bail poses and retry checked |
| S.K.A.T.E. rule scenarios | **PASS** | Same Play Mode smoke log; temporary seeded assertions covered B01–B10, both players reaching five letters, wrong-trick defense, duplicate resolution, mode toggle, and battle reset preserving mastery |
| Scene generation preservation | **PASS** | Batch generation created `SkaterSeoul_MVP.unity` and then `_2.unity`; both remained in the temporary project, and the second generation did not overwrite the first |
| Unity iOS project generation | **PASS** | `/tmp/skater-seoul-controls-ios-build.log`; BuildResult Succeeded, 0 Unity build errors, 0 Unity build warnings |
| Xcode unsigned device-target compile | **PASS** | `/tmp/skater-seoul-xcodebuild-ios.log`; `** BUILD SUCCEEDED **`; output app at `/tmp/skater-seoul-unity-validation-20261023/Builds/DerivedData/Build/Products/Release-iphoneos/skater-seoul-unity-validation-20261023.app` |
| Xcode signed iPhone install and launch | **PASS** | Xcode installed the temporary app on the connected iPhone 15 Pro; `devicectl device info apps` listed the bundle, and `devicectl device info processes` showed its Unity process running |

The touch-routing smoke invokes `BeginPointer`, `MovePointer`, and `EndPointer` with simulated pointer IDs through reflection. It verifies the left pad, right push control, release behavior, and simultaneous fingers. It does **not** inject OS-level physical touchscreen events. The physics smoke also forces selected success/failure outcomes to check landing, recovery, visual separation, and battle resolution in Unity Play Mode.

## iOS build details

- The Unity batch build generated an Xcode project for one landscape scene. The build harness set the temporary project's orientation to landscape; this is not stored in the source pack.
- Xcode first compiled the device-target `Release-iphoneos` app without code signing. For the run check, automatic signing was enabled only in the disposable `/tmp` Xcode project and Xcode selected a managed profile for its temporary app identifier.
- The app process appeared on the iPhone, but Xcode then showed a wait dialog while LLDB resolved device symbols. The app launch is verified; debugger attachment and on-screen gameplay are not.
- Xcode emitted 10 warnings, all from generated Unity project files/build phases: deprecated Unity iOS APIs, an unused variable, the missing 1024×1024 App Store icon, and a build script with no declared outputs. There were no Xcode compile errors.
- Output size: Xcode project about 706 MB; compiled app about 53 MB.

## Not run

- Manual keyboard shortcuts or mouse swipe in the Unity Game view.
- Hands-on iPhone touch and safe-area/notch layout (the user will check these directly), controller/haptics, or five-minute play session.
- iOS Simulator launch; verification used the connected iPhone device target.
- URP project import/compile, 19.5:9/20:9 visual HUD review, FPS/memory profiling, or Android compatibility.
- Statistical 10,000-trial probability calibration, production seed injection, or audio checks.
- Full S.K.A.T.E. match played via normal user input. Transition cases were automated directly against runtime methods.
- Screenshot capture; none was saved.

## Console notes

- Final rules and Play Mode runs had no C# compiler errors or gameplay exceptions.
- One earlier Unity 6.3 Editor startup logged `SearchDatabase.EnumerateAll` `ArgumentOutOfRangeException` while initializing Search indexing. The final Play Mode run completed without that exception. It originated in the Editor Search indexer stack, not in `Assets/SkaterSeoul` code.
- The initial temporary Play Mode test helper had C# iterator compile errors; that test-only harness was corrected before the final compile and run. No such errors remain in the delivered source.

## Reproduction commands

```sh
/Users/osteoclasts_/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath /tmp/skater-seoul-unity-validation-20261023 \
  -executeMethod SkaterSeoul.Editor.SkaterSeoulValidation.RunBatch \
  -logFile /tmp/skater-seoul-controls-rules-final.log
```

The Play Mode runner and iOS build helper were temporary scripts under the `/tmp` project; they are intentionally not copied into the source pack. The source-pack validation menu remains **Tools > SKATER SEOUL > 2 - Validate Game Rules**.
