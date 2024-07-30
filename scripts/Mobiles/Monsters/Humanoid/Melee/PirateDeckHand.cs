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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/PirateDeckHand.cs	
 * ChangeLog:
 *	7/9/10, adam
 *		o Merge pirate class hierarchy (all pirates are now derived from class Pirate)
 *		o Remove old RunUO Heal-with-bandages model and use new style which uses real bandages
 *		o Replace AI with new AI_HumanMelee AI .. allows healing with bandages, potions and potion buffs
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	1/2/05, Adam
 *		Cleanup pirate name management, make use of Titles
 *			Show title when clicked = false
 *  1/02/05, Jade
 *      Increased speed to bring Pirates up to par with other human IOB kin.
 *	12/30/04 Created by Adam
 */

using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("corpse of a salty seadog")]
    public class PirateDeckHand : Pirate
    {
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 2 : 0; } }

        [Constructable]
        public PirateDeckHand()
            : base(AIType.AI_HumanMelee)
        {
        }
        public override Characteristics MyCharacteristics { get { return base.MyCharacteristics & ~Characteristics.DamageSlows; } }
        public override void InitClass()
        {
            ControlSlots = 2;

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetSkill(SkillName.Swords, 60.0, 82.5);
            SetSkill(SkillName.Tactics, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 57.5, 80.0);
            SetSkill(SkillName.Healing, 60.0, 82.5);
            SetSkill(SkillName.Anatomy, 60.0, 82.5);

            Fame = 1000;
            Karma = -1000;

            // weapons only
            FightStyle = FightStyle.Melee;
        }

        public override void InitBody()
        {
            base.InitBody();
            Title = "the deckhand";
        }
        public override void InitOutfit()
        {
            base.InitOutfit();
            Item hat = FindItemOnLayer(Layer.Helm);
            if (hat != null)
                hat.Delete();

            AddItem(new SkullCap(Utility.RandomRedHue()));
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AllShards)
            {
                if (Spawning)
                {
                    // No at spawn loot
                }

                PackGem();
                PackMagicEquipment(1, 3);
                PackGold(100, 150);

                // Froste: 12% random IOB drop
                if (Core.RuleSets.AngelIslandRules())
                    if (0.12 > Utility.RandomDouble())
                    {
                        Item iob = Loot.RandomIOB();
                        PackItem(iob);
                    }

                // pack bulk reg
                PackItem(new MandrakeRoot(Utility.RandomMinMax(5, 10)));

                if (Core.RuleSets.AngelIslandRules())
                    if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                    {
                        // 30% boost to gold
                        PackGold(base.GetGold() / 3);
                    }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {
                    // ai special
                }
            }
        }

        public PirateDeckHand(Serial serial)
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

            if (base.Version == 0)
                return;

            int version = reader.ReadInt();
        }
    }
}