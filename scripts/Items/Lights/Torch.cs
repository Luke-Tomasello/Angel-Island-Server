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
    public class Torch : BaseEquipableLight
    {
        public override int LitItemID { get { return 0xA12; } }
        public override int UnlitItemID { get { return 0xF6B; } }

        public override int LitSound { get { return 0x54; } }
        public override int UnlitSound { get { return 0x4BB; } }

        [Constructable]
        public Torch()
            : base(0xF6B)
        {
            if (Burnout)
                Duration = TimeSpan.FromMinutes(30);
            else
                Duration = TimeSpan.Zero;

            Burning = false;
            Light = LightType.Circle300;
            Weight = 1.0;
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

            if (parent is Mobile && Burning)
                Mobiles.MeerMage.StopEffect((Mobile)parent, true);
        }

        public override void Ignite()
        {
            base.Ignite();

            if (Parent is Mobile && Burning)
                Mobiles.MeerMage.StopEffect((Mobile)Parent, true);
        }

        public Torch(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (Weight == 2.0)
                Weight = 1.0;
        }
    }
}