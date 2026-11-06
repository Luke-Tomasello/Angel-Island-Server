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
            // http://web.archive.org/web/20020214225651/uo.stratics.com/hunters/spectralarmour.shtml
            // Loot: None (no gold, no items)
            // Cut Up: None
            // See OnBeforeDeath for weapon drop.

            if (Spawning)
            {
                // No gold
            }
            else
            {
                // No loot
            }
        }

        public override bool OnBeforeDeath()
        {
            if (!base.OnBeforeDeath())
                return false;

            // Spectral Armour: no corpse; drop weapon directly to world
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=334
            if (Map != null && Map != Map.Internal)
            {
                Scimitar weapon = new Scimitar
                {
                    DamageLevel = Loot.GetGearForLevel<WeaponDamageLevel>(3, upgrade_chance: 0.2),
                    AccuracyLevel = Utility.RandomEnumValue<WeaponAccuracyLevel>(),
                    DurabilityLevel = Utility.RandomEnumValue<WeaponDurabilityLevel>()
                };

                weapon.MoveToWorld(Location, Map);
                Effects.SendLocationEffect(Location, Map, 0x376A, 10, 1);
            }

            // No corpse: delete creature and suppress normal death flow.
            Delete();
            return false;
        }
    }
}