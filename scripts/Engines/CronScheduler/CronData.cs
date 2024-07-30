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

/* Scripts\Engines\CronScheduler\CronData.cs
 * CHANGELOG:
 *	11/24/08, Adam
 *		Remove link to outdated Island Balance Sheet in donation reminder
 *	7/3/08, Adam
 *		Update the RansomQuestReminderMsg and ServerWarsMsg text and format fields
 *	3/29/08, Adam
 *		We no longer schedule events on Pacific Standard Time.
 *		Change reference from Pacific Standard Time to Pacific Time.
 *		Pacific Time Definition Pacific Time
 *			noun
 *			standard time or daylight saving time in the time zone which includes the W states of the continental U.S.
 *		http://www.yourdictionary.com/pacific-time
 *	3/27/08, Adam
 *		Rename from Scripts/Engines/Heartbeat/HeartbeatData.cs
 *  2/28/07, Adam
 *      Remove the 'chest open time' from the ransom event email.
 *	6/24/06, Adam
 *		Minor wording change for RansomQuestReminderMsg
 *	6/22/06, Adam
 *		Add new GenericReminderMsg for generic account mailings. 
 *		See also: [email command
 *	6/19/06, Adam
 *		- Rename MonthlyReminder to DonationReminder
 *		- Add RansomQuestReminderMsg
 *	2/1/06, Adam
 *		Initial Version.
 */

namespace Server.Engines.CronScheduler
{
    // we'll probably change this message as needs dictate
    public class GenericReminderMsg
    {
        public string m_subject = "blah blah blah";
        public string m_body =
            "Dear AI Player, \n";
    }

    public class ServerWarsMsg
    {
        public string m_subject = "Angel Island Events: Cross Shard Challenge this {0:D}"; // Sunday, November 20th
        public string m_body =

            // Sunday, November 20 at 12:00 noon.
            "Angel Island will be hosting another Cross Shard Challenge this {0:D} from {0:t} - {1:t} Pacific Time. ({0:t} Pacific Time is {2:t} Eastern).\n" +
            "type [time ingame to get the current server time.\n" +
            "\n" +
            "Angel Island would like to invite all you PvP'ers from Defiance, Divinity, PJX, Hybrid, Demise and even any unicorn shards out there to come mix it up with us this Sunday and show off your PvP skills. For this event you are encouraged to advertise your shard by setting your guild title to your shard name, for example: [Divinity]\n" +
            "\n" +
            "The usually scheduled Server Wars on Angel Island have been enhanced with full TEST CENTER functionality. This means that you can simply show up, login a new character, get all the gold and supplies you need, Set Skill, and head to Dungeon Wrong or West Britain Bank for 3 hours of PvP madness. All NPCs that sell weapons and armor will be selling Exceptional quality goods for the duration of Server Wars. (Players seem to be enjoying camping the supply stones at WBB, go figure.)\n" +
            "Our server has been updated so that you can log in with whatever client you are most comfortable with.\n" +
            "\n" +
            "If you're coming from another shard for the afternoon; come early, place a small house and drop a guildstone so that your shardies can join and fly your shards colors (we want to know who we're fighting) then head to Wrong and pick a dye tub and robe (supplied) and suit up! (The robes stay with you when you die and will retain your colors so everyone will know which group you belong to.)\n" +
            "\n" +
            "Angel Islanders will be in Red, but Bobs and Orcs are exempt from any dress code. (Heh, Bobs will be Bobs.)\n" +
            "\n" +
            "OBJECTIVE: \n" +
            "The shard with the greatest number of living members when Server Wars end wins. (Pics or it didn't happen!)\n" +
            "\n" +
            "Tips, tricks and hints! \n" +
            "\n" +
            "WAR RING: If you setup a guildstone to fly your shards colors, have your guildmaster click the War Ring option, this will automatically place you at war with all other guilds in the War Ring. This is really only important if enemy/ally hueing is important to you.\n" +
            "\n" +
            "MURDER COUNTS: None for Server Wars!\n" +
            "No prison, no stat loss, no murder counts\n" +
            "\n" +
            "GETTING READY: Mages should be ready to go immediately after logging in with their full spell book and thousands of reagents, but any needed weapons or armor should be banked early to ensure you have what you need when you need it.\n" +
            "There will be a reagent stone and potion stone at West Britain Bank for restocking. There will be no Guards in Britain, so mark a rune! (I'd mark a couple, stone camping is not uncommon.)\n" +
            "\n" +
            "As is the case for all Server Wars, there will be no server saves, and so the server will revert back to pre Server War status. (All items and accounts created during Server Wars will be deleted.)\n" +
            "\n" +
            "I hope you will take this opportunity to mix-it-up with some PvP'ers from other shards that you may not know .. some of which may just surprise you ;-)\n" +
            "\n" +
            "As always; this is a Bob griefing, Player Killing, PvP madhouse.\n" +
            "If you are pregnant, have a bad back, suffer from motion sickness, or are too old and decrepit to appreciate such things, this may not be the event for you.\n" +
            "\n" +
            "How do I know Server Wars has begun?:\n" +
            "Server Wars are actually scheduled for {0:t}, but sometimes start from 3-5 minutes later. If you login at {3:t}, you should be safe.\n" +
            "If you want to be there from the moment it begins, login at {0:t} and simply wait for the \"Server Wars have begun!\" global announcement.\n" +
            "You will also see the global announcement \"Server Wars in progress...\" every 10 minutes during Server Wars.\n" +
            "\n" +
            "How to Connect:\n" +
            "IP: Tamke.net\n" +
            "Port: 2593\n" +
            "http://www.game-master.net\n" +
            "\n" +
            "See you on Sunday!\n" +
            "Adam Ant and the Angel Island team.\n" +
            "\n" +
            "PS. Please forward this to all your email and IM contacts especially those playing on other shards. And with the permission of your message board administrator, post this on your shards message board. This is a rare opportunity for you to represent your shard in a shard vs shard battle royal.\n" +
            "Represent!\n";

    }

    public class RansomQuestReminderMsg
    {
        public string m_subject = "Angel Island: Kin's Ransom Quest this {0:D}"; // Sunday, November 20th
        public string m_body =
            "We will be holding another Kin's Ransom Quest this {0:D} at {0:t} Pacific Time.\n" + // Sunday, November 20 at 12:00 noon.
            "\n" +
            "The 'Ransom Chest' will be located at one of the Kin Strongholds and will be filled with about 100K gold, lots premium armor pieces and weapons, and some other goodies like the finest leather dyes etc. \n" +
            "\n" +
            "There are no murder counts in the event area. The no-count zone will include the entire Kin Stronghold under siege.\n" +
            "\n" +
            "The event starts at {0:t} Pacific Time and the Chest will open at some point after the event starts.\n" + // 12:00 noon, 2:45 PM
            "You will want to come early to this large scale PvP event and try to hold the stronghold for your team.\n" +
            "Once any local NPC kin have been killed, you need only concern yourself with the other players that will be trying to take your treasure!\n" +
            "\n" +
            "Objective: I believe the primary tactic for one of these Kin Quests is to get a group of players together that share your alignment and attempt an overthrow of the stronghold; then hold it until the chest can be opened by human skills.\n" +
            "\n" +
            "The chest will require the talents of a highly skilled treasure hunter. At some point after the events starts, the magical protections about the chest shall be lifted, and the highly skilled treasure hunter will be able to open the chest.\n" +
            "\n" +
            "Note: This is a Bob griefing, Player Killing, PvP, event where there will be lots of death and dismemberment. If you are uncomfortable in such situations, please steer clear as this event is not for the faint of heart!\n" +
            "Oh, and if you don't yet have an account, this would be a great time to come make an account and just watch the event - even as a ghost.\n" +
            "\n" +
            "PS. ICQ all of your friends, and forward this email. We will have a big turnout for this event. \n" +
            "\n" +
            "How to Connect:\n" +
            "IP: Tamke.net\n" +
            "Port: 2593\n" +
            "http://www.game-master.net\n" +
            "\n" +
            "See you on Sunday!\n" +
            "Adam Ant and the Angel Island team.\n";
    }

    public class DonationReminderMsg
    {
        public string m_subject = "Remember to support Angel Island with a donation";
        public string m_body =
            "Please help keep Angel Island strong by helping us to pay the bills.\n" +
            "\n" +
            "The cost of running the Angel Island Server 24/7 costs $283.85 per month.\n" +
            "The donations are used to payback the actual hard-costs of setting up and maintaining our game server. \n" +
            "Ongoing maintenance, design, bug fixes, and new features are all donated by the Angel Island team.\n" +
            "\n" +
            //"Please see the Angel Island Balance Sheet for a breakdown of our costs.\n" + 
            //"http://www.game-master.net/pages/ai_balance.html\n" + 
            //" \n" + 
            "Keep Angel Island strong by making a donation today.  \n" +
            "You can also save yourself the hassle of monthly payments by setting up a recurring monthly donation.\n" +
            "\n" +
            "Donate any amount:\n" +
            "https://www.paypal.com/cgi-bin/webscr?cmd=_xclick&business=luke%40tomasello%2ecom&item_name=Make%20donation%20and%20keep%20Angel%20Island%20strong%21&item_number=206&no_shipping=0&no_note=1&tax=0&currency_code=USD&bn=PP%2dDonationsBF&charset=UTF%2d8\n" +
            "\n" +
            "Donate $5 per month:\n" +
            "https://www.paypal.com/cgi-bin/webscr?cmd=_xclick-subscriptions&business=luke%40tomasello%2ecom&item_name=Keep%20Angel%20Island%20strong%20with%20a%20monthly%20donation%2e&item_number=207&no_shipping=1&no_note=1&currency_code=USD&bn=PP%2dSubscriptionsBF&charset=UTF%2d8&a3=5%2e00&p3=1&t3=M&src=1&sra=1\n" +
            "\n" +
            "Donate $10 per month:\n" +
            "https://www.paypal.com/cgi-bin/webscr?cmd=_xclick-subscriptions&business=luke%40tomasello%2ecom&item_name=Angel%20Island%20Monthly%20Donation&item_number=205&no_shipping=1&no_note=1&currency_code=USD&bn=PP%2dSubscriptionsBF&charset=UTF%2d8&a3=10%2e00&p3=1&t3=M&src=1&sra=1\n" +
            "\n" +
            "Thank you,\n" +
            "Adam Ant and the Angel Island Team\n";
    }
}