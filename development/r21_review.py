"""Pair R21 developer readbacks by virtual pose and make compact review clips."""
from pathlib import Path
import json
import statistics
import cv2
import numpy as np

root = Path(__file__).resolve().parent
out = root / "r21-review"
out.mkdir(exist_ok=True)
runs = {
    "join": {"control": "046b63e635bd4c7ab2dba88dff0b0202",
             "static": "556e529c796741dc8041f157d306670a"},
    "interior": {"control": "d4cc47baf8864208aa9d48aa29d1f8fb",
                 "static": "e014d3ecbb8e48fd99dc63167250740b"},
}

def load_frames(run_id):
    folder = root / "r21-runs" / ("run-" + run_id)
    metrics = json.loads(next((folder / "metrics").glob("b1-*.json")).read_text())
    by = {}
    for f in metrics["frames"]:
        name = f["display"].split("DISPLAY")[-1]
        tick = round(f["virtualMs"] * .06)
        by.setdefault(name, {})[tick] = f
    return metrics, by

def read(f):
    c = f["crop"]
    pixels = np.fromfile(f["path"], np.uint8).reshape(c["height"], c["width"], 4)
    return np.ascontiguousarray(pixels[::-1, :, :3][:, :, ::-1])

def join_image(by, tick):
    return np.concatenate([read(by["1"][tick]), read(by["3"][tick])], axis=1)

def annotate(canvas, yaw, timestamp, region, note):
    cv2.putText(canvas, f"R13 cylindrical | full source 2x MSAA | yaw {yaw:+.2f} | t={timestamp:.0f} ms",
                (12, 25), cv2.FONT_HERSHEY_SIMPLEX, .55, (255,255,255), 1, cv2.LINE_AA)
    cv2.putText(canvas, f"CONTROL | {region}", (12, 48), cv2.FONT_HERSHEY_SIMPLEX, .55, (200,240,255), 1, cv2.LINE_AA)
    cv2.putText(canvas, f"STATIC +1 LEFT SECTOR | {note}", (canvas.shape[1]//2+12, 48),
                cv2.FONT_HERSHEY_SIMPLEX, .55, (200,240,255), 1, cv2.LINE_AA)

summary = {}
for region in runs:
    control_metrics, control = load_frames(runs[region]["control"])
    static_metrics, static = load_frames(runs[region]["static"])
    names = ["1", "3"] if region == "join" else ["1"]
    ticks = sorted(set.intersection(*(set(group[n]) for group in (control, static) for n in names)))
    rows = []
    for tick in ticks:
        c = join_image(control, tick) if region == "join" else read(control["1"][tick])
        s = join_image(static, tick) if region == "join" else read(static["1"][tick])
        diff = np.max(np.abs(c.astype(np.int16)-s.astype(np.int16)), axis=2)
        row = {"tick": tick, "virtualMs": control["1"][tick]["virtualMs"],
               "yaw": control["1"][tick]["intendedYaw"],
               "changedPixelsOver2": int(np.count_nonzero(diff > 2)),
               "changedPixelsOver20": int(np.count_nonzero(diff > 20)),
               "peakChannelDifference": int(diff.max())}
        if region == "join":
            for label, img in (("control", c), ("static", s)):
                left = img[:,191,:].astype(np.float32)
                right = img[:,192,:].astype(np.float32)
                luma = (np.abs(left-right) * np.array([.114,.587,.299])).sum(axis=1)
                row[label+"JoinRowsOver20"] = int(np.count_nonzero(luma > 20))
                row[label+"PeakJoinLuma"] = float(luma.max())
        rows.append(row)
    summary[region] = {"runs": runs[region], "matchedTicks": len(ticks),
                       "controlCallbacks": {n: control_metrics["summaries"][next(k for k in control_metrics["summaries"] if k.endswith(n))]["callbacks"] for n in names},
                       "staticCallbacks": {n: static_metrics["summaries"][next(k for k in static_metrics["summaries"] if k.endswith(n))]["callbacks"] for n in names},
                       "frames": rows}

    clips = (("negative", 0, 650), ("zero", 900, 1150), ("positive", 1440, 1700)) if region == "join" else (
             ("negative", 0, 650), ("zero", 900, 1150), ("positive", 1440, 1700))
    for label, low, high in clips:
        selected = [t for t in ticks if low <= control["1"][t]["virtualMs"] <= high]
        if not selected:
            continue
        shape = join_image(control, selected[0]).shape if region == "join" else read(control["1"][selected[0]]).shape
        width = shape[1]*2
        height = shape[0]+65
        path = out / f"{region}-{label}-control-vs-static.mp4"
        writer = cv2.VideoWriter(str(path), cv2.VideoWriter_fourcc(*"mp4v"), 24, (width,height))
        assert writer.isOpened(), path
        for tick in selected:
            c = join_image(control, tick) if region == "join" else read(control["1"][tick])
            s = join_image(static, tick) if region == "join" else read(static["1"][tick])
            canvas = np.zeros((height,width,3), np.uint8)
            canvas[65:, :shape[1]] = c
            canvas[65:, shape[1]:] = s
            f = control["1"][tick]
            annotate(canvas, f["intendedYaw"], f["virtualMs"], region,
                     "same pose and state")
            for _ in range(3):
                writer.write(canvas)
        writer.release()
        summary[region].setdefault("clips", {})[label] = {"path": str(path), "frames":len(selected),
            "firstMs": control["1"][selected[0]]["virtualMs"],
            "lastMs": control["1"][selected[-1]]["virtualMs"]}

(out / "analysis.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
print({region: {"matchedTicks": summary[region]["matchedTicks"], "clips":summary[region]["clips"]} for region in summary})
