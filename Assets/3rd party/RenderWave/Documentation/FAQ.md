# RenderWave FAQ

## Which Documentation Actually Matters
- `QUICKSTART.md` to get running
- `USER_GUIDE.md` to choose rig, preset, and optional features
- `TROUBLESHOOTING.md` if something fails
- `BUOYANCY_LITE.md` only if you plan to use buoyancy

## Is RenderWave physically accurate?
No. RenderWave is intentionally practical and performance-oriented. It is designed for scalable game-ready oceans, not simulation accuracy.

## Which render pipeline does V1 support?
URP only.

## Does RenderWave support multiple render-driving cameras?
Not in V1. One `OceanManager` supports one explicit render-driving camera.

## Does RenderWave include underwater support?
Yes. It includes a safe Lite underwater path plus optional enhanced underwater presentation.

## Is enhanced underwater required?
No. Lite remains the safe default. Enhanced underwater is optional presentation polish on top of the same core underwater state system.

## Do I need URP Opaque Texture enabled?
Only if you want the intended underside/refraction presentation from below the surface. The core ocean system, underwater overlay, wakes, and buoyancy do not require that texture to function.

## Does RenderWave include wakes?
Yes. The wake system is lightweight, pooled, and intended for boats or similar movers.

## Why is my wake not emitting even though the object is moving?
Wake emission is treated as a surface-contact effect. The `WakeEmitter` should stay near the actual waterline contact area, not clearly above it or deep below it.

## Does RenderWave include buoyancy?
It includes Buoyancy Lite, which is a lightweight helper for floating rigidbodies. `Single Point` is the safe default. `Four Point` is available for better pitch and roll behavior on boats and rafts, but it is still not a full boat-physics or realistic hull simulation package.

## Does RenderWave include ready-made presets?
Yes. It includes the core Classic and Enhanced presets plus packaged commercial presets for Calm, Storm, Tropical, and Deep ocean looks.

## Does RenderWave include shoreline foam?
No. V1 includes optional contact/intersection foam only. That improves water-to-geometry readability, but it is not a shoreline simulation system. It is a restrained depth-based contact effect, not a coastal wave solver.

## Can I use RenderWave for lakes or pools?
Yes, in a limited and honest way. V2.5 adds contained water profiles for `Lake` and `Pool` on the same chunked surface/query architecture. They are intended for contained water bodies, not as separate simulation systems.

## Does RenderWave support rivers?
No. RenderWave V2.5 supports contained Lake and Pool profiles, but it does not ship river tooling or a supported river workflow.

## Do I need to use all features?
No. Wakes, buoyancy, contact foam, caustics, and enhanced underwater are all optional. The core ocean system works without them.

## What are the main limitations I should care about?
The practical ones are:
- URP only
- one render-driving camera per `OceanManager`
- no river workflow
- no shoreline simulation
- no realistic boat physics
- `Lake` and `Pool` are contained profiles, not separate solvers
- far-field horizon coverage is for infinite ocean only, not for contained water
