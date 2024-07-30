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

/* Scripts/Mobiles/Animals/Misc/GreatHart.cs
 * ChangeLog
 *  5/10/23, Yoar
 *      Reverted taming requirements for non-AI/MO shards.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 3 lines removed.
 *	1/14/05, Jade
 *      Changed taming requirement to give tamers a new creature to train taming skill on.
 *      This is being done to provide a replacement for ridgeback-level taming training, because ridgebacks no longer spawn.
 */

namespace Server.Mobiles
{
    [CorpseName("a deer corpse")]
    [TypeAlias("Server.Mobiles.Greathart")]
    public class GreatHart : BaseCreature
    {
        [Constructable]
        public GreatHart()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a great hart";
            Body = 0xEA;

            SetStr(41, 71);
            SetDex(47, 77);
            SetInt(27, 57);

            SetHits(27, 41);
            SetMana(0);

            SetDamage(5, 9);

            SetSkill(SkillName.MagicResist, 26.8, 44.5);
            SetSkill(SkillName.Tactics, 29.8, 47.5);
            SetSkill(SkillName.Wrestling, 29.8, 47.5);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 24;

            Tamable = true;
            ControlSlots = 1;

            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
                MinTameSkill = 83.1;//Jade: Change to give tamers a ridgeback replacement for training.
            else
                MinTameSkill = 59.1;
        }

        public override int Meat { get { return 6; } }
        public override int Hides { get { return 15; } }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        private static readonly CarveEntry[] m_RareCarvables = new CarveEntry[]
            {
                new CarveEntry(1, typeof(Item), 0x1E90, 0x1E91), // skinned deer
            };

        public override double RareCarvableChance { get { return 0.02; } }
        public override CarveEntry[] RareCarvables { get { return m_RareCarvables; } }

        public GreatHart(Serial serial)
            : base(serial)
        {
        }

        public override int GetAttackSound()
        {
            return 0x82;
        }

        public override int GetHurtSound()
        {
            return 0x83;
        }

        public override int GetDeathSound()
        {
            return 0x84;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1 && MinTameSkill == 83.1 && !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules())
                MinTameSkill = 59.1;
        }
    }
}