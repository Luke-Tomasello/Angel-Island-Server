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

/* Scripts/Misc/Notoriety.cs
 * ChangeLog
 *  4/14/23, Yoar
 *      Added support for new Alignment system.
 * 12/18/22, Adam (MobileNotoriety)
 *      Remove the special dispensation for staff .. if you're not blessed, you can be attacked.
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *  12/5/21, Yoar
 *      Added virtual BaseCreature.NotorietyOverride: Overrides normal notoriety calculations.
 *	12/4/07, Adam
 *		Add support for peaceful guilds (no notoriety)
 *  12/4/07, Pix
 *      now kin-healers flag canbeattacked to kin instead of enemy
 *	12/3/07, Pix
 *		Now if we're a kin-healer, allnames shows our name in purple and we hue
 *		enemy to ourself.
 *  12/3/07, Pix
 *      Added IOBAlignement.Healer for kin-healers instead of overloading Outcast
 *	8/26/07 - Pix
 *		Dueling system notoriety handlers (beneficial and harmful) in place.
 *	08/01/07, Pix
 *		Implemented NotorietyBeneficialActsHandler.
 *	6/18/06, Pix
 *		Special case for new Outcast IOBAlignment
 *	6/15/06, Pix
 *		Commented out prior change.
 *	6/11/06, Pix
 *		Mobs in CustomRegions which are NoCountZones now are attackable.
 *	06/09/06, Pix
 *		Same-aligned NPCs hue ally/green.
 *	06/06/06, Pix
 *		Changes for Kin System
 *	8/02/05, Pix
 *		Added check in addition to InitialInnocent check to see whether the basecreature is controled
 *	4/30/05, Kit
 *		Added in support for flagging for iob regions defined by custom regions(DRDT)
 *	3/31/05, Pix
 *		Change IOBStronghold to IOBRegion
 *	3/31/05, Pix
 *		Added IOB Flagging orange/green depending on location and alignment and core setting
 *	12/20/04, Pix
 *		Changed so IOBFollowers are not innocent (so they can be attacked).
 *	11/16/04, Pix
 *		Fixed self-notoriety if unguilded and registered with fightbroker.
 *		Fixed pet notoriety if registered with fightbroker.
 *	10/24/04, Pix
 *		Made it so if two people are both registered with the fight broker, they appear as enemies to
 *		each other.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.PartySystem;
using Server.Factions;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Spells;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Misc
{
    public class NotorietyHandlers
    {
        public static void Initialize()
        {
            Notoriety.Hues[Notoriety.Innocent] = 0x59;
            Notoriety.Hues[Notoriety.Ally] = 0x3F;
            Notoriety.Hues[Notoriety.CanBeAttacked] = 0x3B2;
            Notoriety.Hues[Notoriety.Criminal] = 0x3B2;
            Notoriety.Hues[Notoriety.Enemy] = 0x90;
            Notoriety.Hues[Notoriety.Murderer] = 0x22;
            Notoriety.Hues[Notoriety.Invulnerable] = 0x35;

            Notoriety.Handler = new NotorietyHandler(MobileNotoriety);
            Notoriety.BeneficialActsHandler = new NotorietyBeneficialActsHandler(MobileBeneficialActs);

            Mobile.AllowBeneficialHandler = new AllowBeneficialHandler(Mobile_AllowBeneficial);
            Mobile.AllowHarmfulHandler = new AllowHarmfulHandler(Mobile_AllowHarmful);
        }

        private enum GuildStatus { None, Peaceful, Warring }

        private static GuildStatus GetGuildStatus(Mobile m)
        {
            Guild g = m.Guild as Guild;

            if (g == null)
                return GuildStatus.None;

            if (g.Enemies.Count > 0)
                return GuildStatus.Warring;

            if (Guild.OrderChaosEnabled && g.Type != GuildType.Regular)
                return GuildStatus.Warring;

            if (AlignmentSystem.Enabled && g.Alignment != AlignmentType.None)
                return GuildStatus.Warring;

            return GuildStatus.Warring;
        }

        private static bool CheckBeneficialStatus(GuildStatus from, GuildStatus target)
        {
            if (from == GuildStatus.Warring || target == GuildStatus.Warring)
                return false;

            return true;
        }

        /*private static bool CheckHarmfulStatus( GuildStatus from, GuildStatus target )
		{
			if ( from == GuildStatus.Waring && target == GuildStatus.Waring )
				return true;

			return false;
		}*/

        public static bool Mobile_AllowBeneficial(Mobile from, Mobile target)
        {
            // 1/24/2023, Adam: Remove accesslevel checks as it complicates testing
            if (from == null || target == null /*|| from.AccessLevel > AccessLevel.Player || target.AccessLevel > AccessLevel.Player*/)
                return true;

            #region Dueling
            PlayerMobile pmFrom = from as PlayerMobile;
            PlayerMobile pmTarg = target as PlayerMobile;

            if (pmFrom == null && from is BaseCreature)
            {
                BaseCreature bcFrom = (BaseCreature)from;

                if (bcFrom.Summoned)
                    pmFrom = bcFrom.SummonMaster as PlayerMobile;
            }

            if (pmTarg == null && target is BaseCreature)
            {
                BaseCreature bcTarg = (BaseCreature)target;

                if (bcTarg.Summoned)
                    pmTarg = bcTarg.SummonMaster as PlayerMobile;
            }

            if (pmFrom != null && pmTarg != null)
            {
                if (pmFrom.DuelContext != pmTarg.DuelContext && ((pmFrom.DuelContext != null && pmFrom.DuelContext.Started) || (pmTarg.DuelContext != null && pmTarg.DuelContext.Started)))
                    return false;

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext && ((pmFrom.DuelContext.StartedReadyCountdown && !pmFrom.DuelContext.Started) || pmFrom.DuelContext.Tied || pmFrom.DuelPlayer.Eliminated || pmTarg.DuelPlayer.Eliminated))
                    return false;

                if (pmFrom.DuelPlayer != null && !pmFrom.DuelPlayer.Eliminated && pmFrom.DuelContext != null && pmFrom.DuelContext.IsSuddenDeath)
                    return false;

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext && pmFrom.DuelContext.m_Tournament != null && pmFrom.DuelContext.m_Tournament.IsNotoRestricted && pmFrom.DuelPlayer != null && pmTarg.DuelPlayer != null && pmFrom.DuelPlayer.Participant != pmTarg.DuelPlayer.Participant)
                    return false;

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext && pmFrom.DuelContext.Started)
                    return true;
            }

            if ((pmFrom != null && pmFrom.DuelContext != null && pmFrom.DuelContext.Started) || (pmTarg != null && pmTarg.DuelContext != null && pmTarg.DuelContext.Started))
                return false;

            Engines.ConPVP.SafeZone sz = from.Region.GetRegion(typeof(Engines.ConPVP.SafeZone)) as Engines.ConPVP.SafeZone;

            if (sz != null /*&& sz.IsDisabled()*/ )
                return false;

            sz = target.Region.GetRegion(typeof(Engines.ConPVP.SafeZone)) as Engines.ConPVP.SafeZone;

            if (sz != null /*&& sz.IsDisabled()*/ )
                return false;
            #endregion

            #region Factions
            Faction targetFaction = Faction.Find(target, true);

            if (targetFaction != null)
            {
                if (Faction.Find(from, true) != targetFaction)
                    return false;
            }
            #endregion

            #region Alignment
            if (AlignmentSystem.Enabled && !AlignmentSystem.AllowBeneficialAction(from, target))
                return false;
            #endregion

            Map map = from.Map;
            /* pla: comment duel stuff out
			if (target is PlayerMobile && from is PlayerMobile)
			{
				bool tc = ((PlayerMobile)target).IsInChallenge;
				bool sc = (((PlayerMobile)from).IsInChallenge);
				if (tc && sc) //both in challenge
				{
					foreach (Item c in Challenge.Challenge.WorldStones)
					{
						ChallengeStone cs = c as ChallengeStone;
						if (cs.OpponentTeam.Contains(target) || cs.ChallengeTeam.Contains(target))
						{
							if (cs.OpponentTeam.Contains(from) || cs.ChallengeTeam.Contains(from))
							{
								return true;
							}
						}
					}

					//if we're here, then we have two people in challenges, but different ones - don't allow
					return false;
				}
				else if (tc && !sc) //target in challenge but source not
				{
					return false;
				}
				else if (sc && !tc) //source in challenge but target not
				{
					return false;
				}
			}
			*/
            if (map != null && (map.Rules & MapRules.BeneficialRestrictions) == 0)
                return true; // In felucca, anything goes

            if (!from.Player)
                return true; // NPCs have no restrictions

            if (target is BaseCreature && !((BaseCreature)target).Controlled)
                return false; // Players cannot heal uncontroled mobiles

            Guild fromGuild = from.Guild as Guild;
            Guild targetGuild = target.Guild as Guild;

            if (fromGuild != null && targetGuild != null && (targetGuild == fromGuild || fromGuild.IsAlly(targetGuild)))
                return true; // Guild members can be beneficial

            return CheckBeneficialStatus(GetGuildStatus(from), GetGuildStatus(target));
        }

        public static bool Mobile_AllowHarmful(Mobile from, Mobile target)
        {
            if (from == null || target == null)
                return true;

            #region Dueling
            PlayerMobile pmFrom = from as PlayerMobile;
            PlayerMobile pmTarg = target as PlayerMobile;

            if (pmFrom == null && from is BaseCreature)
            {
                BaseCreature bcFrom = (BaseCreature)from;

                if (bcFrom.Summoned)
                    pmFrom = bcFrom.SummonMaster as PlayerMobile;
            }

            if (pmTarg == null && target is BaseCreature)
            {
                BaseCreature bcTarg = (BaseCreature)target;

                if (bcTarg.Summoned)
                    pmTarg = bcTarg.SummonMaster as PlayerMobile;
            }

            if (pmFrom != null && pmTarg != null)
            {
                if (pmFrom.DuelContext != pmTarg.DuelContext && ((pmFrom.DuelContext != null && pmFrom.DuelContext.Started) || (pmTarg.DuelContext != null && pmTarg.DuelContext.Started)))
                    return false;

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext && ((pmFrom.DuelContext.StartedReadyCountdown && !pmFrom.DuelContext.Started) || pmFrom.DuelContext.Tied || pmFrom.DuelPlayer.Eliminated || pmTarg.DuelPlayer.Eliminated))
                    return false;

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext && pmFrom.DuelContext.m_Tournament != null && pmFrom.DuelContext.m_Tournament.IsNotoRestricted && pmFrom.DuelPlayer != null && pmTarg.DuelPlayer != null && pmFrom.DuelPlayer.Participant == pmTarg.DuelPlayer.Participant)
                    return false;

                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext && pmFrom.DuelContext.Started)
                    return true;
            }

            if ((pmFrom != null && pmFrom.DuelContext != null && pmFrom.DuelContext.Started) || (pmTarg != null && pmTarg.DuelContext != null && pmTarg.DuelContext.Started))
                return false;

            Engines.ConPVP.SafeZone sz = from.Region.GetRegion(typeof(Engines.ConPVP.SafeZone)) as Engines.ConPVP.SafeZone;

            if (sz != null /*&& sz.IsDisabled()*/ )
                return false;

            sz = target.Region.GetRegion(typeof(Engines.ConPVP.SafeZone)) as Engines.ConPVP.SafeZone;

            if (sz != null /*&& sz.IsDisabled()*/ )
                return false;
            #endregion

            Map map = from.Map;

            /*
			if (target is PlayerMobile && from is PlayerMobile)
			{
				bool tc = ((PlayerMobile)target).IsInChallenge;
				bool sc = (((PlayerMobile)from).IsInChallenge);
				if (tc && sc) //both in challenge
				{
					foreach (Item c in Challenge.Challenge.WorldStones)
					{
						ChallengeStone cs = c as ChallengeStone;
						if (cs.OpponentTeam.Contains(target) || cs.ChallengeTeam.Contains(target))
						{
							if (cs.OpponentTeam.Contains(from) || cs.ChallengeTeam.Contains(from))
							{
								return true;
							}
						}
					}

					//if we're here, then we have two people in challenges, but different ones - don't allow
					return false;
				}
				else if (tc && !sc) //target in challenge but source not
				{
					return false;
				}
				else if (sc && !tc) //source in challenge but target not
				{
					return false;
				}
			}
			*/

            if (map != null && (map.Rules & MapRules.HarmfulRestrictions) == 0)
                return true; // In felucca, anything goes

            if (!from.Player && !(from is BaseCreature && (((BaseCreature)from).Controlled || ((BaseCreature)from).Summoned)))
                return true; // Uncontroled NPCs have no restrictions

            Guild fromGuild = GetGuildFor(from.Guild as Guild, from);
            Guild targetGuild = GetGuildFor(target.Guild as Guild, target);

            if (fromGuild != null && targetGuild != null && (fromGuild.Peaceful == false && targetGuild.Peaceful == false) && (fromGuild == targetGuild || fromGuild.IsAlly(targetGuild) || fromGuild.IsEnemy(targetGuild)))
                return true; // Guild allies or enemies can be harmful

            if (target is BaseCreature && (((BaseCreature)target).Controlled || (((BaseCreature)target).Summoned && from != ((BaseCreature)target).SummonMaster)))
                return false; // Cannot harm other controled mobiles

            if (target.Player)
                return false; // Cannot harm other players

            if (!(target is BaseCreature && ((BaseCreature)target).InitialInnocent && ((BaseCreature)target).Controlled == false))
            {
                if (Notoriety.Compute(from, target) == Notoriety.Innocent)
                    return false; // Cannot harm innocent mobiles
            }

            return true;
        }

        public static Guild GetGuildFor(Guild def, Mobile m)
        {
            Guild g = def;

            BaseCreature c = m as BaseCreature;

            if (c != null && c.Controlled && c.ControlMaster != null)
            {
                c.DisplayGuildTitle = false;

                if (c.Map != Map.Internal && (c.ControlOrder == OrderType.Attack || c.ControlOrder == OrderType.Guard))
                    g = (Guild)(c.Guild = c.ControlMaster.Guild);
                else if (c.Map == Map.Internal || c.ControlMaster.Guild == null)
                    g = (Guild)(c.Guild = null);
            }

            return g;
        }

        public static int CorpseNotoriety(Mobile source, Corpse target)
        {
            if (target.AccessLevel > AccessLevel.Player)
                return Notoriety.CanBeAttacked;

            Body body = (Body)target.Amount;

            BaseCreature cretOwner = target.Owner as BaseCreature;

            if (cretOwner != null)
            {
                Guild sourceGuild = GetGuildFor(source.Guild as Guild, source);
                Guild targetGuild = GetGuildFor(target.Guild as Guild, target.Owner);

                if (sourceGuild != null && targetGuild != null)
                {
                    if (sourceGuild == targetGuild || sourceGuild.IsAlly(targetGuild))
                        return Notoriety.Ally;
                    else if (sourceGuild.IsEnemy(targetGuild))
                        return Notoriety.Enemy;
                }

                #region Factions
                Faction srcFaction = Faction.Find(source, true, true);
                Faction trgFaction = Faction.Find(target.Owner, true, true);

                if (srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet)
                    return Notoriety.Enemy;
                #endregion

                #region Ethics
                if (Core.OldEthics)
                {
                    Ethics.Ethic srcEthic = Ethics.Ethic.Find(source, true);
                    Ethics.Ethic trgEthic = Ethics.Ethic.Find(target.Owner, true);

                    // Hero/Evil always at war
                    if (srcEthic != null && trgEthic != null && srcEthic != trgEthic)
                        return Notoriety.Enemy;
                }
                #endregion

                #region Alignment
                if (AlignmentSystem.Enabled)
                {
                    AlignmentSystem.Allegiance allegiance = AlignmentSystem.ComputeAllegiance(source, target.Owner, true, true);

                    if (allegiance == AlignmentSystem.Allegiance.Traitor)
                        return Notoriety.CanBeAttacked;

                    if (allegiance == AlignmentSystem.Allegiance.Ally)
                        return Notoriety.Ally;

                    if (allegiance == AlignmentSystem.Allegiance.Enemy)
                        return Notoriety.Enemy;

                    if (AlignmentConfig.StrongholdNotoriety)
                    {
                        AlignmentType srcAlignment = AlignmentSystem.Find(source, true, true);
                        AlignmentType trgAlignment = AlignmentSystem.Find(target.Owner, true, true);

                        if (srcAlignment != AlignmentType.None && trgAlignment == AlignmentType.None && AlignmentSystem.IsInStronghold(source, srcAlignment) && AlignmentSystem.IsInStronghold(target.Owner, srcAlignment))
                            return Notoriety.Enemy;
                    }
                }
                #endregion

                if (CheckHouseFlag(source, target.Owner, target.Location, target.Map))
                    return Notoriety.CanBeAttacked;

                int actual = Notoriety.CanBeAttacked;

                if (target.Kills >= 5 || (body.IsMonster && IsSummoned(target.Owner as BaseCreature)) || (target.Owner is BaseCreature && (((BaseCreature)target.Owner).AlwaysMurderer || ((BaseCreature)target.Owner).IsAnimatedDead)))
                    actual = Notoriety.Murderer;

                if (DateTime.UtcNow >= (target.TimeOfDeath + Corpse.MonsterLootRightSacrifice))
                    return actual;

                Party sourceParty = Party.Get(source);

                ArrayList list = target.Aggressors;

                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i] == source || (sourceParty != null && Party.Get((Mobile)list[i]) == sourceParty))
                        return actual;
                }

                return Notoriety.Innocent;
            }
            else
            {
                if (target.Kills >= 5 || (body.IsMonster && IsSummoned(target.Owner as BaseCreature)) || (target.Owner is BaseCreature && (((BaseCreature)target.Owner).AlwaysMurderer || ((BaseCreature)target.Owner).IsAnimatedDead)))
                    return Notoriety.Murderer;

                if (target.Criminal)
                    return Notoriety.Criminal;

                Guild sourceGuild = GetGuildFor(source.Guild as Guild, source);
                Guild targetGuild = GetGuildFor(target.Guild as Guild, target.Owner);

                if (sourceGuild != null && targetGuild != null)
                {
                    if (sourceGuild == targetGuild || sourceGuild.IsAlly(targetGuild))
                        return Notoriety.Ally;
                    else if (sourceGuild.IsEnemy(targetGuild))
                        return Notoriety.Enemy;
                }

                #region Factions
                Faction srcFaction = Faction.Find(source, true, true);
                Faction trgFaction = Faction.Find(target.Owner, true, true);

                if (srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet)
                {
                    ArrayList secondList = target.Aggressors;

                    for (int i = 0; i < secondList.Count; ++i)
                    {
                        Mobile mob = secondList[i] as Mobile;

                        if (mob == null)
                            continue;

                        if (mob == source || mob is BaseFactionGuard)
                            return Notoriety.Enemy;
                    }
                }
                #endregion

                #region Ethics
                if (Core.OldEthics)
                {
                    Ethics.Ethic srcEthic = Ethics.Ethic.Find(source, true, true);
                    Ethics.Ethic trgEthic = Ethics.Ethic.Find(target.Owner, true, true);

                    if (srcEthic != null && trgEthic != null && srcEthic != trgEthic)
                        return Notoriety.Enemy;
                }
                #endregion

                #region Alignment
                if (AlignmentSystem.Enabled)
                {
                    AlignmentSystem.Allegiance allegiance = AlignmentSystem.ComputeAllegiance(source, target.Owner, true, true);

                    if (allegiance == AlignmentSystem.Allegiance.Traitor)
                        return Notoriety.CanBeAttacked;

                    if (allegiance == AlignmentSystem.Allegiance.Ally)
                        return Notoriety.Ally;

                    if (allegiance == AlignmentSystem.Allegiance.Enemy)
                        return Notoriety.Enemy;

                    if (AlignmentConfig.StrongholdNotoriety)
                    {
                        AlignmentType srcAlignment = AlignmentSystem.Find(source, true, true);
                        AlignmentType trgAlignment = AlignmentSystem.Find(target.Owner, true, true);

                        if (srcAlignment != AlignmentType.None && trgAlignment == AlignmentType.None && AlignmentSystem.IsInStronghold(source, srcAlignment) && AlignmentSystem.IsInStronghold(target.Owner, srcAlignment))
                            return Notoriety.Enemy;
                    }
                }
                #endregion

                if (target.Owner != null && target.Owner is BaseCreature && ((BaseCreature)target.Owner).AlwaysAttackable)
                    return Notoriety.CanBeAttacked;

                if (CheckHouseFlag(source, target.Owner, target.Location, target.Map))
                    return Notoriety.CanBeAttacked;

                if (!body.IsHuman && !body.IsGhost && !IsPet(target.Owner as BaseCreature) && !IsLivestock(target.Owner as BaseCreature))
                    return Notoriety.CanBeAttacked;

                ArrayList list = target.Aggressors;

                for (int i = 0; i < list.Count; ++i)
                {
                    if (list[i] == source)
                        return Notoriety.CanBeAttacked;
                }

                return Notoriety.Innocent;
            }
        }

        /*
		 * PIX: this returns the special name hue for innocent names
		 * It *assumes* that target is innocent to source otherwise.
		 * Return 0 if no special name hue is to be displayed.
		 */
        public static int MobileBeneficialActs(Mobile source, Mobile target)
        {
            if (source == target) return 0; //if looking at oneself - return normal hue

            #region FightBroker
            //FightBroker - blue fightbrokers should hue different to non-fightbrokers.
            if (FightBroker.IsAlreadyRegistered(target) ||
                FightBroker.IsHealerInterferer(target))
            {
                return 0x18;
            }
            #endregion

            #region kin
            //Kin - blue kin should hue differently to non-kin.
            //if (Engines.IOBSystem.KinSystemSettings.KinNameHueEnabled)
            {
                if (target is PlayerMobile && source is PlayerMobile)
                {
                    PlayerMobile pmsource = source as PlayerMobile;
                    PlayerMobile pmtarget = target as PlayerMobile;

                    if (pmtarget.IOBAlignment != IOBAlignment.None)
                    {
                        if (pmsource.IOBAlignment == IOBAlignment.None)
                        {
                            return 0x18;
                        }
                    }

                    //if we're looking at ourself and we're a healer, show our name in purple
                    if (pmsource == pmtarget && pmtarget.IOBAlignment == IOBAlignment.Healer)
                    {
                        return 0x18;
                    }
                }
            }
            #endregion

            return 0;
        }

        public static int MobileNotoriety(Mobile source, Mobile target)
        {
            #region Sanity
            // adam: sanity
            if (source == null || target == null)
            {
                if (source == null)
                    Console.WriteLine("(source == null) in Notoriety::MobileNotoriety");
                if (target == null)
                    Console.WriteLine("(target == null) in Notoriety::MobileNotoriety");
                //return;
            }
            #endregion

            if (source is BaseCreature)
            {
                int result = ((BaseCreature)source).NotorietyOverride(target);

                if (result != 0)
                    return result;
            }
            else if (source is PlayerMobile && target is BaseCreature)
            {   // doesn't seem to work in at least the case where the BaseCreature is invulnerable
                //  We are correctly sending the Innocent flag in the mobile packet. 
                int result = ((BaseCreature)target).NotorietyOverride(target);

                if (result != 0)
                    return result;
            }
#if false
            if (target.Blessed || (target is BaseVendor && ((BaseVendor)target).IsInvulnerable) || target is PlayerVendor || target is TownCrier)
                return Notoriety.Invulnerable;
#else
            if (Core.AOS && (target.Blessed || (target is BaseVendor && ((BaseVendor)target).IsInvulnerable) || target is PlayerVendor || target is TownCrier))
                return Notoriety.Invulnerable;
#endif

            #region Dueling
            if (source is PlayerMobile && target is PlayerMobile)
            {
                PlayerMobile pmFrom = (PlayerMobile)source;
                PlayerMobile pmTarg = (PlayerMobile)target;

                if (pmFrom.DuelContext != null && pmFrom.DuelContext.StartedBeginCountdown && !pmFrom.DuelContext.Finished && pmFrom.DuelContext == pmTarg.DuelContext)
                    return pmFrom.DuelContext.IsAlly(pmFrom, pmTarg) ? Notoriety.Ally : Notoriety.Enemy;
            }
            #endregion

            // 12/18/22, Adam: Remove the special dispensation for staff .. if you're not blessed, you can be attacked.
#if false
            if (target.AccessLevel > AccessLevel.Player)
                return Notoriety.CanBeAttacked;
#endif

            #region Duel
            /*
			//Begin Challenge Duel Additions
			if (target is PlayerMobile && source is PlayerMobile)
			{
				bool tc = ((PlayerMobile)target).IsInChallenge;
				bool sc = (((PlayerMobile)source).IsInChallenge);
				if (tc && sc) //both in challenge
				{
					foreach (Item c in Challenge.Challenge.WorldStones)
					{
						ChallengeStone cs = c as ChallengeStone;
						if (cs.OpponentTeam.Contains(target) || cs.ChallengeTeam.Contains(target))
						{
							if (cs.OpponentTeam.Contains(source) || cs.ChallengeTeam.Contains(source))
							{
								return Notoriety.CanBeAttacked;
							}
						}
					}
				}
			}
			//End Challenge Duel Additions
			*/
            #endregion

            // it's a pet
            if (source.Player && !target.Player && source is PlayerMobile && target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;

                Mobile master = bc.GetMaster();

                if (master != null && master.AccessLevel > AccessLevel.Player)
                    return Notoriety.CanBeAttacked;

                if (!bc.Summoned && !bc.Controlled && ((PlayerMobile)source).EnemyOfOneType == target.GetType())
                    return Notoriety.Enemy;

                if (Core.OldEthics)
                {
                    Ethics.Ethic srcEthic = Ethics.Ethic.Find(source);
                    Ethics.Ethic trgEthic = Ethics.Ethic.Find(target, true);

                    // outside of town: evil's the target: evil's are red
                    if (trgEthic != null && trgEthic == Ethics.Ethic.Evil && !target.Region.IsGuarded && target.Map == Map.Felucca)
                        return Notoriety.Murderer;
                    // a fallen hero will flag gray and not enemy � this is per the original docs
                    else if (srcEthic == Ethics.Ethic.Evil && target.CheckState(Mobile.ExpirationFlagID.FallenHero))
                        return Notoriety.CanBeAttacked;
                    // An innocent that attacks an evil (evil noto) will be attackable by all evils for two minutes
                    else if (srcEthic == Ethics.Ethic.Evil && target.CheckState(Mobile.ExpirationFlagID.EvilNoto))
                        return Notoriety.CanBeAttacked;
                    // Hero/Evil always at war
                    else if (srcEthic != null && trgEthic != null && srcEthic != trgEthic)
                        return Notoriety.Enemy;
                }
            }

            // 8/10/22, Adam: I don't think Core.RedsInTown matters here. Notoriety is Notoriety
            if (target.Red || (target.Body.IsMonster && IsSummoned(target as BaseCreature) && !(target is BaseFamiliar) && !(target is Golem)) || (target is BaseCreature && (((BaseCreature)target).AlwaysMurderer || ((BaseCreature)target).IsAnimatedDead)))
                return Notoriety.Murderer;

            if (target.Criminal)
                return Notoriety.Criminal;

            #region Alignment Traitor
            if (AlignmentSystem.Enabled && AlignmentSystem.IsTraitor(source, target, true))
                return Notoriety.Enemy;
            #endregion

            Guild sourceGuild = GetGuildFor(source.Guild as Guild, source);
            Guild targetGuild = GetGuildFor(target.Guild as Guild, target);

            if ((sourceGuild != null && targetGuild != null) && (sourceGuild.Peaceful == false && targetGuild.Peaceful == false))
            {
                if (sourceGuild == targetGuild || sourceGuild.IsAlly(targetGuild))
                    return Notoriety.Ally;
                else if (sourceGuild.IsEnemy(targetGuild))
                    return Notoriety.Enemy;
            }

            #region Fightbroker
            //If both are registered with the fightbroker, they can both attack each other
            if (FightBroker.IsAlreadyRegistered(source) && FightBroker.IsAlreadyRegistered(target)
                && (source != target))
            {
                return Notoriety.Enemy;
            }
            //If the source is registered on the fightbroker and the target is an interferer, make the target fair game
            if (FightBroker.IsAlreadyRegistered(source) && FightBroker.IsHealerInterferer(target)
                && (source != target))
            {
                return Notoriety.CanBeAttacked;
            }

            //Now handle pets of the people registered with the fightbroker
            if (source is BaseCreature && target is BaseCreature)
            {
                BaseCreature src = (BaseCreature)source;
                BaseCreature tgt = (BaseCreature)target;
                if (src.ControlMaster != null && tgt.ControlMaster != null)
                {
                    if (FightBroker.IsAlreadyRegistered(src.ControlMaster) &&
                        FightBroker.IsAlreadyRegistered(tgt.ControlMaster) &&
                        (src.ControlMaster != tgt.ControlMaster))
                    {
                        return Notoriety.Enemy;
                    }
                }
            }
            else if (source is PlayerMobile && target is BaseCreature)
            {
                BaseCreature tgt = (BaseCreature)target;
                if (tgt.ControlMaster != null)
                {
                    if (FightBroker.IsAlreadyRegistered(source) &&
                        FightBroker.IsAlreadyRegistered(tgt.ControlMaster) &&
                        (source != tgt.ControlMaster))
                    {
                        return Notoriety.Enemy;
                    }
                }
            }
            else if (source is BaseCreature && target is PlayerMobile)
            {
                BaseCreature src = (BaseCreature)source;
                if (src.ControlMaster != null)
                {
                    if (FightBroker.IsAlreadyRegistered(target) &&
                        FightBroker.IsAlreadyRegistered(src.ControlMaster) &&
                        (target != src.ControlMaster))
                    {
                        return Notoriety.Enemy;
                    }
                }
            }
            //done with pets/fightbroker status
            #endregion

            #region IOB
            //Now handle IOB status hueing
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.IOBShardWide)
                || (Server.Engines.IOBSystem.IOBRegions.IsInIOBRegion(source)
                     && Server.Engines.IOBSystem.IOBRegions.IsInIOBRegion(target)))
            {
                IOBAlignment srcIOBAlignment = IOBAlignment.None;
                IOBAlignment trgIOBAlignment = IOBAlignment.None;
                if (source is BaseCreature) { srcIOBAlignment = ((BaseCreature)source).IOBAlignment; }
                else if (source is PlayerMobile) { srcIOBAlignment = ((PlayerMobile)source).IOBAlignment; }
                if (target is BaseCreature) { trgIOBAlignment = ((BaseCreature)target).IOBAlignment; }
                else if (target is PlayerMobile) { trgIOBAlignment = ((PlayerMobile)target).IOBAlignment; }

                if (srcIOBAlignment != IOBAlignment.None &&
                    trgIOBAlignment != IOBAlignment.None &&
                    srcIOBAlignment != IOBAlignment.Healer
                    )
                {
                    //If they're different alignments OR target is OutCast, then they're an enemy
                    //Pix 12/3/07: added healer target
                    //Pix: 12/4/07 - now kin-healers flag canbeattacked to kin instead of enemy
                    if (trgIOBAlignment == IOBAlignment.Healer)
                    {
                        return Notoriety.CanBeAttacked;
                    }
                    else if (srcIOBAlignment != trgIOBAlignment ||
                        trgIOBAlignment == IOBAlignment.OutCast)
                    {
                        return Notoriety.Enemy;
                    }
                    else
                    {
                        if (source is PlayerMobile && target is BaseCreature)
                        {
                            return Notoriety.Ally;
                        }
                        else
                        {
                            //Pix: 4/28/06 - removed Ally notoriety of same-aligned kin -
                            // this is now handled by guilds via allying
                            //return Notoriety.Ally;
                        }
                    }
                }

                //if we're looking at ourselves, and we're a KinHealer, show ourself as enemy
                if (source == target && srcIOBAlignment == IOBAlignment.Healer)
                {
                    return Notoriety.Enemy;
                }
            }
            //done with IOB status hueing

            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;
                if (bc.IOBFollower)
                {
                    return Notoriety.CanBeAttacked;
                }
            }
            #endregion

            #region Factions
            Faction srcFaction = Faction.Find(source, true, true);
            Faction trgFaction = Faction.Find(target, true, true);

            if (srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet)
                return Notoriety.Enemy;
            #endregion

            #region Ethics
            if (Core.OldEthics)
            {
                Ethics.Ethic srcEthic = Ethics.Ethic.Find(source);
                Ethics.Ethic trgEthic = Ethics.Ethic.Find(target);

                // outside of town: evil's the target: evil's are red
                if (trgEthic != null && trgEthic == Ethics.Ethic.Evil && !target.Region.IsGuarded && target.Map == Map.Felucca)
                    return Notoriety.Murderer;
                // a fallen hero will flag gray and not enemy � this is per the original docs
                else if (srcEthic == Ethics.Ethic.Evil && target.CheckState(Mobile.ExpirationFlagID.FallenHero))
                    return Notoriety.CanBeAttacked;
                // An innocent that attacks an evil (evil noto) will be attackable by all evils for two minutes
                else if (srcEthic == Ethics.Ethic.Evil && target.CheckState(Mobile.ExpirationFlagID.EvilNoto))
                    return Notoriety.CanBeAttacked;
                // Hero/Evil always at war
                else if (srcEthic != null && trgEthic != null && srcEthic != trgEthic)
                    return Notoriety.Enemy;
            }
            #endregion

            #region Alignment
            if (AlignmentSystem.Enabled)
            {
                AlignmentSystem.Allegiance allegiance = AlignmentSystem.ComputeAllegiance(source, target, true, true);

                if (allegiance == AlignmentSystem.Allegiance.Ally)
                    return Notoriety.Ally;

                if (allegiance == AlignmentSystem.Allegiance.Enemy)
                    return Notoriety.Enemy;

                if (AlignmentConfig.StrongholdNotoriety)
                {
                    AlignmentType srcAlignment = AlignmentSystem.Find(source, true, true);
                    AlignmentType trgAlignment = AlignmentSystem.Find(target, true, true);

                    if (srcAlignment != AlignmentType.None && trgAlignment == AlignmentType.None && AlignmentSystem.IsInStronghold(source, srcAlignment) && AlignmentSystem.IsInStronghold(target, srcAlignment))
                        return Notoriety.Enemy;
                }
            }
            #endregion

            if (SkillHandlers.Stealing.ClassicMode && target is PlayerMobile && ((PlayerMobile)target).PermaFlags.Contains(source))
                return Notoriety.CanBeAttacked;

            if (target is BaseCreature && ((BaseCreature)target).AlwaysAttackable)
                return Notoriety.CanBeAttacked;

            if (CheckHouseFlag(source, target, target.Location, target.Map))
                return Notoriety.CanBeAttacked;

            if (!(target is BaseCreature && ((BaseCreature)target).InitialInnocent))
            {
                if (!target.Body.IsHuman && !target.Body.IsGhost && !IsPet(target as BaseCreature) && !IsLivestock(target as BaseCreature) && !TransformationSpellHelper.UnderTransformation(target) /*&& !AnimalForm.UnderTransformation(target) Ninja stuff*/)
                    return Notoriety.CanBeAttacked;
            }

            if (CheckAggressor(source.Aggressors, target))
                return Notoriety.CanBeAttacked;

            if (CheckAggressed(source.Aggressed, target))
                return Notoriety.CanBeAttacked;

            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;

                if (bc.Controlled && bc.ControlOrder == OrderType.Guard && bc.ControlTarget == source)
                    return Notoriety.CanBeAttacked;
            }

            if (source is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)source;

                Mobile master = bc.GetMaster();
                if (master != null && CheckAggressor(master.Aggressors, target))
                    return Notoriety.CanBeAttacked;
            }

            return Notoriety.Innocent;
        }

        public static bool CheckHouseFlag(Mobile from, Mobile m, Point3D p, Map map)
        {
            BaseHouse house = BaseHouse.FindHouseAt(p, map, 16);

            if (house == null || house.Public || !house.IsFriend(from))
                return false;

            if (m != null && house.IsFriend(m))
                return false;

            // June 2, 2001
            // http://martin.brenner.de/ultima/uo/news1.html
            // the (invulnerable) tag has been removed; invulnerable NPCs and players can now be identified by the yellow hue of their name
            // Adam: June 2, 2001 probably means Publish 12 which was July 24, 2001
            // Adam: Because we removed the NameHue for PlayerVendors, we needed to add a Notoriety case (when NameHue is set, there is no Notoriety check)
            if (m is PlayerVendor)
                return false;

            BaseCreature c = m as BaseCreature;

            if (c != null && !c.Deleted && c.Controlled && c.ControlMaster != null)
                return !house.IsFriend(c.ControlMaster);

            return true;
        }

        public static bool IsPet(BaseCreature c)
        {
            return (c != null && c.Controlled);
        }

        public static bool IsSummoned(BaseCreature c)
        {
            return (c != null && /*c.Controled &&*/ c.Summoned);
        }

        public static bool IsLivestock(BaseCreature c)
        {
            return (c != null && c.GetCreatureBool(CreatureBoolTable.IsTownshipLivestock));
        }

        public static bool CheckAggressor(List<AggressorInfo> list, Mobile target)
        {
            for (int i = 0; i < list.Count; ++i)
                if (((AggressorInfo)list[i]).Attacker == target)
                    return true;

            return false;
        }

        public static bool CheckAggressed(List<AggressorInfo> list, Mobile target)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (!info.CriminalAggression && info.Defender == target)
                    return true;
            }

            return false;
        }
    }
}