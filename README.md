# Angel Island Server

The Ultima Online server behind the **Angel Island** shards, free and open source. It began in 2004 on RunUO and
has grown ever since: a 64-bit, .NET 8 server that runs Angel Island, Siege Perilous, Mortalis, Seasons, Upheaval and
an event shard, each from the same code, with a login server in front of them.

> **Looking for just the music system?** The Angel Island Music System (AIMS) is also published on its own, ready to
> drop into a RunUO server: **[Angel Island Music System](https://github.com/Luke-Tomasello/Angel-Island-Music-System)**.

## What you need

- **Windows** and the **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (Visual Studio 2022 is optional).
- **Ultima Online client files.** The server finds them through the registry, or asks for the folder the first time.
  Players connect with client **6.0.1.10**; other versions get a warning.

## Build

Open `GMN Core.sln` and build the **whole solution**, or from a command prompt in this folder:

```
dotnet build "GMN Core.sln" -c Release
```

`GMN Core.exe` is written to this folder, with the three patch libraries (`Patches001_100.dll`, `Patches101_200.dll`,
`Patches201_300.dll`) beside it. Build the whole solution, not just `GMN Core`: the server won't start without them.

## Run

Every shard is the same program, started with a flag. With no flag, it asks which one to run.

| Flag | Shard | Port |
|---|---|---|
| `-uols` | Login server: players connect here and pick a shard | 2593 |
| `-uoai` | Angel Island | 3593 |
| `-uosp` | Siege Perilous (`-Siege1`, `-Siege2` for its variants) | 3594 |
| `-uomo` | Mortalis | 3595 |
| `-uose` | Seasons (`-Season1`, `-Season2`, ... for a season) | 3596 |
| `-uoup` | Upheaval | 3597 |
| `-uomc` | The Angel Island Music Conservatory: a shard for musicians, on Upheaval's rules with Trammel (PvE) play | 3603 |
| `-uotc` | Test Center: add to a shard's flag to run it as its test center | 3602 |
| `-uoev` | Event shard: add to a shard's flag to run it as an event shard | 3601 |

The ports are in `Ports.json`. Other flags you may want:

| Flag | What it does |
|---|---|
| `-timezone=<zone>` | The shard's time zone, for events and scheduled jobs: an IANA name (`-timezone=America/Los_Angeles`) or `UTC`, `UTC-5`, `UTC+5:30`. Not given: the server machine's zone. An unknown zone stops startup |
| `-nopatch` | Skip the startup patcher (one-time patches for existing worlds) |
| `-nosaves` | Starts with saves off, for a development or test world you don't want to keep: no automatic saves, and a `[Maintenance` shutdown or restart doesn't save first. `[AutoSave on` turns saves back on (then both save again); a manual `[save` always saves |
| `-saves` | This launch saves, like a production shard. A Debug build must be given `-saves` or `-nosaves` (neither, or both, and the server won't start); in a Release build, which saves unless told `-nosaves`, it changes nothing |

The startup banner shows what's in effect: `[Saves are Enabled.]` or `[Saves are Disabled.]`, the time zone, and
where the databases are.

```
"GMN Core.exe" -uols                 the login server
"GMN Core.exe" -uoai                 Angel Island
"GMN Core.exe" -uosp -uotc           Siege Perilous Test Center
```

`-usedb` and `-sharemusic` are retired: accounts and music sharing are always on (see Databases). A server given one
stops at startup and says so.

## Databases

Accounts and passwords, character names, the shared music library and the firewall lists are SQLite databases, kept in
one folder:

- `AI.DBDIR` set (a Windows environment variable): that folder. It must exist and be writable, or the server stops.
- Not set: `Databases` in the shard's own folder, created at the first start. A shard on its own needs nothing more.

Shards that use the same folder share their accounts, character names and music. To run several shards as one family,
set `AI.DBDIR` to one folder for all of them. (The Angel Island shards use `C:\AIDB`: a machine that keeps databases
there must have `AI.DBDIR=C:\AIDB`, or its shards stop at startup rather than start on empty databases of their own.)

The databases live outside `Saves`, so back them up as well as your world. After restoring an older world, the
databases are kept as they are: they hold accounts, names and the music library, never the world's items, characters
or gold, so the newer copy is the right one. (The design note is in `Server/DatabaseFolder.cs`.)

To run several shards, give each its own folder (for example `ShardAI`, `ShardSP`, `ShardLS`), start the login server
and the shards, and connect your client to the login server on port 2593. To try one shard alone, connect to its own
port instead.

## The first start

- With no world yet, the server starts a new one: it creates `Saves\` and `Logs\` here, runs its one-time patches,
  and spawns the world. Messages about things a patch expected in an older world are normal on a new one.
- It offers to create the first account. That account is **Owner**, one level above Administrator.

## Optional settings

The server runs without any of these; each turns on a feature. They are Windows environment variables.

| Variables | For |
|---|---|
| `AI.NOREPLY.ADDRESS`, `AI.NOREPLY.PASSWORD`, `AI.EMAIL.HOST`, `AI.EMAIL.HOST.PORT`, `AI.EMAIL.USER`, `AI.EMAIL.ACCOUNTING`, `AI.EMAIL.ANNOUNCEMENTS`, `AI.EMAIL.DEVNOTIFY`, `AI.EMAIL.DISTLIST.PASSWORD`, `AI.EMAIL.SHARDOWNER` | Email: account notices, announcements, crash reports (see `scripts/Engines/email/smtp.cs`) |
| `AI.MMAccountId`, `AI.MMlicenseKey` | GeoIP lookups with your own MaxMind account (see `scripts/Accounting/AccountHandler.cs`) |
| `AI.DBDIR` | The database folder (see Databases). `AI.LOGINDB`, `AI.IPEXCEPTIONDB`, `AI.FIREWALLDB`, `AI.MUSICDB` and `AI.NAMESDB` are retired: a server that finds one set stops |

## The version

The server reports its version at startup: `Version 6.1.1360`. The first two numbers are set by hand in
`GMN Core.csproj`; the last is the SVN revision of the source, the number in this repository's tags (`svn-r1360`).
`Directory.Build.targets` explains how. A clone of this repository has no SVN to ask, so it builds as `6.1.0` (with a
warning) unless you name the revision: `dotnet build "GMN Core.sln" -p:AppRevision=1360`.

## World files

A new server starts with a new world. To start from ours instead, the fully spawned worlds are in their own
repositories: copy a world repository's `Saves` folder into your shard's folder (beside `GMN Core.exe`) before its
first start.

- [Angel Island World Files](https://github.com/Luke-Tomasello/Angel-Island-World-files): Angel Island as of July 2024,
  a customized OSI Publish 15 spawn.
- [Siege Perilous World Files](https://github.com/Luke-Tomasello/Siege-Perilous-World-Files): Siege Perilous as of July
  2024, a faithful OSI Publish 13.6 spawn.

Both include the Custom Housing Preview area, where players can see and buy any of our 56 custom houses (you don't want
to build that by hand), every township, left intact so your staff can see what players built, and the music library:
the players' compositions for the music system, about a hundred songs. The server brings an older save up to date as it
loads it (the startup patches).

## Clients and related repositories

The server can check that players use an approved client configuration, through a handshake with Angel Island's
versions of ClassicUO and RazorCE:

- [ClassicUO, Angel Island version](https://github.com/Luke-Tomasello/ClassicUO-Angel-Island-Version)
- [RazorCE, Angel Island version](https://github.com/Luke-Tomasello/RazorCE-Angel-Island-Version)
- [Common Shard Launcher](https://github.com/Luke-Tomasello/Common-Shard-Launcher): the launcher players use for Angel
  Island and Siege Perilous
- [Client map and music files](https://github.com/Luke-Tomasello/client-server-map-and-music-files): the map files, and
  the original Ultima Online music as MP3

## The music system

Angel Island players compose and perform real music on the game's own instruments: they write songs in composition
books, turn them into sheet music anyone can play, and publish them to music boxes, where other players listen and buy
them. The system is in `scripts/Engines/AIMusicSystem`, with the story behind it in its
[Why I Built AIMS](scripts/Engines/AIMusicSystem/Docs/WHY.md). **MidiToAims**, a composer's tool that turns MIDI files into
songs, is in `AI Music Composer/`, along with the shards' music library, shared with its composers' permission
(`AI Music Composer/Data`, one folder per composer): songs by Galois, Angel Island's Grandmaster Composer, Jocelyn,
Bushwick Bill, Pastor Wydstrin, Bravado, Loser Girl, Willibald, Xavier, Lady Runa and a sheep.

How players use it is on the Angel Island wiki:
[Music Composition](https://game-master.net/aiwiki/index.php/Music_Composition),
[Sheet Music](https://game-master.net/aiwiki/index.php/Sheet_Music),
[Music Box](https://game-master.net/aiwiki/index.php/Music_Box),
[Musicianship](https://game-master.net/aiwiki/index.php/Musicianship),
[Sound List](https://game-master.net/aiwiki/index.php/Sound_List) and
[MidiToAIMS](https://game-master.net/aiwiki/index.php/MidiToAIMS). The wiki pages' sources are in
`AI Music Composer/Docs`.

## See it

- YouTube: [Ultima Online Free Shard](https://www.youtube.com/@UltimaOnlineFreeShard)
- Getting started: [part 1](https://youtu.be/0kauX-W5BLI), [part 2](https://youtu.be/8zNv5ryd6_U)
- The 2022 [Battle of the Bards](https://www.youtube.com/watch?v=GCqLpk5YO1c)
- Discord: [discord.gg/GXcR2DsSnb](https://discord.gg/GXcR2DsSnb)

## License and credits

GPL (see `LICENSE`). Angel Island is built on RunUO, by the RunUO Software Team, and on twenty years of work by Angel
Island's developers, staff and players. The source files' changelogs tell that story, name by name.
