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

/* Scripts/Misc/Keywords.cs 
 *	ChangeLog
 *  10/14/06, Rhiannon
 *		Changed test for guild resignation to match the whole text line instead of as a keyword.
 *  5/08/06, Kit
 *		changed shrine of sacrafice to tell them to goto TOK for releaseing pets now.
 *	4/21/05, Adam
 *		change ConfirmReleaseGump invocation to use 'range check false' param
 *  4/20/05, smerX
 *		Added Bonded Pet release at Shrine of Sacrifice
 *  1/12/05, Albatross
 *      Changed color of out of stat loss text to blue and added "...for some time" to the end of response for players with no short term murders and 1 or more long term murders
 *	1/10/05
 *		Changed response for "i must consider my sins", to old RP text.  Murder counts will now be displayed when the player says "what have i done". 
 *  6/5/04, Pix 
 *		Merged in 1.0RC0 code. 
 *	5/02/04 
 *		Added code to decay kills when player says "i must consider my sins" in addition to code that already existed in serialize. This way, if time between saves are increased 
 *		players can still decay their counts at the appropriate time by simply checking how many counts they have left. 
 */

using Server.Accounting;
using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Mobile;

namespace Server.Misc
{
    public class Keywords
    {
        public static void Initialize()
        {
            // Register our speech handler 
            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
        }

        public static void EventSink_Speech(SpeechEventArgs args)
        {
            Mobile from = args.Mobile;
            int[] keywords = args.Keywords;

            // "what have i done"
            string speech = args.Speech == null ? "" : args.Speech.Trim();

            if (speech.Equals("what have i done", StringComparison.OrdinalIgnoreCase) || speech.Equals("what have i done?", StringComparison.OrdinalIgnoreCase))
            {
                WhatHaveIDone(from);
                return;
            }

            // Shrine of Sacrifice - pet release / smerx
            else if ((args.Speech.ToLower()).StartsWith("i wish to release ") && args.Speech.Length > 18 && from is PlayerMobile && from.CheckAlive() && from.X > 3350 && from.X < 3359 && from.Y > 284 && from.Y < 296)
            {
                string torelease = args.Speech.Remove(0, 18);
                torelease.ToLower();
                PlayerMobile? pm = from as PlayerMobile;
                Mobile? pet = FindPet(torelease, pm);

                if (!ValidatePet(pet, pm))
                    return;

                if (pet != null)
                {
                    pm.SendMessage("Seek out the Tree of Knowledge, for it is he who will be able to assist you and your companion.");
                    return;
                }
                else if (!(pet is BaseCreature))
                {
                    return;
                }
            }
            // Only if player's whole line matches the resignation phrase, make them resign from the guild.
            else if ((args.Speech.ToLower()).Equals("i resign from my guild"))
            {
                if (GuildLastMemberWarningGump.CheckTownship(from) && GuildLastMemberWarningGump.CheckLastMember(from))
                {
                    from.CloseGump(typeof(GuildLastMemberWarningGump));
                    from.SendGump(new GuildLastMemberWarningGump(from, GuildWarning.IResign));
                }
                else if (from.Guild != null)
                    ((Guild)from.Guild).ResignMember(from);
            }

            for (int i = 0; i < keywords.Length; ++i)
            {
                switch (keywords[i])
                {
                    //					case 0x002A: // *i resign from my guild*
                    //					{
                    //						if (from.Guild != null)
                    //							((Guild)from.Guild).RemoveMember(from);
                    //
                    //						break;
                    //					}
                    //
                    case 0x0032: // "*i must consider my sins* 
                        {
                            if (from.LongTermMurders == 0)
                            {
                                if (from.ShortTermMurders < 5 && from.ShortTermMurders > 0)
                                {
                                    from.SendMessage(0x59, "Although thou hast slain the innocent, thy deeds shall not bring retribution upon thy return to the living.", true); //  
                                }
                                else if (from.ShortTermMurders > 4)
                                {
                                    from.SendMessage(0x22, "If thou should return to the land of the living, the innocent shall wreak havoc upon thy soul.", true); //  
                                }
                                else if (from.ShortTermMurders == 0)
                                {
                                    from.SendMessage(0x59, "Fear not, for thou hast not slain the innocent.", true); //  
                                }
                            }
                            else if (from.LongTermMurders != 0)
                            {
                                if (from.ShortTermMurders < 5 && from.ShortTermMurders > 0)
                                {
                                    from.SendMessage(0x59, "Although thou hast slain the innocent, thy deeds shall not bring retribution upon thy return to the living.", true); //  
                                }
                                else if (from.ShortTermMurders > 4)
                                {
                                    from.SendMessage(0x22, "If thou should return to the land of the living, the innocent shall wreak havoc upon thy soul.", true); //  
                                }
                                else if (from.ShortTermMurders == 0)
                                {
                                    from.SendMessage(0x59, "Fear not, for thou hast not slain the innocent in some time.", true); //  
                                }
                            }
                        }

                        break;
                }
            }
        }
        private static void WhatHaveIDone(Mobile from)
        {
            if (from is not PlayerMobile pm) return;
            if (pm.Account is not Account acct) return;
            DateTime now = DateTime.UtcNow;

            pm.DecayKills();

            bool any = false;

            MurderStatusInfo status = MurderCountUtility.GetMurderStatus(pm);

            #region Murders & Status
            if (pm.LongTermMurders > 0 || pm.ShortTermMurders > 0)
            {
                from.SendMessage("Murder counts:");
                if (pm.LongTermMurders > 0)
                    from.SendMessage("  Long-term: {0}", pm.LongTermMurders);
                if (pm.ShortTermMurders > 0)
                    from.SendMessage("  Short-term: {0}", pm.ShortTermMurders);
                any = true;
            }
            #endregion

            #region Criminality (prioritized)
            if (!status.GlobalWhackable)
            {
                // Non-red: full prioritized display
                var sources = new[]
                {
                (Severity: 4, Label: "Harmful", GrayEnds: acct.HarmfulCriminalityEnds, WhackEnds: acct.HarmfulGuardWhackEnds, TravelEnds: acct.HarmfulTravelLockEnds),
                (Severity: 3, Label: "AFK", GrayEnds: acct.AFKCriminalityEnds, WhackEnds: acct.AFKGuardWhackEnds, TravelEnds: acct.AFKTravelLockEnds),
                (Severity: 2, Label: "Thieving", GrayEnds: acct.ThievingCriminalityEnds, WhackEnds: acct.ThievingGuardWhackEnds, TravelEnds: acct.ThievingTravelLockEnds),
                (Severity: 1, Label: "Criminal", GrayEnds: DateTime.MinValue, WhackEnds: DateTime.MinValue, TravelEnds: DateTime.MinValue)
            };

                var activeSources = sources
                    .Where(s => s.GrayEnds > now)
                    .OrderByDescending(s => s.GrayEnds)
                    .ThenByDescending(s => s.Severity)
                    .ToList();

                if (activeSources.Any())
                {
                    var dominant = activeSources[0];

                    from.SendMessage("{0} criminality:", dominant.Label);
                    SendRemaining(from, "  Criminal status remaining", RemainingUntil(dominant.GrayEnds, now));
                    SendRemaining(from, "  Travel restricted remaining", RemainingUntil(dominant.TravelEnds, now));
                    SendRemaining(from, "  Guards callable remaining", RemainingUntil(dominant.WhackEnds, now));

                    any = true;

                    if (activeSources.Count > 1)
                    {
                        var others = activeSources.Skip(1).Select(s => s.Label);
                        from.SendMessage("  Also active: {0}", string.Join(", ", others));
                    }
                }
                else if (pm.ExpireCriminal != null && pm.ExpireCriminal.Running)
                {
                    YouAreACriminal(from);
                    any = true;
                }
            }
            else // Red players: show max gray and travel from any source
            {
                TimeSpan maxGray = TimeSpan.Zero;
                TimeSpan maxTravel = TimeSpan.Zero;

                if (acct.HarmfulCriminalityEnds > now)
                {
                    TimeSpan gray = RemainingUntil(acct.HarmfulCriminalityEnds, now);
                    if (gray > maxGray) maxGray = gray;

                    TimeSpan travel = RemainingUntil(acct.HarmfulTravelLockEnds, now);
                    if (travel > maxTravel) maxTravel = travel;
                }

                if (acct.AFKCriminalityEnds > now)
                {
                    TimeSpan gray = RemainingUntil(acct.AFKCriminalityEnds, now);
                    if (gray > maxGray) maxGray = gray;

                    TimeSpan travel = RemainingUntil(acct.AFKTravelLockEnds, now);
                    if (travel > maxTravel) maxTravel = travel;
                }

                if (acct.ThievingCriminalityEnds > now)
                {
                    TimeSpan gray = RemainingUntil(acct.ThievingCriminalityEnds, now);
                    if (gray > maxGray) maxGray = gray;

                    TimeSpan travel = RemainingUntil(acct.ThievingTravelLockEnds, now);
                    if (travel > maxTravel) maxTravel = travel;
                }

                // Runtime criminal timer (fallback/legacy)
                if (pm.ExpireCriminal != null && pm.ExpireCriminal.Running)
                {
                    TimeSpan runtimeGray = pm.ExpireCriminal.NextTick - now;
                    if (runtimeGray > maxGray) maxGray = runtimeGray;
                }

                if (maxGray > TimeSpan.Zero)
                {
                    SendRemaining(from, "  Criminal status remaining", maxGray);
                    any = true;
                }

                if (maxTravel > TimeSpan.Zero)
                {
                    SendRemaining(from, "  Travel restricted remaining", maxTravel);
                    any = true;
                }
            }
            #endregion Criminality (prioritized)

            #region Secondary (only for non-reds)
            if (!status.GlobalWhackable)
            {
                var permaTable = pm.PermaFlagsTable;
                if (permaTable != null && permaTable.Count > 0)
                {
                    foreach (var kvp in permaTable)
                    {
                        Mobile attacker = kvp.Key;
                        DateTime expiresAt = kvp.Value;

                        if (attacker == null || attacker.Deleted) continue;

                        TimeSpan remaining = expiresAt == DateTime.MaxValue ? TimeSpan.MaxValue : expiresAt - now;
                        if (remaining <= TimeSpan.Zero) continue;

                        if (remaining == TimeSpan.MaxValue)
                            from.SendMessage("{0} may always attack you.", attacker.Name);
                        else if (remaining.TotalMinutes < 60)
                            from.SendMessage("{0} may attack you for {1} minutes.", attacker.Name, (int)Math.Ceiling(remaining.TotalMinutes));
                        else
                            from.SendMessage("{0} may attack you for {1}h {2}m.", attacker.Name, (int)remaining.TotalHours, remaining.Minutes);

                        any = true;
                    }
                }

                if (Spells.SpellHelper.CheckCombat(from, out TimeSpan combatRemaining, out Mobile against) &&
                    against != null && against.Player)
                {
                    int secs = (int)Math.Ceiling(combatRemaining.TotalSeconds);
                    if (secs < 1) secs = 1;

                    if (secs < 3600)
                    {
                        int mm = secs / 60;
                        int ss = secs % 60;
                        from.SendMessage("Aggression timer against {0}: {1}:{2:D2} remaining.", against.Name, mm, ss);
                    }
                    else
                    {
                        int hh = secs / 3600;
                        int mm = (secs % 3600) / 60;
                        from.SendMessage("Aggression timer against {0}: {1}h {2}m remaining.", against.Name, hh, mm);
                    }
                    any = true;
                }
                if (acct != null && CharacterCreation.GrieferHotList.TryGetValue(acct.GUID, out DateTime hotUntil) && hotUntil > now)
                {
                    SendRemaining(from, "Guards actively looking for you (hot list) remaining", RemainingUntil(hotUntil, now));
                    any = true;
                }
            }
            #endregion Secondary (only for non-reds)

            if (!any)
                from.SendMessage("You have no outstanding judgments or restrictions.");
        }
        private static void SendRemaining(Mobile from, string label, TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return;

            if (remaining == TimeSpan.MaxValue)
            {
                from.SendMessage("{0}: forever", label);
                return;
            }

            if (remaining.TotalMinutes < 60)
                from.SendMessage("{0}: {1} minutes", label, (int)Math.Ceiling(remaining.TotalMinutes));
            else
                from.SendMessage("{0}: {1}h {2}m", label, (int)remaining.TotalHours, remaining.Minutes);
        }
        private static TimeSpan RemainingUntil(DateTime endsAt, DateTime now)
        {
            if (endsAt == DateTime.MaxValue) return TimeSpan.MaxValue;
            TimeSpan rem = endsAt - now;
            return rem > TimeSpan.Zero ? rem : TimeSpan.Zero;
        }
        private static void YouAreACriminal(Mobile from)
        {
            var timer = from.ExpireCriminal;

            if (timer == null || !timer.Running)
                return;

            DateTime now = DateTime.UtcNow;
            TimeSpan remaining = timer.NextTick - now;

            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            int secs = (int)Math.Ceiling(remaining.TotalSeconds);
            if (secs < 1 && remaining > TimeSpan.Zero)
                secs = 1;

            string reason = timer.Context?.Text ?? "source unknown";

            if (secs < 3600)
            {
                int mm = secs / 60;
                int ss = secs % 60;

                from.SendMessage("You are a criminal ({0}). {1}:{2:D2} remaining.",
                    reason, mm, ss);
            }
            else
            {
                int hh = secs / 3600;
                int mm = (secs % 3600) / 60;

                from.SendMessage("You are a criminal ({0}). {1}h {2}m remaining.",
                    reason, hh, mm);
            }
        }
        private static Mobile? FindPet(string petname, PlayerMobile? owner)
        {
            if (owner == null || petname == null)
                return null;

            List<BaseCreature> list = new List<BaseCreature>();
            if (AnimalTrainer.Table.ContainsKey(owner))
                list = AnimalTrainer.Table[owner];
            if (list.Count > 0)
            {
                foreach (Mobile m in list)
                {
                    if (Insensitive.Equals(m.Name, petname))
                        return m as Mobile;
                }
            }

            foreach (Mobile n in World.Mobiles.Values)
            {
                if (n is BaseCreature && !n.Deleted)
                {
                    BaseCreature bc = (BaseCreature)n;

                    if (bc.Controlled && bc.ControlMaster == owner)
                    {
                        if (Insensitive.Equals(bc.Name, petname))
                            return bc as Mobile;
                    }
                }
            }

            return null;
        }


        private static bool ValidatePet(Mobile? pet, PlayerMobile messageReciever)
        {
            if (pet != null && pet is BaseCreature && !pet.Deleted)
            {
                BaseCreature? bc = pet as BaseCreature;

                if (bc.IsAnyStabled)
                {
                    messageReciever.SendMessage("That creature is in your {0} stables.", bc.GetStableName);
                    return false;
                }
                else if (!bc.IsBonded)
                {
                    messageReciever.SendMessage("That pet is not bonded to you.");
                    return false;
                }
                else if (messageReciever.InRange(bc, 12))
                {
                    messageReciever.SendMessage("Your pet is not far from here, you do not require my assistance.");
                    return false;
                }

                return true;
            }

            else
                messageReciever.SendMessage("You have no pets by that name.");

            return false;

        }

    }
}