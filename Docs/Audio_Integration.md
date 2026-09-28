# Audio integration

## Setup (2026-09-28)

The 18 selected clips are imported from `Assets/Audio/Downloaded Audio/`.
The five edited clips are used instead of the recordings under `Source/`.
Original audio files are not rewritten by this integration.

`Game Audio` in `Assets/Scenes/GameScene.unity` holds clip references and per-event
volume, distance, interval, and voice limits in its `GameAudio` component.
`ShipAudio` on `Assets/Prefabs/Ship.prefab` supplies collision and capsize sounds
to both the player and enemies. The existing Main Camera listener is retained.

World effects are imported as mono for positional playback; UI/ambience retain
their channels. Long ocean, bow-water, and music clips use streaming Vorbis;
short effects use PCM/decompress-on-load. These are Unity import settings only.

## Connected behavior

- Ocean loops throughout gameplay. Bow water fades with player speed; sails form
  a quiet player-ship layer and fade on death.
- Actual cannon volleys emit one report per volley. Ship/land hits and water hits
  have separate clips. Projectile whistles were removed at the user's request
  after audition on 2026-09-28; the audio files are retained for reference.
- Hull collision sound uses closing speed, one ship per ship pair, collision-enter
  callbacks and a cooldown. Projectiles are excluded from collision audio.
- Deployed barrels emit quiet water-entry sounds; their group detonation emits
  one heavy explosion. Ship destruction emits a blast at each of the three VFX.
- Capsize creaks run during slow roll/fast capsize and fade out afterwards.
  Hull/mast splash sounds follow the existing water-impact markers.
- Both successful pickup types play the shared collection sound. Player repair
  plays after health increases and a part is consumed. Direction switching and
  blocked player actions have UI feedback; blocked feedback is rate limited.

## Pending

There are no main-menu scenes yet. `Music_MainMenu` and `UI_MenuClick` are assigned
in the manager for future use but are not triggered during gameplay. Main-screen
music transitions/loop preparation and menu click integration remain to be done.
Menu click and confirmation are one shared requirement, using `UI_MenuClick.wav`, as confirmed by the user on 2026-09-28. Optional sailing hull creaks are undecided.

The heavy blast is 7.31 seconds; its fit against successive blasts still needs listening.
Ocean/water/sail loop seams, creak repetition, relative levels and distances are
initial settings pending an audible gameplay pass. Source loops were not edited.

## Listening checklist

1. Idle, sail, brake and stop: ocean stays stable, bow water follows speed.
2. Fire front and broadsides; retry on cooldown. Listen to enemy shots near/far,
   ship/land hits and water hits.
3. Contact another ship and the shore; sustained contact must not chatter.
4. Deploy a barrel group; hear its water entry and one group explosion.
5. Destroy a ship: three blasts, creaks during roll, splashes at water contact.
6. Collect each pickup; repair successfully and attempt invalid actions.
7. Switch gameplay cameras: one listener remains active.

## Required attribution (from the approved source checklist)

- **Item Pickup sound — Strechy**:
  https://freesound.org/people/Strechy/sounds/654251/
  License: CC BY 4.0, https://creativecommons.org/licenses/by/4.0/
  File: `Pickup_Collect.ogg`. Renamed; no source waveform edits by this integration.
- **Change Weapon Sound — knova**:
  https://freesound.org/people/knova/sounds/170273/
  License: CC BY 4.0, https://creativecommons.org/licenses/by/4.0/
  File: `UI_SwitchFireDirection.wav`. Renamed; no source waveform edits.
- **The Buccaneer's Haul — Shane Ivers**, https://www.silvermansound.com
  Source: https://www.silvermansound.com/free-music/the-buccaneers-haul
  License: CC BY 4.0, https://creativecommons.org/licenses/by/4.0/
  File: `Music_MainMenu.mp3`. Renamed; no source waveform edits.

Playback volume is adjusted in Unity; music is transcoded by the importer.
Include these credits with the distributed game's credits/license materials.
Other chosen sources and the user's editing decisions are in `Audio_Checklist.md`.
