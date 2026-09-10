# RenderWave Buoyancy Lite

## What It Is
Buoyancy Lite is an optional helper that makes a `Rigidbody` float on the RenderWave water surface. It applies vertical buoyancy forces, vertical damping, and optional surface-alignment torque so that props, buoys, rafts, and demo boats bob and tilt believably on the ocean.

It is a presentation and gameplay-helper layer, not a marine physics package.

## What It Is For
- floating props that should rise and fall with waves
- demo boats that need to sit on the water without custom buoyancy code
- lightweight pitch and roll reaction for small and medium craft
- making sample scenes and gameplay prototypes feel more complete

## What It Is NOT For
- realistic hull displacement
- boat propulsion or steering
- volumetric buoyancy
- shoreline grounding or beaching behavior
- splash simulation or wave slamming
- multiplayer-synchronized water interaction
- replacing a dedicated boat-physics solution

If a project needs real marine simulation, RenderWave should remain the water-query source and buoyancy should come from a dedicated physics package.

## Sampling Modes
### Single Point
Safe default.

Use this when:
- the object is small
- the object is visually simple
- you want the cheapest possible floating behavior
- the object does not need clearly different bow and stern response

Cost:
- 1 water query per physics tick

### Four Point
Optional upgrade for medium boats, rafts, and longer props.

Use this when:
- one sample makes the craft look too rigid or too "center-pivoted"
- you need better pitch and roll stability
- you want more believable behavior on longer hulls without moving into full hull simulation

Cost:
- 4 water queries per physics tick

Important:
- this is still Buoyancy Lite
- it is not hull simulation
- it does not add propulsion, water drag, or wave impacts

## Quick Start
### Floating Cube In 30 Seconds
1. Have a working RenderWave ocean in the scene.
2. Create `GameObject > 3D Object > Cube`.
3. Position it above the water.
4. Add `RenderWave > Buoyancy Body`. A `Rigidbody` is auto-added if missing.
5. Set `Rigidbody > Interpolation` to `Interpolate`.
6. Press Play.

### Demo Boat Setup
1. Place the `SM_Boat` prefab in a scene with a RenderWave ocean and a `WakeSystem`.
2. On the boat root, add:
   - `Rigidbody` with Mass `10`, Drag `1.5`, Angular Drag `3`, Interpolation `Interpolate`
   - `BuoyancyBody`
   - `DemoBoatAutopilot`
3. Leave `Sample Mode` on `Single Point` for a first pass.
4. If the hull looks too rigid in pitch or roll, switch to `Four Point`.
5. Press Play.

## Recommended Usage
### Small Props
- `Sample Mode`: `Single Point`
- `Buoyancy Force`: `12-20`
- `Vertical Damping`: `1.5-3`
- `Alignment Strength`: `3-5`
- `Surface Offset`: `0` to `-0.3`

### Medium Boats And Rafts
- `Sample Mode`: `Four Point`
- `Buoyancy Force`: `10-18`
- `Vertical Damping`: `2-4`
- `Alignment Strength`: `1-2.5`
- `Surface Offset`: `0.1-0.3`
- `Sample Footprint Half Extents`: start around `X 0.6`, `Z 1.2` and scale to hull size

### Buoys And Markers
- `Sample Mode`: `Single Point`
- `Buoyancy Force`: `20-30`
- `Vertical Damping`: `1-2`
- `Alignment Strength`: `4-6`
- `Surface Offset`: `0.5-1.0`

### Floating Platforms
- `Sample Mode`: `Four Point`
- `Buoyancy Force`: `15-25`
- `Vertical Damping`: `4-6`
- `Alignment Strength`: `0.5-1.5`
- `Surface Offset`: `0.2-0.5`

## Parameters
### Core
| Parameter | Default | Purpose |
|-----------|---------|---------|
| Sample Mode | Single Point | Chooses between the cheapest single-sample behavior and optional four-point sampling. |
| Buoyancy Force | 15 | Upward force per meter of submersion. Higher values return the object to the surface faster. |
| Vertical Damping | 2 | Opposes vertical velocity and helps the object settle. |
| Surface Offset | 0 | Target float height relative to the water surface. Positive floats higher, negative sits deeper. |

### Surface Alignment
| Parameter | Default | Purpose |
|-----------|---------|---------|
| Alignment Strength | 2 | Torque strength for tilting the object's up axis toward the water normal. |
| Alignment Damping | 1.5 | Damping torque that reduces rotational oscillation. |

### Advanced
| Parameter | Default | Purpose |
|-----------|---------|---------|
| Sample Point Offset | `(0,0,0)` | Local-space center of the buoyancy query or four-point footprint. |
| Sample Footprint Half Extents | `(0.6,1.2)` | Half-size of the four-point footprint in local X/Z. Only used in `Four Point`. |
| Submersion Depth Clamp | `3` | Caps how much submersion contributes to buoyancy. Prevents deep spawns from exploding upward. |
| Force Ramp Duration | `0.25` | Time in seconds over which buoyancy ramps from zero to full after activation. |

## Runtime Readout
In Play Mode, `BuoyancyBody` shows:
- `Has Water`
- `Active Sample Count`
- `Surface Height`
- `Signed Distance`

`Active Sample Count` is important in `Four Point` mode:
- `4` means every corner found valid water
- `1-3` means only some points are currently over a valid zone
- `0` means no sample found water and buoyancy is not being applied

If a long object is crossing the zone edge, partial sample counts are expected.

## Known Limitations
### Still Not Hull Simulation
Four Point improves pitch and roll stability, but it still does not model hull volume, water displacement, or per-triangle buoyancy. It is a practical approximation.

### No Lateral Water Drag
`BuoyancyBody` does not add horizontal drag. Sideways slowing still comes from `Rigidbody.drag` or user movement code.

### No Wave Slamming
Objects do not receive impact forces from wave hits. Waves affect the object only through buoyancy and optional surface alignment torque.

### Mass-Independent Forces
Forces use `ForceMode.Acceleration`. That keeps tuning simpler, but it also means mass does not directly change float response. Heavier-looking objects should be tuned by settings, not by assuming mass alone will solve it.

### Alignment Damping Affects All Rotation
Alignment damping opposes all angular velocity, including yaw from user scripts like `DemoBoatAutopilot`. At reasonable values this is fine. At aggressive values it can make steering feel sluggish.

### Query Cost Scales With Sample Mode
- `Single Point`: 1 query per physics tick
- `Four Point`: 4 queries per physics tick

That is still cheap at modest object counts, but it is not free. `Four Point` should be used where it adds visible value, not on every floating prop by default.

### No Sleeping Optimization
Buoyancy runs every `FixedUpdate` while the component is active. This is acceptable for V1/V2 scope, but very large numbers of floating objects should still be treated as a production-budget question.

## How It Uses WaterLevelQueryService
`BuoyancyBody` depends only on `WaterLevelQueryService`.

`Single Point` mode:
- queries once at the configured sample point

`Four Point` mode:
- queries at four local-space corners around the sample-point center
- applies force at each valid corner
- averages successful water results for inspector state and surface alignment

This keeps buoyancy decoupled from `OceanManager`, `OceanWaterZone`, and internal ocean rendering state.

## Common Mistakes
- **Using Four Point on every prop.** That is lazy tuning. Use it only where one sample looks visibly wrong.
- **Setting the footprint larger than the hull.** That makes the object react to water that is not actually under it.
- **Trying to solve bad steering with buoyancy.** `BuoyancyBody` is not a boat controller.
- **Turning alignment too high in Four Point mode.** If the object over-tilts, reduce `Alignment Strength` before inventing more systems.
- **Driving the transform directly.** Use physics forces or torque. Direct transform writes fight the `Rigidbody`.
- **Expecting shoreline or grounding behavior.** Buoyancy Lite has no notion of beaching, hull collision with the seabed, or coastal friction.

## Troubleshooting
### Object Does Not Float
Check:
- `Rigidbody` is not kinematic
- `Use Gravity` is enabled
- the object is inside a RenderWave water zone
- `Has Water` becomes true in Play Mode

### Boat Looks Too Rigid
Likely cause:
- `Single Point` is too crude for the hull size

What to do:
- switch to `Four Point`
- set a modest footprint
- lower `Alignment Strength` if the result becomes too aggressive

### Boat Wobbles Too Much
Likely causes:
- `Alignment Strength` too high
- `Alignment Damping` too low
- footprint too large for the hull

What to do:
- reduce `Alignment Strength`
- increase `Alignment Damping`
- shrink the four-point footprint until it matches the hull better

### Only Some Samples Are Active
Likely causes:
- the craft is crossing the edge of a water zone
- the footprint is too wide

What to do:
- check `Active Sample Count`
- reduce `Sample Footprint Half Extents`
- confirm the object is not partly outside the water coverage area

### Wake Stops Working After Adding BuoyancyBody
That is usually not a buoyancy problem.

Check:
- `WakeSystem` exists in the scene
- `WakeEmitter` is present on the craft
- the craft is moving fast enough to exceed wake thresholds

`BuoyancyBody` and `WakeEmitter` are independent systems.
