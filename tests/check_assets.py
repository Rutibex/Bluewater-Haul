"""Small dependency-free integrity check for the assets shipped with the game."""
from pathlib import Path
import json
import struct

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "Source" / "Data"


def read_json(filename):
    with (DATA / filename).open(encoding="utf-8") as handle:
        return json.load(handle)


def main():
    world = read_json("world.json")
    places = read_json("landmarks.json")
    jobs = read_json("jobs.json")

    assert world.get("version") is not None, "Map version missing"
    assert len(world.get("bounds", [])) == 4, "Map bounds missing"
    for key in ("roads", "buildings", "areas", "pois", "trees", "addresses"):
        assert isinstance(world.get(key), list) and world[key], f"Map missing {key}"
    assert len(places) >= 13, "Missing landmark notes"
    assert len(jobs) >= 5, "Missing playable delivery contracts"
    for place in places:
        assert place.get("name") and len(place.get("point", [])) == 2
        assert place.get("source", "").startswith("https://"), place.get("name")
    for job in jobs:
        assert job.get("title") and len(job.get("point", [])) == 2
        assert job.get("pay", 0) > 0
    image = (DATA / "font.png").read_bytes()
    assert image.startswith(b"\x89PNG\r\n\x1a\n"), "Font atlas is not a PNG"
    width, height = struct.unpack(">II", image[16:24])
    assert width > 0 and height > 0
    assert (ROOT / "MapSource/sarnia.osm.pbf").stat().st_size > 100000
    for path in ("LICENSE", "THIRD_PARTY_NOTICES.md", "Source/FONT_LICENSE.txt"):
        assert (ROOT / path).is_file(), f"Missing legal notice: {path}"
    print(
        f"OK: {len(world['roads']):,} roads, {len(world['buildings']):,} "
        f"buildings, {len(places)} landmarks, {len(jobs)} deliveries; "
        f"PNG {width}x{height}; licences present."
    )


if __name__ == "__main__":
    main()
