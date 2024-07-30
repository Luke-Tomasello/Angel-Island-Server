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
using Server.Misc;
using System;

namespace Server.Mobiles
{
    [CorpseName("a dark wisp corpse")]
    public class DarkWisp : BaseCreature
    {
        public override InhumanSpeech SpeechType { get { return InhumanSpeech.Wisp; } }

        public override Ethics.Ethic EthicAllegiance { get { return Ethics.Ethic.Evil; } }

        public override TimeSpan ReacquireDelay { get { return TimeSpan.FromSeconds(1.0); } }

        // they seem to be red!
        // http://www.uoguide.com/index.php?title=Dark_Wisp&action=historysubmit&diff=42259&oldid=39297
        public override bool AlwaysMurderer { get { return true; } }

        // An evil version of the normal Wisp. These dark wisps attack on sight and are slightly stronger than their neutral cousins. 
        //	They should not be mistaken for the Shadow Wisps, doing so could be very lethal.
        // http://web.archive.org/web/20080804183753/uo.stratics.com/database/view.php?db_content=hunters&id=364
        [Constructable]
        public DarkWisp()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "a dark wisp";
            Body = 165;
            BaseSoundID = 466;

            SetStr(196, 225);
            SetDex(196, 225);
            SetInt(196, 225);

            SetHits(118, 135);

            SetDamage(17, 18);

            //SetDamageType( ResistanceType.Physical, 50 );
            //SetDamageType( ResistanceType.Energy, 50 );

            //SetResistance( ResistanceType.Physical, 35, 45 );
            //SetResistance( ResistanceType.Fire, 20, 40 );
            //SetResistance( ResistanceType.Cold, 10, 30 );
            //SetResistance( ResistanceType.Poison, 5, 10 );
            //SetResistance( ResistanceType.Energy, 50, 70 );

            SetSkill(SkillName.EvalInt, 80.0);
            SetSkill(SkillName.Magery, 80.0);
            SetSkill(SkillName.MagicResist, 80.0);
            SetSkill(SkillName.Tactics, 80.0);
            SetSkill(SkillName.Wrestling, 80.0);

            Fame = 4000;
            Karma = -4000;

            VirtualArmor = 40;

            AddItem(new LightSource());
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20080804183753/uo.stratics.com/database/view.php?db_content=hunters&id=364
                    // 2008 is the best we can find
                    if (Spawning)
                    {
                        PackGold(400, 500);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    if (Spawning)
                    {
                    }
                    else
                    {
                        AddLoot(LootPack.Rich);
                        AddLoot(LootPack.Average);
                    }
                }
            }
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.FeyAndUndead; }
        }

        public DarkWisp(Serial serial)
            : base(serial)
        {
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