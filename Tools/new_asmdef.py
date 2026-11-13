"""Writes a Unity .asmdef with the project's defaults.

Usage: new_asmdef.py <dir> <name> <ref> [<ref> ...]
Example: python Tools/new_asmdef.py Assets/game/scripts/systems/daily-reward Game.Systems.DailyReward \
             Paowa.Core Game.Contracts Game.Foundation
Folders are lowercase kebab-case; the assembly name (and root namespace) stays PascalCase.
"""
import json
import pathlib
import re
import sys

directory, name, *references = sys.argv[1:]
folder = pathlib.Path(directory).name
if not re.fullmatch(r"[a-z0-9]+(-[a-z0-9]+)*", folder):
    sys.exit(f"Folder '{folder}' must be lowercase kebab-case (for example 'daily-reward').")
data = {
    "name": name,
    "rootNamespace": name,
    "references": references,
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": False,
    "overrideReferences": False,
    "precompiledReferences": [],
    "autoReferenced": True,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": False,
}
path = pathlib.Path(directory) / f"{name}.asmdef"
path.parent.mkdir(parents=True, exist_ok=True)
path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
print(path)
