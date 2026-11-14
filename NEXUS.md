# Nexus Mods page: ReStory More Work

Everything to paste into the Nexus upload form, field by field. Game page:
https://www.nexusmods.com/games/restorychillelectronicrepairs

## Basics

| Field          | Value                                                         |
|----------------|---------------------------------------------------------------|
| Game           | ReStory: Chill Electronic Repairs                             |
| Mod name       | ReStory More Work - Extra Orders and Marketplace Listings     |
| Category       | Miscellaneous                                                 |
| Version        | 1.0.0                                                         |
| Language       | English                                                       |
| Author         | ravidd9                                                       |
| Adult content  | No                                                            |

## Summary (short description)

```
More Outlink Express orders and second-hand Marketplace listings: press F2 or F3, or get extras every morning. Made by the game's own generators, so they pay, expire, rate and save like normal. Pick order types (e.g. cleaning only) and arrival hours. Licensed devices only. Works with the clock paused.
```

## Description (BBCode)

Paste everything inside the block into the description editor.

```
[size=5][b]ReStory More Work[/b][/size]

Ran out of jobs, or bought everything on the Marketplace? This mod gives you more work: extra Outlink Express orders and extra second-hand listings, whenever you want them.

Everything it adds is made by the game's [b]own[/b] order and listing generators. Payment, deadlines, reputation, prices, conditions, seller ratings and descriptions all come out exactly like the game's own, and they save and load like the game's own.

[size=4][b]Features[/b][/size]

[b]Outlink Express orders[/b]
[list]
[*][b]F2[/b] puts a new order in your inbox right away.
[*][b]Shift + F2[/b] receives, right now, every order the game already scheduled for later today. Handy when the clock is paused, because scheduled mail only arrives while it runs.
[*]Optional [b]extra orders every morning[/b], arriving only between the in-game hours you choose (for example 10:00 to 14:00, or 20:00 to 02:00).
[*]Choose the [b]kind of orders[/b] the mod makes, each with its own weight: the game's normal mix, cleaning only, repair only, painting, hacking or stickers. Job types you haven't unlocked yet are skipped.
[/list]

[b]Second-hand Marketplace[/b]
[list]
[*][b]F3[/b] adds new listings right away (5 by default).
[*][b]Shift + F3[/b] replaces the random listings that are not in your cart. Scripted story listings, the game's unlicensed teaser listings and your cart are never touched.
[*]Optional [b]extra listings every morning[/b].
[*][b]Only devices you hold the licence for[/b] are listed.
[/list]

[b]Works with the clock paused[/b] (for example with TimeIsMoney). Every hotkey works while the clock is stopped.

[size=4][b]Safe for your saves[/b][/size]
[list]
[*]The mod stores no data of its own. Orders and listings are the game's own types.
[*]Tested: a save with mod-made orders and listings loads fine [b]with and without[/b] the mod. If you remove the mod, what it already added simply stays in your game.
[*]The defaults change nothing until you press a hotkey.
[*]At startup the mod checks everything it uses in the game. After a game update that breaks it, it turns itself off and says so in the log instead of misbehaving.
[/list]

[size=4][b]Requirements[/b][/size]
[list]
[*][url=https://github.com/BepInEx/BepInEx/releases]BepInEx 5.4.23.x[/url] (the [i]win_x64[/i] zip). MelonLoader is not supported.
[/list]

[size=4][b]Installation[/b][/size]
[list=1]
[*]Install BepInEx 5 into the game folder (the one with [i]Restory.exe[/i]) and start the game once.
[*]Extract this mod's zip into the same game folder, so the DLL ends up at:
[code]...\Restory\BepInEx\plugins\ReStoryMoreWork\ReStoryMoreWork.dll[/code]
[*]Start the game. [i]BepInEx\LogOutput.log[/i] should contain:
[code][Info   :ReStory More Work] Loaded. Self-check passed.[/code]
[/list]

[size=4][b]Controls[/b][/size]
[list]
[*][b]F2[/b]: add one order now
[*][b]Shift + F2[/b]: receive today's scheduled orders now
[*][b]F3[/b]: add Marketplace listings now
[*][b]Shift + F3[/b]: replace the listings that aren't in your cart
[/list]
All keys can be changed in the config. Hotkeys are ignored while you type in a text box.

[size=4][b]Configuration[/b][/size]
The config file [i]BepInEx\config\restory.morework.cfg[/i] is created the first time you start the game with the mod. Edit it while the game is closed. Every setting has a description in the file.
[list]
[*][b][Orders][/b] Enabled, AddOrderKey, ExtraOrdersPerDay (0-30), ArrivalStartHour / ArrivalEndHour, MoveGameOrdersIntoHours
[*][b][Orders - Types][/b] Normal, CleaningOnly, RepairOnly, Painting, Hacking, Stickers (weights 0-100). Cleaning jobs only: Normal = 0, CleaningOnly = 100.
[*][b][Marketplace][/b] Enabled, RestockKey, RestockAmount (1-30), ExtraListingsEachMorning (0-30), MaxListings
[*][b][General][/b] ShowMessages (the short on-screen message after a hotkey)
[/list]

[size=4][b]Good to know[/b][/size]
[list]
[*]With the clock paused, the hotkeys work, but anything that is [i]scheduled[/i] waits for the clock: extra daily orders, the game's own listings for later today, and the courier who brings and collects your devices.
[*]Painting, hacking and sticker orders always come with a cleaning task. That's the game's own rule: every order needs a cleaning or repair job.
[*]The game skips its own daily Marketplace batch while 15 or more devices are listed, so restocking past 15 means fewer new game listings the next morning.
[*]Shift + F3 can replace the listing the game posts when you buy a licence, because it looks like any other random listing.
[/list]

[size=4][b]Compatibility[/b][/size]
Tested together with [b]Better Workbench[/b] and [b]TimeIsMoney[/b]. Not tested with other mods that change orders or the Marketplace (for example Auto Accept Orders or ReStory Desktop Tools). They will probably work side by side, but let me know if not.

[size=4][b]Source code[/b][/size]
[url=https://github.com/ravidd9/ReStory-MoreWork]github.com/ravidd9/ReStory-MoreWork[/url] (MIT License)

[size=4][b]Credits[/b][/size]
Project layout and a few helper routines are adapted from [url=https://www.nexusmods.com/restorychillelectronicrepairs/mods/42]Better Workbench[/url] by CogworkDancer (MIT License).

[size=4][b]AI disclosure[/b][/size]
The code and this description were written with an AI coding assistant (Claude Code). I directed it and tested the mod in-game, including the save tests above.
```

## Files tab

| Field            | Value                                                                  |
|------------------|------------------------------------------------------------------------|
| File             | `dist/ReStoryMoreWork-1.0.0.zip`                                       |
| File name        | ReStory More Work 1.0.0                                                |
| Category         | Main files                                                             |
| Version          | 1.0.0                                                                  |
| File description | BepInEx 5 plugin. Extract into the game folder (the one with Restory.exe). |

## Requirements tab

- Off-site requirement: **BepInEx 5.4.23.x** - https://github.com/BepInEx/BepInEx/releases
  Note: "Requires BepInEx 5 (win_x64). MelonLoader is not supported."

## Permissions and credits tab

- Credits: Better Workbench by CogworkDancer (project layout and helpers, MIT) - https://www.nexusmods.com/restorychillelectronicrepairs/mods/42
- Source is MIT on GitHub, so open permissions fit: modification and reuse allowed with credit.
- AI training / forks: your choice. Nexus defaults to "Ask Me".

## AI disclosure

- **AI-Generated Content:** the code was mostly written by an AI assistant.
- **AI Media:** the mod page description was written by AI. Tick this too unless you rewrite the text
  yourself.
- Screenshots you take in-game are not AI media.

## Images

Take 3-4 in-game screenshots (F12 in Steam):

1. The Outlink Express inbox with a new order and the "New order in the inbox" message at the top.
2. The Marketplace after F3, showing new listings.
3. Optional: the config file open, showing the order type weights.

Use the inbox or Marketplace shot as the primary image.

## Changelog

```
1.0.0 - First release.
```
