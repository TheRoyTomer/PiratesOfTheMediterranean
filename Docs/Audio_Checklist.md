# Pirates of the Mediterranean — Audio Checklist

Companion document to the [GDD](GDD.md), based on the agreed sound and music requirements.

Status updated on 2026-10-02. `[x]` means implemented and connected to its gameplay event; `[ ]` means not implemented. Downloading or assigning a clip alone does not count as implementation. Checked items may still need listening checks and adjustments to volume, timing, distance, or loop seams. Omitted items are outside the current scope and have no checkbox.

## Gameplay listening review — 2026-10-02

Numbers match the 17-item gameplay checklist supplied to the user.

**FINAL ACCEPTANCE — 2026-10-02:** The user approved the final faded impact and confirmed that all sound checks are complete. All 17 gameplay audio items, menu sounds, and main-screen music are approved. Earlier pending statuses in the review history below are superseded by this acceptance. Optional unimplemented sailing hull creaks are not part of these completed checks.

### Current impact selection — 2026-10-02

The user rejected both replacement candidates and requested removal of the comparison tool. `ImpactAudioComparison.cs` and its meta were deleted and its window closed. The user shortened the original impact themselves and supplied `Assets/Audio/Downloaded Audio/Cannon_Impact_New.wav` (approximately 2.006 seconds). This clip is now saved as the CannonImpact sound in GameScene, at the existing volume 0.4 and attenuation distances 60–800. The original is retained under `Assets/Audio/Downloaded Audio/Source/Cannon_Impact.ogg`. At the user's request, a linear fade-out was added over the final 0.300 seconds on 2026-10-02. The original user edit was backed up as `Source/Cannon_Impact_New_BeforeFade.wav`; all samples before the fade remain byte-identical, the duration is unchanged and both channels end at zero. The user approved the faded version on 2026-10-02. Original cannon firing remains restored. Import/decoding and scene assignment were checked; final gameplay listening approval is complete. Earlier feedback below is historical and superseded by this selection.

### Standalone follow-up — 2026-10-02

**Latest correction:** The user clarified that **4 (cannon firing) was already satisfactory** and the unresolved sound is **5 (cannonball impact on ships/land)**. Restored the original `Cannon_Fire.ogg` in GameScene. Do not shorten or otherwise change item 5: the user wants to choose a replacement together. `Cannon_Impact.ogg` and its current settings remain unchanged. Only item 5 is open; all other gameplay sounds are approved. The WindowsAudioReview build predates this restoration; its shortened firing clip does not reflect the current project. The earlier numbered feedback below is historical and superseded by this correction.

The user tested `Builds/WindowsAudioReview/PiratesOfTheMediterranean.exe` and confirmed that everything was resolved except item **4**. Items **3, 6, 12, 13 and 14** are now approved, alongside the previously approved items below. Only item **4** remains open. The user explicitly requested no further sound changes yet and is considering selecting a different cannonball-impact sound. The original numbered list called item 4 cannon firing and item 5 ship/land impact; the feedback under item 4 covers both firing and impact audibility, so confirm the replacement event before integrating a new clip. Current firing and impact clips/settings remain unchanged. The entries below preserve the initial review and tuning history; this follow-up supersedes their pending-listening statuses except item 4.

- Approved by the user: **1** ocean, **2** bow water, **5** ship/land impact sound, **7** collisions, **8** barrel water entry, **9** barrel explosions, **10** death explosions, **11** capsize creaks, **15** repair, **16** direction switching, **17** unavailable actions.
- **3 — Sails:** not heard; low priority. Increased scene volume from 0.08 to 0.12. Awaiting another listening check.
- **4 — Cannon fire and impact audibility:** user requested a shorter firing sound without its late tail, especially under overlapping combat. Created `Cannon_Fire_Short.wav` from the original: 0.70 seconds, with a fade from 0.50 to 0.70 seconds. Original retained. Ship/land impact attenuation distances increased from 30–400 to 60–800 world units; the approved impact clip and volume are unchanged. Awaiting listening checks for both changes.
- **6 — Cannonball water splash:** not heard. Clip and gameplay call are connected; the previous 400-unit cutoff could exclude distant splashes. Increased attenuation distances from 30–400 to 60–800. Cause not conclusively reproduced; awaiting gameplay listening.
- **12 — Capsize water impact:** not heard. Both ship variants have the splash prefab and all eight water-impact markers. Increased scene volume from 0.45 to 0.60 and attenuation distances from 30–400 to 60–650. Existing event intensity remains 0.65. Cause not conclusively reproduced; awaiting gameplay listening.
- **13/14 — Both pickups:** user requested slightly louder feedback. Increased shared pickup volume from 0.50 to 0.65. Awaiting listening confirmation.

Saved scene references, clip duration, PCM fade endpoint, and prefab marker bindings were checked in the Editor. No new Play Mode or audible gameplay validation was performed for these adjustments. Menu sounds and main-screen music were separately approved by the user on 2026-10-02.

## 1. Ambience and Sailing

- [x] **Continuous ocean ambience:** A seamless loop of waves and sea ambience throughout gameplay, balanced to leave room for combat sounds.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Ambience_Ocean.flac`.
  - Selected by the user on 2026-09-27: [Calm ocean waves by SamsterBirdies](https://freesound.org/people/SamsterBirdies/sounds/578524/). Source license: CC0.
- **Wind while moving — Omitted:** The user decided on 2026-09-27 that a separate gentle wind layer is not needed.
- [x] **Bow water movement:** Water flowing and breaking around the bow while moving, growing louder with speed and fading as the ship slows.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Sailing_BowWater.mp3`.
  - Selected by the user on 2026-09-27: [Sailing boat moving, water, onboard recording](https://www.zapsplat.com/music/sailing-boat-moving-water-onboard-recording/) on ZapSplat. Source license: CC0.
- [ ] **Wood creaks during sailing — Optional:** Subtle, intermittent hull creaks with enough variation to avoid obvious repetition.
  - Decision pending: intended to suggest the wooden hull flexing during sailing. The user asked why this layer is needed; no sound has been selected. This is separate from capsizing creaks in the destruction sequence.
- [x] **Sail rustling:** Gentle fabric and sail movement sounds as an atmospheric layer around the ship.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Sailing_SailRustle.wav`.
  - Selected by the user on 2026-09-27: [flag.wav by GeorgeHopkins](https://freesound.org/people/GeorgeHopkins/sounds/448975/). Source license: CC0.

## 2. Cannons and Impacts

- [x] **Cannon fire:** Sound when player or enemy cannons actually fire. Balance simultaneous shots so a broadside does not produce excessive volume.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Cannon_Fire.ogg`. Original restored on 2026-10-02 after the user clarified that firing was already satisfactory. The unused `Cannon_Fire_Short.wav` is retained as an earlier edit, not assigned to gameplay.
  - Selected by the user on 2026-09-27: `cannon_fire.ogg` from [Battle at Sea by Thimras](https://opengameart.org/content/battle-at-sea). Source license: CC0.
- [x] **Cannonball impact on ships or land:** Use the same sound type for both impact categories, played at the impact position.
  - Implemented on 2026-09-28; user-edited replacement selected on 2026-10-02: `Assets/Audio/Downloaded Audio/Cannon_Impact_New.wav`. Original retained in `Source/Cannon_Impact.ogg`.
  - Selected by the user on 2026-09-27: `cannon_hit.ogg` from [Battle at Sea by Thimras](https://opengameart.org/content/battle-at-sea). Source license: CC0.
- [x] **Cannonball impact on water:** A splash at the point where the projectile enters the water, distinct from ship or land impacts.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Cannon_WaterSplash.ogg`.
  - Selected by the user on 2026-09-27: `cannon_miss.ogg` from [Battle at Sea by Thimras](https://opengameart.org/content/battle-at-sea). Source license: CC0.
- **Incoming or passing cannonball whistle — Omitted:** Removed at the user's request on 2026-09-28 after gameplay audition and timing adjustments. The edited clip and original recording remain in the project for reference; projectiles no longer trigger a whistle.
  - Selected source by the user on 2026-09-27: [R12-28-Cannon Shot and Whistling.wav by craigsmith](https://freesound.org/people/craigsmith/sounds/486026/). Source license: CC0. Extract only the shell-whistle section, excluding the cannon shot; the user confirmed that this recording contains the desired whistle. Keep the whistle high-pitched and subtle.
  - Earlier user feedback on 2026-09-27: rejected [Incoming mortar 1 by Zagge28](https://freesound.org/people/Zagge28/sounds/241840/) in favor of a higher-pitched, quieter, more delicate whistle.
  - Earlier user feedback on 2026-09-27: rejected [Whoosh - Weighted string - Airy whistle by Sadiquecat](https://freesound.org/people/Sadiquecat/sounds/855733/) and [whoosh_long_high.wav by DJT4NN3R](https://freesound.org/people/DJT4NN3R/sounds/449991/). The sound must be a recognizable artillery-shell whistle, not a generic air whoosh.

## 3. Collisions

- [x] **Collision impact:** A shared impact sound for ship-to-ship and ship-to-land collisions. Scale intensity with collision strength; sustained contact must not trigger a new impact every frame.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Ship_Collision.wav`.
  - Selected by the user on 2026-09-27: `ship_ram_ship_shortened.ogg` from [Battle at Sea by Thimras](https://opengameart.org/content/battle-at-sea). Source license: CC0. Shorten the tail so the sound does not continue too long after the collision.

## 4. Barrels Deployed as Weapons

These items apply to barrels deployed into the sea by ships as weapons, separately from collectible Barrel Sets.

- [x] **Barrels entering the water:** Splash sounds when deployed barrels reach the water. Balance overlapping splashes when several barrels land close together.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Barrel_WaterSplash.wav`.
  - Selected by the user on 2026-09-27: [Large Splash by roboroo](https://freesound.org/people/roboroo/sounds/436792/). Source license: CC0.
- [x] **Barrel explosion:** An explosion sound suited to barrels detonating on water. Since four barrels explode together, balance the group event rather than stacking four full-volume sounds.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Explosion_Heavy.wav`.
  - Selected by the user on 2026-09-27: [Huge Explosion by unfa](https://freesound.org/people/unfa/sounds/259300/). Source license: CC0. Shared with ship-destruction explosions. The user approved its large, long, powerful character; optionally trim the late tail with a smooth fade rather than an abrupt cutoff.
  - Selection requirement confirmed by the user on 2026-09-27: must sound substantially bigger and more powerful than a regular cannonball impact. Look for a heavier explosive character, not merely a louder playback of the impact sound.

## 5. Ship Destruction

Earlier explosion audition feedback (2026-09-27), applying to barrel and ship-destruction explosions: [Explosions by EZduzziteh](https://opengameart.org/content/explosions-4) was rejected as too short and abruptly cut off. In [2 High Quality Explosions by Michel Baradari](https://opengameart.org/content/2-high-quality-explosions), only the regular explosion was considered a possible fallback; the mini version was unsuitable. The regular version sounded too weak relative to the selected cannonball impact. Neither pack was selected; Huge Explosion by unfa was subsequently selected for both uses.

- [x] **Heavy death explosions:** A powerful explosion at each of the three destruction points — bow, middle, and stern — synchronized with the visual sequence and its order. Variations of one sound type can be reused.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Explosion_Heavy.wav`.
  - Selected by the user on 2026-09-27: [Huge Explosion by unfa](https://freesound.org/people/unfa/sounds/259300/). Source license: CC0. Use the shared barrel-explosion asset for each individual destruction blast, synchronized with the existing three-point sequence. Optionally trim the late tail with a smooth fade while preserving its weight and natural decay.
  - Selection requirement confirmed by the user on 2026-09-27: must sound substantially bigger and more powerful than a regular cannonball impact. Select individual heavy blasts that can follow the three-point sequence; their tails must allow the successive blasts to remain distinct.
- [x] **Capsizing creaks:** Hull strain and creaking during the roll and capsize, synchronized with the movement and ending when that phase finishes.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Ship_CapsizeCreak.wav`.
  - Selected by the user on 2026-09-27: [Wood Creaking.wav by laft2k](https://freesound.org/people/laft2k/sounds/397620/). Source license: CC0. Extract suitable sections for the roll and capsize rather than playing the entire recording.
- [x] **Heavy water impact:** A strong impact sound when the hull or masts hit the water during capsizing, synchronized with the existing splash events.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Ship_CapsizeSplash.mp3`.
  - User-approved candidate on 2026-09-27: [Water Splosh by benj500](https://freesound.org/people/benj500/sounds/545823/). Source license: CC0. The user considers it suitable as a water-impact sound; confirm its weight and timing against the capsize visuals in gameplay.

## 6. Pickups and Repair

- [x] **Collect Ship Parts:** Brief audio confirmation when a repair part is successfully collected and added to inventory.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Pickup_Collect.ogg`.
  - Selected by the user on 2026-09-27: [Item Pickup sound by Strechy](https://freesound.org/people/Strechy/sounds/654251/). Source license: CC BY 4.0; credit Strechy, link the source and license, and identify any modifications. Shared with Barrel Set collection.
- [x] **Collect a Barrel Set:** Brief audio confirmation when a barrel set is successfully collected and added to inventory.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Pickup_Collect.ogg`.
  - Selected by the user on 2026-09-27: [Item Pickup sound by Strechy](https://freesound.org/people/Strechy/sounds/654251/), shared with Ship Parts collection. Source license: CC BY 4.0; attribution required as above.
- [x] **Ship repair:** A short woodwork or repair sound, played only when repair succeeds, health is restored, and a Ship Part is consumed.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/Repair_Hammer.wav`.
  - Selected by the user on 2026-09-27: [hammer on wood - Martelo em madeira by rodrigocswm](https://freesound.org/people/rodrigocswm/sounds/449427/). Source license: CC0. Extract a short sequence of hammer knocks from the recording for successful repair.

**Current selection:** One shared sound is connected to both pickup types. Each collection event still needs its own listening check.

Pickups require sound only when collected. No emergence, floating, or sinking sounds are required for pickups.

## 7. User Interface

- [x] **Switch firing direction:** Brief feedback when cycling between Front, Right, and Left.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/UI_SwitchFireDirection.wav`.
  - Selected by the user on 2026-09-27: [Change Weapon Sound by knova](https://freesound.org/people/knova/sounds/170273/). Source license: CC BY 4.0; credit knova, link the source and license, and identify any modifications.
  - Earlier audition feedback on 2026-09-27: the user could not hear [Wooden Click by BenjaminNelan](https://freesound.org/people/BenjaminNelan/sounds/321083/). Not selected. The cause of the inaudible preview has not been established.
- [x] **Unavailable action:** Subtle feedback for a blocked action, such as firing during cooldown, deploying barrels without ammunition, or attempting an unavailable repair. Prevent sound spam from repeated input.
  - Implemented on 2026-09-28: `Assets/Audio/Downloaded Audio/UI_ActionDenied.mp3`.
  - Selected by the user on 2026-09-27: [Denied sound.mp3 by Mendenhall02](https://freesound.org/people/Mendenhall02/sounds/522720/). Source license: CC0.
  - Earlier audition feedback on 2026-09-27: the user could not hear [pong sound effect ui button by Troube](https://freesound.org/people/Troube/sounds/686543/). Not selected. The cause of the inaudible preview has not been established.
- [x] **Menu click / confirmation:** One shared feedback sound for menu buttons and confirming selections. Implemented and finally approved by the user on 2026-10-02. The user confirmed on 2026-09-28 that click and confirmation are the same requirement; do not layer two sounds.
  - Selected by the user on 2026-09-27: [Click by colorsCrimsonTears](https://freesound.org/people/colorsCrimsonTears/sounds/562294/). Source license: CC0. Imported as `Assets/Audio/Downloaded Audio/UI_MenuClick.wav`; connected to Game Over restart, Main Menu and Pause Menu buttons.

## 8. Music

- [x] **Background music for the main screens:** Implemented and finally approved by the user on 2026-10-02.
  - Selected by the user on 2026-09-27: [The Buccaneer's Haul by Shane Ivers](https://www.silvermansound.com/free-music/the-buccaneers-haul). Source license: CC BY 4.0; credit Shane Ivers, link the source and license, and identify any modifications. The track is approximately 2:43 long. Imported as `Assets/Audio/Downloaded Audio/Music_MainMenu.mp3`; connected to the Main Menu and approved.

Sailing and combat music are not part of the agreed list at this stage.

## Shared Guidelines

- World sounds, such as gunfire, impacts, and splashes, should account for position and distance so distant events do not sound as though they happened beside the player.
- UI and collection feedback should remain clear during combat.
- Audio files may be shared between suitable events. Each checkbox represents behavior to complete, not necessarily a separate audio file.
- After completing an item, its selected asset name or file path can be recorded underneath for tracking.
