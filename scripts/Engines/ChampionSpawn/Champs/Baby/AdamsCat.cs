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

/* Scripts\Engines\ChampionSpawn\Champs\Baby\AdamsCat.cs
 *	ChangeLog:
 *	7/1/2023, Adam
 *	    1. Drops 2-3 Champ loot items 'to corpse'
 *	    2. throws a ball of yarn (para)
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  1/11/07, Adam
 *      Select 'None' Skull type for this special champ
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  8/16/06, Rhiannon
 *		Moved speed settings into constructor
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	12/7/04, Adam
 *		non-champ AI
 *		minor tweaks to damage
 *	12/6/05, Adam
 *		Cleanup:
 *			1. get rid of OnDeath() override
 *	8/7/04, Adam
 *		Up the gold a bit, add a bal-o-yarn, and level 4 MID 
 *	7/1/04, Adam
 *		Update OnBeforeDeath() to drop WhiteWyrm'ish loot as we don't want folks farming
 *			Adam's Cat (more loot can be had elsewhere for less bother.)
 * 		Update OnDeath() to skip the skull drop
 *  6/12/04, Adam
 *		Converted Mephitis to Adam's Cat.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/30/04 smerX
 *		Coded/Added special abilities
 *	4/xx/04 Mith
 *		Removed spawn of gold items in pack.
 *
 */

using Server.Engines.ChampionSpawn;
using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("the corpse of Adam's cat")]
    public class AdamsCat : BaseChampion
    {
        public override ChampionSkullType SkullType { get { return ChampionSkullType.None; } }

        [Constructable]
        public AdamsCat()
            : base(AIType.AI_Melee, FightMode.Aggressor, 0.2, 0.4)
        {
            // non-champ AI
            RangePerception = 10;
            RangeFight = 1;

            Body = 201;
            Name = "Adam's cat";
            BaseSoundID = 105;

            SetStr(505, 1000);
            SetDex(102, 300);
            SetInt(402, 600);

            SetHits(3000);
            SetStam(105, 600);

            SetDamage(15, 30);

            SetSkill(SkillName.MagicResist, 70.7, 140.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 60;
        }
        public override void DistributeLoot()
        {
            // do nothing. Lucy and Adam's Cat are baby champs that don't drop all that crazy loot
            //  It's also not distributed, it's on the corpse
        }
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AllShards)
            {
                if (Spawning)
                {
                    PackGold(0);
                }
                else
                {
                    // set this so we don't generate all that crazy champ loot
                    NoKillAwards = true;

                    // special/magic items & rares
                    ChampLootPack.PackChampLoot(this, MagicItemCount: Utility.RandomMinMax(2, 3), SpecialRewardCount: Utility.RandomMinMax(2, 3));

                    int gems = Utility.RandomMinMax(1, 5);
                    for (int i = 0; i < gems; ++i)
                        PackGem();

                    // adam: add 25% chance to get a Random Slayer 
                    if (Utility.Chance(.25))
                    {
                        if (Core.RuleSets.AngelIslandRules())
                            PackSlayerInstrument();
                        else
                            PackSlayerWeapon();
                    }

                    // yarn ;p
                    if (Utility.RandomBool())
                        PackItem(new DarkYarn());
                    else
                        PackItem(new LightYarn());

                    Commands.SplashGold.DoSplashGold(this.Map, this, 10000);
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        public override bool OnBeforeDeath()
        {
            return base.OnBeforeDeath();
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        //public override Poison HitPoison{ get{ return Poison.Lethal; } }

        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            base.Damage(amount, from, source_weapon);

            Mobile m = VerifyValidMobile(from, 9);

            if (m != null)
            {
                ThrowYarn(m, 9);
                new WarpTimer(this, m, 8, TimeSpan.FromSeconds(2.0)).Start();
            }
        }

        public Mobile VerifyValidMobile(Mobile m, int tileRange)
        {
            if (m != null && m is PlayerMobile && m.AccessLevel == AccessLevel.Player || m != null && m is BaseCreature && ((BaseCreature)m).Controlled)
            {
                if (m != null && m.Map == this.Map && m.InRange(this, tileRange) && m.Alive)
                    return m;
            }

            return null;
        }

        private void ThrowYarn(Mobile target, int range)
        {
            Mobile m = VerifyValidMobile(target, range);

            if (m != null && !(m is BaseCreature))
            {
                //this.MovingParticles( m, 0x379F, 7, 0, false, true, 3043, 4043, 0x211 );
                this.MovingParticles(m, 0xE1E/*yarn* /*web 0x10d4*/, 7, 0, false, false, 3043, 4043, 0x211);
                m.Freeze(TimeSpan.FromSeconds(4.0));
                BallOfYarn w = new BallOfYarn(TimeSpan.FromSeconds(15.0), TimeSpan.FromSeconds(4.0));
                w.MoveToWorld(new Point3D(m.X, m.Y, m.Z), m.Map);
            }
        }

        private class WarpTimer : Timer
        {
            private Mobile m_From;
            private Mobile m_To;
            private int m_Range;

            public WarpTimer(Mobile from, Mobile to, int range, TimeSpan delay)
                : base(delay)
            {
                m_From = from;
                m_To = to;
                m_Range = range;
            }

            protected override void OnTick()
            {
                if (m_From != null && m_To != null && !(m_To.Hidden) && m_To.InRange(m_From, m_Range))
                    m_From.Location = new Point3D(m_To.X, m_To.Y, m_To.Z);

                this.Stop();
            }
        }

        public AdamsCat(Serial serial)
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

    public class BallOfYarn : Item
    {
        private TimeSpan m_ParaTime;

        [Constructable]
        public BallOfYarn(TimeSpan delay, TimeSpan paraTime)
            : base(0xE1E/*yarn* /*web 0x10d4*/)
        {
            m_ParaTime = paraTime;

            this.Movable = false;

            new DeletionTimer(delay, this).Start();
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m != null && m is PlayerMobile && m.AccessLevel == AccessLevel.Player || m != null && m is BaseCreature && ((BaseCreature)m).Controlled)
            {
                if (m != null && m.Map == this.Map && m.Alive)
                {
                    m.Freeze(m_ParaTime);
                    m.SendMessage("You become entangled in a ball of yarn!");
                }
            }

            return base.OnMoveOver(m);
        }

        private class DeletionTimer : Timer
        {
            private Item m_ToDelete;

            public DeletionTimer(TimeSpan delay, Item todelete)
                : base(delay)
            {
                m_ToDelete = todelete;
                Priority = TimerPriority.TwentyFiveMS;
            }

            protected override void OnTick()
            {
                m_ToDelete.Delete();
                this.Stop();
            }
        }

        public BallOfYarn(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); //vers
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}