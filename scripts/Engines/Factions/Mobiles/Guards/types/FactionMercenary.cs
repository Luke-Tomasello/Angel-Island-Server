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
    public class FactionMercenary : BaseFactionGuard
    {
        public override GuardAI GuardAI { get { return GuardAI.Melee | GuardAI.Smart; } }

        [Constructable]
        public FactionMercenary() : base("the mercenary")
        {
            GenerateBody(false, true);

            SetStr(116, 125);
            SetDex(61, 85);
            SetInt(81, 95);

            //SetResistance( ResistanceType.Physical, 20, 40 );
            //SetResistance( ResistanceType.Fire, 20, 40 );
            //SetResistance( ResistanceType.Cold, 20, 40 );
            //SetResistance( ResistanceType.Energy, 20, 40 );
            //SetResistance( ResistanceType.Poison, 20, 40 );

            VirtualArmor = 16;

            SetSkill(SkillName.Fencing, 90.0, 100.0);
            SetSkill(SkillName.Wrestling, 90.0, 100.0);
            SetSkill(SkillName.Tactics, 90.0, 100.0);
            SetSkill(SkillName.MagicResist, 90.0, 100.0);
            SetSkill(SkillName.Healing, 90.0, 100.0);
            SetSkill(SkillName.Anatomy, 90.0, 100.0);

            AddItem(new ChainChest());
            AddItem(new ChainLegs());
            AddItem(new RingmailArms());
            AddItem(new RingmailGloves());
            AddItem(new ChainCoif());
            AddItem(new Boots());
            AddItem(Newbied(new ShortSpear()));

            PackItem(new Bandage(Utility.RandomMinMax(20, 30)), lootType: LootType.UnStealable);
            PackStrongPotions(3, 8);
        }

        public FactionMercenary(Serial serial) : base(serial)
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