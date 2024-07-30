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

/* Scripts\Engines\Music\BeatMonkey.cs
 * Changelog
 *  2/26/22, Adam
 *      Update the call to PlaySound to match the new parms
 *  1/31/22, Adam (Play.Player)
 *      This is just a test harness for experimental work ongoing in the music system.
 *		Initial creation.
 */

/*
 * The BPM to MS Formula
 * The formula is pretty simple, but it can be annoying to calculate over and over again.
 * 1 Minute = 60,000 milliseconds (ms)
 * To get the duration of a quarter note (a quarter note = 1 beat) for any tempo/BPM we divide the number of milliseconds per minute by the BPM. So:
 * 60,000 (ms) ÷ BPM = duration of a quarter note
 * Example:
 * 60,000 ÷ 140 BPM = 428.57 ms per beat/quarter note.
 * https://tuneform.com/tools/time-tempo-bpm-to-milliseconds-ms
 */

using Server.Misc;
using System;
using System.Collections.Generic;

/*
 * Grave – slow and solemn (20–40 BPM)
 * Lento – slowly (40–45 BPM)
 * Largo – broadly (45–50 BPM)
 * Adagio – slow and stately (literally, “at ease”) (55–65 BPM)
 * Adagietto – rather slow (65–69 BPM)
 * Andante – at a walking pace (73–77 BPM)
 * Moderato – moderately (86–97 BPM)
 * Allegretto – moderately fast (98–109 BPM)
 * Allegro – fast, quickly and bright (109–132 BPM)
 * Vivace – lively and fast (132–140 BPM)
 * Presto – extremely fast (168–177 BPM)
 * Prestissimo – even faster than Presto (178 BPM and over)
 * https://symphonynovascotia.ca/faqs/symphony-101/how-do-musicians-know-how-fast-to-play-a-piece-and-why-are-the-terms-in-italian/
*/
namespace Server.Engines
{
    public static class BeatMonkey
    {
        // name, beats per minute (BPM)
        private static Dictionary<string, double> Tempo = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            {"Grave", 3000          /*20  BPM*/ },
            {"Lento", 1500          /*40  BPM*/ },
            {"Largo", 1333          /*45  BPM*/ },
            {"Adagio", 1091         /*55  BPM*/ },
            {"Adagietto", 923       /*65  BPM*/ },
            {"Andante", 822         /*73  BPM*/ },
            {"Moderato", 698        /*86  BPM*/ },
            {"Allegretto", 612      /*98  BPM*/ },
            {"Allegro", 550         /*109 BPM*/},
            {"Vivace", 455          /*132 BPM*/ },
            {"Presto", 357          /*168 BPM*/ },
            {"Prestissimo", 337     /*178 BPM*/ },
        };

        public static void Do()
        {

        }
        private static bool m_stop;
        private static void Tick(object state)
        {
            object[] aState = (object[])state;
            double tempo = (double)aState[0];
            Mobile m = (Mobile)aState[1];
            bool unused = (bool)aState[2];
            TimerStateCallback callback = (TimerStateCallback)aState[3];
            if (m_stop)
                return;

            // play middle C
            CoreMusicPlayer.PlaySound(m, 1182, 20, true);

            Timer.DelayCall(TimeSpan.FromMilliseconds(tempo), callback, new object[] { tempo, m, unused, callback });
        }

        #region command interface
        public static void Initialize()
        {
            CommandSystem.Register("BeatMonkey", AccessLevel.Administrator, new CommandEventHandler(BeatMonkey_OnCommand));
        }
        [Usage("BeatMonkey")]
        [Description("Development tool for music system.")]
        public static void BeatMonkey_OnCommand(CommandEventArgs e)
        {
            TimerStateCallback callback = new TimerStateCallback(Tick);
            switch (e.Arguments[0])
            {
                case "tempo":
                    {
                        double tempo = Tempo[e.Arguments[1]];
                        Timer.DelayCall(TimeSpan.FromMilliseconds(tempo), callback, new object[] { tempo, e.Mobile, false, callback });
                        break;
                    }
                case "stop":
                    {
                        //double tempo = 0;
                        //Timer.DelayCall(TimeSpan.FromMilliseconds(tempo), callback, new object[] { tempo, e.Mobile, true, callback });
                        m_stop = true;
                        break;
                    }
            }
        }
        #endregion command interface
    }
}