"""Assemble exact R13 WebView2 monitor captures without altering source pixels."""

import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).parent / "r13-runs"
OUT = ROOT / "montage"
OUT.mkdir(exist_ok=True)

RUNS = {("hole-yaw0", "rectilinear"): "c45ebf68e80a435e8981f933e2e2176e"}
for line in (ROOT / "runs.jsonl").read_text().splitlines():
    item = json.loads(line)
    RUNS[(item["Case"], item["Mode"])] = Path(item["Root"]).name[4:]

CASES = ["hole-yaw0", "hole-yaw-6", "hole-yaw-plus", "peripheral", "starfield"]
MODES = ["rectilinear", "cylindrical"]
font = ImageFont.truetype("C:/Windows/Fonts/consola.ttf", 22)


def load(case, mode):
    root = ROOT / ("run-" + RUNS[(case, mode)])
    manifests = list((root / "metrics").glob("fixture-*.json"))
    assert len(manifests) == 1, (case, mode, manifests)
    data = json.loads(manifests[0].read_text())
    assert data["mode"] == mode and len(data["captures"]) == 3
    assert len({s["fixture"]["nativeFixture"]["stateKey"] for s in data["statuses"]}) == 1
    desktop = data["layout"]["desktop"]
    assert (desktop["Width"], desktop["Height"]) == (8320, 2160)
    full = Image.new("RGB", (8320, 2160), (0, 0, 0))
    refs = []
    for entry in data["captures"]:
        path = Path(entry["path"])
        raw = path.read_bytes()
        assert hashlib.sha256(raw).hexdigest().upper() == entry["sha256"].upper()
        region = next(d for d in data["layout"]["displays"] if d["Name"] == entry["display"])
        b = region["Bounds"]
        with Image.open(path) as im:
            assert im.size == (b["Width"], b["Height"])
            full.paste(im.convert("RGB"), (b["X"] - desktop["X"], b["Y"] - desktop["Y"]))
        refs.append({"display": entry["display"], "path": str(path), "sha256": entry["sha256"],
                     "virtualRect": [b["X"] - desktop["X"], b["Y"] - desktop["Y"], b["Width"], b["Height"]]})
    key = data["stateKey"].encode()
    return full, {"run": str(root), "manifest": str(manifests[0]), "fixture": data["fixtureId"],
                  "stateKeySha256": hashlib.sha256(key).hexdigest(), "captures": refs,
                  "statuses": [{"display": s["fixture"]["display"],
                                "targets": s["fixture"]["cylindrical"]["sectorTargets"] if mode == "cylindrical" else [],
                                "postTarget": s["fixture"]["cylindrical"]["postTarget"] if mode == "cylindrical" else None,
                                "buffer": s["fixture"]["drawingBuffer"]} for s in data["statuses"]]}


def label(draw, xy, value, color="white"):
    draw.text(xy, value, font=font, fill=color, stroke_width=2, stroke_fill="black")


manifest = {}
overview_rows = []
for case in CASES:
    a, ma = load(case, "rectilinear")
    b, mb = load(case, "cylindrical")
    assert ma["stateKeySha256"] == mb["stateKeySha256"], case
    manifest[case] = {"rectilinear": ma, "cylindrical": mb}
    # Full layouts are nearest-neighbor downscaled for legibility; source PNGs remain exact.
    scale = 0.28
    w, h = round(8320 * scale), round(2160 * scale)
    cw, ch = 580, 600
    panel = Image.new("RGB", (w * 2 + 50, h + ch + 110), (20, 20, 24))
    panel.paste(a.resize((w, h), Image.Resampling.LANCZOS), (10, 65))
    panel.paste(b.resize((w, h), Image.Resampling.LANCZOS), (w + 30, 65))
    draw = ImageDraw.Draw(panel)
    label(draw, (10, 8), f"{case} | RECTILINEAR")
    label(draw, (w + 30, 8), f"{case} | CYLINDRICAL")
    # Same physical join coordinates for both projections; no cross-projection subtraction.
    crops = [("L/P x=3840", (3540, 850, 4140, 1450)),
             ("P/R x=6400", (6100, 1140, 6700, 1740))]
    for i, (name, box) in enumerate(crops):
        ax = 10 + i * (cw + 20)
        bx = w + 30 + i * (cw + 20)
        y = h + 95
        panel.paste(a.crop(box).resize((cw, ch), Image.Resampling.NEAREST), (ax, y))
        panel.paste(b.crop(box).resize((cw, ch), Image.Resampling.NEAREST), (bx, y))
        label(draw, (ax, y), name)
        label(draw, (bx, y), name)
    path = OUT / f"R13_{case}.png"
    panel.save(path)
    manifest[case]["montage"] = str(path)
    overview_rows.append(panel.resize((2400, round(panel.height * 2400 / panel.width)), Image.Resampling.LANCZOS))

gap = 16
review = Image.new("RGB", (2400, sum(x.height for x in overview_rows) + gap * (len(overview_rows) - 1)), (12, 12, 12))
y = 0
for row in overview_rows:
    review.paste(row, (0, y))
    y += row.height + gap
review_path = OUT / "R13_OWNER_REVIEW_MONTAGE.png"
review.save(review_path)
(OUT / "capture_manifest.json").write_text(json.dumps(manifest, indent=2))
print(review_path)
