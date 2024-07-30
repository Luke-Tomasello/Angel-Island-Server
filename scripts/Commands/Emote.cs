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

/* Scripts\Commands\Emote.cs
 * ChangeLog
 *    3/1/22, Yoar
 *		Initial version.
 */

using Server.Gumps;
using Server.Network;
using System;

namespace Server.Commands
{
    public static class EmoteCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("Emote", AccessLevel.Player, new CommandEventHandler(OnCommand));
            CommandSystem.Register("E", AccessLevel.Player, new CommandEventHandler(OnCommand));
        }

        private static readonly TimeSpan m_ShortDelay = TimeSpan.FromMilliseconds(500.0);
        private static readonly TimeSpan m_LongDelay = TimeSpan.FromSeconds(5.0);
        public static void RandomEmote(Mobile from)
        {
            Emote em = m_Emotes[Utility.Random(m_Emotes.Length)];
            if (!from.CanBeginAction(typeof(EmoteCommand)))
                from.SendLocalizedMessage(500119); // You must wait to perform another action.
            else
                em.DoEmote(from);
        }
        private static readonly Emote[] m_Emotes = new Emote[]
            {
                new Emote( "ah", "ah", 778, 1049, 0 ),
                new Emote( "ahha", "ah ha!", 779, 1050, 0 ),
                new Emote( "applaud", "applauds", 780, 1051, 0 ),
                new Emote( "blownose", "blows nose", 781, 1052, 34 ),
                new Emote( "bow", "bows", 0, 0, 32 ),
                new Emote( "bscough", "bs cough", 786, 1057, 0 ),
                new Emote( "burp", "burp", 782, 1053, 33 ),
                new Emote( "clearthroat", "clears throat", 784, 1055, 33 ),
                new Emote( "cough", "cough", 785, 1056, 33 ),
                new Emote( "cry", "cries", 787, 1058, 0 ),
                new Emote( "faint", "faints", 791, 1063, 22 ),
                new Emote( "fart", "farts", 792, 1064, 0 ),
                new Emote( "gasp", "gasps", 793, 1065, 0 ),
                new Emote( "giggle", "giggles", 794, 1066, 0 ),
                new Emote( "groan", "groans", 795, 1067, 0 ),
                new Emote( "growl", "growls", 796, 1068, 0 ),
                new Emote( "hey", "hey!", 797, 1069, 0 ),
                new Emote( "hiccup", "hiccup", 798, 1070, 0 ),
                new Emote( "huh", "huh?", 799, 1071, 0 ),
                new Emote( "kiss", "kisses", 800, 1072, 0 ),
                new Emote( "laugh", "laughs", /* 801 */ 794, 1073, 0 ),
                new Emote( "no", "no!", 802, 1074, 0 ),
                new Emote( "oh", "oh!", 803, 1075, 0 ),
                new Emote( "oooh", "oooh", 811, 1085, 0 ),
                new OomphEmote(),
                new Emote( "oops", "oops", 812, 1086, 0 ),
                new PukeEmote(),
                new Emote( "punch", "punches", 315, 315, 31 ),
                new Emote( "scream", "ahhh!", 814, 1088, 0 ),
                new Emote( "shush", "shhh", 815, 1089, 0 ),
                new Emote( "sigh", "sigh", 816, 1090, 0 ),
                new Emote( "slap", "slaps", 948, 948, 11 ),
                new Emote( "sneeze", "ahh-choo!", 817, 1091, 32 ),
                new Emote( "sniff", "sniff", 818, 1092, 34 ),
                new Emote( "snore", "snore", 819, 1093, 0 ),
                new Emote( "spit", "spits", 820, 1094, 6 ),
                new Emote( "stickouttongue", "sticks out tongue", 792, 792, 0 ),
                new Emote( "tapfoot", "taps foot", 874, 874, 38 ),
                new Emote( "whistle", "whistles", 821, 1095, 5 ),
                new Emote( "woohoo", "woohoo", 783, 1054, 0 ),
                new Emote( "yawn", "yawns", 822, 1096, 17 ),
                new Emote( "yea", "yea!", 823, 1097, 0 ),
                new Emote( "yell", "yells", 824, 1098, 0 ),
            };

        [Usage("Emote <action>")]
        [Aliases("E")]
        [Description("Emote with sounds, words, and/or an animation.")]
        public static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!from.Alive || from.Squelched)
                return;

            bool found = false;

            string key = e.ArgString.Trim().ToLower();

            if (key.Length != 0)
            {
                foreach (Emote em in m_Emotes)
                {
                    if (key.Equals(em.Key))
                    {
                        if (!from.CanBeginAction(typeof(EmoteCommand)))
                            from.SendLocalizedMessage(500119); // You must wait to perform another action.
                        else
                            em.DoEmote(from);

                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                from.CloseGump(typeof(EmoteGump));
                from.SendGump(new EmoteGump(from));
            }
        }

        public class EmoteGump : Gump
        {
            private int m_Page;

            public EmoteGump(Mobile from)
                : this(from, 0)
            {
            }

            private EmoteGump(Mobile from, int page)
                : base(600, 50)
            {
                m_Page = page;

                AddBackground(0, 65, 130, 360, 5054);
                AddAlphaRegion(10, 70, 110, 350);
                AddImageTiled(10, 70, 110, 20, 9354);
                AddLabel(13, 70, 200, "Emote List");

                int index = 12 * m_Page;
                int y = 90;

                for (int i = 0; i < 12 && index < m_Emotes.Length; i++, index++, y += 25)
                    AddButtonLabeled(10, y, 100 + index, m_Emotes[index].Key);

                if (page < (m_Emotes.Length - 1) / 12)
                    AddButton(70, 380, 4502, 0504, 1, GumpButtonType.Reply, 0); // next page

                if (page > 0)
                    AddButton(10, 380, 4506, 4508, 2, GumpButtonType.Reply, 0); // previous page
            }

            private void AddButtonLabeled(int x, int y, int buttonID, string text)
            {
                AddButton(x, y - 1, 4005, 4007, buttonID, GumpButtonType.Reply, 0);
                AddHtml(x + 35, y, 240, 20, String.Format("<BASEFONT COLOR=#FFFFFF>{0}</BASEFONT>", text), false, false);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;

                if (!from.Alive || from.Squelched)
                    return;

                int page = m_Page;
                bool resend = false;

                switch (info.ButtonID)
                {
                    case 1: // next page
                        {
                            if (m_Page < (m_Emotes.Length - 1) / 12)
                            {
                                page++;
                                resend = true;
                            }

                            break;
                        }
                    case 2: // previous page
                        {
                            if (m_Page > 0)
                            {
                                page--;
                                resend = true;
                            }

                            break;
                        }
                    default:
                        {
                            int index = info.ButtonID - 100;

                            if (index >= 0 && index < m_Emotes.Length)
                            {
                                if (!from.CanBeginAction(typeof(EmoteCommand)))
                                    from.SendLocalizedMessage(500119); // You must wait to perform another action.
                                else
                                    m_Emotes[index].DoEmote(from);

                                resend = true;
                            }

                            break;
                        }
                }

                if (resend)
                {
                    from.CloseGump(typeof(EmoteGump));
                    from.SendGump(new EmoteGump(from, page));
                }
            }
        }

        private class Emote
        {
            private string m_Key;
            private string m_Emote;
            private int m_MSound;
            private int m_FSound;
            private int m_Animation;

            public string Key { get { return m_Key; } }

            public Emote(string key, string emote, int fSound, int mSound, int animation)
            {
                m_Key = key;
                m_Emote = emote;
                m_MSound = fSound;
                m_FSound = mSound;
                m_Animation = animation;
            }

            public virtual void DoEmote(Mobile from)
            {
                from.RevealingAction();

                DoOverheadEmote(from);

                int sound = GetSoundID(from);

                if (sound != 0)
                    from.PlaySound(sound);

                if (m_Animation != 0 && from.Body.IsHuman && !from.Mounted)
                    from.Animate(m_Animation, 5, 1, true, false, 0);

                from.BeginAction(typeof(EmoteCommand));

                new ReleaseLockTimer(from, from.AccessLevel >= AccessLevel.GameMaster ? m_ShortDelay : m_LongDelay).Start();
            }

            protected virtual void DoOverheadEmote(Mobile from)
            {
                from.Emote(String.Format("*{0}*", m_Emote));
            }

            protected virtual int GetSoundID(Mobile from)
            {
                return from.Female ? m_MSound : m_FSound;
            }

            private class ReleaseLockTimer : Timer
            {
                private Mobile m_From;

                public ReleaseLockTimer(Mobile from, TimeSpan delay)
                    : base(delay)
                {
                    m_From = from;
                }

                protected override void OnTick()
                {
                    m_From.EndAction(typeof(EmoteCommand));
                }
            }
        }

        private class OomphEmote : Emote
        {
            public OomphEmote()
                : base("oomph", "oomph", 0, 0, 0)
            {
            }

            protected override int GetSoundID(Mobile from)
            {
                if (from.Female)
                    return Utility.Random(804, 7);
                else
                    return Utility.Random(1076, 9);
            }
        }

        private class PukeEmote : Emote
        {
            public PukeEmote()
                : base("puke", "pukes", 813, 1087, 32)
            {
            }

            public override void DoEmote(Mobile from)
            {
                base.DoEmote(from);

                int x = from.X;
                int y = from.Y;

                Movement.Movement.Offset(from.Direction, ref x, ref y);

                new Puke().MoveToWorld(new Point3D(x, y, from.Z), from.Map);
            }

            private class Puke : Item
            {
                public override string DefaultName { get { return "puke"; } }

                [Constructable]
                public Puke()
                    : base(Utility.Random(4650, 6))
                {
                    Hue = 647 + Utility.Random(0, 3) * 5;
                    Movable = false;
                    Timer.DelayCall(TimeSpan.FromSeconds(10.0), Delete);
                }

                public Puke(Serial serial)
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

                    Delete();
                }
            }
        }
    }
}