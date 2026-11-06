/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Mobiles\Vendors\NPC\Stablehand.cs
 * ChangeLog
 *  6/19/2026, Adam
 *      Initial version.
 *      Special stable master that only stables mounts
 */

using Server.Items;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
    public class Stablehand : AnimalTrainer
    {
        public override bool IsInvulnerable { get { return false; } }
        public override bool IsSelling(Mobile from) { return false; }
        public override bool IsBuying(Mobile from) { return false; }
        public override void NotSellingMessage(Mobile from)
        {
            SayTo(from, "I only watch mounts, friend. I've nothing to sell.");
        }

        public override void NotBuyingMessage(Mobile from)
        {
            SayTo(from, "I only watch mounts, friend. I've no use for anything.");
        }
        [Constructable]
        public Stablehand() : base("the stablehand")
        {
            // Angel Island style Brigand - not a poshover
            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetSkill(SkillName.Fencing, 60.0, 82.5);
            SetSkill(SkillName.Macing, 60.0, 82.5);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 57.5, 80.0);
            SetSkill(SkillName.Swords, 60.0, 82.5);
            SetSkill(SkillName.Tactics, 60.0, 82.5);

            Delta(MobileDelta.Stat | MobileDelta.Hits | MobileDelta.Stam | MobileDelta.Mana);
        }
        public override void InitBody()
        {
            SpeechHue = Utility.RandomSpeechHue();
            Hue = Utility.RandomSkinHue();

            Female = false;
            Body = 0x190;
            Name = NameList.RandomName("male");
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

            AddItem(Utility.RandomBool() ? (Item)new QuarterStaff() : (Item)new ShepherdsCrook());
        }
        // Only allow claiming of mounts that were stabled here
        public override IList<BaseCreature> GetStabledPets(Mobile m, BaseCreature? bc = null)
        {
            var allStabled = base.GetStabledPets(m, bc);

            // Filter to only valid mounts
            return allStabled
                .Where(pet => IsValidMountForStabling(pet))
                .ToList();
        }
        public override bool IsValidMountForStabling(BaseCreature pet)
        {
            if (pet == null)
                return false;

            // Only allow normal rideable mounts
            if (pet is not IMount)
                return false;

            // Explicitly disallow problematic types
            if (pet is PackHorse || pet is PackLlama || pet is Beetle)
                return false;

            if (pet is Dragon || pet is Nightmare)
                return false;

            // Optionally disallow bonded pets if you want
            // if (pet.IsBonded) return false;

            return true;
        }
        public override bool CheckPetCondition(Mobile from, BaseCreature pet, bool message = true)
        {
            if (!IsValidMountForStabling(pet))
            {
                if (message)
                {
                    if (pet is Dragon || pet is Nightmare)
                        SayTo(from, "You must be joking. I'm not getting eaten today, thank you very much.");
                    else if (pet is PackHorse || pet is PackLlama || pet is Beetle)
                        SayTo(from, "I stable mounts, not pack mules. Take that thing to the butcher!");
                    else if (pet is Chicken)
                        SayTo(from, "A chicken?! Lad, I'm running a stable, not a  soup kitchen.");
                    else
                        SayTo(from, "I'm just a stable boy. Whatever that thing is, I'm not watching it.");
                }
                return false;
            }

            return base.CheckPetCondition(from, pet, message);
        }

        // Optional: Give him different dialogue
        public override void HereYouGo(Mobile from)
        {
            SayTo(from, "Kept him fed, watered, and out of trouble. Can't say the same for you.");
        }

        public Stablehand(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}