"""Generate and validate CKAN .ckan files from registry.toml plus release.json."""
from __future__ import annotations

import json
import re
import urllib.request
from pathlib import Path

from .registry import RELATIONSHIP_KEYS, Module, Registry
from .util import SkizzyError, write_json

ZERO_SHA1 = "0" * 40
ZERO_SHA256 = "0" * 64
HEX40 = re.compile(r"^[0-9A-F]{40}$")
HEX64 = re.compile(r"^[0-9A-F]{64}$")


def _spec_tuple(spec) -> tuple[int, int]:
    if spec == 1 or spec == "1":
        return (1, 0)
    m = re.match(r"^v(\d+)\.(\d+)$", str(spec))
    return (int(m.group(1)), int(m.group(2))) if m else (0, 0)


def _resources(reg: Registry, mod: Module) -> dict:
    base = f"https://github.com/{reg.pack['release_repo']}"
    homepage = base if mod.strategy == "metapackage" else f"{base}/tree/main/modules/{mod.key}"
    res = {"homepage": homepage, "repository": base, "bugtracker": f"{base}/issues"}
    res.update(mod.get("resources", {}))
    return res


def _pack_members(reg: Registry, role: str) -> list[dict]:
    return [{"name": m.identifier} for m in reg.modules
            if m.enabled and m.is_package and m.pack == role]


def _dedupe(rels: list[dict]) -> list[dict]:
    seen, out = set(), []
    for rel in rels:
        key = json.dumps(rel, sort_keys=True)
        if key not in seen:
            seen.add(key)
            out.append(rel)
    return out


def build_ckan(reg: Registry, mod: Module, release_version: str, tag: str,
               asset: dict | None, release_date: str | None) -> dict:
    """One .ckan document. asset=None renders a draft with placeholder size and hashes."""
    pack = reg.pack
    doc: dict = {
        "spec_version": mod.get("spec_version", pack["spec_version"]),
        "identifier": mod.identifier,
        "name": mod.get("name"),
        "abstract": mod.get("abstract"),
    }
    if mod.get("description"):
        doc["description"] = " ".join(mod.get("description").split())
    doc["author"] = mod.get("author")
    doc["version"] = mod.version_for(release_version)
    doc["ksp_version_min"] = mod.get("ksp_version_min", pack["ksp_version_min"])
    doc["ksp_version_max"] = mod.get("ksp_version_max", pack["ksp_version_max"])
    doc["license"] = mod.get("license")
    doc["resources"] = _resources(reg, mod)
    if mod.get("tags"):
        doc["tags"] = list(mod.get("tags"))
    if mod.strategy == "metapackage":
        doc["kind"] = "metapackage"
    if mod.get("provides"):
        doc["provides"] = list(mod.get("provides"))
    for rel in RELATIONSHIP_KEYS:
        items = reg.expand(mod.get(rel))
        if mod.strategy == "metapackage" and rel in ("depends", "recommends", "suggests"):
            items = items + _pack_members(reg, rel)
        if items:
            doc[rel] = _dedupe(items)
    if mod.is_package:
        doc["install"] = [dict(d) for d in mod.get("install")]
        if mod.strategy == "external":
            doc["download"] = mod.external_url
        else:
            asset_name = asset["asset"] if asset else mod.asset_name(pack["asset_prefix"], release_version)
            doc["download"] = (f"https://github.com/{pack['release_repo']}/releases/download/"
                               f"{tag}/{asset_name}")
        doc["download_size"] = asset["download_size"] if asset else 0
        doc["download_hash"] = {"sha1": asset["sha1"] if asset else ZERO_SHA1,
                                "sha256": asset["sha256"] if asset else ZERO_SHA256}
        doc["download_content_type"] = "application/zip"
        doc["install_size"] = asset["install_size"] if asset else 0
    if mod.strategy == "external":
        release_date = mod.get("release_date")   # our release date says nothing about theirs
    if release_date:
        doc["release_date"] = release_date
    return doc


def ckan_filename(doc: dict) -> str:
    return f"{doc['identifier']}-{str(doc['version']).replace(':', '-')}.ckan"


def write_ckan(out_dir: Path, doc: dict, draft: bool = False) -> Path:
    if draft:
        path = out_dir / f"{doc['identifier']}.ckan.draft.json"
    else:
        path = out_dir / doc["identifier"] / ckan_filename(doc)
    write_json(path, doc)
    return path


# --------------------------------------------------------------------------- validation

def load_schema(source: str) -> dict:
    if re.match(r"^https?://", source):
        req = urllib.request.Request(source, headers={"User-Agent": "skizzy-naval"})
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read().decode("utf-8"))
    return json.loads(Path(source).read_text(encoding="utf-8"))


def _walk_relationships(rels):
    for rel in rels or []:
        yield rel
        if "any_of" in rel:
            yield from _walk_relationships(rel["any_of"])


def semantic_problems(doc: dict, filename: str | None) -> list[str]:
    """Checks the JSON schema can't express (mirrors CKAN/netkan's own validators)."""
    problems = []
    spec = _spec_tuple(doc.get("spec_version"))
    rel_lists = [doc.get(k) for k in RELATIONSHIP_KEYS]
    if any("any_of" in r for rels in rel_lists for r in _walk_relationships(rels)) and spec < (1, 26):
        problems.append("any_of needs spec_version v1.26 or later")
    if any("as" in d for d in doc.get("install", [])) and spec < (1, 18):
        problems.append("install 'as' needs spec_version v1.18 or later")
    if doc.get("kind") == "metapackage":
        if "install" in doc or "download" in doc:
            problems.append("metapackages must not have install or download")
    else:
        hashes = doc.get("download_hash", {})
        if not HEX40.match(hashes.get("sha1", "")):
            problems.append("download_hash.sha1 must be 40 upper-case hex digits")
        if not HEX64.match(hashes.get("sha256", "")):
            problems.append("download_hash.sha256 must be 64 upper-case hex digits")
        if not str(doc.get("download", "")).startswith("https://"):
            problems.append("download must be an https URL")
        if not doc.get("install"):
            problems.append("packages need install directives")
    for rels in rel_lists:
        for rel in _walk_relationships(rels):
            if rel.get("name") == doc.get("identifier"):
                problems.append("module relates to itself")
    if filename and filename.endswith(".ckan") and filename != ckan_filename(doc):
        problems.append(f"file name should be {ckan_filename(doc)}")
    return problems


def validate_documents(paths: list[Path], schema: dict) -> list[str]:
    try:
        import jsonschema
    except ImportError as exc:
        raise SkizzyError("validation needs jsonschema: pip install -r scripts/requirements.txt") from exc
    validator = jsonschema.Draft4Validator(schema)
    problems = []
    for path in paths:
        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            problems.append(f"{path}: invalid JSON: {exc}")
            continue
        for err in sorted(validator.iter_errors(doc), key=lambda e: list(e.path)):
            loc = "/".join(str(p) for p in err.path) or "(root)"
            problems.append(f"{path.name}: schema: {loc}: {err.message}")
        for msg in semantic_problems(doc, path.name):
            problems.append(f"{path.name}: {msg}")
    return problems
