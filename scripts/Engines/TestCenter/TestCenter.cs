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

/* Scripts/Misc/TestCenter.cs
 * CHANGELOG
 *	3/27/23, Yoar
 *	    Added bondpet
 *	6/30/21, Adam
 *		Add additional parsing an reverse lookup (Levenshtein.BestEnumMatch) to allow users to set
 *		set tasteid 100
 *		set taste identification 100
 *		set tasteidentification 100
 *	4/10/10, adam
 *		Add tamepet command
 *	5/12/08, Adam
 *		Switch from Convert.ToDouble() to double.TryParse()so that we could handle the error
 *	2/19/08, Adam
 *		Remove MaxAccountsPerIP setting for test center since it is now in the Core Command Console
 *  10/5/07, Adam
 *      Add  special ServerWars() function to turn on TEST CENTER for server wars on prod
 *  1/09/07 Taran Kain
 *      Added "petset"
 *	9/15/05, Adam
 *		set AccountHandler.MaxAccountsPerIP = 5 when test center is enabled
 *	9/14/05, Adam
 *		Cleaned up Test Center message that get written to the console
 */

using Server.Diagnostics;
using Server.Engines;
using Server.Gumps;
using Server.Network;
using System;
using System.Text;

namespace Server.Misc
{
    public class TestCenter
    {
        private static bool m_Enabled = false;
        private static bool m_Init = false;

        public static bool Enabled
        {
            get
            {
                if (m_Init == false)
                {
                    try { throw new ApplicationException("You cannot call TestCenter.Enabled from Configure()"); }
                    catch (Exception ex) { LogHelper.LogException(ex); }
                }

                return m_Enabled;
            }
        }

        public static void Configure()
        {
            if (Core.UOTC_CFG == true)
            {
                m_Enabled = true;
            }
            else
                m_Enabled = false;

            // indicated we have been initializied.
            //	TestCenter.Enabled will throw an exception (and caught) if it is called
            //	before TestCenter is Configured()
            m_Init = true;
        }

        public static void Initialize()
        {
            if (Enabled)
            {
                EventSink.Speech += new SpeechEventHandler(TCHelpGump.EventSink_Speech);
                EventSink.Speech += new SpeechEventHandler(TCSetSkills.EventSink_Speech);
            }
        }

        public static bool ServerWars()
        {
            // be very, very clear about our intent here. 
            // Make no mistakes, and make no assumptions
            if (Enabled == false)
            {
                // be SURE we are not going to save
                if (Server.Misc.AutoRestart.ServerWars == true)
                {
                    m_Enabled = true;
                    m_Init = true;
                    EventSink.Speech += new SpeechEventHandler(TCHelpGump.EventSink_Speech);
                    EventSink.Speech += new SpeechEventHandler(TCSetSkills.EventSink_Speech);
                    return true;
                }
            }

            return false;
        }

        public class TCSetSkills
        {
            public class TamePetTarget : Server.Targeting.Target
            {
                public TamePetTarget()
                    : base(11, false, Server.Targeting.TargetFlags.None)
                {

                }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    Server.Mobiles.BaseCreature pet = targeted as Server.Mobiles.BaseCreature;
                    if (pet != null)
                    {
                        if (pet.ControlMaster != null)
                        {
                            from.SendMessage("That creature is already tame.");
                            return;
                        }
                        if (pet.Tamable == false)
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

            public class PetSetTarget : Server.Targeting.Target
            {
                private SpeechEventArgs m_Args;

                public PetSetTarget(SpeechEventArgs args)
                    : base(11, false, Server.Targeting.TargetFlags.None)
                {
                    m_Args = args;
                }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    Server.Mobiles.BaseCreature pet = targeted as Server.Mobiles.BaseCreature;
                    if (pet == null || pet.ControlMaster != from)
                    {
                        from.SendMessage("That's not your pet!");
                        return;
                    }

                    string[] split = m_Args.Speech.Split(' ');

                    if (split.Length == 3)
                    {
                        try
                        {
                            string name = split[1];
                            //double value = Convert.ToDouble(split[2]);
                            double value;
                            if (double.TryParse(split[2], out value) == false)
                            {
                                from.SendMessage("'{0}' is not a valid value.", split[2]);
                                return;
                            }

                            if (Insensitive.Equals(name, "str"))
                                ChangeStrength(from, pet, (int)value);
                            else if (Insensitive.Equals(name, "dex"))
                                ChangeDexterity(from, pet, (int)value);
                            else if (Insensitive.Equals(name, "int"))
                                ChangeIntelligence(from, pet, (int)value);
                            else
                                ChangeSkill(from, pet, name, value);
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    }
                }

                private static void ChangeStrength(Mobile from, Server.Mobiles.BaseCreature pet, int value)
                {
                    if (value < 10 || value > pet.StrMax)
                    {
                        from.SendMessage("Your pet's strength ranges between 10 and {0}.", pet.StrMax);
                    }
                    else
                    {
                        if (pet is Server.Mobiles.Dragon)
                        {
                            pet.RawStr = value;
                            pet.ValidateStatCap(Stat.Str);
                            from.SendMessage("Your pet's stats have been adjusted.");
                        }
                        else
                        {
                            if ((value + pet.RawDex + pet.RawInt) > pet.StatCap)
                            {
                                from.SendMessage("Your pet's statcap is {0}. Try setting another stat lower first.", pet.StatCap);
                            }
                            else
                            {
                                pet.RawStr = value;
                                from.SendMessage("Your pet's stats have been adjusted.");
                            }
                        }
                    }
                }

                private static void ChangeDexterity(Mobile from, Server.Mobiles.BaseCreature pet, int value)
                {
                    if (value < 10 || value > pet.DexMax)
                    {
                        from.SendMessage("Your pet's dexterity ranges between 10 and {0}.", pet.DexMax);
                    }
                    else
                    {
                        if (pet is Server.Mobiles.Dragon)
                        {
                            pet.RawDex = value;
                            pet.ValidateStatCap(Stat.Dex);
                            from.SendMessage("Your pet's stats have been adjusted.");
                        }
                        else
                        {
                            if ((value + pet.RawStr + pet.RawInt) > pet.StatCap)
                            {
                                from.SendMessage("Your pet's statcap is {0}. Try setting another stat lower first.", pet.StatCap);
                            }
                            else
                            {
                                pet.RawDex = value;
                                from.SendMessage("Your pet's stats have been adjusted.");
                            }
                        }
                    }
                }

                private static void ChangeIntelligence(Mobile from, Server.Mobiles.BaseCreature pet, int value)
                {
                    if (value < 10 || value > pet.IntMax)
                    {
                        from.SendMessage("Your pet's intelligence ranges between 10 and {0}.", pet.IntMax);
                    }
                    else
                    {
                        if (pet is Server.Mobiles.Dragon)
                        {
                            pet.RawInt = value;
                            pet.ValidateStatCap(Stat.Int);
                            from.SendMessage("Your pet's stats have been adjusted.");
                        }
                        else
                        {
                            if ((value + pet.RawDex + pet.RawStr) > pet.StatCap)
                            {
                                from.SendMessage("Your pet's statcap is {0}. Try setting another stat lower first.", pet.StatCap);
                            }
                            else
                            {
                                pet.RawInt = value;
                                from.SendMessage("Your pet's stats have been adjusted.");
                            }
                        }
                    }
                }

                private static void ChangeSkill(Mobile from, Server.Mobiles.BaseCreature pet, string name, double value)
                {
                    SkillName index;

                    try
                    {
                        index = (SkillName)Enum.Parse(typeof(SkillName), name, true);
                    }
                    catch
                    {
                        from.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
                        return;
                    }

                    Skill skill = pet.Skills[index];

                    if (skill != null)
                    {
                        if (value < 0 || value > skill.Cap)
                        {
                            from.SendMessage(string.Format("Your pet's skill in {0} is capped at {1:F1}.", skill.Info.Name, skill.Cap));
                        }
                        else
                        {
                            int newFixedPoint = (int)(value * 10.0);
                            int oldFixedPoint = skill.BaseFixedPoint;

                            if (((skill.Owner.Total - oldFixedPoint) + newFixedPoint) > skill.Owner.Cap)
                            {
                                from.SendMessage("You can not exceed the skill cap.  Try setting another skill lower first.");
                            }
                            else
                            {
                                skill.BaseFixedPoint = newFixedPoint;
                                from.SendMessage("Your pet's skill has been adjusted.");
                            }
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
                    }
                }
            }

            private class BondPetTarget : Server.Targeting.Target
            {
                public BondPetTarget()
                    : base(-1, false, Targeting.TargetFlags.None)
                {
                }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    Server.Mobiles.BaseCreature pet = targeted as Server.Mobiles.BaseCreature;

                    if (pet == null || pet.ControlMaster != from)
                    {
                        from.SendMessage("That's not your pet!");
                        return;
                    }

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
            }

            public static void EventSink_Speech(SpeechEventArgs args)
            {
                try
                {
                    if (!args.Handled)
                        HandleSpeech(args);
                }
                catch (Exception ex)
                {
                    EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                }
            }

            private static void HandleSpeech(SpeechEventArgs args)
            {
                if (Insensitive.Equals(args.Speech, "tamepet"))
                {
                    args.Mobile.Target = new TamePetTarget();
                    args.Mobile.SendMessage("Target a an animal to tame.");
                    args.Handled = true;
                }
                else if (Insensitive.StartsWith(args.Speech, "petset"))
                {
                    if (args.Speech.Split(' ').Length == 3)
                    {
                        args.Mobile.Target = new PetSetTarget(args);
                        args.Mobile.SendMessage("Target your pet.");

                        args.Handled = true;
                    }
                }
                else if (Insensitive.Equals(args.Speech, "bondpet"))
                {
                    args.Mobile.Target = new BondPetTarget();
                    args.Mobile.SendMessage("Target your pet.");
                    args.Handled = true;
                }
                else if (Insensitive.Equals(args.Speech, "toggle whiff"))
                {
                    if (Core.HITMISS > 0)
                        Core.HITMISS = 0;
                    else if (Core.HITMISS == 0)
                        Core.HITMISS = 1;

                    Core.HITMISS = (Core.HITMISS > 0 ? 0 : 1);
                    args.Mobile.SendMessage("Weapon whiff tracking: {0}", (Core.HITMISS > 0) ? "on" : "off");
                    args.Handled = true;
                }
                else if (Insensitive.Equals(args.Speech, "toggle damage"))
                {
                    if (Core.DAMAGE > 0)
                        Core.DAMAGE = 0;
                    else if (Core.DAMAGE == 0)
                        Core.DAMAGE = 1;

                    args.Mobile.SendMessage("Weapon damage tracking: {0}", (Core.DAMAGE > 0) ? "on" : "off");
                    args.Handled = true;
                }
                else if (Insensitive.Equals(args.Speech, "reset whiff"))
                {
                    Core.HITMISS = 2; // reset flag
                    args.Mobile.SendMessage("Weapon whiff tracking reset.");
                    args.Handled = true;
                }
                else if (Insensitive.Equals(args.Speech, "reset damage"))
                {
                    Core.DAMAGE = 2; // reset flag
                    args.Mobile.SendMessage("Weapon damage tracking reset.");
                    args.Handled = true;
                }
                else if (Insensitive.StartsWith(args.Speech, "go ") && args.Speech.Length > 3)
                {
                    GoTo(args.Mobile, args.Speech.Substring(3).Trim());
                    args.Handled = true;
                }
                else if (Insensitive.StartsWith(args.Speech, "set"))
                {
                    Mobile from = args.Mobile;

                    string[] split = args.Speech.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length > 2)
                    {
                        string name = string.Join("", split, 1, split.Length - 2);
                        double value;

                        if (!double.TryParse(split[split.Length - 1], out value))
                        {
                            from.SendMessage("Invalid value specified.");
                            args.Handled = true;
                        }
                        else if (Insensitive.Equals(name, "str"))
                        {
                            ChangeStrength(from, (int)value);
                            args.Handled = true;
                        }
                        else if (Insensitive.Equals(name, "dex"))
                        {
                            ChangeDexterity(from, (int)value);
                            args.Handled = true;
                        }
                        else if (Insensitive.Equals(name, "int"))
                        {
                            ChangeIntelligence(from, (int)value);
                            args.Handled = true;
                        }
                        else if (Insensitive.Equals(name, "lifeforce"))
                        {
                            Ethics.Player ethicPlayer = Ethics.Player.Find(from);

                            if (ethicPlayer == null)
                            {
                                from.SendMessage("You are not hero/evil aligned.");
                            }
                            else if (value < 0 || value > 100)
                            {
                                from.SendMessage("Invalid value specified.");
                            }
                            else if (ethicPlayer.Power == (int)value)
                            {
                                from.SendMessage("Your life force has not changed.");
                            }
                            else
                            {
                                ethicPlayer.Power = (int)value;
                                from.SendMessage("Your life force has changed.");
                            }

                            args.Handled = true;
                        }
                        else
                        {
                            if (Insensitive.StartsWith(name, "skill") && name.Length > 5)
                                name = name.Substring(5);

                            ChangeSkill(from, name, value);
                            args.Handled = true;
                        }
                    }
                }
            }

            private static void ChangeStrength(Mobile from, int value)
            {
                if (value < 10 || value > 125)
                {
                    from.SendLocalizedMessage(1005628); // Stats range between 10 and 125.
                }
                else
                {
                    if ((value + from.RawDex + from.RawInt) > from.StatCap)
                    {
                        from.SendLocalizedMessage(1005629); // You can not exceed the stat cap.  Try setting another stat lower first.
                    }
                    else
                    {
                        from.RawStr = value;
                        from.SendLocalizedMessage(1005630); // Your stats have been adjusted.
                    }
                }
            }

            private static void ChangeDexterity(Mobile from, int value)
            {
                if (value < 10 || value > 125)
                {
                    from.SendLocalizedMessage(1005628); // Stats range between 10 and 125.
                }
                else
                {
                    if ((from.RawStr + value + from.RawInt) > from.StatCap)
                    {
                        from.SendLocalizedMessage(1005629); // You can not exceed the stat cap.  Try setting another stat lower first.
                    }
                    else
                    {
                        from.RawDex = value;
                        from.SendLocalizedMessage(1005630); // Your stats have been adjusted.
                    }
                }
            }

            private static void ChangeIntelligence(Mobile from, int value)
            {
                if (value < 10 || value > 125)
                {
                    from.SendLocalizedMessage(1005628); // Stats range between 10 and 125.
                }
                else
                {
                    if ((from.RawStr + from.RawDex + value) > from.StatCap)
                    {
                        from.SendLocalizedMessage(1005629); // You can not exceed the stat cap.  Try setting another stat lower first.
                    }
                    else
                    {
                        from.RawInt = value;
                        from.SendLocalizedMessage(1005630); // Your stats have been adjusted.
                    }
                }
            }

            private static void ChangeSkill(Mobile from, string name, double value)
            {
                SkillName index;

                try
                {
                    int score;
                    string real_name = IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), name, out score);
                    if (score > 5) // a 'distance' of > 5 is likely bad input (5 is a heuristically derived value)
                    {
                        from.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
                        return;
                    }
                    index = (SkillName)Enum.Parse(typeof(SkillName), real_name, true);
                }
                catch
                {
                    from.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
                    return;
                }

                Skill skill = from.Skills[index];

                if (skill != null)
                {
                    if (value < 0 || value > skill.Cap)
                    {
                        from.SendMessage(string.Format("Your skill in {0} is capped at {1:F1}.", skill.Info.Name, skill.Cap));
                    }
                    else
                    {
                        int newFixedPoint = (int)(value * 10.0);
                        int oldFixedPoint = skill.BaseFixedPoint;

                        if (((skill.Owner.Total - oldFixedPoint) + newFixedPoint) > skill.Owner.Cap)
                        {
                            from.SendMessage("You can not exceed the skill cap.  Try setting another skill lower first.");
                        }
                        else
                        {
                            skill.BaseFixedPoint = newFixedPoint;
                        }
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
                }
            }
        }

        private static void GoTo(Mobile from, string name)
        {
            if (from.Region.IsJailRules)
            {
                from.SendLocalizedMessage(1041530); // You'll need a better jailbreak plan then that!
            }
            else
            {
                Map map = from.Map;

                Point3D loc = FindLocation(map, name);

                if (loc == Point3D.Zero || !ValidateGo(loc, map))
                {
                    from.SendMessage("No region with that name was found.");
                }
                else
                {
                    Mobiles.BaseCreature.TeleportPets(from, loc, map);
                    from.MoveToWorld(loc, map);
                }
            }
        }

        public static Point3D FindLocation(Map map, string name)
        {
            foreach (GoEntry e in m_GoTable)
            {
                if (Map.Maps[e.MapIndex] == map && e.IsMatch(name))
                    return e.Location;
            }

            Region region = Region.FindByName(name, map);

            if (region != null && region.GoLocation != Point3D.Zero)
                return region.GoLocation;

            region = Region.FindByName(string.Concat("the ", name), map);

            if (region != null && region.GoLocation != Point3D.Zero)
                return region.GoLocation;

            return Point3D.Zero;
        }

        private static bool ValidateGo(Point3D loc, Map map)
        {
            Region region = Region.Find(loc, map);

            if (region.IsGreenAcresRules || region.IsJailRules || region.IsHouseRules)
                return false;

            // felucca overworld
            if (map == Map.Felucca && loc.X < 5120)
                return true;

            // felucca dungeon
            if (map == Map.Felucca && region.IsDungeonRules)
                return true;

            return false;
        }

        private static readonly GoEntry[] m_GoTable = new GoEntry[]
            {
                // moongates
                new GoEntry(0, new Point3D(1336, 1997, 5 ), "Britain Moongate", "Moongate"),
                new GoEntry(0, new Point3D(4467, 1283, 5 ), "Moonglow Moongate"),
                new GoEntry(0, new Point3D(3563, 2139, 34), "Magincia Moongate"),
                new GoEntry(0, new Point3D( 643, 2067, 5 ), "Skara Brae Moongate", "Skara Moongate"),
                new GoEntry(0, new Point3D(1828, 2948,-20), "Trinsic Moongate"),
                new GoEntry(0, new Point3D(2701,  692, 5 ), "Minoc Moongate"),
                new GoEntry(0, new Point3D( 771,  752, 5 ), "Yew Moongate"),
                new GoEntry(0, new Point3D(1499, 3771, 5 ), "Jhelom Moongate"),

                // shrines
                new GoEntry(0, new Point3D(1456,  854,  0), "Chaos"),
                new GoEntry(0, new Point3D(1856,  872, -1), "Compassion"),
                new GoEntry(0, new Point3D(4217,  564, 36), "Honesty"),
                new GoEntry(0, new Point3D(1730, 3528,  3), "Honor"),
                new GoEntry(0, new Point3D(4276, 3699,  0), "Humility"),
                new GoEntry(0, new Point3D(1301,  639, 16), "Justice"),
                new GoEntry(0, new Point3D(1589, 2485,  5), "Spirituality"),
                new GoEntry(0, new Point3D(2496, 3932,  0), "Valor"),

                // faction bases
                new GoEntry(0, new Point3D(3750, 2241, 20), "Council of Mages", "CoM"),
                new GoEntry(0, new Point3D(1172, 2593,  0), "Minax", "Min"),
                new GoEntry(0, new Point3D( 969,  768,  0), "Shadowlords", "SL"),
                new GoEntry(0, new Point3D(1419, 1622, 20), "True Britannians", "True Brits", "TB"),

                // special
                new GoEntry(0, new Point3D(2188, 1985, 0), "Blanche Island", "Blanche"),
                new GoEntry(0, new Point3D(2060, 805, 0), "Stonegate"),
            };

        private class GoEntry
        {
            private int m_MapIndex;
            private Point3D m_Location;
            private string[] m_Keywords;

            public int MapIndex { get { return m_MapIndex; } }
            public Point3D Location { get { return m_Location; } }
            public string[] Keywords { get { return m_Keywords; } }

            public GoEntry(int mapIndex, Point3D location, params string[] keywords)
            {
                m_MapIndex = mapIndex;
                m_Location = location;
                m_Keywords = keywords;
            }

            public bool IsMatch(string name)
            {
                for (int i = 0; i < m_Keywords.Length; i++)
                {
                    if (Insensitive.Equals(m_Keywords[i], name))
                        return true;
                }

                return false;
            }
        }

        public class TCHelpGump : Gump
        {
            public static void EventSink_Speech(SpeechEventArgs args)
            {
                if (!args.Handled && Insensitive.Equals(args.Speech, "help"))
                {
                    args.Mobile.SendGump(new TCHelpGump());

                    args.Handled = true;
                }
            }

            public TCHelpGump()
                : base(40, 40)
            {
                AddPage(0);
                AddBackground(0, 0, 160, 120, 5054);

                AddButton(10, 10, 0xFB7, 0xFB9, 1, GumpButtonType.Reply, 0);
                AddLabel(45, 10, 0x34, "RunUO.com");

                AddButton(10, 35, 0xFB7, 0xFB9, 2, GumpButtonType.Reply, 0);
                AddLabel(45, 35, 0x34, "List of skills");

                AddButton(10, 60, 0xFB7, 0xFB9, 3, GumpButtonType.Reply, 0);
                AddLabel(45, 60, 0x34, "Command list");

                AddButton(10, 85, 0xFB1, 0xFB3, 0, GumpButtonType.Reply, 0);
                AddLabel(45, 85, 0x34, "Close");
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                switch (info.ButtonID)
                {
                    case 1: // RunUO.com
                        {
                            sender.LaunchBrowser("http://www.RunUO.com");
                            break;
                        }
                    case 2: // List of skills
                        {
                            string[] strings = Enum.GetNames(typeof(SkillName));

                            Array.Sort(strings);

                            StringBuilder sb = new StringBuilder();

                            if (strings.Length > 0)
                                sb.Append(strings[0]);

                            for (int i = 1; i < strings.Length; ++i)
                            {
                                string v = strings[i];

                                if ((sb.Length + 1 + v.Length) >= 256)
                                {
                                    sender.Send(new AsciiMessage(Server.Serial.MinusOne, -1, MessageType.Label, 0x35, 3, "System", sb.ToString()));
                                    sb = new StringBuilder();
                                    sb.Append(v);
                                }
                                else
                                {
                                    sb.Append(' ');
                                    sb.Append(v);
                                }
                            }

                            if (sb.Length > 0)
                            {
                                sender.Send(new AsciiMessage(Server.Serial.MinusOne, -1, MessageType.Label, 0x35, 3, "System", sb.ToString()));
                            }

                            break;
                        }
                    case 3: // Command list
                        {
                            sender.Mobile.SendAsciiMessage(0x482, "The command prefix is \"{0}\"", Server.CommandSystem.CommandPrefix);
                            Server.Commands.CommandHandlers.Help_OnCommand(new CommandEventArgs(sender.Mobile, "help", "", new string[0]));

                            break;
                        }
                }
            }
        }
    }
}