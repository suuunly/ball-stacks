# /// script
# dependencies = ["numpy", "scipy", "pillow"]
# ///
"""Remove white UV background from ball textures to stop mipmap seam bleeding.

Background is opaque pure white and connected to the image border; island
interiors may also contain white (football panels), so we flood from the
border instead of keying on color alone. The mask is grown a few pixels to
consume the anti-aliased fringe, then filled with the nearest island color.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

WHITE_THRESHOLD = 250   # channel value at/above which a pixel counts as background-white
FRINGE_GROW_PX = 3      # how far to push the background mask into the AA fringe

def background_mask(color_arr: np.ndarray) -> np.ndarray:
    whiteish = (color_arr[..., :3] >= WHITE_THRESHOLD).all(axis=-1)
    labels, _ = ndimage.label(whiteish)
    border_labels = np.unique(np.concatenate([
        labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]]))
    border_labels = border_labels[border_labels != 0]
    mask = np.isin(labels, border_labels)
    mask = ndimage.binary_dilation(mask, iterations=FRINGE_GROW_PX)
    return mask

def fill_with_nearest(path: Path, mask: np.ndarray) -> None:
    img = Image.open(path)
    mode = img.mode
    arr = np.array(img)
    if arr.shape[:2] != mask.shape:
        m = Image.fromarray(mask.astype(np.uint8) * 255).resize(
            (arr.shape[1], arr.shape[0]), Image.NEAREST)
        mask = np.array(m) > 127
    _, (iy, ix) = ndimage.distance_transform_edt(mask, return_indices=True)
    arr[mask] = arr[iy, ix][mask]
    Image.fromarray(arr, mode).save(path)

if __name__ == "__main__":
    root = Path(sys.argv[1])
    for folder in sorted(p for p in root.iterdir() if p.is_dir()):
        color = folder / "UV_Sphere-color.png"
        if not color.exists():
            print(f"skip {folder.name}: no color map")
            continue
        color_arr = np.array(Image.open(color).convert("RGB"))
        mask = background_mask(color_arr)
        pct = 100.0 * mask.mean()
        for png in sorted(folder.glob("*.png")):
            fill_with_nearest(png, mask)
        print(f"{folder.name}: filled {pct:.1f}% background on {len(list(folder.glob('*.png')))} maps")
