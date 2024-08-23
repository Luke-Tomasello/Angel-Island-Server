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

/* Scripts\Engines\Music\SystemMusicPlayer.cs
 * Changelog
 *  12/28/23, Adam (MusicConfig[player].Tick)
 *      Add a callback to notify the caller we are still processing notes. 
 *      Since this is called on every tick, it is the callers responsibility to limit processing so as to not impact playback.
 *      Typically, the caller will only process the tick every two seconds or so to verify the player is still around to hear the music.
 *  3/12/22, Adam
 *      Improve console out based on whether the player is 'listening to' or 'composing' music.
 *  3/3/22, Adam
 *      1. Add new skill gain system
 *      2. Add test mode for new skill gain system
 *      3. add [play MusicKarma command so players can learn what their karma is
 *      4. better reporting of point gains
 *      5. disallow the use of sound effects only when composing, playback via RSM is allowed
 *          This is how players with no music karma can listen to songs with sound effects when they themselves have no music karma.
 *      6. replace GetHashcode() with GetStableHashCode() so that hashes are valid across server restarts.
 *  3/2/22, Adam
 *      Serialize SameLine memory table.
 *  2/28/22, Adam
 *      Implement a skill gain system for composers.
 *      Skill gain is based on line complexity.
 *      There is also a database of previous lines. Gains get harder the more times we see the same line.
 *  2/26/22, Adam
 *      Add a new AI special 'skill' for Composers
 *      Process point accumulation for composition.
 *  2/26/22, Adam
 *      Fix the regular expression for processing tempo changes .. it was grabbing volume and changing it!
 *  2/26/22, Adam
 *      massive changes
 *      1. Removed all barding requirements (skill, backpack, etc.)
 *      2. increased queue size from 32 notes to 1024 objects (queue now contains volume objects, instrument objects, etc.)
 *      3. added macros (and a list command)
 *      4. added 'music algebra' expressions that can be applied to macros. Expressions include:
 *          4a. Tempo changes
 *          4b. Transposition
 *          4c. reversing
 *      5. Added Volume and ConcertMode. Together, these provide separate 'sound packets' for each player hearing the tune.
 *      6. added an inspect command for staff to see the queue/depth
 *  2/19/22, Adam
 *      Cleanup string constants
 *  2/9/22, Adam
 *      Move virtual instruments to ItemToIntStorage for safe keeping.
 *  2/8/22, Adam
 *      Replace ~MusicContext() with an explicit MusicContext.Finish() method for reliable control over when virtual instruments get deleted.
 *  2/7/22
 *      Sound effects enabled for players with 1000 music karma.
 *      You get music karma by publishing songs, and selling songs.
 *      implement 'note offsets' to select a different instrument.
 *  2/6/11, Adam,
 *      serialize music contexts associated with a player
 *      Sound effects currently turned off pending further review
 *  1/4/22, Adam
 *  pretty much a rewrite of the legacy music system.
 *  Remove all 'pick instrument' stuff since the music system now keeps a table of virtual instruments for players.
 *  Add sounds: +NNN
 *  Add support for changing instruments intra-song.
 *  Remove the requirement for picking an instrument. Instead you 'configure' the player with commands such as:
 *  [play instrument lute
 *  [play prefetch true
 *  [play newtimer true
 *  [play tempo NNN
 *  1/2/21, Adam (Play.Player)
 *      Create the new, more flexable Player interface that accepts finer grained configuration of the music player
 *          New properties include minMusicSkill, instrument, requiresBackpack, musicChatter, and checkMusicianship
 *  12/10/21, Adam (PlayerMobile)
 *      Convert most all uses of PlayerMobile to Mobile as thos function have moved down to Mobile.
 *      This was done so that NPC can eventually 'play music'.
 *  12/7/21, Adam (MaxQueueSize)
 *      Make MaxQueueSize public so automated of song playing can be managed
 *	3/12/11, Adam
 *		Music is only available for AI and MO
 *	3/22/08, Adam
 *		Add checks to OnPickedInstrument
 *			Disallow percussion instruments
 *			Make sure it's in your backpack
 *			Convert all the specific Catch types to generic catches and use LogHelper to log them
 *  09/03/06, Rhiannon
 *		Fixed bug in StopMusic_OnCommand().
 *  08/20/06, Rhiannon
 *		Added emote when player starts playing
 *		Added detection of repeated notes
 *	07/30/06, Rhiannon
 *		Initial creation.
 */
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace Server.Commands
{
    public record MusicContext
    {
        public bool Configured;                                 // flag to tell us if this object has yet been configured.
        public bool NewTimer;                                   // using the new timer model
        public int Tempo;                                       // timer frequency
        public bool Prefetch;                                   // schedule the next timer tick immediately without finishing current timer cycle
        public bool MusicChatter;                               // turn on/off music chatter
        public int Volume = 20;                                 // values 0-20, 20 being the loudest
        public bool ConcertMode = false;                        // disables volume and enables relative distribution of sound packets
        public RazorInstrument.InstrumentSoundType Instrument;  // currently selected instrument - pointed to by Selector (pending instrument change)
        public RazorInstrument.InstrumentSoundType Selector;    // Selector pointing at current instrument
        public BaseInstrument VirtualInstrument;                // actual instrument
        private RazorInstrument m_harp;
        private RazorInstrument m_lapHarp;
        private RazorInstrument m_lute;
        private const string harp = "harp";
        private const string lapHarp = "lapharp";
        private const string lute = "lute";
        #region Source
        IEntity m_Source = null;
        public IEntity Source { get { return m_Source; } set { m_Source = value; } }
        #endregion Source   
        #region Callback
        public delegate void TickCallback(Mobile m, IEntity source);    // notify our caller of progress
        public TickCallback Tick = null;                                // the caller may want to check if, for instance, the mobile is still online
        #endregion Callback
        #region Composition points
        public int ObjectCount = 0;                                         // More objects, a better chance at a gain (Notes, Volume, etc.)
        public int ComplexityPoints = 0;                                    // earned for complexity: music algebra, volume, etc.
        public double LastGain = 0;
        public bool SkillTesting = false;                                   // true when we are running skill tests
        public Dictionary<int, int> SameLine = new Dictionary<int, int>();  // keep track of repeated lines and mak gains harder
        #endregion Composition points
        #region Session Data, Not Serialized
        public Queue<object> PlayList = new Queue<object>();
        public bool Playing = false;
        #endregion Session Data, Not Serialized
        #region User Macros
        public SortedList<string, string> Macros = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
        #endregion User Macros
        public static RazorInstrument.InstrumentSoundType GetSelector(string instrument)
        {
            if (string.IsNullOrEmpty(instrument))
                return RazorInstrument.InstrumentSoundType.None;

            switch (instrument.ToLower())
            {
                case harp: return RazorInstrument.InstrumentSoundType.Harp;
                case lapHarp: return RazorInstrument.InstrumentSoundType.LapHarp;
                case lute: return RazorInstrument.InstrumentSoundType.Lute;
                default: return RazorInstrument.InstrumentSoundType.None;
            }
        }
        public RazorInstrument GetOffsetInstrument(int offset)
        {
            if (offset > 2 || offset < -2)
                return null;

            if (VirtualInstrument == m_harp)
            {
                if (offset == 1)
                    return m_lapHarp;
                if (offset == 2)
                    return m_lute;
                // no instrument there
                return null;
            }
            if (VirtualInstrument == m_lapHarp)
            {
                if (offset == -1)
                    return m_harp;
                if (offset == 1)
                    return m_lute;
                // no instrument there
                return null;
            }
            if (VirtualInstrument == m_lute)
            {
                if (offset == -1)
                    return m_lapHarp;
                if (offset == -2)
                    return m_harp;
                // no instrument there
                return null;
            }
            return null;
        }
        public void SelectInstrument()
        {
            switch (Selector)
            {
                default:
                case RazorInstrument.InstrumentSoundType.Harp: VirtualInstrument = m_harp; break;
                case RazorInstrument.InstrumentSoundType.LapHarp: VirtualInstrument = m_lapHarp; break;
                case RazorInstrument.InstrumentSoundType.Lute: VirtualInstrument = m_lute; break;
            }
        }
        public MusicContext(bool ignored)
            : this() // construct the default object
        {
            Configured = false;
        }
        public MusicContext(bool newTimer = false, int tempo = SystemMusicPlayer.DefaultTempo, bool prefetch = false, string instrument = null, bool chatter = true, bool concertMode = false)
        {
            Configured = true;
            NewTimer = newTimer;
            Tempo = tempo;
            Prefetch = prefetch;
            MusicChatter = chatter;
            ConcertMode = concertMode;
            // current Instrument and Selector are different so that we can detect instrument changes
            if (string.IsNullOrEmpty(instrument))
            {   // default instrument
                Instrument = GetSelector(harp);               // currently selected instrument - pointed to by Selector
                Selector = GetSelector(harp);                 // Selector pointing at current instrument
            }
            else
            {   // specific instrument
                Instrument = GetSelector(instrument);           // currently selected instrument - pointed to by Selector
                Selector = GetSelector(instrument);             // Selector pointing at current instrument
            }
            // selector 0
            m_harp = new RazorInstrument(RazorInstrument.GetSoundType("harp"));
            m_harp.Name = harp;
            m_harp.RequiresBackpack = false;
            //m_harp.PreferredInstrument = "harp";
            m_harp.ConfigureInstrument(harp);
            m_harp.MoveToIntStorage();
            // selector 1
            m_lapHarp = new RazorInstrument(RazorInstrument.GetSoundType("lapharp"));
            m_lapHarp.Name = lapHarp;
            m_lapHarp.RequiresBackpack = false;
            //m_lapHarp.PreferredInstrument = "lapharp";
            m_lapHarp.ConfigureInstrument(lapHarp);
            m_lapHarp.MoveToIntStorage();
            // selector 2
            m_lute = new RazorInstrument(RazorInstrument.GetSoundType("lute"));
            m_lute.Name = lute;
            m_lute.RequiresBackpack = false;
            //m_lute.PreferredInstrument = "lute";
            m_lute.ConfigureInstrument(lute);
            m_lute.MoveToIntStorage();
        }
        public bool IsValid()
        {
            return m_harp != null && m_lapHarp != null && m_lute != null;
        }
        public void Finish()
        {
            if (m_harp != null)
                m_harp.Delete();
            if (m_lapHarp != null)
                m_lapHarp.Delete();
            if (m_lute != null)
                m_lute.Delete();
        }
        public MusicContext(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();
            switch (version)
            {
                case 3:
                    {
                        int count = reader.ReadEncodedInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            int hash = reader.ReadEncodedInt();
                            int seen = reader.ReadEncodedInt();
                            SameLine.Add(hash, seen);
                        }
                        goto case 2;
                    }
                case 2:
                    {
                        Volume = reader.ReadEncodedInt();
                        ConcertMode = reader.ReadBool();
                        int count = reader.ReadEncodedInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            string key = reader.ReadString();
                            string value = reader.ReadString();
                            Macros.Add(key, value);
                        }
                        goto case 1;
                    }
                case 1:
                    {
                        MusicChatter = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        Configured = reader.ReadBool();
                        NewTimer = reader.ReadBool();
                        Tempo = reader.ReadEncodedInt();
                        Prefetch = reader.ReadBool();
                        Instrument = (RazorInstrument.InstrumentSoundType)reader.ReadEncodedInt();
                        Selector = (RazorInstrument.InstrumentSoundType)reader.ReadEncodedInt();
                        VirtualInstrument = (BaseInstrument)reader.ReadItem();
                        m_harp = (RazorInstrument)reader.ReadItem();
                        m_lapHarp = (RazorInstrument)reader.ReadItem();
                        m_lute = (RazorInstrument)reader.ReadItem();
                        break;
                    }
            }
        }
        public void Serialize(GenericWriter writer)
        {
            int version = 3;
            writer.WriteEncodedInt(version);
            switch (version)
            {
                case 3:
                    {
                        writer.WriteEncodedInt(SameLine.Count);
                        foreach (KeyValuePair<int, int> kvp in SameLine)
                        {
                            writer.WriteEncodedInt(kvp.Key);
                            writer.WriteEncodedInt(kvp.Value);
                        }
                        goto case 2;
                    }
                case 2:
                    {
                        writer.WriteEncodedInt(Volume);
                        writer.Write(ConcertMode);
                        writer.WriteEncodedInt(Macros.Count);
                        foreach (KeyValuePair<string, string> kvp in Macros)
                        {
                            writer.Write(kvp.Key);
                            writer.Write(kvp.Value);
                        }
                        goto case 1;
                    }
                case 1:
                    {
                        writer.Write(MusicChatter);
                        goto case 0;
                    }
                case 0:
                    {
                        writer.Write(Configured);
                        writer.Write(NewTimer);
                        writer.WriteEncodedInt(Tempo);
                        writer.Write(Prefetch);
                        writer.WriteEncodedInt((int)Instrument);
                        writer.WriteEncodedInt((int)Selector);
                        writer.Write(VirtualInstrument);
                        writer.Write(m_harp);
                        writer.Write(m_lapHarp);
                        writer.Write(m_lute);
                        break;
                    }
            }
        }
    }
    public class SystemMusicPlayer
    {
        public static Dictionary<Mobile, MusicContext> MusicConfig = new Dictionary<Mobile, MusicContext>();
        public const int DefaultTempo = 100;
        private static RazorInstrument.InstrumentSoundType SelectInstrument(string instrument)
        {
            if (instrument.ToLower() == "harp")
                return RazorInstrument.InstrumentSoundType.Harp;
            else if (instrument.ToLower() == "lapharp")
                return RazorInstrument.InstrumentSoundType.LapHarp;
            else if (instrument.ToLower() == "lute")
                return RazorInstrument.InstrumentSoundType.Lute;
            else
                return RazorInstrument.InstrumentSoundType.None;
        }
        public static bool ForceStop(Mobile m)
        {
            if (MusicConfig.ContainsKey(m))
            {   // this forces an immediate shutdown of the context and blows away the various queues
                //  which may still be loaded with notes. 
                //MusicConfig.Remove(m);
                //MusicConfig.Add(m, new MusicContext(true));
                MusicConfig[m].Source = null;
                MusicConfig[m].Tick = null;
                MusicConfig[m].PlayList.Clear();
                MusicConfig[m].Playing = false;
                return true;
            }
            return false;
        }
        private static void InitializeContext(Mobile m)
        {
            // if the user has no music object
            if (!MusicConfig.ContainsKey(m))
                // create a default one
                MusicConfig.Add(m, new MusicContext(true));
            else if (MusicConfig[m].IsValid() == false)
            {   // This should never execute
                //  it's a patch for a bug in an early version of [play that didn't move
                //  instruments to IntMapstorage and instruments were getting deleted
                Utility.Monitor.WriteLine("Resetting player context object.", ConsoleColor.Red);
                MusicConfig[m].Finish();
                MusicConfig.Remove(m);
                MusicConfig.Add(m, new MusicContext(true));
            }
        }
        public static bool IsUserBlocked(Mobile from)
        {
            return from != null && SystemMusicPlayer.MusicConfig.ContainsKey(from) && SystemMusicPlayer.MusicConfig[from].Playing;
        }
        public static void ConfigurePlayer(
            Mobile m,
            bool newTimer = false,
            int tempo = DefaultTempo,
            bool prefetch = false,
            string instrument = null,
            bool chatter = true,
            bool concertMode = false,
            MusicContext.TickCallback callback = null,
            IEntity source = null)
        {   // called via API (RSM uses this)
            if (MusicConfig.ContainsKey(m))
            {
                MusicConfig[m].NewTimer = newTimer;                                 // new timer true/false
                MusicConfig[m].Tempo = tempo;                                       // tempo
                MusicConfig[m].Prefetch = prefetch;                                 // prefetch
                MusicConfig[m].Instrument = MusicContext.GetSelector(instrument);   // what instrument we would like to play
                MusicConfig[m].Selector = MusicContext.GetSelector(instrument);     // instrument to select 
                MusicConfig[m].SelectInstrument();                                  // select that instrument
                MusicConfig[m].MusicChatter = chatter;
                MusicConfig[m].ConcertMode = concertMode;
                MusicConfig[m].Tick = callback;                                     // generic callback to notify the caller of progress
                MusicConfig[m].Source = source;                                     // from what object does the music emanate? Mobile or Item?
            }
            else
                MusicConfig.Add(m, new MusicContext(newTimer, tempo, prefetch, instrument, chatter, concertMode));

            // record this session data change (if the recorder is turned on)
            Engines.MusicRecorder.Record(m);
        }
        private static List<string> DefineMacro(string[] arguments)
        {
            List<string> macro = new List<string>();
            string text = string.Empty;
            // glue
            for (int ix = 1; ix < arguments.Length; ix++)
                text += arguments[ix].ToString() + " ";

            if (!text.Contains("="))
                return macro;

            string[] words = text.Split('=', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2 || string.IsNullOrEmpty(words[1].Trim()))
            {   // xxx= is how you delete a macro
                macro.Add(words[0].Trim());
                return macro;
            }

            // okay, we have a good macro
            if (words.Length == 2)
            {   // foo=a b c d
                macro.Add(words[0].Trim());
                macro.Add(words[1].Trim());
            }

            return macro;
        }
        private static bool DoConfigure(Mobile m, string[] arguments, bool bSilent)
        {   // called by the user via the [play command

            // Error: ill-formed [play command
            if (arguments == null || arguments.Length == 0)
            {
                // don't allow the player to attempy yo play this
                if (!bSilent)
                    m.SendMessage("Error: ill-formed [play command.");
                return true;
            }

            string command = arguments[0].ToLower();

            if (
                // music configuration directives
                command == "newtimer" ||
                command == "tempo" ||
                command == "prefetch" ||
                command == "instrument" ||
                command == "chatter" ||
                command == "volume" ||          // only useful while in concert mode. Not currently processed in (RZR) directives. Should add
                command == "concertmode" ||
                // commands
                command == "reset" ||
                command == "config" ||
                command == "inspect" ||
                command == "macro" ||
                command == "list" ||
                command == "musickarma" ||
                command == "test")
            {
                if (command == "reset")
                {
                    if (MusicConfig.ContainsKey(m))
                    {   // free up instruments
                        MusicConfig[m].Finish();

                        // save macros
                        SortedList<string, string> temp = MusicConfig[m].Macros;

                        // free old, and create new context
                        MusicConfig.Remove(m);
                        ///  create a default context
                        MusicConfig.Add(m, new MusicContext());

                        // reassign saved elements. users don't lose macros etc. on a "reset"
                        MusicConfig[m].Macros = temp;
                    }
                    if (!bSilent)
                        m.SendMessage("Music object reset.");
                }
                else if (command == "test")
                {
                    if (m.AccessLevel > AccessLevel.GameMaster)
                    {
                        (m as PlayerMobile).NpcGuildPoints = 0;
                        MusicConfig[m].SameLine.Clear();
                        MusicConfig[m].SkillTesting = true;
                        if (!bSilent)
                            m.SendMessage("Guild points are now {0}.", (m as PlayerMobile).NpcGuildPoints);
                        Console.Clear();
                        Utility.Monitor.WriteLine(string.Format("Start test at {0}", DateTime.UtcNow), ConsoleColor.Red);
                    }
                    else
                        return true;
                }
                else if (command == "musickarma")
                {
                    if (!bSilent)
                        m.SendMessage("Your music karma is {0}.", MusicBox.GetMusicKarma(m));
                }
                else if (command == "volume")
                {
                    int volume;
                    if (arguments.Length == 2 && int.TryParse(arguments[1], out volume) == true)
                    {
                        // we call map volume twice to return the User's setting and not the internal representation
                        // (map volume handles clipping values)
                        MusicConfig[m].Volume = CoreMusicPlayer.MapVolume(CoreMusicPlayer.MapVolume(volume));
                        MusicConfig[m].Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("Error in Volume format.");
                        return true;
                    }
                    if (!bSilent)
                        m.SendMessage("Volume set to {0}.", MusicConfig[m].Volume);
                }
                else if (command == "concertmode")
                {
                    bool concertmode;
                    if (arguments.Length == 2 && bool.TryParse(arguments[1], out concertmode) == true)
                    {
                        MusicConfig[m].ConcertMode = concertmode;
                        MusicConfig[m].Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("Error in ConcertMode format.");
                        return true;
                    }
                    if (!bSilent)
                        m.SendMessage("ConcertMode set to {0}.", MusicConfig[m].ConcertMode);
                }
                else if (command == "list")
                {
                    if (MusicConfig.ContainsKey(m) && MusicConfig[m].Macros.Count > 0)
                    {
                        if (!bSilent)
                            foreach (KeyValuePair<string, string> kvp in MusicConfig[m].Macros)
                                m.SendMessage("{0}={1}", kvp.Key, kvp.Value);
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("No macros to list.");
                    }
                }
                else if (command == "macro")
                {
                    if (MusicConfig.ContainsKey(m))
                    {
                        List<string> macro = DefineMacro(arguments);

                        if (MusicConfig[m].Macros.Count > 128 && macro.Count == 2)
                        {
                            if (!bSilent)
                                m.SendMessage("Too many macros.");
                        }
                        else if (macro.Count == 1)
                        {   // delete macro
                            if (MusicConfig[m].Macros.ContainsKey(macro[0]))
                            {
                                MusicConfig[m].Macros.Remove(macro[0]);
                                if (!bSilent)
                                    m.SendMessage("Macro {0} deleted.", macro[0]);
                            }
                            else
                            {
                                if (!bSilent)
                                    m.SendMessage("Macro not found.");
                            }
                        }
                        else if (macro.Count == 2)
                        {   // defining a new macro
                            bool overwrite = false;
                            if (overwrite = MusicConfig[m].Macros.ContainsKey(macro[0]))
                                MusicConfig[m].Macros.Remove(macro[0]);

                            MusicConfig[m].Macros.Add(macro[0], macro[1]);
                            if (!bSilent)
                                m.SendMessage("{0} {2}defined as '{1}'.", macro[0], macro[1], overwrite ? "re" : "");
                        }
                        else
                        {
                            if (!bSilent)
                                m.SendMessage("Error: ill-formed macro.");
                        }
                    }
                }
                else if (command == "inspect" && m.AccessLevel >= AccessLevel.GameMaster)
                {
                    if (MusicConfig.ContainsKey(m) && MusicConfig[m].PlayList.Count > 0)
                    {
                        string text = string.Empty;
                        foreach (object o in MusicConfig[m].PlayList)
                        {
                            if (o is RazorInstrument.InstrumentSoundType)
                                text += ((RazorInstrument.InstrumentSoundType)o).ToString() + " ";
                            else if (o is double)
                                text += ((double)o).ToString() + " ";
                            else if (o is string)
                                text += (o as string) + " ";
                        }

                        Console.WriteLine(text);
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("Nothing to inspect.");
                    }
                }
                else if (command == "config")
                {
                    if (MusicConfig[m].Configured == false)
                    {
                        if (!bSilent)
                            m.SendMessage("Default values:");
                    }

                    // if a dependent mode is off, then the this mode will show in a special hue
                    int disabled = 0x3B2;
                    int enabled = 0x40;
                    int vmode = MusicConfig[m].ConcertMode == false ? disabled : enabled;
                    int nmode = MusicConfig[m].NewTimer == false ? disabled : enabled;

                    if (!bSilent)
                    {
                        m.SendMessage(enabled, "Instrument set to {0}.", MusicConfig[m].Instrument.ToString());
                        m.SendMessage(enabled, "NewTimer set to {0}.", MusicConfig[m].NewTimer);
                        m.SendMessage(nmode, "Prefetch set to {0}.", MusicConfig[m].Prefetch);
                        m.SendMessage(nmode, "Tempo set to {0} milliseconds.", MusicConfig[m].Tempo);
                        m.SendMessage(vmode, "Volume set to {0}.", MusicConfig[m].Volume);
                        m.SendMessage(enabled, "ConcertMode set to {0}.", MusicConfig[m].ConcertMode);
                        m.SendMessage(enabled, "Chatter set to {0}.", MusicConfig[m].MusicChatter);
                    }
                }
                else if (command == "instrument")
                {
                    if (arguments.Length == 2 && SelectInstrument(arguments[1]) != RazorInstrument.InstrumentSoundType.None)
                    {
                        // check for too many notes.
                        if (MusicConfig[m].PlayList.Count > MaxQueueSize)
                        {
                            if (!bSilent)
                                m.SendMessage("Warning: Max queued notes reached.");
                            return true;
                        }

                        // we will change to this instrument...
                        MusicConfig[m].Instrument = MusicContext.GetSelector(arguments[1]);

                        // ...at this place in the queue
                        MusicConfig[m].PlayList.Enqueue(MusicConfig[m].Instrument);

                        MusicConfig[m].Configured = true;
                        //Utility.ConsoleOut("request to change instrument to {0}", ConsoleColor.Yellow, MusicContext.GetSelector(arguments[1]));
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("Error in Instrument format.");
                        return true;
                    }

                    if (!bSilent)
                        m.SendMessage("Instrument set to {0}.", MusicConfig[m].Instrument.ToString());
                }
                else if (command == "prefetch")
                {
                    bool prefetch;
                    if (arguments.Length == 2 && bool.TryParse(arguments[1], out prefetch) == true)
                    {
                        MusicConfig[m].Prefetch = prefetch;
                        MusicConfig[m].Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("Error in Prefetch format.");
                        return true;
                    }

                    if (!bSilent)
                        m.SendMessage("Prefetch set to {0}.", MusicConfig[m].Prefetch);
                }
                else if (command == "newtimer")
                {
                    bool newTimer;
                    if (arguments.Length == 2 && bool.TryParse(arguments[1], out newTimer) == true)
                    {
                        MusicConfig[m].NewTimer = newTimer;
                        MusicConfig[m].Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("Error in NewTimer format.");
                        return true;
                    }

                    if (!bSilent)
                        m.SendMessage("NewTimer set to {0}.", MusicConfig[m].NewTimer);
                }
                else if (command == "tempo")
                {
                    int tempo = DefaultTempo;
                    if (arguments.Length == 2 && int.TryParse(arguments[1], out tempo) == true)
                    {
                        MusicConfig[m].Tempo = tempo;
                        MusicConfig[m].Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("Error in Tempo format.");
                        return true;
                    }
                    if (!bSilent)
                        m.SendMessage("Tempo set to {0} milliseconds.", MusicConfig[m].Tempo);
                }
                else if (command == "chatter")
                {
                    bool chatter;
                    if (arguments.Length == 2 && bool.TryParse(arguments[1], out chatter) == true)
                    {
                        MusicConfig[m].MusicChatter = chatter;
                        MusicConfig[m].Configured = true;
                    }
                    else
                    {
                        if (!bSilent)
                            m.SendMessage("Error in Chatter format.");
                        return true;
                    }
                    if (!bSilent)
                        m.SendMessage("Chatter set to {0}.", MusicConfig[m].MusicChatter);
                }
                else
                {
                    if (!bSilent)
                        m.SendMessage("Unknown instrument setting.");
                }

                // record this session data change (if the recorder is turned on)
                Engines.MusicRecorder.Record(m);

                return true;
            }
            else
                return false;
        }
        public static void Initialize()
        {   // too good to limit to any specific shard.
            if (Core.RuleSets.AllServerRules())
            {
                Server.CommandSystem.Register("Play", CoreAI.PlayAccessLevel, new CommandEventHandler(Play_OnCommand));
                Server.CommandSystem.Register("StopMusic", AccessLevel.Player, new CommandEventHandler(StopMusic_OnCommand));
                Server.CommandSystem.Register("FilterMusic", AccessLevel.Player, new CommandEventHandler(FilterMusic_OnCommand));
            }
        }
        private const int MaxLines = 128;
        public static int MaxQueueSize { get { return 1024; } }
        public static void Pack(WorldSaveEventArgs e)
        {
            Console.WriteLine("Music System Packing...");
            try
            {
                foreach (var mc in MusicConfig)
                    while (mc.Value.SameLine.Count > MaxLines + MaxLines / 2)
                        if (mc.Value.SameLine.ContainsKey(mc.Value.SameLine.Keys.First()))
                            mc.Value.SameLine.Remove(mc.Value.SameLine.Keys.First());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while packing music data.");
                Console.WriteLine(ex.ToString());
            }
        }
        //public static Mobile NewTempMobile(string name = null)
        //{   // 10 minutes should outlast the longest song
        //    return new Mobile(name: name ?? "music context object", 10.0);
        //}
        private static bool InstrumentChanging(Mobile m)
        {
            return MusicConfig.ContainsKey(m) && MusicConfig[m].Selector != MusicConfig[m].Instrument;
        }
        public static bool IsSound(string sound)
        {
            if (string.IsNullOrEmpty(sound) || sound.Length < 2 || sound[0] != '+')
                return false;

            for (int i = 1; i < sound.Length; i++)
            {
                if (char.IsDigit(sound[i]))
                    continue;
                else return false;
            }

            return true;
        }
        private static bool IsSelector(string sound)
        {
            int direction;
            return IsSelector(sound, out direction);
        }
        private static bool IsSelector(string sound, out int direction)
        {
            direction = 0;

            // must be of the form +[+] | -[-] note (note can be 1-3 characters)
            // there are also only 3 instruments, so the offset can never exceed 2 (or be 0)
            /* Explained:
             * in order:
             * Harp < Lapharp < Lute
             * Whatever your main instrument ([play instrument harp) is set to, you add a "+" or "-" or two of them to play the note from the instrument to the left or right of your main selected.
             * So if harp were my set instrument, and i wanted a note from the lute, i'd add ++.
             * If Lute were my set instrument, and i wanted a note from the Harp, i'd add --.
             * If Lute were my set instrument, and i wanted a note from the Lapharp, i'd add -.
             * If lapharp were my set instrument, and i would only need a single + or - to play the other instruments.
             */
            if (string.IsNullOrEmpty(sound) || sound.Length < 2 || (sound[0] != '+' && sound[0] != '-'))
                return false;

            int pluses = 0;
            int minuses = 0;
            int chars = 0;
            bool havenote = false;
            for (int i = 0; i < sound.Length; i++)
            {
                if (char.IsLower(sound[i]))
                {
                    havenote = true;
                    chars++;
                }
                else if (sound[i] == '+')
                {
                    if (havenote || minuses > 0)
                        return false;
                    pluses++;
                    direction++;
                }
                else if (sound[i] == '-')
                {
                    if (havenote || pluses > 0)
                        return false;
                    minuses++;
                    direction--;
                }
                else return false;
            }
            if (pluses > 2 || minuses > 2 || chars > 3 || direction == 0 || Math.Abs(direction) > 2)
                return false;

            return true;
        }
        private static bool ParseSelector(string sound, out int direction)
        {
            return IsSelector(sound, out direction);
        }
        public static HashSet<string> validNotes = new HashSet<string>()
        {   "cl", "csl", "d", "ds", "e", "f", "fs", "g", "gs", "a", "as",
            "b", "c", "cs", "dh", "dsh", "eh", "fh", "fsh", "gh", "gsh",
            "ah", "ash", "bh", "ch"
        };
        private static Regex transpose = new Regex(@"\[([+-])(\d\d?)\]", RegexOptions.Compiled);
        private static Regex tempo = new Regex(@"\<([+*-\/])([0-9]*[.]?[0-9]+)\>", RegexOptions.Compiled);
        private static Regex pause = new Regex(@"^([-]?)([0-9]*[\.]?[0-9]+)$", RegexOptions.Compiled);
        public static string ExpandMacros(Mobile m, string musicString)
        {
            // first for a quick test
            int first;
            if ((first = musicString.IndexOf('%')) == -1)
                // no macros here
                return musicString;

            Queue<int> positions = new Queue<int>();
            for (int ix = 0; ix < musicString.Length; ix++)
                if (musicString[ix] == '%')
                    // record it
                    positions.Enqueue(ix);

            List<string> macros = new List<string>();
            while (positions.Count > 0)
            {
                int head, tail;
                head = positions.Dequeue();
                if (positions.Count == 0)
                {   // error in macro format
                    m.SendMessage("Error: ill-formed macro.");
                    return null;
                }
                tail = positions.Dequeue();
                macros.Add(musicString.Substring(head + 1, tail - head - 1));
            }
            ///
            // process decorations
            ///
            for (int ix = 0; ix < macros.Count; ix++)
            {
                string macro = macros[ix];
                string raw = Raw(macro);    // macro name without decorations
                if (!MusicConfig[m].Macros.ContainsKey(raw))
                {
                    m.SendMessage(string.Format("Error: Unknown macro '{0}'.", macro));
                    return null;
                }

                // this is the macro we will apply transformations on
                string modified_macro = MusicConfig[m].Macros[raw];

                // macro B7 =  b dsh fsh ah
                // - reversing([play % -B7 % would do [play a fs ds b)
                if (macro[0] == '-')
                {   // simple reverse
                    modified_macro = ReverseWords(modified_macro);
                    MusicConfig[m].ComplexityPoints++;
                }

                // - adjusting tempo([play % blues < *2 >% would multiply all pauses by 2, [play % blues < -0.05 >% could subtract 0.05 from all pauses)
                var match_tempo = tempo.Match(macro);
                if (match_tempo.Success)
                {
                    string text = match_tempo.Groups[0].Value;
                    string op = match_tempo.Groups[1].Value;
                    string val = match_tempo.Groups[2].Value;
                    object[] chunks = modified_macro.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int jx = 0; jx < chunks.Length; jx++)
                    {
                        var match_pause = pause.Match(chunks[jx] as string);
                        if (!match_pause.Success)
                            continue;

                        string opx = match_pause.Groups[1].Value;
                        string number = match_pause.Groups[2].Value;
                        double d;
                        if (double.TryParse(number, out d))
                            ;

                        double valx;
                        if (double.TryParse(val, out valx))
                            ;

                        switch (op)
                        {
                            case "+":
                                {
                                    chunks[jx] = opx + (d + valx).ToString();
                                    break;
                                }
                            case "-":
                                {
                                    chunks[jx] = opx + (d - valx).ToString();
                                    break;
                                }
                            case "*":
                                {
                                    chunks[jx] = opx + (d * valx).ToString();
                                    break;
                                }
                            case "/":
                                {
                                    chunks[jx] = opx + (d / valx).ToString();
                                    break;
                                }
                        }

                    }

                    string temp = string.Empty;
                    for (int kx = 0; kx < chunks.Length; kx++)
                        temp += chunks[kx].ToString() + " ";

                    modified_macro = temp.Trim();
                    MusicConfig[m].ComplexityPoints++;
                }

                // -transposing([play % blues[-1] %) would transpose all of the notes in % blues % down by 1 halfstep(c becomes b, ds becomes d, etc)
                var match_transpose = transpose.Match(macros[ix]);
                if (match_transpose.Success)
                {
                    string opx = match_transpose.Groups[1].Value;
                    string number = match_transpose.Groups[2].Value;
                    string[] chunks = modified_macro.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int mx = 0; mx < chunks.Length; mx++)
                    {
                        int index = Array.IndexOf(NoteLookup, chunks[mx]);
                        if (index == -1)
                            // not a note
                            continue;

                        int offset;
                        int.TryParse(number, out offset);

                        // still need instrument wraparound
                        index += (opx == "+" ? offset : -offset);
                        if (index < 0 || index >= NoteLookup.Length)
                            chunks[mx] = string.Empty;  // drop the note
                        else
                            chunks[mx] = NoteLookup[index];
                    }

                    string temp = string.Empty;
                    for (int kx = 0; kx < chunks.Length; kx++)
                        temp += chunks[kx].ToString() + " ";

                    modified_macro = temp.Trim();
                    MusicConfig[m].ComplexityPoints++;
                }

                // replace the text verbatim
                musicString = musicString.Replace("%" + macros[ix] + "%", modified_macro);
            }
            m.SendMessage(musicString);
            Utility.Monitor.WriteLine("macro: {0}", ConsoleColor.Cyan, musicString);
            return musicString;
        }
        private static string[] NoteLookup = new string[]
        {
            "cl",
            "csl",
            "d",
            "ds",
            "e",
            "f",
            "fs",
            "g",
            "gs",
            "a",
            "as",
            "b",
            "c",
            "cs",
            "dh",
            "dsh",
            "eh",
            "fh",
            "fsh",
            "gh",
            "gsh",
            "ah",
            "ash",
            "bh",
            "ch"
        };
        public static string ReverseWords(string s)
        {
            string[] wordArray = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Array.Reverse(wordArray);
            string temp = string.Empty;
            foreach (string word in wordArray)
                temp += word + " ";
            return new string(temp);
        }
        public static string Raw(string s)
        {
            string raw = string.Empty;
            bool first = true;
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c))
                    raw += c;
                else if (first == false)    // handles the leading '-' for reversing
                    return raw;         // we've started decorations, just return what we have
                first = false;
            }
            return raw;                     // no decorations beyond maybe a leading '-' for reversing
        }
        public static bool IsVolume(string text)
        {
            // v[0-99]
            // Although i think the effective volumn cuts off at about 15, so any thing above that will be clipped
            if (!string.IsNullOrEmpty(text) && (text.Length == 2 || text.Length == 3) && text.ToLower()[0] == 'v' && char.IsDigit(text[1]))
                if (text.Length == 2) return true;
                else if (text.Length == 3) return char.IsDigit(text[2]);

            return false;
        }
        [Usage("Play note|pause [note|pause]")]
        [Description("Plays a note or a series of notes and pauses.")]
        public static void Play_OnCommand(CommandEventArgs e)
        {   // called via the [play command on the commandline
            Player(e.Mobile, e.ArgString, true);
        }
        public static void Player(Mobile m, string musicString, bool IsCommandLine = false, bool bSilent = false)
        {   // API called via RSM

            // lets see who's using the music system
            if (Utility.Random(10) == 0)
                if (IsCommandLine)
                    Utility.Monitor.DebugOut(string.Format("{0} composing music.", m), ConsoleColor.Cyan);
                else
                    Utility.Monitor.DebugOut(string.Format("{0} is listening to music.", m), ConsoleColor.Cyan);

            // Allows dynamic control through the CoreManagementConsole.
            if (m.AccessLevel < CoreAI.PlayAccessLevel)
            {
                m.SendMessage("Playing music is currently disabled.");
                return;
            }
            if (string.IsNullOrEmpty(musicString) || string.IsNullOrWhiteSpace(musicString))
            {
                m.SendMessage("Error: ill-formed [play command.");
                return;
            }

            // initialize player context for this mobile
            InitializeContext(m);

            // setup the source for this music
            if (MusicConfig[m].Source == null)
                MusicConfig[m].Source = m;

            // expand macros
            if ((musicString = ExpandMacros(m, musicString)) == null)
                // something went wrong with macro expansion, the user has already been informed
                return;

            // chop up the string into 'notes'
            string[] notes = musicString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Object LastItem = null;

            // keep track of how many times we've seen this line and scale gains accordingly
            int gain_offset = 0;
            if (IsCommandLine)
                gain_offset = GetGainOffset(m, musicString);

            // process configure options ... don't try and 'play' them
            if (DoConfigure(m, notes, bSilent) == true)
                return;

            // used to unlock special sound effects
            int musicKarma = 1000;                          // rolled up sheet music will play sound effects
            if (IsCommandLine)
                musicKarma = MusicBox.GetMusicKarma(m);     // composers won't be able to use them intil music karma is >= 1000

            // check for too many notes.
            if (MusicConfig[m].PlayList.Count > MaxQueueSize)
            {
                m.SendMessage("Warning: Max queued notes reached.");
                return;
            }

            // record this session (if the recorder is turned on)
            Engines.MusicRecorder.Record(m, musicString);

            // points for each note
            MusicConfig[m].ObjectCount += notes.Length;

            for (int i = 0; i < notes.Length; ++i)
            {
                string note = notes[i].ToLower();
                double d;
                // If the argument is a note or a sound, add it directly to the queue.
                //  You need 1000 music karma to play sound effects
                if (IsSound(note) && musicKarma < 1000)
                {
                    m.SendMessage("Warning: You do not yet have sufficient CoreMusicPlayer Karma to play sound effects.");
                    continue;
                }
                // it's a play object
                else if (validNotes.Contains(note) || IsSound(note) || IsSelector(note) || IsVolume(note))
                {
                    // Detect repeated notes
                    if (!MusicConfig[m].NewTimer)
                        if (MusicConfig[m].PlayList.Count > 0 && LastItem is String && ((String)LastItem).ToLower() == note && MusicConfig[m].MusicChatter)
                            m.SendMessage("Warning: Repeated note detected. Some notes may not play. Insert a 0.3 pause between repeated notes.");
                    MusicConfig[m].PlayList.Enqueue(note);
                    LastItem = note;
                    continue;
                }
                // it's a pause
                else if (double.TryParse(note, out d) == true)
                {
                    if (!MusicConfig[m].NewTimer)
                        // classic timer only supports pauses of this range
                        if (!(d >= 0.0 && d <= 1.0))
                        {
                            Usage(m);
                            return;
                        }

                    MusicConfig[m].PlayList.Enqueue(d); // If so, add it to the queue as a double.
                    LastItem = note;
                    continue;
                }
                else
                {
                    Usage(m);
                    return;
                }
            }

            // did we get anything to play?
            if (MusicConfig[m].PlayList.Count == 0)
            {
                Usage(m);
                return;
            }

            // process the queue
            ProcessPlayQueue(m);

            // if they made it this far, we will award some points for composition
            // do they gain a point?
            if (m is PlayerMobile pm && pm.NpcGuild == NpcGuild.BardsGuild)
            {   // 1. must come in on a commandline which indicates 'composition' and not a replay of RSM.
                // 2. make gains more difficult the more times we've seen this line.
                //  we count the times we have seen this line (gain_offset) and add that the the random chance 1-in-10
                //  so gains get progressively the more repeat lines we see (anti-macro)
                if (IsCommandLine && Utility.Random(10 + gain_offset) == 0)
                {   // how gains are calculated:
                    //  they get 1 point per note, capped at 10.
                    //  they get a 1/2 point for each complex musical construct (music algebra)
                    //  We then use AdjustedGain to reduce these points to some fractional value based on their current skill level.
                    //  Example: At Novice they are recveiving nearly full point credits, and nearing Legendary, they are recveiving only 1/100 of a point
                    double point_gain = MusicConfig[m].ObjectCount * 0.5;
                    // factor in the bonus for using music algebra
                    point_gain += (MusicConfig[m].ComplexityPoints * .5);
                    // max out at 10 points
                    // point_gain = Math.Min(point_gain, 10.0);
                    double adjusted_gain = AdjustedGain(pm.NpcGuildPoints, point_gain);
                    pm.NpcGuildPoints += adjusted_gain;
                    OnSkillChange(m, adjusted_gain);
                }
                //Console.WriteLine("Chance to gain is 1/{0}", 10 + gain_offset);
            }
            MusicConfig[m].ObjectCount = MusicConfig[m].ComplexityPoints = 0;
        }

        /*
         * DifficultyTable will remain only until we have nailed down the skill gain formula.
         * Once set in stone, we can delete this.
         * PS. I think we are very close now.
         */
        public class DifficultyTable
        {
            public double[] Table;
            public DifficultyTable()
            {   // I think those could be reduced to 0.1 after gm,
                //  and the gains rates for all levels after 100 reduced to 1/10 of their present levels
                List<double> dlist = new List<double>();
                for (double ix = 0.001; ix <= 1.0; ix += 0.01)
                    dlist.Add(ix);

                Table = dlist.ToArray();
                Array.Reverse(Table);
            }
        }
        private static DifficultyTable DifficultyLookup = new DifficultyTable();

        /*	** OSI MODEL **
			1.  Legendary 120 
			2.  Elder 110 
			3.  Grandmaster 100 
			4.  Master 90 
			5.  Adept 80 
			6.  Expert 70 
			7.  Journeyman 60 
			8.  Apprentice 50 
			9.  Novice 40 
			10. Neophyte 30 
			No Title 29 or below 
		 */
        public static double AdjustedGain(in double current_skill, double proposed_gain)
        {
            /*LogHelper logger = new LogHelper("'991-'001.log", true, true);
            for (int ix=0; ix < DifficultyLookup.Table.Count(); ix++)
            {
                logger.Log(string.Format("Table[{0:D2}]={1:N3}", ix.ToString(), DifficultyLookup.Table[ix]));
            }
            logger.Finish();*/
            double gain = 0;
            if (current_skill < 10)
                gain = AdjustedGain(10.0, proposed_gain);
            // very quick gains, nearly full point gains
            else if (current_skill < 200)   // No Title 29 or below 
                gain = proposed_gain * DifficultyLookup.Table[0];
            else if (current_skill < 300)   // Neophyte 30 
                gain = proposed_gain * DifficultyLookup.Table[50];
            else if (current_skill < 400)   // Novice 40 
                gain = proposed_gain * DifficultyLookup.Table[70];
            else if (current_skill < 500)   // Apprentice 50 
                gain = proposed_gain * DifficultyLookup.Table[83];
            else if (current_skill < 600)   // Journeyman 60 
                gain = proposed_gain * DifficultyLookup.Table[84];
            else if (current_skill < 700)   // Expert 70 
                gain = proposed_gain * DifficultyLookup.Table[85];
            else if (current_skill < 800)   // Adept 80 
                gain = proposed_gain * DifficultyLookup.Table[86];
            else if (current_skill < 900)   // Master 90 
                gain = proposed_gain * DifficultyLookup.Table[95];
            else if (current_skill < 1000)  // Grandmaster 100 
                gain = proposed_gain * DifficultyLookup.Table[96];
            else if (current_skill < 1100)  // Elder 110 
                gain = proposed_gain * DifficultyLookup.Table[97];
            else if (current_skill < 1190)
                gain = proposed_gain * DifficultyLookup.Table[98];
            // very slow, 0.001 of point gains
            else if (current_skill < 1200)  // Legendary 120 
                gain = proposed_gain * DifficultyLookup.Table[99];

            // we allow the user to keep collecting points, we may use them later.
            //  Note the MusicBox is similar in that it continues to add whole, unmodified points for Sales, Plays, and Publications
            else if (current_skill >= 1200)
                gain = proposed_gain * DifficultyLookup.Table[99];

            return gain;
        }
        private static int GetGainOffset(Mobile m, string line)
        {
            // keep track of how many times we've seen this line and scale gains accordingly
            //  special note: I wanted to 'trim' the database here, but it seemed to lag the player so 
            //  I moved it to a Pack() routine called during World Save

            ///////////////////////////
            // This is really fast
            // Time taken 00:00:00.0000013
            //Stopwatch Timer = new Stopwatch();
            //Timer.Start();

            int hash = Utility.GetStableHashCode(line, version: 1);
            if (MusicConfig[m].SameLine.ContainsKey(hash))
                MusicConfig[m].SameLine[hash]++;
            else
                MusicConfig[m].SameLine.Add(hash, 0);

            //Timer.Stop();
            //Console.WriteLine("Time taken {0}", Timer.Elapsed);
            ///////////////////////////

            //Console.WriteLine("SameLine depth {0}", MusicConfig[m].SameLine.Count);

            // return the number of times we've seen this line
            return MusicConfig[m].SameLine[hash];
        }
        public static void OnSkillChange(Mobile m, double points_gained)
        {
            double real = (m as PlayerMobile).NpcGuildPoints;                                                   // actual real skill points
            double before_change = real - points_gained;                                                        // points before the gain
            MusicConfig[m].LastGain = MusicConfig[m].LastGain == 0 ? before_change : MusicConfig[m].LastGain;   // on server restart, seed last gain with a reasonable value
            double delta = real - MusicConfig[m].LastGain;                                                      // how many points gained since last gain
            double display = ((real > 1200) ? 1200 : real) / 10.0;                                              // total display points
            if (before_change < 1200)
                if (delta >= .1 || display == 120)
                    m.SendMessage("Your skill in CoreMusicPlayer Composition increased. It is now {0:N1}", display);
            // else, don't show sub .1 gains
            Utility.Monitor.DebugOut(string.Format("{1}: skill is now {0}", display, m), ConsoleColor.Cyan);
            if (real >= 1200)
            {
                Utility.Monitor.DebugOut(string.Format("{1}: skill is now {0}", display, m), ConsoleColor.Red);
                if (MusicConfig[m].SkillTesting)
                {
                    Utility.Monitor.DebugOut(string.Format("End test at {0}", DateTime.UtcNow), ConsoleColor.Red);
                    MusicConfig[m].SkillTesting = false;
                }
            }
            MusicConfig[m].LastGain = real;                                                                     // accumulator for point gains < .1
        }
        public static void ProcessPlayQueue(Mobile from)
        {
            Mobile pm = (Mobile)from;
            int tempo = MusicConfig[pm].Tempo;
            bool newTimer = MusicConfig[pm].NewTimer;
            bool prefetch = MusicConfig[pm].Prefetch;
            bool musicChatter = MusicConfig[pm].MusicChatter;

            if (!MusicConfig[pm].Playing) // If this is a new tune, create a new timer and start it.
            {
                if (musicChatter)
                    pm.Emote("*plays a tune*"); // Player emotes to indicate they are playing
                MusicConfig[pm].Playing = true;
                if (newTimer == true)
                {
                    //Utility.ConsoleOut("new timer model", ConsoleColor.Cyan);
                    TimerStateCallback callback = new TimerStateCallback(DynamicTimer);
                    Timer.DelayCall(TimeSpan.FromMilliseconds(tempo), callback, new object[] { pm, callback, tempo, prefetch });
                }
                else
                {
                    //Utility.ConsoleOut("old timer model", ConsoleColor.Cyan);
                    //if (instrument is RazorInstrument)
                    //Utility.ConsoleOut("at time start, instrument is {0}", ConsoleColor.Cyan, (instrument as RazorInstrument).GetInstrumentType().ToString());
                    ClassicTimer pt = new ClassicTimer(pm);
                    pt.Start();
                }
            }
            else
            {
                //Utility.ConsoleOut("Queueing while playing. Backlog: {0}", ConsoleColor.Cyan, MusicConfig[pm].PlayList.Count);
            }
        }
        private static int NoteCount(string[] notes)
        {
            int count = 0;
            foreach (string note in notes)
                if (char.IsLetter(note[0]))
                    count++;
            return count;
        }
        public static void Usage(Mobile to)
        {
            to.SendMessage("Usage: [play note|pause [note|pause] ...");
            // you messed up .. you lose
            // Reset the [play context variables
            MusicConfig[to].ComplexityPoints = 0;
            MusicConfig[to].ObjectCount = 0;
        }

        public class ClassicTimer : Timer
        {
            private Mobile m_Mobile;
            public DateTime m_PauseTime;

            public ClassicTimer(Mobile pm)
                : base(TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(0.1), 0)
            {
                m_Mobile = pm;
                Priority = TimerPriority.FiftyMS;
                m_PauseTime = DateTime.UtcNow;
            }

            protected override void OnTick()
            {
                // we can't use MusicConfig[].VirtualInstrument past this point since we may be using
                //  instrument offsets, that is, instruments relative to the VirtualInstrument. I.e., -cl +cl
                BaseInstrument instrument = null;

                // let the caller know we are still running
                if (MusicConfig[m_Mobile].Tick != null)
                    MusicConfig[m_Mobile].Tick(m_Mobile, SystemMusicPlayer.MusicConfig[m_Mobile].Source);

                // wait on pause
                if (DateTime.UtcNow < m_PauseTime)
                    return;

                if (MusicConfig[m_Mobile].PlayList.Count == 0) // If the tune is done, stop the timer.
                {
                    MusicConfig[m_Mobile].Playing = false;
                    Stop();
                    return;
                }
                else
                {
                    try
                    {
                        // always select the default instrument before proceeding
                        MusicContext mo = MusicConfig[m_Mobile];
                        mo.SelectInstrument();
                        instrument = mo.VirtualInstrument;
                        // dequeue an object
                        object obj = MusicConfig[m_Mobile].PlayList.Dequeue();

                        // switch instruments now?
                        if (obj is RazorInstrument.InstrumentSoundType)
                        {
                            mo = MusicConfig[m_Mobile];
                            mo.Selector = (RazorInstrument.InstrumentSoundType)obj;
                            mo.SelectInstrument();
                            instrument = mo.VirtualInstrument;
                            //Utility.ConsoleOut("Instrument {0}", ConsoleColor.Red, mo.Selector.ToString());
                            OnTick();
                            return;
                        }
                        else if (obj.GetType() == typeof(string) && IsVolume(obj as string))
                        {
                            int volume;
                            int.TryParse((obj as string).Substring(1), out volume);
                            MusicConfig[m_Mobile].Volume = volume;
                            OnTick();
                            return;
                        }
                        else if (obj.GetType() == typeof(string) && IsSelector(obj as string))
                        {
                            int index;
                            if (ParseSelector(obj as string, out index) && (instrument = MusicConfig[m_Mobile].GetOffsetInstrument(index)) != null)
                            {
                                // okay, now fixup the note by stripping away all selector decorations
                                obj = (obj as string).Replace("+", "").Replace("-", "").Trim();
                                if (!validNotes.Contains(obj as string))
                                {
                                    m_Mobile.SendMessage("Error: bad note{0}.", obj as string);
                                    return;
                                }
                            }
                            else
                            {
                                // bad selector. 
                                m_Mobile.SendMessage("Error: bad instrument offset.");
                                return;
                            }
                        }

                        // okay, play the note!
                        if (obj.GetType() == typeof(string))
                        {
                            CoreMusicPlayer.PlayNote(m_Mobile, (string)obj, instrument);
                            //Utility.ConsoleOut("Note {0}", ConsoleColor.Red, (string)obj);
                        }
                        // If the first item is a double, treat it as a pause.
                        else if (obj is double)
                        {
                            double pause = (double)obj;
                            m_PauseTime = DateTime.UtcNow + TimeSpan.FromSeconds(pause);
                            return;
                        }
                        else
                            Utility.Monitor.WriteLine("Error: Unknown object type {0}", ConsoleColor.Red, (string)obj);
                    }
                    catch (Exception ex)
                    {
                        Server.Diagnostics.LogHelper.LogException(ex);
                    }
                }
            }
        }
        private static void DynamicTimer(object state)
        {
            object[] aState = (object[])state;
            Mobile player = (Mobile)aState[0];                              // mobile playing this song
            TimerStateCallback callback = (TimerStateCallback)aState[1];    // this routine
            TimeSpan tempo = TimeSpan.FromMilliseconds((int)aState[2]);     // tempo
            bool prefetch = (bool)aState[3];                                // prefetch next timer delay
            TimeSpan delay = tempo;                                         // default delay = tempo

            // we can't use MusicConfig[].VirtualInstrument past this point since we may be using
            //  instrument offsets, that is, instruments relative to the VirtualInstrument. I.e., -cl +cl
            BaseInstrument instrument = null;

            // incase we defraged the database while a timer was running
            if (!MusicConfig.ContainsKey(player))
                return;

            // let the caller know we are still running
            if (MusicConfig[player].Tick != null)
                MusicConfig[player].Tick(player, SystemMusicPlayer.MusicConfig[player].Source);

            if (MusicConfig[player].PlayList.Count == 0) // If the tune is done, stop the timer.
            {
                MusicConfig[player].Playing = false;
                return;
            }
            else
            {
                try
                {
                    // always select the default instrument before proceeding
                    MusicContext mo = MusicConfig[player];
                    mo.SelectInstrument();
                    instrument = MusicConfig[player].VirtualInstrument;

                    // dequeue an object to play
                    object obj = MusicConfig[player].PlayList.Dequeue();

                    // switch instruments now
                    if (obj is RazorInstrument.InstrumentSoundType)
                    {
                        mo = MusicConfig[player];
                        mo.Selector = (RazorInstrument.InstrumentSoundType)obj;
                        mo.SelectInstrument();
                        instrument = MusicConfig[player].VirtualInstrument;
                        DynamicTimer(state);
                        return;
                    }
                    else if (obj.GetType() == typeof(string) && IsVolume(obj as string))
                    {
                        int volume;
                        int.TryParse((obj as string).Substring(1), out volume);
                        MusicConfig[player].Volume = volume;
                        DynamicTimer(state);
                        return;
                    }
                    else if (obj.GetType() == (typeof(string)) && IsSelector(obj as string))
                    {
                        int index;
                        if (ParseSelector(obj as string, out index) && (instrument = MusicConfig[player].GetOffsetInstrument(index)) != null)
                        {
                            // okay, now fixup the note by stripping away all selector decorations
                            obj = (obj as string).Replace("+", "").Replace("-", "").Trim();
                            if (!validNotes.Contains(obj as string))
                            {
                                player.SendMessage("Error: bad note{0}.", obj as string);
                                goto skip;
                            }
                        }
                        else
                        {
                            // bad selector. 
                            player.SendMessage("Error: bad instrument offset.");
                            goto skip;
                        }

                    }

                    // play the note!
                    if (obj.GetType() == (typeof(string)))
                    {
                        // play the note
                        CoreMusicPlayer.PlayNote(player, (string)obj, instrument);
                        //Utility.ConsoleOut("note {0}", ConsoleColor.Green, (string)obj);

                        bool fauxPrefetch = false;
                        // faux prefetch negative pauses
                        if (MusicConfig[player].PlayList.Count != 0 && MusicConfig[player].PlayList.Peek() is double)
                        {
                            if ((double)MusicConfig[player].PlayList.Peek() < 0)
                                fauxPrefetch = true;
                        }

                        // now look ahead to see if the next element is a pause. If so  we should process it now.
                        if (MusicConfig[player].PlayList.Count != 0 && (prefetch || fauxPrefetch))
                        {
                            if (MusicConfig[player].PlayList.Peek() is double)
                            {
                                obj = MusicConfig[player].PlayList.Dequeue();
                                double offset = (double)obj * 1000;
                                if (fauxPrefetch)
                                    offset = Math.Abs(offset);
                                double pause = offset;

                                //Utility.ConsoleOut("(p)delay {0}", ConsoleColor.Cyan, delay.TotalMilliseconds);
                                //Utility.ConsoleOut("(p)offset {0}", ConsoleColor.Cyan, offset);
                                //Utility.ConsoleOut("(p)pause {0}", ConsoleColor.Cyan, pause);

                                delay = TimeSpan.FromMilliseconds(pause);
                            }
                        }
                    }
                    // If the first item is a double, treat it as a pause.
                    else if (obj is double)
                    {
                        double offset = (double)obj * 1000;
                        double pause = offset;

                        //Utility.ConsoleOut("delay {0}", ConsoleColor.Cyan, delay.TotalMilliseconds);
                        //Utility.ConsoleOut("offset {0}", ConsoleColor.Cyan, offset);
                        //Utility.ConsoleOut("pause {0}", ConsoleColor.Cyan, pause);

                        delay = TimeSpan.FromMilliseconds(pause);
                    }
                    else
                        Utility.Monitor.WriteLine("Error: Unknown object type {0}", ConsoleColor.Red, (string)obj);
                }
                catch (Exception ex)
                {
                    Server.Diagnostics.LogHelper.LogException(ex);
                }
            }

        skip:
            Timer.DelayCall(delay, callback, new object[] { player, callback, (int)tempo.TotalMilliseconds, prefetch });
        }

        [Usage("[StopMusic")]
        [Description("Stops a current melody.")]
        public static void StopMusic_OnCommand(CommandEventArgs e)
        {
            Mobile pm = (Mobile)e.Mobile;
            if (MusicConfig[pm].PlayList == null) pm.SendMessage("You are not playing anything.");
            else
            {
                MusicConfig[pm].PlayList.Clear();
                MusicConfig[pm].Playing = false;
                pm.SendMessage("Music stopped.");
            }
        }

        [Usage("[FilterMusic")]
        [Description("Toggles the ability to hear music")]
        public static void FilterMusic_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                bool filter = (pm as PlayerMobile).FilterMusic;
                if (!filter) pm.SendMessage("You are now filtering music.");
                else pm.SendMessage("You are no longer filtering music.");
                (pm as PlayerMobile).FilterMusic = !filter;
            }
        }

        #region Global Music Context Serialization
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Pack);
        }
        public static void Load()
        {
            if (!File.Exists("Saves/MusicContexts.bin"))
                return;

            Console.WriteLine("Global music contexts Loading...");
            BinaryFileReader reader = null;
            try
            {
                reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/MusicContexts.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadEncodedInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                if (m != null)
                                    MusicConfig.Add(m, new MusicContext(reader));
                                else
                                {
                                    // throw it away
                                    MusicContext temp = new MusicContext(reader);
                                    // free up instruments
                                    temp.Finish();
                                }
                            }
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid MusicContexts.bin savefile version.");
                        }
                }
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Saves/MusicContexts.bin, using default values...", ConsoleColor.Red);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Global music contexts Saving...");
            BinaryFileWriter writer = null;
            try
            {
                writer = new BinaryFileWriter("Saves/MusicContexts.bin", true);
                int version = 0;
                writer.WriteEncodedInt(version);

                switch (version)
                {
                    case 0:
                        {
                            writer.WriteEncodedInt(MusicConfig.Count);
                            foreach (KeyValuePair<Mobile, MusicContext> kvp in MusicConfig)
                            {
                                writer.Write(kvp.Key);
                                kvp.Value.Serialize(writer);
                            }

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Utility.Monitor.WriteLine("Error writing Saves/MusicContexts.bin", ConsoleColor.Red);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }
        #endregion Global Music Context Serialization
    }

}