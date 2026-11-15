"""Copy the R20 developer bundles for bounded, pose-matched R21 capture."""
from pathlib import Path
import hashlib
import json
import shutil

root = Path(__file__).resolve().parent
out = root / "r21-variants"
old_trajectory = "o=Math.min(4e3,Math.max(0,t-$.epochMs)),s=o<=2e3?-10+o/100:30-o/100"
new_trajectory = (
    "o=Math.min(4e3,Math.max(0,Math.round((t-$.epochMs)/"
    "(1e3/60))*(1e3/60))),s=o<=2e3?-12+o*.012:36-o*.012"
)
old_crop = "Z===`left`?{x:zm.width-192,y:800,width:192,height:550}"
interior_crop = "Z===`left`?{x:3040,y:900,width:560,height:280}"
manifest = []

for source in ("control-2x", "static-2x"):
    for region in ("join", "interior"):
        target = out / f"{source}-{region}"
        if target.exists():
            raise SystemExit(f"Refusing to overwrite {target}")
        shutil.copytree(root / "r20-variants" / source, target)
        bundle = next(p for p in target.glob("assets/index-*.js") if "__b1Start" in p.read_text(encoding="utf-8"))
        text = bundle.read_text(encoding="utf-8")
        for token in (old_trajectory, old_crop, "$.capacity=256"):
            assert text.count(token) == 1, (bundle, token, text.count(token))
        text = text.replace(old_trajectory, new_trajectory)
        if region == "interior":
            text = text.replace(old_crop, interior_crop)
        text = text.replace("$.capacity=256", "$.capacity=320")
        bundle.write_text(text, encoding="utf-8")
        manifest.append({"source": source, "region": region, "bundle": str(bundle),
                         "sha256": hashlib.sha256(bundle.read_bytes()).hexdigest(),
                         "trajectory": "-12 to +12 to -12 degrees / 4 seconds; 60 Hz quantized virtual time",
                         "crop": "left interior 3040,900 560x280" if region == "interior" else
                                 "left join 3648,800 192x550; primary join 0,80 192x550"})

(out / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
