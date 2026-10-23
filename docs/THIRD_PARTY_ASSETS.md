# Third party assets

## Quaternius Ultimate Modular Men Pack

- **Author:** Quaternius (`quaternius.com`)
- **Source:** [Ultimate Modular Men Pack](https://quaternius.com/packs/ultimatemodularcharacters.html)
- **License:** CC0 1.0; the pack's `License.txt` is preserved beside the imported model.
- **Used in the scene:** `Casual_Hoodie.fbx` is the licensed base for `Art/Blender/SeoulSkater.fbx`. `Tools/generate_skater_character.py` derives the custom Seoul rider mesh, clothing, shoes and palette from that base. The scene builder prepares its Humanoid Avatar and retargets Kenney's animation clips. The Quaternius base is therefore part of the runtime asset lineage, while the customized Seoul FBX is what the scene loads.
- **Excluded from the Unity project:** the other ten characters and unused source formats.

## Kenney Animated Characters Protagonists 1.1

- **Author:** Kenney (`kenney.nl`)
- **Source:** [Animated Characters Protagonists](https://kenney.nl/assets/animated-characters-protagonists)
- **License:** Creative Commons Zero (CC0 1.0). The source license is preserved at [License.txt](../UnityProject/Assets/SkaterSeoul/ThirdParty/KenneyAnimatedProtagonists/License.txt).
- **Used in the scene:** `Animations/{idle,run,jump}.fbx`.
- **Use:** Humanoid idle, run, and jump clips, retargeted onto the customized Seoul rider derived from Quaternius. Board motion, foot contact, push, catch, landing, and bail poses are project code and geometry.
- **Excluded from the Unity project:** the alternate criminal/cyborg/female skin variants and source SVGs from Kenney's download. They are not needed by this scene.

The Kenney files are permissively licensed placeholders for this vertical slice. No Skate City models, textures, sounds, source code, or extracted game files are included.

## Original project art

- `Art/GeneratedSeoul/seoul_han_riverside_panorama.png` — generated for this project as a Seoul Han River skyline panorama.
- `Art/GeneratedSeoul/warm_concrete_albedo.png` — generated for this project as a warm concrete surface texture.
- `Art/GeneratedSeoul/seoul_deck_artwork.png` — original Hangang sunset screen-print graphic generated for the skateboard underside.
- `Tools/generate_skateboard.py` builds the original `Art/Blender/SeoulBoard.fbx`; `Tools/generate_hangang_environment.py` builds the original `Art/Blender/HangangRiversideSet.fbx`. `SkaterSeoulSceneBuilder` assembles those assets with project-generated plaza details, facade blocks, materials, lighting and gameplay objects.

The attached reference image was used only to guide lighting, framing, and skate-park atmosphere. Its depicted assets were not copied.

## Blender Human Base Meshes bundle — research only

- **Author:** Blender Foundation
- **Source:** [Human Base Meshes bundle v1.4.1](https://www.blender.org/download/demo/asset-bundles/human-base-meshes/human-base-meshes-bundle-v1.4.1.zip)
- **License:** CC0, per Blender's official asset-bundle page.
- **Local research copy:** `docs/visual_overhaul/research/human-base-meshes-bundle-v1.4.1/`.
- **Used in the game:** No. The bundle was inspected with Blender CLI only; no object was imported into Unity or the runtime scene.
- **Observed limits:** the realistic male body/head assets are unrigged, untextured, and have no skater clothing. This is a sculpting source, not a finished game character.

## Poly Haven Concrete Pavement

- **Author:** Poly Haven
- **Source:** [Concrete Pavement](https://polyhaven.com/a/concrete_pavement)
- **License:** CC0; see [Poly Haven's license](https://polyhaven.com/license).
- **Research source maps:** `docs/visual_overhaul/research/polyhaven_concrete_pavement/` contains the 2K Base Color, OpenGL tangent normal, roughness and AO source PNGs plus `pack_polyhaven_pavement.py` and its derived mask.
- **Used in the game:** Yes, as the Hangang plaza floor material in R73. Unity receives the Base Color, Normal, and a packed linear mask (AO in green; smoothness `1 − roughness` in alpha). No paid or Skate-branded content is included.
