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

using Server.Items;

namespace Server.Factions
{
    public class FactionKnight : BaseFactionGuard
    {
        public override GuardAI GuardAI { get { return GuardAI.Magic | GuardAI.Melee | GuardAI.Smart | GuardAI.Curse | GuardAI.Bless; } }

        [Constructable]
        public FactionKnight() : base("the knight")
        {
            GenerateBody(false, false);

            SetStr(126, 150);
            SetDex(61, 85);
            SetInt(81, 95);

            //SetDamageType( ResistanceType.Physical, 100 );

            //SetResistance( ResistanceType.Physical, 30, 50 );
            //SetResistance( ResistanceType.Fire, 30, 50 );
            //SetResistance( ResistanceType.Cold, 30, 50 );
            //SetResistance( ResistanceType.Energy, 30, 50 );
            //SetResistance( ResistanceType.Poison, 30, 50 );

            VirtualArmor = 24;

            SetSkill(SkillName.Swords, 100.0, 110.0);
            SetSkill(SkillName.Wrestling, 100.0, 110.0);
            SetSkill(SkillName.Tactics, 100.0, 110.0);
            SetSkill(SkillName.MagicResist, 100.0, 110.0);
            SetSkill(SkillName.Healing, 100.0, 110.0);
            SetSkill(SkillName.Anatomy, 100.0, 110.0);

            SetSkill(SkillName.Magery, 100.0, 110.0);
            SetSkill(SkillName.EvalInt, 100.0, 110.0);
            SetSkill(SkillName.Meditation, 100.0, 110.0);

            AddItem(Immovable(Rehued(new ChainChest(), 2125)));
            AddItem(Immovable(Rehued(new ChainLegs(), 2125)));
            AddItem(Immovable(Rehued(new ChainCoif(), 2125)));
            AddItem(Immovable(Rehued(new PlateArms(), 2125)));
            AddItem(Immovable(Rehued(new PlateGloves(), 2125)));

            AddItem(Immovable(Rehued(new BodySash(), 1254)));
            AddItem(Immovable(Rehued(new Kilt(), 1254)));
            AddItem(Immovable(Rehued(new Sandals(), 1254)));

            AddItem(Newbied(new Bardiche()));

            PackItem(new Bandage(Utility.RandomMinMax(30, 40)), lootType: LootType.UnStealable);
            PackStrongPotions(6, 12);
        }

        public FactionKnight(Serial serial) : base(serial)
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