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

/* Scripts/Items/Suits/BaseSuit.cs
 * ChangeLog
 *	6/27/06, Adam
 *		Make setting of robe hues an Admin only function
 *	4/17/06, Adam
 *		Override Item.Hue to prevent the setting of a hue value < 0
 *		hue values < 0 crash the UO client when trying to display the "GM Robe" text-graphic
 */

using System;

namespace Server.Items
{
    public abstract class BaseSuit : Item
    {
        private AccessLevel m_AccessLevel;

        [CommandProperty(AccessLevel.Administrator)]
        public AccessLevel AccessLevel { get { return m_AccessLevel; } set { m_AccessLevel = value; } }

        public BaseSuit(AccessLevel level, int hue, int itemID)
            : base(itemID)
        {
            Hue = hue;
            Weight = 1.0;
            Movable = false;
            LootType = LootType.Newbied;
            Layer = Layer.OuterTorso;

            m_AccessLevel = level;
        }

        public BaseSuit(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_AccessLevel);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_AccessLevel = (AccessLevel)reader.ReadInt();
                        break;
                    }
            }
        }

        public bool Validate()
        {
            object root = RootParent;

            if (root is Mobile && ((Mobile)root).AccessLevel < m_AccessLevel)
            {
                Delete();
                return false;
            }

            return true;
        }

        public override void OnSingleClick(Mobile from)
        {
            if (Validate())
                base.OnSingleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Validate())
                base.OnDoubleClick(from);
        }

        public override bool VerifyMove(Mobile from)
        {
            return (from.AccessLevel >= m_AccessLevel);
        }

        public override bool OnEquip(Mobile from)
        {
            if (from.AccessLevel < m_AccessLevel)
                from.SendMessage("You may not wear this.");

            return (from.AccessLevel >= m_AccessLevel);
        }

        [Hue, CommandProperty(AccessLevel.Administrator)]
        public override int Hue
        {
            get
            {
                return base.Hue;
            }
            set
            {
                // avoid client crash when setting GMRobes to hue -1 or any value < 0
                if (value < 0)
                {
                    Mobile from = RootParent as Mobile;

                    if (from != null)
                        from.SendMessage("Illegal value.");

                    // throw an exception so that the "Property has been set" message is supressed
                    throw new ApplicationException("Invalid argument value.");
                }
                else
                    base.Hue = value;
            }
        }
    }
}