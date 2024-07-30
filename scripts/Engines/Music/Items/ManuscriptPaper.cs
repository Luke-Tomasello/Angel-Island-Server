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

/* Scripts/Engines/Music/ManuscriptPaper.cs
 * Changelog
 *  3/3/22, Adam (Music Karma / IsSound)
 *      Don't allow the creation of Rolled Up Sheet music if the author is using sound effects and doesn't have sufficient Music Karma.
 *  2/26/22, Adam (ConcertMode)
 *      Add support for ConcertMode
 *  2/19/22, (Napster)
 *      Add support for new Napster style music sharing
 *  2/4/22, Adam
 *      Add support for prefetch (need to pass it to the output RolledUpSheetMusic)
 *  1/27/22, Adam (CompileHashCode)
 *      Make CompileHashCode public so Nuke can use it to patch existing music
 *      Update CompileHashCode so that it adds a small amount of copy protection to music.
 *      Copy Protection: we only hash every mod 5th byte, so players can't recreate someone's work, change one note/pause
 *          and call it their own (well, they would need to be lucky and change mod 5th byte.)
 *  1/25/22, Adam
 *      Automatically parse Author, Title, Pricing, and Instrument from RZR file. 
 *      We then pass this metadata to the RolledUpSheetMusic.
 *  1/13/22, Adam (TrueLineParser)
 *      Add the TrueLineParser to fix all the funk linebreaks you get when pasting into a book.
 *  1/12/22, Adam
 *		Initial creation.
 */
using Server.Misc;
using Server.Targeting;
using System;
using System.Text.RegularExpressions;

namespace Server.Items
{
    public class ManuscriptPaper : Item
    {
        [Constructable]
        public ManuscriptPaper()
            : this(1)
        {
        }

        [Constructable]
        public ManuscriptPaper(int amount)
            : base(0xE34 /*0xEF3 has magically odd behavior*/)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
            Name = "manuscript paper";
        }

        public ManuscriptPaper(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("Target the music composition book to transcribe...");
            from.Target = new LocationTarget(this); // Call our target
        }

        public class LocationTarget : Target // Create our targeting class (which we derive from the base target class)
        {
            private ManuscriptPaper m_manuscript;
            public LocationTarget(ManuscriptPaper manuscript)
                : base(2, false, TargetFlags.None)
            {
                m_manuscript = manuscript;
            }

            protected override void OnTarget(Mobile from, object target)
            {   // added owner for clarity only
                Mobile owner = from;
                if (target is MusicCompositionBook mcb)
                {
                    if (mcb != null)
                    {

                        // read the books text
                        string lines = string.Empty;
                        for (int ix = 0; ix < mcb.PagesCount; ix++)
                        {
                            for (int mx = 0; mx < mcb.Pages[ix].Lines.Length; mx++)
                            {
                                lines += string.Format("{0}", mcb.Pages[ix].Lines[mx]) + Environment.NewLine;
                            }
                        }
                        // make sure the book contains something!
                        if (string.IsNullOrEmpty(lines))
                        {
                            from.SendMessage("your music composition book seems to be empty.");
                            return;
                        }

                        // preprocess lines: remove 'Loop', empty lines, 
                        //  and fix 'book induced wrap'
                        lines = PreProcesss(lines);

                        // check for constructs not supported by current music karma
                        if (CheckValidKarma(from, lines) == false)
                        {
                            from.SendMessage("Error: You do not yet have sufficient CoreMusicPlayer Karma to play sound effects.");
                            return;
                        }

                        // parse out title, author, instrument
                        string title, author, instrument;
                        int price;
                        bool newTimer, prefetch, napster, concertmode;
                        int tempo;
                        ProcessDirectives(lines, out title, out author, out price, out instrument, out newTimer, out tempo, out prefetch, out napster, out concertmode);
                        if (title != null)
                            mcb.Title = title;
                        if (author != null)
                            mcb.Author = author;

                        if (mcb.Title == null || mcb.Title.Length == 0 || mcb.Title.ToLower() == "title")
                        {
                            from.SendMessage("your music composition book must have a title.");
                            return;
                        }
                        if (mcb.Author == null || mcb.Author.Length == 0 || mcb.Author.ToLower() == "author")
                        {
                            from.SendMessage("your music composition book must have an author.");
                            return;
                        }
                        if (price > 0 && napster == true)
                        {
                            from.SendMessage("You cannot charge for Napster music.");
                            return;
                        }

                        RolledUpSheetMusic rsm = new RolledUpSheetMusic(owner, TonalQuality(instrument), mcb.Author, mcb.Title, price, lines);
                        rsm.HashCode = CompileHashCode(lines, false);
                        rsm.SongBytes = null;
                        rsm.SongName = title;
                        rsm.Author = author;
                        rsm.ConfigureInstrument(instrument);
                        rsm.NewTimer = newTimer;
                        rsm.Tempo = tempo;
                        rsm.Prefetch = prefetch;
                        rsm.Napster = napster;
                        rsm.ConcertMode = concertmode;

                        if (m_manuscript.Parent is Container c && c != null)
                        {
                            c.AddItem(rsm);
                            if (m_manuscript.Amount == 1)
                                rsm.Location = m_manuscript.Location;
                            m_manuscript.Consume();
                        }
                        else
                        {
                            rsm.MoveToWorld(m_manuscript.Location, m_manuscript.Map);
                            m_manuscript.Consume();
                        }

                    }
                    else
                        from.SendMessage("That is not a music composition book.");
                }
            }

            private string NormalizeString(string lines)
            {
                return Regex.Replace(lines, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
            }
            private string RemoveLoop(string lines)
            {
                return Regex.Replace(lines, "#*loop", string.Empty, RegexOptions.IgnoreCase);
            }

            string PreProcesss(string lines)
            {
                // normalize input
                lines = NormalizeString(lines);

                // comment out 'loop' statements - not supported
                lines = RemoveLoop(lines);

                // remove all 'book induced wrap'
                return TrueLineParser(lines);
            }
            bool CheckValidKarma(Mobile m, string text)
            {
                string[] lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    if (line.ToLower().StartsWith("say '[play"))
                    {
                        string[] chunks = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string chunk in chunks)
                            if (Commands.SystemMusicPlayer.IsSound(chunk) && MusicBox.GetMusicKarma(m) < 1000)
                                return false;
                    }
                }
                return true;
            }
            public int CompileHashCode(string lines, bool parse = true)
            {
                // when called externally, like from Nuke, we need to parse the lines so the format
                // matches that of the rsm created here.
                if (parse == true)
                    lines = PreProcesss(lines);

                // our hash is only the notes.
                //  we don't want someone stealing a composition book, changing the author to themselves, and claiming ownership.
                // If there is a legitimate update, it will (currently) require staff to delete the old track so the new track can be added. 
                // additionally, we 'munge' each line to make it difficult to submit a look alike tune by changing one note (or other element)
                string representation = string.Empty;
                foreach (var line in lines.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    if (line.StartsWith("#")) continue;     // throw away the header and other comments
                    representation += CompileLine(line);      // keep every 5th note (or other line element)
                }

                // normalize the string
                representation = representation.Replace(" ", "").ToLower().Trim();

                // okay, this is our hash
                return Utility.GetStableHashCode(representation, version: 1);
            }
            private string CompileLine(string text)
            {   // munge string: The user will have had to change the mod 5th character(s) in order for the system
                //  to recognize this as a *different* song.
                //  I.e., stealing someone's song and changing one pause or note isn't likely to matter to this system.
                string output = string.Empty;
                int trip = 0;
                foreach (char ch in text)
                    if (trip++ == 0)
                    {   // changing the first character does not change the hash
                        continue;
                    }
                    else if (trip++ % 5 == 0)
                        //  magic character. If changed, hash changes
                        //  i.e., it's a new song
                        output += ch;
                    else
                    {
                        //  changing this character does not change the hash
                        continue;
                    }

                return Utility.GetStableHashCode(output, version: 1).ToString();
            }
            private RazorInstrument.InstrumentSoundType TonalQuality(string instrument)
            {
                if (instrument == null)
                    return RazorInstrument.InstrumentSoundType.Harp;
                if (instrument.ToLower() == "harp")
                    return RazorInstrument.InstrumentSoundType.Harp;
                else if (instrument.ToLower() == "lapharp")
                    return RazorInstrument.InstrumentSoundType.LapHarp;
                else if (instrument.ToLower() == "lute")
                    return RazorInstrument.InstrumentSoundType.Lute;

                return RazorInstrument.InstrumentSoundType.Harp;
            }

            private void ProcessDirectives(string lines, out string title, out string author, out int price, out string instrument, out bool newTimer, out int tempo, out bool prefetch, out bool napster, out bool concertmode)
            {
                title = author = instrument = string.Empty;
                price = 0;
                newTimer = false;
                tempo = Commands.SystemMusicPlayer.DefaultTempo;
                prefetch = false;
                napster = false;
                concertmode = false;
                foreach (var line in lines.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.ToLower().Contains("author:"))
                    {
                        author = line;
                        author = author.Replace("#", "");
                        author = author.Replace("author:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                    }
                    else if (line.ToLower().Contains("napster:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("napster:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"true", RegexOptions.IgnoreCase).Value;
                        if (bool.TryParse(resultString, out napster) == false)
                            napster = false;
                    }
                    else if (line.ToLower().Contains("prefetch:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("prefetch:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"true", RegexOptions.IgnoreCase).Value;
                        if (bool.TryParse(resultString, out prefetch) == false)
                            prefetch = false;
                    }
                    else if (line.ToLower().Contains("concertmode:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("concertmode:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"true", RegexOptions.IgnoreCase).Value;
                        if (bool.TryParse(resultString, out concertmode) == false)
                            concertmode = false;
                    }
                    else if (line.ToLower().Contains("newtimer:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("newtimer:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"true", RegexOptions.IgnoreCase).Value;
                        if (bool.TryParse(resultString, out newTimer) == false)
                            newTimer = false;
                    }
                    else if (line.ToLower().Contains("tempo:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("tempo:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"\d+").Value;
                        if (int.TryParse(resultString, out tempo) == false)
                            tempo = Commands.SystemMusicPlayer.DefaultTempo;
                    }
                    else if (line.ToLower().Contains("instrument:"))
                    {
                        instrument = line;
                        instrument = instrument.Replace("#", "");
                        instrument = instrument.Replace("instrument:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                    }
                    else if (line.ToLower().Contains("price:"))
                    {
                        string sx;
                        sx = line;
                        sx = sx.Replace("#", "");
                        sx = sx.Replace("place:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                        string resultString = Regex.Match(sx, @"\d+").Value;
                        if (int.TryParse(resultString, out price) == false)
                            price = 0;
                    }
                    else if (line.ToLower().Contains("song:"))
                    {
                        title = line;
                        title = title.Replace("#", "");
                        title = title.Replace("song:", "", StringComparison.CurrentCultureIgnoreCase).Trim();
                    }
                }
            }
            private string TrueLineParser(string text)
            {
                string trueLines = string.Empty;
                for (int ndx = 0; ndx < text.Length; ndx++)
                {
                    if (text.Substring(ndx).Length > 2 && text[ndx] == '\r' && text[ndx + 1] == '\n')
                        if (CheckBookWrap(text.Substring(ndx + 2)))
                        {   // still processing the same command, eat the newline
                            ndx += 1;
                            continue;
                        }

                    // the command changed, so we can just output as is.
                    //  until of course, we detect more 'book induced wrap'
                    // output this character
                    trueLines += text[ndx];
                }
                return trueLines;
            }

            private bool CheckBookWrap(string text)
            {
                text = text.ToLower();
                if (text.StartsWith("#"))
                    return false;
                if (text.StartsWith("wait "))
                    return false;
                if (text.StartsWith("say "))
                    return false;

                return true;
            }
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new ManuscriptPaper(amount), amount);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}