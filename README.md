# FinalProject

This is the repository from which we will create branches to test the different features we are working on.

---

## Project Overview

DeadPowerGame is a WPF (VB.NET) 2D room‑based game.  
The player explores a small building, fights zombies, flips power switches, and searches for a way to escape.  
The game is built around:

- A `Room` system with named rooms and exits (North, South, East, West)
- A `Player` character with health, attack power, gold, and inventory
- `Enemy` entities (e.g., Zombie) with health, attack power, and loot drops
- A simple save/load system using JSON (`SaveData.vb` + `Data/save.json`)

Core UI components:

- `MainWindow.xaml` + `MainWindow.xaml.vb`
- `Canvas` for the room, player, and enemy sprites
- Directional buttons (North/East/South/West)
- Attack, Save, and Light Switch buttons
- Health bars for player and enemy
- Inventory list and combat log

---

## Game Narrative & Setting

Sam Stones is a U.S. paratrooper whose plane is shot down over hostile territory. He watches the aircraft explode and realizes all of his companions are dead. He survives the fall and lands just outside an abandoned concentration camp.

The camp is no longer in operation, but it is not empty. Inside roam zombies — the remains of grotesque experiments conducted when the camp was active. With no other options, Sam ventures inside, hoping to find a radio to contact his base and get rescued.

His mission:

- Survive encounters with zombies  
- Find and turn on the power breaker  
- Reach the radio room and call for extraction  

Sam’s inventory begins with a scoped AK, and the world fiction allows for additional weapons such as an M3, a Grease Gun, and a 1911.

### Settings / Locations

- **Crash site** – Sam lands outside the concentration camp grounds.
- **Main Entrance Room** – first room inside the camp (West Entrance Hall).
- **Power Room** – room where the breaker can be switched on.
- **Radio Room** – final objective where Sam can use the radio to escape.

(Current implementation includes three core rooms: Main Entrance Room, Power Room / East Dark Room, and Radio/Zombie Room / North Zombie Room.)

### Core Mechanics

- **Room navigation**
- Buttons labeled **North**, **East**, **South**, and **West** move Sam between rooms.
- Only available exits are enabled for each room.
- **Character movement**
- Sam can move left/right/up/down on the canvas using the keyboard (W/A/S/D).
- **Combat**
- An **Attack** button (and the spacebar) triggers attacks on the enemy.
- Enemies retaliate, reducing Sam’s health.
- **Saving progress**
- A **Save Game** button writes the current game state to a JSON file so the player can safely exit and resume later.

---

## Current Game Features

### Room & Navigation

- Player starts in **West Entrance Hall**.
- Can move to:
- **East Dark Room** (Power Room)
- **North Zombie Room** (Radio/Zombie Room) from East Dark Room.
- Each `Room` has:
- `Name`
- `Description`
- `Exits` (dictionary of directions → room names)
- Optional `Enemy`

Navigation is handled both by:

- **Keyboard movement**: WASD keys move the player on the canvas.
- **Directional buttons**: North/East/South/West buttons change rooms via exits.

### Player & Enemy

- Player:
- Name: `Sam Stones`
- Health / MaxHealth
- AttackPower
- Gold
- Inventory (List of strings)
- Enemy:
- Example: `Zombie`
- Health / MaxHealth
- AttackPower
- Optional `LootDrop` (e.g., “Rusty Key”)

Combat:

- `Attack` button or **spacebar** triggers a player attack when an enemy is present.
- Player hits enemy first; if enemy survives, it counter‑attacks.
- If enemy dies:
- Loot is added to inventory.
- Room is marked “clear”.
- If player dies:
- Game over dialog, then application closes.

### Save / Load System

- `SaveData.vb` defines the serializable state:
- Player name, health, max health, attack power, gold.
- Current room name.
- Inventory (list of strings).
- Save:
- Serializes `SaveData` to JSON and writes to `Data/save.json`.
- Load:
- Reads `Data/save.json` and rebuilds player and current room state.
- Restores health bars and inventory UI.

---

## Work Completed in This Branch (`my-update-branch`)

Below is a staged list of the major changes and bug fixes implemented so far.

### 1. Collision Detection & Movement

**Goal:** Make movement and room transitions feel consistent and prevent random or stuck collisions.

Changes:

- Switched player sprite movement from `Margin` to `Canvas.SetLeft` / `Canvas.SetTop` so collision rectangles (`Rect`) and on‑screen position use the same coordinate system.
- `imageToRect` reads `Canvas.GetLeft` / `Canvas.GetTop` from the `Canvas`, aligning with how sprites are positioned.
- Collision rectangles for walls and player (`rectNorth`, `rectEast`, `rectSouth`, `rectWest`, `rectPlayer`) are refreshed in `UpdateRoomDisplay` whenever the room changes.
- Added `collisionHandled` flag to prevent the collision handler from firing every render frame while the player is touching the same wall.
- Implemented “no exit” bounce logic:
- When the player collides with a wall **without** a corresponding exit (e.g., no “North” exit), the player is nudged slightly back away from that wall, allowing future collisions to register correctly instead of “locking” the system.

### 2. Spacebar Attack & Focus Handling

**Goal:** Spacebar should always trigger “attack” when appropriate, not re‑trigger whatever button was last clicked (e.g., light switch).

Changes:

- Spacebar attack is now fired once per keypress using an `attackPressed` flag in `Window_KeyDown` / `Window_KeyUp`:
- Prevents attack from being spammed every frame by the game loop while the key is held.
- Removed polling of `spaceKey` from the main `GameLoop` so attack is event‑driven, not frame‑driven.
- For every actionable button (`btnNorth`, `btnSouth`, `btnEast`, `btnWest`, `btnAttack`, `btnSave`, `btnLightSwitch`), added `Me.Focus()` at the end of the click handler:
- Returns keyboard focus to the window after a button click.
- Prevents WPF from rerouting spacebar presses to the last clicked button and ensures spacebar goes through the central keyboard handler.

### 3. Game State & Logging

**Goal:** Keep the UI and log in sync with game state without flooding messages or crashing.

Changes:

- `AddToLog`:
- Handles the first message cleanly without a leading blank line.
- Appends subsequent messages with `vbCrLf` + message.
- Calls `ScrollToEnd` so the most recent log entry is always visible.
- Guards against `txtCombatLog` being `Nothing`.
- `ShowGameOver` shuts down the application cleanly after the game‑over message.
- Save/Load:
- Ensured `save.json` path uses `Data\save.json` consistently.
- Confirmed `SaveData.vb` (Name, Health, MaxHealth, AttackPower, Gold, CurrentRoom, Inventory) matches the fields used in the save/load process.

### 4. Canvas Boundary Clamping

**Goal:** Prevent the player from walking off-screen.

Changes:

- Updated `MoveLeft`, `MoveRight`, `MoveUp`, and `MoveDown` to clamp positions:
- Left edge: `Math.Max(0, ...)`
- Right edge: `Math.Min(mainCanvas.ActualWidth - imgPlayer.Width, ...)`
- Top edge: `Math.Max(0, ...)`
- Bottom edge: `Math.Min(mainCanvas.ActualHeight - imgPlayer.Height, ...)`
- Keeps the entire player sprite visible within the `Canvas`.

### 5. Enemy Health Bar Behavior

**Goal:** Avoid stale enemy health bar values in rooms without enemies.

Changes:

- `UpdateHealthBars`:
- If `currentRoom.Enemy` is `Nothing`, `pbarEnemyHealth.Value` is set to `0`.
- Prevents the red bar from sticking around after leaving a combat room or entering a room with no enemies.

### 6. Room Description Display

**Goal:** Surface the narrative text (`Room.Description`) that was already defined but never displayed.

Changes:

- Added `lblRoomDescription` label in `MainWindow.xaml`, positioned below `lblRoomName`.
- In `UpdateRoomDisplay`, set:
```vb
lblRoomDescription.Content = currentRoom.Description
```
- `lblRoomDescription` uses text wrapping to show multi‑line descriptions without overlapping the enemy health bar.

---

## Git & Project Hygiene

To keep the repository clean:

- Added a `.gitignore` (based on GitHub’s VisualBasic template) under `DeadPower/DeadPowerGame/` to exclude:
- `.vs/` (Visual Studio workspace files)
- `bin/` (compiled binaries)
- `obj/` (build artifacts)

This ensures only source files and relevant assets are tracked in version control.

---

## Branch Workflow

Development is currently happening on:

- Branch: `my-update-branch`

Typical workflow:

1. Implement and test changes on `my-update-branch`.
2. Commit with descriptive messages (e.g., “Fix collision lock, spacebar focus hijack, canvas boundaries, health bar, room description”).
3. Push to GitHub and open a Pull Request into the main branch for review.

---

## Next Steps / Ideas

- Add additional rooms and enemies using the existing `Room` and `Enemy` blueprints.
- Improve combat feedback (visual hit effects or sounds).
- Add basic player stats or a simple main menu / restart flow.
- Expand the save system to track more story progress (keys used, doors unlocked, etc.).

---

_Last updated: after implementing collision fixes, spacebar focus behavior, boundary clamping, enemy bar cleanup, room descriptions, and Git ignore rules._
