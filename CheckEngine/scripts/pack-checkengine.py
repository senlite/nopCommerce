#!/usr/bin/env python3
"""Pack TwinParticles.CheckEngine.{version}.zip for drop-in Plugins/ install (G11 / AC-33.4).

Does not sign the artefact. Production licence vendor signing remains an external G11 gate.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

PLUGIN_RELATIVE = Path("src/Plugins/TwinParticles.CheckEngine")
OUTPUT_RELATIVE = Path("src/Presentation/Nop.Web/Plugins/TwinParticles.CheckEngine")
FOLDER_NAME = "TwinParticles.CheckEngine"

ALLOWED_DLL_PREFIXES = ("TwinParticles.CheckEngine",)
ALLOWED_DIRS = ("Content", "Views")
REQUIRED_FILES = (
    "plugin.json",
    "TwinParticles.CheckEngine.dll",
    "TwinParticles.CheckEngine.Domain.dll",
    "TwinParticles.CheckEngine.Application.dll",
    "TwinParticles.CheckEngine.Infrastructure.dll",
)
FORBIDDEN_NAME_PARTS = (
    "nop.web",  # host Nop.Web binaries and staticwebassets
    "app_data",
    "appsettings",
    "connectionstring",
    "runtimeconfig",
    "staticwebassets",
    ".pdb",
)
FORBIDDEN_ROOT_DIRS = {"runtimes", "App_Data", "bin", "obj"}


def repo_root() -> Path:
    here = Path(__file__).resolve()
    return here.parents[2]


def read_plugin_version(root: Path) -> str:
    plugin_json = root / PLUGIN_RELATIVE / "plugin.json"
    csproj = root / PLUGIN_RELATIVE / "TwinParticles.CheckEngine.csproj"
    version = json.loads(plugin_json.read_text(encoding="utf-8"))["Version"]
    csproj_text = csproj.read_text(encoding="utf-8")
    if f"<Version>{version}</Version>" not in csproj_text:
        raise SystemExit(f"[pack] plugin.json Version {version} does not match csproj")
    return version


def build_plugin(root: Path, configuration: str) -> None:
    csproj = root / PLUGIN_RELATIVE / "TwinParticles.CheckEngine.csproj"
    print(f"[pack] building {csproj} -c {configuration}")
    subprocess.run(
        ["dotnet", "build", str(csproj), "-c", configuration],
        cwd=root,
        check=True,
    )


def is_forbidden(relative: Path) -> bool:
    parts_lower = [part.lower() for part in relative.parts]
    if parts_lower and parts_lower[0] in {name.lower() for name in FORBIDDEN_ROOT_DIRS}:
        return True
    joined = str(relative).replace("\\", "/").lower()
    return any(token in joined for token in FORBIDDEN_NAME_PARTS)


def stage_files(output_dir: Path, stage_dir: Path) -> list[Path]:
    if not (output_dir / "plugin.json").is_file():
        raise SystemExit(f"[pack] plugin output missing at {output_dir}; build the plugin first")

    if stage_dir.exists():
        shutil.rmtree(stage_dir)
    stage_dir.mkdir(parents=True)

    copied: list[Path] = []
    plugin_json = output_dir / "plugin.json"
    target = stage_dir / "plugin.json"
    shutil.copy2(plugin_json, target)
    copied.append(target)

    for dll in sorted(output_dir.glob("*.dll")):
        if dll.name.startswith(ALLOWED_DLL_PREFIXES):
            dest = stage_dir / dll.name
            shutil.copy2(dll, dest)
            copied.append(dest)

    for folder in ALLOWED_DIRS:
        source = output_dir / folder
        if not source.is_dir():
            raise SystemExit(f"[pack] required folder missing: {source}")
        dest = stage_dir / folder
        shutil.copytree(source, dest)
        copied.extend(path for path in dest.rglob("*") if path.is_file())

    for required in REQUIRED_FILES:
        if not (stage_dir / required).is_file():
            raise SystemExit(f"[pack] required file missing from stage: {required}")

    return copied


def write_zip(stage_parent: Path, zip_path: Path) -> None:
    if zip_path.exists():
        zip_path.unlink()
    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for file_path in sorted((stage_parent / FOLDER_NAME).rglob("*")):
            if not file_path.is_file():
                continue
            archive.write(file_path, file_path.relative_to(stage_parent).as_posix())


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def scan_zip(zip_path: Path) -> list[str]:
    names: list[str] = []
    with zipfile.ZipFile(zip_path) as archive:
        for info in archive.infolist():
            names.append(info.filename.replace("\\", "/"))
    forbidden = [name for name in names if is_forbidden(Path(name))]
    if forbidden:
        raise SystemExit("[pack] forbidden entries in zip:\n  " + "\n  ".join(forbidden))
    prefix = f"{FOLDER_NAME}/"
    if any(not name.startswith(prefix) for name in names):
        raise SystemExit("[pack] zip must contain a single TwinParticles.CheckEngine/ root")
    for required in REQUIRED_FILES:
        expected = f"{prefix}{required}"
        if expected not in names:
            raise SystemExit(f"[pack] zip missing {expected}")
    return names


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", action="store_true", help="dotnet build the plugin before packing")
    parser.add_argument("--configuration", default=os.environ.get("CONFIGURATION", "Release"))
    parser.add_argument(
        "--out-dir",
        default=os.environ.get("CHECKENGINE_PACK_OUT", "/tmp/checkengine-pack"),
        help="Directory for the zip, checksum, and manifest",
    )
    args = parser.parse_args()

    root = repo_root()
    version = read_plugin_version(root)
    if args.build:
        build_plugin(root, args.configuration)

    out_dir = Path(args.out_dir)
    if not out_dir.is_absolute():
        out_dir = root / out_dir
    out_dir.mkdir(parents=True, exist_ok=True)
    stage_dir = out_dir / FOLDER_NAME
    zip_path = out_dir / f"{FOLDER_NAME}.{version}.zip"

    print(f"[pack] staging {version} from {OUTPUT_RELATIVE}")
    stage_files(root / OUTPUT_RELATIVE, stage_dir)
    write_zip(out_dir, zip_path)
    names = scan_zip(zip_path)
    checksum = sha256_file(zip_path)
    checksum_path = Path(str(zip_path) + ".sha256")
    checksum_path.write_text(f"{checksum}  {zip_path.name}\n", encoding="utf-8")
    manifest = {
        "nfr": "G11",
        "systemName": FOLDER_NAME,
        "version": version,
        "zip": zip_path.name,
        "sha256": checksum,
        "files": len(names),
        "bytes": zip_path.stat().st_size,
        "signed": False,
        "note": "Unsigned operator artefact. Production licence vendor signing is an external G11 gate.",
    }
    manifest_path = out_dir / f"{FOLDER_NAME}.{version}.manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"[pack] wrote {zip_path} ({manifest['bytes']} bytes, {manifest['files']} files)")
    print(f"[pack] sha256 {checksum}")
    print(f"[pack] manifest {manifest_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
