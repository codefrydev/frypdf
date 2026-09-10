# FryPDF disk image assets

These files define what a user sees when they open `FryPDF-<version>-arm64.dmg`.

| File | Role |
|---|---|
| `settings.py` | dmgbuild settings: window size, icon size and positions, hidden Finder chrome |
| `make-background.py` | Pillow generator for the two background images |
| `background.png` | 660x440 background, **committed** |
| `background@2x.png` | 1320x880 retina background, **committed** |

The build itself is [`../make-dmg.sh`](../make-dmg.sh), which the release workflow
calls. It bootstraps a pinned `dmgbuild` into `.venv-dmg/`, builds the image, then
re-mounts the result and asserts the payload, the `Applications` symlink, the
retina background, the volume icon, the Finder layout and the code signature — so
a release cannot silently ship an unstyled DMG.

## Regenerating the background

```bash
python3 packaging/macos/dmg/make-background.py
```

Requires Pillow. Both PNGs are committed rather than generated in CI: that keeps
release artifacts reproducible and avoids font-rendering differences between a
developer's Mac and the GitHub runner. Commit the regenerated PNGs alongside any
change to the generator.

## Two things that will bite you

**Geometry is duplicated.** `make-background.py` draws the arrow, the caption and
the label pedestals at coordinates that assume the icon positions in `settings.py`
(`FryPDF.app` at `(170, 196)`, `Applications` at `(490, 196)`, icon size 128).
Change one and you must change the other.

**Finder colours the labels, not the image.** Icon labels are drawn in the
viewer's system appearance — black in light mode, white in dark. That is why the
background carries blurred mid-tone pedestals under the label row: they are the
only reason one fixed image stays readable in both. After changing the background,
check it in *both* appearances:

```bash
bash packaging/macos/make-dmg.sh --app FryPDF.app --output /tmp/FryPDF-dev.dmg
killall Finder && open /tmp/FryPDF-dev.dmg
# then flip System Settings > Appearance to Dark, eject, killall Finder, reopen
```

The bottom 40px of the image is deliberate bleed. Finder anchors the background at
the content view's origin and neither scales nor tiles it, so an oversize image
crops harmlessly while an undersize one leaves a bare strip when the title-bar
height changes between macOS releases.
