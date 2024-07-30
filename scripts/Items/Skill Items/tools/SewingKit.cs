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
    public class SewingKit : BaseTool
    {
        public override CraftSystem CraftSystem { get { return DefTailoring.CraftSystem; } }

        [Constructable]
        public SewingKit()
            : base(0xF9D)
        {
            Weight = 2.0;
        }

        [Constructable]
        public SewingKit(int uses)
            : base(uses, 0xF9D)
        {
            Weight = 2.0;
        }

        public SewingKit(Serial serial)
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

    public class TenjinsNeedle : SewingKit
    {
        public override CraftSystem CraftSystem { get { return DefTailoring.CraftSystem; } }

        [Constructable]
        public TenjinsNeedle()
            : this(100)
        {
        }

        [Constructable]
        public TenjinsNeedle(int uses)
            : base(uses)
        {
            Name = "Tenjin's needle";
            Hue = CraftResources.GetHue(CraftResource.Verite);
        }

        public TenjinsNeedle(Serial serial)
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