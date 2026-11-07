"""Small helpers shared by the skizzy-naval scripts."""
from __future__ import annotations

import fnmatch
import hashlib
import json
import os
import shutil
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
MODULES_DIR = REPO_ROOT / "modules"
IN_CI = os.environ.get("GITHUB_ACTIONS") == "true"


class SkizzyError(Exception):
    """A user-facing error: printed without a traceback."""


def log(msg: str) -> None:
    print(msg, flush=True)


def warn(msg: str) -> None:
    prefix = "::warning::" if IN_CI else "warning: "
    print(prefix + msg, file=sys.stderr, flush=True)


def hash_file(path: Path) -> tuple[int, str, str]:
    """Return (size, SHA1, SHA256) with upper-case hex digests, as CKAN writes them."""
    sha1 = hashlib.sha1()
    sha256 = hashlib.sha256()
    size = 0
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            size += len(chunk)
            sha1.update(chunk)
            sha256.update(chunk)
    return size, sha1.hexdigest().upper(), sha256.hexdigest().upper()


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def walk_files(root: Path) -> dict[str, Path]:
    """All regular files under root, keyed by POSIX path relative to root, sorted."""
    out: dict[str, Path] = {}
    if not root.exists():
        return out
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames.sort()
        for name in sorted(filenames):
            full = Path(dirpath) / name
            out[full.relative_to(root).as_posix()] = full
    return dict(sorted(out.items()))


def tree_manifest(root: Path) -> dict[str, list]:
    """{relative path: [size, SHA256]} for every file under root."""
    manifest = {}
    for rel, full in walk_files(root).items():
        size, _, sha256 = hash_file(full)
        manifest[rel] = [size, sha256]
    return manifest


def copy_file(src: Path, dst: Path) -> None:
    """Copy file contents into a new file. Never hard-links, so NTFS hard links in the
    source tree are broken and the source can't be modified through the copy."""
    dst.parent.mkdir(parents=True, exist_ok=True)
    if dst.exists():
        dst.unlink()
    shutil.copyfile(src, dst)


def rmtree(path: Path) -> None:
    if path.exists():
        def onerror(func, p, _exc):
            os.chmod(p, 0o666)
            func(p)
        shutil.rmtree(path, onerror=onerror)


def read_json(path: Path):
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)


def write_json(path: Path, data, indent: int = 4) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(data, indent=indent, ensure_ascii=False) + "\n"
    path.write_bytes(text.encode("utf-8"))


def matches_any(rel_path: str, patterns: list[str]) -> str | None:
    """Return the first glob pattern matching the path or its basename."""
    base = rel_path.rsplit("/", 1)[-1]
    for pat in patterns:
        if fnmatch.fnmatchcase(base, pat) or fnmatch.fnmatchcase(rel_path, pat):
            return pat
    return None
