# Combinight Shift – Delta Challenge 2025
---
## 1.File Structure
### **Folder Organization:**

- **Audio** - Sound effects (SFX) and background music used in the game.
- **Fonts** - Fonts used for the user interface (UI)
- **Hyper Casual Characters** - NPC character models, including cashier, thief, and customer assets.
- **Materials** - All material files used for models, including the texture atlas and trim sheet.
- **Models** - 3D models exported from Maya. 
- **Prefabs** - Fully unpacked models with materials applied, saved as reusable prefabs.
- **Scenes** - All game scenes (e.g., Main Menu, Gameplay Scene).
- **Scripts** - All gameplay and system scripts for the project.
- **Starter Assets** - First-person controller and related assets.
- **Textures** - Image files used for materials and in-game assets (e.g., newspaper images).
- **VFX** - Visual effects used in the game (e.g., Fridge Mist).
- **Videos** - Splash Screen at the start of the game.

---

## 2. Overview

A first-person convenience store simulation where players act as a night-shift cashier to monitor customers via a CCTV system, identify suspicious activity, and apprehend shoplifters before they escape. The game integrates NPC AI, Unity’s Navigation System, raycasting-based interactions, and a day/wave progression system.

**Core Features:**
- NPCs with state-based AI (customers, Cashier & thieves) using FSM-like behavior.
- Unity NavMesh for pathfinding and dynamic movement.
- Raycasting for monitor/Thief interaction and object highlighting.
- Cashier NPC for in-store apprehension
- Day & wave-based progression with increasing difficulty.
- Splash screen, main menu, and smooth scene transitions.

---

## 3. Controls

### Normal Gameplay
- **Movement** - W: Forward / S: Backward, / A: Go left / D: Go right - Player Movement
- **Look Around** - Mouse - First-person camera control
- **Interact with Monitor** - Left Mouse Click - Switch to CCTV camera view
- **Return to Main Camera** - F - Exit CCTV view
- **Apprehend Customers** - Y - Confirm apprehension
- **Cancel Apprehension** - N - Dismiss apprehension popup
- **Pause Menu** - Esc - Pause, Exit or Resume game

### Hacks
- **Skip Day** - J - Move on to the next day
- **Speed Up** - K - Speed up the current wave
- **Skip Splash Screen** - Space Bar - Skip the opening animation for our game

---

## 4. Environment & Navigation

- **NavMesh Baking:** All store interiors and walkable areas are baked for customer and cashier navigation.
- **ShelfSystem** Tagged checkpoints (ShelfCheckpoint) and landmarks (ShelfLandmark) guide NPC browsing.

--- 

## 5. Features

### 5.1 AI & FSM — Customer/Thief + Cashier

**Script:** *Thief.cs* / *CashierBehaviour.cs*
**Purpose:** State-driven NPC behaviour for browsing/setaling customers and thieves, plus a cashier NPC that processes apprehensions.

**Customer NPC — FSM**
**Purpose:** Regular shopper behaviour with occasional warning cues
- **States:**

    - Moving - Walk to a random ShelfCheckpoint (NavMesh).

    - AtShelf - Rotate to face nearest ShelfLandmark.

    - Browsing - Idle for shelfTime while evaluating warning chance.

    - ShowWarningSign - Brief warning indicator (e.g., fidget/looking around) then return to Browsing.

    - Exit - Leave the store (end-of-day or flow directive).

- **Transitions:**

    - Moving → AtShelf (reached destination)

    - AtShelf → Browsing (finished rotate)

    - Browsing → ShowWarningSign (timer ≥ warningInterval and quota not reached)

    - ShowWarningSign → Browsing (~3s display elapsed)

    - Browsing → Moving (shelf time finished → pick new checkpoint)

    - Any → Exit (forced by game flow)
- **Implementation Details:**
    - Controlled by the Thief.cs script with customer settings. Moves between shelf checkpoints using NavMesh, occasionally shows warning signs, and exits when the day ends or told to leave.

**Thief NPC — FSM**
**Purpose:** Looks like a customer but escalates behaviour; “Stealing” is modeled as an elevated‑risk action and/or third warning that confirms guilt.
- **States:**
    - Moving → Walk to a random ShelfCheckpoint.

    - AtShelf → Rotate to face ShelfLandmark.

    - Browsing → Idle while building suspicion.

    - ShowWarningSign → Visible suspicious cue; increments warning count.

    - Stealing → Risk action window (can be represented by the 3rd warning).

    - Exit → Leaves store (caught or forced).
- **Transitions**
    - Moving → AtShelf (destination reached)

    - AtShelf → Browsing (finished rotate)

    - Browsing → ShowWarningSign (timer ≥ warningInterval)

    - ShowWarningSign → Browsing (display over)

    - Browsing/ShowWarningSign → Stealing (suspicion threshold reached, e.g., 3rd cue)

    - Stealing → Exit (if not apprehended)

    - Any → Exit (apprehended by Cashier or day ends)
- **Implementation Details:**
    - Also controlled by the Thief.cs script with thief settings. Displays three warnings before being marked as a confirmed thief. Can be apprehended by the cashier or will exit if the day ends.

**Cashier NPC — FSM**
**Purpose:** Processes apprehension requests triggered from CCTV.
- **States:** 
    - IdleAtCounter → Waiting for request.

    - MovingToCustomer → Navigate to target.

    - Apprehending → Run apprehension logic (GameManager handler).

    - Returning → Go back to original counter position.
- **Transitions**
    - IdleAtCounter → MovingToCustomer (apprehension request queued)

    - MovingToCustomer → Apprehending (arrived at target)

    - Apprehending → Returning (resolution complete)

    - Returning → IdleAtCounter (reached counter; pull next request)
- **Implementation Details:**
    - Controlled by CashierBehaviour.cs. Waits at the counter until given a target via the CCTV system, moves to apprehend the suspect, and returns to the counter afterwards.

### 5.2 CCTV & Apprehension Pipeline
**Scripts:** CameraSystem.cs, CameraBehaviour.cs, PlayerBehaviour.cs, Monitor.cs
**Purpose:** Allows players to monitor the store through CCTV feeds, identify suspicious individuals, and request apprehension.

- **Key Features:**

    - Click on a monitor in first-person view to switch to its linked CCTV camera.

    - Switch between CCTV cameras using input keys (if enabled).

    - In CCTV mode, raycast from the cursor to highlight NPCs in view.

    - Click on a highlighted customer to send an apprehension request to the Cashier NPC.

    - Supports difficulty scaling by disabling certain cameras on later days.

### 5.3 Player Controller & Input Handling
**Script:** PlayerBehaviour.cs
**Purpose:** Manages player interactions and ensures input is responsive only when appropriate.

- **Key Features:**

    - Uses raycasting from the player camera to detect interactive objects (monitors, NPCs).

    - Prevents interactions when the game is paused via GameManager.IsPaused().

    - Handles camera return from CCTV to first-person view.

### 5.4 Environment Navigation Helpers
**Script:** ShelfSystem.cs
**Purpose:** Defines key navigation points for NPC behaviour.

- **Key Features:**

    - ShelfCheckpoint → NPC walking destinations.

    - ShelfLandmark → Look-at points for NPC browsing animations.

    - In-editor Gizmos make shelf layout and navigation testing easier.

### 5.5 Sliding Entrance Doors
**Script:** SlidingDoor.cs
**Purpose:** Provides realistic, automated entrance/exit doors for the store.

- **Key Features:**

    - Opens when a Customer-tagged object enters the detection radius.

    - Smooth open/close animation using position lerping.

    - Visual detection radius displayed in the Unity editor for easy adjustment.

### 5.6 Scene & UI Flow
**Script:** SplashScreenManager.cs, MainMenuManager.cs, SceneTransitionManager.cs
**Purpose:** Controls all game scenes, menus, and transitions.

- **Key Features:**

    - Splash Screen: Plays intro video with optional skip (Space key).

    - Main Menu: Start Game, Tutorial slideshow (advance with G), Quit; optional animated camera background.

    - Scene Transitions: Smooth fade effects with audio fade-out; supports level changes by index or name; holds fade for multi-day transitions.

### 5.7 Core Game Flow
**Script:** GameManager.cs
**Purpose:** Oversees game rules, NPC interactions, and progression.

- **Key Features:**

    - Tracks caught vs. escaped thieves per day.

    - Processes apprehension results from CCTV.

    - Manages pause menu and game state transitions.

    - Integrates day/wave progression with difficulty scaling.

### 5.8 Code Quality & Debugging Tools
**Purpose:** Maintain clarity and testability during development.

- **Key Features:**

    - All scripts contain file headers and XML documentation.

    - Gizmos in scripts like ShelfSystem and SlidingDoor assist in debugging navigation and triggers.

    - Console warnings for missing references or invalid configurations.

---

## 7. Puzzle Answer Key

The game does not contain traditional puzzles. The main challenge is observing behaviour and accurately identifying thieves under time pressure.

To succeed:

- 1. Monitor all customers via CCTV.

- 2. Count warning signs to confirm thieves.

- 3. Apprehend only confirmed thieves.

- 4. Avoid false accusations to maintain trust fund.

- 5. Meet daily performance thresholds to progress.

---

## 8.Limitations / Known Bugs

- SFX does not play in the build version
- Customers may occasionally get stuck despite avoidance logic.
- UI does not scale properly to certain aspect ratio.

---

## 9.Platforms / Hardware Requirements

- **Engine:** Unity

- **Platform:** Windows PC

## 10. Assets, References & Credits

### SFX
- Click: https://pixabay.com/sound-effects/click-21156
- Day Complete: https://pixabay.com/sound-effects/purchase-success-384963
- Camear Open and close: https://pixabay.com/sound-effects/canon-40d-camera-shutter-85733
- Caught Correct: https://pixabay.com/sound-effects/c-371145 
- Wrong: https://pixabay.com/sound-effects/wronganswer-37702

### Unity Assets Store
- First Person: https://assetstore.unity.com/packages/essentials/starter-assets-firstperson-updates-in-new-charactercontroller-pa-196525
- NPCs: https://assetstore.unity.com/packages/3d/characters/hyper-casual-human-characters-305473

### Tutorials
- Changing camera: https://www.youtube.com/watch?v=0t_3Yer6Mng&list=WL&index=8&t=2s

### Usage of AI
- Co-Pilot: Writing XML, Refinement of code


