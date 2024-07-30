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

/* Scripts\Mobiles\Monsters\Polymorphs\PolymorphicBob.cs
 * ChangeLog
 *  9/18/21, Adam
 *      Created.
 */

using Server.Items;

namespace Server.Mobiles
{
    public class PolymorphicBob : BasePolymorphic
    {
        [Constructable]
        public PolymorphicBob()
            : base()
        {
            SpeechHue = Utility.RandomSpeechHue();
            Fame = 1000;
            Karma = -1000;

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
        }

        public PolymorphicBob(Serial serial)
            : base(serial)
        {
        }

        public override void InitStats()
        {
            base.InitStats();
        }

        public override void InitBody()
        {
            this.Female = false;
            Body = 0x190;
            Hue = 0x83EA;
            switch (Utility.Random(4))
            {
                case 0:     // classic bob names
                    Name = NameList.RandomName("Bob");
                    break;
                default:    // build your own bob names (much bigger list)
                    Name = NameList.RandomName("Negative Adjectives") + " Bob";
                    break;
            }
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            Robe robe = new Robe(23);
            AddItem(robe);

            if (AI == AIType.AI_Melee)
                switch (Utility.Random(3))
                {
                    case 0: AddItem(new Club()); break;
                    case 1: AddItem(new Dagger()); break;
                    case 2: AddItem(new Spear()); break;
                }
            else if (AI == AIType.AI_Archer)
                switch (Utility.Random(6))
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        AddItem(new Bow());
                        break;
                    case 4:
                        AddItem(new Crossbow());
                        break;
                    case 5:
                        AddItem(new HeavyCrossbow());
                        break;
                }
            else if (AI == AIType.AI_BaseHybrid)
            {
                PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
                PackStrongPotions(6, 12);
                PackItem(new Pouch(), lootType: LootType.UnStealable);
            }
        }

        public override void GenerateLoot()
        {
            base.GenerateLoot();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    goto case 0;
                case 0:
                    break;
            }

        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    goto case 0;
                case 0:
                    break;
            }
        }
    }
}