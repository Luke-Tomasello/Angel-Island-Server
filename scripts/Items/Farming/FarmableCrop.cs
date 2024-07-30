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

/* Scripts/Items/Farming/FarmableCrop.cs
 * CHANGELOG
 * 	8/20/23, Yoar
 * 		Merge with RunUO
 */

using Server.Network;
using System;

namespace Server.Items
{
    public abstract class FarmableCrop : Item
    {
        private bool m_Picked;

        public abstract Item GetCropObject();

        public virtual int GetPickedID()
        {
            return 0;
        }

        public FarmableCrop(int itemID) : base(itemID)
        {
            Movable = false;
        }

        public override void OnDoubleClick(Mobile from)
        {
            Map map = this.Map;
            Point3D loc = this.Location;

            if (map == null)
                return;

            if (!from.InRange(loc, 2) || !from.InLOS(this))
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            else if (!m_Picked)
                OnPicked(from, loc, map);
        }
        private static Memory m_PlayerMemory = new Memory();                // memory used to remember if a player picked something
        const int MemoryTime = 60;                                          // how long (seconds) we remember this player
        public static Memory Pickers { get { return m_PlayerMemory; } }    // Farm dogs and Farm crows check this
        public virtual void OnPicked(Mobile from, Point3D loc, Map map)
        {
            ItemID = GetPickedID();

            Item spawn = GetCropObject();

            if (spawn != null)
                spawn.MoveToWorld(loc, map);

            if (ItemID == 0)
            {
                Delete();
                return;
            }

            m_Picked = true;

            Unlink();

            Timer.DelayCall(TimeSpan.FromMinutes(5.0), new TimerCallback(Delete));

            if (Pickers.Recall(from))
                Pickers.Forget(from);
            // we need to freshen this record to pick up the new context.
            Pickers.Remember(from, this.Spawner, TimeSpan.FromSeconds(MemoryTime).TotalSeconds);   // remember him for this long
        }

        public void Unlink()
        {
            // TODO
#if false
            ISpawner se = this.Spawner;

            if (se != null)
            {
                this.Spawner.Remove(this);
                this.Spawner = null;
            }
#endif
        }

        public FarmableCrop(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Picked);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    m_Picked = reader.ReadBool();
                    break;
            }

            if (m_Picked)
            {
                Unlink();
                Delete();
            }
        }
    }
}