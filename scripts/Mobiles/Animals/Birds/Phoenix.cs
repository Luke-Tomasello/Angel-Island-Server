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

/* Scripts/Mobiles/Animals/Birds/Phoenix.cs
 * ChangeLog
 *  5/30/2023, Adam
 *      Added fire aura
 *      http://uo.stratics.com/hunters/phoenix.shtml
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a phoenix corpse")]
    public class Phoenix : BaseCreature
    {
        [Constructable]
        public Phoenix()
            : base(AIType.AI_Mage, FightMode.Aggressor, 10, 1, 0.2, 0.4)
        {
            Name = "a phoenix";
            Body = 5;
            Hue = 0x674;
            BaseSoundID = 0x8F;

            SetStr(504, 700);
            SetDex(202, 300);
            SetInt(504, 700);

            SetHits(340, 383);

            SetDamage(25);

            SetSkill(SkillName.EvalInt, 90.2, 100.0);
            SetSkill(SkillName.Magery, 90.2, 100.0);
            SetSkill(SkillName.Meditation, 75.1, 100.0);
            SetSkill(SkillName.MagicResist, 86.0, 135.0);
            SetSkill(SkillName.Tactics, 80.1, 90.0);
            SetSkill(SkillName.Wrestling, 90.1, 100.0);

            Fame = 15000;
            Karma = 0;

            VirtualArmor = 60;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override int Meat { get { return 1; } }
        public override MeatType MeatType { get { return MeatType.Bird; } }
        public override int Feathers { get { return 36; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 4 : 0; } }
        public override AuraType MyAura { get { return AuraType.Fire; } }

        public Phoenix(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(500, 600);
                PackScroll(2, 7);
                PackMagicEquipment(2, 3, 0.50, 0.50);
                PackMagicEquipment(2, 3, 0.10, 0.10);
                PackMagicItem(1, 2, 1.0);
                PackMagicItem(1, 2, 0.70);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020403082709/uo.stratics.com/hunters/phoenix.shtml
                    // 600 - 1000 Gold, Magic Items, 36 feathers, raw bird

                    if (Spawning)
                    {
                        PackGold(600, 1000);
                    }
                    else
                    {
                        PackMagicEquipment(2, 3);
                        PackMagicItem(1, 2);
                    }
                }
                else
                {
                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Rich);
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