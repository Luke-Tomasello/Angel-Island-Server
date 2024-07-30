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

/* Scripts\Engines\ChampionSpawn\Champs\Semidar.cs
 * CHANGELOG
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  03/31/07, plasma
 *      Added LOS check to life drain attack
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  8/16/06, Rhiannon
 *		Added speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  3/23/04, mith
 *		Removed spawn of gold items in pack.
*/

using Server.Engines.ChampionSpawn;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class Semidar : BaseChampion
    {

        public override ChampionSkullType SkullType { get { return ChampionSkullType.Pain; } }

        [Constructable]
        public Semidar()
            : base(AIType.AI_Mage, 0.175, 0.350)
        {
            Name = "Semidar";
            Body = 174;
            BaseSoundID = 0x4B0;

            SetStr(502, 600);
            SetDex(102, 200);
            SetInt(601, 750);

            SetHits(1500);
            SetStam(103, 250);

            SetDamage(29, 35);

            SetSkill(SkillName.EvalInt, 95.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 105.0);
            SetSkill(SkillName.Meditation, 95.1, 100.0);
            SetSkill(SkillName.MagicResist, 120.2, 140.0);
            SetSkill(SkillName.Tactics, 90.1, 105.0);
            SetSkill(SkillName.Wrestling, 90.1, 105.0);

            Fame = 24000;
            Karma = -24000;

            VirtualArmor = 20;
        }
        #region DraggingMitigation
        public override List<Mobile> GetDraggingMitigationHelpers()
        {
            List<Mobile> helpers = new List<Mobile>();
            // okay, now we take action against this bothersome individual!
            for (int ix = 0; ix < 2; ix++)
            {   // these will be out helpers
                if (Utility.RandomBool())
                    helpers.Add(new Daemon());
                else
                    helpers.Add(new Succubus());
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
                    AddLoot(LootPack.FilthyRich);
                }
            }
        }

        public override AuraType MyAura { get { return AuraType.Hate; } }
        public override int AuraRange { get { return 5; } }
        public override int AuraMin { get { return 5; } }
        public override int AuraMax { get { return 10; } }
        public override TimeSpan NextAuraDelay { get { return TimeSpan.FromSeconds(4.0); } }


        public override bool Unprovokable { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }

        public override void CheckReflect(Mobile caster, ref bool reflect)
        {
            if (caster.Body.IsMale)
                reflect = true; // Always reflect if caster isn't female
        }

        public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
            if (caster.Body.IsMale)
                scalar = 20; // Male bodies always reflect.. damage scaled 20x
        }

        public void DrainLife()
        {
            ArrayList list = new ArrayList();
            //pla: Added LOS check!
            IPooledEnumerable eable = this.GetMobilesInRange(2);
            foreach (Mobile m in eable)
            {
                if (m == this || !CanBeHarmful(m) || !InLOS(m))
                    continue;

                if (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned || ((BaseCreature)m).Team != this.Team))
                    list.Add(m);
                else if (m.Player)
                    list.Add(m);
            }
            eable.Free();

            foreach (Mobile m in list)
            {
                DoHarmful(m);

                m.FixedParticles(0x374A, 10, 15, 5013, 0x496, 0, EffectLayer.Waist);
                m.PlaySound(0x231);

                m.SendMessage("You feel the life drain out of you!");

                int toDrain = Utility.RandomMinMax(10, 40);

                Hits += toDrain;
                m.Damage(toDrain, this, this);
            }
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (0.25 >= Utility.RandomDouble())
                DrainLife();
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (0.25 >= Utility.RandomDouble())
                DrainLife();
        }

        public Semidar(Serial serial)
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