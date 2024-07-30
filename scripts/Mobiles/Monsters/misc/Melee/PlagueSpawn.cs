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

/* Scripts/Mobiles/Monsters/Misc/Melee/PlagueSpawn.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System;
using System.Collections;

namespace Server.Mobiles
{
    [CorpseName("a plague spawn corpse")]
    public class PlagueSpawn : BaseCreature
    {
        private Mobile m_Owner;
        private DateTime m_ExpireTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ExpireTime
        {
            get { return m_ExpireTime; }
            set { m_ExpireTime = value; }
        }

        [Constructable]
        public PlagueSpawn()
            : this(null)
        {
        }

        public override bool AlwaysMurderer { get { return true; } }

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

        public override void OnThink()
        {
            bool expired;

            expired = (DateTime.UtcNow >= m_ExpireTime);

            if (!expired && m_Owner != null)
                expired = m_Owner.Deleted || Map != m_Owner.Map || !InRange(m_Owner, 16);

            if (expired)
            {
                PlaySound(GetIdleSound());
                Delete();
            }
            else
            {
                base.OnThink();
            }
        }

        public PlagueSpawn(Mobile owner)
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            m_Owner = owner;
            m_ExpireTime = DateTime.UtcNow + TimeSpan.FromMinutes(1.0);

            Name = "a plague spawn";
            // 10/16/23, Yoar: Let's use slime hues here instead of these jarring colors
#if false
            Hue = Utility.Random(0x11, 15);
#else
            Hue = Utility.RandomSlimeHue();
#endif

            switch (Utility.Random(12))
            {
                case 0: // earth elemental
                    Body = 14;
                    BaseSoundID = 268;
                    break;
                case 1: // headless one
                    Body = 31;
                    BaseSoundID = 0x39D;
                    break;
                case 2: // person
                    Body = Utility.RandomList(400, 401);
                    break;
                case 3: // gorilla
                    Body = 0x1D;
                    BaseSoundID = 0x9E;
                    break;
                case 4: // serpent
                    Body = 0x15;
                    BaseSoundID = 0xDB;
                    break;
                default:
                case 5: // slime
                    Body = 51;
                    BaseSoundID = 456;
                    break;
            }

            SetStr(201, 300);
            SetDex(80);
            SetInt(16, 20);

            SetHits(121, 180);

            SetDamage(11, 17);

            SetSkill(SkillName.MagicResist, 25.0);
            SetSkill(SkillName.Tactics, 25.0);
            SetSkill(SkillName.Wrestling, 50.0);

            Fame = 1000;
            Karma = -1000;

            VirtualArmor = 20;
        }

        public PlagueSpawn(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
                PackGold(0, 25);
            else
            {
                AddLoot(LootPack.Poor);
                AddLoot(LootPack.Gems);
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