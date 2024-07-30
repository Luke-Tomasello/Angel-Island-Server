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

/* Items/Containers/SupplyDepotChest.cs
 * ChangeLog:
 *	1/24/06, Adam
 *		Add a couple Lesser Explosion Potions to reveal hiders in the 
 *		escape cave. We choose 'lesser' because we don't want these used
 *		to enhance escape chances.
 *	4/8/05, Adam
 *		make use of the SpiritDepotTRPots from the CoreAI global variables (setable
 *		within the CoreManagementConsole)
 *	8/3/04, Adam
 *		Created from DungeonTreasureChest
 *		We've addopted this modle instead of the SupplyDepotChestSpawner because the 
 *		static chests made it too easy for someone to macro looting on the chests.
 *		This SupplyDepotChest is spawned readomly in the supply room and is therefore 
 *		much more difficuly to macro looting on.
 */

using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{
    //[FlipableAttribute( 0xE41, 0xE40 )]
    [Flipable(0x9A9, 0xE7E)]
    public class SupplyDepotChest : LockableContainer
    {
        private int m_Level;
        private DateTime m_DeleteTime;
        private Timer m_Timer;
        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; } }

        //[CommandProperty(AccessLevel.GameMaster)]
        //public Mobile Owner { get { return m_Owner; } set { m_Owner = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime DeleteTime { get { return m_DeleteTime; } }

        [Constructable]
        public SupplyDepotChest()
            : this(null)
        {
        }

        public SupplyDepotChest(Mobile owner)
            : base(0x9A9) /*base( 0xE41 )*/
        {
            m_Owner = owner;
            m_Level = 0;

            // adam: usual decay time is CoreAI.SpiritDepotRespawnFreq * 10 seconds
            //	See also: ExecuteTrap() where decay is accelerated
            m_DeleteTime = DateTime.UtcNow + TimeSpan.FromSeconds(CoreAI.SpiritDepotRespawnFreq * 10);
            m_Timer = new DeleteTimer(this, m_DeleteTime);
            m_Timer.Start();

            Fill(this, m_Level);
        }

        //public override int DefaultGumpID{ get{ return 0x42; } }
        public override int DefaultGumpID { get { return 0x44; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            //get{ return new Rectangle2D( 18, 105, 144, 73 ); }
            get { return new Rectangle2D(20, 10, 150, 90); }
        }

        public static void Fill(LockableContainer cont, int level)
        {
            cont.Movable = false;
            cont.TrapEnabled = false;
            cont.Locked = false;

            int bandies = CoreAI.SpiritDepotBandies;
            int ghpots = CoreAI.SpiritDepotGHPots;
            int regs = CoreAI.SpiritDepotReagents;
            int trpots = CoreAI.SpiritDepotTRPots;

            Item item = null;

            // add a couple lesser explosion potions
            //	these are for batteling hiders in the cave and not for damage
            for (int ix = 0; ix < 2; ix++)
            {
                item = new LesserExplosionPotion();
                cont.DropItem(item);
            }

            // add bandages
            item = new Bandage(bandies);
            cont.DropItem(item);

            // add greater heal potions
            for (int ix = 0; ix < ghpots; ix++)
            {
                item = new GreaterHealPotion();
                cont.DropItem(item);
            }

            // add total refresh potions
            for (int ix = 0; ix < trpots; ix++)
            {
                item = new TotalRefreshPotion();
                cont.DropItem(item);
            }
            // drop reagents
            cont.DropItem(new BagOfReagents(regs));

            // drop res scroll
            cont.DropItem(new ResurrectionScroll());

            return;
        }

        private ArrayList m_Lifted = new ArrayList();

        private bool CheckLoot(Mobile m, bool criminalAction)
        {
            return true;
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            return CheckLoot(from, item != this) && base.CheckItemUse(from, item);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            return CheckLoot(from, true) && base.CheckLift(from, item, ref reject);
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            bool notYetLifted = !m_Lifted.Contains(item);

            if (notYetLifted)
            {
                m_Lifted.Add(item);

                // adam: do we even want this code?
                double chance = 0.25;
                if ((from.Hidden) && (Utility.RandomDouble() < chance))
                {
                    from.SendMessage("You have been revealed!");
                    from.RevealingAction();
                }
            }

            base.OnItemLifted(from, item);
        }

        public SupplyDepotChest(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write(m_Owner);

            writer.Write((int)m_Level);
            writer.WriteDeltaTime(m_DeleteTime);
            writer.WriteItemList(m_Lifted, true);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Level = reader.ReadInt();
                        m_DeleteTime = reader.ReadDeltaTime();
                        m_Lifted = reader.ReadItemList();

                        m_Timer = new DeleteTimer(this, m_DeleteTime);
                        m_Timer.Start();

                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;

            base.OnAfterDelete();
        }

        private class DeleteTimer : Timer
        {
            private Item m_Item;

            public DeleteTimer(Item item, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_Item = item;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                m_Item.Delete();
            }
        }

        public override void OnTelekinesis(Mobile from)
        {
            //Do nothing, telekinesis doesn't work here.
        }

        public override bool ExecuteTrap(Mobile from, bool bAutoReset)
        {
            bool bReturn = base.ExecuteTrap(from, bAutoReset);

            // adam: reset decay timer when the trap is messed with
            if (m_Timer != null)
            {
                m_Timer.Stop();

                m_Timer = null;

                // adam: once the trap has been tripped, it decays in SpiritDepotRespawnFreq seconds
                m_DeleteTime = DateTime.UtcNow + TimeSpan.FromSeconds(CoreAI.SpiritDepotRespawnFreq);
                from.SendMessage("The chest begins to decay.");
                m_Timer = new DeleteTimer(this, m_DeleteTime);
                m_Timer.Start();
            }

            return bReturn;
        }
    }
}