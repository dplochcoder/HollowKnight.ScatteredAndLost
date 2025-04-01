# Scattered and Lost

Scattered and Lost is a [Celeste](https://www.celestegame.com/)-themed extension of Hollow Knight, [showcased](https://www.twitch.tv/videos/2387984304?collection=g3fP-sl7JhgoHQ) during rando day of the HK8Y celebration. It introduces a new sub-area to Hallownest, "Bretta's House: C-Side" consisting of four difficult platforming challenges built around Celeste platforming elements.

It is intended to be played with standard HK movement, and is not designed for or tested with the separate, unaffiliated [CelesteDash](https://github.com/kot9pa16lvl/HKCelesteDash) or [CelesteKnight](https://github.com/zoteline/CelesteKnight) mods.

Bretta's House: C-Side can be enjoyed either in its original Plando form, as a [Randomizer](https://github.com/homothetyhk/RandomizerMod) connection, or as a DLC extension of a vanilla HK save file.

## Installation

Scattered and Lost can be installed through [Lumafly](https://themulhima.github.io/Lumafly/).

The intended experience involves Celeste BGM. If you have Hollow Knight _and_ Celeste installed through Steam, you can select 'Extract Celeste BGM' from the mod options menu to automatically perform the music copying outlined below. If you have a Celeste installation elsewhere, you can provide its path in the ScatteredAndLost global settings .json explicitly as well.

Otherwise, you can purchase the tracks from [Bandcamp](https://radicaldreamland.bandcamp.com/album/celeste-b-sides), or install custom music instead. To manually install music, create a 'Music' directory inside the Scattered and Lost mod directory, and add files named the following:

  - music1.(mp3|ogg|wav) or music1intro.(mp3|ogg|wav) + music1loop.(mp3|ogg|wav)
  - Same for music2*.*

The intended tracks are `01 - Forsaken City (Sever the Skyline Mix)` (music1) and `05 - Mirror Temple (Mirror Magic Mix)` (music2)

## DLC mode

If you just want to play the DLC rooms, then after installing the mod you need to:

1)  In mod options, set "Enable in Vanilla" to "Enabled"
2)  Create a new classic HK save file
3)  Rescue Bretta from Fungal Wastes and acquire full movement, including Nail Arts, to access the DLC.

For quick set up, install [DebugMod](https://github.com/TheMulhima/HollowKnight.DebugMod). Once your new save is loaded, activate the "Gear up for content" hook on the "Scattered and Lost" page of debug bindings, then warp to Dirtmouth bench. This will tick the 'saved Bretta' flag, give all necessary movement, spells, and combat upgrades intended for the DLC.

Bretta's House: C-Side implements a checkpointing system, where re-entering Bretta's house will always put you directly into the furthest room of the DLC you have reached. You can use DebugMod bindings to reset this checkpoint to replay the content without creating a new save file.

## The Plando

To get the same experience as the HK8Y runners, download the [HK8Y Plando](https://github.com/dplochcoder/HollowKnight.ScatteredAndLost/blob/main/ScatteredAndLost/Plando/HK8Y%20Scattered%20and%20Lost.zip) in addition to the Scattered and Lost mod. See [SHO](https://www.smallhomothetyorganization.org/rando/plandoguide) for detailed instructions on installing plandos.

The plando is a challenging experience crafted specifically for the best of HK's rando runners, and requires executing many difficult shade skips and other techniques to complete. The plando is also specifically designed to be played by two players in a cooperative [ItemSync](https://github.com/Shadudev/HollowKnight.MultiWorld/blob/master/ItemSyncMod/README.md) and cannot be experienced by a single player, so make sure you have a buddy to play with.

The goal of the plando is to check Bretta's mask shard. The plando is compatible with [RMM](https://github.com/syyePhenomenol/RandoMapMod) for logic tracking.

### Required Mods

In addition to ScatteredAndLost and its dependencies, to play the plando you will need to install the following mods as well:

- AccessRandomizer
- ICExtraDeployers
- ItemChangerDataLoader
- ItemSync
- MoreDoors
- RandoPlus
- RainbowEggs
- ReopenCityDoor
- Scatternest

## Randomizer Connection

Bretta's House: C-Side can be experienced as a randomizer connection with various configurable settings. It is integrated with [RSM](https://github.com/BadMagic100/RandoSettingsManager) for ease of sharing.

### Heart Logic

If enabled, two heart gates will block off access to the DLC rooms. Runners must obtain the required number of hearts for both doors to gain access to the rest of the content.

A preview tablet reveals what lies behind each gate, at Bretta's Mask Shard, and at Great Prince Zote. It does not however reveal the items at soul totems within the DLC rooms, if randomized.

Enabling heart logic means adding N+Tolerance heart items to the rando without adding any additional locations, which can greatly unbalance shops. It is recommended to set N relatively low, unless re-balancing the count through other means, such as adding [MoreDoors](https://github.com/dplochcoder/HollowKnight.MoreDoors) key locations but no keys, enabling full flexible, or consolidating mask shards/vessel fragments to smaller counts.

### Soul Totems

Bretta's House: C-Side is full of infinite, super-powered soul totems that immediately refill your health and soul to full in a single hit. If you are so inclined, these soul totems can be randomized, putting random obtains into the DLC rooms and generally making them more difficult. The soul totem items themselves can offer a nice reprieve wherever else they are discovered.

### Transition Rando

Bretta's House: C-Side supports all manners of transition rando, so long as the checkpoint system is disabled. Dreamgates can be placed all throughout the rooms for ease of re-access. All DLC rooms are considered to lie within a separate titled area, a sub-area within Dirtmouth, for the purpose of map & full-area rando.

For your sanity, a shortcut gate exists at the end of the final room, allowing you to skip the final gauntlet and battle after it has been completed once.

## Decoration Master Integration

Scattered and Lost integrates with [Decoration Master](https://github.com/ygsbzr/HollowKnight.Decoration/tree/master), allowing you to place custom Celeste assets in Hollow Knight levels.

Some assets have complicated interactions, which are documented here.

### Zippers

Zippers must have their path and spikes configured.

- Use "Size X" and "Size Y" to set the direction and distance the zipper moves once activated.
- Use "Set Note" to set spikes, using 1 for no spikes and 2 for spikes in clockwise order from the top.
    - 1111 is no spikes.
    - 2222 is all spikes.
    - 2112 is spikes top and left.
    - 1221 is spikes right and bottom.
    - If an invalid number is used, no spikes will appear.

### Switches

Switches and Switch-Doors must have the same "Gate" set to function as a group.

- Switch-Doors have a configurable size, and a configurable offset to move when opened.
    - The distance in x-units is configured using "Color: Red"
    - The distance in y-units is configured using "Color: Green"
    - Decoration Master doesn't support generic property names, don't blame me
- Doors will never open unless at least one Switch is assigned to them, with the same group number.
    - If multiple doors are assigned to the same group, they will all open only when all switches are activated.
    - Switches de-activate, and doors close, if the player takes hazard damage.

### Bumpers

Bumpers have no special requirements, and can be placed freely.

### Bubbles

Bubbles can be placed freely, but it is strongly recommended not to make them overlap. This can lead to buggy interactions.

Bw careful not to place bubbles on top of the player while in noclip, and do not noclip into bubbles. It is rather buggy.
