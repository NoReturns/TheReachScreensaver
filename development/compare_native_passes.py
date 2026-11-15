"""Compare matched 26-radii native pass manifests without changing captures."""

import hashlib
import json
import sys
from pathlib import Path

import numpy as np


def luminance(rgb):
    return float(np.dot(rgb, [0.2126, 0.7152, 0.0722]))


def read(path):
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    state = json.loads(data["stateKey"])
    if state["id"] != "hole-26-left" or data["fixtureId"] != "hole-26-left":
        raise ValueError("Expected the 26-radii left seam fixture")
    state.pop("post")
    geometry_key = json.dumps(state, sort_keys=True, separators=(",", ":"))
    samples = {s["fixture"]["display"]: s["fixture"]["nativeFixture"]
               for s in data["statuses"]}
    left, primary = samples["left"]["hdrSeam"], samples["primary"]["hdrSeam"]
    if samples["left"]["hdrError"] or samples["primary"]["hdrError"]:
        raise ValueError("Native HDR readback failed")
    if left["format"] != "half-float" or primary["format"] != "half-float":
        raise ValueError("Native readback was not half-float")
    rows_left = {row["y"]: row["pixels"] for row in left["rows"]}
    rows_primary = {row["y"]: row["pixels"] for row in primary["rows"]}
    if rows_left.keys() != rows_primary.keys():
        raise ValueError("Half-float rows do not cover matching absolute coordinates")
    impulses = []
    jumps = []
    examples = {}
    for y in sorted(rows_left):
        near_left, edge_left = rows_left[y]
        edge_primary, near_primary = rows_primary[y]
        jump = abs(luminance(edge_left) - luminance(edge_primary))
        baseline = (abs(luminance(near_left) - luminance(edge_left))
                    + abs(luminance(edge_primary) - luminance(near_primary))) / 2
        jumps.append(jump)
        impulses.append(max(0, jump - baseline))
        if y in (1050, 1200):
            examples[y] = {"left3839": edge_left, "primary3840": edge_primary,
                           "luminanceJump": round(jump, 5)}
    return (geometry_key, data["mode"], data["post"], {
        "manifest": str(Path(path).resolve()),
        "halfFloatMeanJump": round(float(np.mean(jumps)), 5),
        "halfFloatMeanImpulse": round(float(np.mean(impulses)), 5),
        "halfFloatMaxImpulse": round(float(np.max(impulses)), 5),
        "absoluteRowsAboveImpulse0_1": [800 + i for i, value in enumerate(impulses)
                                       if value > 0.1],
        "examples": examples,
    })


def main(paths):
    if len(paths) != 8:
        raise ValueError("Expected rectilinear and cylindrical manifests for four passes")
    records = [read(path) for path in paths]
    if len({item[0] for item in records}) != 1:
        raise ValueError("Pass variants changed the synthetic geometry state")
    output = {f"{mode}-{post}": result for _, mode, post, result in records}
    if len(output) != 8:
        raise ValueError("Missing or duplicate projection/pass variant")
    destination = Path(next(path for path in paths if "cylindrical-final" in Path(path).name))
    saved = destination.parent / "pass-comparison.json"
    saved.write_text(json.dumps({"geometryStateSha256": hashlib.sha256(
        records[0][0].encode()).hexdigest(), "passes": output}, indent=2), encoding="utf-8")
    print(json.dumps({key: {field: value for field, value in result.items()
                             if field not in ("manifest", "absoluteRowsAboveImpulse0_1")}
                      for key, result in output.items()}, separators=(",", ":")))
    print(saved)


if __name__ == "__main__":
    main(sys.argv[1:])
