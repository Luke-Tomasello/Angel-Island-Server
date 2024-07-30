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
    [FlipableAttribute(0x13E4, 0x13E3)]
    public class SmithHammer : BaseTool
    {
        public override CraftSystem CraftSystem { get { return DefBlacksmithy.CraftSystem; } }

        [Constructable]
        public SmithHammer()
            : base(0x13E4)
        {
            Weight = 8.0;
            Layer = Layer.OneHanded;
        }

        [Constructable]
        public SmithHammer(int uses)
            : base(uses, 0x13E4)
        {
            Weight = 8.0;
            Layer = Layer.OneHanded;
        }

        public SmithHammer(Serial serial)
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

    public class TenjinsHammer : SmithHammer
    {
        [Constructable]
        public TenjinsHammer()
            : this(100)
        {
        }

        [Constructable]
        public TenjinsHammer(int uses)
            : base(uses)
        {
            Name = "Tenjin's hammer";
            Hue = CraftResources.GetHue(CraftResource.Verite);
        }

        public TenjinsHammer(Serial serial)
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