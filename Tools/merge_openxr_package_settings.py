#!/usr/bin/env python3
"""
Merge two Unity Open XR Package Settings.asset YAML files (e.g. from pico and htc branches).
Unions MonoBehaviour blocks by ID and merges the Android platform features list.
Usage:
  python3 Tools/merge_openxr_package_settings.py openxr_a.asset openxr_b.asset out.asset
"""
import re
import sys


def load_blocks(text: str):
    m = re.search(r"^--- !u!114", text, re.M)
    if not m:
        return "", {}
    header = text[: m.start()]
    rest = text[m.start() :]
    parts = re.split(r"(--- !u!114[^\n]+\n)", rest)
    blocks = {}
    i = 1
    while i < len(parts):
        delim = parts[i]
        body = parts[i + 1] if i + 1 < len(parts) else ""
        mid = re.search(r"!u!114\s+&(-?\d+)", delim)
        bid = mid.group(1) if mid else delim
        blocks[bid] = delim + body
        i += 2
    return header, blocks


def extract_android_features(block: str):
    m = re.search(
        r"(  features:\n)(.*?)(  m_renderMode:)", block, re.S
    )
    if not m:
        return []
    return re.findall(r"fileID:\s*(-?\d+)", m.group(2))


def merge_android_features(block_a: str, block_b: str) -> str:
    ids_a = extract_android_features(block_a)
    ids_b = extract_android_features(block_b)
    seen = set()
    union = []
    for idlist in (ids_a, ids_b):
        for x in idlist:
            if x not in seen:
                seen.add(x)
                union.append(x)
    new_features = "  features:\n" + "".join(
        f"  - {{fileID: {fid}}}\n" for fid in union
    )
    return re.sub(
        r"(  features:\n)(.*?)(  m_renderMode:)",
        lambda m: new_features + m.group(3),
        block_a,
        count=1,
        flags=re.S,
    )


def main():
    if len(sys.argv) != 4:
        print(__doc__)
        sys.exit(2)
    a, b, out = sys.argv[1:4]
    t1 = open(a, encoding="utf-8").read()
    t2 = open(b, encoding="utf-8").read()
    h1, b1 = load_blocks(t1)
    _, b2 = load_blocks(t2)
    merged = dict(b1)
    for bid, blk in b2.items():
        if bid not in merged:
            merged[bid] = blk
    aid = "-64324148185763206"
    if aid in merged and aid in b2:
        merged[aid] = merge_android_features(b1.get(aid, ""), b2.get(aid, ""))
    with open(out, "w", encoding="utf-8") as f:
        f.write(h1 + "".join(merged.values()))


if __name__ == "__main__":
    main()
