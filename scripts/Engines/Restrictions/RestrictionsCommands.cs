/***************************************************************************
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

/* Scripts/Engines/Restrictions/RestrictionsCommands.cs
 * ChangeLog
 *  11/18/26, Adam (account restrictions)
 *      Created. The staff commands for Restrictions (Administrator). Each has a form with names, not targets, so
 *      UOAdmin (on every shard at once) and harnesses can drive it, and each answers with one line saying exactly what
 *      happened ("Restrictions: added #12 ban: account morgoth, every shard, ..."):
 *          [ban                                     target a player: disconnected, banned here; the gump asks the scope
 *                                                   (this shard, every shard), the duration and the reason
 *          [ban <account|character> [scope] [duration] [reason...]
 *          [unban <account|character> [scope]      scope omitted: every ban on the account
 *          [firewall                                target a player: their address
 *          [firewall add <address> [reason...] | remove <address>
 *          [ipexception <address> <accounts> [scope] [reason...] | remove <address> [scope]
 *          [clientexception <account|address> [scope] [reason...] | remove <account|address> [scope]
 *          [restrictions <account|character|address>   everything that applies, history included
 *          [restrictions report                    what's in force on this shard, by kind
 *      scope: all (every shard), here (this shard), or a shard: AI, SP, MO, SE, UP, MC, LS (or its ServerID).
 *      duration: 30m, 12h, 7d, 2w, or forever. Addresses may be patterns (1.2.3.*).
 *      Replaces the targeted [ban (KickCommand) and [firewall (FirewallCommand) in Commands.cs.
 */

using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Server.Engines
{
    public static class RestrictionsCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("Ban", AccessLevel.Administrator, new CommandEventHandler(Ban_OnCommand));
            CommandSystem.Register("Unban", AccessLevel.Administrator, new CommandEventHandler(Unban_OnCommand));
            CommandSystem.Register("Firewall", AccessLevel.Administrator, new CommandEventHandler(Firewall_OnCommand));
            CommandSystem.Register("IPException", AccessLevel.Administrator, new CommandEventHandler(IPException_OnCommand));
            CommandSystem.Register("ClientException", AccessLevel.Administrator, new CommandEventHandler(ClientException_OnCommand));
            CommandSystem.Register("Restrictions", AccessLevel.Administrator, new CommandEventHandler(Restrictions_OnCommand));
        }

        private static void Reply(Mobile from, string text, params object[] args)
        {
            string line = "Restrictions: " + (args.Length > 0 ? string.Format(text, args) : text);
            from.SendMessage(line);
        }

        #region [ban, [unban
        [Usage("Ban [<account|character> [all|here|AI|SP|..] [30m|12h|7d|2w|forever] [reason...]]")]
        [Description("Bans an account: no arguments, target a player (the gump asks scope, duration and reason); or by name. The default scope is this shard.")]
        private static void Ban_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 0)
            {
                e.Mobile.SendMessage("Restrictions: target the player to ban.");
                e.Mobile.Target = new BanTarget();
                return;
            }
            Account? acct = FindAccount(e.GetString(0), out string? who);
            if (acct == null)
            {
                Reply(e.Mobile, "no account or character '{0}' here.", e.GetString(0));
                return;
            }
            if (acct.AccessLevel >= e.Mobile.AccessLevel)
            {
                Reply(e.Mobile, "you may not ban {0} (their access is {1}).", acct.Username, acct.AccessLevel);
                return;
            }
            int next = 1;
            string scope = Restrictions.ThisShard;
            if (e.Length > next && Restrictions.ParseScope(e.GetString(next)) is string s)
            {
                scope = s;
                next++;
            }
            DateTime? expires = null;
            if (e.Length > next && TryDuration(e.GetString(next), out DateTime? until))
            {
                expires = until;
                next++;
            }
            string reason = string.Join(" ", e.Arguments.Skip(next));
            Restrictions.Row? row = Restrictions.Add(Restrictions.Kind.Ban, acct.Username, scope, 0, reason, e.Mobile.Name, expires);
            if (row == null)
            {
                Reply(e.Mobile, "the accounts database can't be written: {0} is NOT banned.", acct.Username);
                return;
            }
            int kicked = Disconnect(acct, row);
            CommandLogging.WriteLine(e.Mobile, "{0} {1} banning {2}: {3}", e.Mobile.AccessLevel, CommandLogging.Format(e.Mobile), acct.Username, row.Describe());
            Reply(e.Mobile, "added {0}{1}", row.Describe(), kicked > 0 ? string.Format(" ({0} disconnected)", kicked) : "");
        }

        private class BanTarget : Target
        {
            public BanTarget() : base(-1, false, TargetFlags.None) { }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not Mobile targ || targ.Account is not Account acct)
                {
                    from.SendMessage("Restrictions: that is not a player.");
                    return;
                }
                if (targ.AccessLevel >= from.AccessLevel || acct.AccessLevel >= from.AccessLevel)
                {
                    from.SendMessage("Restrictions: you do not have the required access level to do this.");
                    return;
                }
                // banned here at once; the gump then asks the scope, the duration and the reason
                Restrictions.Row? row = Restrictions.Add(Restrictions.Kind.Ban, acct.Username, Restrictions.ThisShard, 0, "", from.Name, null);
                if (row == null)
                {
                    from.SendMessage("Restrictions: the accounts database can't be written: {0} is NOT banned.", acct.Username);
                    return;
                }
                targ.Say("I've been banned!");
                Disconnect(acct, row);
                CommandLogging.WriteLine(from, "{0} {1} banning {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targ));
                from.SendMessage("Restrictions: added {0}. Now choose its scope, duration and reason.", row.Describe());
                from.SendGump(new BanDurationGump(acct));
            }
        }

        [Usage("Unban <account|character> [all|here|AI|SP|..]")]
        [Description("Lifts an account's bans (in one scope, or every one). The rows stay, marked lifted.")]
        private static void Unban_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 0)
            {
                Reply(e.Mobile, "usage: [unban <account|character> [scope]");
                return;
            }
            Account? acct = FindAccount(e.GetString(0), out _);
            string name = acct?.Username ?? e.GetString(0);
            string? scope = e.Length > 1 ? Restrictions.ParseScope(e.GetString(1)) : null;
            if (e.Length > 1 && scope == null)
            {
                Reply(e.Mobile, "unknown scope '{0}' (all, here, AI, SP, MO, SE, UP, MC, LS).", e.GetString(1));
                return;
            }
            int n = Restrictions.Lift(Restrictions.Kind.Ban, name, scope, e.Mobile.Name);
            CommandLogging.WriteLine(e.Mobile, "{0} {1} unbanning {2} ({3})", e.Mobile.AccessLevel, CommandLogging.Format(e.Mobile), name, scope ?? "every scope");
            Reply(e.Mobile, n < 0 ? "the accounts database can't be written: nothing lifted."
                : n == 0 ? "no ban in force on {0}{1}." : "lifted {2} ban(s) on {0}{1}.", name, scope == null ? "" : " (" + scope + ")", n);
        }
        #endregion

        #region [firewall
        [Usage("Firewall [add <address> [reason...] | remove <address>]")]
        [Description("Blocks an address on every shard: no arguments, target a player (their address); or by address.")]
        private static void Firewall_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 0)
            {
                e.Mobile.SendMessage("Restrictions: target the player whose address to firewall.");
                e.Mobile.Target = new FirewallTarget();
                return;
            }
            string verb = e.GetString(0).ToLowerInvariant();
            if ((verb == "add" || verb == "remove") && e.Length > 1 && Restrictions.LooksLikeAddress(e.GetString(1)))
            {
                string ip = e.GetString(1);
                if (verb == "add")
                {
                    Restrictions.Row? row = Restrictions.Add(Restrictions.Kind.Firewall, ip, Restrictions.All, 0, string.Join(" ", e.Arguments.Skip(2)), e.Mobile.Name, null);
                    Reply(e.Mobile, row == null ? "the accounts database can't be written: nothing added." : "added " + row.Describe());
                }
                else
                {
                    int n = Restrictions.Lift(Restrictions.Kind.Firewall, ip, null, e.Mobile.Name);
                    Reply(e.Mobile, n < 0 ? "the accounts database can't be written." : n == 0 ? "{0} isn't firewalled." : "lifted the firewall on {0}.", ip);
                }
                return;
            }
            Reply(e.Mobile, "usage: [firewall (target) | [firewall add <address> [reason] | [firewall remove <address>");
        }

        private class FirewallTarget : Target
        {
            public FirewallTarget() : base(-1, false, TargetFlags.None) { }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not Mobile targ || targ.NetState == null)
                {
                    from.SendMessage("Restrictions: that player isn't online (firewall an address with [firewall add <address>).");
                    return;
                }
                string ip = targ.NetState.Address.ToString();
                Restrictions.Row? row = Restrictions.Add(Restrictions.Kind.Firewall, ip, Restrictions.All, 0, "", from.Name, null);
                CommandLogging.WriteLine(from, "{0} {1} firewalling {2}", from.AccessLevel, CommandLogging.Format(from), CommandLogging.Format(targ));
                from.SendMessage(row == null ? "Restrictions: the accounts database can't be written: nothing added." : "Restrictions: added " + row.Describe() + " (this does not ban or kick).");
            }
        }
        #endregion

        #region [ipexception, [clientexception
        [Usage("IPException <address> <accounts> [all|here|AI|..] [reason...] | remove <address> [scope]")]
        [Description("Lets an address have more accounts than the shard's limit (in one scope; the default is this shard).")]
        private static void IPException_OnCommand(CommandEventArgs e)
        {
            if (e.Length >= 2 && e.GetString(0).Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                string? scope = e.Length > 2 ? Restrictions.ParseScope(e.GetString(2)) : null;
                int n = Restrictions.Lift(Restrictions.Kind.IpException, e.GetString(1), scope, e.Mobile.Name);
                Reply(e.Mobile, n < 0 ? "the accounts database can't be written." : n == 0 ? "no IP exception in force on {0}." : "lifted {1} IP exception(s) on {0}.", e.GetString(1), n);
                return;
            }
            if (e.Length >= 2 && Restrictions.LooksLikeAddress(e.GetString(0)) && int.TryParse(e.GetString(1), out int limit) && limit > 0)
            {
                int next = 2;
                string scope = Restrictions.ThisShard;
                if (e.Length > next && Restrictions.ParseScope(e.GetString(next)) is string s)
                {
                    scope = s;
                    next++;
                }
                Restrictions.Row? row = Restrictions.Add(Restrictions.Kind.IpException, e.GetString(0), scope, limit, string.Join(" ", e.Arguments.Skip(next)), e.Mobile.Name, null);
                Reply(e.Mobile, row == null ? "the accounts database can't be written: nothing added." : "added " + row.Describe());
                return;
            }
            Reply(e.Mobile, "usage: [ipexception <address> <accounts> [scope] [reason] | [ipexception remove <address> [scope]");
        }

        [Usage("ClientException <account|address> [all|here|AI|..] [reason...] | remove <account|address> [scope]")]
        [Description("Excuses an account (or address) from the ClassicUO/Razor code tests (in one scope; the default is every shard).")]
        private static void ClientException_OnCommand(CommandEventArgs e)
        {
            if (e.Length >= 2 && e.GetString(0).Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                string subject = Subject(e.GetString(1));
                string? scope = e.Length > 2 ? Restrictions.ParseScope(e.GetString(2)) : null;
                int n = Restrictions.Lift(Restrictions.Kind.ClientException, subject, scope, e.Mobile.Name);
                Reply(e.Mobile, n < 0 ? "the accounts database can't be written." : n == 0 ? "no client exception in force on {0}." : "lifted {1} client exception(s) on {0}.", subject, n);
                return;
            }
            if (e.Length >= 1)
            {
                string subject = Subject(e.GetString(0));
                int next = 1;
                string scope = Restrictions.All;
                if (e.Length > next && Restrictions.ParseScope(e.GetString(next)) is string s)
                {
                    scope = s;
                    next++;
                }
                Restrictions.Row? row = Restrictions.Add(Restrictions.Kind.ClientException, subject, scope, 0, string.Join(" ", e.Arguments.Skip(next)), e.Mobile.Name, null);
                Reply(e.Mobile, row == null ? "the accounts database can't be written: nothing added." : "added " + row.Describe());
                return;
            }
            Reply(e.Mobile, "usage: [clientexception <account|address> [scope] [reason] | [clientexception remove <account|address> [scope]");
        }
        #endregion

        #region [restrictions
        [Usage("Restrictions <account|character|address> | report")]
        [Description("Everything that applies to an account or an address (bans, firewall, IP and client exceptions; history included), or what's in force on this shard.")]
        private static void Restrictions_OnCommand(CommandEventArgs e)
        {
            if (e.Length == 0)
            {
                Reply(e.Mobile, "usage: [restrictions <account|character|address> | [restrictions report");
                return;
            }
            if (e.GetString(0).Equals("report", StringComparison.OrdinalIgnoreCase))
            {
                Reply(e.Mobile, "{0}: {1}.", Core.Server, Restrictions.Summary());
                return;
            }
            string arg = e.GetString(0);
            List<Restrictions.Row> rows = new List<Restrictions.Row>();
            string what;
            if (Restrictions.LooksLikeAddress(arg))
            {
                what = "address " + arg;
                rows.AddRange(Restrictions.About(arg));
            }
            else
            {
                Account? acct = FindAccount(arg, out _);
                string name = acct?.Username ?? arg;
                what = "account " + name;
                rows.AddRange(Restrictions.About(name));
                // the addresses the account has logged in from here: anything firewalled or excepted on them
                if (acct != null)
                    foreach (IPAddress ip in acct.LoginIPs ?? Array.Empty<IPAddress>())
                        foreach (Restrictions.Row r in Restrictions.About(ip.ToString()))
                            if (!rows.Any(x => x.Id == r.Id))
                                rows.Add(r);
            }
            if (rows.Count == 0)
            {
                Reply(e.Mobile, "nothing on {0}.", what);
                return;
            }
            Reply(e.Mobile, "{0}: {1} row(s), {2} in force here:", what, rows.Count,
                rows.Count(r => r.InForce(DateTime.UtcNow) && r.Applies(Restrictions.ThisShard)));
            foreach (Restrictions.Row r in rows.OrderBy(r => r.Id))
                e.Mobile.SendMessage(r.InForce(DateTime.UtcNow) ? 0x35 : 0x3B2, "  " + r.Describe());
        }
        #endregion

        #region Helpers
        /// <summary>An account by its name, or by one of its characters' names on this shard.</summary>
        public static Account? FindAccount(string name, out string? character)
        {
            character = null;
            if (Accounts.GetAccount(name) is Account a)
                return a;
            PlayerMobile? pm = PlayerMobile.Instances.FirstOrDefault(p => p != null && !p.Deleted && p.Name != null && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            character = pm?.Name;
            return pm?.Account as Account;
        }

        // a client exception's subject: an address as given, else the account (by name or character)
        private static string Subject(string arg) =>
            Restrictions.LooksLikeAddress(arg) ? arg : (FindAccount(arg, out _)?.Username ?? arg);

        /// <summary>30m, 12h, 7d, 2w, or forever (null: never expires).</summary>
        public static bool TryDuration(string text, out DateTime? until)
        {
            until = null;
            string t = text.Trim().ToLowerInvariant();
            if (t == "forever" || t == "infinite" || t == "permanent")
                return true;
            if (t.Length < 2 || !double.TryParse(t.Substring(0, t.Length - 1), out double n) || n <= 0)
                return false;
            TimeSpan span;
            switch (t[^1])
            {
                case 's': span = TimeSpan.FromSeconds(n); break;
                case 'm': span = TimeSpan.FromMinutes(n); break;
                case 'h': span = TimeSpan.FromHours(n); break;
                case 'd': span = TimeSpan.FromDays(n); break;
                case 'w': span = TimeSpan.FromDays(7 * n); break;
                default: return false;
            }
            until = DateTime.UtcNow + span;
            return true;
        }

        // the account's characters online here: disconnected (a ban that applies here)
        private static int Disconnect(Account acct, Restrictions.Row row)
        {
            if (!row.Applies(Restrictions.ThisShard))
                return 0;
            int n = 0;
            foreach (NetState ns in NetState.Instances.ToArray())
                if (ns?.Account == acct)
                {
                    ns.Dispose();
                    n++;
                }
            return n;
        }
        #endregion
    }
}
