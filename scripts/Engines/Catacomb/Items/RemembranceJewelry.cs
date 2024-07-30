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

using Server.Items;

namespace Server.Engines.Catacomb
{
    public class RemembranceBracelet : BaseBracelet
    {
        public override string DefaultName { get { return "bracelet of remembrance"; } }

        [Constructable]
        public RemembranceBracelet()
            : base(0x1086)
        {
            Weight = 0.1;
            Hue = 1425;
        }

        public RemembranceBracelet(Serial serial)
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

    public class RemembranceEarrings : BaseEarrings
    {
        public override string DefaultName { get { return "earrings of remembrance"; } }

        [Constructable]
        public RemembranceEarrings()
            : base(0x1087)
        {
            Weight = 0.1;
            Hue = 1425;
        }

        public RemembranceEarrings(Serial serial)
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

    public class RemembranceNecklace : BaseNecklace
    {
        public override string DefaultName { get { return "necklace of remembrance"; } }

        [Constructable]
        public RemembranceNecklace()
            : base(0x1088)
        {
            Weight = 0.1;
            Hue = 1425;
        }

        public RemembranceNecklace(Serial serial)
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

    public class RemembranceRing : BaseRing
    {
        public override string DefaultName { get { return "ring of remembrance"; } }

        [Constructable]
        public RemembranceRing()
            : base(0x108A)
        {
            Weight = 0.1;
            Hue = 1425;
        }

        public RemembranceRing(Serial serial)
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