# AIMusicSystem

*[Why I Built AIMS](WHY.md): why this exists, and whose work it is.*

Angel Island's music system: playback engine, sheet music, authoring, the music box library and music devices.
Consolidated here in Phase 2 of the modernization (physical relocation only: namespaces, type names,
serialization, save-file locations, commands and behavior are unchanged). Since November 2026 the same folder also
runs on a stock RunUO server: see [Running on another RunUO server](#running-on-another-runuo-server).

## History

The Angel Island shards began in 2004 on RunUO, with the game's instruments and bard skills. Rhiannon wrote the first
music player in July 2006, for simple tunes played note by note, with fixes by Pix and Adam. On January 4, 2022, Adam
rewrote it into a real music player: no instrument to pick, the player configured with [play (instrument, tempo,
prefetch), instrument changes within a song, sound effects (+NNN), and the Dynamic timer. Through 2022 he built most of
what players use today on that foundation: Razor scripts, composition books, manuscript paper and sheet music, Music
Boxes, the recorder, macros, volume and concert mode; more followed in 2023-2024.

The 2026 modernization was designed and directed by Adam, who guided and reviewed every step. AI assistants did much of
the hands-on work: writing code to his specifications, researching the game client and engine, and testing each
change. Each source file's changelog sums up its 2026 work in one entry; the earlier entries are as they were written.

## Layers

**Clients depend on the engine; the engine should not depend on its clients.** (The direction we are moving in;
the known exceptions are listed below.)

| Folder | Layer | Contents |
|---|---|---|
| `Core/` | Engine | `SystemMusicPlayer` (sessions `MusicContext`, parser, scheduler, `[play`/`[StopMusic`/`[FilterMusic`, `MusicContexts.bin`), `CoreMusicPlayer` (note table, sound packets), `RazorInstrument` (playback device and script runner), `MusicEngineMetrics` (timing and load measurements: note lateness, a main-loop reference probe, drift per run, packets; `Logs/MusicEngine.log`, `[MusicMetrics report [label]`) |
| `Compiler/` | Engine | `MusicCompiler` (a book's sections, plays and repeats expanded when it becomes sheet music; `[play preview`) |
| `Host/` | Host | `AimsHost` (what AIMS needs from the shard it runs on, each with a stock default), `AimsLog`, `AimsUtil` (the stable hash and other helpers), `AimsPlayerData` (Music Karma and the music filter, when the shard doesn't keep them) |
| `RSM/` | Integration | `RolledUpSheetMusic` (RSM), `ManuscriptPaper` + `MusicCompositionBook` (authoring into RSM), `CalypsoMusicPlayer` (device orchestrator used by items) |
| `Library/` | Integration | `MusicLibrary` (the global music library: every published track and its record, `Music.bin`, the review queue, `FindSong(title)`, `[DupeMusic`, composers: ownership, music karma, publishing points, sales) |
| `MusicBox/` | Client | `MusicBox` (the music box item: speech commands (a command table), playlists, publishing and review, royalties, sale notices) |
| `Items/` | Clients | `MusicController` (trigger device; also plays the client's UO soundtrack), `MusicMotionController` |
| `Instruments/` | Extensions | `AwardInstrument`, legacy `AwardHarp`/`AwardLapHarp`/`AwardLute` (load-only) |
| `Tools/` | Tools | `MusicRecorder` (`[MusicRecorder start|stop`: a performance, its lines and their timing, written into a composition book), `MidiToAimsCommand` (`[m2a`: a MIDI to composition books, with MidiToAims.Core), `BeatMonkey` (experimental harness), `MusicLoadTest` (`[MusicLoadTest`: music versus AI load test, alternating music-off and music-on phases, measured in `Logs/MusicEngine.log`) |
| `Docs/` | | This file |

Notation, timing and verified engine behavior: `AI Music Composer/Data/Notes/ENGINE.md`.

## Outside this folder (by design)

- Standard instruments (`Harp`, `LapHarp`, `Lute`, `BaseInstrument`) remain stock.
- Consumers with their own gameplay: `RedBeard` (champion spawn), `HireMinstrel` (hirelings), trophy addons
  (`BaseAddon`, `BaseAddonDeed`, `TaxidermyKit`), `PlaySoundEffect` (shared sound-effect utility).
- Hooks: `AngelIslandAimsHost` (`Scripts/Engines/AIMusicHost`: Angel Island's `AimsHost`), `PlayerMobile` (`FilterMusic`,
  guild points), `CharacterCreation` (`MusicLibrary.ReconnectPlayer`), `Main.cs` ruleset gating,
  `CronTasks.CMusicConfigCleanup`, `Utility.MakeTempMobile`, Nuke, patches.
- One-time music migrations live with their patches in `Patches201_300/`: `MusicOwnershipReconciler` (PatchV252),
  `LegacyRsmRepair` (PatchV255), `MusicOrphanCleanup` (PatchV256), `RemasterOrphanCleanup` (PatchV262).

## Compatibility notes

- Persisted type names (in world saves): `Server.Items.RazorInstrument`, `Server.Misc.RolledUpSheetMusic`,
  `Server.Items.ManuscriptPaper`, `Server.Misc.MusicCompositionBook`, `Server.Items.MusicBox`,
  `Server.Items.Triggers.MusicController`, `Server.Items.MusicMotionController`, `Server.Items.AwardInstrument`,
  `Server.Items.AwardHarp` / `AwardLapHarp` / `AwardLute`. Changing a namespace needs `[TypeAlias]`, and
  `ManuscriptPaper` / `MusicBox` are also named in `data/vendor pricing.txt` and `data/objects.xml`.
- Side files: `Saves/Music.bin` (library), `Saves/MusicContexts.bin` (sessions), `Saves/AIMS/Players.bin` (Music Karma
  and the music filter, only on a shard whose host doesn't keep them; never written on Angel Island).

## Running on another RunUO server

AIMS provides the vehicle; the shard provides the destination. Everything AIMS needs from a shard goes through
`AimsHost` (`Host/AimsHost.cs`): logging and log files, the Data and Logs folders, account identity, households, Music
Karma and the music filter, paying for a song, copies of sheets, internal storage, the sale event (and whether a
leaderboard shows it), and the shard's settings (Test Center, `[play` access). Each member has a default that works on
stock RunUO 2.6. A shard wires its own systems by deriving a class from `AimsHost` anywhere in its scripts; AIMS finds
it. Angel Island's is `AngelIslandAimsHost` (`Scripts/Engines/AIMusicHost`), outside this folder, and reproduces what
AIMS did here before it was portable.

- Where Angel Island's core differs from stock RunUO (a signature, a type, a system stock doesn't have), the code is
  under `#if ANGEL_ISLAND`, defined in `GMN Core.csproj`. Without the symbol, AIMS builds for stock RunUO.
- The drop-in: `Compiler/`, `Core/`, `Host/`, `Library/`, `MusicBox/`, `RSM/`, `Tools/MusicRecorder.cs` and `Docs/`.
  Angel Island extras, not part of it: `Items/`, `Instruments/` and the other `Tools/`.
- Outside the folder, a stock server needs: `BaseInstrument` (`ConsumeUse` virtual; a `RazorInstrument` is not picked
  as an instrument), a vendor for manuscript paper, composition books and music boxes (`SBBard`), and, for a book's
  Paste, `BulletinBoards` routing a post to the book. .NET 8.
- The test bed is RunUO.Model: stock RunUO 2.6 on .NET 8, with AIMS dropped in. Its client files are from before High
  Seas (7.0.9.0), so `MapDefinitions` sets `MultiComponentList.PostHSFormat = false`; a mismatch misreads every house.

## Known engine-to-client dependencies (Phase 3)

- (Resolved 10/29/26: `SystemMusicPlayer` reads music karma from `MusicLibrary.GetMusicKarma`; it read it from the music box.)
- `RazorInstrument.WarnPlaybackConflicts` names client types (`BaseAddon`, `TrophyAddon`, `MusicMotionController`).
- The session table (`MusicConfig`) is keyed by `Mobile` for players and NPCs; devices use their own unregistered
  sessions (Phase 4 Step 2b; before, they created temporary mobiles). Sound packets are built directly
  (`CoreMusicPlayer.PlaySound`).

## Session lifecycle

A `MusicContext` holds no world items: its instrument is an `InstrumentType` (harp, lap harp, lute), which selects the
note sounds. (Until Phase 4 Step 2a each context created three virtual `RazorInstrument` items in internal storage;
`MusicContexts.bin` context version 4 stores none, and loading an older context deletes the three it referenced while
keeping all its settings. An older build cannot read version 4.) `SystemMusicPlayer.ReleaseContext(Mobile)` is the one
way a context leaves `MusicConfig`. `[play reset`, the invalid-context rebuild and the hourly `CMusicConfigCleanup`
(contexts of deleted mobiles) all use it.

Device sessions: devices that play through `CalypsoMusicPlayer` (music controllers, addons, the taxidermy kit) play each
performance on their own session: a `MusicContext` with no mobile, never in `MusicConfig` (never saved, never seen by
cron cleanup), created by `RazorInstrument.StartDeviceMusic` via `SystemMusicPlayer.ConfigureDeviceSession` and
released when the performance ends (`CalypsoMusicPlayer.EndMusic`: completed, cancelled, out of range, or nothing to
play). No temporary mobile is created (Phase 4 Step 2b). The engine works on the session (`PlaySession`, the timers,
`PlayNote`/`PlaySound`); the Mobile forms remain for players, NPCs, MusicBox and commands. `SystemMusicPlayer.IsCurrent`
decides whether a session still plays: a mobile's context while it is in `MusicConfig`, a device session until released.
The device play lock (`NextMusicTime`, stopped while a performance runs) and `CancelPerformance` live in
`CalypsoMusicPlayer` too. A device is `Playing` for its whole performance, pauses included, so a double-click in a pause
cancels it. A cancelled device stops playing at once and unlocks when its performance completes (after the drain); a
completion of an earlier performance never touches a newer one.

Performances: `RazorInstrument` keeps everything that belongs to one execution of a song in a `Performance` (note queue,
script buffer, playing and paused state, its session, the user check, and the `AllDone` and `CheckSourceStatus` set when
it started); each start begins a new one and the instrument plays its current one. A `MusicContext` is the session and
its configuration; a session may run many performances (Phase 4 Step 3a).

The performance contract (Phase 4 Step 3b): `StartMusic` / `StartDeviceMusic` return the `Performance` (null if none
started). It has a `State` (Playing, Paused, Ending, Completed), `Cancel()`, and `Completed(performance, reason)`, raised
exactly once at its real end, with the reason: Finished, Cancelled, StatusFailed, SessionLost or NothingToPlay. A cancel or
a failed status check stops it at once (state Ending) and `Completed` follows after the drain of its last notes, about
half a second later; `AllDone` is raised with it. Completed is never raised during the start. All three orchestration
styles use it: Calypso devices keep the `Performance` and unlock on its completion; `MusicBox` subscribes to each song's
`Completed` (and forgets a song it stops itself; its temporary copies are unchanged); a direct double-click on sheet music
toggles `CancelPlayback` / `StartMusic`. The engine-only path (`[play`, `PlaySoundEffect`) is not a performance.

One performance per performer: a mobile's context plays one performance at a time, from its start until it completes
(`RazorInstrument.PerformanceOf`), including the silent moments when a script's `wait` outlasts its notes; a performance
whose context was released or replaced no longer counts (it is over, though it completes at its next tick). Another sheet
is refused meanwhile, typed `[play` may only look (`config`, `list`, `inspect`, `musickarma`) or `reset` (which ends the
performance), and `[StopMusic` ends the performance as double-clicking the sheet does. `[FilterMusic` is the listener's
mute for other players' music.

Timer modes (Phase 4 Step 4c): a session's `TimerMode` is Classic (one note per 100 ms tick), Dynamic (each note's own
delay: tempo, prefetch, pauses; scheduled from when the previous note ran, so timer lateness accumulates, about 10% slow)
or Anchored (Dynamic's delays, scheduled from when the previous note was due). `NewTimer` is the legacy form (True:
Dynamic, False: Classic). A song selects its mode with `# Timer: Classic|Dynamic|Anchored`, which takes precedence over
`# NewTimer:`; players use `[play timer <mode>` (stored in `MusicContexts.bin` context version 5). Anchored rules: a note
more than `SystemMusicPlayer.ReanchorMs` (100 ms) late shifts the rest of the schedule once (an interruption: a world save,
a long GC); smaller lateness is absorbed; notes are never rushed. The schedule lives in the session (`AnchorNext`), so a
script block that arrives within 100 ms of where the previous one ran out continues it. An Anchored song's `wait` lines are
anchored the same way (the script's own schedule), so the notes realign with the script at every block. Classic and
Dynamic are unchanged. `[MusicMetrics report` shows anchored run drift and re-anchors separately.

Session identity: the context object itself identifies a performance. The device (`RazorInstrument`) and the engine
timers (`ClassicTimer`, `DynamicTimer`) remember the context they started with and compare it by reference
(`ReferenceEquals`; `MusicContext` is a record). If it has been replaced (`[play reset` during playback) or released,
the performance ends through the device's normal `AllDone` notification, once, and the replacement context is
never touched.

History: before Phase 3 Stage 1 (2026-10-24), `CMusicConfigCleanup` removed contexts without `Finish()`, so every
temporary-mobile performance left three items behind. The Upheaval save of 2026-10-23 held 33,033 `RazorInstrument`
items, of which 33,006 are orphaned virtual instruments (11,002 harp/lapharp/lute triplets). Existing orphans are not
removed automatically. `GetMetaData` leaked a set per call until Phase 3 Stage 2. PatchV256 (`Patches201_300/MusicOrphanCleanup.cs`)
deletes the orphaned sets once: only complete, unreferenced sets, after a reference scan of the world and static fields.
