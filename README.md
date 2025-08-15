# Combinight Shift – Delta Challenge 2025
---
## 1.File Structure
**Folder Organization:**

- *Audio* - Sound effects (SFX) and background music used in the game.
- *Fonts* - Fonts used for the user interface (UI)
- *Hyper Casual Characters* - NPC character models, including cashier, thief, and customer assets.
- *Materials* - All material files used for models, including the texture atlas and trim sheet.
- *Models* - 3D models exported from Maya. 
- *Prefabs* - Fully unpacked models with materials applied, saved as reusable prefabs.
- *Scenes* - All game scenes (e.g., Main Menu, Gameplay Scene).
- *Scripts* - All gameplay and system scripts for the project.
- *Starter Assets* - First-person controller and related assets.
- *Textures* - Image files used for materials and in-game assets (e.g., newspaper images).
- *VFX* - Visual effects used in the game (e.g., Fridge Mist).
- *Videos* - Splash Screen at the start of the game.

---

## 2. Overview

A first-person convenience store simulation where players act as a night-shift cashier to monitor customers via a CCTV system, identify suspicious activity, and apprehend shoplifters before they escape. The game integrates NPC AI, Unity’s Navigation System, raycasting-based interactions, and a day/wave progression system.

*Core Features:*
- NPCs with state-based AI (customers, Cashier & thieves) using FSM-like behavior.
- Unity NavMesh for pathfinding and dynamic movement.
- Raycasting for monitor/Thief interaction and object highlighting.
- Cashier NPC for in-store apprehension
- Day & wave-based progression with increasing difficulty.
- Splash screen, main menu, and smooth scene transitions.

---

## 3. Controls

### Normal Gameplay
*Movement* - W: Forward / S: Backward, / A: Go left / D: Go right - Player Movement
*Look Around* - Mouse - First-person camera control
*Interact with Monitor* - Left Mouse Click - Switch to CCTV camera view
*Return to Main Camera* - F - Exit CCTV view
*Apprehend Customers* - Y - Confirm apprehension
*Cancel Apprehension* - N - Dismiss apprehension popup
*Pause Menu* - Esc - Pause, Exit or Resume game

### Hacks
*Skip Day* - J - Move on to the next day
*Speed Up* - K - Speed up the current wave




