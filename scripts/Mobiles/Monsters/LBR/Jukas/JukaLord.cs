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

/* Scripts/Mobiles/Monsters/LBR/Jukas/JukaLord.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	6/11/04, mith
 *		Moved the equippable combat items out of OnBeforeDeath()
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a juka corpse")] // Why is this 'juka' and warriors 'jukan' ? :-(
    public class JukaLord : BaseCreature
    {
        [Constructable]
        public JukaLord()
            : base(AIType.AI_Archer, FightMode.All | FightMode.Closest, 10, 3, 0.25, 0.5)
        {
            Name = "a juka lord";
            Body = 766;
            BardImmune = true;

            SetStr(401, 500);
            SetDex(81, 100);
            SetInt(151, 200);

            SetHits(241, 300);

            SetDamage(10, 12);

            SetSkill(SkillName.Anatomy, 90.1, 100.0);
            SetSkill(SkillName.Archery, 95.1, 100.0);
            SetSkill(SkillName.Healing, 80.1, 100.0);
            SetSkill(SkillName.MagicResist, 120.1, 130.0);
            SetSkill(SkillName.Swords, 90.1, 100.0);
            SetSkill(SkillName.Tactics, 95.1, 100.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = -15000;

            VirtualArmor = 28;

            AddItem(new JukaBow());
            // TODO: Bandage self
        }

        public override void OnDamage(int amount, Mobile from, bool willKill, DamageContext context)
        {
            if (from != null && !willKill && amount > 5 && from.Player && 5 > Utility.Random(100))
            {
                string[] toSay = new string[]
                    {
                        "{0}!!  You will have to do better than that!",
                        "{0}!!  Prepare to meet your doom!",
                        "{0}!!  My armies will crush you!",
                        "{0}!!  You will pay for that!"
                    };

                this.Say(true, string.Format(toSay[Utility.Random(toSay.Length)], from.Name));
            }

            base.OnDamage(amount, from, willKill, context);
        }

        public override int GetIdleSound()
        {
            return 0x263;
        }

        public override int GetHurtSound()
        {
            return 0x1D0;
        }

        public override int GetDeathSound()
        {
            return 0x28D;
        }

        public override bool AlwaysMurderer { get { return true; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override int Meat { get { return 1; } }

        public JukaLord(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // https://www.uoguide.com/Juka_Lord
            // ML-era creature (Samurai Empire). No Stratics-era entry.
            // 400 - 600 Gold
            // Special: 4-8 Gems, Magic Weapons/Armor
            // Note: Excludes ML/SE systems (faction items, minor artifacts, etc.)

            if (Spawning)
            {
                PackGold(400, 600);
            }
            else
            {
                PackGem(4, 8);

                PackMagicEquipment(3, 4, 1.0, 1.0);
                PackMagicEquipment(3, 4, 0.50, 0.50);
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