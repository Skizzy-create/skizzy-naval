"""Load and check registry.toml (one [[module]] entry per sub-mod)."""
from __future__ import annotations

import re
import tomllib
from dataclasses import dataclass, field
from pathlib import Path

from .util import MODULES_DIR, REPO_ROOT, SkizzyError

IDENTIFIER_RE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9-]+$")
KEY_RE = re.compile(r"^[A-Za-z0-9][A-Za-z0-9_-]*$")

# bundle      modules/<key>/GameData is the complete mod (our own work or a full port).
# overlay     modules/<key>/GameData holds only files we add on top of an original that
#             users install separately (from CKAN, or by hand). Nothing is replaced.
# rebuild     CI downloads the original archive, applies modules/<key>/delete.txt and the
#             files in modules/<key>/GameData, checks the result against manifest.json,
#             and ships the full patched mod. Keeps large third-party art out of git.
# external    a mod released from its own repository (e.g. Skizzy-create/SubTrim). Nothing is
#             built or uploaded here; the .ckan points at [module.external].url and its size
#             and hashes come from downloading that file. Uses the module's own `version`.
# metapackage CKAN metapackage, no download.
# stub        placeholder; never packaged.
PACKAGE_STRATEGIES = {"bundle", "overlay", "rebuild"}      # built and uploaded by our release
STRATEGIES = PACKAGE_STRATEGIES | {"external", "metapackage", "stub"}
PACK_ROLES = {"depends", "recommends", "suggests", "none"}
RELATIONSHIP_KEYS = ("depends", "recommends", "suggests", "supports", "conflicts")


@dataclass
class Module:
    raw: dict
    key: str
    identifier: str
    strategy: str
    enabled: bool
    pack: str
    upstream: dict = field(default_factory=dict)

    @property
    def folder(self) -> Path:
        return MODULES_DIR / self.key

    @property
    def gamedata_dir(self) -> Path:
        return self.folder / "GameData"

    @property
    def manifest_path(self) -> Path:
        return self.folder / "manifest.json"

    @property
    def delete_path(self) -> Path:
        return self.folder / "delete.txt"

    @property
    def is_built(self) -> bool:
        """Built from this repository and uploaded to our GitHub release."""
        return self.strategy in PACKAGE_STRATEGIES

    @property
    def is_package(self) -> bool:
        """Has a download (ours or an external release), unlike metapackages and stubs."""
        return self.is_built or self.strategy == "external"

    @property
    def external_url(self) -> str:
        url = self.raw.get("external", {}).get("url", "")
        return url.format(version=self.raw.get("version", ""))

    def get(self, name, default=None):
        return self.raw.get(name, default)

    def delete_list(self) -> list[str]:
        if not self.delete_path.exists():
            return []
        lines = self.delete_path.read_text(encoding="utf-8").splitlines()
        return [ln.strip() for ln in lines if ln.strip() and not ln.lstrip().startswith("#")]

    def version_for(self, release_version: str) -> str:
        if self.strategy == "external":
            return str(self.raw.get("version", ""))
        template = self.raw.get("version_template", "{version}")
        return template.format(version=release_version)

    def asset_name(self, prefix: str, release_version: str) -> str:
        return f"{prefix}-{self.raw.get('asset', self.key)}-{release_version}.zip"


@dataclass
class Registry:
    path: Path
    pack: dict
    aliases: dict
    policy: dict
    modules: list[Module]

    def get(self, key: str) -> Module:
        for m in self.modules:
            if m.key == key or m.identifier == key:
                return m
        raise SkizzyError(f"no module '{key}' in {self.path.name}")

    def select(self, only: list[str] | None, include_disabled: bool = False) -> list[Module]:
        if only:
            return [self.get(k) for k in only]
        return [m for m in self.modules if m.enabled or include_disabled]

    def expand(self, items) -> list[dict]:
        """Turn registry relationship shorthand into CKAN relationship objects.
        "Name" -> {"name": "Name"}; "@alias" -> the [aliases] entry; tables pass through."""
        out = []
        for item in items or []:
            if isinstance(item, str) and item.startswith("@"):
                alias = item[1:]
                if alias not in self.aliases:
                    raise SkizzyError(f"unknown relationship alias '{item}'")
                out.append(_plain(self.aliases[alias]))
            elif isinstance(item, str):
                out.append({"name": item})
            elif isinstance(item, dict):
                out.append(_plain(item))
            else:
                raise SkizzyError(f"bad relationship entry {item!r}")
        return out


def _plain(value):
    """Deep-copy TOML data into plain dicts/lists."""
    if isinstance(value, dict):
        return {k: _plain(v) for k, v in value.items()}
    if isinstance(value, list):
        return [_plain(v) for v in value]
    return value


def load_registry(path: Path | None = None) -> Registry:
    path = path or (REPO_ROOT / "registry.toml")
    with open(path, "rb") as fh:
        data = tomllib.load(fh)
    modules = []
    for raw in data.get("module", []):
        modules.append(Module(
            raw=raw,
            key=raw.get("key", ""),
            identifier=raw.get("identifier", ""),
            strategy=raw.get("strategy", ""),
            enabled=bool(raw.get("enabled", False)),
            pack=raw.get("pack", "none"),
            upstream=raw.get("upstream", {}),
        ))
    return Registry(path=path, pack=data.get("pack", {}), aliases=data.get("aliases", {}),
                    policy=data.get("policy", {}), modules=modules)


def check_registry(reg: Registry) -> list[str]:
    """Static checks that need no network. Returns a list of problems."""
    problems: list[str] = []
    for req in ("release_repo", "ckan_repo", "asset_prefix", "spec_version",
                "ksp_version_min", "ksp_version_max"):
        if not reg.pack.get(req):
            problems.append(f"[pack] is missing '{req}'")
    seen_keys, seen_ids = set(), set()
    for m in reg.modules:
        where = f"module '{m.key or '?'}'"
        if not KEY_RE.match(m.key):
            problems.append(f"{where}: bad key")
        if not IDENTIFIER_RE.match(m.identifier):
            problems.append(f"{where}: identifier '{m.identifier}' is not a valid CKAN identifier")
        if m.key in seen_keys:
            problems.append(f"{where}: duplicate key")
        if m.identifier in seen_ids:
            problems.append(f"{where}: duplicate identifier")
        seen_keys.add(m.key)
        seen_ids.add(m.identifier)
        if m.strategy not in STRATEGIES:
            problems.append(f"{where}: unknown strategy '{m.strategy}'")
            continue
        if m.pack not in PACK_ROLES:
            problems.append(f"{where}: pack must be one of {sorted(PACK_ROLES)}")
        for req in ("name", "abstract", "author", "license"):
            if not m.get(req):
                problems.append(f"{where}: missing '{req}'")
        for rel in RELATIONSHIP_KEYS:
            try:
                reg.expand(m.get(rel))
            except SkizzyError as exc:
                problems.append(f"{where}: {rel}: {exc}")
        if m.strategy not in ("metapackage", "external") and not m.folder.is_dir():
            problems.append(f"{where}: folder modules/{m.key}/ does not exist")
        if m.strategy == "metapackage":
            if m.pack != "none":
                problems.append(f"{where}: a metapackage can't be part of the pack")
            continue
        if m.strategy == "stub":
            if m.enabled:
                problems.append(f"{where}: stubs can't be enabled; pick a real strategy first")
            continue
        if not m.get("install"):
            problems.append(f"{where}: packages need an 'install' list")
        for directive in m.get("install", []):
            if "install_to" not in directive:
                problems.append(f"{where}: every install directive needs install_to")
        if m.strategy == "external":
            if not m.get("version"):
                problems.append(f"{where}: external modules need their own 'version'")
            if not m.external_url.startswith("https://"):
                problems.append(f"{where}: external modules need an https [module.external] url")
            if m.get("version_template"):
                problems.append(f"{where}: version_template does not apply to external modules")
        if m.strategy in ("bundle", "overlay") and m.enabled and not m.gamedata_dir.is_dir():
            problems.append(f"{where}: modules/{m.key}/GameData is missing")
        if m.strategy == "rebuild":
            up = m.upstream
            if not up.get("urls"):
                problems.append(f"{where}: rebuild needs upstream.urls")
            if not up.get("extract"):
                problems.append(f"{where}: rebuild needs upstream.extract")
            if not up.get("sha256") and m.enabled and not m.manifest_path.exists():
                problems.append(f"{where}: upstream has no sha256, so manifest.json is required")
        if m.enabled and m.strategy in ("overlay", "rebuild") and not m.manifest_path.exists():
            problems.append(f"{where}: manifest.json is missing (run: skizzy.py import {m.key} ...)")
    return problems
