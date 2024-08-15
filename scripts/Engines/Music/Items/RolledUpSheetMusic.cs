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

/* Scripts/Engines/Music/RolledUpSheetMusic.cs
 * Changelog
 *  4/24/2024, Adam (TrackID)
 *      Add a trackID for identifying a RSM by title, and author/owner (differs from HashCode which is a hash of the music.)
 *  2/26/22, Adam (ConcertMode)
 *      Add support for ConcertMode
 *  2/18/22, Adam (Napster)
 *      Add the notion of a 'Napster' model for music sharing
 *  2/4/22, Adam
 *      Set the Instrument to 'this' during construction and when playing a file
 *  1/26/22, Adam
 *      Update the hash system to use a persistent and deterministic hash.
 *  1/25/22, Adam
 *      Add the mobile/price to the persistent elements of the RolledUpSheetMusic. This is used to pay the owner when his music is purchased.
 *  1/20/22, Adam
 *      Add IComparable/IEquatable
 *      Set configuration parameters on creation instead of on use.
 *  1/16/22, Adam (Dupe)
 *      Added public properties for Author, Title, and Composition so that completed compositions can be duped.
 *  1/12/22, Adam
 *		Initial creation.
 */
using Server.Items;
using System;

namespace Server.Misc
{
    public class RolledUpSheetMusic : RazorInstrument, IComparable<RolledUpSheetMusic>, IEquatable<RolledUpSheetMusic>
    {
        private static int[] graphicIDs =
            { 
            // 8th
            0x1F65, 0x1F66, 0x1F67, 0x1F68, 0x1F69, 0x1F6A, 0x1F6B, 0x1F6C,
            // 5th
            0x1F4D, 0x1F4E, 0x1F4F, 0x1F50, 0x1F51, 0x1F52, 0x1F53, 0x1F54,
            // 1st
            0x1F2E, 0x1F2F, 0x1F30, 0x1F31, 0x1F32, 0x1F33, 0x1F2D, 0x1F34,
            // 4th
            0x1F45, 0x1F46, 0x1F47, 0x1F48, 0x1F49, 0x1F4A, 0x1F4B, 0x1F4C,
            // 2nd
            0x1F35, 0x1F36, 0x1F37, 0x1F38, 0x1F39, 0x1F3A, 0x1F3B, 0x1F3C,
            // 7th
            0x1F5D, 0x1F5E, 0x1F5F, 0x1F60, 0x1F61, 0x1F62, 0x1F63, 0x1F64,
            // 6th
            0x1F55, 0x1F56, 0x1F57, 0x1F58, 0x1F59, 0x1F5A, 0x1F5B, 0x1F5C,
            // 3rd
            0x1F3D, 0x1F3E, 0x1F3F, 0x1F40, 0x1F41, 0x1F42, 0x1F43, 0x1F44
        };
        Mobile m_owner = null;
        private string m_title;
        private int m_price;
        private bool m_napster;
        private string m_composition;
        private int m_hashCode = 0;
        #region Dupe
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public Mobile Owner { get { return m_owner; } set { m_owner = value; } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public string Title { get { return m_title; } set { m_title = value; } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int Price { get { return m_price; } set { m_price = value; } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool Napster { get { return m_napster; } set { m_napster = value; } }
        public string Composition { get { return m_composition; } set { m_composition = value; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public int HashCode { get { return m_hashCode; } set { m_hashCode = value; } }
        [CommandProperty(AccessLevel.Owner)]
        public int GenHashCode
        {
            get { return m_hashCode; }
            set
            {
                ManuscriptPaper mp = new ManuscriptPaper();
                ManuscriptPaper.LocationTarget lt = new ManuscriptPaper.LocationTarget(mp);
                m_hashCode = lt.CompileHashCode(this.Composition == null ? "" : this.Composition);
            }
        }
        public int TrackID
        {
            get
            {
                string text = string.Empty;
                if (m_title != null)
                    text += m_title;
                if (base.Author != null)
                    text += base.Author;
                if (Owner != null)
                    text += Owner.GUID.ToString();

                return Utility.GetStableHashCode(text.Replace(" ", "").ToLower());
            }
        }
        #endregion Dupe
        #region IComparable/IEquatable and common base overrides
        public int CompareTo(RolledUpSheetMusic rsm)
        {
            return rsm.HashCode - HashCode;
        }

        public bool Equals(RolledUpSheetMusic other)
        {
            if (other == null)
                return false;

            if (this.GetHashCode() == other.GetHashCode())
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            if (this.HashCode == 0)
                return base.GetHashCode();
            return this.HashCode;
        }

        public override string ToString()
        {
            return this.Title;
        }

        #endregion IComparable/IEquatable and common base overrides

        [Constructable]
        public RolledUpSheetMusic()
            : this(1)
        {
        }

        [Constructable]
        public RolledUpSheetMusic(int amount)
            //int itemID, int wellSound, int badlySound, Item playItem
            : base(graphicIDs[Utility.Random(graphicIDs.Length)], (int)RazorInstrument.InstrumentSoundType.Harp, (int)RazorInstrument.InstrumentSoundType.Harp + 1, null)
        {
            Name = "rolled up sheet music";
            Amount = amount;

            IgnoreDirectives_obsolete = true;
            RequiresBackpack = true;
            MusicChatter_doWeNeedThis = false;
            CheckMusicianship_obsolete = true;
            Instrument = this;
        }

        public RolledUpSheetMusic(string author, string title, string composition)
            : this(1)
        {
            base.Author = author;
            m_title = title;
            m_composition = composition;
        }
        public RolledUpSheetMusic(Mobile owner, RazorInstrument.InstrumentSoundType instrumentType, string author, string title, int price, string composition)
            : base(graphicIDs[Utility.Random(graphicIDs.Length)], (int)instrumentType, (int)instrumentType + 1, null)
        {
            Name = "rolled up sheet music";

            IgnoreDirectives_obsolete = true;
            RequiresBackpack = true;
            MusicChatter_doWeNeedThis = false;
            CheckMusicianship_obsolete = true;
            Instrument = this;

            m_owner = owner;
            base.Author = author;
            m_title = title;
            m_price = price;
            m_composition = composition;
        }
        /// <summary>
        /// Overridable. virtual method indicating this temporary item has expired and can be cleaned up by the normal decay/cleanup Cron routines
        /// </summary>
        /// <returns>true if this method can be deleted</returns>
        public override bool CanDelete()
        {
            bool canDelete = base.CanDelete() &&                            // marked as a temp object

                IsInstancePlaying == false;                                 // not actively playing

            return canDelete;
        }
        public override void OnSingleClick(Mobile from)
        {
            if (this.Name != null)
                LabelTo(from, this.Name);
            if (m_title != null)
                LabelTo(from, m_title);
            if (base.Author != null)
                LabelTo(from, base.Author);
        }
        public override void OnDoubleClick(Mobile from)
        {   // pass our song to the base to play
            DebugOut(string.Format("OnDoubleClick in RSM: begin"));
            SongMemory = m_composition;
            Instrument = this;
            DebugOut(string.Format("OnDoubleClick in RSM: SongMemory: {0}", SongMemory?.Substring(0, Math.Min(SongMemory.Length, 25))));
            base.OnDoubleClick(from);
        }

        public RolledUpSheetMusic(Serial serial)
        : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 4;
            writer.Write(version); // version

            switch (version)
            {
                case 4:
                    {
                        // remove m_author_obsolete from versiom 0
                        goto case 3;
                    }
                case 3:
                    {
                        writer.Write(m_napster);
                        goto case 2;
                    }
                case 2:
                    {
                        writer.Write(m_hashCode);
                        goto case 1;
                    }
                case 1:
                    {
                        writer.Write(m_owner);
                        writer.WriteEncodedInt(m_price);
                        goto case 0;
                    }
                case 0:
                    {
                        if (version <= 3)
                            writer.Write(string.Empty);     // m_author_obsolete
                        writer.Write(m_title);
                        writer.Write(m_composition);
                        break;
                    }
            }

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        // remove m_author_obsolete from version 0
                        goto case 3;
                    }
                case 3:
                    {
                        m_napster = reader.ReadBool();
                        goto case 2;
                    }
                case 2:
                    {
                        m_hashCode = reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_owner = reader.ReadMobile();
                        m_price = reader.ReadEncodedInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version <= 3)
                            /*m_author_obsolete =*/
                            reader.ReadString();
                        m_title = reader.ReadString();
                        m_composition = reader.ReadString();
                        break;
                    }
            }
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new RolledUpSheetMusic(amount), amount);
        }
    }
}