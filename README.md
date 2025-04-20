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

## 🌐 Design Blueprint

### 🔹 1. Design Pillars
✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦

**Atmospheric Minimalism**

Keep the interface lean—every element serves a purpose in the cyber‑CLI vibe.

**Player Agency via Choices**

Even in an “idle” loop, let the player steer risk versus reward through timely upgrades and special jobs.

**Seamless Flow**

Splash → Menu → Idle Progress → Actions → Save/Exit should feel like one uninterrupted sequence.

💡 **TIP:** Use subtle ASCII noise or scan‑lines in backgrounds rather than bulky art assets.

### 🔹 2. Core Gameplay Loop
✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦✦

1.  **Logon** → Load `GameState` (or create new profile)
2.  **Idle Progress Cycle**
    * Timer ticks every N seconds (`HackSpeed` dependent) → “█” bar advances + mission results append to log
    * Credits and experience awarded on completion
3.  **Intervene (Optional - Future Milestone)**
    * Upgrade Implants (spend credits)
    * Accept Special Job (one‑off mission with modifiers)
4.  Repeat until player hits **“Logoff”** or **“Shutdown”** (Save `GameState`)

🔐 **IMPORTANT (Future Milestone):** If the player takes too long to intervene, introduce “Overheat” or “Trace” penalties to keep tension.

### 3. Systems Overview

*(Sections 3.1 to 3.6 remain the same as your original document: Mission Generator, Progression & Stats, Economy & Upgrades, Player Profile & Perks, Idle Tick Engine Design, Flavor Events)*

#### ⚙️ 3.1 Mission Generator
Types: Data Heist, Smuggle, Counter‑ICE
Parameters:
Difficulty (affects duration & reward)
Risk (chance of “Trace” penalty)
Output:

```json
{
  "id": "job_001",
  "type": "DataHeist",
  "duration": 30,        // seconds
  "reward": { "credits": 50, "xp": 10 },
  "traceRisk": 0.1       // 10% per tick
}
[!NOTE]Keep mission templates in a JSON file so you can hot‑swap new job types.⚙️ 3.2 Progression & StatsStats:HackSpeed (ticks/sec)Stealth (reduces Trace events)DataYield (bonus credits)Leveling:XP thresholds per level (level^1.5 * baseXP)Stat points awarded on level up⚙️ 3.3 Economy & UpgradesCurrency: CreditsShop:ItemCostEffectNeural Accelerator100+10% HackSpeedSignal Scrambler150+5 StealthData Compressor200+15% DataYield[!CAUTION]Upgrades persist only after “Logoff” or “Shutdown” (auto‑save).⚙️ 3.4 Player Profile & Perks(Content remains the same)⚙️ 3.5 Idle Tick Engine Design(Content remains the same)⚙️ 3.6 Flavor Events(Content remains the same)4. UI & Flow(Sections 4.1 to 4.3 remain the same: Splash & Menu, In-Game View, Dialogs)4.1 Splash & MenuAnimated neon‑glitch splash (2 s)MenuBar: Logon, Logoff, Shutdown4.2 In‑Game ViewProgress Bar: ASCII block fillingLog Window: Scrollable feed of mission resultsStatus Pane: Current stats & credits4.3 DialogsLogon Dialog:Prompt Username: + Password: (mask input - Password deferred post-M1)Upgrade Prompt:List items, navigate with arrow keys and mouse clicksSpecial Job Prompt:Risk/reward summary4.4 Visual UI Mockups(Mockups remain the same)Dev Menu (Developer Mode Only)┌────────────────────────────┐
│         Dev Menu           │
├────────────────────────────┤
│ [1] Edit Items             │
│ [2] Edit Missions          │
│ [3] Edit Flavor Events     │
│ [4] Toggle Tick Debug      │
│                            │
│      [ Return to Game ]    │
└────────────────────────────┘
Editor View (Conceptual - Form-Based)(This replaces the raw JSON editor mockup)Instead of editing raw JSON, the Dev Menu options will open form-based editors built with Terminal.Gui. For example, editing items would show fields for ID, Name, Effect, Cost, etc.┌────────────────────────────────────────────┐
│ Item Editor - items.json                   │
├────────────────────────────────────────────┤
│ ID:       [neural_accel____________]       │
│ Name:     [Neural Accelerator____]       │
│ Effect:   [+10% HackSpeed________]       │
│ Cost:     [100___________________]       │
│                                            │
├────────────────────────────────────────────┤
│ [ Save & Close ]    [ Cancel ]             │
└────────────────────────────────────────────┘
Perks Panel (read-only during game, editable in dev)(Mockup remains the same)Main Menu (Post-Splash)(Mockup remains the same)Logon Dialog(Mockup remains the same)In-Game View (Idle Screen)(Mockup remains the same)Upgrade Prompt(Mockup remains the same)[!TIP]These visualizations serve as layout guidance for building Terminal.Gui interfaces.[!TIP]Editor Implementation Note: Modding support via the Dev Menu will use form-based Terminal.Gui editors for a user-friendly experience, loading from and saving back to the underlying JSON data files (items.json, missions.json, etc.). This replaces the initial idea of editing raw JSON directly.[!IMPORTANT]Developer Mode only: launch with --dev flag to enable the Dev menu.5. Data Model(Content remains the same, but reflects the PlayerStats class instead of dictionary)// Note: Represents the conceptual model. Actual implementation in Core/Models/
public class GameState
{
    public int Version { get; set; } // e.g., 2
    public string Username { get; set; }
    public int Level { get; set; }
    public double Experience { get; set; }
    public int Credits { get; set; }
    public PlayerStats Stats { get; set; } // Using dedicated class
    public List<string> ActiveMissionIds { get; set; }
    public List<string> UnlockedPerkIds { get; set; }
    public string Checksum { get; set; }
    public bool IsDevSave { get; set; }
    // Other fields: StartingClass, DifficultyModifiers etc.
}

public class PlayerStats
{
    public int HackSpeed { get; set; }
    public int Stealth { get; set; }
    public int DataYield { get; set; }
}
[!TIP]Store a Version field to enable future migrations using GameStateMigrator.[!IMPORTANT]Dev-mode saves should be flagged (IsDevSave = true) to avoid mixing test and normal profiles.6. Persistence & File I/O(Content remains largely the same, referencing the updated GameState model)(Sections 6.1 GameState Versioning and 6.2 Save Integrity Checksum remain conceptually the same)Save File: ${Username}.save.jsonOn Logon: Load using JsonGameStateRepository (handles deserialization, migration via GameStateMigrator, and checksum validation).On Save/Shutdown: Save using JsonGameStateRepository (handles serialization, checksum calculation).[!WARNING]Wrap file reads/writes in try/catch within the repository implementation.[!TIP]Add support for config/preferences files to store last used profile, theme settings, and dev mode toggles.7. Testing Strategy(Content remains the same, add specifics as tests are written)Unit Tests (xUnit + NFluent):Mission logic (when implemented)Stat effects (when implemented)Save/load round‑trip fidelity (including versioning & checksum)GameStateMigrator logicTickService logicIntegration Tests:Simulate multiple ticks → verify progress, rewards, state changes.7.1 JSON Schema Files(Content remains the same)8. Future RoadmapMilestone 2+: Upgrades, Economy, Trace Mechanics, Stealth/DataYield Effects, Mission Generator, Flavor Events, Perks, Dev Menu Implementation