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

/* Scripts/Mobiles/Monsters/AOS/ShadowKnight.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("a shadow knight corpse")]
    public class ShadowKnight : BaseCreature
    {
        public override WeaponAbility GetWeaponAbility()
        {
            return WeaponAbility.CrushingBlow;
        }

        [Constructable]
        public ShadowKnight()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = NameList.RandomName("shadow knight");
            Title = "the Shadow Knight";
            Body = 311;
            BardImmune = true;

            SetStr(250);
            SetDex(100);
            SetInt(100);

            SetHits(2000);

            SetDamage(20, 30);

            //SetSkill(SkillName.Chivalry, 120.0);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 100.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 120.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 25000;
            Karma = -25000;

            VirtualArmor = 54;
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (!Summoned && !NoKillAwards && DemonKnight.CheckArtifactChance(this))
                DemonKnight.DistributeArtifact(this);
        }

        public override int GetIdleSound()
        {
            return 0x2CE;
        }

        public override int GetDeathSound()
        {
            return 0x2C1;
        }

        public override int GetHurtSound()
        {
            return 0x2D1;
        }

        public override int GetAttackSound()
        {
            return 0x2C8;
        }

        private Timer m_SoundTimer;
        private bool m_HasTeleportedAway;

        public override void OnCombatantChange()
        {
            base.OnCombatantChange();

            if (Hidden && Combatant != null)
                Combatant = null;
        }

        public virtual void SendTrackingSound()
        {
            if (Hidden)
            {
                Effects.PlaySound(this.Location, this.Map, 0x2C8);
                Combatant = null;
            }
            else
            {
                Frozen = false;

                if (m_SoundTimer != null)
                    m_SoundTimer.Stop();

                m_SoundTimer = null;
            }
        }

        public override void OnThink()
        {
            if (!m_HasTeleportedAway && Hits < (HitsMax / 2))
            {
                Map map = this.Map;

                if (map != null)
                {
                    // try 10 times to find a teleport spot
                    for (int i = 0; i < 10; ++i)
                    {
                        int x = X + (Utility.RandomMinMax(5, 10) * (Utility.RandomBool() ? 1 : -1));
                        int y = Y + (Utility.RandomMinMax(5, 10) * (Utility.RandomBool() ? 1 : -1));
                        int z = Z;

                        if (!Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.requireSurface))
                            continue;

                        Point3D from = this.Location;
                        Point3D to = new Point3D(x, y, z);

                        this.Location = to;
                        this.ProcessDelta();
                        this.Hidden = true;
                        this.Combatant = null;

                        Effects.SendLocationParticles(EffectItem.Create(from, map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                        Effects.SendLocationParticles(EffectItem.Create(to, map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

                        Effects.PlaySound(to, map, 0x1FE);

                        m_HasTeleportedAway = true;
                        m_SoundTimer = Timer.DelayCall(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(2.5), new TimerCallback(SendTrackingSound));

                        Frozen = true;

                        break;
                    }
                }
            }

            base.OnThink();
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }

        public ShadowKnight(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            PackGem();
            PackGold(700, 1100);
            PackMagicEquipment(1, 3, 0.80, 0.80);
            PackMagicEquipment(1, 3, 0.40, 0.40);
        }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.FeyAndUndead; }
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

            if (BaseSoundID == 357)
                BaseSoundID = -1;
        }
    }
}