# Skxll Island

![Skxll Island cover](https://img.itch.zone/aW1nLzMwMDg0NTcyLnBuZw==/original/bRi35w.png)

A pirate captain finally gets his hands on the old Pirate Prince's chest of gold. Little did he know that this OLD GOLD was cursed...

Skxll Island is a short, round-based action game. Skeletons board your pirate ship and try to steal the gold from your chest. You fight them off, take back any gold they grab, and carry it back to the chest.

We made it in Unity for [GBJAM 14](https://itch.io/jam/gbjam-14) (September 11 to 21, 2026). The jam's main theme was "GameBoy" and the secondary theme was "OLD GOLD". Entries had to stick to the Game Boy's 160x144 resolution, show at most four colors at once, and use only the Game Boy's buttons.

**[Play it in your browser on itch.io](https://jhorro.itch.io/skxll-island)**

![Gameplay](https://img.itch.zone/aW1hZ2UvNTAyMTYxOC8zMDE0NTkyMy5naWY=/original/gY0Qb7.gif)

## Screenshots

<p>
  <img src="https://img.itch.zone/aW1hZ2UvNTAyMTYxOC8zMDEyOTczNC5wbmc=/original/Mad0hL.png" width="32%">
  <img src="https://img.itch.zone/aW1hZ2UvNTAyMTYxOC8zMDEyOTczNS5wbmc=/original/tLu8Kx.png" width="32%">
  <img src="https://img.itch.zone/aW1hZ2UvNTAyMTYxOC8zMDEyOTczNi5wbmc=/original/BgeEyj.png" width="32%">
  <img src="https://img.itch.zone/aW1hZ2UvNTAyMTYxOC8zMDEyOTczMi5wbmc=/original/oTiVga.png" width="32%">
  <img src="https://img.itch.zone/aW1hZ2UvNTAyMTYxOC8zMDEyOTczMy5wbmc=/original/EkkPdi.png" width="32%">
</p>

## Controls

| Action | Keyboard | Gamepad (Xbox / PS) |
| --- | --- | --- |
| Move | WASD or arrow keys | D-pad or left stick |
| Fire | F | X / Square |
| Interact, advance text, confirm | E | A / Cross |
| Pause | P or Esc | Start / Options |

<p>
  <img src="https://img.itch.zone/aW1nLzMwMTI1NDk5LnBuZw==/original/WowMUo.png" width="48%" alt="Keyboard layout">
  <img src="https://img.itch.zone/aW1nLzMwMTI1MzczLnBuZw==/original/b%2FVroM.png" width="48%" alt="Gamepad layout">
</p>

## Running the project

You need Unity 6000.3.12f1. The project uses the Input System and TextMesh Pro and builds to WebGL with the custom template in `Assets/WebGLTemplates`.

1. Clone the repo:
   ```
   git clone https://github.com/vinicimdev/GBJAM14-Project-C-11-26.git
   ```
2. Open the folder in Unity Hub.
3. Open `Assets/Scenes/Splash.unity` and press Play.

```
Assets/
  Art/          sprites, palettes, fonts
  Prefabs/      reusable objects and UI windows
  Scenes/       Splash, Menu, Cutscene, Level
  Scripts/
    Player/     movement, shooting, health, ship controls
    Enemy/      movement, attacks, spawning
    Environment/
    UI/         menus, settings, cutscenes
```

## Team

| Who | What they did |
| --- | --- |
| [Jhonnatan Barbosa](https://jhorro.com) | Game and art direction, design, cinematics, UI |
| [Vinicius Januzzi](https://vinicimdev.github.io) | Gameplay, enemy AI, UI programming, music, SFX |
| Zander Aguirre | 3D art |
| Paul A | SFX and VFX |

Special thanks to **Josemar J**, who joined as a guest collaborator and composed music for the game.
