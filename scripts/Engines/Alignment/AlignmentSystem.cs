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

/* Scripts\Engines\Alignment\AlignmentSystem.cs
 * Changelog:
 *  8/14/2023, Adam (GetDynamicAlignment)
 *      Refactor alignment system as follows:
 *          1. Deprecate SecondaryGuildAlignment mechanism.
 *          2. Creatures that are alignment capable, but are not in an aligned region, are unaligned.
 *          3. Remove the two alignment hardcodedness. Now all creatures are allowed any number of alignments. (unusual.)
 *  8/2/2023, Adam (GetRegionAlignment)
 *      Added ChampEngine test where we test for spawner, they're equivalent.
 *  7/23/23, Yoar
 *      Added BaseCreature.SecondaryGuildAlignment
 *      We prefer secondary alignment if the creature is spawning in a matching alignment region.
 *  7/19/23, Yoar
 *      Added CreatureAllegiance. Can switch between:
 *      1. Disabled,
 *      2. Global (default)
 *      3. Regional
 *  5/3/23, Yoar
 *      Added AllowBeneficialAction handle
 *      Players can now heal opposing kin if they're guild allies
 *  4/30/23, Yoar
 *      Rewrote TitleDisplay from index to flags
 *      Added GetTitleDisplay(Mobile mob)
 *  4/19/23, Yoar
 *      Added TitleDisplay enum and 3 ways to display alignment titles.
 *  4/16/23, Yoar
 *      Traitor status now carries over to pets.
 *  4/15/23, Yoar
 *      Added notion of traitors. Attacking guild-aligned monsters makes you a traitor.
 *      Also, performing beneficial acts on a traitor makes you a traitor as well.
 *      Traitors will be attacked by guild-aligned monsters.
 *  4/15/23, Yoar
 *      Reworked notoriety: Guild diplomacy takes precedence over guild alignment.
 *  4/14/23, Yoar
 *      Initial version.
 */

using Server.Guilds;
using Server.Mobiles;
using Server.Regions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Engines.Alignment
{
    [Flags]
    public enum TitleDisplay : byte
    {
        None = 0x00,

        Paperdoll = 0x01,
        GuildSuffix = 0x02,
        Overhead = 0x04,

        Default = 0x80,
    }

    public enum CreatureAllegiance : byte
    {
        Disabled,
        Global,
        Regional,

        Default = Global,
    }

    public static class AlignmentSystem
    {
        public static bool Enabled { get { return (Core.RuleSets.AngelIslandRules() || Core.RuleSets.SiegeStyleRules()); } }

        public static void Initialize()
        {
            AlignmentPersistence.EnsureExistence();

            EventSink.Login += EventSink_OnLogin;
            EventSink.Logout += EventSink_OnLogout;
            EventSink.AggressiveAction += EventSink_OnAggressiveAction;
        }

        private static void EventSink_OnLogin(LoginEventArgs e)
        {
            if (Enabled)
                HandleLogin(e.Mobile);
        }

        private static void EventSink_OnLogout(LogoutEventArgs e)
        {
            if (Enabled)
                HandleLogout(e.Mobile);
        }

        private static void EventSink_OnAggressiveAction(AggressiveActionEventArgs e)
        {
            if (Enabled)
                HandleAggressiveAction(e.Aggressor, e.Aggressed);
        }

        public static AlignmentType Find(Mobile mob, bool inherit = false, bool creatureAllegiances = false)
        {
            if (mob == null)
                return AlignmentType.None;

            Guild g = mob.Guild as Guild;

            if (g != null)
                return g.Alignment;

            if (inherit && mob is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)mob;

                if (bc.Controlled)
                    return Find(bc.ControlMaster, false);
                else if (bc.Summoned)
                    return Find(bc.SummonMaster, false);
                else if (creatureAllegiances && CheckCreatureAllegiance(bc))
                    return bc.GuildAlignment;
            }

            return AlignmentType.None;
        }

        public enum Allegiance : byte
        {
            None,
            Ally,
            Enemy,
            Traitor,
        }

        public static Allegiance ComputeAllegiance(Mobile source, Mobile target, bool inherit, bool creatureAllegiances)
        {
            if (source == null || target == null)
                return Allegiance.None;

            Mobile sourceMaster = source;
            Mobile targetMaster = target;

            if (inherit)
            {
                sourceMaster = GetMaster(sourceMaster);
                targetMaster = GetMaster(targetMaster);
            }

            Guild sourceGuild = sourceMaster.Guild as Guild;
            Guild targetGuild = targetMaster.Guild as Guild;

            AlignmentType sourceAlignment = (sourceGuild == null ? AlignmentType.None : sourceGuild.Alignment);
            AlignmentType targetAlignment = (targetGuild == null ? AlignmentType.None : targetGuild.Alignment);

            if (creatureAllegiances)
            {
                if (sourceAlignment == AlignmentType.None && source is BaseCreature && CheckCreatureAllegiance((BaseCreature)source))
                    sourceAlignment = ((BaseCreature)source).GuildAlignment;

                if (targetAlignment == AlignmentType.None && target is BaseCreature && CheckCreatureAllegiance((BaseCreature)target))
                    targetAlignment = ((BaseCreature)target).GuildAlignment;
            }

            if (sourceAlignment != AlignmentType.None && sourceAlignment == targetAlignment && IsTraitor(targetMaster))
                return Allegiance.Traitor;

            if (sourceGuild != null && targetGuild != null)
            {
                if (sourceGuild == targetGuild)
                    return Allegiance.Ally;

                if (sourceGuild.IsAlly(targetGuild))
                    return Allegiance.Ally;

                if (sourceGuild.IsEnemy(targetGuild))
                    return Allegiance.Enemy;
            }

            if (sourceAlignment != AlignmentType.None && targetAlignment != AlignmentType.None)
            {
                if (sourceAlignment == targetAlignment)
                    return Allegiance.Ally;

                if (sourceAlignment != targetAlignment)
                    return Allegiance.Enemy;
            }

            return Allegiance.None;
        }

        private static Mobile GetMaster(Mobile m)
        {
            if (m is BaseCreature)
            {
                Mobile master = ((BaseCreature)m).GetMaster();

                if (master != null)
                    return master;
            }

            return m;
        }

        public static bool IsAlly(Mobile source, Mobile target, bool inherit = false, bool creatureAllegiances = false)
        {
            return (ComputeAllegiance(source, target, inherit, creatureAllegiances) == Allegiance.Ally);
        }

        public static bool IsEnemy(Mobile source, Mobile target, bool inherit = false, bool creatureAllegiances = false)
        {
            return (ComputeAllegiance(source, target, inherit, creatureAllegiances) == Allegiance.Enemy);
        }

        public static bool IsTraitor(Mobile source, Mobile target, bool inherit = false, bool creatureAllegiances = false)
        {
            return (ComputeAllegiance(source, target, inherit, creatureAllegiances) == Allegiance.Traitor);
        }

        public static string GetName(AlignmentType type)
        {
            Alignment alignment = Alignment.Get(type);

            if (alignment != null)
                return alignment.Definition.Name;

            return null;
        }

        public static TitleDisplay GetTitleDisplay(Mobile mob)
        {
            TitleDisplay display = TitleDisplay.Default;

            if (mob is PlayerMobile)
                display = ((PlayerMobile)mob).AlignmentTitleDisplay;

            if (display == TitleDisplay.Default)
                return AlignmentConfig.TitleDisplay;

            return display;
        }

        public static string GetTitle(Mobile mob)
        {
            Alignment alignment = Alignment.Get(Find(mob));

            if (alignment == null)
                return null;

            return alignment.Definition.Title;
        }

        public static string GetRankTitle(Mobile mob)
        {
            Alignment alignment = Alignment.Get(Find(mob));

            if (alignment == null)
                return null;

            string[] titles = alignment.Definition.RankTitles;

            int index = Math.Max(0, Math.Min(titles.Length - 1, GetRank(mob)));

            if (index >= 0 && index < titles.Length)
                return titles[index];

            return null;
        }

        public static int GetRank(Mobile mob)
        {
            AlignmentPlayer pl = AlignmentPlayer.Find(mob);

            if (pl != null)
                return pl.Rank;

            return 0;
        }

        public static int GetHue(AlignmentType type, bool primary = true)
        {
            Alignment alignment = Alignment.Get(type);

            if (alignment == null)
                return 0;

            if (primary)
                return alignment.Definition.PrimaryHue;
            else
                return alignment.Definition.SecondaryHue;
        }

        public static bool IsInStronghold(Mobile mob, AlignmentType type)
        {
            if (mob == null)
                return false;

            StaticRegion sr = mob.Region.GetRegion(typeof(StaticRegion)) as StaticRegion;

            return (sr != null && sr.GuildAlignment == type);
        }

        public static void OnAlign(Guild g, AlignmentType oldType)
        {
            foreach (Mobile m in g.Members)
            {
                TheFlag.ReturnFlags(m);

                AlignmentPlayer pl = AlignmentPlayer.Find(m);

                if (pl != null)
                    pl.OnAlign(g.Alignment, oldType);

                ClearTraitor(m);
            }

            Server.Items.TownshipStone ts = g.TownshipStone as Server.Items.TownshipStone;

            if (ts != null && ts.CustomRegion != null)
                ts.CustomRegion.GuildAlignment = g.Alignment;
        }

        public static void HandleLogin(Mobile mob)
        {
        }

        public static void HandleLogout(Mobile mob)
        {
            TheFlag.ReturnFlags(mob);
        }

        public static void HandleAggressiveAction(Mobile source, Mobile target)
        {
            if (source == null || target == null)
                return;

            AlignmentType sourceAlignment = Find(source, true);

            if (target is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)target;

                if (sourceAlignment != AlignmentType.None && sourceAlignment == bc.GuildAlignment && !bc.Controlled && !bc.Summoned && CheckCreatureAllegiance(bc))
                {
                    Mobile responsible = GetDamageMaster(source, target);

                    if (responsible != null)
                        ResetTraitor(responsible);
                }
            }
        }

        public static bool AllowBeneficialAction(Mobile source, Mobile target)
        {
            AlignmentType sourceAlignment = Find(source, true);
            AlignmentType targetAlignment = Find(target, true);

            // disallow if (1) they're aligned and (2) we're not
            if (targetAlignment != AlignmentType.None && sourceAlignment == AlignmentType.None)
                return false;

            // disallow if...
            // (1) they are of opposing alignment and...
            if (targetAlignment != AlignmentType.None && sourceAlignment != targetAlignment)
            {
                Guild sourceGuild = source.Guild as Guild;
                Guild targetGuild = target.Guild as Guild;

                // (2) we're not guild allies
                if (sourceGuild == null || targetGuild == null || (sourceGuild != targetGuild && !sourceGuild.IsAlly(targetGuild)))
                    return false;
            }

            return true;
        }

        public static void HandleBeneficialAction(Mobile source, Mobile target)
        {
            if (source == null || target == null)
                return;

            AlignmentType sourceAlignment = Find(source, true);
            AlignmentType targetAlignment = Find(target, true);

            if (source != target && sourceAlignment != AlignmentType.None && sourceAlignment == targetAlignment && IsTraitor(target))
            {
                Mobile responsible = GetDamageMaster(source, null);

                if (responsible != null)
                    ResetTraitor(responsible);
            }
        }

        private static Mobile GetDamageMaster(Mobile source, Mobile target)
        {
            if (source is BaseCreature)
            {
                Mobile master = ((BaseCreature)source).GetDamageMaster(target);

                if (master != null)
                    return master;
            }

            return source;
        }

        #region Traitor Timer

        private static readonly Dictionary<Mobile, TraitorTimer> m_TraitorTimers = new Dictionary<Mobile, TraitorTimer>();

        public static bool IsTraitor(Mobile m)
        {
            return (m_TraitorTimers.ContainsKey(m));
        }

        public static void ResetTraitor(Mobile m)
        {
            TraitorTimer timer;

            if (m_TraitorTimers.TryGetValue(m, out timer))
                timer.LastAggression = DateTime.UtcNow;
            else
                (m_TraitorTimers[m] = new TraitorTimer(m)).Start();
        }

        public static void ClearTraitor(Mobile m)
        {
            TraitorTimer timer;

            if (m_TraitorTimers.TryGetValue(m, out timer))
            {
                timer.Stop();
                m_TraitorTimers.Remove(m);
            }
        }

        private class TraitorTimer : Timer
        {
            private Mobile m_Mobile;
            private DateTime m_LastAggression;

            public DateTime LastAggression
            {
                get { return m_LastAggression; }
                set { m_LastAggression = value; }
            }

            public TraitorTimer(Mobile m)
                : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
            {
                m_Mobile = m;
                m_LastAggression = DateTime.UtcNow;
            }

            protected override void OnTick()
            {
                if (DateTime.UtcNow >= m_LastAggression + AlignmentConfig.TraitorCooldown)
                    ClearTraitor(m_Mobile);
            }
        }

        #endregion

        public static void HandleDeath(Mobile victim, Mobile killer)
        {
            TheFlag.ReturnFlags(victim);

            if (killer == null)
                return;

            AlignmentType victimAlignment = Find(victim, false, true);
            AlignmentType killerAlignment = Find(killer, false, true);

            if (killer.Player && killerAlignment != AlignmentType.None)
                KillCounter.RegisterKills(killerAlignment);

            if (AlignmentConfig.KillPointsEnabled && victim.Player && killer.Player && victimAlignment != AlignmentType.None && killerAlignment != victimAlignment)
            {
                AlignmentPlayer victimPlayer = AlignmentPlayer.Find(victim, true);
                AlignmentPlayer killerPlayer = AlignmentPlayer.Find(killer, true);

                if (victimPlayer != null && killerPlayer != null && DateTime.UtcNow >= victimPlayer.NextKillAward)
                {
                    if (AwardElo(killerPlayer, victimPlayer))
                    {
                        // TODO: Update ranks?
                    }

                    victimPlayer.NextKillAward = DateTime.UtcNow + AlignmentConfig.KillAwardCooldown;
                }
            }
        }

        #region Elo

        public const int Elo0 = 1600;
        public const int EloK = 32;

        public static bool AwardElo(AlignmentPlayer player1, AlignmentPlayer player2)
        {
            return AwardElo(new AlignmentPlayer[] { player1 }, new AlignmentPlayer[] { player2 });
        }

        public static bool AwardElo(AlignmentPlayer[] team1, AlignmentPlayer[] team2)
        {
            if (team1.Length == 0 || team2.Length == 0)
                return false;

            int iTeam1Rating = 0;
            int iTeam2Rating = 0;

            foreach (AlignmentPlayer pl in team1)
                iTeam1Rating += pl.Points;

            foreach (AlignmentPlayer pl in team2)
                iTeam2Rating += pl.Points;

            double team1Rating = (double)iTeam1Rating / team1.Length;
            double team2Rating = (double)iTeam2Rating / team2.Length;

            int delta = (int)(EloK * (1.0 / (1.0 + Math.Pow(10.0, (team1Rating - team2Rating) / 400.0))));

            int award;

            if (team1.Length == 1 && team2.Length == 1)
                award = delta;
            else if (team1.Length == 1)
                award = delta / team2.Length;
            else if (team2.Length == 1)
                award = delta / team1.Length;
            else
                award = 0; // not supported

            if (award == 0)
                return false;

            int offset = (award != 1 ? 0 : 2); // for pluralization

            string team1Name = (team1.Length == 1 ? team1[0].Owner.Name : GetName(Find(team1[0].Owner, false, true)));
            string team2Name = (team2.Length == 1 ? team2[0].Owner.Name : GetName(Find(team2[0].Owner, false, true)));

            string args = String.Format("{0}\t{1}\t{2}", award, team2Name, team1Name);

            for (int i = 0; i < team1.Length; i++)
            {
                team1[i].Points += award;
                team1[i].Owner.SendLocalizedMessage(1042737 + offset, args); // Thou hast been honored with ~1_KILL_POINTS~ kill point(s) for vanquishing ~2_DEAD_PLAYER~!
            }

            for (int i = 0; i < team2.Length; i++)
            {
                team2[i].Points -= award;
                team2[i].Owner.SendLocalizedMessage(1042738 + offset, args); // Thou has lost ~1_KILL_POINTS~ kill point(s) to ~3_ATTACKER_NAME~ for being vanquished!
            }

            return true;
        }

        #endregion

        public static bool CheckEquip(Mobile mob, Item item)
        {
            if (!AlignmentItem.CanEquip(mob, item, true))
                return false;

            return true;
        }

        public static bool ValidateEquipment(Mobile mob, Item item)
        {
            if (!AlignmentItem.CanEquip(mob, item, false))
                return false;

            return true;
        }

        public static void HandleEquip(Mobile mob, Item item)
        {
        }

        public static void HandleUnequip(Mobile mob, Item item)
        {
        }

        public static void OnEnterRegion(StaticRegion sr, Mobile mob)
        {
            if (sr.GuildAlignment != AlignmentType.None)
            {
                // TODO: Alert aligned guilds?
            }
        }

        public static void OnExitRegion(StaticRegion sr, Mobile mob)
        {
        }

        public static bool CheckCreatureAllegiance(BaseCreature bc)
        {
            switch (AlignmentConfig.CreatureAllegiance)
            {
                case CreatureAllegiance.Disabled:
                    {
                        return false;
                    }
                case CreatureAllegiance.Global:
                    {
                        return true;
                    }
                case CreatureAllegiance.Regional:
                    {
                        Alignment alignment = Alignment.Get(bc.GuildAlignment);

                        if (alignment != null && alignment.Definition.GlobalAllegiance)
                            return true;

                        return (GetRegionAlignment(bc) != AlignmentType.None);
                    }
            }

            return false;
        }

        public static void HandleSpawn(BaseCreature bc)
        {
#if false
            AlignmentType prim = bc.DefaultGuildAlignment;
            AlignmentType scnd = AlignmentType.None;// bc.SecondaryGuildAlignment;

            // prefer secondary alignment if we're spawning in a matching alignment region
            if (bc.GuildAlignment == prim && scnd != AlignmentType.None && scnd == GetRegionAlignment(bc))
                bc.GuildAlignment = scnd;
#endif
        }
        public static AlignmentType GetDynamicAlignment(BaseCreature bc, AlignmentType[] types)
        {
            if (AlignmentSystem.Enabled)
            {
                if (AlignmentConfig.CreatureAllegiance == CreatureAllegiance.Regional)
                {
                    AlignmentType at = AlignmentSystem.GetRegionAlignment(bc);
                    if (types.Contains(at))
                        return at;
                }
                else if (AlignmentConfig.CreatureAllegiance == CreatureAllegiance.Global)
                {
                    if (types.Length > 0)
                        return types[0];
                }
            }
            return AlignmentType.None;
        }
        public static AlignmentType GetRegionAlignment(BaseCreature bc)
        {
            Point3D loc = bc.Location;

            if (bc.Spawner != null && bc.Home != Point3D.Zero)
                loc = bc.Home;

            if (bc.ChampEngine != null)
                loc = bc.ChampEngine.Location;

            return GetRegionAlignment(loc, bc.Map);
        }

        public static AlignmentType GetRegionAlignment(Point3D loc, Map map)
        {
            StaticRegion sr = StaticRegion.FindStaticRegion(loc, map);

            if (sr != null)
                return sr.GuildAlignment;

            return AlignmentType.None;
        }
    }
}