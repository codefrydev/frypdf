"""Shared brand constants and helpers for FryPDF's generated art.

Used by tools/icons/generate_icons.py and packaging/macos/dmg/make-background.py.
Palette is sampled from packaging/macos/AppIcon.iconset/icon_256x256.png and matches
the tokens in src/PdfEditorApp/Styles/Material3ExpressiveTokens.axaml.
"""

from __future__ import annotations

import pathlib

from PIL import Image, ImageFont

REPO_ROOT = pathlib.Path(__file__).resolve().parents[1]

MASTER_LOGO = REPO_ROOT / "src" / "PdfEditorApp" / "Assets" / "app-logo.png"
ICONSET_1024 = REPO_ROOT / "packaging" / "macos" / "AppIcon.iconset" / "icon_512x512@2x.png"

# Brand palette.
NAVY = (13, 43, 69)
NAVY_DEEP = (8, 28, 46)
RED = (238, 75, 50)
WHITE = (255, 255, 255)
MUTED = (159, 179, 200)

# The dragon disc inside the 1024px master. app-logo.svg declares
# <circle cx="512" cy="440" r="144">, so the badge lives in this box.
DRAGON_BOX = (360, 288, 664, 592)

# Named weights available on SFNS.ttf, with a Helvetica Neue fallback per weight.
_SFNS = "/System/Library/Fonts/SFNS.ttf"
_HELVETICA_NEUE = "/System/Library/Fonts/HelveticaNeue.ttc"
_HELVETICA_NEUE_INDEX = {"Regular": 0, "Medium": 0, "Semibold": 1, "Bold": 1}


def font(size: int, weight: str = "Regular") -> ImageFont.FreeTypeFont:
    """Load a system font at `size` px, preferring SF Pro at the requested weight."""
    try:
        f = ImageFont.truetype(_SFNS, size)
        f.set_variation_by_name(weight)
        return f
    except Exception:
        pass
    try:
        return ImageFont.truetype(
            _HELVETICA_NEUE, size, index=_HELVETICA_NEUE_INDEX.get(weight, 0)
        )
    except Exception:
        return ImageFont.load_default(size)


def load_master() -> Image.Image:
    """The 1024x1024 RGBA app logo."""
    path = MASTER_LOGO if MASTER_LOGO.exists() else ICONSET_1024
    return Image.open(path).convert("RGBA")


def dragon_badge(size: int) -> Image.Image:
    """The circular dragon mark, cropped out of the master and alpha-masked."""
    from PIL import ImageDraw

    crop = load_master().crop(DRAGON_BOX)
    # Render the mask at 4x and downsample so the circle edge is antialiased.
    mask = Image.new("L", (crop.width * 4, crop.height * 4), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, mask.width - 1, mask.height - 1), fill=255)
    crop.putalpha(mask.resize(crop.size, Image.LANCZOS))
    return crop.resize((size, size), Image.LANCZOS)


def fit(image: Image.Image, box: int) -> Image.Image:
    """Scale `image` so its longest side is `box` px."""
    scale = box / max(image.size)
    return image.resize(
        (max(1, round(image.width * scale)), max(1, round(image.height * scale))),
        Image.LANCZOS,
    )
