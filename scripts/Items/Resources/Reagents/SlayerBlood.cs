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

using System;

namespace Server.Items
{
    public class SlayerBlood : BloodVial
    {
        SlayerName m_slayerName = SlayerName.None;
        [CommandProperty(AccessLevel.GameMaster)]
        public SlayerName SlayerName { get { return m_slayerName; } set { m_slayerName = value; } }
        int m_equipSoundID = 0;
        [CommandProperty(AccessLevel.GameMaster)]
        public int EquipSoundID { get { return m_equipSoundID; } set { m_equipSoundID = value; } }
        [Flags]
        public enum Config : uint
        {
            None = 0x0,
            Super = 0x01,
            something = 0x02,
        }
        private Config m_properties;
        [CommandProperty(AccessLevel.GameMaster)]
        public Config Properties { get { return m_properties; } set { m_properties = value; } }

        private SlayerBlood(Mobile m, SlayerName name, int soundID, Config properties, int amount)
            : base(0xF7D)
        {
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
            m_slayerName = name;
            m_equipSoundID = soundID;
            m_properties = properties;
            if (m == null)
                Name = "Blood";
            else if (m_slayerName == SlayerName.None)
                Name = "unknown blood";
            else
                Name = m.GetType().Name.ToString() + " blood";
        }
        private SlayerBlood(SlayerName slayerName, int soundID, Config properties, int amount)
            : this(null, slayerName, soundID, properties, amount)
        {
        }

        [Constructable]
        public SlayerBlood()
            : this(null, SlayerName.None, 0, Config.None, 1)
        {
        }
        public SlayerBlood(Mobile m, SlayerName slayerName, int soundID, int amount)
            : this(m, slayerName, soundID, Config.None, amount)
        {

        }

        public SlayerBlood(Serial serial)
            : base(serial)
        {
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);
            if ((m_properties & Config.Super) == Config.Super)
                LabelTo(from, string.Format("(Semi-rare)"));
        }
        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (dropped is SlayerBlood sb)
                if (sb.SlayerName == this.m_slayerName && sb.EquipSoundID == this.m_equipSoundID && sb.Properties == this.m_properties)
                    return base.StackWith(from, dropped, playSound);

            return false;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new SlayerBlood(m_slayerName, m_equipSoundID, m_properties, amount), amount);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 2;
            writer.Write(version); // version

            switch (version)
            {
                case 2:
                    {
                        writer.Write((uint)m_properties);
                        goto case 1;
                    }
                case 1:
                    {
                        writer.Write((int)m_slayerName);
                        writer.Write(m_equipSoundID);
                        goto case 0;
                    }
                case 0:
                    {
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
                case 2:
                    {
                        m_properties = (Config)reader.ReadUInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_slayerName = (SlayerName)reader.ReadInt();
                        m_equipSoundID = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
        }
    }
}