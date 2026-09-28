# Polyzero Electric Cables — User Manual

## 1. Overview

Polyzero Electric Cables is an editor-focused Unity tool for generating procedural electric cables between utility poles.

The system is based on three simple concepts:

1. A pole prefab has cable sockets.
2. Pole instances are placed under a `PoleChain` object.
3. The generator connects matching socket indices between consecutive poles.

Example:

```text
Pole_01 Socket_01 -> Pole_02 Socket_01
Pole_01 Socket_02 -> Pole_02 Socket_02
Pole_01 Socket_03 -> Pole_02 Socket_03
```

## 2. Installation

Import the package into your Unity project.

Recommended final package layout:

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

In the current development version, runtime/editor scripts may still exist under:

```text
Assets/Game/Spline
```

## 3. Quick Start

1. Open the setup wizard:

```text
Polyzero > Spline > Electric Cable Setup Wizard
```

2. Assign your pole prefab.
3. Select how many sockets each pole should have.
4. Choose the scene chain name. Default:

```text
PoleChain
```

5. Choose a quality mode.
6. Press:

```text
Setup Prefab + Scene Chain
```

7. Move the generated socket transforms on the prefab to the correct visual attachment points.
8. Duplicate pole instances under `PoleChain`.
9. Select `PoleChain` and press:

```text
Rebuild Chain
```

## 4. Pole Prefab Setup

The wizard adds these objects/components to the pole prefab:

```text
Pole Prefab
├── CableSockets
│   ├── CableSocket_01
│   ├── CableSocket_02
│   └── CableSocket_03
├── ElectricPoleCableHub
└── ElectricPoleLocalBrokenCables optional
```

Move the sockets manually to match the pole model.

The socket names are not used for linking. The array index/order in `ElectricPoleCableHub` controls the connection.

## 5. Scene Setup

Recommended scene hierarchy:

```text
PoleChain
├── Pole_01
├── Pole_02
├── Pole_03
├── AutoCable_01_01
├── AutoCable_01_02
└── AutoCable_02_01
```

If using Direct Children mode, all pole instances should be direct children of the `PoleChain` object.

## 6. Main Inspector

Select `PoleChain`. The inspector is divided into tabs.

### Start

Use this tab for normal workflow.

Important buttons:

- `Validate Setup`
- `Fix Common Issues`
- `Apply Profile`
- `Apply Profile + Rebuild`
- `Create Default Profiles`
- `Apply Quality + Rebuild`
- `Rebuild Chain`
- `Clear`

Expand `Generated Cables` to inspect every generated wire. Click a cable name to select it, or use its `Invert` / `Restore` button to change only that cable's normals. The override follows the same socket pair when the chain rebuilds. The selected cable's own inspector also provides the same control.

### Look

Controls the visual shape of generated cables:

- cable width;
- sag;
- material;
- fallback color;
- segment count;
- roundness.

Use `Apply Look To Existing Cables` if cables already exist.

### Motion

Controls wind:

- enable wind;
- amount;
- speed;
- wave length;
- direction;
- lock ends;
- vary each cable.

Keep `Vary Each Cable` enabled for more natural movement.

### Optimize

Controls:

- bake all cables;
- return to dynamic;
- LOD distances;
- near/mid/far quality;
- baked wind behavior.

### Damage

Recommended for local damage presets on poles.

Use:

- `Randomize Local Damage On Poles`
- `Clear Local Damage On Poles`

Avoid using long mid-span broken AutoCables unless you have a specific stylized use case.

### Advanced

Contains lower-level settings and the raw inspector option.

## 7. Profiles

Profiles are ScriptableObjects that store cable settings.

Create defaults from:

```text
Create Default Profiles
```

Default profiles:

- Thin Power Cable
- Thick Power Cable
- Loose Old Cable
- Telephone Wire

Use `Apply Profile + Rebuild` to apply a profile to the chain and regenerate all cables.

## 8. Quality Modes

Quality modes provide a fast way to configure mesh density and LOD.

### Mobile

Low mesh density and aggressive LOD. Best for mobile or very large scenes.

### Balanced

Recommended default.

### High Quality

Better close-up fidelity.

### Cinematic

Highest quality. Best for trailers, screenshots and hero shots.

## 9. Wind

Wind is procedural. It is not physics simulation.

Recommended defaults:

```text
Amount: 0.04
Speed: 3.32
Wave Length: 0.14
Vary Each Cable: true
```

Per-cable variation prevents wires from moving in the exact same phase.

## 10. LOD

LOD reduces cable complexity based on distance from the camera.

Main controls:

- Near Distance
- Mid Distance
- Far Distance
- Near Quality
- Mid Quality
- Far Quality
- Near/Mid/Far Roundness

If `LOD Camera` is empty, the system tries to use `Camera.main`.

## 11. Bake Workflow

Use `Bake All Cables` to freeze cable topology and LOD.

Use `Return To Dynamic` to restore procedural behavior.

Baked cables can still animate with wind when:

```text
Baked Keeps Wind = true
```

## 12. Local Broken Cables

Local broken cables are short hanging or jumper wires near pole sockets.

Presets:

- None
- One Loose Wire
- Two Loose Wires
- Hanging Jumper
- Messy Old Pole

This is the recommended damage workflow because it avoids unrealistic floating broken wires across long spans.

## 13. Validation

Press `Validate Setup` to check for common problems.

The validation checks:

- pole count;
- missing hubs;
- missing sockets;
- null socket references;
- generated cable count;
- material/shader compatibility.

The result appears in the inspector and a detailed report is printed to the Console.

## 14. Fix Common Issues

`Fix Common Issues` attempts to:

- restore safe generation defaults;
- enable material fallback;
- replace unsupported materials;
- apply current settings to generated cables;
- rebuild the chain.

## 15. Troubleshooting

### No cables appear

Check:

- poles are direct children of PoleChain;
- at least two poles exist;
- each pole has ElectricPoleCableHub;
- sockets are assigned;
- `Rebuild Chain` has been pressed.

Run `Validate Setup`.

### Cables connect in the wrong order

Direct Children mode uses hierarchy order. Reorder poles under PoleChain.

### Cables or demo assets are purple

The package normally adapts its demo materials automatically after import. If the project changed render pipeline later:

1. Run `Tools > Polyzero > Electric Cables > Refresh Render Pipeline Compatibility`.
2. Leave the chain material empty or assign a compatible production material.
3. Enable `Auto Material If Empty`.
4. Enable `Replace Unsupported Material`.
5. Press `Fix Common Issues`.

### Wind looks fake or synchronized

Enable:

```text
Vary Each Cable
```

Then rebuild the chain.

### Baked cables stopped moving

Check:

```text
Baked Keeps Wind = true
```

Then bake again.

### Local broken cables do not appear

Check that the pole prefab has:

```text
ElectricPoleLocalBrokenCables
```

Then apply a damage preset and rebuild.

## 16. Render Pipeline Compatibility

The editor detects the active pipeline after import, converts the included demo materials, and removes incompatible HDRP-only components from the demo scene when required. Runtime fallback material logic supports:

- Built-in Render Pipeline
- Universal Render Pipeline
- High Definition Render Pipeline

The cable scripts do not directly reference URP or HDRP assemblies. For production, you can still assign your own material made specifically for your render pipeline.

## 17. Best Practices

- Use the wizard for first setup.
- Use profiles for consistent scenes.
- Keep long generated cables intact.
- Use local damage presets near sockets.
- Use quality mode before manually tweaking LOD.
- Bake finished scenes for stable topology.
- Test the package in a clean Unity project before shipping.
