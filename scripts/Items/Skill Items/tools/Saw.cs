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
    [FlipableAttribute(0x1034, 0x1035)]
    public class Saw : BaseTool
    {
        public override CraftSystem CraftSystem { get { return DefCarpentry.CraftSystem; } }

        [Constructable]
        public Saw()
            : base(0x1034)
        {
            Weight = 2.0;
        }

        [Constructable]
        public Saw(int uses)
            : base(uses, 0x1034)
        {
            Weight = 2.0;
        }

        public Saw(Serial serial)
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

    // https://covingtonandsons.com/2020/11/22/the-carpenter-and-the-angel/
    public class TenjinsSaw : Saw
    {
        [Constructable]
        public TenjinsSaw()
            : base(100)
        {
            Name = "Tenjin's saw";
            Hue = CraftResources.GetHue(CraftResource.Verite);
        }

        public TenjinsSaw(Serial serial)
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

}