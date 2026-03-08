# LeapCraftVR — VR Pottery Training Simulator 

**LeapCraftVR** is a Unity **PCVR** pottery simulation designed for **training-oriented clay modelling** using **real-time 3D mesh deformation** and **hand-tracking technology**. The project uses the Leap Motion Controller mounted for VR use and was tested with **Meta Quest 3**.

## Demo
- Pottery game demo video: **<https://youtu.be/Y8tuj2MCTgI>**

## Highlights
- **Real-time clay deformation** inspired by real pottery techniques:
  - **Chain-based pull-up** deformation (raise the clay while thinning the affected area)
  - **Push-down / opening** deformation (flattening and creating openings)
  - **Thinning** deformation for vase-like shapes (stronger inner-zone shaping + smoother transition)
  - **Pressure-based indentation** (force-dependent indent area + falloff)
- **Mass/volume preservation + refinement**
  - Volume is recomputed and preserved across deformation steps, with synchronization between modes.
  - Smoothing refinements (e.g., Laplacian smoothing) are used to reduce artifacts.
- **Training UI and usability**
  - Floating menus for starting/resetting/exiting and clay selection.
  - Hover-based guidance: placing the palm near controls shows contextual instructions.
  - Tutorial video buttons (e.g., vase/cup/bowl shaping guidance).
  - Button press animation + sound feedback.
- **Mini-game for tracking comparison**
  - A secondary scene designed to collect performance parameters (latency, accuracy-like parameters) for comparing hand-tracking setups (Leap vs Meta).

## Hardware / Platform
- **PCVR** project (Windows recommended for full setup)
- Tested with:
  - **Meta Quest 3**
  - **Leap Motion Controller**

> The project can be opened in Unity without VR hardware, but hand-tracked gameplay requires the devices above.

## Unity Version
- Developed and tested with **Unity 6 (6000.1.4f1)**  
- Unity **6.x** recommended.

## How to Run
1. Install **Unity Hub** and Unity **6 (6000.1.x)**.
2. Clone the repository:
   ```bash
   git clone <REPO_URL>
