#!/usr/bin/env python3
"""skizzy-naval build tool.

  python scripts/skizzy.py list
  python scripts/skizzy.py check
  python scripts/skizzy.py import KEY --patched DIR [--archive ZIP] [--force] [--allow-modified]
  python scripts/skizzy.py manifest KEY [--archive ZIP]
  python scripts/skizzy.py package --version 0.1.0 [--only KEY ...] [--archive KEY=ZIP ...]
  python scripts/skizzy.py ckan --version 0.1.0 [--draft] [--only KEY ...]
  python scripts/skizzy.py ckan --only SubTrim --include-disabled --out ../skizzy-ckan
  python scripts/skizzy.py validate [PATH ...] [--schema FILE_OR_URL]
  python scripts/skizzy.py verify-downloads PATH ...
  python scripts/skizzy.py notes --tag v0.1.0

Run with Python 3.11+. See RELEASING.md.
"""
from __future__ import annotations

import argparse
import sys
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from skizzylib import build, ckan  # noqa: E402
from skizzylib.registry import check_registry, load_registry  # noqa: E402
from skizzylib.util import (MODULES_DIR, REPO_ROOT, SkizzyError, copy_file, hash_file,  # noqa: E402
                            log, matches_any, read_json, rmtree, tree_manifest, walk_files, warn)

DEFAULT_SCHEMA = "https://raw.githubusercontent.com/KSP-CKAN/CKAN/master/CKAN.schema"


def _pairs(values: list[str] | None, flag: str) -> dict[str, str]:
    out = {}
    for value in values or []:
        if "=" not in value:
            raise SkizzyError(f"{flag} expects KEY=PATH, got '{value}'")
        key, path = value.split("=", 1)
        out[key] = path
    return out


def _release_version(args) -> str:
    version = args.version
    if version.startswith("v"):
        version = version[1:]
    return version


# --------------------------------------------------------------------------- commands

def cmd_list(reg, args) -> int:
    for m in reg.modules:
        state = "on " if m.enabled else "off"
        lic = m.get("license")
        lic = ", ".join(lic) if isinstance(lic, list) else lic
        log(f"{state} {m.key:28s} {m.identifier:40s} {m.strategy:11s} pack={m.pack:10s} {lic}")
    return 0


def cmd_check(reg, args) -> int:
    problems = check_registry(reg)
    max_bytes = int(float(reg.policy.get("max_file_mb", 95)) * 1024 * 1024)
    forbidden = reg.policy.get("forbidden", [])
    known = {m.key for m in reg.modules}
    for folder in sorted(p for p in MODULES_DIR.iterdir() if p.is_dir()):
        if folder.name not in known:
            problems.append(f"modules/{folder.name}/ has no registry entry")
    for rel, path in walk_files(REPO_ROOT).items():
        if rel.startswith((".git/", "dist/", "build/", ".cache/")) or "/__pycache__/" in rel:
            continue
        if path.stat().st_size > max_bytes:
            problems.append(f"{rel} is larger than {max_bytes // (1024 * 1024)} MB")
    for m in reg.modules:
        payload = walk_files(m.gamedata_dir)
        for rel in payload:
            if matches_any(rel, forbidden):
                problems.append(f"{m.key}: third-party library in GameData: {rel}")
        if not m.manifest_path.exists() or m.strategy in ("stub", "metapackage"):
            continue
        manifest = read_json(m.manifest_path)["files"]
        for rel in manifest:
            if matches_any(rel, forbidden):
                problems.append(f"{m.key}: manifest ships a third-party library: {rel}")
        committed = tree_manifest(m.gamedata_dir)
        committed = {f"GameData/{k}": v for k, v in committed.items()}
        if m.strategy in ("bundle", "overlay"):
            for p in build.compare_to_manifest(committed, manifest):
                problems.append(f"{m.key}: GameData vs manifest.json: {p}")
        else:  # rebuild: every committed file must land in the result unchanged
            for rel, entry in committed.items():
                if manifest.get(rel) != entry:
                    problems.append(f"{m.key}: overlay file not in manifest as-is: {rel}")
            for rel in m.delete_list():
                if rel in manifest:
                    problems.append(f"{m.key}: delete.txt entry is in the manifest: {rel}")
        if m.enabled and not any((m.folder / n).is_file() for n in ("LICENSE", "LICENSE.txt", "LICENSE.md")):
            problems.append(f"{m.key}: no LICENSE file in modules/{m.key}/")
    for p in problems:
        log(f"problem: {p}")
    log(f"check: {len(reg.modules)} modules, {len(problems)} problems")
    return 1 if problems else 0


def cmd_import(reg, args) -> int:
    """Populate modules/<key>/ from a patched tree (a folder that contains GameData/)."""
    mod = reg.get(args.key)
    patched_gd = Path(args.patched) / "GameData"
    if not patched_gd.is_dir():
        raise SkizzyError(f"{patched_gd} does not exist")
    if mod.gamedata_dir.exists() and not args.force:
        raise SkizzyError(f"modules/{mod.key}/GameData exists; pass --force to replace it")
    patched = {f"GameData/{rel}": p for rel, p in walk_files(patched_gd).items()}
    shipped: dict[str, Path] = {}
    deleted: list[str] = []
    not_shipped: list[str] = []
    if mod.strategy == "bundle":
        shipped = dict(patched)
        manifest = tree_manifest(patched_gd)
        manifest = {f"GameData/{k}": v for k, v in manifest.items()}
        note = "Complete payload, committed in GameData/."
    elif mod.strategy in ("overlay", "rebuild"):
        local = {mod.key: args.archive} if args.archive else {}
        archive = build.fetch_upstream(mod, Path(args.cache), local)
        zf, entries = build.upstream_entries(archive, mod.upstream)
        with zf:
            upstream = build.upstream_hashes(zf, entries)
        # Files under the extract targets or the install folders belong to this module.
        prefixes = [r["to"].strip("/") for r in mod.upstream["extract"]]
        prefixes += [d["file"].strip("/") for d in mod.get("install", []) if "file" in d]
        in_scope = {rel: p for rel, p in patched.items()
                    if any(not px or rel == px or rel.startswith(px + "/") for px in prefixes)}
        ignored = sorted(set(patched) - set(in_scope))
        if ignored:
            tops = sorted({"/".join(r.split("/")[:2]) for r in ignored})
            warn(f"{mod.key}: ignoring patched files outside the extract rules: {tops}")
        actual = {rel: list(hash_file(p)[::2]) for rel, p in in_scope.items()}
        lower_up = {k.lower(): k for k in upstream}
        for rel in actual:
            if rel not in upstream and rel.lower() in lower_up:
                raise SkizzyError(f"case mismatch: patched '{rel}' vs upstream '{lower_up[rel.lower()]}'")
        added = [r for r in actual if r not in upstream]
        changed = [r for r in actual if r in upstream and actual[r] != upstream[r]]
        deleted = [r for r in upstream if r not in actual]
        log(f"{mod.key}: {len(added)} added, {len(changed)} changed, {len(deleted)} deleted, "
            f"{len(actual) - len(added) - len(changed)} unchanged")
        if mod.strategy == "overlay":
            if (changed or deleted) and not args.allow_modified:
                raise SkizzyError(f"{mod.key}: an overlay can only add files; changed={changed} "
                                  f"deleted={deleted}. Use --allow-modified to skip them.")
            not_shipped = changed + [f"(deleted) {r}" for r in deleted]
            shipped = {r: in_scope[r] for r in added}
            manifest = {r: actual[r] for r in added}
            note = "Overlay: only these files are shipped; users install the original separately."
            deleted = []
        else:
            shipped = {r: in_scope[r] for r in added + changed}
            manifest = actual
            note = ("Expected result after extracting the upstream archive, applying delete.txt and "
                    "copying GameData/ on top.")
    else:
        raise SkizzyError(f"{mod.key}: strategy '{mod.strategy}' has nothing to import")
    rmtree(mod.gamedata_dir)
    for rel, src in shipped.items():
        if not rel.startswith("GameData/"):
            raise SkizzyError(f"overlay files must live under GameData/: {rel}")
        copy_file(src, mod.gamedata_dir / rel[len("GameData/"):])
    if mod.strategy == "rebuild":
        header = ("# Files removed from the upstream archive before the overlay is applied.\n"
                  "# One path per line, relative to the zip root of the release asset.\n")
        mod.delete_path.write_text(header + "".join(f"{r}\n" for r in sorted(deleted)),
                                   encoding="utf-8")
    if not_shipped:
        (mod.folder / "not-shipped.txt").write_text(
            "# Upstream files our patch edits but an overlay can't replace (see README.md).\n"
            + "".join(f"{r}\n" for r in not_shipped), encoding="utf-8")
    build.write_manifest(mod, dict(sorted(manifest.items())), note)
    log(f"{mod.key}: wrote {len(shipped)} payload files and manifest.json ({len(manifest)} files)")
    return 0


def cmd_manifest(reg, args) -> int:
    """Rebuild manifest.json from the current overlay (after editing a payload file)."""
    mod = reg.get(args.key)
    local = {mod.key: args.archive} if args.archive else {}
    stage = build.stage_module(reg, mod, Path(args.build), Path(args.cache), local, {})
    files = tree_manifest(stage)
    default_notes = {
        "bundle": "Complete payload, committed in GameData/.",
        "overlay": "Overlay: only these files are shipped; users install the original separately.",
        "rebuild": ("Expected result after extracting the upstream archive, applying delete.txt and "
                    "copying GameData/ on top."),
    }
    note = (read_json(mod.manifest_path).get("note", "") if mod.manifest_path.exists()
            else default_notes.get(mod.strategy, ""))
    build.write_manifest(mod, files, note)
    log(f"{mod.key}: manifest.json now lists {len(files)} files")
    return 0


def cmd_package(reg, args) -> int:
    modules = reg.select(args.only, args.include_disabled)
    release = build.package_modules(
        reg, modules, _release_version(args), Path(args.out), Path(args.build), Path(args.cache),
        _pairs(args.archive, "--archive"), _pairs(args.overlay_dir, "--overlay-dir"),
        merge=args.merge)
    log(f"packaged {len(release['modules'])} modules into {args.out}")
    return 0


def cmd_ckan(reg, args) -> int:
    modules = [m for m in reg.select(args.only, args.include_disabled) if m.strategy != "stub"]
    externals_only = all(m.strategy == "external" for m in modules)
    if not args.version and not externals_only:
        raise SkizzyError("--version is required (only external modules can do without it)")
    version = _release_version(args) if args.version else ""
    tag = args.tag or f"v{version}"
    release = {}
    if not args.draft and any(m.is_built for m in modules):
        release_path = Path(args.release_json)
        if not release_path.exists():
            raise SkizzyError(f"{release_path} not found; run 'package' first or use --draft")
        release = read_json(release_path)
        if release.get("version") != version:
            raise SkizzyError(f"{release_path} is for version {release.get('version')}, not {version}")
    date = args.release_date or datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    out = Path(args.out)
    written = []
    for mod in modules:
        asset = None
        if mod.is_built and not args.draft:
            asset = release["modules"].get(mod.identifier)
            if asset is None:
                raise SkizzyError(f"{mod.identifier} is not in {args.release_json}")
        elif mod.strategy == "external" and not args.draft:
            asset = build.external_asset(mod)
        doc = ckan.build_ckan(reg, mod, version, tag, asset, date)
        written.append(ckan.write_ckan(out, doc, draft=args.draft))
    for path in written:
        log(f"wrote {path}")
    return 0


def cmd_validate(reg, args) -> int:
    paths: list[Path] = []
    for p in args.paths or ["dist/ckan"]:
        p = Path(p)
        if p.is_dir():
            paths += sorted(p.rglob("*.ckan")) + sorted(p.rglob("*.ckan.draft.json"))
        else:
            paths.append(p)
    if not paths:
        raise SkizzyError("no .ckan files found")
    schema = ckan.load_schema(args.schema)
    problems = ckan.validate_documents(paths, schema)
    for p in problems:
        log(f"problem: {p}")
    log(f"validated {len(paths)} files against {args.schema}: {len(problems)} problems")
    return 1 if problems else 0


def _ckan_paths(items: list[str]) -> list[Path]:
    paths: list[Path] = []
    for p in items:
        p = Path(p)
        paths += sorted(p.rglob("*.ckan")) if p.is_dir() else [p]
    return paths


def cmd_verify_downloads(reg, args) -> int:
    """Download every .ckan's file and compare it with download_size and download_hash."""
    import json
    import tempfile
    problems, checked = [], 0
    for path in _ckan_paths(args.paths):
        doc = json.loads(path.read_text(encoding="utf-8"))
        if doc.get("kind") == "metapackage":
            continue
        url = doc["download"] if isinstance(doc["download"], str) else doc["download"][0]
        with tempfile.TemporaryDirectory() as tmp:
            target = Path(tmp) / "download.zip"
            try:
                build._download(url, target, attempts=args.attempts)
            except SkizzyError as exc:
                problems.append(f"{path.name}: {url}: {exc}")
                continue
            size, sha1, sha256 = hash_file(target)
        expected = (doc["download_size"], doc["download_hash"]["sha1"], doc["download_hash"]["sha256"])
        checked += 1
        if (size, sha1, sha256) != expected:
            problems.append(f"{path.name}: got size={size} sha1={sha1} sha256={sha256}, "
                            f".ckan says size={expected[0]} sha1={expected[1]} sha256={expected[2]}")
        else:
            log(f"ok  {path.name}: {size:,} bytes, sha256 {sha256}")
    for p in problems:
        log(f"problem: {p}")
    log(f"verified {checked} downloads: {len(problems)} problems")
    return 1 if problems else 0


def cmd_notes(reg, args) -> int:
    """Release notes: one table row per asset, plus how to add the CKAN repository."""
    release = read_json(Path(args.release_json))
    ckan_repo = reg.pack["ckan_repo"]
    lines = [
        f"Skizzy Naval {args.tag}: KSP 1.12.5 + BDArmory Plus 1.13 naval/combat compatibility builds.",
        "",
        "Install with CKAN: *Settings > Metadata repositories > New*, then paste",
        f"`https://github.com/{ckan_repo}/archive/refs/heads/main.tar.gz`",
        "and install **Skizzy Naval Pack** (`SkizzyNavalPack`) or single modules.",
        "",
        "| Asset | CKAN identifier | Version | Size | SHA-256 |",
        "|---|---|---|---:|---|",
    ]
    for ident, m in sorted(release["modules"].items(), key=lambda kv: kv[1]["asset"]):
        lines.append(f"| `{m['asset']}` | `{ident}` | {m['version']} | {m['download_size']:,} | `{m['sha256']}` |")
    lines += ["", "Licences and what each module changes: see `modules/<name>/README.md` in the repository."]
    Path(args.out).write_text("\n".join(lines) + "\n", encoding="utf-8")
    log(f"wrote {args.out}")
    return 0


# --------------------------------------------------------------------------- main

def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--registry", default=str(REPO_ROOT / "registry.toml"))
    sub = ap.add_subparsers(dest="cmd", required=True)

    sub.add_parser("list", help="list modules")
    sub.add_parser("check", help="offline repo and registry checks (CI)")

    p = sub.add_parser("import", help="fill modules/<key>/ from a patched tree")
    p.add_argument("key")
    p.add_argument("--patched", required=True, help="folder that contains the patched GameData/")
    p.add_argument("--archive", help="local copy of the upstream archive")
    p.add_argument("--cache", default=str(REPO_ROOT / ".cache" / "upstream"))
    p.add_argument("--force", action="store_true")
    p.add_argument("--allow-modified", action="store_true",
                   help="overlay only: skip edited upstream files instead of failing")

    p = sub.add_parser("manifest", help="regenerate manifest.json from the current overlay")
    p.add_argument("key")
    p.add_argument("--archive")
    p.add_argument("--cache", default=str(REPO_ROOT / ".cache" / "upstream"))
    p.add_argument("--build", default=str(REPO_ROOT / "build"))

    p = sub.add_parser("package", help="build release zips into dist/")
    p.add_argument("--version", required=True)
    p.add_argument("--only", nargs="*")
    p.add_argument("--include-disabled", action="store_true")
    p.add_argument("--out", default=str(REPO_ROOT / "dist"))
    p.add_argument("--build", default=str(REPO_ROOT / "build"))
    p.add_argument("--cache", default=str(REPO_ROOT / ".cache" / "upstream"))
    p.add_argument("--archive", action="append", help="KEY=PATH: use a local upstream archive")
    p.add_argument("--overlay-dir", action="append",
                   help="KEY=PATH: take the overlay GameData/ from PATH instead of modules/KEY")
    p.add_argument("--merge", action="store_true", help="add to an existing dist/release.json")

    p = sub.add_parser("ckan", help="generate .ckan files")
    p.add_argument("--version", help="release version (optional with --only <external modules>)")
    p.add_argument("--tag", help="release tag (default v<version>)")
    p.add_argument("--release-json", default=str(REPO_ROOT / "dist" / "release.json"))
    p.add_argument("--out", default=str(REPO_ROOT / "dist" / "ckan"))
    p.add_argument("--only", nargs="*")
    p.add_argument("--include-disabled", action="store_true")
    p.add_argument("--draft", action="store_true", help="placeholder sizes/hashes, *.ckan.draft.json")
    p.add_argument("--release-date")

    p = sub.add_parser("validate", help="validate .ckan files against CKAN.schema")
    p.add_argument("paths", nargs="*")
    p.add_argument("--schema", default=DEFAULT_SCHEMA)

    p = sub.add_parser("verify-downloads", help="download each .ckan's file and check size and hashes")
    p.add_argument("paths", nargs="+")
    p.add_argument("--attempts", type=int, default=5)

    p = sub.add_parser("notes", help="write release notes from dist/release.json")
    p.add_argument("--tag", required=True)
    p.add_argument("--release-json", default=str(REPO_ROOT / "dist" / "release.json"))
    p.add_argument("--out", default=str(REPO_ROOT / "dist" / "RELEASE_NOTES.md"))

    args = ap.parse_args(argv)
    try:
        reg = load_registry(Path(args.registry))
        handler = {"list": cmd_list, "check": cmd_check, "import": cmd_import,
                   "manifest": cmd_manifest, "package": cmd_package, "ckan": cmd_ckan,
                   "validate": cmd_validate, "verify-downloads": cmd_verify_downloads,
                   "notes": cmd_notes}[args.cmd]
        return handler(reg, args)
    except SkizzyError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
