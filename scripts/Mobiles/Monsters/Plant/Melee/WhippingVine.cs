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

/* Scripts/Mobiles/Monsters/Plants/Melee/WhippingVine.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  9/30/04,Pigpen
 *  	Added 4 Fertile dirt and Decorative vines to loot.
 *		1 hour later fixed to have 5-10 Fertile dirt drop.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a whipping vine corpse")]
    public class WhippingVine : BaseCreature
    {
        [Constructable]
        public WhippingVine()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a whipping vine";
            Body = 8;
            Hue = 0x851;
            BaseSoundID = 352;

            SetStr(251, 300);
            SetDex(76, 100);
            SetInt(26, 40);

            SetMana(0);

            SetDamage(7, 25);

            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 70.0);
            SetSkill(SkillName.Wrestling, 70.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 45;
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public WhippingVine(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackReg(6);
                PackItem(new FertileDirt(Utility.RandomMinMax(5, 10)));

                if (0.2 >= Utility.RandomDouble())
                    PackItem(new ExecutionersCap());

                PackItem(new Vines());
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // no pages until...
                    // http://web.archive.org/web/20021019193742/uo.stratics.com/hunters/whippingvine.shtml
                    // Reagents, Fertile Dirt, Executioner's Cap, Vines
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                        PackReg(3);
                        PackItem(new FertileDirt(Utility.RandomMinMax(1, 10)));

                        if (0.2 >= Utility.RandomDouble())
                            PackItem(new ExecutionersCap());

                        PackItem(new Vines());
                    }
                }
                else
                {   // Standard RunUO
                    if (Spawning)
                    {
                        PackReg(3);
                        PackItem(new FertileDirt(Utility.RandomMinMax(1, 10)));

                        if (0.2 >= Utility.RandomDouble())
                            PackItem(new ExecutionersCap());

                        PackItem(new Vines());
                    }
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