# 🧠 **Hardline Prophet**

### *💻 When Progress Is Your Only Religion.*

![Hardline Prophet Logo](images/project1.png)

✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦

## 🚀 Project Milestones

### Milestone 1: Core Loop Proof-of-Concept

**Goal:** Create a basic, working sample demonstrating the core mechanics.

**Features:**
* **Project Foundation:** Solution setup (`HardlineProphet` + `HardlineProphet.Tests`), core models (`GameState`, `PlayerStats`).
* **Persistence:** Basic Save/Load (`${Username}.save.json`), Versioning, Checksum validation.
* **New Player:** Default `GameState` creation (Username prompt only).
* **Basic Mission:** Load and repeat one simple mission from `missions.json`.
* **Tick Engine (`TickService`):** Main loop timer, `HackSpeed` affects interval, progress mission, award Credits/XP, log basic messages, auto-restart mission.
* **Basic UI (`Terminal.Gui`):** Splash, Main Menu (Logon, Shutdown), Logon Prompt, In-Game view (progress bar, log, Credits/XP/HackSpeed).
* **Core Loop:** Logon -> Idle Loop Runs -> Shutdown (Save).

**Explicitly Excluded from M1:** Complex profile creation, Mission Generation, Upgrades/Shop, Flavor Events, Trace Mechanics, Stealth/DataYield effects, Perks, Dev Menu/Editors, Passwords, Multiple mission types.

*(Future milestones will build upon this foundation, adding features like upgrades, diverse missions, trace mechanics, etc.)*

✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦

# 🌐 Design Blueprint

## 🔹 1. Design Pillars

✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦

- **Atmospheric Minimalism**\
  Keep the interface lean—every element serves a purpose in the cyber‑CLI vibe.
- **Player Agency via Choices**\
  Even in an “idle” loop, let the player steer risk versus reward through timely upgrades and special jobs.
- **Seamless Flow**\
  Splash → Menu → Idle Progress → Actions → Save/Exit should feel like one uninterrupted sequence.

> 💡 **TIP:**\
> Use subtle ASCII noise or scan‑lines in backgrounds rather than bulky art assets.

---

## 🔹 2. Core Gameplay Loop

✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦

1. **Logon** → Load `GameState`
2. **Idle Progress Cycle**
   - Timer ticks every N seconds → “█” bar advances + mission results append to log
   - Credits and experience awarded
3. **Intervene** (optional)
   - **Upgrade Implants** (spend credits)
   - **Accept Special Job** (one‑off mission with modifiers)
4. **Repeat** until player hits “Logoff” or “Shutdown”

> 🔐 **IMPORTANT:**\
> If the player takes too long to intervene, introduce “Overheat” or “Trace” penalties to keep tension.

---

## 3. Systems Overview

### ⚙️ 3.6 Flavor Events

Flavor events introduce world-building text and optional effects. Triggered during ticks, logins, or milestone completions.

**Example structure:**
```json
{
  "id": "event_001",
  "trigger": "onTick",
  "chance": 0.02,
  "text": "A rogue AI whispers from the void.",
  "effect": { "stealth": +1 }
}
```

- **Trigger Types:** onTick, onLogin, onLevelUp
- **Effects:** Stat bonuses, temp boosts, trace adjustments, flavor-only

> [!TIP]
> Events can be defined in JSON and hot-loaded in dev mode. Keep flavor modular for expansion.

### ⚙️ 3.5 Idle Tick Engine Design

#### Tick Flow Diagram

```text
┌──────────────┐
│  Start Tick  │
└──────┬───────┘
       ▼
┌────────────────────────────┐
│ Wait for Tick Interval     │◄────────────┐
└────────────┬───────────────┘             │
             ▼                             │
┌────────────────────────────┐             │
│ Apply Mission Progress     │             │
└────────────┬───────────────┘             │
             ▼                             │
┌────────────────────────────┐             │
│ Award Rewards (XP/Credits) │             │
└────────────┬───────────────┘             │
             ▼                             │
┌────────────────────────────┐             │
│ Append Outcome to Log      │             │
└────────────┬───────────────┘             │
             ▼                             │
┌────────────────────────────┐             │
│ Check Trace Risk           │             │
└────────────┬───────────────┘             │
             ▼                             │
┌────────────────────────────┐             │
│ Trigger Flavor Event?      │             │
└────────────┬───────────────┘             │
             ▼                             │
      (Loop or Wait Again) ────────────────┘
```

The tick engine drives core automation and pacing of the game loop. It should be efficient, interruptible, and deterministic for consistent state management.

- **Tick Interval:** Fixed default (e.g., 2 seconds), modified by `HackSpeed` stat.
- **Tick Lifecycle:**
  1. Wait X seconds
  2. Apply mission progress
  3. Award rewards (XP, credits)
  4. Append outcome to log
  5. Check Trace risk
  6. Trigger flavor/narrative events (occasionally)

- **Pause Conditions:**
  - During modal dialogs or menu navigation
  - Game paused by user or dev override

- **Tick Modifiers:**
  - `HackSpeed` shortens delay between ticks
  - Trace accumulation increases urgency
  - Certain perks can double reward or allow multi-tick execution

- **Trace Mechanics:**
  - Each tick rolls against mission `traceRisk`
  - Failure adds to a global `TraceLevel`
  - High TraceLevel may cause events, failure, or cooldowns

> ⚠️ **CAUTION:**
> Trace penalties stack across missions and persist until manually cleared or after cooldown windows.

- **Dev Mode Perks:**
  - Adjust tick rate or inject test missions
  - Skip cooldowns or force trigger trace for debugging

### 3.4 Player Profile & Perks

> 🧠 **NOTE:**
> A new player (no save file) will be prompted to set up their profile during the **Logon** process.

### 🧭 Profile Setup Flow (New Player)

#### UI Flow Diagram
```text
┌──────────────────────────────┐
│  New User Detected           │
└─────────────┬────────────────┘
              ▼
┌──────────────────────────────┐
│ Prompt: Username / Password  │
└─────────────┬────────────────┘
              ▼
┌──────────────────────────────┐
│ Choose Starting Class        │
│ (Runner / Broker / Ghost)    │
└─────────────┬────────────────┘
              ▼
┌──────────────────────────────┐
│ Select One Starting Perk     │
└─────────────┬────────────────┘
              ▼
┌──────────────────────────────┐
│ Choose Difficulty Modifiers  │
└─────────────┬────────────────┘
              ▼
┌──────────────────────────────┐
│ Confirm Setup & Save Profile │
└─────────────┬────────────────┘
              ▼
       → Begin Gameplay ←
```
- Prompt for: `Username`, `Password`, `Starting Class`
- Allow choosing one starting perk
- Assign default stats based on selected class

### Starting Classes

Each class starts with a unique stat allocation and small bonus to differentiate early strategies:

| Class   | Description                         | HackSpeed | Stealth | DataYield | Bonus                      |
|---------|-------------------------------------|-----------|---------|-----------|----------------------------|
| Runner | Fast, reckless intrusion             | +10%      | +5      | +0%       | Bonus XP for fast missions |
| Broker | Profits from clean data resale      | +0%       | +5      | +10%      | Starts with 250 credits    |
| Ghost  | Stealthy, avoids trace buildup      | +5%       | +15     | +0%       | 10% reduced trace chance   |

> [!TIP]
> Starting classes only affect initial stats and flavor. All builds can evolve in any direction.

### Starting Perk Pool

New players select one starting perk. These are exclusive early-game benefits:

| Perk Name           | Description                             |
|---------------------|-----------------------------------------|
| Trace Dampener      | -25% trace build-up rate                |
| Stim Surge          | First 5 missions complete instantly     |
| Seed Capital        | Start with an extra 500 credits         |
| Soft Override       | First failure is auto-converted to success |

> [!NOTE]
> Additional perks are unlocked permanently through progression or achievements.

### Permanent Perks
- Unlocked by reaching XP milestones, mission streaks, or purchasing with premium currency.
- Examples:
  - *Black ICE Immunity*: +50% trace resistance
  - *Quantum Tap*: +5% credits on all missions

- **Permanent Perks:**
  - Unlocked by reaching XP milestones, mission streaks, or purchasing with premium currency.
  - Examples:
    - *Black ICE Immunity*: +50% trace resistance
    - *Quantum Tap*: +5% credits on all missions

- **Difficulty Modifiers:**
  - Set during `Logon` or first time profile creation
  - Includes:
    - *Trace Mode*: Higher risk of mission failures
    - *Ironlink*: Save-once-per-session mode
    - *NoLogoff*: Save only on shutdown

- **Unlockable Game Modes:**
  - Each tied to profile-level achievements or manual opt-in
  - Examples:
    - *Silent Stack*: All upgrades hidden until discovered
    - *Burst Cycle*: Progress bar resets every 10 minutes for bonus XP

### 3.1 Mission Generator

- **Types:** Data Heist, Smuggle, Counter‑ICE
- **Parameters:**
  - Difficulty (affects duration & reward)
  - Risk (chance of “Trace” penalty)
- **Output:**
  ```json
  {
    "id": "job_001",
    "type": "DataHeist",
    "duration": 30,        // seconds
    "reward": { "credits": 50, "xp": 10 },
    "traceRisk": 0.1       // 10% per tick
  }
  ```
  > [!NOTE]\
  > Keep mission templates in a JSON file so you can hot‑swap new job types.

### 3.2 Progression & Stats

- **Stats:**
  - `HackSpeed` (ticks/sec)
  - `Stealth` (reduces Trace events)
  - `DataYield` (bonus credits)
- **Leveling:**
  - XP thresholds per level (`level^1.5 * baseXP`)
  - Stat points awarded on level up

### 3.3 Economy & Upgrades

- **Currency:** Credits
- **Shop:**
  | Item               | Cost | Effect         |
  | ------------------ | ---- | -------------- |
  | Neural Accelerator | 100  | +10% HackSpeed |
  | Signal Scrambler   | 150  | +5 Stealth     |
  | Data Compressor    | 200  | +15% DataYield |

> [!CAUTION]\
> Upgrades persist only after “Logoff” or “Shutdown” (auto‑save).

---

## 4. UI & Flow

### 4.4 Visual UI Mockups

#### Dev Menu (Developer Mode Only)
```
┌────────────────────────────┐
│         Dev Menu           │
├────────────────────────────┤
│ [1] Edit Items             │
│ [2] Edit Missions          │
│ [3] Edit Flavor Events     │
│ [4] Toggle Tick Debug      │
│                            │
│      [ Return to Game ]    │
└────────────────────────────┘
```

#### JSON Editor View (example: Items)
```
┌────────────────────────────────────────────┐
│ Item Editor - items.json                   │
├────────────────────────────────────────────┤
│ {                                          │
│   "id": "neural_accel",                   │
│   "name": "Neural Accelerator",           │
│   "effect": "+10% HackSpeed",             │
│   "cost": 100                             │
│ }                                          │
├────────────────────────────────────────────┤
│ [ Save & Close ]    [ Cancel ]             │
└────────────────────────────────────────────┘
```

#### Perks Panel (read-only during game, editable in dev)
```
┌───────────────────────────────┐
│         Perk List             │
├───────────────────────────────┤
│ ✓ Trace Dampener              │
│ ✓ Soft Override               │
│ ☐ Black ICE Immunity          │
│ ☐ Quantum Tap                 │
│                               │
│       [ Close Panel ]         │
└───────────────────────────────┘
```


#### Main Menu (Post-Splash)
```
╔══════════════════════════════╗
║         HARDLINE PROPHET     ║
╠══════════════════════════════╣
║ [1] Logon                    ║
║ [2] Logoff                   ║
║ [3] Shutdown                 ║
╚══════════════════════════════╝
```

#### Logon Dialog
```
┌──────────────────────────────┐
│ Username: [______________]   │
│ Password: [**************]   │
│                              │
│      [ Cancel ] [ Logon ]    │
└──────────────────────────────┘
```

#### In-Game View (Idle Screen)
```
╔════════════════════════════════════╗
║ ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒  ║ Progress Bar
╠════════════════════════════════════╣
║ >> Mission: Data Heist [Success]   ║ Log Window
║ >> Credits Gained: 60             ║
║ >> Trace Risk: 0.1                ║
╠════════════════════════════════════╣
║ Stats: HS:12 | ST:18 | DY:9       ║ Status Panel
║ Credits: 540 | XP: 345            ║
╚════════════════════════════════════╝
```

#### Upgrade Prompt
```
┌────────────────────────────────────┐
│ Available Upgrades:                │
│                                    │
│ [ ] Neural Accelerator (+10% HS)   │
│ [ ] Signal Scrambler (+5 ST)       │
│ [ ] Data Compressor (+15% DY)      │
│                                    │
│  [ Close ]  [ Purchase Selected ]  │
└────────────────────────────────────┘
```

> [!TIP]
> These visualizations serve as layout guidance for building Terminal.Gui interfaces.

> [!TIP]
> Modding support is built directly into the **Dev Menu**, allowing for real-time editing of game data via JSON editors.

### 4.1 Splash & Menu

- Animated neon‑glitch splash (2 s)
- MenuBar: **Logon**, **Logoff**, **Shutdown**

### 4.2 In‑Game View

- **Progress Bar:** ASCII block filling
- **Log Window:** Scrollable feed of mission results
- **Status Pane:** Current stats & credits

### 4.3 Dialogs

- **Logon Dialog:**
  - Prompt `Username:` + `Password:` (mask input)
- **Upgrade Prompt:**
  - List items, navigate with arrow keys **and mouse clicks**
- **Special Job Prompt:**
  - Risk/reward summary

> [!IMPORTANT]
> Developer Mode only: launch with `--dev` flag to enable the **Dev** menu.

- **Dev Menu:**
  - Appears only in developer mode (`--dev`)
  - Provides data editors: `Edit Items`, `Edit Missions`, etc.
  - Each option opens a Terminal.Gui JSON editor for corresponding files.

---

## 5. Data Model

> [!IMPORTANT]
> Dev-mode saves should be flagged (`IsDevSave = true`) to avoid mixing test and normal profiles.

```csharp
public class GameState
{
    public string Username { get; set; }
    public int Level { get; set; }
    public double Experience { get; set; }
    public int Credits { get; set; }
    public Dictionary<string, int> Stats { get; set; }
    public List<Mission> ActiveMissions { get; set; }
}
```

> [!TIP]\
> Store a `Version` field to enable future migrations.

---

## 6. Persistence & File I/O

> [!TIP]
> Add support for config/preferences files to store last used profile, theme settings, and dev mode toggles.

### 6.2 Save Integrity Checksum

To protect against file tampering or accidental corruption, a checksum field is added to each save file.

```csharp
public string Checksum { get; set; }
```

The checksum is calculated on all serialized data *excluding* the `Checksum` property itself:

```csharp
public static string ComputeChecksum(GameState state)
{
    var clone = state with { Checksum = null }; // exclude checksum
    var json = JsonSerializer.Serialize(clone);
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
    return Convert.ToBase64String(hash);
}
```

**On Save:**
```csharp
state.Checksum = ComputeChecksum(state);
var json = JsonSerializer.Serialize(state, options);
File.WriteAllText(path, json);
```

**On Load:**
```csharp
var json = File.ReadAllText(path);
var state = JsonSerializer.Deserialize<GameState>(json);
if (!devMode && state.Checksum != ComputeChecksum(state))
    throw new InvalidDataException("Save file integrity check failed.");
```

> 🚨 **WARNING:**
> If checksum fails outside `--dev` mode, the load should abort or prompt the player for recovery.

> [!TIP]
> Inside developer mode, failed checksums will issue a warning but still load the save normally.

### 6.1 GameState Versioning

### GameStateMigrator Class

Encapsulate all migrations in a single utility class to centralize logic and support testing:

```csharp
public static class GameStateMigrator
{
    public static GameState Migrate(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        int version = root.TryGetProperty("Version", out var v) ? v.GetInt32() : 1;

        return version switch
        {
            1 => JsonSerializer.Deserialize<GameState>(json)!,
            2 => UpgradeFromV1ToV2(json),
            _ => throw new NotSupportedException($"Unknown GameState version {version}")
        };
    }

    public static GameState UpgradeFromV1ToV2(string json)
    {
        var legacy = JsonSerializer.Deserialize<GameState>(json)!;
        legacy.Version = 2;
        legacy.Stats.TryAdd("Firewall", 0);
        return legacy;
    }
}
```

> [!TIP]
> Use `GameStateMigrator.Migrate(json)` instead of calling deserialization directly.

To ensure compatibility across future updates, each save file includes a `Version` field:

```csharp
public int Version { get; set; } = 1;
```

> [!TIP]
> Always default to version 1 when deserializing files without a version field.

Version control is managed through a simple enum:

```csharp
public enum GameStateVersion
{
    V1 = 1,
    V2 = 2
    // future versions...
}
```

**Loading logic:**

```csharp
public static GameState Load(string path)
{
    var json = File.ReadAllText(path);
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    int version = root.TryGetProperty("Version", out var v)
        ? v.GetInt32()
        : 1;

    return version switch
    {
        1 => JsonSerializer.Deserialize<GameState>(json)!,
        // future:
        // 2 => UpgradeFromV1ToV2(json),
        _ => throw new NotSupportedException($"Unknown GameState version {version}")
    };
}
```

**On Save:**

```csharp
gameState.Version = (int)GameStateVersion.V1;
var json = JsonSerializer.Serialize(gameState, options);
File.WriteAllText(path, json);
```

> [!IMPORTANT]
> When migrating old save formats, always write to a backup or new file. Never overwrite original saves directly.

#### Future Migration Example:
```csharp
public static GameState UpgradeFromV1ToV2(string json)
{
    var legacy = JsonSerializer.Deserialize<GameState>(json)!;
    legacy.Version = 2;
    legacy.Stats.TryAdd("Firewall", 0);
    return legacy;
}
```

- **Save File:** `${Username}.save.json`
- **On Logon:**
  ```csharp
  var json = File.ReadAllText(path);
  var state = JsonSerializer.Deserialize<GameState>(json);
  ```
- **On Save/Shutdown:**
  ```csharp
  var json = JsonSerializer.Serialize(state, options);
  File.WriteAllText(path, json);
  ```

> [!WARNING]\
> Wrap file reads/writes in try/catch to handle disk errors gracefully.

---

## 7. Testing Strategy

- **Unit Tests (xUnit + NFluent):**
  - Mission generator outputs valid parameters
  - Stat growth formula correctness
  - Save/load round‑trip fidelity
- **Integration Tests:**
  - Simulate multiple ticks → verify progress and credits

---

## 7.1 JSON Schema Files

To support editing and validation in Dev Mode, JSON schema definitions should be created for each data type:

### 🧬 Schema: items.schema.json
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Item",
  "type": "object",
  "required": ["id", "name", "effect", "cost"],
  "properties": {
    "id": { "type": "string" },
    "name": { "type": "string" },
    "effect": { "type": "string" },
    "cost": { "type": "integer", "minimum": 0 }
  }
}
```

### Schema: missions.schema.json
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Mission",
  "type": "object",
  "required": ["id", "type", "duration", "reward", "traceRisk"],
  "properties": {
    "id": { "type": "string" },
    "type": { "type": "string" },
    "duration": { "type": "integer", "minimum": 1 },
    "reward": {
      "type": "object",
      "properties": {
        "credits": { "type": "integer" },
        "xp": { "type": "integer" }
      },
      "required": ["credits", "xp"]
    },
    "traceRisk": { "type": "number", "minimum": 0, "maximum": 1 }
  }
}
```

### Schema: perks.schema.json
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Perk",
  "type": "object",
  "required": ["name", "description"],
  "properties": {
    "name": { "type": "string" },
    "description": { "type": "string" },
    "unlockedByDefault": { "type": "boolean" }
  }
}
```

> [!TIP]
> Schemas live in `/schemas/` and are auto-linked by the JSON editors for validation and code completion.

---

## 8. Future Roadmap

1. **Flavor Modules:** Plug‑and‑play narrative events
2. **Leaderboard Upload:** Spectre.Console CLI command
3. **Themed Skins:** Alternate ASCII art palettes

> [!NOTE]\
> This blueprint sits in `README.md`—contributors can jump right in without separate docs.
---
# Notes
- move constans so they can me modifiable through dev menu
- 