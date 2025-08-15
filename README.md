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







