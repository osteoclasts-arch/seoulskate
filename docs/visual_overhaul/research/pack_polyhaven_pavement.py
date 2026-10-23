"""Pack Poly Haven's linear AO and roughness maps for Unity URP/Lit."""

from pathlib import Path

import bpy
import numpy as np


ROOT = Path(__file__).parent / "polyhaven_concrete_pavement"
AO_PATH = ROOT / "concrete_pavement_ao_2k.png"
ROUGHNESS_PATH = ROOT / "concrete_pavement_roughness_2k.png"
OUTPUT_PATH = ROOT / "concrete_pavement_mask_2k.png"


def load_linear(path: Path):
    image = bpy.data.images.load(str(path), check_existing=False)
    image.colorspace_settings.name = "Non-Color"
    pixels = np.empty(image.size[0] * image.size[1] * 4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    return image.size[:], pixels.reshape(image.size[1], image.size[0], 4)


ao_size, ao = load_linear(AO_PATH)
roughness_size, roughness = load_linear(ROUGHNESS_PATH)
assert ao_size == roughness_size == (2048, 2048), (ao_size, roughness_size)

mask = np.zeros((2048, 2048, 4), dtype=np.float32)
mask[:, :, 1] = ao[:, :, 0]  # URP/Lit Occlusion channel
mask[:, :, 3] = 1.0 - np.clip(roughness[:, :, 0], 0.0, 1.0)  # Metallic Alpha smoothness

packed = bpy.data.images.new("Concrete Pavement · AO + Smoothness", 2048, 2048, alpha=True)
packed.colorspace_settings.name = "Non-Color"
packed.file_format = "PNG"
packed.filepath_raw = str(OUTPUT_PATH)
packed.pixels.foreach_set(mask.reshape(-1))
packed.save()
assert OUTPUT_PATH.is_file() and OUTPUT_PATH.stat().st_size > 0
print(f"Packed 2048² AO(G) + smoothness(A) mask: {OUTPUT_PATH}")
