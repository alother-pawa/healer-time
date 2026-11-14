"""Static checks for this Unity project (no Unity or C# compiler needed).

Run from anywhere:  python Tools/check/check.py
Exit code 1 when any problem is found. Checks:
  1. JSON validity of asmdefs, input actions and the package manifest; file: package paths resolve.
  2. Asmdef rules: name == file == rootNamespace, known references, modules never reference other modules,
     editor assemblies are Editor-only.
  3. C# files: an asmdef above each file, namespace inside the assembly root, a top-level type named like the file,
     no syntax newer than C# 9, usings backed by asmdef references, `using System` when System types are used.
  4. Known bug classes: `?.`/`??` lines listed for review, `?.Kill()` on tween fields, awaitable completion inside
     DOTween callbacks.
  5. Builders: every Fields.Set field name exists as a [SerializeField] on the target type (game, Core and plugin
     types, inherited fields included); GameTypes entries resolve; BuildPaths constants exist.
  6. Sprites referenced by builders exist; art/art-spec.json (from Tools/ShapeGen) lists exactly the PNGs on disk, sheets
     have tags; every PNG sits in a pack folder (one Sprite Atlas each).
  7. Folders under Assets/game are lowercase kebab-case (Unity special folders excepted).
  8. Localization CSV: header, unique keys, complete rows, same placeholders per language, builder keys present.
  9. Input actions: unique ids, bindings point to actions, scheme coverage for non-UI maps, action strings in code exist.
 10. Config ResourcePath constants match where builders save the assets.
 11. Name resolution over the game and its file: packages (check_names.py of the packages repo): namespace shadowing,
     ambiguous types, missing usings, missing members, `out _` in `_ =>` lambdas, obsolete Unity 6.5 APIs.
"""
import csv
import io
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
GAME = ROOT / "Assets" / "game"
SCRIPTS = GAME / "scripts"
BUILDERS = SCRIPTS / "editor" / "builders"
PACKAGES = ROOT / "Packages"
SPECIAL = {"Editor", "Resources", "Plugins", "StreamingAssets"}
KEBAB = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
KEY_SHAPE = re.compile(r"^[a-z][a-z0-9-]*(?:\.[a-z0-9-]+)+$")
PLACEHOLDER = re.compile(r"\{\d+(?:[,:][^}]*)?\}")
UNITY_ASSEMBLIES = {
    "Unity.InputSystem", "UnityEngine.UI", "Unity.TextMeshPro", "DOTween.Modules", "UnityEngine.TestRunner",
    "UnityEditor.TestRunner", "Unity.RenderPipelines.Core.Runtime", "Unity.RenderPipelines.Universal.Runtime",
    "Unity.2D.Sprite.Editor",
}
problems, notes = [], []


def problem(message):
    problems.append(message)


def rel(path):
    try:
        return str(path.relative_to(ROOT)).replace("\\", "/")
    except ValueError:
        return str(path)


def read(path):
    return path.read_text(encoding="utf-8")


# 1. JSON and manifest.
parsed = {}
for path in list(ROOT.joinpath("Assets").rglob("*.asmdef")) + list(ROOT.joinpath("Assets").rglob("*.inputactions")) \
        + [PACKAGES / "manifest.json"]:
    try:
        parsed[path] = json.loads(read(path))
    except Exception as error:  # noqa: BLE001
        problem(f"{rel(path)}: invalid JSON ({error})")
manifest = parsed.get(PACKAGES / "manifest.json", {})
local_packages = []
for name, version in manifest.get("dependencies", {}).items():
    if version.startswith("file:"):
        target = (PACKAGES / version[5:]).resolve()
        if not target.exists():
            problem(f"manifest: {name} path {version} does not resolve")
        else:
            local_packages.append(target)
package_assemblies = set()
for package in local_packages:
    for asmdef in package.rglob("*.asmdef"):
        try:
            package_assemblies.add(json.loads(read(asmdef))["name"])
        except Exception:  # noqa: BLE001
            pass

# 2. Asmdefs.
asmdefs = {}
for path, data in parsed.items():
    if path.suffix == ".asmdef" and SCRIPTS in path.parents:
        asmdefs[data["name"]] = (path, data)
        if path.stem != data["name"]:
            problem(f"{rel(path)}: file name differs from assembly name {data['name']}")
        if data.get("rootNamespace") != data["name"]:
            problem(f"{rel(path)}: rootNamespace differs from name")
roots = {name.split(".")[0] for name in asmdefs}
for name, (path, data) in asmdefs.items():
    for reference in data.get("references", []):
        if reference not in asmdefs and reference not in package_assemblies and reference not in UNITY_ASSEMBLIES:
            problem(f"{rel(path)}: unknown reference {reference}")
    shared = {r for r in asmdefs if r.endswith(".Contracts") or r.endswith(".Foundation")}
    location = rel(path)
    if "/systems/" in location or "/objects/" in location:
        bad = [r for r in data.get("references", []) if r in asmdefs and r not in shared]
        if bad:
            problem(f"{location}: module references other modules {bad}")
    if ".Editor" in name and data.get("includePlatforms") != ["Editor"]:
        problem(f"{location}: editor assembly must include only the Editor platform")


def owning_asmdef(cs_path):
    for folder in cs_path.parents:
        found = list(folder.glob("*.asmdef"))
        if found:
            return parsed.get(found[0])
        if folder == SCRIPTS:
            return None
    return None


# 3. C# files.
cs_files = sorted(SCRIPTS.rglob("*.cs"))
namespace_owner = {}
texts = {path: read(path) for path in cs_files}
SYSTEM_TYPES = re.compile(r"\b(IDisposable|Action<|new Action\(|Func<|Math\.|Array\.|\w*Exception\(|"
                          r"catch \(\w*Exception|StringComparison\.|TimeSpan|DateTime\.|Guid\.)")
for path, text in texts.items():
    asm = owning_asmdef(path)
    if asm is None:
        problem(f"{rel(path)}: no asmdef above it")
        continue
    if path.name == "AssemblyInfo.cs":
        continue
    if re.search(r"^namespace [\w.]+;", text, re.M):
        problem(f"{rel(path)}: file-scoped namespace (C# 10)")
    ns = re.search(r"^namespace ([\w.]+)\s*$", text, re.M)
    if not ns:
        problem(f"{rel(path)}: no namespace")
        continue
    namespace_owner.setdefault(ns.group(1), set()).add(asm["name"])
    if not ns.group(1).startswith(asm["rootNamespace"]):
        problem(f"{rel(path)}: namespace {ns.group(1)} outside {asm['rootNamespace']}")
    types = re.findall(r"^    (?:public|internal)\s+(?:static\s+|sealed\s+|abstract\s+|readonly\s+|partial\s+)*"
                       r"(?:class|struct|interface|enum)\s+(\w+)", text, re.M)
    if path.stem.split(".")[0] not in types:
        # Partial files (Name.Part.cs) and contract files grouping several plain types are accepted; Unity object
        # types must live in a file named exactly like the class.
        if any(re.search(r"(?<!abstract )class " + t + r"\b[^{]*:\s*(MonoBehaviour|ScriptableObject)", text)
               for t in types):
            problem(f"{rel(path)}: Unity object type not named like the file ({types})")
        else:
            notes.append(f"{rel(path)}: groups types {types}")
    code = "\n".join(line for line in text.splitlines() if not line.strip().startswith("//") and '"' not in line)
    for keyword in ("record ", "global using", " init;", " is not ", "switch {", " new();", "= new() "):
        if keyword in code:
            problem(f"{rel(path)}: '{keyword.strip()}' needs C# > 9 or review")
    if "using System;" not in text and SYSTEM_TYPES.search(code) and "System." not in code:
        problem(f"{rel(path)}: uses System types without 'using System;'")
    for number, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()
        if stripped.startswith("//"):
            continue
        if "?.Kill(" in line:
            problem(f"{rel(path)}:{number}: '?.Kill()' on a tween field (use KillIfActive + clear in OnKill)")
        if "?." in line or "??" in line:
            notes.append(f"review ?./??: {rel(path)}:{number}: {stripped}")
    if re.search(r"(onComplete|onKill|OnComplete\(|OnKill\()[^;]*TrySetResult", text):
        problem(f"{rel(path)}: completes an awaitable inside a DOTween callback (defer to the next frame)")

for path, text in texts.items():
    asm = owning_asmdef(path)
    if asm is None:
        continue
    refs = set(asm.get("references", [])) | {asm["name"]}
    for using in re.findall(r"^using ([\w.]+);", text, re.M):
        root = using.split(".")[0]
        if root in roots:
            owners = namespace_owner.get(using)
            if owners is None:
                problem(f"{rel(path)}: using {using} matches no project namespace")
            elif not owners & refs:
                problem(f"{rel(path)}: using {using} from {sorted(owners)} not referenced by {asm['name']}")
        elif using.startswith("TMPro") and "Unity.TextMeshPro" not in refs:
            problem(f"{rel(path)}: TMPro without Unity.TextMeshPro")
        elif using.startswith("UnityEngine.InputSystem") and "Unity.InputSystem" not in refs:
            problem(f"{rel(path)}: InputSystem without Unity.InputSystem")
        elif using in ("UnityEngine.UI", "UnityEngine.EventSystems") and "UnityEngine.UI" not in refs:
            problem(f"{rel(path)}: {using} without UnityEngine.UI")
        elif using.startswith("UnityEditor") and asm.get("includePlatforms") != ["Editor"] \
                and "#if UNITY_EDITOR" not in text:
            problem(f"{rel(path)}: UnityEditor in a runtime assembly")
        elif using == "NUnit.Framework" and "nunit.framework.dll" not in asm.get("precompiledReferences", []):
            problem(f"{rel(path)}: NUnit without nunit.framework.dll")
        elif using.startswith("Paowa."):
            prefix = ".".join(using.split(".")[:3]) if using.startswith("Paowa.Core.") else ".".join(using.split(".")[:2])
            if not any(r == prefix or prefix.startswith(r) or r.startswith(prefix) for r in refs if r.startswith("Paowa")):
                problem(f"{rel(path)}: using {using} without a matching Paowa reference")
    if re.search(r"\.DO(Fade|AnchorPos|SizeDelta|Color)\(", text) and "DOTween.Modules" not in refs:
        problem(f"{rel(path)}: DOTween module extension without DOTween.Modules")

# 5. Builders: serialized fields, GameTypes, BuildPaths.
serialized, bases = {}, {}
sources = cs_files + [p for package in local_packages for p in package.rglob("*.cs") if "Tests" not in p.parts]
for path in sources:
    text = texts.get(path) or read(path)
    serialized.setdefault(path.stem, set()).update(
        re.findall(r"\[SerializeField[^\]]*\][^;]*?\s(_\w+)\s*(?:=[^;]*)?;", text))
    for nested, body in re.findall(r"class (\w+)[^{]*\{(.*?)\n        \}", text, re.S):
        serialized.setdefault(nested, set()).update(
            re.findall(r"\[SerializeField[^\]]*\][^;]*?\s(_\w+)\s*(?:=[^;]*)?;", body))
    base = re.search(r"class " + re.escape(path.stem) + r"(?:<[^>]*>)?\s*:\s*(\w+)", text)
    if base:
        bases[path.stem] = base.group(1)
for name in list(serialized):
    parent, seen = bases.get(name), set()
    while parent and parent not in seen:
        seen.add(parent)
        serialized[name] |= serialized.get(parent, set())
        parent = bases.get(parent)

builder_files = sorted(BUILDERS.glob("*.cs")) if BUILDERS.exists() else []
DECLARATION = re.compile(r"(?:var|Component|ScriptableObject|[A-Z]\w+)\s+(\w+)\s*=\s*[^;]*?"
                         r"(?:AddComponent<(\w+)>|GetComponent<(\w+)>|AddComponent\(GameTypes\.(\w+)\)|"
                         r"Asset<(\w+)>|Asset\(GameTypes\.(\w+)|Definition<(\w+)>)")
checked = unresolved = 0
for path in builder_files:
    text = texts[path]
    declarations = sorted((m.start(), m.group(1), next(g for g in m.groups()[1:] if g)) for m in DECLARATION.finditer(text))
    for call in re.finditer(r"Fields\.Set(?:List)?\((.*?)\);\n", text, re.S):
        body = call.group(1)
        target = body.split(",")[0].strip()
        inline = re.search(r"(?:AddComponent<|GetComponent<|Asset<)(\w+)>|GameTypes\.(\w+)", target)
        type_name = (inline.group(1) or inline.group(2)) if inline else None
        if type_name is None:
            candidates = [d for d in declarations if d[1] == target and d[0] < call.start()]
            type_name = candidates[-1][2] if candidates else None
        if type_name is None or type_name == "T":
            unresolved += 1
            continue
        if type_name not in serialized:
            notes.append(f"{rel(path)}: Fields.Set target type {type_name} not found in sources")
            continue
        for field in re.findall(r'\("(_[\w.]+)"', body):
            if field.split(".")[0] not in serialized[type_name]:
                problem(f"{rel(path)}: {type_name} has no [SerializeField] {field.split('.')[0]}")
            else:
                checked += 1
notes.append(f"builder field writes checked: {checked}; targets not inferred (review by hand): {unresolved}")

game_types = BUILDERS / "GameTypes.cs"
if game_types.exists():
    text = texts[game_types]
    consts = dict(re.findall(r'private const string (\w+) = "([\w.]+)";', text))
    for module_const, namespace_const, type_name in re.findall(r'Resolve\((\w+), (\w+), "(\w+)"\)', text):
        namespace = consts.get(namespace_const, consts.get(module_const, ""))
        if not any(p.stem == type_name and f"namespace {namespace}\n" in texts[p] for p in cs_files) and \
                not any(p.stem == type_name for p in sources):
            problem(f"GameTypes: {namespace}.{type_name} not found")
paths_file = BUILDERS / "BuildPaths.cs"
if paths_file.exists():
    paths_text = texts[paths_file]
    for name in set(re.findall(r"BuildPaths\.(\w+)", "\n".join(texts[p] for p in builder_files))):
        if not re.search(r"\b(?:const string|static readonly [\w\[\]]+|static [\w\[\]]+) " + name + r"\b", paths_text):
            problem(f"BuildPaths.{name} undefined")

# 6. Sprites.
sprites_root = GAME / "art" / "sprites"
if sprites_root.exists():
    on_disk = {str(p.relative_to(sprites_root)).replace("\\", "/")[:-4] for p in sprites_root.rglob("*.png")}
    for path in builder_files:
        for sprite in re.findall(r'Sprite\("([\w/-]+)"\)', texts[path]):
            if sprite not in on_disk and not sprite.endswith("-"):
                problem(f"{rel(path)}: sprite {sprite}.png missing")
    # The art spec (written by Tools/ShapeGen with the placeholders) lists exactly the PNGs on disk; the builders import
    # from it, so a PNG outside it, or an entry without its PNG, stops Build All.
    spec_path = GAME / "art" / "art-spec.json"
    if not spec_path.exists():
        problem("Assets/game/art/art-spec.json missing: run python Tools/ShapeGen/generate_shapes.py")
    else:
        art = json.loads(read(spec_path))
        listed = {entry["path"] for entry in art.get("sprites", []) + art.get("sheets", [])}
        if listed != on_disk:
            problem(f"art-spec.json and sprites on disk differ (run Tools/ShapeGen): only in spec {sorted(listed - on_disk)[:10]}, "
                    f"only on disk {sorted(on_disk - listed)[:10]}")
        for sheet in art.get("sheets", []):
            if not sheet.get("tags") or any(tag["frames"] < 1 or tag["frameMs"] < 1 for tag in sheet["tags"]):
                problem(f"art-spec.json: sheet {sheet['path']} needs tags with frames and frameMs >= 1")
    # Packs (Docs/STRUCTURE.md): every PNG sits in a pack folder, each folder is one atlas.
    pack = re.compile(r"^(ui-common|ui-glyphs|backgrounds|screens/[a-z0-9-]+|game/[a-z0-9-]+(/[a-z0-9-]+)?)/[^/]+$")
    for name in sorted(on_disk):
        if not pack.match(name):
            problem(f"art/sprites/{name}.png is not in a pack folder (ui-common, ui-glyphs, screens/<screen>, backgrounds, "
                    "game/<group>[/<name>])")

# 6b. Sounds: the sound spec (written by Tools/AudioGen with the placeholders) lists exactly the WAV files on disk.
audio_root = GAME / "audio"
if audio_root.exists():
    sounds_on_disk = {str(p.relative_to(audio_root)).replace("\\", "/").rsplit(".", 1)[0]
                      for p in audio_root.rglob("*") if p.suffix.lower() in (".wav", ".ogg", ".mp3", ".aif", ".aiff")}
    audio_spec = audio_root / "audio-spec.json"
    if not audio_spec.exists():
        problem("Assets/game/audio/audio-spec.json missing: run python Tools/AudioGen/generate_sounds.py")
    else:
        sound_entries = json.loads(read(audio_spec)).get("sounds", [])
        listed_sounds = {entry["path"] for entry in sound_entries}
        if listed_sounds != sounds_on_disk:
            problem(f"audio-spec.json and sounds on disk differ (run Tools/AudioGen): only in spec "
                    f"{sorted(listed_sounds - sounds_on_disk)[:10]}, only on disk {sorted(sounds_on_disk - listed_sounds)[:10]}")
        for entry in sound_entries:
            if entry["path"].split("/", 1)[0] != entry["category"]:
                problem(f"audio-spec.json: {entry['path']} is not in the folder of its category {entry['category']}")
        for path in audio_root.rglob("*"):
            if path.suffix.lower() in (".ogg", ".mp3", ".aif", ".aiff"):
                problem(f"{rel(path)}: sounds are delivered as .wav (same name as the placeholder)")

# 7. Folders.
for folder in GAME.rglob("*"):
    if folder.is_dir():
        parts = folder.relative_to(GAME).parts
        if any(p in SPECIAL for p in parts[:-1]) or parts[-1] in SPECIAL:
            continue
        if not KEBAB.match(parts[-1]):
            problem(f"folder not kebab-case: {rel(folder)}")

# 8. Localization.
for csv_path in GAME.rglob("Localization/*.csv"):
    rows = [row for row in csv.reader(io.StringIO(read(csv_path).lstrip("﻿"))) if row]
    header, entries = rows[0], [r for r in rows[1:] if not r[0].startswith("#")]
    if header[0] != "key" or len(header) < 2:
        problem(f"{rel(csv_path)}: header {header}")
    keys = [r[0] for r in entries]
    for key in {k for k in keys if keys.count(k) > 1}:
        problem(f"{rel(csv_path)}: duplicate key {key}")
    for row in entries:
        if len(row) != len(header) or any(not cell for cell in row):
            problem(f"{rel(csv_path)}: incomplete row {row[0]}")
            continue
        shapes = [set(PLACEHOLDER.findall(cell)) for cell in row[1:]]
        if any(s != shapes[0] for s in shapes):
            problem(f"{rel(csv_path)}: placeholders differ in {row[0]}")
    for code in header[1:]:
        if f"language.{code}" not in keys:
            problem(f"{rel(csv_path)}: missing language.{code}")
    view_ids = set()
    builder_keys = set()
    for path in builder_files:
        view_ids.update(re.findall(r'"_(?:viewId|id|anchorId|iapProductId|entitlementId)",\s*"([\w.-]+)"', texts[path]))
        view_ids.update(re.findall(r'\b\w*Anchor\w*\([^;]*?"([\w.-]+)"', texts[path]))
        view_ids.update(re.findall(r'\bExplain\(\s*"\w+",\s*"([\w.-]+)"', texts[path]))
        view_ids.update(re.findall(r'\("\w+",\s*BuildPaths\.\w+,\s*"([\w.-]+)"\)', texts[path]))
        view_ids.update(re.findall(r'"([\w.-]+)"\s*\)\s*;\s*//\s*anchor', texts[path]))
        # Setting definition ids: Toggle/Slider/Choice/Button/Binding(tab, "AssetName", "id", ...).
        view_ids.update(re.findall(r'\b(?:Toggle|Slider|Choice|Button|Binding)\(\w+,\s*"\w+",\s*"([\w.-]+)"',
                                   texts[path]))
        # Setting definition ids: Toggle/Slider/Choice/Button/Binding(tab, "AssetName", "id", ...).
        view_ids.update(re.findall(r'(?:Toggle|Slider|Choice|Button|Binding)\(\w+,\s*"\w+",\s*"([\w.-]+)"', texts[path]))
        builder_keys.update(k for k in re.findall(r'"([a-z][a-z0-9.-]*)"', texts[path]) if KEY_SHAPE.match(k))
    for key in sorted(builder_keys - view_ids - set(keys)):
        if not re.search(r"\.(asset|prefab|unity|png|csv|html)$|^(game|paowa|com)\.", key):
            problem(f"builder string {key} looks like a localization key but is not in {csv_path.name}")

# 9. Input actions.
for asset_path, asset in parsed.items():
    if asset_path.suffix != ".inputactions":
        continue
    schemes = {s["bindingGroup"] for s in asset.get("controlSchemes", [])}
    ids = []
    all_actions = set()
    for m in asset["maps"]:
        ids += [m["id"]] + [a["id"] for a in m["actions"]] + [b["id"] for b in m["bindings"]]
        names = {a["name"] for a in m["actions"]}
        all_actions |= {f"{m['name']}/{n}" for n in names}
        for b in m["bindings"]:
            if b["action"] not in names:
                problem(f"{rel(asset_path)}: binding to unknown action {m['name']}/{b['action']}")
        if m["name"] != "UI" and schemes:
            for a in m["actions"]:
                for scheme in schemes:
                    if not any(b["action"] == a["name"] and scheme in b.get("groups", "").split(";")
                               for b in m["bindings"]):
                        problem(f"{rel(asset_path)}: {m['name']}/{a['name']} lacks a {scheme} binding")
    for duplicate in {i for i in ids if ids.count(i) > 1}:
        problem(f"{rel(asset_path)}: duplicate id {duplicate}")
    map_names = "|".join(re.escape(m["name"]) for m in asset["maps"])
    for path, text in texts.items():
        for action in re.findall(r'"((?:' + map_names + r')/\w+)"', text):
            if action not in all_actions and "Tests" not in path.parts and "tests" not in path.parts:
                problem(f"{rel(path)}: action {action} not in {asset_path.name}")

# 10. Config ResourcePath constants match the builder save locations.
all_builder_text = "\n".join(texts[p] for p in builder_files)
for path, text in texts.items():
    match = re.search(r'ResourcePath\s*=\s*"([\w/.-]+)"', text)
    if match and builder_files:
        resource = match.group(1)
        if f"Resources/{resource}" not in all_builder_text and f"{pathlib.PurePosixPath(resource).name}.asset" \
                not in all_builder_text and f"{pathlib.PurePosixPath(resource).name}.prefab" not in all_builder_text:
            problem(f"{rel(path)}: ResourcePath {resource} is not created by any builder")

# 11. Name resolution, with the checker shipped in the packages repo (Tools/check_names.py next to the tier folders).
names_tool = next((pkg.parents[1] / "Tools" / "check_names.py" for pkg in local_packages
                   if (pkg.parents[1] / "Tools" / "check_names.py").exists()), None)
if names_tool is None:
    notes.append("name resolution skipped: no Tools/check_names.py beside the file: packages")
else:
    sys.path.insert(0, str(names_tool.parent))
    import check_names  # noqa: E402

    for item in check_names.main(local_packages + [ROOT / "Assets"]):
        problem(item)

for note in notes if "-v" in sys.argv else [n for n in notes if not n.startswith("review ?.")]:
    print("  " + note)
print()
print("\n".join(problems) if problems else "No problems.")
sys.exit(1 if problems else 0)
