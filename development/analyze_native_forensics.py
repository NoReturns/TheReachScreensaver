"""Check one exact-state, disposable 26-radii native forensic capture."""

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image


STAGES = (
    "unlensed", "lensed", "bright", "horizontal0", "vertical0",
    "horizontal1", "vertical1", "horizontal2", "vertical2",
    "horizontal3", "vertical3", "horizontal4", "vertical4",
    "composite", "postOutput",
)


def main(path):
    path = Path(path).resolve()
    run = path.parent.parent
    evidence = json.loads(path.read_text(encoding="utf-8"))
    manifests = list((run / "metrics").glob("fixture-hole-26-left-cylindrical-final-*.json"))
    if len(manifests) != 1:
        raise ValueError("Expected one matching frozen fixture manifest")
    manifest = json.loads(manifests[0].read_text(encoding="utf-8"))
    if (manifest["sessionId"] != evidence["sessionId"]
            or manifest["stateKey"] != evidence["stateKey"]
            or manifest["frozenYawDegrees"] != evidence["frozenYawDegrees"]
            or manifest["guardPhysicalPixels"] not in (128, 256, 512)
            or not manifest["forensics"]):
        raise ValueError("Fixture and forensic capture contracts differ")
    records = {r["display"]: r for r in evidence["records"]}
    if set(records) != {"left", "primary", "right"}:
        raise ValueError("Expected all three displays")
    for display, record in records.items():
        if (record["sessionId"] != evidence["sessionId"]
                or record["fixtureId"] != "hole-26-left"
                or record["stateKey"] != evidence["stateKey"]
                or record["frameRevision"] != 1):
            raise ValueError("Forensic display state differs")
        status = next(s for s in manifest["statuses"]
                      if s["fixture"]["display"] == display)
        state = status["fixture"]["nativeFixture"]
        if (state["frameRevision"] != 1 or not state["renderReady"]
                or state["stateKey"] != evidence["stateKey"]
                or state["state"]["yawDegrees"] != evidence["frozenYawDegrees"]
                or status["fixture"]["mode"] != "cylindrical"):
            raise ValueError("Captured display was not at the frozen state")
    left, primary = records["left"], records["primary"]
    if not set(left["sectorCenters"]).intersection(primary["sectorCenters"]):
        raise ValueError("Displays have no shared sector centre")
    stages = {}
    for name in STAGES:
        a, b = (np.asarray(record["stages"][name]["rows"], dtype=np.float64)
                for record in (left, primary))
        if a.shape != (420, 4, 3) or b.shape != a.shape:
            raise ValueError("Invalid forensic strip dimensions")
        delta = np.abs(a - b)
        stages[name] = {
            "targetLeft": left["stages"][name]["target"],
            "targetPrimary": primary["stages"][name]["target"],
            "meanAbsoluteOverlapRgb": round(float(delta.mean()), 8),
            "peakAbsoluteOverlapRgb": round(float(delta.max()), 8),
        }
    assembly = run / "captures" / "assembled.png"
    if not assembly.is_file():
        raise ValueError("Run the fixture capture verifier first")
    with Image.open(assembly) as image:
        pixels = np.asarray(image.convert("RGB"), dtype=np.float64)
    metric = json.loads((run / "metrics" / "visual-analysis.json").read_text(encoding="utf-8"))
    hard_rows = metric["leftPrimary"]["rowsWithImpulseAbove20"]
    spikes = []
    for y in hard_rows:
        output_left = np.asarray(left["stages"]["postOutput"]["rows"][y - 800])
        output_primary = np.asarray(primary["stages"]["postOutput"]["rows"][y - 800])
        adjacent = float(np.max(np.abs(output_left[1] - output_left[2])))
        overlap = float(np.max(np.abs(output_left - output_primary)))
        spikes.append({"y": y, "finalRgb": pixels[y, 3838:3842].astype(int).tolist(),
                       "sameContextOutputAdjacentPeak": round(adjacent, 6),
                       "crossContextOutputOverlapPeak": round(overlap, 6)})
    report = {"schema": 1, "sessionId": evidence["sessionId"],
              "yawDegrees": evidence["frozenYawDegrees"],
              "stateKey": evidence["stateKey"], "exactStateValidated": True,
              "stageOverlap": stages,
              "seamMetrics": metric["leftPrimary"],
              "hardRows": spikes}
    destination = run / "metrics" / "forensic-analysis.json"
    destination.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"yaw": report["yawDegrees"], "hardRows": len(spikes),
                      "meanImpulse": metric["leftPrimary"]["meanLuminanceImpulse"],
                      "peakImpulse": metric["leftPrimary"]["maxLuminanceImpulse"],
                      "rawLensPeakDifference": stages["lensed"]["peakAbsoluteOverlapRgb"],
                      "mip4HorizontalPeakDifference": stages["horizontal4"]["peakAbsoluteOverlapRgb"],
                      "outputPeakDifference": stages["postOutput"]["peakAbsoluteOverlapRgb"]}))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Use: python analyze_native_forensics.py <forensics.json>")
    main(sys.argv[1])
