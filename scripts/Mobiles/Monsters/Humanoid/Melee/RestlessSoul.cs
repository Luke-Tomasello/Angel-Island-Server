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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/RestlessSoul.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Quests;
using Server.Engines.Quests.Haven;
using System.Collections;

namespace Server.Mobiles
{
    [CorpseName("a ghostly corpse")]
    public class RestlessSoul : BaseCreature
    {
        [Constructable]
        public RestlessSoul()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a restless soul";
            Body = 0x3CA;
            Hue = 0x453;

            SetStr(26, 40);
            SetDex(26, 40);
            SetInt(26, 40);

            SetHits(16, 24);

            SetDamage(1, 10);

            SetSkill(SkillName.MagicResist, 20.1, 30.0);
            SetSkill(SkillName.Swords, 20.1, 30.0);
            SetSkill(SkillName.Tactics, 20.1, 30.0);
            SetSkill(SkillName.Wrestling, 20.1, 30.0);

            Fame = 500;
            Karma = -500;

            VirtualArmor = 6;
        }

        public override bool AlwaysAttackable { get { return true; } }

        public override void DisplayPaperdollTo(Mobile to)
        {
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is ContextMenus.PaperdollEntry)
                    list.RemoveAt(i--);
            }
        }

        public override int GetIdleSound()
        {
            return 0x107;
        }

        public override int GetDeathSound()
        {
            return 0xFD;
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            PlayerMobile player = m as PlayerMobile;

            if (player != null && Map == Map.Trammel && X >= 5199 && X <= 5271 && Y >= 1812 && Y <= 1865) // Schmendrick's cave
            {
                QuestSystem qs = player.Quest;

                if (qs is UzeraanTurmoilQuest && qs.IsObjectiveInProgress(typeof(FindSchmendrickObjective)))
                {
                    return false;
                }
            }

            return base.IsEnemy(m, filter);
        }

        public RestlessSoul(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
                PackGold(0, 25);
            else
            {
                if (Core.RuleSets.AllShards)
                {   // non era
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
                    AddLoot(LootPack.Poor);
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