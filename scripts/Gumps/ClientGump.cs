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

/* Scripts/Gumps/ClientGump.cs
 * Changelog:
 *	10/17/26, Adam (Confinement Stage 3)
 *		Bring here, Jail now and Prison now go through the confinement coordinator (Confinement.Execute):
 *		same prison/jail = plain move; out = release; in = admission; between facilities (jail <-> prison, Felucca <-> Trammel
 *		prison) = transfer with full check-out and check-in. Staff transfers need no parole eligibility. Replaces the
 *		interim boot/refusal logic below. Staff are told what happened, or why nothing did.
 *	10/17/26, Adam (Bring here)
 *		Bringing a prisoner to another spot in the same prison or jail region is a plain move (no boot).
 *		Bringing them out checks Utility.CanBoot first and only moves them if the boot succeeds; otherwise
 *		staff are told why and the prisoner is left alone. Bringing them into a different prison or jail is refused.
 *	9/7/05, Pix
 *		Added install versions for newer clients.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Accounting;
using Server.Commands;
using Server.Engines.ConfinementSystem;
using Server.Mobiles;
using Server.Network;
using Server.Scripts.Gumps;
using Server.Targets;

namespace Server.Gumps
{
    public class ClientGump : Gump
    {
        private NetState m_State;

        private void Resend(Mobile to, RelayInfo info)
        {
            TextRelay? te = info.GetTextEntry(0);

            to.SendGump(new ClientGump(to, m_State, te == null ? "" : te.Text));
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_State == null)
                return;

            Mobile focus = m_State.Mobile;
            Mobile from = state.Mobile;

            if (focus == null)
            {
                from.SendMessage("That character is no longer online.");
                return;
            }
            else if (focus.Deleted)
            {
                from.SendMessage("That character no longer exists.");
                return;
            }
            else if (from != focus && focus.Hidden && from.AccessLevel < focus.AccessLevel)
            {
                from.SendMessage("That character is no longer visible.");
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Tell
                    {
                        TextRelay? text = info.GetTextEntry(0);

                        if (text != null)
                        {
                            focus.SendMessage(0x482, "{0} tells you:", from.Name);
                            focus.SendMessage(0x482, text.Text);
                        }

                        from.SendGump(new ClientGump(from, m_State));

                        break;
                    }
                case 4: // Props
                    {
                        Resend(from, info);
                        from.SendGump(new PropertiesGump(from, focus));

                        break;
                    }
                case 5: // Go to
                    {
                        if (focus.Map == null || focus.Map == Map.Internal)
                        {
                            from.SendMessage("That character is not in the world.");
                        }
                        else
                        {
                            from.MoveToWorld(focus.Location, focus.Map);
                            Resend(from, info);
                        }

                        break;
                    }
                case 6: // Get
                    {
                        if (from.Map == null || from.Map == Map.Internal)
                        {
                            from.SendMessage("You cannot bring that person here.");
                        }
                        else
                        {   // the confinement coordinator decides: same prison/jail = plain move; out = release; in = admission;
                            //  another prison/jail = transfer. Staff and other non-players are just moved.
                            ConfinementResult r = Confinement.Execute(new ConfinementRequest(focus, from, TransitionAuthority.Staff, TransitionMode.Administrative,
                                ConfinementDestination.At(from.Location, from.Map)) { Comment = "Brought here by staff (Bring here)" });
                            Report(from, focus, r);
                            Resend(from, info);
                        }

                        break;
                    }
                case 7: // Move
                    {
                        from.Target = new MoveTarget(focus);
                        Resend(from, info);

                        break;
                    }
                case 8: // Kick
                    {
                        if (from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > focus.AccessLevel)
                        {
                            focus.Say("I've been kicked!");

                            m_State.Dispose();
                        }

                        break;
                    }
                case 9: // Kill
                    {
                        if (from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > focus.AccessLevel)
                            focus.Kill();

                        Resend(from, info);

                        break;
                    }
                case 10: //Res
                    {
                        if (from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > focus.AccessLevel)
                        {
                            focus.PlaySound(0x214);
                            focus.FixedEffect(0x376A, 10, 16);

                            focus.Resurrect();
                        }

                        Resend(from, info);

                        break;
                    }
                case 11: // Skills
                    {
                        Resend(from, info);

                        if (from.AccessLevel > focus.AccessLevel)
                            from.SendGump(new SkillsGump(from, (Mobile)focus));

                        break;
                    }
                case 12: // Jail (immediate)
                    {
                        ImmediateJail(from, focus);
                        Resend(from, info);
                        break;
                    }
                case 13: // Prison (immediate)
                    {
                        ImmediatePrison(from, focus);
                        Resend(from, info);
                        break;
                    }
            }
        }

        public ClientGump(Mobile from, NetState state)
            : this(from, state, "")
        {
        }

        private const int LabelColor32 = 0xFFFFFF;

        public ClientGump(Mobile from, NetState state, string initialText)
            : base(30, 20)
        {
            if (state == null)
                return;

            m_State = state;

            AddPage(0);

            AddBackground(0, 0, 400, 314, 5054);

            AddImageTiled(10, 10, 380, 19, 0xA40);
            AddAlphaRegion(10, 10, 380, 19);

            // Main body taller to match new gump height
            AddImageTiled(10, 32, 380, 272, 0xA40);
            AddAlphaRegion(10, 32, 380, 272);

            AddHtml(10, 10, 380, 20, Color(Center("User Information"), LabelColor32), false, false);

            int line = 0;

            AddHtml(14, 36 + (line * 20), 200, 20, Color("Address:", LabelColor32), false, false);
            AddHtml(70, 36 + (line++ * 20), 200, 20, Color(state.ToString(), LabelColor32), false, false);

            AddHtml(14, 36 + (line * 20), 200, 20, Color("Client:", LabelColor32), false, false);
            AddHtml(70, 36 + (line++ * 20), 200, 20, Color(state.Version == null ? "(null)" : state.Version.ToString(), LabelColor32), false, false);

            AddHtml(14, 36 + (line * 20), 200, 20, Color("Version:", LabelColor32), false, false);

            ExpansionInfo info = state.ExpansionInfo;
            string expansionName = info.Name;

            AddHtml(70, 36 + (line++ * 20), 200, 20, Color(expansionName, LabelColor32), false, false);

            Account? a = state.Account as Account;
            Mobile m = state.Mobile;

            if (from.AccessLevel >= AccessLevel.GameMaster && a != null)
            {
                AddHtml(14, 36 + (line * 20), 200, 20, Color("Account:", LabelColor32), false, false);
                AddHtml(70, 36 + (line++ * 20), 200, 20, Color(a.Username, LabelColor32), false, false);
            }

            if (m != null)
            {
                AddHtml(14, 36 + (line * 20), 200, 20, Color("Mobile:", LabelColor32), false, false);
                AddHtml(70, 36 + (line++ * 20), 200, 20, Color(string.Format("{0} (0x{1:X})", m.Name, m.Serial.Value), LabelColor32), false, false);

                AddHtml(14, 36 + (line * 20), 200, 20, Color("Location:", LabelColor32), false, false);
                AddHtml(70, 36 + (line++ * 20), 200, 20, Color(string.Format("{0} [{1}]", m.Location, m.Map), LabelColor32), false, false);
                #region new
                // Incarceration status (world state)
                AddHtml(14, 36 + (line * 20), 200, 20,
                    Color("Incarceration:", LabelColor32), false, false);

                string incarcerationText;
                int incarcerationColor;

                if (Utility.IsPlayerImprisoned(m))
                {
                    incarcerationText = "In Prison";
                    incarcerationColor = 0xFF4040; // red
                }
                else if (Utility.IsPlayerJailed(m))
                {
                    incarcerationText = "In Jail";
                    incarcerationColor = 0xFFFF00; // yellow
                }
                else
                {
                    incarcerationText = "Free";
                    incarcerationColor = 0xC0C0C0; // neutral
                }

                AddHtml(110, 36 + (line++ * 20), 200, 20,
                    Color(incarcerationText, incarcerationColor), false, false);

                #endregion new
                // Move Send Message section down by ~2 rows + margin
                AddButton(13, 202, 0xFAB, 0xFAD, 1, GumpButtonType.Reply, 0);
                AddHtml(48, 203, 200, 20, Color("Send Message", LabelColor32), false, false);

                // Move message box down accordingly (restore original height)
                AddImageTiled(12, 227, 376, 80, 0xA40);
                AddImageTiled(13, 228, 374, 78, 0xBBC);
                AddTextEntry(15, 228, 372, 78, 0x480, 0, initialText);

                // Right-side action panel (keep your taller panel)
                AddImageTiled(245, 35, 142, 184, 5058);

                AddImageTiled(246, 36, 140, 182, 0xA40);
                AddAlphaRegion(246, 36, 140, 182);

                line = 0;

                AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 4, GumpButtonType.Reply, 0);
                AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Properties", LabelColor32), false, false);

                if (from != m)
                {
                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 5, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Go to them", LabelColor32), false, false);

                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 6, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Bring here", LabelColor32), false, false);
                }

                AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 7, GumpButtonType.Reply, 0);
                AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Move to target", LabelColor32), false, false);

                if (from.AccessLevel >= AccessLevel.GameMaster && from.AccessLevel > m.AccessLevel)
                {
                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 8, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Disconnect", LabelColor32), false, false);

                    // Emergency confinement actions (defaults)
                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 12, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Jail now", LabelColor32), false, false);

                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 13, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Prison now", LabelColor32), false, false);

                    if (m.Alive)
                    {
                        AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 9, GumpButtonType.Reply, 0);
                        AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Kill", LabelColor32), false, false);
                    }
                    else
                    {
                        AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 10, GumpButtonType.Reply, 0);
                        AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Resurrect", LabelColor32), false, false);
                    }
                }

                if (from.AccessLevel >= AccessLevel.Counselor && from.AccessLevel > m.AccessLevel)
                {
                    AddButton(246, 36 + (line * 20), 0xFA5, 0xFA7, 11, GumpButtonType.Reply, 0);
                    AddHtml(280, 38 + (line++ * 20), 100, 20, Color("Skills browser", LabelColor32), false, false);
                }

            }


        }

        private static bool CanConstrain(Mobile staff, Mobile target)
        {
            return staff != null
                   && target != null
                   && staff.AccessLevel >= AccessLevel.GameMaster
                   && staff.AccessLevel > target.AccessLevel;
        }
        private static void ImmediatePrison(Mobile staff, Mobile target)
        {
            if (!CanConstrain(staff, target))
                return;

            PlayerMobile? pm = target as PlayerMobile;

            if (pm == null || (pm.Account is Accounting.Account) == false)
                return;

            // admission from the world, or a transfer from jail (jail check-out, then prison check-in), through the coordinator
            ConfinementResult r = Confinement.Execute(new ConfinementRequest(pm, staff, TransitionAuthority.Staff, TransitionMode.Administrative,
                ConfinementDestination.Prison()) { Comment = "Sent to prison", PrisonSentence = 3 });

            // Already in prison (by region)
            if (r.Plan.Operation == ConfinementOperation.Relocate)
                staff.SendMessage("That player is already in prison.");
            else
                Report(staff, pm, r);
        }

        // Tell staff how a coordinated jail/prison move went. Plain moves are silent, as Bring here always was.
        private static void Report(Mobile staff, Mobile focus, ConfinementResult r)
        {
            ConfinementPlan p = r.Plan;
            string from = p.Source.Region != null ? string.Format("{0} ({1})", p.Source.Region, p.Source.Map) : "the world";
            string to = p.DestinationRegion != null ? string.Format("{0} ({1})", p.DestinationRegion, p.DestinationMap) : "the world";

            switch (r.Outcome)
            {
                case ConfinementOutcome.Denied:
                    staff.SendMessage("{0} cannot be {1} right now: {2}", focus.Name, p.Operation switch
                    {
                        ConfinementOperation.Release => "released from " + from,
                        ConfinementOperation.Admit => "admitted to " + to,
                        ConfinementOperation.Transfer => "transferred from " + from + " to " + to,
                        _ => "moved",
                    }, r.Reason);
                    break;

                case ConfinementOutcome.PartialFailure:
                    staff.SendMessage(0x22, "{0}: the {1} did not complete ({2}). Done so far: {3}. See Confinement.log.",
                        focus.Name, p.Operation.ToString().ToLower(), r.Message, string.Join(", ", r.Steps));
                    break;

                default:
                    if (p.Operation == ConfinementOperation.Release)
                        staff.SendMessage("{0} was released from {1}.", focus.Name, from);
                    else if (p.Operation == ConfinementOperation.Admit)
                        staff.SendMessage("{0} was admitted to {1}.", focus.Name, to);
                    else if (p.Operation == ConfinementOperation.Transfer)
                        staff.SendMessage("{0} was transferred from {1} to {2}.", focus.Name, from, to);
                    if (r.Message.Length > 0 && p.Operation != ConfinementOperation.Relocate)
                        staff.SendMessage("({0})", r.Message);
                    break;
            }
        }

        private static void ImmediateJail(Mobile staff, Mobile target)
        {
            if (!CanConstrain(staff, target))
                return;

            PlayerMobile? pm = target as PlayerMobile;

            if (pm == null || (pm.Account is Accounting.Account) == false)
                return;

            // admission from the world, or a transfer from prison (staff prison check-out without parole eligibility,
            //  then jail check-in), through the coordinator
            ConfinementResult r = Confinement.Execute(new ConfinementRequest(pm, staff, TransitionAuthority.Staff, TransitionMode.Administrative,
                ConfinementDestination.Jail()) { Comment = "Sent to jail", JailCell = 3, JailHours = 24 });

            // Already in jail (by region)
            if (r.Plan.Operation == ConfinementOperation.Relocate)
                staff.SendMessage("That player is already jailed.");
            else
                Report(staff, pm, r);
        }

    }
}