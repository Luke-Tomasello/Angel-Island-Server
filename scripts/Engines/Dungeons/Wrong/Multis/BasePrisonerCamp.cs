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

/* Scripts\Multis\Camps\BasePrisonerCamp.cs
 * ChangeLog
 *	6/4/2023, Adam 
 *	    First time checkin
 *	    Base class for various 'prisoner' camps.
 *	    Features:
 *	        Dungeon treasure chest, 
 *	        Prisoner
 *	        Guard
 *	        Key to 'gate' not chest, on guard.
 */

using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Multis
{
    public class BasePrisonerCamp : BaseCamp
    {
        public virtual int WanderRange { get { return 4; } }
        private Mobile m_Prisoner;
        public Mobile Prisoner { get { return m_Prisoner; } set { m_Prisoner = value; } }

        public BasePrisonerCamp()
            : base(0x1B72 /*BronzeShield*/)
        {
        }
        public override void AddMobile(Mobile m, int wanderRange, int xOffset, int yOffset, int zOffset)
        {
            base.AddMobile(m, wanderRange, xOffset, yOffset, zOffset);
        }
        public override void AddComponents()
        {
            AddChest();
            AddGuard();
            AddMobiles();
        }
        public virtual void AddGuard()
        {
            // we want these guys outside the jail cell
            int x = 0;
            int y = 9;

            Mobile m = null;
            if (Utility.Chance(0.10))
                m = new BrigandLeader();
            else
                m = new Brigand();

            AddMobile(m, 15, x, y, 0);
            ManageLock(m);
        }
        public void ManageLock(Mobile m)
        {
            // we're still on the internal map, wait until we get moved to world
            Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(ManageLockTick), new object[] { m });
        }
        private void ManageLockTick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is Mobile m && m.Deleted == false)
            {
                if (m.Map == Map.Internal || m.Map == null)
                {
                    Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(ManageLockTick), new object[] { m });
                    return;
                }

                Point3D loc = m.Location;
                Key key = new Key(KeyType.Gold);
                key.KeyValue = Key.RandomValue();
                IPooledEnumerable eable = Map.GetItemsInRange(loc, 6);
                foreach (Item item in eable)
                    if (item is BaseDoor bd)
                    {
                        bd.KeyValue = key.KeyValue;
                        bd.Locked = true;
                    }

                eable.Free();

                // no more thief steal-griefing the players
                key.LootType = LootType.UnStealable;
                if (m.Backpack == null && m is BaseCreature bc)
                    bc.PackItem(key);
            }

            // no key for you!
            return;
        }
        public virtual void AddChest()
        {
            int x = Utility.RandomMinMax(-2, 2);
            int y = Utility.RandomMinMax(-2, 2);
            int level = Utility.RandomMinMaxScaled(1, 4);
            DungeonTreasureChest chest = new DungeonTreasureChest(level);
            if (Utility.Chance(0.20))   // 20% chance at a port key
                chest.DropItem(new Server.Items.PortKey());
            AddItem(chest, x, y, 0);
        }
        public virtual void AddMobiles()
        {
            switch (Utility.Random(2))
            {
                case 0: m_Prisoner = new Noble(); break;
                case 1: m_Prisoner = new SeekerOfAdventure(); break;
            }

            m_Prisoner.YellHue = Utility.RandomList(0x57, 0x67, 0x77, 0x87, 0x117);

            AddMobile(m_Prisoner, 2, -2, 0, 0);
        }
        public override void OnEnter(Mobile m)
        {
            base.OnEnter(m);

            if (m.Player && m_Prisoner != null)
            {
                int number;

                switch (Utility.Random(4))
                {
                    default:
                    case 0: number = 502264; break; // Help a poor prisoner!
                    case 1: number = 502266; break; // Aaah! Help me!
                    case 2: number = 1046000; break; // Help! These savages wish to end my life!
                    case 3: number = 1046003; break; // Quickly! Kill them for me! HELP!!
                }

                m_Prisoner.Yell(number);
            }
        }

        public BasePrisonerCamp(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Prisoner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Prisoner = reader.ReadMobile();
                        break;
                    }
            }
        }
    }
}