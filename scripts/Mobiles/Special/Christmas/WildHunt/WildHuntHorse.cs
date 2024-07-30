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

/* Scripts/Mobiles/Special/Christmas/WildHunt/WildHuntHorse.cs
 * ChangeLog
 *  1/1/24, Yoar
 *		Initial Version.
 */

using Server.Items;
using System;

namespace Server.Mobiles.WildHunt
{
    [CorpseName("a horse corpse")]
    public class WildHuntHorse : BaseMount
    {
        private Timer m_PoofTimer;
        private int m_PoofTicks;

        [Constructable]
        public WildHuntHorse()
            : base("a horse", 0xE2, 0x3EA0, AIType.AI_Melee, FightMode.All | FightMode.Aggressor, 10, 1, 0.2, 0.4)
        {
            int rnd = Utility.Random(4);

            Body = m_IDs[rnd * 2];
            ItemID = m_IDs[rnd * 2 + 1];
            BaseSoundID = 0xA8;
            Hue = 0x4001;

            SetStr(300);
            SetDex(100);
            SetInt(50);

            SetHits(250);

            SetDamage(8, 14);

            SetSkill(SkillName.MagicResist, 50.0);
            SetSkill(SkillName.Tactics, 70.0);
            SetSkill(SkillName.Wrestling, 70.0);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 30;

            StartPoofTimer();
        }

        private static readonly int[] m_IDs = new int[]
            {
                0xC8, 0x3E9F,
                0xE2, 0x3EA0,
                0xE4, 0x3EA1,
                0xCC, 0x3EA2
            };

        private void StartPoofTimer()
        {
            StopPoofTimer();

            m_PoofTimer = Timer.DelayCall(TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10.0), CheckPoof);
        }

        private void StopPoofTimer()
        {
            if (m_PoofTimer != null)
            {
                m_PoofTimer.Stop();
                m_PoofTimer = null;
            }
        }

        private void CheckPoof()
        {
            if (Map != Map.Internal && !Controlled && ControlMaster == null && Rider == null && Combatant == null)
            {
                if (++m_PoofTicks >= 12)
                {
                    Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

                    Delete();
                }
            }
            else
            {
                m_PoofTicks = 0;
            }
        }

        public override bool DeleteCorpseOnDeath { get { return true; } }
        public override bool DropCorpseItems { get { return true; } }

        public override bool OnBeforeDeath()
        {
            if (!base.OnBeforeDeath())
                return false;

            Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

            return true;
        }

        public override void OnAfterDelete()
        {
            StopPoofTimer();

            base.OnAfterDelete();
        }

        public WildHuntHorse(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            StartPoofTimer();
        }
    }
}