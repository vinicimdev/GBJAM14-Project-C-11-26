# Polyzero Electric Cables

Procedural electric cable generation for Unity pole chains.

This package lets you prepare a pole prefab with cable sockets, duplicate pole instances in a scene, and automatically generate procedural cable meshes between matching sockets. It includes wind animation, per-cable wind variation, LOD, bake/return-to-dynamic workflow, quality modes, profiles, validation, and local broken cable details near sockets.

## Quick Start

1. Open `Polyzero > Spline > Electric Cable Setup Wizard`.
2. Drag your pole prefab into `Pole Prefab`.
3. Choose how many cable sockets the pole should have.
4. Click through the wizard and press `Setup Prefab + Scene Chain`.
5. Open the pole prefab or first pole instance and move `CableSocket_01`, `CableSocket_02`, etc. to the correct attachment points.
6. Duplicate pole instances as direct children of the generated `PoleChain` object.
7. Select `PoleChain` and press `Rebuild Chain`.

The generator connects matching socket indices:

```text
Pole_01 CableSocket_01 -> Pole_02 CableSocket_01
Pole_01 CableSocket_02 -> Pole_02 CableSocket_02
Pole_01 CableSocket_03 -> Pole_02 CableSocket_03
```

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

Poles must be direct children of the `PoleChain` object when using `Direct Children` discovery.

## Main Inspector Workflow

Select `PoleChain`. The custom inspector is split into:

- `Start`: validation, common fixes, profiles, quality mode, rebuild/clear.
- `Look`: cable width, sag, material, color and mesh quality.
- `Motion`: wind settings and per-cable variation.
- `Optimize`: bake, return to dynamic, LOD and profile application.
- `Damage`: local damage utilities and legacy long-cable broken tools.
- `Advanced`: discovery/material/raw settings.

Most users should only need `Start`, `Look`, `Motion`, and `Optimize`.

The `Generated Cables` foldout in `Start` has an `Invert` / `Restore` button for every cable. Use it to correct the visible side of an individual cable without changing the rest of the chain. The override is preserved when the chain rebuilds while the same endpoint sockets still exist. Selecting a generated cable directly also exposes the same button in its inspector.

## Validate Setup

Use `Validate Setup` on the `PoleChain` inspector to check:

- whether poles are detected;
- whether each pole has an `ElectricPoleCableHub`;
- whether sockets exist and are not null;
- whether the generated material is valid for the current render pipeline;
- whether generated cables already exist.

Use `Fix Common Issues` to reset safe defaults, enable fallback material generation, apply settings to existing cables, and rebuild.

## Quality Modes

The generator includes four quality modes:

- `Mobile`: low segment counts and aggressive LOD.
- `Balanced`: good default for gameplay.
- `HighQuality`: better visual fidelity for close inspection.
- `Cinematic`: high segment counts for screenshots/trailers.

Use `Apply Quality + Rebuild` to apply the selected mode and rebuild the chain.

## Profiles

Open the `Start` tab and click `Create Default Profiles`.

This creates cable profile assets under:

```text
Assets/Game/Spline/Profiles
```

Default profiles include:

- `Thin_Power_Cable`
- `Thick_Power_Cable`
- `Loose_Old_Cable`
- `Telephone_Wire`

Use `Apply Profile + Rebuild` for the fastest workflow.

## Wind

Use the `Motion` tab on `PoleChain`.

Recommended settings:

```text
Enable Wind = true
Amount = 0.04
Speed = 3.32
Wave Length = 0.14
Vary Each Cable = true
```

Keep `Vary Each Cable` enabled so parallel cables do not move in the same phase.

## Bake / Return To Dynamic

Use `Optimize > Bake All Cables` to freeze cable topology/LOD. Baked cables can still keep wind if `Baked Keeps Wind` is enabled.

Use `Return To Dynamic` to restore full procedural rebuild behavior.

## Local Broken Cables

Long mid-span broken cables usually look unrealistic. The recommended workflow is local broken cables near the pole sockets.

Add or keep `ElectricPoleLocalBrokenCables` on the pole prefab. Presets:

- `None`
- `OneLooseWire`
- `TwoLooseWires`
- `HangingJumper`
- `MessyOldPole`

Use the `Damage` tab on `PoleChain` to randomize or clear local damage across poles.

## Troubleshooting

### Cables do not appear

Check that your pole instances are direct children of `PoleChain`, then press `Validate Setup` and `Rebuild Chain`.

### Cables connect in the wrong order

The child order under `PoleChain` controls connection order. Reorder pole instances in the hierarchy.

### Cable or demo material is purple

The package adapts its demo materials automatically when scripts finish compiling. If the active render pipeline was changed later, run `Tools > Polyzero > Electric Cables > Refresh Render Pipeline Compatibility`, then use `Fix Common Issues` on the chain if needed.

### Wind looks too synchronized

Enable `Vary Each Cable` in the `Motion` tab and rebuild the chain.

### Baked cables do not move

Enable `Baked Keeps Wind`, then bake again or return to dynamic and bake again.

### Broken long cables look wrong

Avoid using broken mode on long `AutoCable` objects. Use local broken cable presets on the pole prefab instead.

## Render Pipeline Support

On import, the package detects the active pipeline and automatically adapts the demo materials and removes incompatible HDRP-only components from the demo scene when necessary. The runtime generator also creates a compatible fallback cable material for:

- HDRP
- URP
- Built-in Render Pipeline

No direct URP or HDRP assembly dependency is required by the cable scripts. For final production art, you can still assign your own pipeline-specific material.

## Suggested Asset Store Package Layout

The project currently keeps scripts under `Assets/Game/Spline` for compatibility with the existing project. For a Store package, migrate to:

```text
Assets/Polyzero/ElectricCables/
├── Runtime
├── Editor
├── Profiles
├── Materials
├── Prefabs
├── Demo
└── Documentation
```

Move files using Unity's Project window so `.meta` files and references are preserved.
