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

/* /sandbox/ai/Scripts/Items/Misc/MorphItem.cs
 *	ChangeLog :
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 */

namespace Server.Items
{
    public class MorphItem : Item
    {
        private int m_InactiveItemID;
        private int m_ActiveItemID;
        private int m_Range;

        [CommandProperty(AccessLevel.GameMaster)]
        public int InactiveItemID
        {
            get { return m_InactiveItemID; }
            set { m_InactiveItemID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ActiveItemID
        {
            get { return m_ActiveItemID; }
            set { m_ActiveItemID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { if (value > 18) value = 18; m_Range = value; }
        }

        [Constructable]
        public MorphItem(int inactiveItemID, int activeItemID, int range)
            : base(inactiveItemID)
        {
            if (range > 18)
                range = 18;

            Movable = false;

            m_InactiveItemID = inactiveItemID;
            m_ActiveItemID = activeItemID;
            m_Range = range;
        }

        public MorphItem(Serial serial)
            : base(serial)
        {
        }

        public override bool HandlesOnMovement { get { return true; } }

        public void Refresh()
        {
            bool found = false;

            IPooledEnumerable eable = GetMobilesInRange(m_Range);
            foreach (Mobile mob in eable)
            {
                if (mob.Hidden && mob.AccessLevel > AccessLevel.Player)
                    continue;

                found = true;
                break;
            }
            eable.Free();

            if (found)
                ItemID = ActiveItemID;
            else
                ItemID = InactiveItemID;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (Utility.InRange(m.Location, Location, m_Range) || Utility.InRange(oldLocation, Location, m_Range))
                Refresh();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write((int)m_InactiveItemID);
            writer.Write((int)m_ActiveItemID);
            writer.Write((int)m_Range);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_InactiveItemID = reader.ReadInt();
                        m_ActiveItemID = reader.ReadInt();
                        m_Range = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}