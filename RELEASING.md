# Releasing and maintaining Skizzy Naval

## One-time setup: the token secret

The release workflow pushes the generated `.ckan` files to `Skizzy-create/skizzy-ckan`. The
built-in `GITHUB_TOKEN` can't write to another repository, so it needs a token:

1. GitHub > *Settings* > *Developer settings* > *Personal access tokens* > *Fine-grained
   tokens* > *Generate new token*.
   - Resource owner: `Skizzy-create`. Repository access: *Only select repositories* >
     `skizzy-ckan`.
   - Repository permissions: **Contents: Read and write**, **Pull requests: Read and write**
     (Metadata: read is added automatically).
   - Pick an expiry and note it down. When the token expires, the publish step fails with a
     warning.
2. `skizzy-naval` > *Settings* > *Secrets and variables* > *Actions* > *New repository
   secret*. Name: **`SKIZZY_CKAN_TOKEN`**. Value: the token.
3. Optional: under the *Variables* tab, add **`CKAN_PUBLISH_MODE`** = `push` to commit
   straight to skizzy-ckan's `main`. The default (`pr`) opens a pull request instead, and
   skizzy-ckan's validation workflow then checks every changed file's download before you
   merge.

Without the secret, releases still work. The `publish-ckan` job prints a warning, and the
`.ckan` files are kept as the run's `ckan-<tag>` artifact (see "Manual metadata push" below).

## Cutting a release

```sh
git switch main && git pull
python scripts/skizzy.py check
git tag v0.2.0
git push origin v0.2.0
gh run watch            # or follow the "Release" run on the Actions tab
```

The tag is the pack version. Every built module gets version `0.2.0`, except BDArmory-Skizzy,
whose `version_template` makes it `v1.13.1.0-skizzy.0.2.0`. Every enabled module is rebuilt
and re-released. Zips are reproducible (fixed timestamps and order), so an unchanged module
gives the same bytes, but it still gets a new version number.

What the run does:

1. `check`, then download and cache the upstream archives (pinned by SHA-256).
2. `package` builds `dist/SkizzyNaval-<key>-<version>.zip`, plus `release.json` and
   `SHA256SUMS.txt`. Each zip has `GameData/...` and `SkizzyNaval-docs/` (README, CHANGES,
   ATTRIBUTION, LICENSE).
3. `ckan` writes `dist/ckan/<identifier>/<identifier>-<version>.ckan` with the real size,
   SHA-1, SHA-256 and install size, then `validate` checks them against CKAN.schema.
4. It creates the GitHub release, uploads the assets, and runs `verify-downloads`, which
   downloads every asset back and compares it with its `.ckan`.
5. `publish-ckan` copies the files into skizzy-ckan (a pull request or a push).

To re-run a release for an existing tag (assets are replaced): *Actions* > *Release* > *Run
workflow*, input `tag`.

### Manual metadata push (no token)

```sh
gh run download <run-id> --repo Skizzy-create/skizzy-naval --name ckan-v0.2.0 --dir ckan-out
cp -R ckan-out/. ../skizzy-ckan/
cd ../skizzy-ckan
python scripts/validate.py --check-downloads
git add -A && git commit -m "SkizzyNaval v0.2.0 metadata" && git push
```

## Adding a module

1. Add a `[[module]]` entry to `registry.toml` (see the field reference below).
2. Create `modules/<key>/` with `LICENSE*`, `README.md` (including the licence decision),
   `CHANGES.md`, `ATTRIBUTION.md` and, for plugins, `src/`.
3. Fill `GameData/` from your patched tree. Pass `--archive` if you have the original zip
   locally; otherwise the URL is downloaded.

   ```sh
   python scripts/skizzy.py import <key> --patched path/to/patched --archive original.zip
   ```

   - bundle: copies everything.
   - overlay: copies only the added files. Edited upstream files are an error; with
     `--allow-modified` they are listed in `not-shipped.txt` instead.
   - rebuild: copies added and changed files, writes `delete.txt` and the full
     `manifest.json`.

   This only reads the patched tree, and it copies files rather than linking them.
4. Run `python scripts/skizzy.py check`, and `package --version 0.0.0 --only <key>` to try
   it.
5. If the module should be part of the pack, set `pack = "depends"` (or `recommends` or
   `suggests`). The metapackage picks it up automatically.

Changing a rebuild module later: edit its files under `modules/<key>/GameData/`, then run
`python scripts/skizzy.py manifest <key>` to rebuild `manifest.json`, and commit both.

## External modules (SubTrim)

`strategy = "external"` is for mods released from their own repositories. Nothing is built
here. `ckan` downloads `[module.external].url` (with `{version}` filled in from the module's
own `version`), checks that the install folder is in the zip, and writes the `.ckan` with the
real size and hashes.

SubTrim (https://github.com/Skizzy-create/SubTrim) is one. Its release asset is
`SubTrim-<ver>.zip`, containing `GameData/SubTrim/`. Because of `pack = "depends"`, the
metapackage depends on it. Every skizzy-naval release regenerates its `.ckan` (the same
bytes while `version` is unchanged).

For a new SubTrim version, bump `version` and `release_date` in its registry entry. Then either
cut a skizzy-naval release, or publish just SubTrim's metadata:

```sh
python scripts/skizzy.py ckan --only SubTrim --out ../skizzy-ckan
cd ../skizzy-ckan && python scripts/validate.py --check-downloads SubTrim/*.ckan
```

Alternatively, SubTrim's own release workflow can push its `.ckan` to skizzy-ckan, using the
same kind of token.

## registry.toml field reference

`[pack]`: `release_repo`, `ckan_repo`, `asset_prefix`, `spec_version` (default for every
`.ckan`), `ksp_version_min` and `ksp_version_max` (defaults).
`[aliases]`: named relationship entries used as `"@name"`.
`[policy]`: `max_file_mb` (the largest file allowed in git) and `forbidden` (glob patterns
for files that must never be shipped).

| Field | Meaning |
|---|---|
| `key` | Folder name under `modules/`, and the asset name part (`asset` overrides the latter) |
| `identifier` | CKAN identifier |
| `strategy` | `bundle`, `overlay`, `rebuild`, `external`, `metapackage` or `stub` |
| `enabled` | Release it? Disabled modules are skipped by `package` and `ckan` |
| `pack` | Role in SkizzyNavalPack: `depends`, `recommends`, `suggests` or `none` |
| `version` | External modules only: their own version |
| `version_template` | Turns the pack version into the module's CKAN version (default `{version}`) |
| `name`, `abstract`, `description`, `author`, `license`, `tags` | Copied into the `.ckan` (the description is joined into one line) |
| `ksp_version_min`, `ksp_version_max`, `spec_version` | Override the `[pack]` defaults |
| `provides`, `depends`, `recommends`, `suggests`, `supports`, `conflicts` | CKAN relationships. A plain string means a name, `"@alias"` uses `[aliases]`, and tables pass through |
| `replaced_by` | CKAN `replaced_by`: one identifier (or a `{ name, version }` table) that CKAN offers as the replacement (`ckan replace`) |
| `install` | CKAN install directives. Every directive needs `install_to` |
| `resources` | Merged over the default homepage, repository and bugtracker links |
| `release_date` | External modules only (others use the release time) |
| `[module.upstream]` `urls`, `size`, `sha256` | Original archive and mirrors, checked before use. Leave `sha256` out only when the archive isn't byte-stable (GitHub source archives); `manifest.json` still checks every file |
| `[module.upstream]` `strip_top` | Drop the archive's single top folder |
| `[module.upstream]` `extract` | `{ from, to }` folder mappings from the archive into the zip (`from = ""` means everything), optional `exclude` globs |
| `[module.external]` `url` | Release asset URL; `{version}` is filled in (write the URL out in full when the version has an epoch) |

## Checks

- `ci.yml` runs on every push and pull request: `check`, then generates draft `.ckan` files
  and validates them against the latest CKAN.schema.
- Locally, run actionlint on `.github/workflows/*.yml` after editing workflows.
- skizzy-ckan's own workflow validates every `.ckan` against the vendored and the latest
  schema. On pull requests it also downloads the changed files.
