![Data‑Stream Beacon Logo](images/project1.png)

**Hardline Prophet**\
*When Progress Is Your Only Religion.*

# Hardline Prophet — Design Blueprint

## 1. Design Pillars

- **Atmospheric Minimalism**\
  Keep the interface lean—every element serves a purpose in the cyber‑CLI vibe.
  > [!TIP]\
  > Use subtle ASCII noise/scan‑lines in backgrounds rather than bulky art assets.
- **Player Agency via Choices**\
  Even in an “idle” loop, let the player steer risk versus reward through timely upgrades and special jobs.
- **Seamless Flow**\
  Splash → Menu → Idle Progress → Actions → Save/Exit should feel like one uninterrupted sequence.

---

## 2. Core Gameplay Loop

1. **Logon** → Load `GameState`
2. **Idle Progress Cycle**
   - Timer ticks every N seconds → “█” bar advances + mission results append to log
   - Credits and experience awarded
3. **Intervene** (optional)
   - **Upgrade Implants** (spend credits)
   - **Accept Special Job** (one‑off mission with modifiers)
4. **Repeat** until player hits “Logoff” or “Shutdown”

> [!IMPORTANT]\
> If the player takes too long to intervene, introduce “Overheat” or “Trace” penalties to keep tension.

---

## 3. Systems Overview

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

## 8. Future Roadmap

1. **Flavor Modules:** Plug‑and‑play narrative events
2. **Leaderboard Upload:** Spectre.Console CLI command
3. **Themed Skins:** Alternate ASCII art palettes

> [!NOTE]\
> This blueprint sits in `README.md`—contributors can jump right in without separate docs.

