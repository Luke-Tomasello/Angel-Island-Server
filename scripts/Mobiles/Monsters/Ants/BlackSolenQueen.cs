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

/* ./Scripts/Mobiles/Monsters/Ants/BlackSolenQueen.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
*/

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a solen queen corpse")]
    public class BlackSolenQueen : BaseCreature
    {
        [Constructable]
        public BlackSolenQueen()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a black solen queen";
            Body = 807;
            BaseSoundID = 959;
            Hue = 0x453;

            SetStr(296, 320);
            SetDex(121, 145);
            SetInt(76, 100);

            SetHits(151, 162);

            SetDamage(10, 15);

            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 90.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 45;
        }

        public override int GetAngerSound()
        {
            return 0x259;
        }

        public override int GetIdleSound()
        {
            return 0x259;
        }

        public override int GetAttackSound()
        {
            return 0x195;
        }

        public override int GetHurtSound()
        {
            return 0x250;
        }

        public override int GetDeathSound()
        {
            return 0x25B;
        }

        public BlackSolenQueen(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                SolenHelper.PackPicnicBasket(this);
                PackGold(100, 200);
                // TODO: 5-25 zoogi fungus
                // TODO: crystal ball of pet summoning
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        SolenHelper.PackPicnicBasket(this);

                        PackItem(new ZoogiFungus((Utility.RandomDouble() > 0.05) ? 5 : 25));

                        // probably not on siege
                        //if (Utility.RandomDouble() < 0.05)
                        //PackItem(new BallOfSummoning());
                    }

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