#!/usr/bin/env python3
"""Regenerate the FryPDF disk image background.

    python3 packaging/macos/dmg/make-background.py

Writes background.png (660x440) and background@2x.png (1320x880) next to itself.
Both are committed so CI never has to run Pillow - that keeps release artifacts
reproducible and avoids font-rendering drift between a dev Mac and the runner.

Two constraints drive the design:

  * Finder anchors the image at the content view's origin and neither scales nor
    tiles it. Oversize crops harmlessly, undersize leaves a bare strip, so the
    bottom 40px is deliberate bleed that absorbs title-bar height differences.
  * Finder draws icon labels in the *system* appearance's colour - black in light
    mode, white in dark. A single fixed image therefore has to sit the label row
    on a mid-tone, which is what the blurred pedestals under each icon are for.

Geometry here must stay in step with packaging/macos/dmg/settings.py.
"""

from __future__ import annotations

import pathlib
import sys

from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[3] / "tools"))

import branding as b  # noqa: E402

OUT_DIR = pathlib.Path(__file__).resolve().parent

WIDTH, HEIGHT = 660, 440
HEADER_H = 84
RULE_H = 3

# Icon centres, mirrored from settings.py icon_locations.
APP_XY = (170, 196)
APPLICATIONS_XY = (490, 196)

SURFACE_TOP = (238, 242, 247)
SURFACE_BOTTOM = (220, 228, 238)
PEDESTAL = (51, 65, 85)
CAPTION = (13, 43, 69)


def _vertical_gradient(size, top, bottom) -> Image.Image:
    width, height = size
    strip = Image.new("RGB", (1, height))
    pixels = strip.load()
    for y in range(height):
        t = y / max(1, height - 1)
        pixels[0, y] = tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
    return strip.resize((width, height), Image.BILINEAR)


def _pedestals(scale: float) -> Image.Image:
    """Soft mid-tone ellipses under the *label* row - not the icon - so the names
    stay readable whether Finder draws them black (light) or white (dark).

    Icons are 128px centred at y=196, so they occupy y=132..260 and the label
    sits immediately below that.
    """
    s = lambda v: round(v * scale)  # noqa: E731
    layer = Image.new("RGBA", (round(WIDTH * scale), round(HEIGHT * scale)), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for cx, _ in (APP_XY, APPLICATIONS_XY):
        draw.ellipse(
            [s(cx) - s(88), s(273) - s(15), s(cx) + s(88), s(273) + s(15)],
            fill=PEDESTAL + (88,),
        )
    return layer.filter(ImageFilter.GaussianBlur(s(11)))


def _arrow(draw: ImageDraw.ImageDraw, scale: float) -> None:
    """A red-orange arrow pointing from the app towards the Applications alias."""
    s = lambda v: round(v * scale)  # noqa: E731
    cx, cy = s(330), s(190)
    half_w, shaft_h = s(46), s(13)
    head_w, head_h = s(30), s(36)

    draw.rounded_rectangle(
        [cx - half_w, cy - shaft_h // 2, cx + half_w - head_w, cy + shaft_h // 2],
        radius=shaft_h // 2,
        fill=b.RED,
    )
    draw.polygon(
        [
            (cx + half_w, cy),
            (cx + half_w - head_w, cy - head_h // 2),
            (cx + half_w - head_w, cy + head_h // 2),
        ],
        fill=b.RED,
    )


def render(scale: float) -> Image.Image:
    s = lambda v: round(v * scale)  # noqa: E731
    width, height = round(WIDTH * scale), round(HEIGHT * scale)

    canvas = Image.new("RGB", (width, height), SURFACE_TOP)
    canvas.paste(
        _vertical_gradient((width, height - s(HEADER_H)), SURFACE_TOP, SURFACE_BOTTOM),
        (0, s(HEADER_H)),
    )
    canvas.paste(Image.new("RGB", (width, s(HEADER_H)), b.NAVY), (0, 0))
    canvas = canvas.convert("RGBA")

    draw = ImageDraw.Draw(canvas)
    draw.rectangle([0, s(HEADER_H) - s(RULE_H), width, s(HEADER_H)], fill=b.RED)

    badge = b.dragon_badge(s(56))
    canvas.alpha_composite(badge, (s(28), s(14)))

    wordmark = b.font(s(26), "Semibold")
    draw.text((s(100), s(20)), "FryPDF", font=wordmark, fill=b.WHITE)
    tagline = b.font(s(12), "Regular")
    draw.text((s(101), s(54)), "Privacy-first PDF editor", font=tagline, fill=b.MUTED)

    canvas.alpha_composite(_pedestals(scale))
    _arrow(ImageDraw.Draw(canvas), scale)

    caption = b.font(s(15), "Semibold")
    text = "Drag FryPDF to Applications"
    draw = ImageDraw.Draw(canvas)
    draw.text(
        (s(330) - draw.textlength(text, font=caption) / 2, s(316)),
        text,
        font=caption,
        fill=CAPTION,
    )

    return canvas.convert("RGB")


def main() -> int:
    for scale, name in ((1.0, "background.png"), (2.0, "background@2x.png")):
        path = OUT_DIR / name
        image = render(scale)
        image.save(path, optimize=True)
        print(f"  {path.relative_to(b.REPO_ROOT)}  {image.width}x{image.height}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
