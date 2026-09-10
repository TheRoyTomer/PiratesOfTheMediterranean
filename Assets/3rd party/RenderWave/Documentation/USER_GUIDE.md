# RenderWave User Guide

## Which Setup To Use
### Ocean
Use this when you need:
- open ocean
- large horizon coverage
- `Infinite` coverage
- far-field horizon continuity

### Lake
Use this when you need:
- a large contained water body
- `Rectangle` coverage
- calmer motion than open ocean

### Pool
Use this when you need:
- a small contained water body
- decorative or gameplay pool water
- distance culling so it does not keep rendering from unnecessary range

Do not use `Pool` or `Lake` as if they were open ocean.  
Do not keep using `Ocean` for a small pool just to avoid switching profile.

## Included Presets
### Classic / Retro
- stylized look
- simpler visual density
- strong readability

### Calm
- clean open-water look
- moderate motion
- strong default preset for demos

### Storm
- stronger contrast
- more aggressive waves
- good for dramatic scenes, not as a universal default

### Tropical
- brighter, clearer water
- better for sunny, shallow-feeling scenes

### Deep
- darker tone
- heavier mood
- better for serious open-water presentation

Practical rule:
- start with a preset
- adjust the scene second
- fine-tune water values last

## Optional Features
### Underwater
- `Lite` is the safe default path
- `Enhanced` is optional presentation polish

If the scene does not need to sell waterline crossing or underwater visuals, do not enable more than necessary.

### Wakes
- intended for boats and simple movers
- not splash simulation

### Contact Foam
- improves water-to-geometry readability
- not shoreline simulation

If foam looks wrong, the usual problem is:
- scene scale
- geometry not writing to depth
- lighting

### Far Field
- only for `Ocean` with `Infinite` coverage
- keeps water visible at long range with a cheaper mesh
- does not apply to `Lake` or `Pool`

If you try to use far-field to solve contained-water rendering, you are using the wrong tool.

## Rules Worth Respecting
- URP only
- one `OceanManager` per render-driving camera
- identity rotation and unit scale on the core rig
- `Opaque Texture` is only required for the intended underside/refraction ocean look
- `Pool` usually benefits from `Enable Distance Culling`
- `Lake` and `Pool` work best with `Rectangle` coverage

## Recommended Workflow
1. Create the correct rig: `Ocean`, `Lake`, or `Pool`.
2. Apply a preset.
3. Verify camera and material assignment.
4. Add underwater, wakes, or buoyancy only if the scene actually needs them.
5. Tune performance after you have seen the scene, not before.

## What Not To Expect
- no real `River` workflow
- no shoreline simulation
- no realistic boat physics
- no HDRP or Built-in support

If the project needs those systems, RenderWave should not be the asset expected to solve them.
