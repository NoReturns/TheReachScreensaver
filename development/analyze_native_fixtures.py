"""Analyze one disposable Stage A2 native fixture manifest in its own run root."""

import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def seam_metrics(pixels, x, first_y, last_y):
    line = pixels[first_y:last_y, x - 2:x + 2, :3].astype(np.float64)
    left, left_edge, right_edge, right = [line[:, i] for i in range(4)]
    rgb = np.abs(left_edge - right_edge)
    lum = lambda color: color @ np.array([0.2126, 0.7152, 0.0722])
    jump = np.abs(lum(left_edge) - lum(right_edge))
    gradient = (np.abs(lum(left) - lum(left_edge))
                + np.abs(lum(right_edge) - lum(right))) / 2
    impulse = np.maximum(0, jump - gradient)
    rows = np.flatnonzero(impulse > 20) + first_y
    return {
        "x": x,
        "absoluteYRange": [first_y, last_y - 1],
        "meanAdjacentRgbDifference": rgb.mean(axis=0).round(3).tolist(),
        "maxAdjacentRgbDifference": rgb.max(axis=0).astype(int).tolist(),
        "meanLuminanceImpulse": round(float(impulse.mean()), 3),
        "maxLuminanceImpulse": round(float(impulse.max()), 3),
        "p95LuminanceImpulse": round(float(np.percentile(impulse, 95)), 3),
        "p99LuminanceImpulse": round(float(np.percentile(impulse, 99)), 3),
        "rowsWithImpulseAbove20": rows.astype(int).tolist(),
    }


def main(manifest_path):
    manifest_path = Path(manifest_path).resolve()
    run_root = manifest_path.parent.parent
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if len(manifest["captures"]) != 3 or len(manifest["statuses"]) != 3:
        raise ValueError("Expected three captured displays")
    state_key = manifest["stateKey"]
    if not state_key or any(s["fixture"]["nativeFixture"]["stateKey"] != state_key
                            for s in manifest["statuses"]):
        raise ValueError("Fixture states disagree")
    desktop = Image.new("RGB", (8320, 2160), "black")
    for capture, status in zip(manifest["captures"], manifest["statuses"]):
        path = Path(capture["path"]).resolve()
        if not path.is_relative_to(run_root / "captures"):
            raise ValueError("Capture escaped sandbox")
        data = path.read_bytes()
        if hashlib.sha256(data).hexdigest().upper() != capture["sha256"].upper():
            raise ValueError("Capture hash mismatch")
        view = status["fixture"]["contract"]
        x, y = view["viewportOffsetX"], view["viewportOffsetY"]
        width, height = view["viewportWidth"], view["viewportHeight"]
        with Image.open(path) as image:
            if image.size != (width, height):
                raise ValueError("Captured size disagrees with fixture contract")
            desktop.paste(image.convert("RGB"), (x, y))
    output = run_root / "captures"
    desktop.save(output / "assembled.png")
    desktop.crop((3776, 800, 3904, 1500)).save(output / "left-primary-seam.png")
    desktop.crop((6336, 1080, 6464, 1600)).save(output / "primary-right-seam.png")
    if manifest["fixtureId"].startswith("hole-") or manifest["fixtureId"] == "roll-15":
        desktop.crop((3000, 720, 4680, 1800)).save(output / "black-hole-detail.png")
    pixels = np.asarray(desktop)
    repeat_metrics = []
    originals = {item["display"]: item for item in manifest["captures"]}
    for item in manifest.get("repeatCaptures", []):
        path = Path(item["path"]).resolve()
        if not path.is_relative_to(run_root / "captures"):
            raise ValueError("Repeat capture escaped sandbox")
        if hashlib.sha256(path.read_bytes()).hexdigest().upper() != item["sha256"].upper():
            raise ValueError("Repeat capture hash mismatch")
        with Image.open(originals[item["display"]]["path"]) as baseline, Image.open(path) as repeat:
            a = np.asarray(baseline.convert("RGB"), dtype=np.int16)
            b = np.asarray(repeat.convert("RGB"), dtype=np.int16)
            delta = np.abs(a - b)
            repeat_metrics.append({"frame": item["frame"], "display": item["display"],
                                   "meanAbsoluteRgbDifference": round(float(delta.mean()), 6),
                                   "maxAbsoluteRgbDifference": int(delta.max()),
                                   "differentPixels": int(np.count_nonzero(np.any(delta, axis=2)))})
    report = {
        "fixtureId": manifest["fixtureId"],
        "mode": manifest["mode"],
        "post": manifest["post"],
        "stateKeySha256": hashlib.sha256(state_key.encode()).hexdigest(),
        "leftPrimary": seam_metrics(pixels, 3840, 800, 1500),
        "primaryRight": seam_metrics(pixels, 6400, 1080, 1600),
        "repeatFrames": repeat_metrics,
    }
    (run_root / "metrics" / "visual-analysis.json").write_text(
        json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"fixture": report["fixtureId"], "mode": report["mode"],
                      "post": report["post"], "run": str(run_root),
                      "state": report["stateKeySha256"],
                      "leftImpulse": report["leftPrimary"]["meanLuminanceImpulse"],
                      "leftRows": len(report["leftPrimary"]["rowsWithImpulseAbove20"])},
                     separators=(",", ":")))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Use: python analyze_native_fixtures.py <manifest.json>")
    main(sys.argv[1])
