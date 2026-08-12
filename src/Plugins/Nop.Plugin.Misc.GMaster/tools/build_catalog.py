#!/usr/bin/env python3
"""Rebuild the GMaster catalog CSV and product images from the supplier Excel packing lists.

Each workbook has a ``PICTURE`` sheet whose rows carry an OE number, English product name,
fitment description, RMB cost price, and one embedded product photo anchored to the row.

The script is deterministic and idempotent: it rewrites ``Content/gmaster-catalog.csv`` and the
``Content/parts`` image folder from the source workbooks so the dataset can always be regenerated.

Usage:
    python3 build_catalog.py SOURCE.xlsx [SOURCE2.xlsx ...]
"""
from __future__ import annotations

import csv
import hashlib
import io
import re
import sys
from pathlib import Path

import openpyxl
from PIL import Image, ImageFilter

# Storefront canvas for the primary photo. The supplier embeds ~86px thumbnails, so this cannot add
# detail; it resamples cleanly onto a white square instead of letting the theme stretch a tiny bitmap.
CANVAS = 600
CONTENT_SCALE = 0.82

PLUGIN_ROOT = Path(__file__).resolve().parent.parent
CSV_PATH = PLUGIN_ROOT / "Content" / "gmaster-catalog.csv"
IMAGE_DIR = PLUGIN_ROOT / "Content" / "parts"

# Ordered keyword -> category rules. First match wins, so specific terms precede generic ones.
CATEGORY_RULES: list[tuple[str, tuple[str, ...]]] = [
    ("cooling-system", (
        "radiator", "thermostat", "water pump", "coolant", "auxiliary water", "water pipe",
        "water tank", "oil cooler", "water hose", "expansion tank", "water pipe connector",
        "cooling", "fan", "pump core", "connecting pipe", "cylinder block water",
        "expansion valve", "heat exchanger", "inlet pipe", "return line", "exhaust hose",
    )),
    ("fuel-air-intake", (
        "fuel pump", "intake", "mass air flow", "egr", "air flow", "throttle", "filter",
        "vacuum pump", "injector", "carburettor", "fuel", "air pipe", "manifold",
    )),
    ("engine-components", (
        "valve cover", "cylinder head", "oil pan", "gasket", "crankshaft", "camshaft", "piston",
        "timing", "pulley", "seal", "oil pump", "engine cover", "tensioner", "chain",
        "belt", "flange", "repair kit", "transmission oil pipe",
    )),
    ("engine-transmission-mounts", (
        "engine mount", "transmission rubber", "gearbox mount", "mount rubber", "mounting",
    )),
    ("suspension", (
        "control arm", "stabilizer", "shock absorber", "spring pad", "buffer rubber", "strut",
        "swing arm", "ball joint", "bushing", "sway bar", "top rubber", "lower spring",
        "arm bushing",
    )),
    ("steering", (
        "steering", "tie rod", "rack", "steering rack",
    )),
    ("braking", (
        "brake", "caliper", "disc", "abs",
    )),
    ("driveline-hub", (
        "wheel hub", "hub", "bearing", "cv joint", "drive shaft", "axle", "half shaft",
        "half - shaft", "shaft assembly",
    )),
    ("sensors-electrical", (
        "sensor", "switch", "ignition coil", "spark", "solenoid", "actuator", "relay",
        "control unit", "oxygen", "airbag", "clock spring",
    )),
    ("exterior-body", (
        "spoiler", "grill", "grille", "bonnet", "tailgate", "stay rod", "fender", "bumper",
        "mirror", "hood", "wheel arch", "kidney",
    )),
    ("interior-trim", (
        "armrest", "door handle", "cup holder", "cupholder", "panel", "vent", "gear lever",
        "handle set", "trim", "reading lamp", "glove", "window regulator", "handlebar cover",
        "inner handle", "handle cover", "dust cover",
    )),
]

# English base-noun -> Arabic. Applied by longest-substring match; unmatched names keep English.
ARABIC_LEXICON: dict[str, str] = {
    "fuel pump assembly": "طلمبة بنزين كاملة",
    "electric water pump": "طلمبة مياه كهربائية",
    "auxiliary water pump": "طلمبة مياه مساعدة",
    "water pump": "طلمبة مياه",
    "auxiliary water tank water pipe": "ماسورة خزان مياه مساعد",
    "auxiliary water tank": "خزان مياه مساعد",
    "radiator water pipe": "ماسورة رادياتير",
    "radiator assembly box": "علبة رادياتير كاملة",
    "radiator cap": "غطاء رادياتير",
    "radiator": "رادياتير",
    "thermostat - thermal energy module": "حساس حرارة - وحدة طاقة حرارية",
    "thermostat": "ثرموستات",
    "coolant hose": "خرطوم تبريد",
    "upper water pipe": "ماسورة مياه علوية",
    "cylinder block water pipe": "ماسورة مياه بلوك",
    "water pipe connector": "وصلة ماسورة مياه",
    "connecting pipe": "ماسورة وصل",
    "transmission oil cooler": "مبرد زيت الفتيس",
    "oil cooler": "مبرد زيت",
    "pump core": "قلب طلمبة",
    "valve cover gasket": "جوان غطاء التاكيهات",
    "valve cover": "غطاء التاكيهات",
    "cylinder head gasket": "جوان وش السلندر",
    "oil pan gasket": "جوان كرتير الزيت",
    "crankshaft front oil seal": "جوان أمامي لعمود الكرنك",
    "seal": "جوان",
    "pulley": "بكرة",
    "vacuum pump": "طلمبة تخلية",
    "intake pipe": "ماسورة سحب",
    "mass air flow sensor": "حساس هواء",
    "exhaust gas recirculation valve (egr valve)": "صمام إعادة تدوير العادم EGR",
    "filter": "فلتر",
    "engine mount rubber l": "كرسي مكينة شمال",
    "engine mount rubber r": "كرسي مكينة يمين",
    "engine mount rubber": "كرسي مكينة",
    "transmission rubber": "كرسي فتيس",
    "front stabilizer bar bushing": "جلبة موازن أمامي",
    "front shock absorber top rubber": "كرسي مساعد أمامي",
    "front buffer rubber": "مطاط صدمات أمامي",
    "lower spring pad": "قاعدة ياي سفلية",
    "control arm bushing - large": "جلبة مقص كبيرة",
    "front wheel lower control arm l - straight": "مقص سفلي أمامي شمال - مستقيم",
    "front wheel lower control arm r - straight": "مقص سفلي أمامي يمين - مستقيم",
    "front wheel lower control arm l - bend": "مقص سفلي أمامي شمال - منحني",
    "front wheel lower control arm r - bend": "مقص سفلي أمامي يمين - منحني",
    "front wheel lower control arm l": "مقص سفلي أمامي شمال",
    "front wheel lower control arm r": "مقص سفلي أمامي يمين",
    "steering tie rod l": "ذراع تعشيق شمال",
    "steering tie rod r": "ذراع تعشيق يمين",
    "steering rack": "علبة دركسيون",
    "front brake hose": "خرطوم فرامل أمامي",
    "rear brake hose": "خرطوم فرامل خلفي",
    "front wheel hub": "رمان بلي أمامي",
    "rear oxygen sensor": "حساس أكسجين خلفي",
    "window switch": "مفتاح زجاج",
    "front left window switch": "مفتاح زجاج أمامي شمال",
    "bonnet stay rod": "ذراع دعم كبوت",
    "tailgate stay rod": "ذراع دعم باب خلفي",
    "spoiler": "سبويلر",
    "intake manifold": "مجمع سحب",
    "pipe fitting": "وصلة مواسير",
    "front grill": "شبك أمامي",
    "door handle set": "طقم مقابض أبواب",
    "armrest lid panel": "غطاء مسند ذراع",
}

CHASSIS_RE = re.compile(r"\b([EFG]\d{2}|X\d|F\d{2}|N\d{2}|B\d{2}|S\d{2})\b", re.IGNORECASE)


def classify(name: str, description: str) -> str:
    haystack = f"{name} {description}".lower()
    for key, keywords in CATEGORY_RULES:
        if any(word in haystack for word in keywords):
            return key
    return "misc-accessories"


def to_arabic(name: str) -> str:
    lowered = name.lower().strip()
    if lowered in ARABIC_LEXICON:
        return ARABIC_LEXICON[lowered]
    for term in sorted(ARABIC_LEXICON, key=len, reverse=True):
        if term in lowered:
            return ARABIC_LEXICON[term]
    return name.strip()


def chassis_codes(description: str) -> str:
    seen: list[str] = []
    for match in CHASSIS_RE.findall(description or ""):
        code = match.upper()
        if code not in seen:
            seen.append(code)
    return ",".join(seen[:12])


def normalize_image(data: bytes) -> bytes | None:
    """Resample a supplier thumbnail onto a clean white square canvas.

    This does not invent detail. It avoids the browser/theme upscaling an 86px bitmap directly,
    which is what made the grid look like pixel mush.
    """
    try:
        with Image.open(io.BytesIO(data)) as source:
            image = source.convert("RGBA")
    except Exception:
        return None

    # Drop a fully transparent border so the part fills the canvas predictably.
    alpha = image.split()[-1]
    box = alpha.getbbox()
    if box:
        image = image.crop(box)

    flat = Image.new("RGB", image.size, (255, 255, 255))
    flat.paste(image, mask=image.split()[-1])

    target = int(CANVAS * CONTENT_SCALE)
    ratio = min(target / flat.width, target / flat.height)
    size = (max(1, round(flat.width * ratio)), max(1, round(flat.height * ratio)))
    resized = flat.resize(size, Image.LANCZOS)
    resized = resized.filter(ImageFilter.UnsharpMask(radius=1.6, percent=110, threshold=3))

    canvas = Image.new("RGB", (CANVAS, CANVAS), (255, 255, 255))
    canvas.paste(resized, ((CANVAS - size[0]) // 2, (CANVAS - size[1]) // 2))

    buffer = io.BytesIO()
    canvas.save(buffer, format="JPEG", quality=88, optimize=True)
    return buffer.getvalue()


def normalize_oem(raw: str) -> str:
    return re.sub(r"[^0-9A-Za-z]", "", str(raw or "")).upper()


def clean(text) -> str:
    if text is None:
        return ""
    return re.sub(r"\s+", " ", str(text).replace("\xa0", " ")).strip()


def image_rows(ws) -> dict[int, object]:
    mapping: dict[int, object] = {}
    for image in getattr(ws, "_images", []):
        row = image.anchor._from.row + 1
        mapping.setdefault(row, image)
    return mapping


def header_index(ws) -> dict[str, int]:
    index: dict[str, int] = {}
    for col in range(1, ws.max_column + 1):
        label = clean(ws.cell(1, col).value).upper()
        if label:
            index[label] = col
    return index


def main(paths: list[str]) -> None:
    for existing in IMAGE_DIR.glob("*"):
        existing.unlink()
    IMAGE_DIR.mkdir(parents=True, exist_ok=True)

    rows_out: list[dict[str, str]] = []
    seen_keys: set[str] = set()
    used_skus: set[str] = set()
    category_counts: dict[str, int] = {}
    with_image = 0

    for path in paths:
        source = Path(path)
        container = source.name.split("_")[0]
        workbook = openpyxl.load_workbook(source)
        ws = workbook["PICTURE"]
        headers = header_index(ws)
        name_col = headers.get("PRODUCT NAME", 3)
        desc_col = headers.get("CAR FITMENT") or headers.get("DESCRIPTON") or headers.get("DESCRIPTION", 4)
        price_col = headers.get("PRICE(RMB)", 9)
        images = image_rows(ws)

        for row in range(2, ws.max_row + 1):
            oem_raw = clean(ws.cell(row, 1).value)
            name_en = clean(ws.cell(row, name_col).value)
            if not oem_raw or not name_en:
                continue
            description = clean(ws.cell(row, desc_col).value)
            price = ws.cell(row, price_col).value
            try:
                cost_rmb = float(price)
            except (TypeError, ValueError):
                continue
            if cost_rmb <= 0:
                continue

            dedupe_key = f"{normalize_oem(oem_raw)}|{name_en.lower()}|{description.lower()}|{cost_rmb}"
            if dedupe_key in seen_keys:
                continue
            seen_keys.add(dedupe_key)

            base_sku = f"GM-{normalize_oem(oem_raw)}" if normalize_oem(oem_raw) else "GM-" + hashlib.md5(name_en.encode()).hexdigest()[:8].upper()
            sku = base_sku
            if sku in used_skus:
                suffix = hashlib.md5(dedupe_key.encode()).hexdigest()[:4].upper()
                sku = f"{base_sku}-{suffix}"
                while sku in used_skus:
                    suffix = hashlib.md5((dedupe_key + sku).encode()).hexdigest()[:4].upper()
                    sku = f"{base_sku}-{suffix}"
            used_skus.add(sku)

            category = classify(name_en, description)
            category_counts[category] = category_counts.get(category, 0) + 1

            # Primary photo is always "<SKU>-1.jpg". Additional licensed photos can be dropped in as
            # "<SKU>-2.jpg" / "<SKU>-3.jpg"; the importer discovers them automatically.
            image_file = ""
            image = images.get(row)
            if image is not None:
                normalized = normalize_image(image._data())
                if normalized is not None:
                    image_file = f"{sku}-1.jpg"
                    (IMAGE_DIR / image_file).write_bytes(normalized)
                    with_image += 1

            rows_out.append({
                "sku": sku,
                "name_ar": to_arabic(name_en),
                "name_en": name_en.title() if name_en.isupper() else name_en,
                "oem": oem_raw,
                "vehicle_models": chassis_codes(description),
                "category_key": category,
                "cost_rmb": f"{cost_rmb:g}",
                "image_file": image_file,
                "source_file": container,
            })

    fieldnames = ["sku", "name_ar", "name_en", "oem", "vehicle_models", "category_key",
                  "cost_rmb", "image_file", "source_file"]
    with CSV_PATH.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows_out)

    print(f"rows written: {len(rows_out)}")
    print(f"rows with image: {with_image}")
    print(f"categories: {len(category_counts)}")
    for key in sorted(category_counts, key=lambda k: -category_counts[k]):
        print(f"  {category_counts[key]:4d}  {key}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise SystemExit("provide at least one source .xlsx path")
    main(sys.argv[1:])
