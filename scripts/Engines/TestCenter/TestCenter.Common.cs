
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

/* Scripts\Engines\TestCenter\TestCenter.Common.cs
 * CHANGELOG:
 * 4/16/2026, Adam
 *      Introduced unified Test Center command registry, replacing legacy speech handling.
 *      Supports exact and prefix commands, shared across all Test Center variants.
 *      Added data-driven Help gump and LocationTree-integrated Go command.
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

namespace Server.Engines
{
    /// <summary>
    /// Shared Test Center command system used by Server-wide, Facet, and Region variants.
    /// </summary>
    public static class TestCenterCommands
    {
        public delegate bool TestCenterCheck(Mobile m);
        // ===================================================================
        // Test Center Command Registry (new - replaces the old mess)
        // ===================================================================
        public enum CommandMatchType
        {
            Exact,      // must match the entire speech exactly (tamepet, bondpet, toggle whiff)
            StartsWith  // command is a prefix (set, petset, go)
        }

        public class TestCenterCommand
        {
            public string Name { get; }
            public string[] Aliases { get; }
            public string Description { get; }
            public string Usage { get; }
            public Action<SpeechEventArgs> Handler { get; }
            public CommandMatchType MatchType { get; }

            public TestCenterCommand(string name, string description, string usage,
                Action<SpeechEventArgs> handler,
                CommandMatchType matchType = CommandMatchType.StartsWith,
                params string[] aliases)
            {
                Name = name.ToLowerInvariant();
                Description = description;
                Usage = usage;
                Handler = handler;
                MatchType = matchType;
                Aliases = aliases.Select(a => a.ToLowerInvariant()).ToArray();
            }
        }

        public static class TestCenterCommandRegistry
        {
            private static readonly Dictionary<string, TestCenterCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
            private static readonly List<TestCenterCommand> _orderedCommands = new();

            public static void Register(TestCenterCommand command)
            {
                _commands[command.Name] = command;
                foreach (var alias in command.Aliases)
                    _commands[alias] = command;

                _orderedCommands.Add(command);
            }

            /// <summary>
            /// Called from EventSink_Speech. Returns true if a TC command was handled.
            /// </summary>
            public static bool TryExecute(SpeechEventArgs args)
            {
                string lowerSpeech = args.Speech.Trim().ToLowerInvariant();

                // 1. Exact match first (higher priority)
                if (_commands.TryGetValue(lowerSpeech, out TestCenterCommand? cmd) &&
                    cmd.MatchType == CommandMatchType.Exact)
                {
                    cmd.Handler(args);
                    args.Handled = true;
                    return true;
                }

                // 2. Prefix match (longest match wins)
                TestCenterCommand? best = null;
                int bestLength = 0;

                foreach (var c in _orderedCommands)
                {
                    if (c.MatchType == CommandMatchType.StartsWith)
                    {
                        string command = c.Name;

                        // Only trigger if it's exactly the command OR command + space
                        if (lowerSpeech == command ||
                            lowerSpeech.StartsWith(command + " ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (command.Length > bestLength)
                            {
                                best = c;
                                bestLength = command.Length;
                            }
                        }
                    }
                }

                if (best != null)
                {
                    best.Handler(args);
                    args.Handled = true;
                    return true;
                }

                return false;
            }

            public static IReadOnlyList<TestCenterCommand> AllCommands => _orderedCommands.AsReadOnly();
        }

        // ===================================================================
        // TCSetSkills - All command logic (now using registry)
        // ===================================================================
        public static class TCSetSkills
        {
            #region Target Classes

            public class TamePetTarget : Target
            {
                public TamePetTarget() : base(11, false, TargetFlags.None) { }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    if (targeted is BaseCreature pet)
                    {
                        if (pet.ControlMaster != null)
                        {
                            from.SendMessage("That creature is already tame.");
                            return;
                        }
                        if (!pet.Tamable)
                        {
                            from.SendMessage("that creature cannot be tamed.");
                            return;
                        }
                        pet.ControlMaster = from;
                        pet.Controlled = true;
                        from.SendMessage(string.Format("That creature had no choice but to accept you as {0} master.", pet.Female ? "her" : "his"));
                    }
                }
            }

            public class PetSetTarget : Target
            {
                private readonly SpeechEventArgs m_Args;

                public PetSetTarget(SpeechEventArgs args) : base(11, false, TargetFlags.None)
                {
                    m_Args = args;
                }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    if (targeted is BaseCreature pet && pet.ControlMaster == from)
                    {
                        string[] split = m_Args.Speech.Split(' ');
                        if (split.Length != 3) return;

                        if (!double.TryParse(split[2], out double value))
                        {
                            from.SendMessage($"'{split[2]}' is not a valid value.");
                            return;
                        }

                        string name = split[1];

                        if (Insensitive.Equals(name, "str"))
                            ChangeStrength(from, pet, (int)value);
                        else if (Insensitive.Equals(name, "dex"))
                            ChangeDexterity(from, pet, (int)value);
                        else if (Insensitive.Equals(name, "int"))
                            ChangeIntelligence(from, pet, (int)value);
                        else
                            ChangeSkill(from, pet, name, value);
                    }
                    else
                    {
                        from.SendMessage("That's not your pet!");
                    }
                }


            }

            private class BondPetTarget : Target
            {
                public BondPetTarget() : base(-1, false, TargetFlags.None) { }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    if (targeted is BaseCreature pet && pet.ControlMaster == from)
                    {
                        if (pet.IsBonded)
                        {
                            from.SendMessage("That pet has already bonded with you.");
                            return;
                        }
                        if (!pet.Tamable || !pet.IsBondable)
                        {
                            from.SendMessage("You cannot bond with that.");
                            return;
                        }

                        pet.IsBonded = true;
                        pet.BondingBegin = DateTime.MinValue;
                        from.SendLocalizedMessage(1049666); // Your pet has bonded with you!
                    }
                    else
                        from.SendMessage("That's not your pet!");
                }
            }

            #endregion Target Classes

            public static void EventSink_Speech(SpeechEventArgs args, TestCenterCommands.TestCenterCheck isAllowed)
            {
                try
                {
                    if (!args.Handled && isAllowed(args.Mobile))
                    {
                        TestCenterCommands.TestCenterCommandRegistry.TryExecute(args);
                    }
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }

            // ===================================================================
            // Register ALL Test Center Commands
            // ===================================================================
            public static void Initialize()
            {
                // ===================== EXACT MATCH COMMANDS =====================
                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "tamepet",
                    "Instantly tame a targeted untamed creature.",
                    "tamepet",
                    args =>
                    {
                        args.Mobile.Target = new TestCenterCommands.TCSetSkills.TamePetTarget();
                        args.Mobile.SendMessage("Target an animal to tame.");
                    },
                    TestCenterCommands.CommandMatchType.Exact));

                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "bondpet",
                    "Instantly bond with a targeted pet.",
                    "bondpet",
                    args =>
                    {
                        args.Mobile.Target = new BondPetTarget();
                        args.Mobile.SendMessage("Target your pet.");
                    },
                    TestCenterCommands.CommandMatchType.Exact));

                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "toggle whiff",
                    "Toggle weapon whiff tracking on/off.",
                    "toggle whiff",
                    args =>
                    {
                        Core.HITMISS = (Core.HITMISS > 0 ? 0 : 1);
                        args.Mobile.SendMessage("Weapon whiff tracking: {0}", (Core.HITMISS > 0) ? "on" : "off");
                    },
                    TestCenterCommands.CommandMatchType.Exact));

                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "toggle damage",
                    "Toggle weapon damage tracking on/off.",
                    "toggle damage",
                    args =>
                    {
                        Core.DAMAGE = (Core.DAMAGE > 0 ? 0 : 1);
                        args.Mobile.SendMessage("Weapon damage tracking: {0}", (Core.DAMAGE > 0) ? "on" : "off");
                    },
                    TestCenterCommands.CommandMatchType.Exact));

                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "reset whiff",
                    "Reset weapon whiff tracking.",
                    "reset whiff",
                    args =>
                    {
                        Core.HITMISS = 2;
                        args.Mobile.SendMessage("Weapon whiff tracking reset.");
                    },
                    TestCenterCommands.CommandMatchType.Exact));

                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "reset damage",
                    "Reset weapon damage tracking.",
                    "reset damage",
                    args =>
                    {
                        Core.DAMAGE = 2;
                        args.Mobile.SendMessage("Weapon damage tracking reset.");
                    },
                    TestCenterCommands.CommandMatchType.Exact));

                // ===================== PREFIX COMMANDS =====================

                // go <location>
                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "go",
                    "Teleport to a named location (or open Go menu if no name given).",
                    "go [location name]",
                    args =>
                    {
                        string name = args.Speech.Length > 3
                            ? args.Speech.Substring(3).Trim()
                            : "";

                        CommandHandlers.ExecuteGo(args.Mobile, name);
                    },
                    TestCenterCommands.CommandMatchType.StartsWith));

                // petset <stat> <value>
                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "petset",
                    "Modify a pet's stats or skills.",
                    "petset <str|dex|int|skillname> <value>",
                    args =>
                    {
                        if (args.Speech.Split(' ').Length == 3)
                        {
                            args.Mobile.Target = new PetSetTarget(args);
                            args.Mobile.SendMessage("Target your pet.");
                        }
                    },
                    TestCenterCommands.CommandMatchType.StartsWith));

                // set <stat|skill|seasonal points> <value>
                TestCenterCommands.TestCenterCommandRegistry.Register(new TestCenterCommands.TestCenterCommand(
                    "set",
                    "Modify player stats, skills, or seasonal points.",
                    "set <str|dex|int|skillname|seasonal points> <value>",
                    args =>
                    {
                        Mobile from = args.Mobile;
                        string[] split = args.Speech.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (split.Length > 2)
                        {
                            string name = string.Join(" ", split, 1, split.Length - 2);
                            string lastToken = split[split.Length - 1];

                            // Seasonal points special case
                            if (Insensitive.Equals(name, "seasonal points") || Insensitive.Equals(name, "seasonalpoints"))
                            {
                                if (int.TryParse(lastToken, out int points) && points >= 0)
                                {
                                    SeasonalAccount acc = SeasonalAccounts.GetAccount(from);
                                    if (acc != null)
                                    {
                                        acc.SeasonPoints = 0;
                                        acc.SeasonSpentPoints = 0;
                                        acc.LifetimeEarnedPoints = points;
                                        from.SendMessage($"Seasonal points set to {points}.");
                                    }
                                    else
                                        from.SendMessage("Could not find a seasonal account for this character.");
                                }
                                else
                                    from.SendMessage("Invalid value specified for seasonal points.");
                                return;
                            }

                            if (!double.TryParse(lastToken, out double value))
                            {
                                from.SendMessage("Invalid value specified.");
                                return;
                            }

                            if (Insensitive.Equals(name, "str"))
                                ChangeStrength(from, (int)value);
                            else if (Insensitive.Equals(name, "dex"))
                                ChangeDexterity(from, (int)value);
                            else if (Insensitive.Equals(name, "int"))
                                ChangeIntelligence(from, (int)value);
                            else if (Insensitive.Equals(name, "lifeforce"))
                            {
                                Ethics.Player ethicPlayer = Ethics.Player.Find(from);
                                if (ethicPlayer == null)
                                    from.SendMessage("You are not hero/evil aligned.");
                                else if (value < 0 || value > 100)
                                    from.SendMessage("Invalid value specified.");
                                else if (ethicPlayer.Power == (int)value)
                                    from.SendMessage("Your life force has not changed.");
                                else
                                {
                                    ethicPlayer.Power = (int)value;
                                    from.SendMessage("Your life force has changed.");
                                }
                            }
                            else
                            {
                                if (Insensitive.StartsWith(name, "skill") && name.Length > 5)
                                    name = name.Substring(5);
                                ChangeSkill(from, name, value);
                            }
                        }
                    },
                    TestCenterCommands.CommandMatchType.StartsWith));
            }

            // ChangeStrength, ChangeDexterity, etc. methods 
            private static void ChangeStrength(Mobile from, BaseCreature pet, int value)
            {
                if (value < 10 || value > pet.StrMax)
                    from.SendMessage("Your pet's strength ranges between 10 and {0}.", pet.StrMax);
                else
                {
                    if (pet is Dragon)
                    {
                        pet.RawStr = value;
                        pet.ValidateStatCap(Stat.Str);
                        from.SendMessage("Your pet's stats have been adjusted.");
                    }
                    else
                    {
                        if ((value + pet.RawDex + pet.RawInt) > pet.StatCap)
                            from.SendMessage("Your pet's statcap is {0}. Try setting another stat lower first.", pet.StatCap);
                        else
                        {
                            pet.RawStr = value;
                            from.SendMessage("Your pet's stats have been adjusted.");
                        }
                    }
                }
            }

            private static void ChangeDexterity(Mobile from, BaseCreature pet, int value)
            {
                if (value < 10 || value > pet.DexMax)
                    from.SendMessage("Your pet's dexterity ranges between 10 and {0}.", pet.DexMax);
                else
                {
                    if (pet is Dragon)
                    {
                        pet.RawDex = value;
                        pet.ValidateStatCap(Stat.Dex);
                        from.SendMessage("Your pet's stats have been adjusted.");
                    }
                    else
                    {
                        if ((value + pet.RawStr + pet.RawInt) > pet.StatCap)
                            from.SendMessage("Your pet's statcap is {0}. Try setting another stat lower first.", pet.StatCap);
                        else
                        {
                            pet.RawDex = value;
                            from.SendMessage("Your pet's stats have been adjusted.");
                        }
                    }
                }
            }

            private static void ChangeIntelligence(Mobile from, BaseCreature pet, int value)
            {
                if (value < 10 || value > pet.IntMax)
                    from.SendMessage("Your pet's intelligence ranges between 10 and {0}.", pet.IntMax);
                else
                {
                    if (pet is Dragon)
                    {
                        pet.RawInt = value;
                        pet.ValidateStatCap(Stat.Int);
                        from.SendMessage("Your pet's stats have been adjusted.");
                    }
                    else
                    {
                        if ((value + pet.RawDex + pet.RawStr) > pet.StatCap)
                            from.SendMessage("Your pet's statcap is {0}. Try setting another stat lower first.", pet.StatCap);
                        else
                        {
                            pet.RawInt = value;
                            from.SendMessage("Your pet's stats have been adjusted.");
                        }
                    }
                }
            }

            private static void ChangeSkill(Mobile from, BaseCreature pet, string name, double value)
            {
                SkillName index;
                try
                {
                    index = (SkillName)Enum.Parse(typeof(SkillName), name, true);
                }
                catch
                {
                    from.SendLocalizedMessage(1005631);
                    return;
                }

                Skill skill = pet.Skills[index];
                if (skill != null)
                {
                    if (value < 0 || value > skill.Cap)
                        from.SendMessage("Your pet's skill in {0} is capped at {1:F1}.", skill.Info.Name, skill.Cap);
                    else
                    {
                        int newFixedPoint = (int)(value * 10.0);
                        int oldFixedPoint = skill.BaseFixedPoint;

                        if (((skill.Owner.Total - oldFixedPoint) + newFixedPoint) > skill.Owner.Cap)
                            from.SendMessage("You can not exceed the skill cap. Try setting another skill lower first.");
                        else
                        {
                            skill.BaseFixedPoint = newFixedPoint;
                            from.SendMessage("Your pet's skill has been adjusted.");
                        }
                    }
                }
                else
                    from.SendLocalizedMessage(1005631);
            }
            // Player stat/skill change methods
            private static void ChangeStrength(Mobile from, int value)
            {
                if (value < 10 || value > 125)
                    from.SendLocalizedMessage(1005628);
                else if ((value + from.RawDex + from.RawInt) > from.StatCap)
                    from.SendLocalizedMessage(1005629);
                else
                {
                    from.RawStr = value;
                    from.SendLocalizedMessage(1005630);
                }
            }

            private static void ChangeDexterity(Mobile from, int value)
            {
                if (value < 10 || value > 125)
                    from.SendLocalizedMessage(1005628);
                else if ((from.RawStr + value + from.RawInt) > from.StatCap)
                    from.SendLocalizedMessage(1005629);
                else
                {
                    from.RawDex = value;
                    from.SendLocalizedMessage(1005630);
                }
            }

            private static void ChangeIntelligence(Mobile from, int value)
            {
                if (value < 10 || value > 125)
                    from.SendLocalizedMessage(1005628);
                else if ((from.RawStr + from.RawDex + value) > from.StatCap)
                    from.SendLocalizedMessage(1005629);
                else
                {
                    from.RawInt = value;
                    from.SendLocalizedMessage(1005630);
                }
            }

            private static void ChangeSkill(Mobile from, string name, double value)
            {
                SkillName index;
                try
                {
                    int score;
                    string real_name = IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), name, out score);
                    if (score > 5)
                    {
                        from.SendLocalizedMessage(1005631);
                        return;
                    }
                    index = (SkillName)Enum.Parse(typeof(SkillName), real_name, true);
                }
                catch
                {
                    from.SendLocalizedMessage(1005631);
                    return;
                }

                Skill skill = from.Skills[index];
                if (skill != null)
                {
                    if (value < 0 || value > skill.Cap)
                        from.SendMessage("Your skill in {0} is capped at {1:F1}.", skill.Info.Name, skill.Cap);
                    else
                    {
                        int newFixedPoint = (int)(value * 10.0);
                        int oldFixedPoint = skill.BaseFixedPoint;

                        if (((skill.Owner.Total - oldFixedPoint) + newFixedPoint) > skill.Owner.Cap)
                            from.SendMessage("You can not exceed the skill cap. Try setting another skill lower first.");
                        else
                            skill.BaseFixedPoint = newFixedPoint;
                    }
                }
                else
                    from.SendLocalizedMessage(1005631);
            }
        }

        // ===================================================================
        // Shared Help Gump - Normal Commands now inside the gump
        // ===================================================================
        public class TCHelpGump : Gump
        {
            private const int EntriesPerPage = 5;

            private readonly Mobile _mobile;
            private readonly int _tab;           // 0 = Main, 1 = TC, 2 = Normal
            private readonly int _tcPage;
            private readonly int _normalPage;

            public static void EventSink_Speech(SpeechEventArgs args, TestCenterCheck isAllowed)
            {
                if (!args.Handled && isAllowed(args.Mobile) && Insensitive.Equals(args.Speech, "help"))
                {
                    args.Mobile.SendGump(new TCHelpGump(args.Mobile, 0, 0, 0));
                    args.Handled = true;
                }
            }

            public TCHelpGump(Mobile mobile, int tab, int tcPage, int normalPage) : base(50, 50)
            {
                _mobile = mobile;
                _tab = tab;
                _tcPage = Math.Max(0, tcPage);
                _normalPage = Math.Max(0, normalPage);

                AddPage(0);
                AddBackground(0, 0, 620, 460, 5054);
                AddBackground(10, 10, 600, 440, 3000);

                // Title
                AddHtml(20, 20, 580, 40, "<center><big>Test Center Help</big></center>", false, false);

                // Tabs
                AddButton(30, 70, _tab == 0 ? 0xFB0 : 0xFAE, _tab == 0 ? 0xFB0 : 0xFAF, 100, GumpButtonType.Reply, 0);
                AddLabel(65, 70, 0x34, "Main Menu");

                AddButton(180, 70, _tab == 1 ? 0xFB0 : 0xFAE, _tab == 1 ? 0xFB0 : 0xFAF, 101, GumpButtonType.Reply, 0);
                AddLabel(215, 70, 0x34, "Test Center Commands");

                AddButton(380, 70, _tab == 2 ? 0xFB0 : 0xFAE, _tab == 2 ? 0xFB0 : 0xFAF, 102, GumpButtonType.Reply, 0);
                AddLabel(415, 70, 0x34, "Normal Commands");

                // Build content
                if (_tab == 0)
                    BuildMainPage();
                else if (_tab == 1)
                    BuildTestCenterCommandsPage(_tcPage);
                else if (_tab == 2)
                    BuildNormalCommandsPage(_normalPage);
            }

            private void BuildTestCenterCommandsPage(int currentPage)
            {
                var commands = TestCenterCommandRegistry.AllCommands;
                int totalPages = (commands.Count + EntriesPerPage - 1) / EntriesPerPage;
                currentPage = Math.Max(0, Math.Min(currentPage, totalPages - 1));

                int start = currentPage * EntriesPerPage;

                AddHtml(40, 110, 200, 25, "Test Center Commands", false, false);

                int y = 140;
                for (int i = start; i < commands.Count && i < start + EntriesPerPage; i++)
                {
                    var cmd = commands[i];
                    string text = $"{cmd.Name} - {cmd.Description}";
                    if (!string.IsNullOrEmpty(cmd.Usage))
                        text += $"<br><i>Usage: {cmd.Usage}</i>";

                    AddHtml(40, y, 540, 48, text, false, false);
                    y += 50;
                }

                AddBottomBar(currentPage, totalPages, isNormal: false);
            }

            private void BuildNormalCommandsPage(int currentPage)
            {
                var visible = new List<CommandEntry>();

                foreach (CommandEntry entry in CommandSystem.Entries.Values)
                {
                    if (entry != null && _mobile.AccessLevel >= entry.AccessLevel)
                        visible.Add(entry);
                }

                visible.Sort((a, b) => string.Compare(a.Command, b.Command, StringComparison.OrdinalIgnoreCase));

                int totalPages = (visible.Count + EntriesPerPage - 1) / EntriesPerPage;
                currentPage = Math.Max(0, Math.Min(currentPage, totalPages - 1));

                int start = currentPage * EntriesPerPage;

                //AddHtml(40, 110, 200, 25, "Normal Commands", false, false);
                AddHtml(40, 110, 200, 25, "Normal Commands", false, false);
                AddHtml(40, 135, 540, 25,
                    $"<i>Prefix: {CommandSystem.CommandPrefix}   •   Total: {visible.Count} commands</i>",
                    false, false);

                int y = 165;
                for (int i = start; i < visible.Count && i < start + EntriesPerPage; i++)
                {
                    var cmd = visible[i];

                    // Simple, clean display since we don't have descriptions
                    //string text = $"{CommandSystem.CommandPrefix}{cmd.Command}";
                    string text = $"{CommandSystem.CommandPrefix}{cmd.Command}";

                    AddHtml(40, y, 540, 40, text, false, false);
                    y += 42;          // slightly tighter than TC commands
                }

                AddBottomBar(currentPage, totalPages, isNormal: true);
            }

            private void AddBottomBar(int currentPage, int totalPages, bool isNormal)
            {
                AddButton(30, 415, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
                AddLabel(65, 415, 0x34, "Close");

                if (totalPages > 1)
                {
                    if (currentPage > 0)
                    {
                        AddButton(220, 415, 0xFAE, 0xFAF, (isNormal ? 300 : 200) + currentPage - 1, GumpButtonType.Reply, 0);
                        AddLabel(255, 415, 0x34, "Previous");
                    }

                    AddLabel(360, 415, 0x34, $"Page {currentPage + 1} of {totalPages}");

                    if (currentPage < totalPages - 1)
                    {
                        AddButton(480, 415, 0xFAF, 0xFB0, (isNormal ? 300 : 200) + currentPage + 1, GumpButtonType.Reply, 0);
                        AddLabel(515, 415, 0x34, "Next");
                    }
                }
                else
                {
                    AddLabel(320, 415, 0x34, "End of list");
                }
            }
            private void BuildMainPage()
            {
                const string content =
                    "<center><big>Getting Around Test Center</big></center><br>" +

                    "The primary way to travel around Test Center is with the go command.<br><br>" +

                    "Examples:<br>" +
                    "• go west brit bank<br>" +
                    "• go animal pen<br>" +
                    "• go the saw horse<br>" +
                    "• go dinky<br>" +
                    "• go 5123 1775<br>" +
                    "• go 45 12 N 72 55 W<br><br>" +

                    "The go command supports:<br>" +
                    "• Named locations from the Go menu<br>" +
                    "• World sign locations<br>" +
                    "• Rune and runebook marks<br>" +
                    "• Player names<br>" +
                    "• Coordinates<br>" +
                    "• Sextant coordinates<br><br>" +

                    "Use the tabs above to browse all available Test Center and normal commands.";

                AddHtml(40, 110, 540, 280, "<basefont color=\"#000000\">" + content, false, true);
            }
            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile m = sender.Mobile;

                switch (info.ButtonID)
                {
                    case 0: // Close
                        return;

                    case 100: // Main Menu
                        m.SendGump(new TCHelpGump(m, 0, 0, 0));
                        break;

                    case 101: // Test Center Commands
                        m.SendGump(new TCHelpGump(m, 1, 0, 0));
                        break;

                    case 102: // Normal Commands
                        m.SendGump(new TCHelpGump(m, 2, 0, 0));
                        break;

                    // Pagination
                    default:
                        if (info.ButtonID >= 300) // Normal Commands
                        {
                            int page = info.ButtonID - 300;
                            m.SendGump(new TCHelpGump(m, 2, 0, page));
                        }
                        else if (info.ButtonID >= 200) // TC Commands
                        {
                            int page = info.ButtonID - 200;
                            m.SendGump(new TCHelpGump(m, 1, page, 0));
                        }
                        break;
                }
            }
        }

    }
}
