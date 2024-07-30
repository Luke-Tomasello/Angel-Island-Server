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

using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections.Generic;

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
            if (args.Speech.ToLower() == "what have i done" || args.Speech.ToLower() == "what have i done?")
            {
                if (from is Server.Mobiles.PlayerMobile)
                {
                    ((Server.Mobiles.PlayerMobile)from).DecayKills();
                }
                from.SendMessage("Short Term Murders : {0}", from.ShortTermMurders);
                from.SendMessage("Long Term Murders : {0}", from.LongTermMurders);
                return;
            }

            // Shrine of Sacrifice - pet release / smerx
            else if ((args.Speech.ToLower()).StartsWith("i wish to release ") && args.Speech.Length > 18 && from is PlayerMobile && from.CheckAlive() && from.X > 3350 && from.X < 3359 && from.Y > 284 && from.Y < 296)
            {
                String torelease = args.Speech.Remove(0, 18);
                torelease.ToLower();
                PlayerMobile pm = from as PlayerMobile;
                Mobile pet = FindPet(torelease, pm);

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

        private static Mobile FindPet(String petname, PlayerMobile owner)
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


        private static bool ValidatePet(Mobile pet, PlayerMobile messageReciever)
        {
            if (pet != null && pet is BaseCreature && !pet.Deleted)
            {
                BaseCreature bc = pet as BaseCreature;

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