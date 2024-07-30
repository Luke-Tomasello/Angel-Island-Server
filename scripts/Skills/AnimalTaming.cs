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

/* Scripts/Skills/AnimalTaming.cs
 * ChangeLog
 *  8/3/2023, Adam
 *      Taming difficulty can be found here
 *      http://web.archive.org/web/20010608143353/http://uo.stratics.com/content/skills/anim.shtml
 *  5/21/2023, Adam, (PetLoyalty.Confused)
 *      On later versions of UO, pets started out as 'wonderfully happy' But in the Siege era
 *      pets start out as 'confused'
 *      https://web.archive.org/web/20010805193803fw_/http://uo.stratics.com/strat/tamer.shtml
 *  11/30/22, Adam
 *      Taming high level monsters (such as nightmares, wyrms, and dragons) will become twice as difficult.
 *      https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
 *      It now takes twice as many attempts to tame these creatures as non-siege.
 *      I.e., you only have a 2.5% chance not to anger the beast as opposed to the usual 5% chance.
 *      Also, Basilisk added to the list of creatures that get angry.
 *  8/28/22, Yoar
 *      Faction war horses can now only be tamed by aligned faction members.
 *	6/7/2021, Adam
 *		Reduce the tame's Lifespan from maybe a day or three to 10-30 minutes.
 *		This change keeps previous tames from clogging up the popular areas.
 *		Note: The DeathStar only cycles once per day at 3am, so the tame will survive at least that long.
 *	4/1/10, adam
 *		Add a check to prevent taming from within a house
 *	10/31/05, erlein
 *		Added flushing of aggressor list on successful taming attempt.
 *	09/14/05 Taran Kain
 *		Add checks to prevent taming provoked creatures.
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Factions;
using Server.Guilds;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.SkillHandlers
{
    public class AnimalTaming
    {
        private static Hashtable m_BeingTamed = new Hashtable();

        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.AnimalTaming].Callback = new SkillUseCallback(OnUse);
        }

        private static bool m_DisableMessage;

        public static bool DisableMessage
        {
            get { return m_DisableMessage; }
            set { m_DisableMessage = value; }
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish >= 13.6)
                m.RevealingAction();

            m.Target = new InternalTarget();

            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish >= 13.6)
                m.RevealingAction();

            if (!m_DisableMessage)
                m.SendLocalizedMessage(502789); // Tame which animal?

            return TimeSpan.FromHours(6.0);
        }

        public static bool CheckMastery(Mobile tamer, BaseCreature creature)
        {
            /*
			BaseCreature familiar = (BaseCreature)Spells.Necromancy.SummonFamiliarSpell.Table[tamer];

			if ( familiar != null && !familiar.Deleted && familiar is DarkWolfFamiliar )
			{
				if ( creature is DireWolf || creature is GreyWolf || creature is TimberWolf || creature is WhiteWolf )
					return true;
			}
			*/
            return false;
        }

        public static bool MustBeSubdued(BaseCreature bc)
        {
            return bc.SubdueBeforeTame && (bc.Hits > (bc.HitsMax / 10));
        }

        public static void Scale(BaseCreature bc, double scalar, bool scaleStats)
        {
            if (scaleStats)
            {
                if (bc.RawStr > 0)
                    bc.RawStr = (int)Math.Max(1, bc.RawStr * scalar);

                if (bc.RawDex > 0)
                    bc.RawDex = (int)Math.Max(1, bc.RawDex * scalar);

                if (bc.HitsMaxSeed > 0)
                {
                    bc.HitsMaxSeed = (int)Math.Max(1, bc.HitsMaxSeed * scalar);
                    bc.Hits = bc.Hits;
                }

                if (bc.StamMaxSeed > 0)
                {
                    bc.StamMaxSeed = (int)Math.Max(1, bc.StamMaxSeed * scalar);
                    bc.Stam = bc.Stam;
                }
            }

            for (int i = 0; i < bc.Skills.Length; ++i)
                bc.Skills[i].Base *= scalar;
        }

        private class InternalTarget : Target
        {
            private bool m_SetSkillTime = true;

            public InternalTarget()
                : base(6, false, TargetFlags.None)
            {
            }

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_SetSkillTime)
                    from.NextSkillTime = Core.TickCount;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish >= 13.6)
                    from.RevealingAction();

                if (targeted is Mobile)
                {
                    if (targeted is BaseCreature)
                    {
                        BaseCreature creature = (BaseCreature)targeted;
                        bool hasMastery;

                        if (!creature.Tamable)
                        {
                            from.SendLocalizedMessage(502469); // That being can not be tamed.
                        }
                        else if (creature.Controlled || creature.GetCreatureBool(CreatureBoolTable.IsTownshipLivestock))
                        {
                            from.SendLocalizedMessage(502467); // That animal looks tame already.
                        }
                        else if (from.Female ? !creature.AllowFemaleTamer : !creature.AllowMaleTamer)
                        {
                            from.SendLocalizedMessage(502801); // You can't tame that!
                        }
                        else if (from.FollowerCount + creature.ControlSlots > from.FollowersMax)
                        {
                            from.SendLocalizedMessage(1049611); // You have too many followers to tame that creature.
                        }
                        else if (creature.Owners.Count >= BaseCreature.MaxOwners && !creature.Owners.Contains(from))
                        {
                            from.SendLocalizedMessage(1005615); // This animal has had too many owners and is too upset for you to tame.
                        }
                        else if (MustBeSubdued(creature))
                        {
                            from.SendLocalizedMessage(1054025); // You must subdue this creature before you can tame it!
                        }
                        else if (creature.BardProvoked)
                        {
                            from.SendMessage("That creature is too angry to tame.");
                        }
                        else if ((hasMastery = CheckMastery(from, creature)) || from.Skills[SkillName.AnimalTaming].Value >= creature.MinTameSkill)
                        {
                            FactionWarHorse warHorse = creature as FactionWarHorse;

                            if (warHorse != null)
                            {
                                Faction faction = Faction.Find(from);

                                if (faction == null || faction != warHorse.Faction)
                                {
                                    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042590, from.NetState); // You cannot tame this creature.
                                    return;
                                }
                            }

                            //if (creature.Owners.Count > 0 && !hasMastery && !creature.Owners.Contains(from) && from.Skills[SkillName.AnimalTaming].Value < GetMinSkill(creature))
                            //{
                            //    creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1005615, from.NetState); // This animal has had too many owners and is too upset for you to tame.
                            //    return;
                            //}

                            // Angel Island adds in Basilisk
                            bool getsAngry = (creature is Dragon || creature is Nightmare || creature is SwampDragon || creature is WhiteWyrm || creature is Basilisk);

                            // Taming high level monsters (such as nightmares, wyrms, and dragons) will become twice as difficult.
                            //  https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                            double anger = Core.RuleSets.SiegeStyleRules() ? 0.95 + 0.025 : 0.95;

                            if (m_BeingTamed.Contains(targeted))
                            {
                                from.SendLocalizedMessage(502802); // Someone else is already taming this.
                            }
                            else if (getsAngry && anger >= Utility.RandomDouble())
                            {
                                from.SendLocalizedMessage(502805); // You seem to anger the beast!
                                creature.PlaySound(creature.GetAngerSound());
                                creature.Direction = creature.GetDirectionTo(from);
                                creature.Combatant = from;
                            }
                            else
                            {
                                m_BeingTamed[targeted] = from;

                                from.LocalOverheadMessage(MessageType.Emote, 0x59, 1010597); // You start to tame the creature.
                                from.NonlocalOverheadMessage(MessageType.Emote, 0x59, 1010598); // *begins taming a creature.*

                                new InternalTimer(from, creature, Utility.Random(3, 2)).Start();

                                m_SetSkillTime = false;
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(502806); // You have no chance of taming this creature.
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(502469); // That being can not be tamed.
                    }
                }
                else
                {
                    from.SendLocalizedMessage(502801); // You can't tame that!
                }
            }

            private class InternalTimer : Timer
            {
                private Mobile m_Tamer;
                private BaseCreature m_Creature;
                private int m_MaxCount;
                private int m_Count;
                private bool m_Paralyzed;

                public InternalTimer(Mobile tamer, BaseCreature creature, int count)
                    : base(TimeSpan.FromSeconds(3.0), TimeSpan.FromSeconds(3.0), count)
                {
                    m_Tamer = tamer;
                    m_Creature = creature;
                    m_MaxCount = count;
                    m_Paralyzed = creature.Paralyzed;
                    Priority = TimerPriority.TwoFiftyMS;
                }
                private void AlignedSpeak(Mobile tamer)
                {
                    if (tamer is null || tamer.Deleted)
                        return;

                    string text = string.Empty;

                    Guild guild = tamer.Guild as Guild;
                    if (guild is not null && guild.Alignment == Engines.Alignment.AlignmentType.Orc)
                    {
                        text = Utility.RandomList<string>(new string[]
                        {
                            "Me nub eet yu, dum grub!",
                            "Nub be skared, or me clomp!",
                            "Lizen tu me, tuupid!",
                            "Git bak heer, kraltch!"
                        }
                        );
                    }
                    else if (guild is not null && guild.Alignment == Engines.Alignment.AlignmentType.Undead)
                    {
                        text = Utility.RandomList<string>(new string[]
                        {
                            "I am your massster!",
                            "Give me your sssoul!",
                            "Bend to death'sss will!",
                            "Your sssoul is mine!"
                        }
                        );
                    }
                    else
                    {
                        //Will you be my friend?
                        //I've always wanted a pet like you.
                        //Don't be afraid.
                        //I won't hurt you.
                        tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.Random(502790, 4));
                        return;
                    }

                    tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, ascii: true, text);
                }
                protected override void OnTick()
                {
                    m_Count++;

                    if (Multis.BaseHouse.FindHouseAt(m_Tamer) != null)
                    {   // cannot be trained in a house
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendMessage("You may not tame that creature from the safety of a house!"); // You may not tame that creature from the safety of a house!
                        Stop();
                    }
                    else if (!m_Tamer.InRange(m_Creature, 6))
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendLocalizedMessage(502795); // You are too far away to continue taming.
                        Stop();
                    }
                    else if (!m_Tamer.CheckAlive())
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendLocalizedMessage(502796); // You are dead, and cannot continue taming.
                        Stop();
                    }
                    else if (!m_Tamer.CanSee(m_Creature) || !m_Tamer.InLOS(m_Creature))
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendLocalizedMessage(502800); // You can't see that.
                        Stop();
                    }
                    else if (!m_Creature.Tamable)
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendLocalizedMessage(502469); // That being can not be tamed.
                        Stop();
                    }
                    else if (m_Creature.Controlled || m_Creature.GetCreatureBool(CreatureBoolTable.IsTownshipLivestock))
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendLocalizedMessage(502804); // That animal looks tame already.
                        Stop();
                    }
                    else if (m_Creature.BardProvoked)
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendMessage("That creature is too angry to tame.");
                        Stop();
                    }
                    else if (m_Creature.Owners.Count >= BaseCreature.MaxOwners && !m_Creature.Owners.Contains(m_Tamer))
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendLocalizedMessage(1005615); // This animal has had too many owners and is too upset for you to tame.
                        Stop();
                    }
                    else if (MustBeSubdued(m_Creature))
                    {
                        m_BeingTamed.Remove(m_Creature);
                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_Tamer.SendLocalizedMessage(1054025); // You must subdue this creature before you can tame it!
                        Stop();
                    }
                    else if (m_Count < m_MaxCount)
                    {
                        m_Tamer.RevealingAction();

                        AlignedSpeak(m_Tamer);

                        if (m_Creature.Paralyzed)
                            m_Paralyzed = true;
                    }
                    else
                    {
                        if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish >= 13.6)
                            m_Tamer.RevealingAction();

                        m_Tamer.NextSkillTime = Core.TickCount;
                        m_BeingTamed.Remove(m_Creature);

                        if (m_Creature.Paralyzed)
                            m_Paralyzed = true;

                        bool alreadyOwned = m_Creature.Owners.Contains(m_Tamer);

                        if (!alreadyOwned) // Passively check animal lore for gain
                            m_Tamer.CheckTargetSkill(SkillName.AnimalLore, m_Creature, 0.0, 120.0, new object[2] { m_Creature, null }/*contextObj*/);

                        double minSkill = m_Creature.MinTameSkill + (m_Creature.Owners.Count * 6.0);

                        if (minSkill > -24.9 && CheckMastery(m_Tamer, m_Creature))
                            minSkill = -24.9; // 50% at 0.0?

                        minSkill += 24.9;

                        if (alreadyOwned || m_Tamer.CheckTargetSkill(SkillName.AnimalTaming, m_Creature, minSkill - 25.0, minSkill + 25.0, new object[2] { m_Creature, null } /*contextObj*/))
                        {
                            if (m_Creature.Owners.Count == 0) // First tame
                            {
                                if (m_Paralyzed)
                                    Scale(m_Creature, 0.86, false); // 86% of original skills if they were paralyzed during the taming
                                else
                                    Scale(m_Creature, 0.90, false); // 90% of original skills

                                if (m_Creature.SubdueBeforeTame)
                                    Scale(m_Creature, 0.50, true); // Creatures which must be subdued take an additional 50% loss of skills and stats
                            }

                            if (alreadyOwned)
                            {
                                m_Tamer.SendLocalizedMessage(502797); // That wasn't even challenging.
                            }
                            else
                            {
                                m_Creature.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502799, m_Tamer.NetState); // It seems to accept you as master.
                                m_Creature.Owners.Add(m_Tamer);

                                // erl: flush aggressor list

                                List<AggressorInfo> aggressors = m_Creature.Aggressors;

                                if (aggressors.Count > 0)
                                {
                                    for (int i = 0; i < aggressors.Count; ++i)
                                    {
                                        AggressorInfo info = (AggressorInfo)aggressors[i];
                                        Mobile attacker = info.Attacker;

                                        if (attacker != null && !attacker.Deleted)
                                            m_Creature.RemoveAggressor(attacker);
                                    }
                                }

                                // ..

                            }

                            // Once tamed, we want the creature to die off as soon as the next mob cleanup (MobileLifespanCleanup)
                            //	not to worry, mobiles aren't cleaned up while controlled.
                            m_Creature.Lifespan = TimeSpan.FromMinutes(Utility.RandomMinMax(10, 30));
                            m_Creature.PreferredFocus = null;
                            m_Creature.SetControlMaster(m_Tamer);
                            m_Creature.IsBonded = false;
                            /* Freshly tamed pets are in a state of "confusion". If you try to command them before you feed them, they will most likely go wild with the first command (depending on their taming difficulty and your skill level). Use Animal Lore on your pet to find out what it likes to eat, if you don't know already. One piece of food will be sufficient in most cases to make your pet "wonderfully happy".
                             * If your mount stops and refuses to carry you any further because it is too fatigued to move, one piece of food will replenish 30% of it's stamina, with a maximum of 90%. Just make sure you feed three pieces, one at a time.
                             * https://web.archive.org/web/20010805193803fw_/http://uo.stratics.com/strat/tamer.shtml
                             */
                            if (Core.RuleSets.SiegeStyleRules())
                            {
                                m_Creature.LoyaltyValue = PetLoyalty.Confused;

                                m_Creature.SetCreatureBool(CreatureBoolTable.FreshTame, true);
                            }
                        }
                        else
                        {
                            m_Tamer.SendLocalizedMessage(502798); // You fail to tame the creature.
                        }
                    }
                }
            }
        }
    }
}