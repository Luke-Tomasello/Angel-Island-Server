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

/* Scripts\Items\Misc\ThrowableRose.cs
 * ChangeLog
 *  3/1/22, Adam (GetSpawnPosition)
 *      Use GetSpawnPosition to find spawnable locations that aren't in a wall etc.
 *      Also increase the delete timer to 30 seconds.
 *  3/1/22, Yoar
 *	    Initial version.
 */

using Server.Mobiles;
using System;
using static Server.Utility;

namespace Server.Items
{
    [FlipableAttribute(0x18E9, 0x18EA)]
    public class ThrowableRose : BaseThrowableItem
    {
        public static readonly TimeSpan CleanupDelay = TimeSpan.FromSeconds(30.0);

        public override bool Explodes { get { return false; } }
        public override bool DelayedHit { get { return true; } }

        private bool m_Thrown;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Thrown
        {
            get { return m_Thrown; }
            set { m_Thrown = value; }
        }

        [Constructable]
        public ThrowableRose()
            : base(0x18EA)
        {
            Name = "a rose";
        }

        public override void OnAim(Mobile from)
        {
            // TODO: Aim message?

            base.OnAim(from);
        }

        public override void OnThrow(Mobile from, Mobile targ)
        {
            targ.SendMessage("You have just been hit by a rose. They love you!");
            from.SendMessage("You throw the rose and hit the target!");

            Movable = false;
            m_Thrown = true;

            Point3D px = Spawner.GetSpawnPosition(targ.Map, targ.Location, Utility.RandomBool() ? 2 : 1, SpawnFlags.None, this);
            MoveToWorld(new Point3D(px.X, px.Y, targ.Z));
        }

        public override void ConsumeCharge(Mobile from)
        {
            Timer.DelayCall(CleanupDelay, Delete);
        }

        public ThrowableRose(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write((bool)m_Thrown);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            m_Thrown = reader.ReadBool();

            if (m_Thrown)
                Delete();
        }
    }
}