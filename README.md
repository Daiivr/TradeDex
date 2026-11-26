<p align="center">
  <img src="SysBot.Pokemon.WinForms/Resources/picture_logo.png" alt="TradeDex" width="540">
</p>

<p align="center">
  A desktop control center for Pokémon trading bots on Nintendo Switch.
  Manage your bots in one place, take trade requests through Discord, and keep track of every queue.
</p>

<p align="center">
  <a href="https://github.com/Daiivr/TradeDex/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/Daiivr/TradeDex?style=flat-square&color=16b8e6"></a>
  <a href="https://github.com/Daiivr/TradeDex/actions/workflows/release.yml"><img alt="Release build" src="https://img.shields.io/github/actions/workflow/status/Daiivr/TradeDex/release.yml?style=flat-square&label=release%20build"></a>
  <a href="LICENSE"><img alt="License: AGPL-3.0" src="https://img.shields.io/badge/license-AGPL--3.0-7756d8?style=flat-square"></a>
  <img alt="Windows x64" src="https://img.shields.io/badge/platform-Windows%20x64-0b73c5?style=flat-square">
</p>

<p align="center">
  <a href="https://github.com/Daiivr/TradeDex/releases/latest">Download TradeDex</a> ·
  <a href="#getting-started">Getting started</a> ·
  <a href="#full-command-reference">Commands</a> ·
  <a href="docs/Cloudflare-Tunnel-Trade-Portal.md">Trade Portal guide</a>
</p>

---

## What is TradeDex?

TradeDex is a Windows application for running and managing Pokémon trade bots. Its desktop interface brings together bot controls, queues, logs, configuration, and trade activity. A connected Discord bot lets your community request trades through slash commands while TradeDex handles the queue and the Switch connection.

It supports these games:

| Game | Abbreviation |
| --- | --- |
| Let's Go, Pikachu! / Let's Go, Eevee! | LGPE |
| Sword / Shield | SWSH |
| Brilliant Diamond / Shining Pearl | BDSP |
| Legends: Arceus | PLA |
| Scarlet / Violet | SV |
| Legends: Z-A | PLZA |

### Highlights

- Run and monitor multiple bots from a themed desktop dashboard, with English and Spanish interface text.
- Accept standard, hidden, item, egg, clone, dump, FixOT, event, and batch trade requests through Discord.
- Let users create Pokémon with guided, game-specific slash commands or submit Showdown sets and supported PKM files.
- Track queue position, trade counts, profiles, and medals; review activity and logs from the app.
- Use the local web control panel and Trade Portal when enabled in your configuration.
- Check GitHub releases from the app for new versions.

### Desktop controls

The app includes bot start, stop, restart, and routine controls; per-game mode panels; queue and trade-status views; searchable logs; configurable themes; and update notices. Where configured, owners can also use Switch-control helpers, SysDVR, and the local web dashboard. The control panel is designed for Windows rather than a browser-only deployment.

## Getting started

1. Download the latest **`TradeDex_<version>.exe`** from [Releases](https://github.com/Daiivr/TradeDex/releases/latest). The release asset is a single, self-contained Windows x64 executable; there is no ZIP to unpack.
2. Run the EXE and configure your Switch connection and bot settings in the desktop app.
3. If you want Discord requests, add your bot token and configure the channels and roles your server should use.
4. Start a bot, then try a trade request or use `/help` in Discord to see the commands available to your server.

Configuration and access rules are controlled by the bot owner. A command may be unavailable if its game, queue, role, or feature is disabled.

> [!NOTE]
> TradeDex is a community project, not an official Nintendo or Pokémon application. Use it responsibly and follow the rules of the games and services you connect to.

## Discord commands

TradeDex uses slash commands for its main request flows. Here are a few starting points:

| Command | What it does |
| --- | --- |
| `/trade` | Queue a trade from a Showdown set or uploaded PKM file. |
| `/hidetrade` | Queue a trade without showing its details in the channel. |
| `/clone` | Clone the Pokémon you show during a Link Trade. |
| `/dump` | Read and send back the Pokémon you show during a Link Trade. |
| `/fixot` | Correct an advertised OT or nickname when possible. |
| `/egg` and `/itemtrade` | Request an egg or an item trade. |
| `/queue status` and `/queue clear` | Check your place or leave the queue. |
| `/batch trade` and `/batch zip` | Request multiple trades from sets or an archive. |
| `/wondercard list` and `/wondercard request` | Browse and request Wonder Card events. |
| `/events list` and `/events request` | Browse and request event files. |
| `/help` | See the complete command list, including owner-only tools. |

Guided creation commands are available for each supported game: `/create-lgpe`, `/create-swsh`, `/create-bdsp`, `/create-pla`, `/create-sv`, and `/create-plza`. TradeDex also has commands for legality checks, profiles, stored trade codes, PokéPaste, mystery trades, and more. Some older prefix commands remain in the codebase, but slash commands are the recommended Discord interface.

## Full command reference

The tables below cover the main request flows as well as the older prefix commands. A slash command may use a guided form rather than the exact text shown in a prefix example. Role and owner restrictions are set in your Discord configuration; `/help` shows the commands available to your server.

### Trades and Pokémon tools

| Slash command | Prefix command / alias | Purpose |
| --- | --- | --- |
| `/trade` | `trade`, `t` | Trade from a Showdown set or PKM file. |
| `/hidetrade` | `hidetrade`, `ht` | Hide trade details from the public channel. |
| `/clone` | `clone`, `c` | Copy the Pokémon shown to the bot during a Link Trade. |
| `/dump` | `dump`, `d` | Send back the data of Pokémon shown during a Link Trade. |
| `/fixot` | `fixOT`, `fix`, `f` | Fix an advertised OT or nickname when possible. |
| `/egg` | `egg` | Trade an egg generated from a submitted set. |
| `/itemtrade` | `itemTrade`, `item`, `it` | Trade a Pokémon holding a requested item. |
| `/dittotrade` | `dittoTrade`, `ditto`, `dt` | Request a Ditto with chosen stats and other options. |
| `/mysteryegg` | `mysteryegg`, `me` | Request a random shiny egg. |
| `/mysterymon` | `mysterymon`, `mm` | Request a random Pokémon. |
| `/pokepaste` | `pokepaste`, `pp` | Generate a team from a PokéPaste URL. |
| `/convert` | `convert`, `showdown` | Convert a Showdown set to RegenTemplate format. |
| `/legalize` | `legalize`, `alm` | Attempt to legalize a PKM file. |
| `/validate`, `/verbose` | `validate`, `lc`, `verbose`, `lcv` | Check legality, with optional verbose output. |

### Batch, events, and file libraries

| Slash command | Prefix command / alias | Purpose |
| --- | --- | --- |
| `/batch trade` | `batchTrade`, `bt` | Trade multiple Showdown sets separated by `---`. |
| `/batch zip` | `batchTradeZip`, `btz` | Trade files from a ZIP, RAR, or 7z archive. |
| `/batch mysterymon`, `/batch mysteryegg` | `batchTradeMysteryMon`, `batchTradeMysteryEgg` | Queue multiple mystery trades. |
| `/batch item` | `itemBatchTrade`, `ibt` | Repeat an item trade. |
| `/texttrade upload`, `/texttrade view`, `/texttrade trade` | `textTrade`, `textView` | Upload, inspect, and trade from a text-based set list. |
| `/wondercard list`, `/wondercard request`, `/wondercard download` | `specialRequest`, `sr`, `getEvent` | Browse, request, or download Wonder Card events. |
| `/events list`, `/events request` | `listEvents`, `eventRequest` | Browse and request event files. |
| `/battleready list`, `/battleready request` | `battleReadyList`, `battleReadyRequest` | Browse and request battle-ready files. |
| `/homeready list`, `/homeready request` | `homeReadylist`, `homeReadyRequest` | Browse and request HOME-ready files. |

### Queue, profile, and trade codes

| Slash command | Prefix command / alias | Purpose |
| --- | --- | --- |
| `/queue status` | `queueStatus`, `qs` | Check your queue position. |
| `/queue clear` | `queueClear`, `qc` | Leave your current queue. |
| `/changetradecode` | `changeTradeCode`, `ctc` | Set a stored eight-digit Link Trade Code. |
| `/deletetradecode` | `deleteTradeCode`, `dtc` | Remove your stored code so the next trade can assign another. |
| `/myinfo` | `profile`, `tp` | View your trainer profile, trade count, and medals. |
| `/help`, `/info` | `help`, `info` | Show commands or bot information. |

Owners and moderators also have tools for queue control, bot start/stop/restart, user and server restrictions, announcements, logging, and Switch control. Those commands are intentionally omitted from the public quick-reference table because access depends on the server's configuration. The bot's help command is the authoritative list for a running instance.

### Owner and moderator prefix commands

These are the established management commands from the prefix-command interface. They are not all slash commands. Availability depends on the bot's owner, sudo list, roles, and channel restrictions.

| Area | Commands | Purpose |
| --- | --- | --- |
| Queue management | `queueClearAll`, `queueClearUser`, `queueToggle`, `queueList` | Clear, pause, or inspect queues. |
| Queue lists | `tradeList`, `fixOTList`, `cloneList`, `dumpList`, `seedList` | Inspect users waiting for each routine. |
| Bot lifecycle | `botStart`, `botStop`, `botIdle`, `botChange`, `botRestart`, `kill` | Start, stop, pause, change routine, restart, or shut down bots. |
| Bot information | `botStatus`, `status`, `ping` | Check bot state, environment, and responsiveness. |
| Access | `addSudo`, `removeSudo`, `blacklist`, `unblacklist`, `blacklistId`, `unBlacklistId` | Manage privileged users and Discord blocklists. |
| Server restrictions | `blacklistServer`, `unblacklistServer`, `listGuilds`, `leave`, `leaveGuild`, `leaveAll` | Restrict or leave Discord servers. |
| Trade restrictions | `banTrade`, `banID`, `unbanID`, `bannedIDComment`, `bannedIDSummary` | Manage trade bans and review their reasons. |
| Echo and logging | `announce`, `dm`, `addEmbedChannel`, `echoInfo`, `echoClear`, `echoClearAll`, `logHere`, `logClear`, `logClearAll`, `logInfo` | Send announcements and configure log/echo channels. |
| Switch controls | `click`, `setStick`, `setScreenOn`, `setScreenOff`, `peek`, `video` | Send controller input or capture the console screen. |
| Streaming and remote control | `startSysdvr`, `sysDvr`, `startController` | Launch or explain connected streaming/control tools. |

For example, `botChange <routine>` changes the bot's routine, `queueClearUser @user` removes a user, and `announce <message>` sends an owner announcement. Use the in-bot help and your configuration to confirm syntax before running a management command.

### Complete prefix-command index

<details>
<summary>Expand the full command and alias reference</summary>

This preserves the detailed prefix-command examples from the previous README. The slash-command groups above are the preferred Discord interface; availability and permissions depend on the current bot configuration.

### ⚡ Basic Commands

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `trade` | t | Trade a Pokémon from Showdown Set or PKM file. | `trade <Showdown Format>` or `<upload pkm>` | Everyone |
| `trade true` | t true | Trade a Pokémon from a PKM file, without AutoOT | `trade true <upload pkm>` | Everyone |
| `tradeUser` | tu, tradeOther | Trade the mentioned user the attached file. | `tradeuser @user` | Everyone |
| `hidetrade` | ht | Same as trade, but hides the embed. | `hidetrade <Showdown Format>` | Everyone |
| `clone` | c | Clone the Pokémon you show via Link Trade. | `clone` | Everyone |
| `dump` | d | Dump the Pokémon you show via Link Trade. | `dump` | Everyone |
| `egg` | Egg | Trade an egg via provided Pokémon set. | `egg <Showdown Format>` | Everyone |
| `seed` | checkMySeed, checkSeed, seedCheck, s, sc | Check a Pokémon seed. | `seedCheck` | Everyone |
| `itemTrade` | it, item | Trade a Pokémon holding a requested item. | `it <Leftovers>` | Everyone |
| `fixOT` | fix, f | Fix OT and Nickname of a Pokémon if an advert is detected. | `fixOT` | Everyone |
| `convert` | showdown | Convert a Showdown Set to RegenTemplate. | `convert <set>` | Everyone |
| `legalize` | alm | Attempt to legalize PKM data. | `legalize <pkm>` | Everyone |
| `validate` | lc, check, verify | Verify PKM legality. | `validate <pkm>` | Everyone |
| `verbose` | lcv | Verify PKM legality with verbose output. | `verbose <pkm>` | Everyone |
| `findFrame` | ff, GetFrameData | Prints next shiny frame from seed. | `findFrame <seed>` | Everyone |
| `deleteTradeCode` | dtc | Deletes the stored Link Trade Code for the user. | `dtc` | Everyone |
| `changeTradeCode` | ctc | Change your stored Link Trade Code. | `ctc 12345678` | Everyone |

### 🎯 Advanced Trade Features

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `textTrade` | tt, text | Upload a .txt/.csv/.rtf/.pdf of Showdown sets for batch trading. | `tt <upload .txt/.csv file>` | Everyone |
| `textView` | tv | View a specific Pokémon from your pending TextTrade file. | `tv 2` | Everyone |
| `listEvents` | le | Lists available event files via DM. | `le <species> <page2>` | Everyone |
| `eventRequest` | er | Downloads event attachments and adds to trade queue. | `eventRequest <file>` | Everyone |
| `battleReadyList` | brl | Lists available battle-ready files via DM. | `brl <species> <page2>` | Everyone |
| `battleReadyRequest` | br, brr | Downloads battle-ready attachments and adds to trade queue. | `battleReadyRequest <file>` | Everyone |
| `pokepaste` | pp, Pokepaste, PP | Generates a team from a PokePaste URL. | `pp <URL>` | Everyone |
| `dittoTrade` | dt, ditto | Trade a Ditto with requested stats, language, and nature. | `dt <IVs> <Language> <Nature> <Shiny> <OT/TID/SID/OTGender> <OriginGame>` | Everyone |
| `mysteryegg` | me | Get a random shiny 6IV egg. | `mysteryegg` | Everyone |
| `mysterymon` | mm, mystery, surprise | Get a fully random Pokémon. | `mysterymon` | Everyone |
| `randomTeam` | rt, RandomTeam, Rt | Generates a random team. | `randomTeam` | Everyone |
| `homeReady` | hr | Displays instructions for HOME-ready trading. | `homeReady` | Everyone |
| `homeReadyRequest` | hrr | Downloads HOME-ready files and adds to trade queue. | `homeReadyRequest <number>` | Everyone |
| `homeReadylist` | hrl | Lists available HOME-ready files. | `homeReadylist` | Everyone |
| `specialRequest` | sr, srp | Lists Wondercard events or requests specific ones. | `srp <game> <page2>` | Everyone |
| `getEvent` | ge, gep | Downloads the requested event as a PKM file. | `getEvent <eventID>` | Everyone |

### 📦 Batch Trading

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `batchTrade` | bt | Trade multiple Pokémon (max 6) from a list. | `bt <Set1> --- <Set2>` | Everyone |
| `batchTradeZip` | btz | Trade multiple Pokémon from a ZIP/RAR/7z file. | `btz <file.7z>` | Everyone |
| `batchInfo` | bei | Get info about a batch property. | `batchInfo <prop>` | Everyone |
| `batchValidate` | bev | Validate a batch property. | `batchValidate <prop>` | Everyone |
| `batchTradeMysteryMon` | btmm | Trade multiple Mystery Pokémon. | `btmm <number>` | Everyone |
| `batchTradeMysteryEgg` | btme | Trade multiple Mystery eggs. | `btme <number>` | Everyone |
| `itemBatchTrade` | ibt | Trade a specific item multiple times. | `ibt <item name> <number>` | Everyone |

### 📊 Queue Management

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `queueMode` | qm | Change queue control (manual/threshold/interval). | `qm manual` | Everyone |
| `queueClearAll` | qca, tca | Clear all users from all queues. | `qca` | Sudo, Owner |
| `queueClear` | qc, tc | Remove yourself from the queue. | `qc` | Everyone |
| `queueClearUser` | qcu, tcu | Clear a specified user (sudo required). | `qcu @user` | Sudo, Owner |
| `queueStatus` | qs, ts | Check your position in the queue. | `qs` | Everyone |
| `queueToggle` | qt | Enable/disable queue joining. | `qt` | Sudo, Owner |
| `queueList` | ql | DM the full queue list. | `ql` | Sudo, Owner |
| `tradeList` | tl | Show users currently in trade queue. | `tl` | Sudo, Owner |
| `fixOTList` | fl, fq | Prints the users in the FixOT queue. | `fixOTList` | Sudo, Owner |
| `cloneList` | cl, cq | Prints the users in the Clone queue. | `cloneList` | Sudo, Owner |
| `dumplist` | dl, dq | Prints the users in the Dump queue. | `dumplist` | Sudo, Owner |
| `seedList` | sl, scq, seedCheckQueue, seedQueue, seedList | Show seed check queue users. | `seedList` | Sudo, Owner |

### 🛠 Admin Tools

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `addSudo` | — | Add a user to global sudo. | `addSudo <ID>` | Owner |
| `removeSudo` | — | Remove a user from global sudo. | `removeSudo <ID>` | Owner |
| `blacklistServer` | bls | Adds a server ID to the server blacklist. | `blacklistServer <ID>` | Sudo, Owner |
| `unblacklistServer` | ubls | Removes a server ID from the server blacklist. | `unblacklistServer <ID>` | Sudo, Owner |
| `blacklist` | — | Blacklist a Discord user. | `blacklist @user` | Sudo, Owner |
| `unblacklist` | — | Remove a user from blacklist. | `unblacklist @user` | Sudo, Owner |
| `blacklistId` | — | Blacklist Discord user IDs. | `blacklistId <ID>` | Sudo, Owner |
| `unBlacklistId` | — | Unblacklist Discord user IDs. | `unBlacklistId <ID>` | Sudo, Owner |
| `blacklistComment` | — | Adds comment for blacklisted user. | `blacklistcomment <ID> <msg>` | Sudo, Owner |
| `banTrade` | bant | Ban a user from trading with reason. | `bant @user <reason>` | Sudo, Owner |
| `banID` | — | Ban an online user ID. | `banID <ID>` | Sudo, Owner |
| `unbanID` | — | Unban an online user ID. | `unbanID <ID>` | Sudo, Owner |
| `bannedIDComment` | — | Adds a comment for banned ID. | `bannedIDcomment <ID> <msg>` | Sudo, Owner |
| `bannedIDSummary` | printBannedID, bannedIDPrint | Show list of banned IDs. | `bannedIDSummary` | Sudo, Owner |
| `blacklistSummary` | printBlacklist, blacklistPrint | Show list of blacklisted users. | `blacklistSummary` | Sudo, Owner |

### 🎮 Switch Control

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `click` | — | Click a button on the Switch. | `click <IP> <Button>` | Sudo, Owner |
| `setStick` | — | Move stick to coordinates. | `setStick <IP> <Coords>` | Sudo, Owner |
| `setScreenOn` | screenOn, scrOn | Turn on screen. | `setScreenOn` | Sudo, Owner |
| `setScreenOff` | screenOff, scrOff | Turn off screen. | `setScreenOff` | Sudo, Owner |
| `setScreenOnAll` | screenOnAll, scrOnAll | Turn on screen for all bots. | `setScreenOnAll` | Sudo, Owner |
| `setScreenOffAll` | screenOffAll, scrOffAll | Turn off screen for all bots. | `setScreenOffAll` | Sudo, Owner |
| `peek` | repeek | Take and send a screenshot. | `peek` | Sudo, Owner |
| `video` | Video | Record a GIF from the Switch. | `video` | Sudo, Owner |
| `startSysdvr` | dvrstart, startdvr, sysdvrstart, dvr, stream | Start SysDVR streaming. | `startSysdvr` | Owner |
| `sysDvr` | — | Show instructions for SysDVR. | `sysDvr` | Owner |
| `startController` | controllerstart, startcontrol, controlstart, startremote, remotestart, sbr, controller | Start Switch Remote controller. | `startController` | Owner |

### 📡 Bot Management

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `ping` | — | Ping the bot to check if it's running. | `ping` | Sudo, Owner |
| `help` | — | Show all commands. | `help` | Everyone |
| `info` | about, whoami, owner, bot | Show bot information. | `info` | Everyone |
| `botStatus` | — | Get bot status via logs. | `botStatus` | Sudo, Owner |
| `botStart` | — | Start the bot. | `botStart` | Sudo, Owner |
| `botStop` | — | Stop the bot. | `botStop` | Sudo, Owner |
| `botIdle` | botPause, idle | Pause the bot. | `botIdle` | Sudo, Owner |
| `botChange` | — | Change the bot routine. | `botChange <FlexTrade>` | Sudo, Owner |
| `botRestart` | — | Restart the bot(s). | `botRestart` | Sudo, Owner |
| `status` | stats | Get the bot environment status. | `status` | Sudo, Owner |
| `kill` | shutdown | Shutdown the bot. | `kill` | Owner |

### 📢 Echo & Logging

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `announce` | Announce | Send an announcement to Echo channels. | `announce <msg>` | Owner |
| `dm` | - | Send a DM to a user as the bot. | `dm @user <msg>` | Owner |
| `addEmbedChannel` | aec | Assign a channel for bot embeds. | `addEmbedChannel #channel` | Sudo, Owner |
| `echoInfo` | — | Dump echo message settings. | `echoInfo` | Sudo, Owner |
| `echoClear` | rec | Clear echo settings for current channel. | `echoClear` | Sudo, Owner |
| `echoClearAll` | raec | Clear echo settings from all channels. | `echoClearAll` | Sudo, Owner |
| `logHere` | — | Log to current channel. | `logHere` | Sudo, Owner |
| `logClearAll` | — | Clear all log settings. | `logClearAll` | Sudo, Owner |
| `logClear` | — | Clear log settings for current channel. | `logClear` | Sudo, Owner |
| `logInfo` | — | Dump logging settings. | `logInfo` | Sudo, Owner |

### 🔐 Permissions & Guild

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `listGuilds` | lg, servers, listservers | List all guilds the bot is in. | `listGuilds` | Sudo, Owner |
| `leave` | bye | Leave current server. | `leave` | Sudo, Owner |
| `leaveGuild` | lg | Leave a guild by ID. | `leaveGuild <ID>` | Sudo, Owner |
| `leaveAll` | — | Leave all servers. | `leaveAll` | Sudo, Owner |

### 🎲 Misc & Fun

| Command | Aliases | Summary | Example | Permission |
|---------|---------|---------|---------|------------|
| `joke` | lol, insult | Tell a random joke. | `joke` | Everyone |
| `hello` | hi, hey, yo | Say hello to the bot. | `hello` | Everyone |
| `profile` | tp | View your profile, badges, trades, trainer info, and Ping Pong stats. | `profile` | Everyone |

</details>


## How trading works

### AutoOT and trainer details

When AutoOT is enabled, TradeDex can apply trainer information from the connected save during a trade. Requests can also carry explicit trainer information where the command and legality settings allow it. If you need to preserve the trainer details already inside an uploaded PKM file, use the trade option that disables AutoOT rather than relying on the default behavior. The exact result depends on the Pokémon's legality and game-specific restrictions.

### Personal Link Trade Codes

TradeDex can remember a user's Link Trade Code between requests. Use `/changetradecode` to choose an eight-digit code or `/deletetradecode` to remove it. The prefix equivalents are `ctc 12345678` and `dtc`. Whether codes are stored is configurable by the bot owner.

### Queues, progress, and medals

Trade requests enter game-appropriate queues. Users can check their position or leave a queue, while moderators can manage queues and bot availability. Completed trades contribute to the user's profile, level, achievements, and milestone medals. The first medal is awarded after one trade, with additional trade-count milestones beginning at 50. Use `/myinfo` or the prefix `profile` command to see your progress.

### DM forwarding and trade files

Owners can set `UserDMsToBotForwarder` under Hub → Discord to forward non-command DMs sent to the bot, including attachments, to a chosen channel. Restart the bot after changing that setting, and consider the privacy expectations of your community before enabling it. Event, battle-ready, and HOME-ready file libraries are served from folders configured by the owner. A PKM filename ending in an eight-digit code, such as `Great Tusk-Tera(Steel)-03760382.pk9`, can provide a trade code for that request.

## Showdown and batch-command options

TradeDex accepts Showdown sets and normalizes a number of batch-editor properties into readable set lines. These are useful when a request needs more than species, moves, and held item. Support varies by game and by what PKHeX can generate legally.

| Batch-editor input | Readable set field | Notes |
| --- | --- | --- |
| `.Scale=` | `Scale:` or `Size:` | Size keywords or a numeric value. |
| `.WeightScalar=` / `.HeightScalar=` | `Weight:` / `Height:` | Size keywords or numeric values. |
| `.OriginalTrainerFriendship=` / `.HandlingTrainerFriendship=` | `OT Friendship:` / `HT Friendship:` | Friendship values. |
| `.MetDate=` / `.MetLocation=` | `Met Date:` / `Met Location:` | Date and location, including supported numeric location IDs. |
| `.StatNature=` | `Stat Nature:` | Nature used for the Pokémon's effective stats. |
| `~=Version=` | `Game:` or `Version:` | Origin game name or abbreviation. |
| `.HyperTrainFlags=` / `.HT_[STAT]=` | `HyperTrain:` / `HT:` | Hyper Training flags by stat. |
| `.Moves=` / `.RelearnMoves=` | `Moves:` / `Relearn Moves:` | Move requests, including supported random/all options. |
| `.Ribbons=` / `.Ribbon[name]=True` | `Ribbons:` / `Ribbon:` | Ribbon requests. |
| `.RibbonMark[mark]=True` | `Mark:` | Mark name without spaces. |
| `.SetEVs=` / `.SetIVs=` / `.GV_[STAT]=` | `Set EVs:` / `Set IVs:` / `GVs:` | Stat presets or per-stat values where supported. |
| `.Marking[type]=` / `.Characteristic=` | `Markings:` / `Characteristic:` | Marking colors and characteristic text. |
| `.Nickname=` | `Nickname:` | Custom nickname or a supported suggestion. |
| `.MoveX_PP=` / `.MoveX_PPUps=` | `PPUps:` | PP-up settings for moves. |

For example, these lines can be included alongside a normal Pokémon set:

```text
Set EVs: Suggest
Set IVs: 5IV
Scale: XL
Met Location: 30024
Game: PLA
Moves: Random
Relearn Moves: All
GVs: 7 HP / 7 Atk / 7 Def / 7 SpA / 7 SpD / 7 Spe
```

The [batch normalizer](SysBot.Pokemon/Helpers/BatchCommandNormalizer.cs) contains the exact parsing rules. For location IDs, the existing [game-location reference](https://pastebin.com/NBu14c6q) can help when a name is not recognized.

### Characteristic reference

The characteristic shown by a Pokémon depends on its IVs and other internal values; requesting a characteristic is not the same as setting one arbitrary string. The following names are useful when constructing a set:

| Stat | Characteristic names |
| --- | --- |
| HP | Likes to eat · Takes plenty of siestas · Scatters things often · Likes to relax · Nods off a lot |
| Attack | Proud of its power · Likes to thrash about · A little quick tempered · Quick tempered · Likes to fight |
| Defense | Sturdy body · Capable of taking hits · Highly persistent · Good endurance · Good perseverance |
| Special Attack | Highly curious · Mischievous · Thoroughly cunning · Often lost in thought · Very finicky |
| Special Defense | Strong willed · Somewhat vain · Strongly defiant · Hates to lose · Somewhat stubborn |
| Speed | Likes to run · Alert to sounds · Impetuous and silly · Somewhat of a clown · Quick to flee |

## Local control panel and Trade Portal

When the web server is enabled, the local control panel uses `http://localhost:8080` by default. The Trade Portal can provide a browser-based way to interact with trading features. Read the [English setup guide](docs/Cloudflare-Tunnel-Trade-Portal.md) or [Spanish setup guide](docs/Cloudflare-Tunnel-Portal-de-Intercambios-ES.md) before exposing it beyond your own computer.

## Build from source

TradeDex targets .NET 10. On Windows, install the .NET 10 SDK, clone this repository, and run:

```powershell
dotnet test SysBot.Tests/SysBot.Tests.csproj
dotnet publish SysBot.Pokemon.WinForms/SysBot.Pokemon.WinForms.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The publish target names the output `TradeDex_<version>.exe` using the version in `SysBot.Pokemon/Helpers/TradeBot.cs`. The [release workflow](.github/workflows/release.yml) builds and attaches that EXE to a GitHub release when a new version is pushed to `main`.

## Project roots and credits

TradeDex builds on the work of the Pokémon bot and legality-tool communities, including [SysBot.NET](https://github.com/kwsch/SysBot.NET), [PokeBot](https://github.com/hexbyt3/PokeBot), [PKHeX](https://github.com/kwsch/PKHeX), and Auto Legality Mod. Thank you to their authors and contributors.

This repository is distributed under the [GNU Affero General Public License v3.0](LICENSE). The Pokémon name and related game assets belong to their respective owners.
