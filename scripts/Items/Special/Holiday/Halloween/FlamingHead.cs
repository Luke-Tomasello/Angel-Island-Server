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

/* Scripts/Items/Special/Holiday/Halloween/FlamingHead.cs
 * CHANGELOG
 *  11/25/2023, Adam (OnMovement)
 *      don't tip off players if staff is hidden nearby
 *  10/5/23, Yoar
 *      Initial commit.
 */

using System;

namespace Server.Items
{
    public class FlamingHead : BaseWallDecoration
    {
        public override Item Deed { get { return new FlamingHeadDeed(); } }

        private Timer m_Timer;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Breathing
        {
            get { return (m_Timer != null); }
            set
            {
                if (Breathing != value)
                {
                    if (value)
                        BeginBreath();
                    else
                        EndBreath();
                }
            }
        }

        [Constructable]
        public FlamingHead()
            : this(false)
        {
        }

        [Constructable]
        public FlamingHead(bool east)
            : base(east ? 0x110F : 0x10FC)
        {
        }

        public override bool HandlesOnMovement { get { return true; } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            // don't tip off players if staff is hidden nearby
            if (m.AccessLevel > AccessLevel.Player && m.Hidden)
                return;

            if (!Breathing && CheckRange(m.Location, 2) && !CheckRange(oldLocation, 2))
                Breathing = true;
        }

        private bool CheckRange(Point3D loc, int range)
        {
            return (Z >= loc.Z - 8 && Z < loc.Z + 16) && Utility.InRange(GetWorldLocation(), loc, range);
        }

        private void BeginBreath()
        {
            EndBreath();

            if (ItemID == 0x10FC || ItemID == 0x10FE)
                ItemID = 0x10FE;
            else
                ItemID = 0x1111;

            Effects.PlaySound(Location, Map, 0x359);

            (m_Timer = new InternalTimer(this)).Start();
        }

        private void EndBreath()
        {
            if (ItemID == 0x10FC || ItemID == 0x10FE)
                ItemID = 0x10FC;
            else
                ItemID = 0x110F;

            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private class InternalTimer : Timer
        {
            private FlamingHead m_Head;

            public InternalTimer(FlamingHead head)
                : base(TimeSpan.FromSeconds(2.0))
            {
                m_Head = head;
            }

            protected override void OnTick()
            {
                m_Head.EndBreath();
            }
        }

        public override void OnAfterDelete()
        {
            EndBreath();
        }

        public FlamingHead(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            if (ItemID == 0x10FE)
                ItemID = 0x10FC;
            else if (ItemID == 0x1111)
                ItemID = 0x110F;
        }
    }

    public class FlamingHeadDeed : BaseWallDecorationDeed
    {
        public override int LabelNumber { get { return 1041050; } } // a flaming head deed

        [Constructable]
        public FlamingHeadDeed()
        {
        }

        public override Item GetAddon(bool east)
        {
            return new FlamingHead(east);
        }

        public FlamingHeadDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}