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

/* Scripts/Mobiles/Monsters/Ants/AntLion.cs
 * ChangeLog
 *	1/28/11, Adam
 *		Fix a bug in Deserialize
 *	12/28/10, adam
 *		updated from:
 *		http://code.google.com/p/runuomondains/source/browse/trunk/Scripts/Items/Containers/UnknownSkeletons.cs?spec=svn121&r=121
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    [CorpseName("an ant lion corpse")]
    public class AntLion : BaseCreature
    {
        [Constructable]
        public AntLion()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "an ant lion";
            Body = 787;
            BaseSoundID = 1006;
            SpeechHue = 0x3B2;

            SetStr(296, 320);
            SetDex(81, 105);
            SetInt(36, 60);

            SetHits(151, 162);

            SetDamage(7, 21);

            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 90.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 45;
        }

        public override int GetAngerSound()
        {
            return 0x5A;
        }

        public override int GetIdleSound()
        {
            return 0x5A;
        }

        public override int GetAttackSound()
        {
            return 0x164;
        }

        public override int GetHurtSound()
        {
            return 0x187;
        }

        public override int GetDeathSound()
        {
            return 0x1BA;
        }

        public AntLion(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGem(2);
                PackItem(new Bone());
                PackGold(100, 150);

                int amount = Utility.RandomMinMax(8, 12);

                switch (Utility.Random(4))
                {
                    case 0: PackItem(new DullCopperOre(amount)); break;
                    case 1: PackItem(new ShadowIronOre(amount)); break;
                    case 2: PackItem(new CopperOre(amount)); break;
                    case 3: PackItem(new BronzeOre(amount)); break;
                }

                // TODO: 1-5 fertile dirt
                // TODO: skeleton
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20021013185154/uo.stratics.com/hunters/antlion.shtml
                    // 100-150 Gold, Gems, 1-10 Large Colored Ore (Dull Copper to Bronze), 1-5 Fertile Dirt, Bones, An Unknown Adventurer's Skeleton
                    if (Spawning)
                    {
                        PackGold(100, 150);
                    }
                    else
                    {
                        PackGem(2);
                        switch (Utility.Random(4))
                        {
                            case 0: PackItem(new DullCopperOre(Utility.RandomMinMax(1, 10))); break;
                            case 1: PackItem(new ShadowIronOre(Utility.RandomMinMax(1, 10))); break;
                            case 2: PackItem(new CopperOre(Utility.RandomMinMax(1, 10))); break;
                            case 3: PackItem(new BronzeOre(Utility.RandomMinMax(1, 10))); break;
                        }
                        PackItem(new FertileDirt(Utility.RandomMinMax(1, 5)));
                        PackItem(new Bone(3));

                        // http://www.google.com/codesearch/p?hl=en#biPgqLK3B_w/trunk/Scripts/Mobiles/Monsters/Ants/AntLion.cs&q=antlion%20package:http://runuomondains%5C.googlecode%5C.com&sa=N&cd=1&ct=rc
                        // (Bones, An Unknown Adventurer's Skeleton)
                        for (int i = 0; i < 3; i++)
                        {
                            switch (Utility.Random(5))
                            {
                                case 0: PackItem(new BoneShards()); break;
                                case 1: PackItem(new SpineBone()); break;
                                case 2: PackItem(new RibCage()); break; ;
                                case 3: PackItem(new PelvisBone()); break;
                                case 4: PackItem(new Skull()); break;
                            }
                        }

                        if (0.07 >= Utility.RandomDouble())
                        {
                            switch (Utility.Random(3))
                            {
                                case 0: PackItem(new UnknownBardSkeleton()); break;
                                case 1: PackItem(new UnknownMageSkeleton()); break;
                                case 2: PackItem(new UnknownRogueSkeleton()); break;
                            }
                        }
                    }
                }
                else
                {   // standard runuo loot
                    if (Spawning)
                    {
                        PackItem(new Bone(3));
                        PackItem(new FertileDirt(Utility.RandomMinMax(1, 5)));

                        switch (Utility.Random(4))
                        {
                            case 0: PackItem(new DullCopperOre(Utility.RandomMinMax(1, 10))); break;
                            case 1: PackItem(new ShadowIronOre(Utility.RandomMinMax(1, 10))); break;
                            case 2: PackItem(new CopperOre(Utility.RandomMinMax(1, 10))); break;
                            case 3: PackItem(new BronzeOre(Utility.RandomMinMax(1, 10))); break;
                        }

                        // http://www.google.com/codesearch/p?hl=en#biPgqLK3B_w/trunk/Scripts/Mobiles/Monsters/Ants/AntLion.cs&q=antlion%20package:http://runuomondains%5C.googlecode%5C.com&sa=N&cd=1&ct=rc
                        // (Bones, An Unknown Adventurer's Skeleton)
                        for (int i = 0; i < 3; i++)
                        {
                            switch (Utility.Random(5))
                            {
                                case 0: PackItem(new BoneShards()); break;
                                case 1: PackItem(new SpineBone()); break;
                                case 2: PackItem(new RibCage()); break; ;
                                case 3: PackItem(new PelvisBone()); break;
                                case 4: PackItem(new Skull()); break;
                            }
                        }

                        if (0.07 >= Utility.RandomDouble())
                        {
                            switch (Utility.Random(3))
                            {
                                case 0: PackItem(new UnknownBardSkeleton()); break;
                                case 1: PackItem(new UnknownMageSkeleton()); break;
                                case 2: PackItem(new UnknownRogueSkeleton()); break;
                            }
                        }
                    }

                    AddLoot(LootPack.Average, 2);
                }
            }
        }

        public override void OnThink()
        {
            base.OnThink();

            if (0.05 >= Utility.RandomDouble())
                BeginAcidBreath();
            else
                BeginTunneling();
        }

        public override void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {
            if (m_TunnelTimer != null && m_TunnelTimer.Running)
            {
                Frozen = false;
                m_TunnelTimer.Stop();

                SayTo(from, "* You interrupt the ant lion's digging! *");
            }

            if (0.25 >= Utility.RandomDouble())
                BeginAcidBreath();

            base.OnDamage(amount, from, willKill, source_weapon);
        }

        #region Acid Breath
        private DateTime m_NextAcidBreath;

        public void BeginAcidBreath()
        {
            Mobile m = Combatant;

            if (m == null || m.Deleted || !m.Alive || !Alive || m_NextAcidBreath > DateTime.UtcNow || !CanBeHarmful(m))
                return;

            PlaySound(0x118);
            MovingEffect(m, 0x36D4, 1, 0, false, false, 0x3F, 0);

            TimeSpan delay = TimeSpan.FromSeconds(GetDistanceToSqrt(m) / 5.0);
            Timer.DelayCall(delay, new TimerStateCallback(EndAcidBreath), m);

            m_NextAcidBreath = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        }

        public void EndAcidBreath(object o)
        {
            Mobile m = o as Mobile;
            if (m == null || m.Deleted || !m.Alive || !Alive)
                return;

            if (0.2 >= Utility.RandomDouble())
                m.ApplyPoison(this, Poison.Greater);

            AOS.Damage(m, Utility.RandomMinMax(20, 40), 0, 0, 0, 100, 0, this);
        }
        #endregion

        #region Tunneling
        private DateTime m_NextTunneling;
        private Timer m_TunnelTimer;
        private Point3D m_NewLocation;
        private Map m_NewMap;

        public void BeginTunneling()
        {
            Mobile m = Combatant;

            if (m == null || m.Deleted || !m.Alive || !Alive || m_NextTunneling > DateTime.UtcNow || !CanBeHarmful(m) || !m.InRange(this, 2))
                return;

            Frozen = true;

            Hole hole = new Hole(0xF34);
            hole.MoveToWorld(Location, Map);

            m_NewLocation = Location;
            m_NewMap = Map;

            PlaySound(0x21E);
            Say("* The ant lion begins tunneling into the ground *");

            m_TunnelTimer = Timer.DelayCall(TimeSpan.FromSeconds(3), new TimerStateCallback(EndTunneling), m);
            m_NextTunneling = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(12, 20));
        }

        public void EndTunneling(object o)
        {
            Mobile target = o as Mobile;
            Hole hole = new Hole(0x1363);
            hole.MoveToWorld(Location, Map);

            Internalize();

            m_TunnelTimer = Timer.DelayCall(TimeSpan.FromSeconds(3), new TimerStateCallback(Reappear), target);
        }

        public void Reappear(object o)
        {
            Mobile target = o as Mobile;
            Hits += 50;
            Frozen = false;

            if (!Alive || target == null || target.Deleted || !target.Alive || !target.InRange(m_NewLocation, 10))
            {
                MoveToWorld(m_NewLocation, m_NewMap);
            }
            else
            {
                Point3D location = target.Location;

                if (Spells.SpellHelper.FindValidSpawnLocation(m_NewMap, ref location, true))
                {
                    Hole hole = new Hole(0xF34);
                    hole.MoveToWorld(location, m_NewMap);
                    hole = new Hole(0x1363);
                    hole.MoveToWorld(location, m_NewMap);

                    MoveToWorld(location, m_NewMap);
                }
                else
                    MoveToWorld(m_NewLocation, m_NewMap);
            }

            Combatant = target;
        }

        private class Hole : Static
        {
            public Hole(int itemID)
                : base(itemID)
            {
                Hue = 0x1;
                Name = "a hole";

                Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerCallback(Delete));
            }

            public Hole(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.WriteEncodedInt(0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadEncodedInt();

                Delete();
            }
        }
        #endregion

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            if (m_TunnelTimer != null && m_TunnelTimer.Running)
            {
                writer.Write(true);
                writer.Write(m_NewLocation);
                writer.Write(m_NewMap);
            }
            else
                writer.Write(false);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        if (reader.ReadBool())
                        {
                            m_NewLocation = reader.ReadPoint3D();
                            m_NewMap = reader.ReadMap();
                            Reappear(null);
                        }
                        goto case 0;
                    }
                case 0:
                    {
                        SpeechHue = 0x3B2;
                        break;
                    }
            }
        }
    }
}