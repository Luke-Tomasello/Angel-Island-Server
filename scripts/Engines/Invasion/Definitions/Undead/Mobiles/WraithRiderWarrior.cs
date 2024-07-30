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

/* Scripts/Engines/Invasion/Undead/Mobiles/WraithRiderWarrior.cs
 * ChangeLog
 *  10/22/23, Yoar
 *		Initial Version.
 */

using Server.Engines.Alignment;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Invasion
{
    public class WraithRiderWarrior : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Undead }); } }

        public override bool ShowFameTitle { get { return false; } }

        private bool m_SteedKilled;

        public WraithRiderWarrior()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Title = "the wraith rider";
            Hue = 0x4001;
            BaseSoundID = 0x482;
            IOBAlignment = IOBAlignment.Undead;
            ControlSlots = 10;

            SetStr(361, 400);
            SetDex(91, 135);
            SetInt(76, 100);

            SetHits(451, 500);

            SetSkill(SkillName.Swords, 90.1, 100.0);
            SetSkill(SkillName.Anatomy, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 90.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);
            SetSkill(SkillName.Parry, 60.1, 80.0);

            Fame = 18000;
            Karma = -18000;

            VirtualArmor = 60;

            InitBody();
            InitOutfit();

            BaseMount steed = new HellSteed();
            steed.Tamable = false;
            steed.Rider = this;
        }

        public override bool AlwaysMurderer { get { return true; } }
        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }

        public override void InitBody()
        {
            Female = false;
            Body = 0x190;
            Name = NameList.RandomName("wraithrider");
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            AddImmovable(new BoneArms());
            AddImmovable(new BoneChest());
            AddImmovable(new BoneGloves());
            AddImmovable(new BoneHelm());
            AddImmovable(new BoneLegs());
            AddImmovable(new Shoes());

            if (Utility.RandomBool())
            {
                AddImmovable(new Buckler());
                AddImmovable(new Katana());

                SetDamage(8, 10); // numbers based on LordGuardian
            }
            else
            {
                AddImmovable(new ExecutionersAxe());

                SetDamage(20, 30); // numbers based on Executioner
            }
        }

        private void AddImmovable(Item item)
        {
            item.Movable = false;
            AddItem(item);
        }

        public override bool DeleteCorpseOnDeath { get { return true; } }

        public override bool OnBeforeDeath()
        {
            BaseMount steed = Mount as BaseMount;

            if (steed == null)
            {
                if (!base.OnBeforeDeath())
                    return false;

                if (m_SteedKilled)
                {
                    new Gold(1800).MoveToWorld(Location, Map);

                    // TODO: Drop magic items?

                    DropMagicEquipment(2, 3, 0.60);
                    DropMagicEquipment(2, 3, 0.25);
                }
                else
                {
                    new Gold(1000).MoveToWorld(Location, Map);
                }

                Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

                Delete();

                return true;
            }
            else
            {
                m_SteedKilled = true;

                steed.Rider = null;
                steed.Kill();

                Fame = 23000;
                Karma = -23000;

                Hits = HitsMax;

                return false;
            }
        }

        private void DropMagicEquipment(int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            if (maxLevel > 3)
                maxLevel = 3;

            Cap(ref minLevel, 0, 3);
            Cap(ref maxLevel, 0, 3);

            Item item;

            if (Utility.RandomBool())
                item = Loot.RandomArmorOrShield();
            else
                item = Loot.RandomWeapon();

            if (item == null)
                return;

            item = Loot.ImbueWeaponOrArmor(item, Loot.ScaleOldLevelToImbueLevel(minLevel), Loot.ScaleOldLevelToImbueLevel(maxLevel));

            if (item == null)
                return;

            item.MoveToWorld(Location, Map);
        }

        public WraithRiderWarrior(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_SteedKilled);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_SteedKilled = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}