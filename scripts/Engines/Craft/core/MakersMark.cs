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

/* Script/Engines/Craft/Core/MakersMark.cs
 * ChangeLog:
 *	4/2/23, Yoar
 *	    Now implements IEquatable.
 *	4/2/22, Yoar
 *		Initial version.
 */

using System;

namespace Server.Engines.Craft
{
    [Parsable]
    [PropertyObject]
    public struct MakersMark : IEquatable<MakersMark>
    {
        public static readonly MakersMark Empty = new MakersMark();

        private Mobile m_Mobile;
        private string m_Name;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Mobile
        {
            get { return m_Mobile; }
            //set { m_Mobile = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name
        {
            get { return m_Name; }
            //set { m_Name = value; }
        }

        public MakersMark(Mobile m, string name)
        {
            m_Mobile = m;
            m_Name = name;
        }

        public static implicit operator MakersMark(Mobile m)
        {
            if (m != null)
                return new MakersMark(m, Utility.Intern(Server.Misc.Titles.FormatShort(m)));

            return new MakersMark();
        }

        public static implicit operator MakersMark(string name)
        {
            return new MakersMark(null, Utility.Intern(name));
        }

        public static implicit operator string(MakersMark mm)
        {
            return mm.ToString();
        }

        public override bool Equals(object obj)
        {
            return (obj is MakersMark && this.Equals((MakersMark)obj));
        }

        public bool Equals(MakersMark other)
        {
            return (m_Mobile == other.m_Mobile && m_Name == other.m_Name);
        }

        public override int GetHashCode()
        {
            SaveFlag flags = GetSaveFlags();

            int result = flags.GetHashCode();

            if (flags.HasFlag(SaveFlag.Mobile))
                result ^= m_Mobile.GetHashCode();

            if (flags.HasFlag(SaveFlag.Name))
                result ^= m_Name.GetHashCode();

            return result;
        }

#if false
        public static bool operator ==(MakersMark l, MakersMark r)
        {
            return (l.m_Mobile == r.m_Mobile && l.m_Name == r.m_Name);
        }

        public static bool operator !=(MakersMark l, MakersMark r)
        {
            return (l.m_Mobile != r.m_Mobile || l.m_Name != r.m_Name);
        }
#endif

        [Flags]
        private enum SaveFlag : byte
        {
            None = 0x0,

            Mobile = 0x1,
            Name = 0x2,
        }

        private SaveFlag GetSaveFlags()
        {
            SaveFlag flags = SaveFlag.None;

            if (m_Mobile != null)
                flags |= SaveFlag.Mobile;

            if (m_Name != null)
                flags |= SaveFlag.Name;

            return flags;
        }

        public bool IsEmpty { get { return (GetSaveFlags() == SaveFlag.None); } }

        public void Serialize(GenericWriter writer)
        {
            SaveFlag flags = GetSaveFlags();

            writer.Write((byte)flags);

            if (flags.HasFlag(SaveFlag.Mobile))
                writer.Write((Mobile)m_Mobile);

            if (flags.HasFlag(SaveFlag.Name))
                writer.Write((string)m_Name);
        }

        public void Deserialize(GenericReader reader)
        {
            SaveFlag flags = (SaveFlag)reader.ReadByte();

            if (flags.HasFlag(SaveFlag.Mobile))
                m_Mobile = reader.ReadMobile();

            if (flags.HasFlag(SaveFlag.Name))
                m_Name = Utility.Intern(reader.ReadString());
        }

        public override string ToString()
        {
            if (m_Name != null)
                return m_Name;

            if (m_Mobile != null)
                return Server.Misc.Titles.FormatShort(m_Mobile);

            return null;
        }

        public static MakersMark Parse(string value)
        {
            return new MakersMark(null, value);
        }
    }
}