# RenderWave Troubleshooting

## No Water Appears
### Likely Causes
- target camera is not assigned
- URP is not active
- ocean material or shader assignment is missing or invalid

### What To Check
- `OceanManager > Target Camera`
- project render pipeline asset
- `OceanManager` preset buttons or source material

## Water Appears But Looks Wrong
### Likely Causes
- non-RenderWave material assigned
- wrong preset for the intended look
- overly aggressive custom wave values

### What To Check
- try `Apply Classic Ocean` or `Apply Enhanced Ocean`
- confirm the assigned material matches the RenderWave ocean shader contract

## Far Ocean Looks Empty
### Likely Causes
- far-field is disabled
- the rig is not using `Infinite` ocean coverage
- the water body is `Lake` or `Pool`, where far-field is intentionally ignored

### What To Check
- `OceanManager > Far Field > Enable Far Field`
- `OceanWaterZone > Coverage Mode = Infinite`
- `Zone Type = Ocean`

## Contact Foam Is Not Visible
### Likely Causes
- contact foam is disabled on the ocean material
- intersecting geometry does not write to depth
- foam settings are too subtle for the scene scale

### What To Check
- ocean material `Enable Contact Foam`
- start with packaged preset values before widening the band
- `Foam Distance`, `Foam Intensity`, and `Foam Edge Sensitivity`
- whether the intersecting mesh is opaque and writes to depth

## Contact Foam Appears Everywhere In Shallow Water
### Likely Causes
- foam distance is too large
- edge sensitivity is too low
- the seabed is broadly shallow and the material is tuned too aggressively

### What To Check
- lower `Foam Distance` aggressively before touching intensity
- lower `Foam Softness`
- raise `Foam Edge Sensitivity`
- reduce `Foam Intensity`

## Rectangle Coverage Does Not Behave As Expected
### Likely Causes
- rectangle mode is active but the zone transform is rotated or scaled
- the user expects local-space rotation, but V1 rectangle coverage is axis-aligned in world space

### What To Check
- keep the zone transform at identity rotation and unit scale
- resize the rectangle directly in Scene view using the zone editor

## Pool Appears Only When The Camera Gets Closer
### Likely Causes
- the pool rectangle is larger than the current total chunk footprint
- the rig is still using camera-centered chunk placement because the contained-water area does not fit in the available chunk footprint

### What To Check
- compare the rectangle size against `Chunk Count Per Axis * Chunk Size`
- for small contained water, keep the full rectangle inside the total chunk footprint
- if needed, raise `Chunk Radius` or `Chunk Size` deliberately instead of assuming the current preset is enough

## Pool Keeps Rendering From Too Far Away
### Likely Causes
- `Enable Distance Culling` is disabled
- `Max Render Distance` is higher than the scene actually needs

### What To Check
- on `OceanManager > Chunk Settings`, enable `Enable Distance Culling`
- reduce `Max Render Distance` until the pool disappears at the intended gameplay distance
- remember this affects rendering only; water queries still remain available

## Underwater Does Not Activate
### Likely Causes
- underwater controller camera does not match `OceanManager` camera
- the camera is not actually in or near valid water
- underwater effects are disabled on the zone

### What To Check
- `UnderwaterStateController > Target Camera`
- `OceanManager > Target Camera`
- `OceanWaterZone` underwater effects flag

## Enhanced Underwater Looks Different Than Expected
### Likely Causes
- enhanced mode is enabled when Lite was intended

### What To Check
- disable `Enable Enhanced Mode` to return to the Lite path

## Ocean Underside Or Refraction Looks Flat
### Likely Causes
- URP `Opaque Texture` is disabled
- the material is using underside/refraction settings that expect the opaque scene texture

### What To Check
- enable `Opaque Texture` in the active URP renderer or pipeline asset
- if you do not want to rely on `Opaque Texture`, reduce underside refraction-driven settings in the ocean material

## Wake Is Not Visible
### Likely Causes
- no `WakeSystem` exists
- the mover does not have a `WakeEmitter`
- the emitter is not traveling over valid water
- the emitter transform is too far above or below the water surface
- wake material or shader setup is missing

### What To Check
- add a wake system using the `OceanManager` inspector
- confirm the mover has `WakeEmitter`
- confirm movement occurs over RenderWave water
- keep the `WakeEmitter` near the actual waterline contact area
- if needed, raise `WakeEmitter > Settings > Surface Contact Tolerance` slightly instead of leaving the emitter clearly above or below the surface
- confirm the wake system is using the packaged wake material or wake shader

## Wake Looks Choppy Or Disconnected
### Likely Causes
- source motion is highly discontinuous
- Rigidbody movement is not visually smooth

### What To Check
- test with smoother motion
- if using Rigidbody movement, confirm interpolation is enabled when appropriate

## Floating Object Does Not Float
### Likely Causes
- Rigidbody is kinematic
- Rigidbody has gravity disabled
- object is not over a RenderWave zone
- `BuoyancyBody` is disabled

### What To Check
- Rigidbody `Is Kinematic` off, `Use Gravity` on
- confirm the object is inside a RenderWave zone
- in play mode, check `BuoyancyBody` runtime state

## Performance Is Lower Than Expected
### Likely Causes
- chunk radius too high
- mesh resolution too high
- enhanced underwater mode enabled unnecessarily
- wake capacity pushed too far
- too many contained-water rigs visible at once
- `Four Point` buoyancy used everywhere without need

### What To Check
- lower chunk radius first
- lower mesh resolution second
- use Lite underwater unless the enhanced presentation is needed
- keep wake capacity practical
- keep `Pool` distance culling enabled unless long-range visibility is intentional
- use `Single Point` buoyancy unless `Four Point` adds visible value
