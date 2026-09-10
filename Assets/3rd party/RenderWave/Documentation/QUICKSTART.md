# RenderWave Quick Start

## Goal
Get water working in under two minutes.

## Requirements
- Unity project using URP
- RenderWave imported

## If You Only Read Two Files
1. This file
2. [USER_GUIDE.md](./USER_GUIDE.md)

Use [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) only when something fails.

## Fastest Path
1. Open Unity.
2. Use `GameObject > RenderWave > Enhanced Ocean Rig`.
3. Select the created object.
4. In `OceanManager`, confirm:
   - `Target Camera`
   - `Ocean Zone`
   - assigned material or shader
5. Press Play.

## Fast Variants
### Want another look without hand-tuning values?
Use:
- `Classic / Retro`
- `Calm`
- `Storm`
- `Tropical`
- `Deep`

### Want contained water?
Use:
- `GameObject > RenderWave > Contained Water > Lake Rig`
- `GameObject > RenderWave > Contained Water > Pool Rig`

### Want underwater?
Click `Add Underwater Controller`.

### Want wakes?
Click `Add Wake System`, then add `WakeEmitter` to the moving object.

### Want floating props or boats?
Add `BuoyancyBody` to a `Rigidbody`.  
If you actually plan to use it, read [BUOYANCY_LITE.md](./BUOYANCY_LITE.md).

## Basic Rules
- do not leave the camera unassigned
- do not assume HDRP or Built-in support exists
- do not put the rig under rotated or scaled transforms
- enable URP `Opaque Texture` only if you want the underside/refraction ocean look
