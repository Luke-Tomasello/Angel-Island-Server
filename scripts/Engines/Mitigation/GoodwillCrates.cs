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

/* Scripts\Engines\Mitigation\GoodwillCrates.cs
 * ChangeLog
 *  7/9/21, Adam
 *		First time checkin
 *		There was a Certain griefer emptying the goodwill boxes at WBB. It has really infuriated the playerbase.
 *		He has now switched to trapping them, or more precisely, dropping trapped containers in and around the goodwill crated.
 */

using Server.Items;
using System;

namespace Server.Misc
{
    public partial class GoodwillAsshat
    {
        [Furniture]
        [TinkerTrapable]
        [Flipable(0x9A9, 0xE7E)]
        public class GoodwillCrate : LockableContainer
        {
            public override int DefaultGumpID { get { return 0x44; } }
            public override int DefaultDropSound { get { return 0x42; } }
            public override Rectangle2D Bounds
            { get { return new Rectangle2D(20, 10, 150, 90); } }

            [Constructable]
            public GoodwillCrate()
                : base(0x9A9)
            {
                Weight = 2.0;
                Hue = 0x979;
            }

            public GoodwillCrate(Serial serial)
                : base(serial)
            {
            }
            public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
            {
                if (item is TrapableContainer tc && tc.TrapEnabled)
                    return HandleAsshat(from, item);
                return base.OnDragDropInto(from, item, p);
            }
            public override bool OnDragDrop(Mobile from, Item dropped)
            {
                if (dropped is TrapableContainer tc && tc.TrapEnabled)
                    return HandleAsshat(from, dropped);
                return base.OnDragDrop(from, dropped);
            }
            private bool HandleAsshat(Mobile from, Item item)
            {
                Timer timer = new InternalTimer(from, this, TimeSpan.FromSeconds(2.0));
                timer.Start();
                return false;
            }
            private class InternalTimer : Timer
            {
                private Mobile m_from;
                private TrapableContainer m_cont;

                public InternalTimer(Mobile from, TrapableContainer cont, TimeSpan delay)
                    : base(delay)
                {
                    m_from = from;
                    Priority = TimerPriority.FiftyMS;
                }

                protected override void OnTick()
                {
                    ApplyPunishment(m_from, m_cont, Poison.Greater);
                    Stop();
                }
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
        [Furniture]
        [TinkerTrapable]
        [Flipable(0x9A9, 0xE7E)]
        public class SmallGoodwillCrate : GoodwillCrate
        {
            public override int DefaultGumpID { get { return 0x44; } }
            public override int DefaultDropSound { get { return 0x42; } }

            public override Rectangle2D Bounds
            {
                get { return new Rectangle2D(20, 10, 150, 90); }
            }

            [Constructable]
            public SmallGoodwillCrate()
                : base()
            {
                Weight = 2.0;
            }

            public SmallGoodwillCrate(Serial serial)
                : base(serial)
            {
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
        [Furniture]
        [TinkerTrapable]
        [Flipable(0xE3F, 0xE3E)]
        public class MediumGoodwillCrate : GoodwillCrate
        {
            public override int DefaultGumpID { get { return 0x44; } }
            public override int DefaultDropSound { get { return 0x42; } }

            public override Rectangle2D Bounds
            {
                get { return new Rectangle2D(20, 10, 150, 90); }
            }

            [Constructable]
            public MediumGoodwillCrate()
                : base()
            {
                Weight = 2.0;
            }

            public MediumGoodwillCrate(Serial serial)
                : base(serial)
            {
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

        [Furniture]
        [TinkerTrapable]
        [FlipableAttribute(0xe3c, 0xe3d)]
        public class LargeGoodwillCrate : GoodwillCrate
        {
            public override int DefaultGumpID { get { return 0x44; } }
            public override int DefaultDropSound { get { return 0x42; } }

            public override Rectangle2D Bounds
            {
                get { return new Rectangle2D(20, 10, 150, 90); }
            }

            [Constructable]
            public LargeGoodwillCrate()
                : base()
            {
                Weight = 1.0;
            }

            public LargeGoodwillCrate(Serial serial)
                : base(serial)
            {
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

                if (Weight == 8.0)
                    Weight = 1.0;
            }
        }

    }
}