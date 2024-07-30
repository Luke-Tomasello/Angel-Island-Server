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

/* Scripts\Engines\ChampionSpawn\Champs\Rikktor.cs
 * ChangeLog:
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  8/16/06, Rhiannon
 *		Added speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 8 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/30/04 smerX
 *		Changed how special abilities work
 *	4/xx/04 Mith
 *		Removed spawn of gold items in pack
 *
 */

using Server.Engines.ChampionSpawn;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class Rikktor : BaseChampion
    {
        public override ChampionSkullType SkullType { get { return ChampionSkullType.Power; } }

        [Constructable]
        public Rikktor()
            : base(AIType.AI_Melee, 0.175, 0.350)
        {
            Body = 172;
            Name = "Rikktor";

            SetStr(701, 900);
            SetDex(201, 350);
            SetInt(51, 100);

            SetHits(3000);
            SetStam(203, 650);

            SetDamage(28, 55);

            SetSkill(SkillName.Anatomy, 100.0);
            SetSkill(SkillName.MagicResist, 140.2, 160.0);
            SetSkill(SkillName.Tactics, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 130;
        }
        #region DraggingMitigation
        public override List<Mobile> GetDraggingMitigationHelpers()
        {
            List<Mobile> helpers = new List<Mobile>();
            // okay, now we take action against this bothersome individual!
            for (int ix = 0; ix < 2; ix++)
            {   // these will be out helpers
                if (Utility.RandomBool())
                    helpers.Add(new Dragon());
                else
                    helpers.Add(new OphidianKnight());
            }
            return helpers;
        }
        #endregion DraggingMitigation
        public override void GenerateLoot()
        {
            if (IsChampion)
            {
                if (!Core.RuleSets.AngelIslandRules())
                {
                    AddLoot(LootPack.UltraRich, 4);
                }
            }
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override ScaleType ScaleType { get { return ScaleType.All; } }
        public override int Scales { get { return 20; } }

        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            base.Damage(amount, from, source_weapon);

            if (0.30 >= Utility.RandomDouble())
                Earthquake();
        }

        public void Earthquake()
        {
            Map map = this.Map;

            if (map == null)
                return;

            ArrayList targets = new ArrayList();

            IPooledEnumerable eable = this.GetMobilesInRange(8);
            foreach (Mobile m in eable)
            {
                if (m == this || !CanBeHarmful(m))
                    continue;

                if (m is BaseCreature && ((BaseCreature)m).Controlled || m is BaseCreature && ((BaseCreature)m).Summoned)
                    targets.Add(m);
                else if (m.Player)
                    targets.Add(m);
            }
            eable.Free();

            PlaySound(0x2F3);

            for (int i = 0; i < targets.Count; ++i)
            {
                Mobile m = (Mobile)targets[i];

                double damage = m.Hits * 0.6;

                if (damage < 10.0)
                    damage = 10.0;
                else if (damage > 75.0)
                    damage = 75.0;

                DoHarmful(m);

                AOS.Damage(m, this, (int)damage, 100, 0, 0, 0, 0, this);

                if (m.Alive && m.Body.IsHuman && !m.Mounted)
                    m.Animate(20, 7, 1, true, false, 0); // take hit
            }
        }

        public override int GetAngerSound()
        {
            return Utility.Random(0x2CE, 2);
        }

        public override int GetIdleSound()
        {
            return 0x2D2;
        }

        public override int GetAttackSound()
        {
            return Utility.Random(0x2C7, 5);
        }

        public override int GetHurtSound()
        {
            return 0x2D1;
        }

        public override int GetDeathSound()
        {
            return 0x2CC;
        }

        public Rikktor(Serial serial)
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
        }
    }
}