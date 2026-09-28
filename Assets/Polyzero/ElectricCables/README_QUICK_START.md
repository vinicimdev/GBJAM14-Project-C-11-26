# Polyzero Electric Cables

Procedural electric cable generation for Unity pole chains.

Create socket-based utility pole prefabs, duplicate poles in your scene, and generate believable electric cables automatically between matching sockets. Includes wind animation, per-cable variation, LOD, baking, quality modes, setup validation and local broken wire details.

---

## Quick Start

Open:

```text
Polyzero > Spline > Electric Cable Setup Wizard
```

Then:

1. Assign your pole prefab.
2. Choose the number of sockets per pole.
3. Create the scene `PoleChain`.
4. Choose a quality mode.
5. Press `Setup Prefab + Scene Chain`.
6. Move the generated `CableSocket_01`, `CableSocket_02`, etc. on the pole prefab.
7. Duplicate pole instances as children of `PoleChain`.
8. Select `PoleChain` and press `Rebuild Chain`.

The system connects matching socket indices automatically:

```text
Pole_01 Socket_01 -> Pole_02 Socket_01
Pole_01 Socket_02 -> Pole_02 Socket_02
Pole_01 Socket_03 -> Pole_02 Socket_03
```

---

## Main Features

- Guided setup wizard.
- Socket-based pole workflow.
- Automatic cable generation between consecutive poles.
- Procedural mesh tube cables.
- Wind sway with per-cable variation.
- Distance-based LOD.
- Bake all cables / return to dynamic.
- Quality modes: Mobile, Balanced, High Quality, Cinematic.
- Cable profiles for reusable presets.
- Local broken/hanging wire presets near sockets.
- Validate Setup and Fix Common Issues tools.
- Built-in, URP and HDRP fallback material workflow.
- Clean custom inspectors.

---

## Recommended Hierarchy

```text
PoleChain
├── Pole_01
├── Pole_02
├── Pole_03
├── AutoCable_01_01
├── AutoCable_01_02
└── AutoCable_02_01
```

In `Direct Children` mode, poles must be direct children of the `PoleChain` object.

---

## Main Inspector

Select `PoleChain`.

Tabs:

- `Start` — validation, fixes, profiles, quality and rebuild.
- `Look` — width, sag, material, color and mesh quality.
- `Motion` — wind and cable variation.
- `Optimize` — LOD, bake and dynamic restore.
- `Damage` — local broken cable controls.
- `Advanced` — discovery/material/raw settings.

Most workflows only need `Start`, `Look`, `Motion` and `Optimize`.

Expand `Generated Cables` in `Start` to select a specific generated cable or click its `Invert` / `Restore` button. The same normal control is available when that cable is selected directly.

---

## Validation

Use `Validate Setup` if cables do not appear or connect incorrectly.

It checks:

- pole count;
- missing hubs;
- missing sockets;
- null socket references;
- material/shader compatibility;
- generated cable count.

Use `Fix Common Issues` to restore safe defaults, apply current settings and rebuild.

---

## Damage Workflow

For believable damaged poles, use local broken wires near sockets.

Available presets:

- None
- One Loose Wire
- Two Loose Wires
- Hanging Jumper
- Messy Old Pole

Avoid breaking long generated cables mid-span unless you intentionally want a stylized look.

---

## Documentation

Full documentation is available in:

```text
Assets/Polyzero/ElectricCables/Documentation/USER_MANUAL.md
Assets/Polyzero/ElectricCables/Documentation/STORE_PAGE_DRAFT.md
```

---

## Troubleshooting Summary

### No cables appear

Run `Validate Setup`, confirm poles are direct children of `PoleChain`, then press `Rebuild Chain`.

### Cables or demo assets are purple

Wait for script compilation to finish. If the project changed render pipeline after importing, run `Tools > Polyzero > Electric Cables > Refresh Render Pipeline Compatibility`, then press `Fix Common Issues` on the chain if needed.

### Wind looks synchronized

Enable `Vary Each Cable` and rebuild.

### Baked cables do not move

Enable `Baked Keeps Wind` and bake again.

---

## Render Pipeline Support

The package detects the active render pipeline on import, adapts the included demo materials, cleans incompatible pipeline-only components from the demo when needed, and configures the runtime fallback material for:

- Built-in Render Pipeline
- Universal Render Pipeline
- High Definition Render Pipeline

For final production art, you can still assign your own pipeline-specific material.
