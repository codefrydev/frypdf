"""A hand-rolled Windows .ico writer/reader.

Pillow's own ICO encoder PNG-compresses *every* frame and writes wPlanes=0.
Windows Vista+ tolerates that, but it is not the canonical layout: plenty of
third-party consumers only understand DIB frames below 256px, and some render
PNG-based frames with a black backplate. So the frames below 256 are written as
32-bit BGRA DIBs with a real AND mask, and only the 256px frame is PNG.
"""

from __future__ import annotations

import io
import struct

from PIL import Image

# Sizes Windows 11 asks for across the context menu, title bar, tray, taskbar
# and Start scale factors. See "Construct your Windows app's icon" on MS Learn.
DIB_SIZES = (16, 20, 24, 32, 40, 48, 64, 128)
PNG_SIZES = (256,)
ALL_SIZES = DIB_SIZES + PNG_SIZES

_PNG_MAGIC = b"\x89PNG\r\n\x1a\n"


def _and_mask_stride(width: int) -> int:
    """AND mask rows are padded to a 4-byte boundary."""
    return ((width + 31) // 32) * 4


def _encode_dib(image: Image.Image) -> bytes:
    """BITMAPINFOHEADER + bottom-up BGRA XOR bitmap + bottom-up 1bpp AND mask."""
    width, height = image.size
    pixels = image.load()

    header = struct.pack(
        "<IiiHHIIiiII",
        40,  # biSize
        width,  # biWidth
        height * 2,  # biHeight - XOR and AND stacked
        1,  # biPlanes
        32,  # biBitCount
        0,  # biCompression = BI_RGB
        0,  # biSizeImage - may be 0 for BI_RGB
        0,  # biXPelsPerMeter
        0,  # biYPelsPerMeter
        0,  # biClrUsed
        0,  # biClrImportant
    )

    xor = bytearray()
    for y in range(height - 1, -1, -1):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            xor += bytes((b, g, r, a))

    # Derive the mask rather than zero-filling it: consumers that ignore the
    # alpha channel then still get a correct silhouette instead of a black box.
    stride = _and_mask_stride(width)
    mask = bytearray()
    for y in range(height - 1, -1, -1):
        row = bytearray(stride)
        for x in range(width):
            if pixels[x, y][3] < 128:
                row[x // 8] |= 0x80 >> (x % 8)
        mask += row

    return bytes(header) + bytes(xor) + bytes(mask)


def _encode_png(image: Image.Image) -> bytes:
    buffer = io.BytesIO()
    image.save(buffer, format="PNG", optimize=True)
    return buffer.getvalue()


def write_ico(master: Image.Image, path, sizes=ALL_SIZES, png_sizes=PNG_SIZES) -> None:
    """Render `master` into an .ico at `path`, one frame per entry in `sizes`."""
    master = master.convert("RGBA")
    frames = []
    for size in sizes:
        scaled = master.resize((size, size), Image.LANCZOS)
        payload = _encode_png(scaled) if size in png_sizes else _encode_dib(scaled)
        frames.append((size, payload))

    offset = 6 + 16 * len(frames)
    directory = bytearray(struct.pack("<HHH", 0, 1, len(frames)))
    for size, payload in frames:
        directory += struct.pack(
            "<BBBBHHII",
            size if size < 256 else 0,  # bWidth  - 0 means 256
            size if size < 256 else 0,  # bHeight - 0 means 256
            0,  # bColorCount - 0 for >8bpp
            0,  # bReserved
            1,  # wPlanes
            32,  # wBitCount
            len(payload),
            offset,
        )
        offset += len(payload)

    with open(path, "wb") as handle:
        handle.write(directory)
        for _, payload in frames:
            handle.write(payload)


def read_ico(path):
    """Parse an .ico into a list of frame dicts, for verification."""
    data = open(path, "rb").read()
    reserved, kind, count = struct.unpack_from("<HHH", data, 0)
    if reserved != 0 or kind != 1:
        raise ValueError(f"{path}: not an icon file (reserved={reserved} type={kind})")

    frames = []
    for index in range(count):
        (
            width,
            height,
            colors,
            entry_reserved,
            planes,
            bit_count,
            length,
            offset,
        ) = struct.unpack_from("<BBBBHHII", data, 6 + index * 16)
        payload = data[offset : offset + length]
        frames.append(
            {
                "width": width or 256,
                "height": height or 256,
                "colors": colors,
                "reserved": entry_reserved,
                "planes": planes,
                "bit_count": bit_count,
                "length": length,
                "is_png": payload[:8] == _PNG_MAGIC,
                "payload": payload,
            }
        )
    return frames
