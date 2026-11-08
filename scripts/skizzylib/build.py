"""Fetch upstream archives, stage module payloads, verify them and write release zips."""
from __future__ import annotations

import hashlib
import shutil
import struct
import time
import urllib.request
import zipfile
from datetime import datetime, timezone
from pathlib import Path

from .registry import Module, Registry
from .util import (SkizzyError, copy_file, hash_file, log, matches_any, read_json, rmtree,
                   sha256_bytes, tree_manifest, walk_files, warn, write_json)

USER_AGENT = "skizzy-naval-packager/1.0 (+https://github.com/Skizzy-create/skizzy-naval)"
ZIP_TIME = (2020, 1, 1, 0, 0, 0)   # fixed timestamps make the zips reproducible
DOC_FILES = ("README.md", "CHANGES.md", "ATTRIBUTION.md")
DOCS_DIR_IN_ZIP = "SkizzyNaval-docs"


# --------------------------------------------------------------------------- upstream

def _download(url: str, target: Path, attempts: int = 5, delay: float = 30) -> None:
    """Download url to target, retrying any network error (GitHub release downloads sometimes
    return 5xx for minutes at a time): `attempts` tries, `delay` seconds apart."""
    last = None
    for i in range(attempts):
        if i:
            log(f"  retrying in {delay:.0f} s ({last})")
            time.sleep(delay)
        try:
            req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=180) as resp, open(target, "wb") as fh:
                shutil.copyfileobj(resp, fh, 1 << 20)
            return
        except Exception as exc:  # noqa: BLE001 - report and retry any network error
            last = exc
    raise SkizzyError(f"download failed after {attempts} attempts: {last}")


def _check_archive(path: Path, up: dict) -> str | None:
    """Return a problem description, or None if the archive matches the registry."""
    size, _, sha256 = hash_file(path)
    if up.get("size") and size != int(up["size"]):
        return f"size {size} != expected {up['size']}"
    if up.get("sha256") and sha256 != up["sha256"].upper():
        return f"sha256 {sha256} != expected {up['sha256'].upper()}"
    if not zipfile.is_zipfile(path):
        return "not a zip file"
    return None


def fetch_upstream(mod: Module, cache_dir: Path, local_archives: dict[str, str]) -> Path:
    """Path to the verified upstream archive: a local override, the cache, or a download."""
    up = mod.upstream
    if mod.key in local_archives:
        path = Path(local_archives[mod.key])
        problem = _check_archive(path, up)
        if problem:
            raise SkizzyError(f"{mod.key}: local archive {path}: {problem}")
        return path
    cache_dir.mkdir(parents=True, exist_ok=True)
    tag = up.get("sha256", "")[:16] or hashlib.sha256(up["urls"][0].encode()).hexdigest()[:16]
    target = cache_dir / f"{mod.key}-{tag}.zip"
    if target.exists() and _check_archive(target, up) is None:
        return target
    errors = []
    for url in up["urls"]:
        tmp = target.with_suffix(".part")
        log(f"  downloading {url}")
        try:
            _download(url, tmp)
            problem = _check_archive(tmp, up)
            if problem:
                raise SkizzyError(problem)
            tmp.replace(target)
            return target
        except SkizzyError as exc:
            errors.append(f"{url}: {exc}")
            tmp.unlink(missing_ok=True)
    raise SkizzyError(f"{mod.key}: no upstream URL worked:\n  " + "\n  ".join(errors))


def entry_name(info: zipfile.ZipInfo) -> str:
    """Entry name, honouring the Info-ZIP Unicode Path extra field (0x7075) that some
    Windows tools write instead of the UTF-8 flag (AWP's Chinese file names need it)."""
    name = info.filename
    if not info.flag_bits & 0x800:
        extra, i = info.extra, 0
        while i + 4 <= len(extra):
            hid, length = struct.unpack("<HH", extra[i:i + 4])
            data = extra[i + 4:i + 4 + length]
            if hid == 0x7075 and length >= 5 and data[0] == 1:
                try:
                    name = data[5:].decode("utf-8")
                except UnicodeDecodeError:
                    pass
                break
            i += 4 + length
    return name.replace("\\", "/")


def upstream_entries(zip_path: Path, up: dict) -> tuple[zipfile.ZipFile, dict[str, zipfile.ZipInfo]]:
    """Map archive entries to staging paths with the registry's extract rules."""
    zf = zipfile.ZipFile(zip_path)
    infos = [(entry_name(i), i) for i in zf.infolist() if not entry_name(i).endswith("/")]
    if up.get("strip_top"):
        tops = {n.split("/", 1)[0] for n, _ in infos}
        if len(tops) != 1 or any("/" not in n for n, _ in infos):
            raise SkizzyError(f"strip_top: {zip_path.name} has no single top-level folder")
        cut = len(tops.pop()) + 1
        infos = [(n[cut:], i) for n, i in infos]
    excludes = up.get("exclude", [])
    mapped: dict[str, zipfile.ZipInfo] = {}
    for name, info in infos:
        for rule in up["extract"]:
            src, dst = rule["from"].strip("/"), rule["to"].strip("/")
            if src and name != src and not name.startswith(src + "/"):
                continue
            rest = name[len(src):].lstrip("/") if src else name
            dest = f"{dst}/{rest}" if dst and rest else (dst or rest)
            if excludes and matches_any(dest, excludes):
                break
            if dest in mapped:
                raise SkizzyError(f"two archive entries map to {dest}")
            mapped[dest] = info
            break
    if not mapped:
        raise SkizzyError(f"extract rules matched nothing in {zip_path.name}")
    return zf, dict(sorted(mapped.items()))


# --------------------------------------------------------------------------- staging

def _prune_empty_dirs(root: Path) -> None:
    for path in sorted((p for p in root.rglob("*") if p.is_dir()), key=lambda p: len(p.parts),
                       reverse=True):
        if not any(path.iterdir()):
            path.rmdir()


def stage_module(reg: Registry, mod: Module, build_dir: Path, cache_dir: Path,
                 local_archives: dict[str, str], overlay_dirs: dict[str, str]) -> Path:
    """Assemble the files a module ships (its zip minus the docs) in build_dir/<key>."""
    stage = build_dir / mod.key
    rmtree(stage)
    stage.mkdir(parents=True)
    if mod.strategy == "rebuild":
        archive = fetch_upstream(mod, cache_dir, local_archives)
        zf, entries = upstream_entries(archive, mod.upstream)
        with zf:
            for dest, info in entries.items():
                out = stage / dest
                out.parent.mkdir(parents=True, exist_ok=True)
                with zf.open(info) as src, open(out, "wb") as dst:
                    shutil.copyfileobj(src, dst, 1 << 20)
        for rel in mod.delete_list():
            target = stage / rel
            if not target.is_file():
                raise SkizzyError(f"{mod.key}: delete.txt lists '{rel}', which the upstream "
                                  "archive does not contain")
            target.unlink()
        _prune_empty_dirs(stage)
    overlay_root = Path(overlay_dirs[mod.key]) if mod.key in overlay_dirs else mod.folder
    overlay = walk_files(overlay_root / "GameData")
    if mod.strategy in ("bundle", "overlay") and not overlay:
        raise SkizzyError(f"{mod.key}: {overlay_root / 'GameData'} is empty")
    for rel, src in overlay.items():
        copy_file(src, stage / "GameData" / rel)
    forbidden = [rel for rel in walk_files(stage)
                 if matches_any(rel, reg.policy.get("forbidden", []))]
    if forbidden:
        raise SkizzyError(f"{mod.key}: refusing to ship third-party libraries: {forbidden}")
    return stage


def compare_to_manifest(actual: dict[str, list], expected: dict[str, list]) -> list[str]:
    problems = []
    for rel in sorted(expected.keys() - actual.keys()):
        problems.append(f"missing  {rel}")
    for rel in sorted(actual.keys() - expected.keys()):
        problems.append(f"extra    {rel}")
    for rel in sorted(actual.keys() & expected.keys()):
        if [actual[rel][0], actual[rel][1].upper()] != [expected[rel][0], expected[rel][1].upper()]:
            problems.append(f"changed  {rel}")
    return problems


def verify_stage(mod: Module, stage: Path) -> dict[str, list]:
    actual = tree_manifest(stage)
    if not mod.manifest_path.exists():
        warn(f"{mod.key}: no manifest.json, skipping content verification")
        return actual
    expected = read_json(mod.manifest_path)["files"]
    problems = compare_to_manifest(actual, expected)
    if problems:
        shown = "\n  ".join(problems[:40])
        more = f"\n  ... and {len(problems) - 40} more" if len(problems) > 40 else ""
        raise SkizzyError(f"{mod.key}: staged files differ from manifest.json:\n  {shown}{more}")
    return actual


def write_manifest(mod: Module, files: dict[str, list], note: str) -> None:
    write_json(mod.manifest_path, {
        "format": 1,
        "module": mod.key,
        "strategy": mod.strategy,
        "note": note,
        "file_count": len(files),
        "total_size": sum(v[0] for v in files.values()),
        "files": files,
    }, indent=1)


# --------------------------------------------------------------------------- zips

def write_zip(out_path: Path, stage: Path, docs: dict[str, Path]) -> None:
    """Deterministic zip: sorted entries, fixed timestamps and attributes."""
    entries = [(rel, path) for rel, path in walk_files(stage).items()]
    entries += [(f"{DOCS_DIR_IN_ZIP}/{name}", path) for name, path in docs.items()]
    entries.sort(key=lambda e: e[0])
    tmp = out_path.with_suffix(".part")
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(tmp, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for rel, path in entries:
            info = zipfile.ZipInfo(rel, date_time=ZIP_TIME)
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            info.compress_type = zipfile.ZIP_DEFLATED
            zf.writestr(info, path.read_bytes(), compress_type=zipfile.ZIP_DEFLATED,
                        compresslevel=9)
    tmp.replace(out_path)


def module_docs(mod: Module) -> dict[str, Path]:
    """README, CHANGES, ATTRIBUTION and every LICENSE* file, shipped at the zip root (CKAN
    installs only GameData, but the download itself carries the attribution)."""
    docs = {name: mod.folder / name for name in DOC_FILES if (mod.folder / name).is_file()}
    for path in sorted(mod.folder.glob("LICENSE*")):
        if path.is_file():
            docs[path.name] = path
    for extra in mod.get("extra_docs", []):   # e.g. diffs/ and tools/ for GPL source
        root = mod.folder / extra
        if root.is_file():
            docs[extra] = root
        elif root.is_dir():
            for rel, path in walk_files(root).items():
                docs[f"{extra}/{rel}"] = path
        else:
            raise SkizzyError(f"{mod.key}: extra_docs entry '{extra}' does not exist")
    return dict(sorted(docs.items()))


def package_modules(reg: Registry, modules: list[Module], release_version: str, out_dir: Path,
                    build_dir: Path, cache_dir: Path, local_archives: dict[str, str],
                    overlay_dirs: dict[str, str], merge: bool = False) -> dict:
    prefix = reg.pack["asset_prefix"]
    release_path = out_dir / "release.json"
    release = {"version": release_version, "modules": {}}
    if merge and release_path.exists():
        release = read_json(release_path)
        if release.get("version") != release_version:
            raise SkizzyError("release.json belongs to another version; can't merge")
    for mod in modules:
        if not mod.is_built:
            continue
        log(f"[{mod.key}] staging ({mod.strategy})")
        stage = stage_module(reg, mod, build_dir, cache_dir, local_archives, overlay_dirs)
        files = verify_stage(mod, stage)
        asset = mod.asset_name(prefix, release_version)
        zip_path = out_dir / asset
        write_zip(zip_path, stage, module_docs(mod))
        size, sha1, sha256 = hash_file(zip_path)
        release["modules"][mod.identifier] = {
            "key": mod.key,
            "version": mod.version_for(release_version),
            "asset": asset,
            "download_size": size,
            "sha1": sha1,
            "sha256": sha256,
            "install_size": sum(v[0] for v in files.values()),
            "file_count": len(files),
        }
        log(f"[{mod.key}] {asset}: {size:,} bytes, {len(files)} files, sha256 {sha256}")
    release["generated"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    write_json(release_path, release)
    lines = [f"{m['sha256'].lower()}  {m['asset']}" for m in release["modules"].values()]
    (out_dir / "SHA256SUMS.txt").write_text("\n".join(sorted(lines)) + "\n", encoding="utf-8")
    return release


def external_asset(mod: Module, attempts: int = 5) -> dict:
    """Download an external module's release asset and describe it for its .ckan."""
    import tempfile
    url = mod.external_url
    with tempfile.TemporaryDirectory() as tmp:
        target = Path(tmp) / "external.zip"
        try:
            _download(url, target, attempts=attempts)
        except SkizzyError as exc:
            raise SkizzyError(f"{mod.key}: {url}: {exc} (does the release exist yet?)") from None
        if not zipfile.is_zipfile(target):
            raise SkizzyError(f"{mod.key}: {url} is not a zip file")
        size, sha1, sha256 = hash_file(target)
        with zipfile.ZipFile(target) as zf:
            names = {entry_name(i): i.file_size for i in zf.infolist() if not i.is_dir()}
    directives = mod.get("install", [])
    roots = [d["file"].rstrip("/") for d in directives if "file" in d]

    def under(name: str) -> bool:
        return any(name == r or name.startswith(r + "/") for r in roots)

    for root in roots:
        if not any(n == root or n.startswith(root + "/") for n in names):
            raise SkizzyError(f"{mod.key}: {url} has nothing under '{root}' (install directive)")
    if roots and len(roots) == len(directives):
        install_size = sum(sz for n, sz in names.items() if under(n))
    else:
        install_size = sum(names.values())
    log(f"[{mod.key}] external {url}: {size:,} bytes, sha256 {sha256}")
    return {"key": mod.key, "version": mod.version_for(""), "asset": url.rsplit("/", 1)[-1],
            "download": url, "download_size": size, "sha1": sha1, "sha256": sha256,
            "install_size": install_size, "file_count": len(names)}


def upstream_hashes(zf: zipfile.ZipFile, entries: dict[str, zipfile.ZipInfo]) -> dict[str, list]:
    return {dest: [info.file_size, sha256_bytes(zf.read(info))] for dest, info in entries.items()}
