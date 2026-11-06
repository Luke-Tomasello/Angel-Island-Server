/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\ChampionSpawn\Champs\Special\PumpkinKing\ThePumpkinKing.cs
 * CHANGELOG
 *  10/28/2026, Adam
 *      Initial version: Halloween champion. Neira's stats, skills, combat behavior and special-attack triggers on a
 *      BoneDemon body. Each trigger throws a skeleton bone (60%) or an exploding pumpkin (40%).
 *      An untargeted attack is lobbed 3-4 tiles away (in sight, nearest a player). Bones rise after 2-3 seconds.
 *      Outstanding bones are tracked and deleted with the King. On death, packs a random weapon imbued to level 5-6,
 *      of a random metal (bows and staves keep theirs), crafted by The Pumpkin King. No champion skull (SkullType None).
 *      Always drops a blessed "Halloween 2026 cake" (RewardCake hues) into his corpse; MakeHalloweenCake() is shared with
 *      the Doppelgangers of his champ. Also drops "The Harvest of Faces" (HarvestOfFacesBook), his story.
 */

using Server.Engines.ChampionSpawn;
using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("the remains of the Pumpkin King")]
    public class ThePumpkinKing : BaseChampion
    {
        public override ChampionSkullType SkullType { get { return ChampionSkullType.None; } }   // no champion skull

        // bones thrown and not yet risen (runtime only)
        private List<PumpkinKingBone> m_Bones = new List<PumpkinKingBone>();

        [Constructable]
        public ThePumpkinKing()
            : base(AIType.AI_Mage, 0.175, 0.350)
        {
            Name = "The Pumpkin King";
            Body = 308;             // bone demon
            BaseSoundID = 0x48D;    // bone demon
            Hue = 0x46F;            // plant 'Orange'
            BardImmune = true;

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4800);
            SetStam(102, 300);

            SetDamage(25, 35);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 150.0);
            SetSkill(SkillName.Swords, 97.6, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 30;
        }

        public override bool OnBeforeDeath()
        {
            // a random weapon, imbued (level 5-6), random resource, crafted by The Pumpkin King
            BaseWeapon weapon = Loot.RandomWeapon();
            Loot.ImbueWeaponOrArmor(noThrottle: true, weapon, Utility.RandomBool() ? Loot.ImbueLevel.Level5 : Loot.ImbueLevel.Level6, 0, false);
            // metal only: as in Loot's hueing rules, bows and staves keep their resource
            if (!(weapon is BaseRanged) && !(weapon is BaseStaff))
                weapon.Resource = (CraftResource)Utility.RandomMinMax((int)CraftResource.DullCopper, (int)CraftResource.Valorite);
            weapon.Crafter = this;      // captures his name
            PackItem(weapon);

            return base.OnBeforeDeath();
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            // always: a Halloween cake (into the corpse: a blessed item in our pack would stay with us)
            c.DropItem(MakeHalloweenCake());

            // and his story
            c.DropItem(new HarvestOfFacesBook());
        }

        // a blessed reward cake (16 random hues), dropped by The Pumpkin King and the Doppelgangers of his champ
        public static Item MakeHalloweenCake()
        {
            RewardCake cake = new RewardCake();
            cake.Name = "a Halloween 2026 cake";
            cake.LootType = LootType.Blessed;
            return cake;
        }

        public override void OnDelete()
        {
            foreach (PumpkinKingBone bone in m_Bones)
                if (!bone.Deleted)
                    bone.Delete();
            m_Bones.Clear();

            base.OnDelete();
        }

        public override bool AlwaysMurderer { get { return true; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(this); } }

        public override bool ShowFameTitle { get { return false; } }
        public override bool ClickTitle { get { return false; } }

        #region Special Attacks (Neira's triggers)
        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (0.1 >= Utility.RandomDouble()) // 10% chance
                SpecialAttack(defender, 0.25);
        }

        public override void Damage(int amount, Mobile from, DamageContext context)
        {
            base.Damage(amount, from, context);

            if (from != null && from is PlayerMobile && from.InRange(this, 10) || from != null && from is BaseCreature && ((BaseCreature)from).Controlled)
            {
                if (0.1 >= Utility.RandomDouble()) // 10% chance
                    SpecialAttack(from, 0.25);
            }
        }

        public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
            base.AlterDamageScalarFrom(caster, ref scalar);

            if (0.1 >= Utility.RandomDouble()) // 10% chance
                SpecialAttack(caster, 1.0);
        }

        // one attack per trigger: 60% skeleton bone, 40% exploding pumpkin
        //  as with Neira, the attack is thrown at the target (chanceToThrow); otherwise it is lobbed to a spot 3-4 tiles
        //  away that we can see, as near a player as we can find
        public void SpecialAttack(Mobile target, double chanceToThrow)
        {
            if (Deleted || Map == null || Map == Map.Internal)
                return;

            bool bone = 0.6 > Utility.RandomDouble();

            if (chanceToThrow >= Utility.RandomDouble())
                Throw(bone, target, target.Location);
            else if (FindLobSpot() is Point3D spot)
                Throw(bone, null, spot);
        }

        private void Throw(bool bone, Mobile? victim, Point3D loc)
        {
            Map map = Map;
            IEntity to = victim != null ? victim : new Entity(Serial.Zero, loc, map);

            Direction = GetDirectionTo(loc);
            if (bone)
                Effects.SendMovingEffect(this, to, 0xF7E, 10, 1, true, false, 0x496, 0);
            else
                Effects.SendMovingEffect(this, to, ExplodingPumpkin.PumpkinID, 10, 1, true, false, ExplodingPumpkin.PumpkinHue, 0);

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), () => OnLanded(bone, victim, loc, map));
        }

        private void OnLanded(bool bone, Mobile? victim, Point3D loc, Map map)
        {
            if (Deleted)
                return;

            if (victim != null)
            {   // thrown at a target: lands where they are now (Neira)
                if (victim.Deleted || victim.Map == null || victim.Map == Map.Internal)
                    return;
                loc = victim.Location;
                map = victim.Map;
            }

            if (bone)
            {
                if (victim != null)
                {
                    if (!CanBeHarmful(victim))
                        return;
                    DoHarmful(victim);
                    AOS.Damage(victim, this, Utility.RandomMinMax(10, 20), new DamageContext { Source = this });
                }
                DropBone(loc, map);
            }
            else
                new ExplodingPumpkin(this).Land(loc, map);
        }

        // a spot 3-4 tiles away, spawnable and in our line of sight, nearest a player (any such spot if no player is near)
        private Point3D? FindLobSpot()
        {
            List<Mobile> players = new List<Mobile>();
            IPooledEnumerable eable = Map.GetMobilesInRange(Location, 12);
            foreach (Mobile m in eable)
                if (m is PlayerMobile && m.Alive && !m.Hidden && m.AccessLevel == AccessLevel.Player)
                    players.Add(m);
            eable.Free();

            Point3D? best = null;
            double bestDistance = double.MaxValue;
            for (int i = 0; i < 20; i++)
            {
                int dx = Utility.RandomMinMax(-4, 4), dy = Utility.RandomMinMax(-4, 4);
                int range = Math.Max(Math.Abs(dx), Math.Abs(dy));
                if (range < 3)
                    continue;

                int x = X + dx, y = Y + dy, z = Map.GetAverageZ(x, y);
                if (!Map.CanSpawnLandMobile(x, y, z))
                    continue;

                Point3D p = new Point3D(x, y, z);
                if (!InLOS(p))
                    continue;

                double distance = 0;
                if (players.Count > 0)
                {
                    distance = double.MaxValue;
                    foreach (Mobile m in players)
                        distance = Math.Min(distance, m.GetDistanceToSqrt(p));
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = p;
                }
            }

            return best;
        }

        private void DropBone(Point3D loc, Map map)
        {
            m_Bones.RemoveAll(b => b.Deleted);
            PumpkinKingBone bone = new PumpkinKingBone();
            m_Bones.Add(bone);
            bone.MoveToWorld(loc, map);
        }
        #endregion Special Attacks

        public ThePumpkinKing(Serial serial)
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

    // Neira's unholy bone, but immovable, rises as a skeleton after 2-3 seconds, and does not survive a restart
    public class PumpkinKingBone : Item
    {
        public PumpkinKingBone()
            : base(0xF7E)
        {
            Movable = false;
            Hue = 0x497;
            Name = "unholy bone";

            Timer.DelayCall(TimeSpan.FromSeconds(2.0 + Utility.RandomDouble()), Rise);
        }

        private void Rise()
        {
            if (Deleted || Map == null || Map == Map.Internal)
                return;

            new Skeleton().MoveToWorld(Location, Map);
            Delete();
        }

        public PumpkinKingBone(Serial serial)
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

            Timer.DelayCall(TimeSpan.Zero, Delete);
        }
    }
}
