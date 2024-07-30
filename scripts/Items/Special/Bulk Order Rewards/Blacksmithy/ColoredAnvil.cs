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

using Server.Engines.Craft;

namespace Server.Items
{
    [Anvil, Flipable(0xFAF, 0xFB0)]
    public class ColoredAnvil : Item
    {
        [Constructable]
        public ColoredAnvil()
            : this(CraftResources.GetHue((CraftResource)Utility.RandomMinMax((int)CraftResource.DullCopper, (int)CraftResource.Valorite)))
        {
        }

        [Constructable]
        public ColoredAnvil(int hue)
            : base(0xFAF)
        {
            Hue = hue;
            Weight = 20;
        }

        public ColoredAnvil(Serial serial)
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
        }
    }
}