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

/* Scripts\Mobiles\Special\Christmas Elves\BaseChristmasElf.cs
 * ChangeLog
 * 12/12/23, Yoar
 *      Creating a base class for christmas elves.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public abstract class BaseChristmasElf : BaseCreature, ISnowballFight
    {
        protected virtual bool ThrowsSnowballs { get { return true; } }
        protected virtual bool HasSnowballFights { get { return false; } }
        protected virtual bool IsEasilyScared { get { return false; } }

        public BaseChristmasElf(AIType ai, FightMode mode, int iRangePerception, int iRangeFight, double dActiveSpeed, double dPassiveSpeed)
            : base(ai, mode, iRangePerception, iRangeFight, dActiveSpeed, dPassiveSpeed)
        {
            ElfHelper.InitBody(this);
            ElfHelper.InitOutfit(this);

            PackItem(ElfHelper.SetMovable(false, new SnowPile()));

            if (Core.RuleSets.AngelIslandRules())
                VirtualArmor = 21;
            else
                VirtualArmor = 16;

            if (Core.RuleSets.AngelIslandRules())
                CanRun = true;
        }

        public override void OnThink()
        {
            base.OnThink();

            if (ThrowsSnowballs && CheckThrowSnowball())
            {
                NextCombatTime = DateTime.UtcNow + TimeSpan.FromSeconds(5.0);

                if (HasSnowballFights)
                {
                    Taunt(0.50,
                        "Hah! Got ya!",
                        "Haha! Take that!",
                        "Na na na na na na!",
                        "Boom!",
                        "Right in the kisser!");
                }
            }

            if (HasSnowballFights)
                CheckSnowballFight();

            if (IsEasilyScared && AIObject != null && AIObject.Action == ActionType.Flee)
            {
                Taunt(0.15,
                    "AAAHH!",
                    "Run away!!",
                    "But... It's Christmas!",
                    "No! Please no! Please! it's christmas!",
                    "You're a mean one, Mr. Grinch!");
            }
        }

        public override void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {
            base.OnDamage(amount, from, willKill, source_weapon);

            if (IsEasilyScared)
                CheckRout(from);
        }

        #region Snowball Throwing

        protected bool CheckThrowSnowball()
        {
            Item snowPile = FindSnowPile();

            if (snowPile == null)
                return false;

            if (!CanThrowSnowball())
                return false;

            Mobile target = Combatant;

            if (!IsValidSnowballTarget(target))
                return false;

            if (Utility.RandomDouble() < 0.10)
            {
                Use(snowPile);

                if (Target != null)
                    Target.Invoke(this, target);

                return true;
            }

            return false;
        }

        private Item FindSnowPile()
        {
            if (Backpack == null)
                return null;

            return Backpack.FindItemByType(typeof(SnowPile));
        }

        private bool CanThrowSnowball()
        {
            return (!IsDeadBondedPet && !BardPacified && CanBeginAction(typeof(SnowPile)));
        }

        private bool IsValidSnowballTarget(Mobile target)
        {
            return (target != null && target.Alive && !target.IsDeadBondedPet && Map == target.Map && InRange(target, 10) && InLOS(target) && CanBeHarmful(target));
        }

        #endregion

        #region Snowball Fights

        private SnowballFight m_SnowballFight;
        private DateTime m_NextSnowballFight;

        protected void CheckSnowballFight()
        {
            if (DateTime.UtcNow >= m_NextSnowballFight)
            {
                if (RangeHome != 0 && Home != Point3D.Zero && !InRange(Home, RangeHome))
                {
                    m_NextSnowballFight = DateTime.UtcNow + TimeSpan.FromSeconds(45.0);

                    SnowballFight.Quit(this);
                }
                else
                {
                    m_NextSnowballFight = DateTime.UtcNow + TimeSpan.FromSeconds(10.0);

                    SnowballFight.Join(this);
                }
            }
        }

        public override bool IsFriend(Mobile m, RelationshipFilter filter)
        {
            if (HasSnowballFights && SnowballFight.IsFriend(this, m))
                return true;

            return base.IsFriend(m, filter);
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            if (HasSnowballFights && SnowballFight.IsEnemy(this, m))
                return true;

            return base.IsEnemy(m, filter);
        }

        public override int NotorietyOverride(Mobile target)
        {
            if (HasSnowballFights && SnowballFight.IsEnemy(this, target))
                return Notoriety.CanBeAttacked;

            return base.NotorietyOverride(target);
        }

        SnowballFight ISnowballFight.SnowballFight { get { return m_SnowballFight; } set { m_SnowballFight = value; } }

        #endregion

        #region Rout

        private DateTime m_NextRout;

        protected void CheckRout(Mobile target)
        {
            if (DateTime.UtcNow >= m_NextRout)
            {
                m_NextRout = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);

                foreach (Mobile m in GetMobilesInRange(RangePerception))
                {
                    BaseChristmasElf elf = m as BaseChristmasElf;

                    if (elf != null && elf.IsEasilyScared && GetDistanceToSqrt(m) <= RangePerception && elf.AIObject != null && elf.AIObject.Action != ActionType.Flee)
                    {
                        elf.AIObject.Action = ActionType.Flee;
                        elf.FocusMob = target;
                    }
                }
            }
        }

        #endregion

        #region Taunts

        public static TimeSpan TauntDelay = TimeSpan.FromSeconds(6.0);

        private DateTime m_NextTaunt;

        protected void Taunt(double chance, params string[] taunts)
        {
            if (taunts.Length == 0)
                return;

            if (DateTime.UtcNow >= m_NextTaunt && Utility.RandomDouble() < chance)
            {
                m_NextTaunt = DateTime.UtcNow + TauntDelay;

                Say(taunts[Utility.Random(taunts.Length)]);
            }
        }

        #endregion

        public BaseChristmasElf(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x80); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version;

            if ((Utility.PeekByte(reader) & 0x80) == 0)
            {
                version = 0; // old version
            }
            else
            {
                version = reader.ReadByte();

                switch (version)
                {
                    case 0x80:
                        {
                            break;
                        }
                }
            }
        }
    }
}