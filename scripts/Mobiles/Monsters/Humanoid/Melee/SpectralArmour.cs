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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/SpectralArmor.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
namespace Server.Mobiles
{

    public class SpectralArmour : BaseCreature
    {
        [Constructable]
        public SpectralArmour()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Body = 637;
            Hue = 32;
            Name = "spectral armour";
            BaseSoundID = 451;

            SetStr(309, 333);
            SetDex(99, 106);
            SetInt(101, 110);
            SetSkill(SkillName.Wrestling, 78.1, 95.5);
            SetSkill(SkillName.Tactics, 91.1, 99.7);
            SetSkill(SkillName.MagicResist, 92.4, 79);
            SetSkill(SkillName.Swords, 78.1, 97.4);

            VirtualArmor = 40;
            SetFameLevel(3);
            SetKarmaLevel(3);
        }

        public override Poison PoisonImmune { get { return Poison.Regular; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax { get { return 323; } }

        public SpectralArmour(Serial serial)
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
            int version = reader.ReadInt();
        }

        public override void GenerateLoot()
        {
            // no corpse, so see OnBeforeDeath()
        }

        public override bool OnBeforeDeath()
        {
            if (!base.OnBeforeDeath())
                return false;

            if (Core.RuleSets.AngelIslandRules())
            {
                Scimitar weapon = new Scimitar();
#if old
                weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(0, 5);
                weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(0, 5);
                weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(0, 5);
#else
                // https://uo.stratics.com/database/view.php?db_content=hunters&id=334
                weapon.DamageLevel = Loot.GetGearForLevel<WeaponDamageLevel>(3/* Not sure if this guy is even supposed to drop weapon*/, upgrade_chance: 0.2);
                weapon.AccuracyLevel = Utility.RandomEnumValue<WeaponAccuracyLevel>();
                weapon.DurabilityLevel = Utility.RandomEnumValue<WeaponDurabilityLevel>();
#endif
                weapon.MoveToWorld(this.Location, this.Map);

                Effects.SendLocationEffect(Location, Map, 0x376A, 10, 1);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {
                    Scimitar weapon = new Scimitar();
#if old
                weapon.DamageLevel = (WeaponDamageLevel)Utility.Random(0, 5);
                weapon.DurabilityLevel = (WeaponDurabilityLevel)Utility.Random(0, 5);
                weapon.AccuracyLevel = (WeaponAccuracyLevel)Utility.Random(0, 5);
#else
                    // https://uo.stratics.com/database/view.php?db_content=hunters&id=334
                    weapon.DamageLevel = Loot.GetGearForLevel<WeaponDamageLevel>(3/* Not sure if this guy is even supposed to drop weapon*/, upgrade_chance: 0.2);
                    weapon.AccuracyLevel = Utility.RandomEnumValue<WeaponAccuracyLevel>();
                    weapon.DurabilityLevel = Utility.RandomEnumValue<WeaponDurabilityLevel>();
#endif
                    weapon.MoveToWorld(this.Location, this.Map);

                    Effects.SendLocationEffect(Location, Map, 0x376A, 10, 1);
                }
                else
                {
                    // run uo code 
                    Gold gold = new Gold(Utility.RandomMinMax(240, 375));
                    gold.MoveToWorld(Location, Map);

                    Effects.SendLocationEffect(Location, Map, 0x376A, 10, 1);
                }
            }

            // we don't want a corpse, so refuse the 'death' and just delete the creature
            this.Delete();
            return false;
        }
    }
}