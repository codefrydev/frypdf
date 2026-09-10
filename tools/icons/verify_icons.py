#!/usr/bin/env python3
"""Assert that src/PdfEditorApp/Assets/app-logo.ico has the canonical layout.

    python3 tools/icons/verify_icons.py

Guards against a regeneration silently falling back to Pillow's ICO encoder,
which PNG-compresses every frame and writes wPlanes=0.
"""

from __future__ import annotations

import pathlib
import struct
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))

import branding as b  # noqa: E402
import icoformat  # noqa: E402

ICO = b.REPO_ROOT / "src" / "PdfEditorApp" / "Assets" / "app-logo.ico"


def main() -> int:
    if not ICO.exists():
        print(f"FAIL  {ICO} does not exist - run tools/icons/generate_icons.py")
        return 1

    frames = icoformat.read_ico(ICO)
    failures = []

    sizes = tuple(f["width"] for f in frames)
    if sizes != icoformat.ALL_SIZES:
        failures.append(f"frame sizes {sizes} != expected {icoformat.ALL_SIZES}")

    for frame in frames:
        size = frame["width"]
        where = f"{size}x{size}"

        if frame["width"] != frame["height"]:
            failures.append(f"{where}: not square")
        if frame["planes"] != 1:
            failures.append(f"{where}: wPlanes={frame['planes']}, expected 1")
        if frame["bit_count"] != 32:
            failures.append(f"{where}: wBitCount={frame['bit_count']}, expected 32")
        if frame["colors"] != 0:
            failures.append(f"{where}: bColorCount={frame['colors']}, expected 0")
        if frame["reserved"] != 0:
            failures.append(f"{where}: bReserved={frame['reserved']}, expected 0")

        should_be_png = size in icoformat.PNG_SIZES
        if frame["is_png"] != should_be_png:
            kind = "PNG" if frame["is_png"] else "DIB"
            want = "PNG" if should_be_png else "DIB"
            failures.append(f"{where}: encoded as {kind}, expected {want}")
            continue

        if not should_be_png:
            _, bi_width, bi_height, planes, bits, compression = struct.unpack_from(
                "<IiiHHI", frame["payload"], 0
            )
            if (bi_width, bi_height) != (size, size * 2):
                failures.append(
                    f"{where}: BITMAPINFOHEADER is {bi_width}x{bi_height}, "
                    f"expected {size}x{size * 2}"
                )
            if planes != 1 or bits != 32 or compression != 0:
                failures.append(
                    f"{where}: header planes={planes} bits={bits} "
                    f"compression={compression}, expected 1/32/0"
                )

            expected = 40 + size * size * 4 + icoformat._and_mask_stride(size) * size
            if frame["length"] != expected:
                failures.append(
                    f"{where}: payload is {frame['length']} bytes, "
                    f"expected {expected} (header + XOR + AND mask)"
                )
            elif not any(frame["payload"][40 + size * size * 4 :]):
                failures.append(f"{where}: AND mask is all zeroes, expected a silhouette")

    for frame in frames:
        kind = "PNG" if frame["is_png"] else "DIB"
        print(f"  {frame['width']:>3}px  {kind}  {frame['length']:>7,} bytes")

    if failures:
        print()
        for failure in failures:
            print(f"FAIL  {failure}")
        return 1

    print(f"\nOK    {ICO.relative_to(b.REPO_ROOT)}: {len(frames)} canonical frames")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
