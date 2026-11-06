using Server.Engines.ChampionSpawn;
using System;

namespace Server.Items
{
    public class BagOfSkulls : Bag
    {
        public override string DefaultName => "a bag of champion skulls";

        [Constructable]
        public BagOfSkulls()
        {
            foreach (ChampionSkullType type in Enum.GetValues(typeof(ChampionSkullType)))
            {
                if (type != ChampionSkullType.None)
                    DropItem(new ChampionSkull(type));
            }
        }

        public BagOfSkulls(Serial serial)
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