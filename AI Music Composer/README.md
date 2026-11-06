# AI Music Composer

Development-only workspace for teaching AI composers to write original music for Angel Island's
rolled-up sheet music (RSM). Nothing here is part of the shard build or deployed to a server.

Composers: **ChatGPT 6** (GPT-6) and **Claude Opus 5.5** (Mikey).

## Layout

| Path | Contents |
|---|---|
| `Data/Galois/` | The 76 Galois library compositions (`.rzr`), exact original notation from the Upheaval world save. Do not edit. |
| `Data/Galois/Variants/` | 7 variant texts found only on player copies. |
| `Data/Galois/Extraction/` | Read-only extraction scripts (world save snapshot -> `.rzr`). |
| `Data/Notes/` | `GUIDE.md` (teaching guide), `INDEX.md` / `index.csv` (per-song analysis). |
| `Data/Examples/` | Small study examples. |
| `Composer/` | Standalone C# composer tool (skeleton; no shard dependencies). |
| `Output/` | Generated compositions and scratch output. Not under source control. |

## Notation (RSM / `.rzr` script)

A composition is plain text, one command per line (`RazorInstrument.BabyRazor`):

- `# Key: value` directives, read before playback: `Instrument:` harp | lapharp | lute, `NewTimer:` true/false,
  `Prefetch:` true/false, `Tempo:` ms per step (default 100), `ConcertMode:`, `Song:`, `Author:`.
- `say '[play <tokens>'` queues music. `say '<text>'` shows text (lyrics). `wait <ms>` paces the script.

Tokens in a `[play` line (`SystemMusicPlayer.Player`); one bad token rejects the whole line:

- Notes, low C to high C (25 semitones): `cl csl d ds e f fs g gs a as b c cs dh dsh eh fh fsh gh gsh ah ash bh ch`
  (`s` = sharp; no flats; `c` = middle C).
- Pause: a number in seconds (`0.2`). Classic timer accepts 0 to 1.0 only.
- Instrument selector: `-x` / `--x` = one / two instruments left, `+x` / `++x` = right, in the order
  harp < lapharp < lute (with lapharp main: `-x` harp, `+x` lute).
- Sound effect: `+NNN` (0-0x5CF). Volume: `vNN` (concert mode only). Avoid macros (`%name%`): they are per player.

## Playback techniques

- **Classic timer** (default): one item per 0.1 s; a pause holds the queue. Put a pause between repeated notes.
- **Galois setup** (73 of 76 songs): `NewTimer: True`, `Prefetch: True`, `Tempo: 0`, `Instrument: LapHarp`.
  No automatic gap: rhythm is written entirely as pauses, and notes written side by side sound together (chords).
- Harmony: stack chord tones (Tempo 0), or arpeggiate them (samples last ~0.4 s, so close spacing overlaps).
- Voices: harp `-x` for bass/pads, lap harp for melody, lute `+x` for accents; sound effects as drums
  (`+943`, `+328`, `+305`, `+309`, `+81`).

See `Data/Notes/GUIDE.md` for worked examples (Scarborough Fair, Swamp Thing, The Boxer), and
`Data/Notes/ENGINE.md` for verified engine behavior, limits, the percussion map and how to audition a song.

## Composition workflow

1. **Study**: pick a reference from `Data/Galois` (start with *51 - Scarborough Fair*), note its rhythm grid,
   voicing and instrument use.
2. **Compose**: write a new `.rzr` into `Output/` with full directives, `Author:` set to the composer's name
   (ChatGPT 6 or Claude Opus 5.5), and a `Song:` title. Original music only.
3. **Validate**: every token must be a valid note, pause, selector, sound or volume; classic-timer pauses stay within 0-1.0.
4. **Listen**: on Test Center, a blank RSM with `SongFile` set (see ENGINE.md, "Auditioning"), or `[play` for one line.
5. **Critique and revise**: note what worked (melody, rhythm, harmony, balance), revise, repeat.
6. **Promote**: a composition worth keeping moves from `Output/` to `Data/Examples/` by agreement.

The `Composer` tool will later automate validation and generation; for now it is a skeleton.
