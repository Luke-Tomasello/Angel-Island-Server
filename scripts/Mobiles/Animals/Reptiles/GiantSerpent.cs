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

/* Scripts/Mobiles/Animals/Reptiles/GiantSerpent.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a giant serpent corpse")]
    [TypeAlias("Server.Mobiles.Serpant")]
    public class GiantSerpent : BaseCreature
    {
        [Constructable]
        public GiantSerpent()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = "a giant serpent";
            Body = 0x15;
            // 10/28/22, Adam: giant serpent's have a beautiful native graphic. We'll make sure
            //  to spawn those 1/2 the time (no hue).
            Hue = Utility.RandomBool() ? Utility.RandomSnakeHue() : 0;
            BaseSoundID = 219;

            SetStr(186, 215);
            SetDex(56, 80);
            SetInt(66, 85);

            SetHits(112, 129);
            SetMana(0);

            SetDamage(7, 17);

            SetSkill(SkillName.Poisoning, 70.1, 100.0);
            SetSkill(SkillName.MagicResist, 25.1, 40.0);
            SetSkill(SkillName.Tactics, 65.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 2500;
            Karma = -2500;

            VirtualArmor = 32;
        }

        public override Poison PoisonImmune { get { return Poison.Greater; } }
        public override Poison HitPoison { get { return (0.8 >= Utility.RandomDouble() ? Poison.Greater : Poison.Deadly); } }

        public override int Meat { get { return 4; } }
        public override int Hides { get { return Core.RuleSets.AngelIslandRules() ? 15 : 20; } }
        public override HideType HideType { get { return HideType.Spined; } }

        public GiantSerpent(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(50, 100);
                PackItem(new Bone());
                // TODO: Body parts
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020210164600/uo.stratics.com/hunters/giantserpent.shtml
                    // 4 Raw Ribs (carved), 20 Hides (carved)
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
                    if (this.Spawning)
                    {
                        PackItem(new Bone());
                        // TODO: Body parts
                    }
                    AddLoot(LootPack.Average);
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

            if (BaseSoundID == -1)
                BaseSoundID = 219;
        }
    }
}