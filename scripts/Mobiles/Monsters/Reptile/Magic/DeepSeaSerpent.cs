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

/* Scripts/Mobiles/Monsters/Reptile/Magic/DeepSeaSerpent.cs
 * ChangeLog
 *	9/3/10, Adam
 *		Remove MIB and SpecialNet as these are added dynamically to the creature in the harvest system
 *		Removal also allows us to add this creature as spawn without adding special loot
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  11/10/04, Froste
 *      Removed PirateHat as loot, now restricted to "brethren only" drop
 *  9/26/04, Jade
 *      Increased gold drop from (25,50) to (150,200).
 *  7/21/04, Adam
 *		CS0654: (line 101, column 18) Method 'Server.Utility.RandomBool()' referenced without parentheses
 *		Fixed a little mith'take ;p
 *	7/21/04, mith
 *		Added PirateHat as loot, 5% drop.
 *	6/29/04, Pix
 *		Fixed MIB loot to spawn for the current facet.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a deep sea serpents corpse")]
    public class DeepSeaSerpent : BaseCreature
    {
        [Constructable]
        public DeepSeaSerpent()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a deep sea serpent";
            Body = 150;
            BaseSoundID = 447;

            SetStr(251, 425);
            SetDex(87, 135);
            SetInt(87, 155);

            SetHits(151, 255);

            SetDamage(6, 14);

            SetSkill(SkillName.MagicResist, 60.1, 75.0);
            SetSkill(SkillName.Tactics, 60.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 70.0);

            Fame = 6000;
            Karma = -6000;

            VirtualArmor = 60;
            CanSwim = true;
            CantWalkLand = true;
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }
        public override bool HasBreath { get { return true; } }
        public override int Meat { get { return 1; } }
        public override MeatType MeatType { get { return MeatType.Fish; } }
        public override int Hides { get { return 10; } }
        public override HideType HideType { get { return HideType.Horned; } }
        public override int Scales { get { return 8; } }
        public override ScaleType ScaleType { get { return ScaleType.Blue; } }

        private static readonly CarveEntry[] m_RareCarvables = new CarveEntry[]
            {
                new CarveEntry(1, typeof(RawFish), 0x1E15, 0x1E16),
                new CarveEntry(1, typeof(RawFishHeadless), 0x1E17, 0x1E18),
                new CarveEntry(1, typeof(FishHead), 0x1E19, 0x1E1A),
                new CarveEntry(1, typeof(FishHeads)),
                new CarveEntry(1, typeof(CookedFish), 0x1E1C, 0x1E1D),
            };

        public override double RareCarvableChance { get { return 0.02; } }
        public override CarveEntry[] RareCarvables { get { return m_RareCarvables; } }

        public DeepSeaSerpent(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(150, 200);
                PackItem(new SulfurousAsh(4));
                PackItem(new BlackPearl(4));
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20011213223007/uo.stratics.com/hunters/deepseaserpent.shtml
                    // 	Special Fishing Net, 1 Fish Steak (carved)

                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                        // no special fishing nets here, thos are dropped as part of the fishing system.
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        if (Utility.RandomBool())
                            PackItem(new SulfurousAsh(4));
                        else
                            PackItem(new BlackPearl(4));

                        // even though loot says, SpecialFishingNet, I don't believe it should ever be added as loot.
                        // the SpecialFishingNet is added dynamically by the fishing system.
                        // fishing.cs
                    }

                    AddLoot(LootPack.Meager);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}