# GPT-6 Music Primer, Part 1B: Learning from Galois

Reference files: `Data/Galois/NN - <title>.rzr` (exact original notation), `Data/Notes/INDEX.md` / `index.csv` (analysis).
Grammar: see Part 1 (notes `cl`..`ch`, pauses in seconds, `-note`/`+note` instrument selectors, `+NNN` sound effects).

## The one setting that explains Galois

73 of 76 songs open with:

```
# Instrument: LapHarp
# Prefetch: True
# NewTimer: True
# Tempo: 0
```

With NewTimer + Tempo 0 the player waits **zero** milliseconds between items; with Prefetch, a pause written
after a note becomes the wait that follows it (SystemMusicPlayer.DynamicTimer). So:

- **Rhythm is written entirely as pauses.** `a 0.08` = play A, then wait 80 ms.
- **Notes written side by side sound together.** `-cl cl eh gh .10` is a four-note C major chord, then 100 ms.
- **The main instrument is the lap harp; selectors add two more voices:** `-x` = harp (one step left),
  `+x` = lute (one step right). Three timbres from one song: bass/pad on harp, melody on lap harp, accents on lute.
- **Sound effects are the drum kit.** `+943`, `+328`, `+305`, `+309`, `+81` recur thousands of times on beats.

The three older songs (Scarborough Fair, Douce Dame Jolie, Revecy Venir du Printemps) use Tempo 90 and are
paced line by line with `wait`.

## Three study pieces

### 1. Simple: 51 - Scarborough Fair (Tempo 90, single instrument, 403 notes)

```
# (Intro arpeggios)
say '[play cl 0.19 gh 0.19 as 0.19 a 0.19 fh 0.19 as 0.19 f 0.19 gh 0.19 as 0.19 a 0.19 fh 0.19 as 0.19'
wait 2400
```

- **Harmony by arpeggiation:** each bar starts on a bass note (`cl`, `ds`, `f`) and walks the chord tones above it
  (`gh as a fh`). Each harp sample rings about 0.4 s, so at 0.19 s spacing consecutive notes overlap and the ear
  hears the chord even though only one note starts at a time (see ENGINE.md: samples cannot sustain longer).
- **Steady grid:** every note is followed by the same `0.19`, giving even eighth notes; `wait 2400` paces each bar.
- One voice, one rhythm value: the clearest model of melody plus implied harmony.

### 2. Intermediate: 60 - Swamp Thing (Tempo 0, 232 notes, 144 selector notes, 74 drum hits)

```
say '[play -a +c 0.2 a 0.08 +a 0.2 a 0.08 g 0.08 -ah 0.08 +a 0.08 a 0.08 -ah 0.08 +a 0.08 c 0.08 dh 0.08 +a 0.2'
say '[play +314 0.05 -a +c 0.2 a 0.08 +a 0.2 a 0.08 g 0.08 -ah 0.02'
```

- **Instrument interleaving:** `-a +c` = harp A with lute C at the same instant (no pause between), then 0.2.
  The riff alternates lap harp (`a`, `g`), harp (`-ah`) and lute (`+a`), giving a call-and-response texture.
- **Two rhythm values:** a long `0.2` and a short `0.08` make the swung, bluesy groove.
- **Percussion pickup:** `+314 0.05` drops a sound effect 50 ms ahead of the bar.
- Short and repetitive: a good first look at three voices plus drums.

### 3. Advanced: 61 - The Boxer (S&G) (Tempo 0, 2,492 notes, 1,214 selector notes, 304 drum hits)

```
say '[play g .14 e .14 d .14 e .14 d .14 -as .14 e .14 d .14'
say '[play +943 -cl cl eh gh .10 c .14 -c e gh .12 g .14 -g d gh .12 e .14 -c g ah .12 c gh .13'
```

- **Fingerpicking pattern:** the opening lines are a travis-style picking figure on a steady `.14` grid,
  alternating bass on harp (`-as`, `-g`, `-cl`) with treble on lap harp.
- **Block chords on the beat:** `+943 -cl cl eh gh .10` = drum + harp bass + three lap-harp chord tones, together.
- **Micro-timing:** pauses vary `.10` to `.15` to humanise the strum; a bare pause (`.13 .15`) extends a beat.
- Full arrangement: bass, chords, melody, drums, in 168 play lines.

## How Galois creates harmony with "single notes"

1. **Stacking** (Tempo 0): write chord tones side by side; they start together.
2. **Arpeggiation** (any tempo): spread chord tones in time; with spacing under ~0.4 s the samples overlap and blend.
3. **Register split:** bass in the low octave (`cl`..`b`), harmony and melody higher (`c`..`ch`).
4. **Timbre split:** harp (`-x`) for bass and pads, lap harp for melody, lute (`+x`) for accents.
5. **Drums mark the downbeat:** a sound effect at the start of a group anchors the ear to the bar.

## Best first composition to study together

**51 - Scarborough Fair.** One instrument, one rhythm value, a clear bass-plus-arpeggio pattern, and comments
in the file marking sections. Next, **60 - Swamp Thing** adds selectors, two rhythm values and percussion in a
short, repetitive form.
