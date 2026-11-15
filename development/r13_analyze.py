"""Summarize R13 paired fixture evidence without altering capture files."""

import json
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).parent / "r13-runs"
MANIFEST = json.loads((ROOT / "montage/capture_manifest.json").read_text())
RESULT = {}


def percentile(values, pct):
    return float(np.percentile(np.asarray(values, dtype=np.float64), pct))


def image_for(record, x):
    for capture in record["captures"]:
        px, py, width, height = capture["virtualRect"]
        if px <= x < px + width:
            return np.asarray(Image.open(capture["path"]).convert("RGB"), dtype=np.float32), px, py
    raise ValueError(x)


for case, modes in MANIFEST.items():
    RESULT[case] = {}
    for mode in ["rectilinear", "cylindrical"]:
        rec = modes[mode]
        joins = {}
        for label, x, y0, y1 in [("left-primary", 3840, 720, 2160),
                                 ("primary-right", 6400, 1080, 2160)]:
            a, ax, ay = image_for(rec, x - 1)
            b, bx, by = image_for(rec, x)
            left = a[y0 - ay:y1 - ay, x - 1 - ax]
            right = b[y0 - by:y1 - by, x - bx]
            assert left.shape == right.shape == (y1 - y0, 3)
            weights = np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
            delta = np.abs(left @ weights - right @ weights)
            joins[label] = {"meanLuma": float(delta.mean()), "p95Luma": percentile(delta, 95),
                            "peakLuma": float(delta.max()), "rowsAbove20": int((delta > 20).sum()),
                            "rows": len(delta)}
        RESULT[case][mode] = {"joins": joins}


for line in (ROOT / "perf-runs.jsonl").read_text().splitlines():
    item = json.loads(line)
    report = next((Path(item["Root"]) / "metrics").glob("r13-*.json"))
    data = json.loads(report.read_text())
    perf = {"report": str(report), "hostWorkingSetMiB": data["hostWorkingSet"] / 1048576,
            "childWorkingSetSumMiB": sum(max(0, p["workingSet"]) for p in data["webView2Processes"]) / 1048576,
            "childPrivateBytesSumMiB": sum(max(0, p["privateBytes"]) for p in data["webView2Processes"]) / 1048576,
            "processKinds": [p["kind"] for p in data["webView2Processes"]], "displays": {}}
    for record in data["records"]:
        m = record["measurement"]
        draw = m["drawCalls"]
        perf["displays"][record["display"]] = {
            "cpuMedianMs": percentile(m["cpuSubmissionMs"], 50),
            "cpuP95Ms": percentile(m["cpuSubmissionMs"], 95),
            "completedMedianMs": percentile(m["completedRenderMs"], 50),
            "completedP95Ms": percentile(m["completedRenderMs"], 95),
            "drawCallsMin": min(draw), "drawCallsMax": max(draw),
            "contextLost": m["contextLost"], "glError": m["glError"],
        }
    RESULT[item["Case"]][item["Mode"]]["performance"] = perf

(ROOT / "montage/analysis.json").write_text(json.dumps(RESULT, indent=2))
for case in RESULT:
    print(case)
    for mode in RESULT[case]:
        r = RESULT[case][mode]
        joins = r["joins"]
        print(mode, "L/P", round(joins["left-primary"]["meanLuma"], 2),
              joins["left-primary"]["rowsAbove20"], "P/R",
              round(joins["primary-right"]["meanLuma"], 2), joins["primary-right"]["rowsAbove20"])
        if "performance" in r:
            p = r["performance"]
            print("  WS MiB", round(p["hostWorkingSetMiB"] + p["childWorkingSetSumMiB"]),
                  "displays", [(k, round(v["cpuMedianMs"], 2), round(v["completedMedianMs"], 2),
                                v["drawCallsMin"]) for k, v in p["displays"].items()])
