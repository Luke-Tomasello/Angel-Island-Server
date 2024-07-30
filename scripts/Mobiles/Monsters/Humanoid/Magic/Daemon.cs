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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/Daemon.cs
 * ChangeLog
 *	7/16/10, adam
 *		o increase average wrestling
 *		o increase average magery
 *		o new skill meditation
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	7/2/04, Adam
 *		Change chance to drop a magic item to 30% 
 *		add a 5% chance for a bonus drop at next intensity level
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Factions;

namespace Server.Mobiles
{
    [CorpseName("a daemon corpse")]
    public class Daemon : BaseCreature
    {
        public override double DispelDifficulty { get { return 125.0; } }
        public override double DispelFocus { get { return 45.0; } }

        public override Faction FactionAllegiance { get { return Shadowlords.Instance; } }
        public override Ethics.Ethic EthicAllegiance { get { return Ethics.Ethic.Evil; } }

        [Constructable]
        public Daemon()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {

            BaseSoundID = 357;

            SetStr(476, 505);
            SetDex(76, 95);
            SetInt(301, 325);
            SetHits(286, 303);
            SetDamage(7, 14);

            SetSkill(SkillName.EvalInt, 70.1, 80.0);
            SetSkill(SkillName.Magery, 70.1, 80.0);
            SetSkill(SkillName.MagicResist, 85.1, 95.0);
            SetSkill(SkillName.Tactics, 70.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 15000;
            Karma = -15000;

            InitBody();
            InitOutfit();

            VirtualArmor = 58;
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=749
            ControlSlots = 4;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public Daemon(bool summoned)
            : this()
        {
            if (summoned == true)
            {
                SetStr(476, 505);
                SetDex(76, 95);
                SetInt(301, 325);
                SetHits(286, 303);
                SetDamage(16 - 3, 16 + 3);

                SetSkill(SkillName.EvalInt, 70.1, 80.0);
                SetSkill(SkillName.Magery, 90 - 10, 90 + 10);
                SetSkill(SkillName.MagicResist, 85.1, 95.0);
                SetSkill(SkillName.Tactics, 70.1, 80.0);
                SetSkill(SkillName.Wrestling, 90 - 10, 90 + 10);
                SetSkill(SkillName.Meditation, 50 - 10, 50.0 + 10);
            }
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override Poison PoisonImmune { get { return Poison.Regular; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 4 : 4; } }
        public override int Meat { get { return 1; } }

        public override void InitBody()
        {
            Name = NameList.RandomName("daemon");
            Body = 9;
        }
        public override void InitOutfit()
        {

        }

        public Daemon(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(300, 400);
                PackScroll(1, 7);
                PackScroll(1, 7);
                PackMagicEquipment(1, 2, 0.40, 0.40);
                PackMagicEquipment(1, 2, 0.05, 0.05);

                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020220114823/uo.stratics.com/hunters/daemon.shtml
                    // 	150 to 600 Gold, Magic items, Gems, Scrolls, Level 4 treasure maps, 1 Raw Ribs (carved)

                    if (Spawning)
                    {
                        PackGold(150, 600);
                    }
                    else
                    {
                        PackMagicStuff(1, 2, 0.40);
                        PackMagicStuff(1, 2, 0.05);
                        PackGem(1, .9);
                        PackGem(1, .5);
                        PackScroll(1, 6, .9);
                        PackScroll(1, 6, .5);
                    }
                }
                else
                {
                    AddLoot(LootPack.Rich);
                    AddLoot(LootPack.Average, 2);
                    AddLoot(LootPack.MedScrolls, 2);
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