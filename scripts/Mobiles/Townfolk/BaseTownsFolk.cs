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

/* Scripts\Mobiles\Townfolk\BaseTownsFolk.cs
 * CHANGELOG:
 * 6/24/2024. Adam
 *   First time check in
 */

using Server.Items;

namespace Server.Mobiles
{
    public class BaseTownsFolk : BaseCreature
    {
        [Constructable]
        public BaseTownsFolk(string title)
            : base(AIType.AI_Vendor, FightMode.None, 2, 1, 0.5, 2)
        {
            SetSkill(SkillName.ItemID, 64.0, 100.0);

            InitBody();
            InitOutfit();
            this.Title = title;

            // defaulted here, but can be overridden by the spawner
            IsInvulnerable = Core.RuleSets.BaseVendorInvulnerability();
        }
        public virtual bool GetGender()
        {
            return Utility.RandomBool();
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            SpeechHue = Utility.RandomSpeechHue();
            Hue = Utility.RandomSkinHue();

            if (Female = GetGender())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            switch (Utility.Random(3))
            {
                case 0: AddItem(new FancyShirt(GetRandomHue())); break;
                case 1: AddItem(new Doublet(GetRandomHue())); break;
                case 2: AddItem(new Shirt(GetRandomHue())); break;
            }

            /* Publish 4
			 * Shopkeeper Changes
			 * NPC shopkeepers will no longer have colored sandals. Evil NPC Mages will carry these items.
			 * adam: I'm unsure of the hue, but sice this was likely the hue 'moved' to evil mage lords in publish 4, we will assume these are the hues used here
			 * http://forums.uosecondage.com/viewtopic.php?f=8&t=22266
			 * runuo.com/community/threads/evil-mage-hues.91540/
			 */
            int sandal_hue = (PublishInfo.Publish >= 4) ? GetShoeHue() : Utility.RandomBool() ? Utility.RandomRedHue() : Utility.RandomBlueHue();

            switch (ShoeType)
            {
                case VendorShoeType.Shoes: AddItem(new Shoes(GetShoeHue())); break;
                case VendorShoeType.Boots: AddItem(new Boots(GetShoeHue())); break;
                case VendorShoeType.Sandals: AddItem(new Sandals(sandal_hue)); break;
                case VendorShoeType.ThighBoots: AddItem(new ThighBoots(GetShoeHue())); break;
            }

            int hairHue = GetHairHue();

            if (Female)
            {
                switch (Utility.Random(6))
                {
                    case 0: AddItem(new ShortPants(GetRandomHue())); break;
                    case 1:
                    case 2: AddItem(new Kilt(GetRandomHue())); break;
                    case 3:
                    case 4:
                    case 5: AddItem(new Skirt(GetRandomHue())); break;
                }

                switch (Utility.Random(9))
                {
                    case 0: AddItem(new Afro(hairHue)); break;
                    case 1: AddItem(new KrisnaHair(hairHue)); break;
                    case 2: AddItem(new PageboyHair(hairHue)); break;
                    case 3: AddItem(new PonyTail(hairHue)); break;
                    case 4: AddItem(new ReceedingHair(hairHue)); break;
                    case 5: AddItem(new TwoPigTails(hairHue)); break;
                    case 6: AddItem(new ShortHair(hairHue)); break;
                    case 7: AddItem(new LongHair(hairHue)); break;
                    case 8: AddItem(new BunsHair(hairHue)); break;
                }
            }
            else
            {
                switch (Utility.Random(2))
                {
                    case 0: AddItem(new LongPants(GetRandomHue())); break;
                    case 1: AddItem(new ShortPants(GetRandomHue())); break;
                }

                switch (Utility.Random(8))
                {
                    case 0: AddItem(new Afro(hairHue)); break;
                    case 1: AddItem(new KrisnaHair(hairHue)); break;
                    case 2: AddItem(new PageboyHair(hairHue)); break;
                    case 3: AddItem(new PonyTail(hairHue)); break;
                    case 4: AddItem(new ReceedingHair(hairHue)); break;
                    case 5: AddItem(new TwoPigTails(hairHue)); break;
                    case 6: AddItem(new ShortHair(hairHue)); break;
                    case 7: AddItem(new LongHair(hairHue)); break;
                }

                switch (Utility.Random(5))
                {
                    case 0: AddItem(new LongBeard(hairHue)); break;
                    case 1: AddItem(new MediumLongBeard(hairHue)); break;
                    case 2: AddItem(new Vandyke(hairHue)); break;
                    case 3: AddItem(new Mustache(hairHue)); break;
                    case 4: AddItem(new Goatee(hairHue)); break;
                }
            }
        }
        public virtual int GetRandomHue()
        {
            switch (Utility.Random(5))
            {
                default:
                case 0: return Utility.RandomBlueHue();
                case 1: return Utility.RandomGreenHue();
                case 2: return Utility.RandomRedHue();
                case 3: return Utility.RandomYellowHue();
                case 4: return Utility.RandomNeutralHue();
            }
        }

        public virtual int GetShoeHue()
        {
            if (0.1 > Utility.RandomDouble())
                return 0;

            return Utility.RandomNeutralHue();
        }

        public virtual VendorShoeType ShoeType
        {
            get { return VendorShoeType.Shoes; }
        }
        public virtual int GetHairHue()
        {
            return Utility.RandomHairHue();
        }
        public BaseTownsFolk(Serial serial)
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