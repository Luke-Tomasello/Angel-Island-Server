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

/* Scripts/Misc/Notoriety.cs
 * ChangeLog
 *  7/29/2026, Adam
 *      Refactored MobileNotoriety() into an ordered rule pipeline using
 *      focused Try...Notoriety() helpers while preserving behavior.
 *      Introduced NotorietyContext to centralize shared relationship state
 *      (principals, guilds, etc.) and establish a common foundation for
 *      notoriety and combat policy. Began extracting canonical relationship
 *      predicates (e.g., AreGuildEnemies(), AreTownshipEnemies()) to separate
 *      relationship facts from policy decisions.
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
using Server.Regions;
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

        public static int MobileNotoriety(Mobile source, Mobile target)
        {
            #region Sanity

            System.Diagnostics.Debug.Assert(
                source != null && target != null,
                "MobileNotoriety() received a null source or target.");

            if (source == null || target == null)
            {
                if (source == null)
                    Console.WriteLine("(source == null) in Notoriety::MobileNotoriety");

                if (target == null)
                    Console.WriteLine("(target == null) in Notoriety::MobileNotoriety");

                return Notoriety.Innocent;
            }

            #endregion

            NotorietyContext context = new NotorietyContext(source, target);

            #region Creature Notoriety Override

            if (TryCreatureNotorietyOverride(
                    context,
                    out int overrideNotoriety))
            {
                return overrideNotoriety;
            }

            #endregion

            #region Invulnerability

            if (TryInvulnerableNotoriety(
                    context,
                    out int invulnerableNotoriety))
            {
                return invulnerableNotoriety;
            }

            #endregion

            #region Dueling

            if (TryDuelingNotoriety(
                    context,
                    out int duelingNotoriety))
            {
                return duelingNotoriety;
            }

            #endregion

            #region Player versus Creature

            if (TryPlayerCreatureNotoriety(
                    context,
                    out int playerCreatureNotoriety))
            {
                return playerCreatureNotoriety;
            }

            if (TryPlayerCreatureEthicsNotoriety(
                    context,
                    out int playerCreatureEthicsNotoriety))
            {
                return playerCreatureEthicsNotoriety;
            }

            #endregion

            #region Murderer and Criminal

            if (TryMurdererOrCriminalNotoriety(
                    context,
                    out int criminalStatusNotoriety))
            {
                return criminalStatusNotoriety;
            }

            #endregion

            #region Alignment Traitor

            if (TryAlignmentTraitorNotoriety(
                    context,
                    out int alignmentTraitorNotoriety))
            {
                return alignmentTraitorNotoriety;
            }

            #endregion

            #region Guild Notoriety

            if (TryGuildNotoriety(
                    context,
                    out int guildNotoriety))
            {
                return guildNotoriety;
            }

            #endregion

            #region Township Enemies

            if (TryTownshipNotoriety(
                    context,
                    out int townshipNotoriety))
            {
                return townshipNotoriety;
            }

            #endregion

            #region FightBroker

            if (TryFightBrokerNotoriety(
                    context,
                    out int fightBrokerNotoriety))
            {
                return fightBrokerNotoriety;
            }

            #endregion

            #region Factions

            if (TryFactionNotoriety(
                    context,
                    out int factionNotoriety))
            {
                return factionNotoriety;
            }

            #endregion

            #region Ethics

            if (TryEthicsNotoriety(
                    context,
                    out int ethicsNotoriety))
            {
                return ethicsNotoriety;
            }

            #endregion

            #region Alignment

            if (TryAlignmentNotoriety(
                    context,
                    out int alignmentNotoriety))
            {
                return alignmentNotoriety;
            }

            #endregion

            #region Classic Stealing Permaflag

            if (TryPermaFlagNotoriety(
                    context,
                    out int permaFlagNotoriety))
            {
                return permaFlagNotoriety;
            }

            #endregion

            #region Structural Attackability

            if (TryStructuralAttackabilityNotoriety(
                    context,
                    out int structuralNotoriety))
            {
                return structuralNotoriety;
            }

            #endregion

            #region Aggression Rights

            if (TryAggressionNotoriety(
                    context,
                    out int aggressionNotoriety))
            {
                return aggressionNotoriety;
            }

            #endregion

            return Notoriety.Innocent;
        }
        private static bool TryCreatureNotorietyOverride(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (context.Source is BaseCreature sourceCreature)
            {
                result = sourceCreature.NotorietyOverride(context.Target);

                return result != 0;
            }

            if (context.Source is PlayerMobile &&
                context.Target is BaseCreature targetCreature)
            {
                result = targetCreature.NotorietyOverride(context.Target);

                return result != 0;
            }

            return false;
        }
        private static bool TryInvulnerableNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (!Core.AOS)
                return false;

            Mobile target = context.Target;

            if (target.Blessed ||
                (target is BaseVendor vendor && vendor.IsInvulnerable) ||
                target is PlayerVendor ||
                target is TownCrier)
            {
                result = Notoriety.Invulnerable;
                return true;
            }

            return false;
        }
        private static bool TryDuelingNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (!(context.Source is PlayerMobile sourcePlayer) ||
                !(context.Target is PlayerMobile targetPlayer))
            {
                return false;
            }

            if (sourcePlayer.DuelContext == null)
                return false;

            if (!sourcePlayer.DuelContext.StartedBeginCountdown)
                return false;

            if (sourcePlayer.DuelContext.Finished)
                return false;

            if (sourcePlayer.DuelContext != targetPlayer.DuelContext)
                return false;

            result = sourcePlayer.DuelContext.IsAlly(
                sourcePlayer,
                targetPlayer)
                ? Notoriety.Ally
                : Notoriety.Enemy;

            return true;
        }
        private static bool TryPlayerCreatureNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (!context.Source.Player ||
                context.Target.Player ||
                !(context.Source is PlayerMobile sourcePlayer) ||
                !(context.Target is BaseCreature targetCreature))
            {
                return false;
            }

            Mobile? master = targetCreature.GetMaster();

            if (master != null &&
                master.AccessLevel > AccessLevel.Player)
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            if (!targetCreature.Summoned &&
                !targetCreature.Controlled &&
                sourcePlayer.EnemyOfOneType == targetCreature.GetType())
            {
                result = Notoriety.Enemy;
                return true;
            }

            return false;
        }
        private static bool TryPlayerCreatureEthicsNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (!Core.OldEthics)
                return false;

            if (!context.Source.Player ||
                context.Target.Player ||
                !(context.Source is PlayerMobile) ||
                !(context.Target is BaseCreature))
            {
                return false;
            }

            Ethics.Ethic sourceEthic =
                Ethics.Ethic.Find(context.Source);

            Ethics.Ethic targetEthic =
                Ethics.Ethic.Find(context.Target, true);

            // Outside guarded towns on Felucca, evil targets appear red.
            if (targetEthic != null &&
                targetEthic == Ethics.Ethic.Evil &&
                !context.Target.Region.IsGuarded &&
                context.Target.Map == Map.Felucca)
            {
                result = Notoriety.Murderer;
                return true;
            }

            // A fallen hero flags gray rather than enemy.
            if (sourceEthic == Ethics.Ethic.Evil &&
                context.Target.CheckState(
                    Mobile.ExpirationFlagID.FallenHero))
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            // An innocent who attacks an evil remains attackable
            // by all evil players for two minutes.
            if (sourceEthic == Ethics.Ethic.Evil &&
                context.Target.CheckState(
                    Mobile.ExpirationFlagID.EvilNoto))
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            // Hero and Evil are always at war.
            if (sourceEthic != null &&
                targetEthic != null &&
                sourceEthic != targetEthic)
            {
                result = Notoriety.Enemy;
                return true;
            }

            return false;
        }

        #region Predicates
        private static bool HasGuildCombatRights(NotorietyContext context)
        {
            Guild sourceGuild = context.SourceGuild;
            Guild targetGuild = context.TargetGuild;

            return sourceGuild != null &&
                   targetGuild != null &&
                   !sourceGuild.Peaceful &&
                   !targetGuild.Peaceful &&
                   (AreSameGuild(context) ||
                    AreGuildAllies(context) ||
                    AreGuildEnemies(context));
        }
        private static bool AreSameGuild(NotorietyContext context)
        {
            return context.SourceGuild != null &&
                   context.SourceGuild == context.TargetGuild;
        }

        private static bool AreGuildAllies(NotorietyContext context)
        {
            return context.SourceGuild != null &&
                   context.TargetGuild != null &&
                   context.SourceGuild.IsAlly(context.TargetGuild);
        }

        private static bool AreGuildEnemies(NotorietyContext context)
        {
            return context.SourceGuild != null &&
                   context.TargetGuild != null &&
                   context.SourceGuild.IsEnemy(context.TargetGuild);
        }
        private static bool AreTownshipEnemies(NotorietyContext context)
        {
            Mobile source = context.SourcePrincipal;
            Mobile target = context.TargetPrincipal;

            if (source == target)
                return false;

            TownshipRegion township =
                TownshipRegion.GetTownshipAt(
                    context.Target.Location,
                    context.Target.Map);

            return township != null &&
                   township.HasEnemyNotoriety(source, target);
        }

        #endregion Predicates

        private static bool TryMurdererOrCriminalNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            Mobile target = context.Target;

            if (target.Red ||
                (target.Body.IsMonster &&
                 IsSummoned(target as BaseCreature) &&
                 !(target is BaseFamiliar) &&
                 !(target is Golem)) ||
                (target is BaseCreature creature &&
                 (creature.AlwaysMurderer ||
                  creature.IsAnimatedDead)))
            {
                result = Notoriety.Murderer;
                return true;
            }

            if (target.Criminal)
            {
                result = Notoriety.Criminal;
                return true;
            }

            return false;
        }
        private static bool TryAlignmentTraitorNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (!AlignmentSystem.Enabled)
                return false;

            if (!AlignmentSystem.IsTraitor(
                    context.Source,
                    context.Target,
                    true))
            {
                return false;
            }

            result = Notoriety.Enemy;
            return true;
        }
        private static bool TryGuildNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            Guild sourceGuild = context.SourceGuild;
            Guild targetGuild = context.TargetGuild;

            if (sourceGuild == null || targetGuild == null)
                return false;

            if (sourceGuild.Peaceful || targetGuild.Peaceful)
                return false;

            if (AreSameGuild(context) ||
                AreGuildAllies(context))
            {
                result = Notoriety.Ally;
                return true;
            }

            if (AreGuildEnemies(context))
            {
                result = Notoriety.Enemy;
                return true;
            }

            return false;
        }
        private static bool TryTownshipNotoriety(
            NotorietyContext context,
            out int notoriety)
        {
            if (AreTownshipEnemies(context))
            {
                notoriety = Notoriety.Enemy;
                return true;
            }

            notoriety = Notoriety.Innocent;
            return false;
        }

        private static bool TryFightBrokerNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            Mobile source = context.Source;
            Mobile target = context.Target;

            // If both are registered with the FightBroker,
            // they can attack each other.
            if (source != target &&
                FightBroker.IsAlreadyRegistered(source) &&
                FightBroker.IsAlreadyRegistered(target))
            {
                result = Notoriety.Enemy;
                return true;
            }

            // If the source is registered and the target is an
            // interferer, make the target fair game.
            if (source != target &&
                FightBroker.IsAlreadyRegistered(source) &&
                FightBroker.IsHealerInterferer(target))
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            // Handle pets belonging to registered players.
            if (source is BaseCreature src && target is BaseCreature tgt)
            {
                if (src.ControlMaster != null &&
                    tgt.ControlMaster != null &&
                    src.ControlMaster != tgt.ControlMaster &&
                    FightBroker.IsAlreadyRegistered(src.ControlMaster) &&
                    FightBroker.IsAlreadyRegistered(tgt.ControlMaster))
                {
                    result = Notoriety.Enemy;
                    return true;
                }
            }
            else if (source is PlayerMobile && target is BaseCreature targetCreature)
            {
                if (targetCreature.ControlMaster != null &&
                    source != targetCreature.ControlMaster &&
                    FightBroker.IsAlreadyRegistered(source) &&
                    FightBroker.IsAlreadyRegistered(targetCreature.ControlMaster))
                {
                    result = Notoriety.Enemy;
                    return true;
                }
            }
            else if (source is BaseCreature sourceCreature &&
                     target is PlayerMobile)
            {
                if (sourceCreature.ControlMaster != null &&
                    target != sourceCreature.ControlMaster &&
                    FightBroker.IsAlreadyRegistered(target) &&
                    FightBroker.IsAlreadyRegistered(sourceCreature.ControlMaster))
                {
                    result = Notoriety.Enemy;
                    return true;
                }
            }

            return false;
        }
        private static bool TryFactionNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            Faction sourceFaction =
                Faction.Find(
                    context.Source,
                    true,
                    true);

            Faction targetFaction =
                Faction.Find(
                    context.Target,
                    true,
                    true);

            if (sourceFaction == null ||
                targetFaction == null ||
                sourceFaction == targetFaction)
            {
                return false;
            }

            if (context.Source.Map != Faction.Facet)
                return false;

            result = Notoriety.Enemy;
            return true;
        }
        private static bool TryEthicsNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (!Core.OldEthics)
                return false;

            Mobile source = context.Source;
            Mobile target = context.Target;

            Ethics.Ethic sourceEthic =
                Ethics.Ethic.Find(source);

            Ethics.Ethic targetEthic =
                Ethics.Ethic.Find(target);

            // Outside guarded towns on Felucca, evil targets appear red.
            if (targetEthic != null &&
                targetEthic == Ethics.Ethic.Evil &&
                !target.Region.IsGuarded &&
                target.Map == Map.Felucca)
            {
                result = Notoriety.Murderer;
                return true;
            }

            // A fallen hero flags gray to evil rather than orange.
            if (sourceEthic == Ethics.Ethic.Evil &&
                target.CheckState(Mobile.ExpirationFlagID.FallenHero))
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            // An innocent who attacks an evil remains attackable
            // by all evil players for two minutes.
            if (sourceEthic == Ethics.Ethic.Evil &&
                target.CheckState(Mobile.ExpirationFlagID.EvilNoto))
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            // Hero and Evil are always at war.
            if (sourceEthic != null &&
                targetEthic != null &&
                sourceEthic != targetEthic)
            {
                result = Notoriety.Enemy;
                return true;
            }

            return false;
        }
        private static bool TryAlignmentNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (!AlignmentSystem.Enabled)
                return false;

            Mobile source = context.Source;
            Mobile target = context.Target;

            AlignmentSystem.Allegiance allegiance =
                AlignmentSystem.ComputeAllegiance(
                    source,
                    target,
                    true,
                    true);

            if (allegiance == AlignmentSystem.Allegiance.Ally)
            {
                result = Notoriety.Ally;
                return true;
            }

            if (allegiance == AlignmentSystem.Allegiance.Enemy)
            {
                result = Notoriety.Enemy;
                return true;
            }

            if (!AlignmentConfig.StrongholdNotoriety)
                return false;

            AlignmentType sourceAlignment =
                AlignmentSystem.Find(
                    source,
                    true,
                    true);

            AlignmentType targetAlignment =
                AlignmentSystem.Find(
                    target,
                    true,
                    true);

            if (sourceAlignment != AlignmentType.None &&
                targetAlignment == AlignmentType.None &&
                AlignmentSystem.IsInStronghold(source, sourceAlignment) &&
                AlignmentSystem.IsInStronghold(target, sourceAlignment))
            {
                result = Notoriety.Enemy;
                return true;
            }

            return false;
        }
        private static bool TryPermaFlagNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            if (!SkillHandlers.Stealing.ClassicMode)
                return false;

            if (!(context.Target is PlayerMobile targetPlayer))
                return false;

            if (!targetPlayer.ContainsPermaFlag(context.Source))
                return false;

            result = Notoriety.CanBeAttacked;
            return true;
        }
        private static bool TryStructuralAttackabilityNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            Mobile source = context.Source;
            Mobile target = context.Target;

            if (target is BaseCreature alwaysAttackable &&
                alwaysAttackable.AlwaysAttackable)
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            if (CheckHouseFlag(
                    source,
                    target,
                    target.Location,
                    target.Map))
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            if (!(target is BaseCreature creature &&
                  creature.InitialInnocent))
            {
                if (!target.Body.IsHuman &&
                    !target.Body.IsGhost &&
                    !IsPet(target as BaseCreature) &&
                    !IsLivestock(target as BaseCreature) &&
                    !TransformationSpellHelper.UnderTransformation(target))
                {
                    result = Notoriety.CanBeAttacked;
                    return true;
                }
            }

            return false;
        }
        private static bool TryAggressionNotoriety(
            NotorietyContext context,
            out int result)
        {
            result = 0;

            Mobile source = context.Source;
            Mobile target = context.Target;

            if (CheckAggressor(source.Aggressors, target))
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            if (CheckAggressed(source.Aggressed, target))
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            if (target is BaseCreature guardedCreature &&
                guardedCreature.Controlled &&
                guardedCreature.ControlOrder == OrderType.Guard &&
                guardedCreature.ControlTarget == source)
            {
                result = Notoriety.CanBeAttacked;
                return true;
            }

            if (source is BaseCreature sourceCreature)
            {
                Mobile? master = sourceCreature.GetMaster();

                if (master != null &&
                    CheckAggressor(master.Aggressors, target))
                {
                    result = Notoriety.CanBeAttacked;
                    return true;
                }
            }

            return false;
        }
        private sealed class NotorietyContext
        {
            public Mobile Source { get; }
            public Mobile Target { get; }

            public Mobile SourcePrincipal { get; }
            public Mobile TargetPrincipal { get; }

            public Guild SourceGuild { get; }
            public Guild TargetGuild { get; }

            public NotorietyContext(Mobile source, Mobile target)
            {
                Source = source;
                Target = target;

                SourcePrincipal = ResolvePrincipal(source);
                TargetPrincipal = ResolvePrincipal(target);

                SourceGuild = GetGuildFor(source.Guild as Guild, source);
                TargetGuild = GetGuildFor(target.Guild as Guild, target);
            }
        }
        private enum GuildStatus { None, Peaceful, Warring }

        private static GuildStatus GetGuildStatus(Mobile m)
        {
            Guild? g = m.Guild as Guild;

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
            PlayerMobile? pmFrom = from as PlayerMobile;
            PlayerMobile? pmTarg = target as PlayerMobile;

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

            Engines.ConPVP.SafeZone? sz = from.Region.GetRegion(typeof(Engines.ConPVP.SafeZone)) as Engines.ConPVP.SafeZone;

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

            Guild? fromGuild = from.Guild as Guild;
            Guild? targetGuild = target.Guild as Guild;

            if (fromGuild != null && targetGuild != null && (targetGuild == fromGuild || fromGuild.IsAlly(targetGuild)))
                return true; // Guild members can be beneficial

            return CheckBeneficialStatus(GetGuildStatus(from), GetGuildStatus(target));
        }

        public static bool Mobile_AllowHarmful(Mobile from, Mobile target)
        {
            if (from == null || target == null)
                return true;

            #region Dueling
            PlayerMobile? pmFrom = from as PlayerMobile;
            PlayerMobile? pmTarg = target as PlayerMobile;

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

            Engines.ConPVP.SafeZone? sz = from.Region.GetRegion(typeof(Engines.ConPVP.SafeZone)) as Engines.ConPVP.SafeZone;

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

            NotorietyContext context = new NotorietyContext(from, target);

            Guild fromGuild = context.SourceGuild;
            Guild targetGuild = context.TargetGuild;

            if (HasGuildCombatRights(context))
            {
                return true; // Guild relationships grant combat rights
            }

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
        private static Mobile ResolvePrincipal(Mobile mobile)
        {
            if (mobile is BaseCreature creature &&
                creature.Controlled &&
                creature.ControlMaster != null)
            {
                return creature.ControlMaster;
            }

            return mobile;
        }
        public static Guild GetGuildFor(Guild? def, Mobile m)
        {
            Guild? g = def;

            BaseCreature? c = m as BaseCreature;

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

            BaseCreature? cretOwner = target.Owner as BaseCreature;

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
                Faction? srcFaction = Faction.Find(source, true, true);
                Faction? trgFaction = Faction.Find(target.Owner, true, true);

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

                Party? sourceParty = Party.Get(source);

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
                Faction? srcFaction = Faction.Find(source, true, true);
                Faction? trgFaction = Faction.Find(target.Owner, true, true);

                if (srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet)
                {
                    ArrayList secondList = target.Aggressors;

                    for (int i = 0; i < secondList.Count; ++i)
                    {
                        Mobile? mob = secondList[i] as Mobile;

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
                    Ethics.Ethic? srcEthic = Ethics.Ethic.Find(source, true, true);
                    Ethics.Ethic? trgEthic = Ethics.Ethic.Find(target.Owner, true, true);

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

            return 0;
        }

        public static bool CheckHouseFlag(Mobile from, Mobile? m, Point3D p, Map map)
        {
            BaseHouse? house = BaseHouse.FindHouseAt(p, map, 16);

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

            BaseCreature? c = m as BaseCreature;

            if (c != null && !c.Deleted && c.Controlled && c.ControlMaster != null)
                return !house.IsFriend(c.ControlMaster);

            return true;
        }

        public static bool IsPet(BaseCreature? c)
        {
            return (c != null && c.Controlled);
        }

        public static bool IsSummoned(BaseCreature? c)
        {
            return (c != null && /*c.Controled &&*/ c.Summoned);
        }

        public static bool IsLivestock(BaseCreature? c)
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