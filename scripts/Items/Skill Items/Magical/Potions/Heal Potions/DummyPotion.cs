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

namespace Server.Items
{
    public class DummyPotion : BaseDummyPotion
    {
        public override int MinHeal { get { return 6; } }
        public override int MaxHeal { get { return 20; } }

        [Constructable]
        public DummyPotion()
            : this(1)
        {
        }

        [Constructable]
        public DummyPotion(int amount)
            : base(PotionEffect.Heal)
        {
            Name = "Dummy Potion";
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
        }
        public override Item Dupe(int amount)
        {
            return base.Dupe(new DummyPotion(), amount);
        }
        public DummyPotion(Serial serial)
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