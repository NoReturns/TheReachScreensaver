"""Verify the disposable native yaw sweep and measure its physical seam."""

import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def main(path):
    path = Path(path).resolve()
    root = path.parent.parent
    data = json.loads(path.read_text(encoding="utf-8"))
    if data["fixtureId"] != "hole-26-left" or data["guardPhysicalPixels"] != 512:
        raise ValueError("Unexpected motion fixture")
    result = []
    for frame in data["frames"]:
        if len(frame["statuses"]) != 3 or len(frame["captures"]) != 2:
            raise ValueError("Incomplete motion group")
        states = [s["fixture"]["nativeFixture"] for s in frame["statuses"]]
        if any(s["sessionId"] != data["sessionId"] or s["stateKey"] != frame["stateKey"]
               or s["state"]["yawDegrees"] != frame["yawDegrees"]
               for s in states):
            raise ValueError("Unsynchronized motion state")
        strips = {}
        for capture, recorded in zip(frame["captures"], frame["statuses"]):
            image_path = Path(capture["path"]).resolve()
            if not image_path.is_relative_to(root / "captures"):
                raise ValueError("Motion image escaped sandbox")
            if hashlib.sha256(image_path.read_bytes()).hexdigest().upper() != capture["sha256"].upper():
                raise ValueError("Motion image hash changed")
            view = recorded["fixture"]["contract"]
            with Image.open(image_path) as image:
                rgb = np.asarray(image.convert("RGB"))
                if tuple(rgb.shape[:2]) != (view["viewportHeight"], view["viewportWidth"]):
                    raise ValueError("Motion capture geometry changed")
                x = rgb.shape[1] - 2 if view["viewportOffsetX"] == 0 else 0
                y0 = 800 - view["viewportOffsetY"]
                y1 = 1500 - view["viewportOffsetY"]
                strips[view["viewportOffsetX"]] = rgb[y0:y1, x:x + 2].astype(float)
        if set(strips) != {0, 3840}:
            raise ValueError("Motion displays do not meet at left seam")
        left, primary = strips[0], strips[3840]
        luminance = lambda pixels: pixels @ np.array([0.2126, 0.7152, 0.0722])
        a, b, c, d = (luminance(left[:, 0]), luminance(left[:, 1]),
                      luminance(primary[:, 0]), luminance(primary[:, 1]))
        impulse = np.maximum(0, np.abs(b-c) - (np.abs(a-b)+np.abs(c-d))/2)
        result.append({"yawDegrees": frame["yawDegrees"],
                       "meanImpulse": round(float(impulse.mean()), 3),
                       "peakImpulse": round(float(impulse.max()), 3),
                       "rowsAbove20": int(np.count_nonzero(impulse > 20))})
    output = {"sessionId": data["sessionId"], "frames": result}
    destination = root / "metrics" / "motion-analysis.json"
    destination.write_text(json.dumps(output, indent=2), encoding="utf-8")
    print(json.dumps(output, separators=(",", ":")))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Use: python analyze_native_motion.py <motion manifest>")
    main(sys.argv[1])
