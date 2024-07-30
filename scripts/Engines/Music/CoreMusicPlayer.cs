/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* Scripts\Engines\Music\CoreMusicPlayer.cs
 * Changelog
 *  2/26/22, Adam
 *      Add support for volume
 *      Move PlaySound here from Mobile
 *  2/23/22, Adam (int.TryParse)
 *      Replace int.Parse with int.TryParse
 *  2/7/22, Adam
 *      Check for illegal sound ids
 *  2/4/22, Adam
 *      Add meta-note for sound effects
 *          +NNN
 *      remove 'pick instrument' logic
 *  1/21/22, Adam (GetInstrumentType)
 *      Add provisions for RazorInstrument
 *  1/2/21, Adam (PlayNote)
 *      PlayNote now accepts BaseInstrument and checkMusicianship as configuration parameters.
 *  12/13/21, Adam (Music System)
 *      Map Award instruments to the appropriate sounds. 
 *  12/10/21, Adam (Music System)
 *      Move the 'play music' system from PlayerMobile down to Mobile. 
 *      We therefore replaces most all instances of PlayerMobile with Mobile.
 *      This was done so that NPC can eventually 'play music'.
 *	3/22/08, Adam
 *		Have exception handler in PlayNote() log the exception.
 *	12/05/06, Pix
 *		Added null sanity checks, array bounds checking, and a try/catch block to PlayNote()
 *  08/20/06, Rhiannon
 *		Changed the way PlaySound is called to use new argument list in Mobile.PlaySound().
 *	08/05/06, Rhiannon
 *		Initial creation.
 */
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Misc
{
    public enum NoteValue
    {
        c_low,
        c_sharp_low,
        d,
        d_sharp,
        e,
        f,
        f_sharp,
        g,
        g_sharp,
        a,
        a_sharp,
        b,
        c,
        c_sharp,
        d_high,
        d_sharp_high,
        e_high,
        f_high,
        f_sharp_high,
        g_high,
        g_sharp_high,
        a_high,
        a_sharp_high,
        b_high,
        c_high
    }

    public enum InstrumentType
    {
        Harp,
        LapHarp,
        Lute
    }

    public class CoreMusicPlayer
    {
        public static void PlayNote(Mobile from, string note, BaseInstrument instrument)
        {
            try
            {
                //Pix: added sanity checks
                if (from == null || instrument == null) return;

                int it = (int)GetInstrumentType(instrument);
                //Utility.ConsoleOut("instrument {0}", ConsoleColor.Red, ((InstrumentType)it).ToString());
                int nv;

                if (note[0] == '+')
                {
                    int sound = 0;
                    if (int.TryParse(note.Substring(1), out sound))
                    {
                        if (sound >= 0x00 && sound <= 0x5CF)
                            CoreMusicPlayer.PlaySound(from, sound, SystemMusicPlayer.MusicConfig[from].Volume, SystemMusicPlayer.MusicConfig[from].ConcertMode);
                        else
                            from.SendMessage("Error: illegal sound id.");
                    }
                    else
                        from.SendMessage("Error: illegal sound id.");
                    return;
                }
                else
                {
                    nv = (int)GetNoteValue(note);
                }

                //Pix: added bounds checking
                if (nv >= 0 && it >= 0 &&
                    nv < NoteSounds.Length && it < NoteSounds[nv].Length)
                {
                    int sound = NoteSounds[nv][it];

                    CoreMusicPlayer.PlaySound(from, sound, SystemMusicPlayer.MusicConfig[from].Volume, SystemMusicPlayer.MusicConfig[from].ConcertMode);
                }
            }
            catch (Exception ex)
            {
                Server.Diagnostics.LogHelper.LogException(ex);
            }
        }

        public static NoteValue GetNoteValue(string note)
        {
            if (note == "cl") return NoteValue.c_low;
            if (note == "csl") return NoteValue.c_sharp_low;
            if (note == "d") return NoteValue.d;
            if (note == "ds") return NoteValue.d_sharp;
            if (note == "e") return NoteValue.e;
            if (note == "f") return NoteValue.f;
            if (note == "fs") return NoteValue.f_sharp;
            if (note == "g") return NoteValue.g;
            if (note == "gs") return NoteValue.g_sharp;
            if (note == "a") return NoteValue.a;
            if (note == "as") return NoteValue.a_sharp;
            if (note == "b") return NoteValue.b;
            if (note == "c") return NoteValue.c;
            if (note == "cs") return NoteValue.c_sharp;
            if (note == "dh") return NoteValue.d_high;
            if (note == "dsh") return NoteValue.d_sharp_high;
            if (note == "eh") return NoteValue.e_high;
            if (note == "fh") return NoteValue.f_high;
            if (note == "fsh") return NoteValue.f_sharp_high;
            if (note == "gh") return NoteValue.g_high;
            if (note == "gsh") return NoteValue.g_sharp_high;
            if (note == "ah") return NoteValue.a_high;
            if (note == "ash") return NoteValue.a_sharp_high;
            if (note == "bh") return NoteValue.b_high;
            if (note == "ch") return NoteValue.c_high;
            else return 0;
        }

        public static InstrumentType GetInstrumentType(BaseInstrument instrument)
        {
            // Can't play notes on drums or tamborines
            if (instrument is Harp || instrument is AwardHarp) return InstrumentType.Harp;
            if (instrument is LapHarp || instrument is AwardLapHarp) return InstrumentType.LapHarp;
            if (instrument is Lute || instrument is AwardLute) return InstrumentType.Lute;
            if (instrument is RazorInstrument) return (instrument as RazorInstrument).GetInstrumentType();
            else return 0;
        }

        private static int[][] NoteSounds = new int[][]
        {
			// Each array represents the sounds for each note on harp, lap harp, and lute
			new int[] { 1181, 976, 1028 }, // c_low
			new int[] { 1184, 979, 1031 }, // c_sharp_low
			new int[] { 1186, 981, 1033 }, // d
			new int[] { 1188, 983, 1036 }, // d_sharp
			new int[] { 1190, 985, 1038 }, // e
			new int[] { 1192, 987, 1040 }, // f
			new int[] { 1194, 989, 1042 }, // f_sharp
			new int[] { 1196, 991, 1044 }, // g
			new int[] { 1198, 993, 1046 }, // g_sharp
			new int[] { 1175, 970, 1021 }, // a
			new int[] { 1177, 972, 1023 }, // a_sharp
			new int[] { 1179, 974, 1025 }, // b
			new int[] { 1182, 977, 1029 }, // c
			new int[] { 1185, 980, 1032 }, // c_sharp
			new int[] { 1187, 982, 1034 }, // d_high
			new int[] { 1189, 984, 1037 }, // d_sharp_high
			new int[] { 1191, 986, 1039 }, // e_high
			new int[] { 1193, 988, 1041 }, // f_high
			new int[] { 1195, 990, 1043 }, // f_sharp_high
			new int[] { 1197, 992, 1045 }, // g_high
			new int[] { 1199, 994, 1047 }, // g_sharp_high
			new int[] { 1176, 971, 1022 }, // a_high
			new int[] { 1178, 973, 1024 }, // a_sharp_high
			new int[] { 1180, 975, 1026 }, // b_high
			new int[] { 1183, 978, 1030 }  // c_high
		};

        public static int[] VolumeMap =
        {
            20,19,18,17,16,15,14,13,12,11,10,9,8,7,6,5,4,3,2,1,0
        };
        public static int MapVolume(int volume)
        {
            if (volume < 0) volume = 0;
            if (volume >= VolumeMap.Length) volume = VolumeMap.Length - 1;
            return VolumeMap[volume];
        }
        public static void PlaySound(Mobile mobile, int soundID, int volume, bool concertMode)
        {
            if (soundID == -1)
                return;

            if (SystemMusicPlayer.MusicConfig.ContainsKey(mobile) == false)
                SystemMusicPlayer.MusicConfig.Add(mobile, new Server.Commands.MusicContext(true));
            if (SystemMusicPlayer.MusicConfig[mobile].Source == null)
                SystemMusicPlayer.MusicConfig[mobile].Source = mobile;

            // what object is this music emanating from? mobile or Item?
            IEntity source = SystemMusicPlayer.MusicConfig[mobile].Source;

            // remap volume
            volume = MapVolume(volume);

            if (source?.Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = source.Map.GetClientsInRange(source.Location);

                foreach (NetState state in eable)
                {   // invisible is okay (music motion controller,) but if it's visible, you need to be able to see it (xmas tree)
                    if (!Visible(source) || state.Mobile.CanSee(source))
                    {
                        // If the mobile is a player who has toggled FilterMusic on, don't play.
                        if (state.Mobile is PlayerMobile pm && pm.FilterMusic)
                            continue;

                        if (concertMode)
                            // need a separate packet per player
                            // we set volume based on distance to the sound if we're in *concert mode*
                            p = Packet.Acquire(new PlaySound(soundID, new Point3D(state.Mobile.Location.X + volume, state.Mobile.Location.Y, state.Mobile.Location.Z)));
                        else
                            p = Packet.Acquire(new PlaySound(soundID, source.Location));

                        state.Send(p);

                        Packet.Release(p);
                        p = null;
                    }
                }

                eable.Free();
            }
        }
        private static bool Visible(IEntity source)
        {
            // invisible is okay (music motion controller,) but if it's visible, you need to be able to see it (xmas tree)
            bool entity_visible = false;
            if (source is Item item)
                entity_visible = item.Visible;
            else if (source is Mobile mx)
                entity_visible = !mx.Hidden;

            return entity_visible;
        }
    }
}