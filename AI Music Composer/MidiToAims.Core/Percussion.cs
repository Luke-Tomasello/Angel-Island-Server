/***************************************************************************
 *
 *  MidiToAims.Core - Percussion: the sound effects any composer may use in a song, whatever their music karma
 *  (+81 in a [play line). Every other sound effect needs 1000 music karma to make sheet music from.
 *
 *  A copy, because Core has no shard dependencies; it is a dependency on the shard. Change them together:
 *      Sounds              SystemMusicPlayer.PercussionSounds (Scripts\Engines\AIMusicSystem\Core): the sounds the
 *                          shard lets through its karma checks ([play, ManuscriptPaper.CheckValidKarma)
 *  [m2a compares them with the shard's each time it loads this assembly, and refuses when they differ.
 *
 *  Chosen by ear (Adam, 11/2/26) from the client's short sounds: clicks, knocks, thuds, swishes, and two long ones (the
 *  anvil, wind chimes). With (11/3/26) 305 and 943, two of Galois's most used drum sounds: the default drum kit
 *  (Converter's Drums) is Galois's, found in his 76 songs (328 kick, 309 snare, 305 hi-hat, 943 accent).
 *
 ***************************************************************************/

namespace MidiToAims
{
    public static class Percussion
    {
        public static readonly int[] Sounds =
        {
            42,     // anvil
            51,     // dropgem2
            66,     // hamer01
            73,     // lever01
            81,     // switch01
            295,    // feet08a
            296,    // feet08b
            304,    // feet14b
            305,    // feet15a (Galois's hi-hat)
            309,    // hit01
            310,    // hit02
            328,    // hit20
            563,    // blunt01
            569,    // swish02
            737,    // dropgem1
            835,    // feet_wood_a
            836,    // feet_wood_b
            931,    // hit10
            937,    // hit16
            942,    // hit20 (b)
            943,    // hit21 (Galois's second downbeat)
            944,    // hit22
            946,    // hit4
            1285,   // windchimes_01
            1334,   // weaponhit_04
            1336,   // weaponmiss_01
            1337,   // weaponmiss_02
            1338,   // weaponmiss_03
        };

        public static bool IsPercussion(int id) => Array.IndexOf(Sounds, id) >= 0;
    }
}
