# ReStory More Work

A BepInEx mod for **ReStory: Chill Electronics Repairs** that gives you more to do: more Outlink Express
orders and more second-hand Marketplace listings. Everything it adds is made by the game's own generators, so
orders and listings look, pay, expire, rate and save exactly like the game's own.

Works with the in-game clock paused (for example with the TimeIsMoney mod).

## Features

**Outlink Express orders**

- Press **F2** to get a new order in your inbox right away.
- Press **Shift + F2** to receive, right now, every order the game has already scheduled for later today. That
  mail only arrives while the clock runs, so this is how to get it while the clock is paused.
- Optional extra orders every morning (`ExtraOrdersPerDay`), arriving only between the hours you choose.
- Choose which kinds of order the mod makes: the game's normal mix, cleaning only, repair only, painting,
  hacking or stickers, each with its own weight.

**Second-hand Marketplace**

- Press **F3** to add new listings right away (`RestockAmount`, default 5).
- Press **Shift + F3** to replace every random listing that is not in your cart with fresh ones. The game's
  scripted (story) listings, its unlicensed teaser listings and your cart are never touched. The listing the
  game posts when you buy a licence looks like any other random listing, so it can be replaced.
- Optional extra listings every morning (`ExtraListingsEachMorning`).
- Only devices you hold the licence for are listed. The game itself also lists a couple of devices you have no
  licence for, as a teaser; you can buy those, but you can't work on them until you buy the licence.

## How it works

- **Orders.** The game makes each order with `RandomDevicesGenerationService`. That picks an unlocked,
  licensed device and rolls its dirty or broken parts, plus any paint, sticker or hacking jobs. Payment comes
  from `DevicePriceEstimationService`, and the sender, comment and 3-day deadline come from the game's own
  lists and settings. The mod calls those same functions and puts the result into the game's own inbox lists.
  Painting, hacking and sticker orders always come with a cleaning task, because the game requires every
  order to include a cleaning or repair job.
- **Listings.** Each listing comes from the game's `GetRandomlyGeneratedDeviceShopLot`, which rolls the
  seller, rating, condition, price, description and how long the listing stays up.
- **Saves.** The mod never stores any data of its own. Orders and listings are the game's own types, so a
  save loads fine after the mod is removed. Orders and listings the mod already made simply stay in the game.

## Requirements

- BepInEx 5.4.23.x (x64, Mono).

## Installation

1. Install BepInEx 5 into the game folder (the one containing `Restory.exe`) and run the game once.
2. Put `ReStoryMoreWork.dll` in `BepInEx\plugins\ReStoryMoreWork\`.
3. Start the game. `BepInEx\LogOutput.log` should contain:

   ```
   [Info   :ReStory More Work] Loaded. Self-check passed. ...
   ```

## Controls

| Key            | Action                                                              |
|----------------|---------------------------------------------------------------------|
| **F2**         | Adds one order to the Outlink Express inbox now.                    |
| **Shift + F2** | Receives all of today's scheduled orders now.                       |
| **F3**         | Adds `RestockAmount` Marketplace listings now.                      |
| **Shift + F3** | Replaces the random listings that are not in your cart.             |

Hotkeys are ignored while you type in a text box.

## Configuration

BepInEx writes `BepInEx\config\restory.morework.cfg` the first time the game runs with the mod. Edit it while
the game is closed. The defaults change nothing about the game until you press a hotkey.

### `[General]`

| Setting        | Default | What it does                                        |
|----------------|---------|-----------------------------------------------------|
| `ShowMessages` | `true`  | Short on-screen message when a hotkey does something. |

### `[Orders]`

| Setting                   | Default | What it does                                                         |
|---------------------------|---------|----------------------------------------------------------------------|
| `Enabled`                 | `true`  | All order features on/off.                                           |
| `AddOrderKey`             | `F2`    | Order hotkey (Shift = receive scheduled orders). `None` turns it off. |
| `ExtraOrdersPerDay`       | `0`     | Extra orders scheduled every morning (0-30).                         |
| `ArrivalStartHour`        | `8`     | Earliest hour extra orders arrive (0-23).                            |
| `ArrivalEndHour`          | `22`    | Latest hour extra orders arrive (0-24). 20 to 2 crosses midnight; 0 to 24 is any time. |
| `MoveGameOrdersIntoHours` | `false` | Also moves the game's own daily orders into those hours.             |

### `[Orders - Types]`

Weights (0-100) for the orders the mod makes. 0 = never. Types you haven't unlocked yet are skipped. The
game's own orders are not affected.

| Setting        | Default |
|----------------|---------|
| `Normal`       | `100`   |
| `CleaningOnly` | `0`     |
| `RepairOnly`   | `0`     |
| `Painting`     | `0`     |
| `Hacking`      | `0`     |
| `Stickers`     | `0`     |

Example, cleaning jobs only: `Normal = 0`, `CleaningOnly = 100`.

### `[Marketplace]`

| Setting                    | Default | What it does                                              |
|----------------------------|---------|-----------------------------------------------------------|
| `Enabled`                  | `true`  | All Marketplace features on/off.                          |
| `RestockKey`               | `F3`    | Restock hotkey (Shift = replace). `None` turns it off.    |
| `RestockAmount`            | `5`     | Listings per restock (1-30).                              |
| `ExtraListingsEachMorning` | `0`     | Extra listings every morning (0-30).                      |
| `MaxListings`              | `40`    | The mod stops adding at this many listed devices (5-100). |

The game skips its own daily Marketplace batch while 15 or more devices are listed. Restocking past 15 means
the next morning brings no new game listings until you buy some.

## Notes

- **Clock paused:** hotkeys work. Scheduled things only happen while the clock runs: extra daily orders, the
  game's own listings for later today, and the courier who drops off and picks up your devices.
- **Game updates:** at startup the mod checks every game function it uses. If the game changed, the log says
  `SELF-CHECK FAILED` and the mod turns itself off instead of misbehaving.

## Building

Needs the .NET SDK and the game with BepInEx installed. If your game is not at
`C:\Program Files (x86)\Steam\steamapps\common\Restory`, create `ReStoryMoreWork.local.props` next to the
`.csproj` file:

```xml
<Project>
  <PropertyGroup>
    <GameDir>D:\Program Files (x86)\Steam\steamapps\common\Restory</GameDir>
  </PropertyGroup>
</Project>
```

Then run `dotnet build -c Release`. The build copies the DLL into `BepInEx\plugins\ReStoryMoreWork\`. No
game or BepInEx DLL is ever copied into this repository.
