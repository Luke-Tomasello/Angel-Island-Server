# Engine Reference: What the Playback Engine Actually Does

Shared by both AI composers. **[V]** = verified in source or client data; **[L]** = needs a listening test.
Sources: `scripts/Engines/Music/` (`SystemMusicPlayer.cs`, `CoreMusicPlayer.cs`, `RazorInstrument.cs`),
client data `AI_Golden_Client/sound.mul` + `soundidx.mul`.

## Simultaneous notes (NewTimer, Prefetch, Tempo 0)

- [V] Each note is its own sound packet. With Tempo 0, the player schedules the next item with a 0 ms delay
  (`DynamicTimer`, `Timer.DelayCall(delay)`), so stacked notes go out on consecutive server timer ticks, not in one packet.
- [L] The real spacing is the server tick (expected a few ms); whether the ear hears a true chord, and how many
  overlapping sounds the client mixes before dropping some, must be heard. Galois stacks up to ~5 sounds.
- [V] With Prefetch, a pause right after a note becomes the wait after that note. Without Prefetch a note costs
  one Tempo step and a following pause adds its own wait. A negative pause (`-0.2`) is applied immediately even without Prefetch.
- [V] Instrument selectors and `vNN` cost no time (processed instantly, then the next item).

## Note samples and sustain

- [V] All 25 notes exist on all 3 instruments (`CoreMusicPlayer.NoteSounds`): harp 1175-1199, lap harp 970-994,
  lute 1021-1047 (`stand_harp_c_1.wav`, `lap_harp_c_1.wav`, `lute_c_1.wav` ...).
- [V] Samples are short: harp and lap harp ~0.4 s, lute ~0.3 s. **A note cannot sound longer than its sample.**
  A pause longer than ~0.4 s is silence, not a held note. Sustain must be written (repeat or roll the note/chord).
- [L] Tuning between instruments and the evenness of loudness across the range.

## Percussion (sound effects `+NNN`)

- [V] `+NNN` is the client sound index (decimal, 0-1487). Sheet-music playback allows effects; typing them on the
  `[play` command line needs 1000 music karma, and so does writing them in a music composition book.
- [V] Galois's kit, by use: `+328` hit20 (0.25 s), `+305` feet15a (0.43 s), `+309` hit01 (0.36 s), `+943` hit21 (0.31 s),
  `+81` switch01 (0.06 s, a click), `+295/296` feet08 (short), `+306/307/292` feet, `+935/938` hit14/hit17, `+314` hit06.
- [V] Families: combat hits `hit01-hit22` at 309-330 and `hit1-hit22` at 930-951; footsteps `feet*` at 292-307.
- [V] Real drums (`drum01` 56, 743-749, tambourine 82/1205-1207) are 1-6 s loops, not single hits: use with care.
- [L] Which hits sound like kick, snare, or rim; levels relative to the harps.

## Dynamics

- [V] `vNN` (and `# ConcertMode: true`) only work in ConcertMode. Volume 0-20 (20 loudest, default 20; higher values clip).
  It works by placing each sound 20 - v tiles east of each listener, so quieter = farther.
- [L] That offset also pans the sound; whether that is noticeable.
- [V] Without ConcertMode, sounds play at the source; listeners within the normal update range hear them, louder when closer.

## Length and queues

- [V] No limit on composition length or line length in the engine. The player's queue holds 1024 items; sheet music
  feeds it as room frees (`RazorInstrument.NotePumpTick`), so nothing is lost.
- [V] A composition book (in-game authoring) is limited by its 100 pages; `.rzr` files are not.

## Timing pitfalls

- [V] **One bad token drops a whole batch, not just a line.** Sheet music hands up to 1024 queued tokens to the player
  at once; any invalid token makes it print the usage message and discard the batch. Validate every token.
- [V] A batch whose first token is a configuration word (`instrument`, `tempo`, `newtimer`, `prefetch`, `volume` ...)
  is treated as configuration and not played. Keep configuration in `#` directives.
- [V] `[play` line boundaries do not matter musically: tokens are concatenated into one queue.
- [V] **But every line must be a complete statement.** The script is read one line at a time, and only lines starting
  with `#`, `say '[play`, `say` or `wait` are used (`RazorInstrument.cs:782-797`). A `[play` statement continued
  onto the next line is silently cut off: the continuation lines are ignored. Write `say '[play ...'` on every line.
  (Found in G-The-Iron-Comet.v01: only its first note played.)
- [V] `wait` only delays reading the next script line; it does not hold notes already queued. A `wait` longer than the
  queued music leaves a gap of silence. Galois's Tempo-0 songs use no `wait` at all.
- [V] With the classic timer (NewTimer false) pauses must be 0-1.0 or the batch is rejected; with NewTimer, any length.
- [V] Notes, selectors and sound tokens are case-insensitive (lowercased).
- [L] Retriggering the same note quickly (no pause) may cut the previous sample.

## Auditioning without publishing

- [V] Put the `.rzr` in `Shard.UP/data/song files/`. As a GM: `[add RolledUpSheetMusic`, `[props` it, set `SongFile`
  to the file name (the setter accepts only an existing `.rzr` there). Keep it in your backpack (required) and double-click.
  A blank RSM has no composition text, so it plays the file, with its `#` directives and sound effects.
- [V] **Fixed 10/23/26 (RazorInstrument.StartMusic re-reads the instrument after loading).** On servers without the
  fix, set the main instrument by hand first: `[props` the RSM, `SuccessSound = 69` (lap harp; 67 harp, 76 lute).
  A blank RSM is a harp, and when it plays a `SongFile` it captures its instrument before the file's
  `# Instrument:` directive is read (`RazorInstrument.cs:562-568`, reused at `:635-644`). Left as harp, every `-x`
  fails with "Error: bad instrument offset" (nothing is left of the harp) and is skipped, and every `+x` plays one
  instrument lower than written. Confirmed by Adam's audition of C-Copper-Tankard-Jig.v01. Published RSMs (made with
  manuscript paper) already carry their instrument and are not affected.
- [V] Everyone in range hears it. `[StopMusic` stops your playback; `[FilterMusic` toggles hearing music.
- [V] `[play <tokens>` tests a single line but uses your own player settings (set them first with `[play newtimer true`,
  `[play prefetch true`, `[play tempo 0`, `[play instrument lapharp`) and needs music karma for sound effects.
