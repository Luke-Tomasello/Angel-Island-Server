MidiToAims - turn a MIDI file into music for Angel Island
=========================================================

MidiToAims turns a MIDI file into an AIMS composition that you paste into a
music composition book in the game. It does the transcription; you make the
musical decisions. It can also play your music on this computer, the way the
game will. No installation needed: everything is in this folder.

The first time you run it, Windows may say "Windows protected your PC".
Click "More info", then "Run anyway".


Your first song (about five minutes)
------------------------------------
This folder includes "Minuet in A minor.mid" (J. S. Bach, public domain), so
you can try everything before using your own music.

1. Drag "Minuet in A minor.mid" onto MidiToAims.exe.
   "Minuet in A minor.rzr" appears next to it: that is your composition.

2. Open the .rzr in Notepad and copy all of the text (Ctrl+A, Ctrl+C).

3. In the game, put a blank music composition book in your backpack,
   right-click it and choose Paste. Click Post Message, click in the message
   box, paste (Ctrl+V) and click Post. The whole composition goes into the
   book at once, line by line.

4. Listen: type [play preview, or choose Start from the book's context menu
   (Stop ends it). Edit the book as you like and preview again.

5. When you're happy, use manuscript paper on the book to make sheet music,
   then submit it to a Music Box.


The m2a> prompt
---------------
Double-click MidiToAims.exe. It opens a window with a prompt, where you type
commands:

    m2a> [play preview minuet
    m2a> [play c e g 0.5 c e g

Press the up arrow to bring back an earlier line, change it, and press Enter
to run it again. "help" lists the commands; "exit" closes the window.

Every command also works from a command prompt (type cmd in the folder's
address bar and press Enter), with MidiToAims in front:

    MidiToAims [play preview minuet

A song can be named by any words from its file name: "minuet" finds
"Minuet in A minor". If several songs match, MidiToAims lists them: type
more of the name.


Making it yours: the arrangement settings
-----------------------------------------
    m2a> init minuet

This writes "Minuet in A minor.m2a", the arrangement settings. Open it in
Notepad and set "Arranged by" to your character's name - that is the name
the Music Box shows. Change anything else you like (which track is the
melody, instruments, octaves), then convert again:

    m2a> minuet

Edit the .m2a and convert as often as you like. Each conversion prints a
report: notes "dropped" (too many at once) or "folded" (moved by an octave to
fit AIMS's two octaves), and the bars where that happened.

The game won't play the same note on the same instrument twice within about
0.2 seconds: a fast repeated note would lose every other repeat. So
MidiToAims moves those repeats to the other harp, and every one plays; the
report says how many. "Repeated notes: off" in the .m2a turns this off.

When a chord has more notes than "Notes at once", MidiToAims keeps the
melody, the bass, and then the notes nearest the melody (Inner notes:
highest). "Inner notes: loudest" keeps the notes the MIDI plays loudest
instead, following the original arrangement's balance; often that makes
the chords sit lower and sound fuller. Try both and listen.

A MIDI's drums play too ("Drums: off" in the .m2a leaves them out): each
drum hit is a percussion sound effect, free for everyone - kick +328, snare
+309, hi-hat +305 and cymbal +943, Galois's kit. Change any of them in the
.m2a. A drum written as an ordinary track of notes (a track named
"BassDrum", say) plays as harp notes; give it "Role: kick" (or snare,
hi-hat...) and it plays as that drum. The .m2a points out tracks named like
drums. We're still looking for a good cymbal and good toms: if you find
percussion sounds that work, tell Adam.


Hear it before you paste it
---------------------------
MidiToAims plays a composition the way the game will, with the harp, lap
harp and lute of your own Ultima Online client:

    m2a> [play preview minuet

If the song has a .rzr, that's what plays: it's what you'll paste, with any
edits you made in it. To convert the .mid again first (after changing the
.m2a), add .mid:

    m2a> [play preview minuet.mid

Converting again replaces the .rzr, including any edits you made in it.
For a song in pieces, the pieces play in order.

While it plays, a line shows the time in the song and the line of the .rzr
playing, so when something sounds wrong you know where to look:

      1:23.4 / 3:12.5   book1.post2 line 140

Ctrl+C stops, and says where. To play just part of a song, give the times:

    m2a> [play preview minuet --from 0:20 --to 0:35

(--from alone plays to the end, --to alone from the start; 1:10, 1:10.5 and
70 are all times.) It plays the part as it sounds in the whole song, then
says the line where it ended, so you know where the next part starts.

Found a passage worth keeping, to show a friend or teach with? Add -clip
(press the up arrow, add it, press Enter):

    m2a> [play preview minuet --from 0:20 --to 0:35 -clip

That writes "Minuet in A minor.clip1.rzr" (then clip2 ...): the passage as
a song of its own, with whole phrases and the song's settings, ready to
preview or paste. Hear it with [play preview minuet clip1. ("minuet" alone is
still the song.)

It also tells you what the game will do differently from what is written:
for example, the game skips the same note on the same instrument played
again within about 0.2 seconds, so you'll hear those notes missing here too.
Add --check to see the details, line by line, without playing. Add --debug
to any command (or type "debug on" at the prompt) to see everything
MidiToAims does, such as which files it found and plays.

Each wait line should be exactly as long as the [play lines above it take
to play. When you edit a phrase, its wait may no longer fit: too long leaves
a silence, too short makes the lyrics come early. The preview says so, and
this fixes them, changing nothing else (the old files are kept as .rzr.bak):

    m2a> waits minuet

To keep a rest after a phrase, end its [play line with a pause instead. This
is for songs on the Anchored timer, as MidiToAims writes them; waits for the
Dynamic timer are set by ear, and MidiToAims leaves them alone.

It finds your client by itself (the Angel Island launcher's). If it can't,
tell it once where the client is, and it remembers:

    m2a> [play preview minuet --uo "C:\...\UO Angel Island"


Try a phrase
------------
Type [play just as in the game, and hear it at once:

    m2a> [play c e g 0.5 c e g
    m2a> [play instrument lute
    m2a> [play tempo 150
    m2a> [play timer anchored
    m2a> [play config          shows the settings
    m2a> [play reset           back to harp, Classic timer, tempo 100

As in the game, a setting stays until you change it (MidiToAims keeps it in
MidiToAims.ini), so set the instrument or tempo once, then try phrase after
phrase. A song you preview uses its own # directives, not these settings.
A line plays to its end (or Ctrl+C) before the prompt comes back. The [ is
optional: "play c e g" works too.


Sound effects
-------------
A song can use the game's sound effects: +81 in a [play line plays sound
81. To hear them and find their numbers:

    m2a> sound 81            one sound
    m2a> sound 300-330       a range, one after another
    m2a> sound wood          every sound with "wood" in its name
    m2a> sound percussion    the sounds anyone may use

Add --list to see the numbers and names without playing them. In the game,
most sound effects need 1000 music karma to use in a composition; the
percussion sounds (clicks, knocks, thuds, swishes, the anvil, wind chimes)
are free for everyone. Each sound listed says which it is. Here, every sound
plays, whatever your karma.


Long songs: several posts, several books
----------------------------------------
One post takes up to 250 lines, so a long song comes in pieces:

    "Song.post1.rzr", "Song.post2.rzr" ...

Post them into the same book in order (Paste, Post Message, Ctrl+V, Post):
each one goes in after the last. If a post won't fit in the book, nothing is
written and the book says so.

A song too long for one book comes in several books: "Song.book1.rzr",
"Song.book2.rzr" ... (or "Song.book1.post1.rzr" ... when a book needs more
than one post). Use a new book for each book number, and keep the books
together in your backpack or one bag.

If an older conversion left other .rzr files for the song, MidiToAims says
so: don't paste those.

The Format setting in the .m2a (or --format) chooses the files:

    Format: post     files for the book's Paste (the default)
    Format: whole    the song in one file, to read, share or keep


Credit where it's due
---------------------
Arranging a piece doesn't make you its composer: put the original composer in
"Composer" and yourself in "Arranged by". Use music you have the right to
arrange - your own, or public domain. The Mutopia Project
(https://www.mutopiaproject.org) has a large free library; the minuet in this
folder comes from it.

The full guide is the MidiToAIMS page on the Angel Island wiki.
