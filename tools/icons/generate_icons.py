#!/usr/bin/env python3
"""Regenerate FryPDF's Windows icon and Inno Setup wizard artwork.

    python3 tools/icons/generate_icons.py

Writes:
    src/PdfEditorApp/Assets/app-logo.ico          canonical Windows icon
    packaging/windows/branding/wizard-large-*.png Inno welcome/finish panel
    packaging/windows/branding/wizard-small-*.png Inno header badge

The app logo artwork itself is not modified - the master stays
src/PdfEditorApp/Assets/app-logo.png. Run tools/icons/verify_icons.py afterwards.
"""

from __future__ import annotations

import pathlib
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))

import branding as b  # noqa: E402
import icoformat  # noqa: E402

ICO_OUT = b.REPO_ROOT / "src" / "PdfEditorApp" / "Assets" / "app-logo.ico"
BRANDING_DIR = b.REPO_ROOT / "packaging" / "windows" / "branding"

# Inno scales these to the wizard's DPI and picks whichever supplied file is
# closest, so ship the 100% design plus two higher-density renders.
WIZARD_LARGE_SCALES = (1.0, 2.0, 2.5)
WIZARD_LARGE_BASE = (164, 314)
WIZARD_SMALL_SIZES = (55, 110, 165)


def _vertical_gradient(size, top, bottom) -> Image.Image:
    width, height = size
    strip = Image.new("RGB", (1, height))
    pixels = strip.load()
    for y in range(height):
        t = y / max(1, height - 1)
        pixels[0, y] = tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
    return strip.resize((width, height), Image.BILINEAR)


def wizard_large(scale: float) -> Image.Image:
    """The tall panel Inno shows on the Welcome and Finished pages."""
    width, height = (round(v * scale) for v in WIZARD_LARGE_BASE)
    s = lambda v: max(1, round(v * scale))  # noqa: E731

    canvas = _vertical_gradient((width, height), b.NAVY_DEEP, b.NAVY).convert("RGBA")
    draw = ImageDraw.Draw(canvas)

    logo = b.fit(b.load_master(), s(112))
    logo_center_y = s(112)
    canvas.alpha_composite(
        logo, ((width - logo.width) // 2, logo_center_y - logo.height // 2)
    )

    def centered(text, y, size, weight, fill):
        f = b.font(s(size), weight)
        w = draw.textlength(text, font=f)
        draw.text(((width - w) / 2, y), text, font=f, fill=fill)

    centered("FryPDF", s(176), 21, "Semibold", b.WHITE)
    draw.rectangle(
        [(width - s(34)) / 2, s(206), (width + s(34)) / 2, s(206) + s(3)], fill=b.RED
    )
    centered("Privacy-first PDF studio", s(218), 9, "Regular", b.MUTED)
    centered("codefrydev.in", height - s(24), 8, "Regular", b.MUTED)

    return canvas.convert("RGB")


def wizard_small(size: int) -> Image.Image:
    """The badge in the wizard header. Inno's modern header is light, so this is
    a navy tile; the corner pixels stay white so a no-alpha fallback still fits."""
    supersample = 4
    big = size * supersample

    tile = Image.new("RGB", (big, big), b.WHITE)
    mask = Image.new("L", (big, big), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        (0, 0, big - 1, big - 1), radius=round(big * 0.22), fill=255
    )
    tile.paste(Image.new("RGB", (big, big), b.NAVY), mask=mask)

    logo = b.fit(b.load_master(), round(big * 0.64))
    tile_rgba = tile.convert("RGBA")
    tile_rgba.alpha_composite(
        logo, ((big - logo.width) // 2, (big - logo.height) // 2)
    )
    tile_rgba.putalpha(mask)

    return tile_rgba.resize((size, size), Image.LANCZOS)


def main() -> int:
    master = b.load_master()

    icoformat.write_ico(master, ICO_OUT)
    print(f"  {ICO_OUT.relative_to(b.REPO_ROOT)}  ({ICO_OUT.stat().st_size:,} bytes)")

    BRANDING_DIR.mkdir(parents=True, exist_ok=True)
    for scale in WIZARD_LARGE_SCALES:
        image = wizard_large(scale)
        path = BRANDING_DIR / f"wizard-large-{image.width}x{image.height}.png"
        image.save(path, optimize=True)
        print(f"  {path.relative_to(b.REPO_ROOT)}")

    for size in WIZARD_SMALL_SIZES:
        image = wizard_small(size)
        path = BRANDING_DIR / f"wizard-small-{size}x{size}.png"
        image.save(path, optimize=True)
        print(f"  {path.relative_to(b.REPO_ROOT)}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
