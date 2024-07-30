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

/* Scripts/Mobiles/Monsters/Special/Christmas/Pagans/PaganDruid.cs
 * ChangeLog
 *	12/13/23, Yoar
 *		Initial version.
 */

using Server.Items;

namespace Server.Mobiles
{
    public class PaganDruid : BaseCreature
    {
        [Constructable]
        public PaganDruid()
            : base(AIType.AI_Mage, FightMode.Aggressor | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Title = "the druid";

            PaganPeasant.InitBody(this);
            PaganPeasant.InitOutfit(this);

            PaganPeasant.ForceAddItem(this, new Robe(PaganPeasant.RandomClothingHue()));

            SetStr(201, 250);
            SetDex(100);
            SetInt(201, 250);

            SetDamage(10, 23);

            SetSkill(SkillName.Anatomy, 75.0, 97.5);
            SetSkill(SkillName.EvalInt, 82.0, 100.0);
            SetSkill(SkillName.Healing, 75.0, 97.5);
            SetSkill(SkillName.Magery, 82.0, 100.0);
            SetSkill(SkillName.MagicResist, 82.0, 100.0);
            SetSkill(SkillName.Tactics, 82.0, 100.0);

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);
            PackItem(new HealPotion());
            PackItem(new CurePotion());

            Fame = 3000;
        }

        public override bool AlwaysAttackable { get { return true; } }
        public override bool CanBandage { get { return true; } }
        public override bool CanRummageCorpses { get { return true; } }
        public override bool ClickTitle { get { return true; } }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            base.AggressiveAction(aggressor, criminal, source);

            PaganPeasant.HandleAggressiveAction(aggressor, this);
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            return (base.IsEnemy(m, filter) || PaganPeasant.IsPaganEnemy(m));
        }

        public override void GenerateLoot()
        {
            // SP custom

            if (m_Spawning)
            {
            }
            else
            {
            }
        }

        public PaganDruid(Serial serial)
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