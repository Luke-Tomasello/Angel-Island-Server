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

/* Scripts/Items/Misc/TrashBarrel.cs
 * ChangeLog:
 *  6/14/2023, Adam
 *      Condition IsLockedDown on AngelIslandRules
 *  04/06/06 Taran Kain
 *      Override CanFreezeDry to always be false. (exception noted)
 *	03/23/06, weaver
 *		Added additional security overrides to allow access via public buildings.
 *	03/20/06, weaver
 *		Trash barrels now override container drop checks.
 *	03/20/06, weaver
 *		Fixed old trash barrels via deserialisation. Incremented version.
 *	03/20/06, weaver
 *		Trash barrels are now no longer rehued grey on construction, are movable and can only be used
 *		whilst locked down.
 *  02/17/05, mith
 *		Changed base object from Container to BaseContainer to include fixes made there that override
 *		functionality in the core.
 */

using Server.Multis;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class TrashBarrel : BaseContainer, IChopable
    {
        public override int LabelNumber { get { return 1041064; } } // a trash barrel

        public override int MaxWeight { get { return 0; } } // A value of 0 signals unlimited weight

        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }
#if false
        public override bool CanFreezeDry
        {
            get
            {
                // This has the effect of disallowing non-FD'ed (ie, normal) trash barrels from freezedrying, but
                // still allowing previously-FD'ed (bugged) barrels to rehydrate and restore themselves to a
                // valid state.
                return IsFreezeDried;
            }
        }
#endif
        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(33, 36, 109, 112); }
        }

        [Constructable]
        public TrashBarrel()
            : base(0xE77)
        {
            if (!Core.RuleSets.AngelIslandRules())
            {
                Hue = 0x3B2;
                Movable = false;
            }
            else
            {
                // wea: trash barrels are now standard wood colour
                // and movable!
            }
        }

        public TrashBarrel(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (Items.Count > 0)
            {
                m_Timer = new EmptyTimer(this);
                m_Timer.Start();
            }

            if (version < 1)
            {
                Hue = 0;
                Movable = true;
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!base.OnDragDrop(from, dropped))
                return false;

            // wea: trash barrels will only work when locked down
            if (Core.RuleSets.AngelIslandRules() && !IsLockedDown)
                return false;

            if (TotalItems >= 50)
            {
                Empty(501478); // The trash is full!  Emptying!
            }
            else
            {
                SendLocalizedMessageTo(from, 1010442); // The item will be deleted in three minutes

                if (m_Timer != null)
                    m_Timer.Stop();
                else
                    m_Timer = new EmptyTimer(this);

                m_Timer.Start();
            }

            return true;
        }

        // wea: anyone can use them
        public override bool IsAccessibleTo(Mobile m)
        {
            return true;
        }

        // wea: override security checks for trash 
        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            DropItem(dropped);
            return true;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!base.OnDragDropInto(from, item, p))
                return false;

            // wea: trash barrels will only work when locked down
            if (Core.RuleSets.AngelIslandRules() && !IsLockedDown)
                return false;

            if (TotalItems >= 50)
            {
                Empty(501478); // The trash is full!  Emptying!
            }
            else
            {
                SendLocalizedMessageTo(from, 1010442); // The item will be deleted in three minutes

                if (m_Timer != null)
                    m_Timer.Stop();
                else
                    m_Timer = new EmptyTimer(this);

                m_Timer.Start();
            }

            return true;
        }

        public void OnChop(Mobile from)
        {
            BaseHouse house = BaseHouse.FindHouseAt(from);

            if (house != null && house.IsCoOwner(from))
            {
                Effects.PlaySound(Location, Map, 0x11C);
                from.SendLocalizedMessage(500461); // You destroy the item.
                Destroy();
            }
        }

        public new void Empty(int message)
        {
            List<Item> items = this.Items;

            if (items.Count > 0)
            {
                PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, message, "");

                for (int i = items.Count - 1; i >= 0; --i)
                {
                    if (i >= items.Count)
                        continue;

                    ((Item)items[i]).Delete();
                }
            }

            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;
        }

        private Timer m_Timer;

        private class EmptyTimer : Timer
        {
            private TrashBarrel m_Barrel;

            public EmptyTimer(TrashBarrel barrel)
                : base(TimeSpan.FromMinutes(3.0))
            {
                m_Barrel = barrel;
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                m_Barrel.Empty(501479); // Emptying the trashcan!
            }
        }
    }
}