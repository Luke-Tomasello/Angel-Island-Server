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

/* CHANGELOG
 * Server\PublishInfo.cs
 * 11/30/22, Adam
 *  Initial version
 */

using System;

namespace Server
{
    public static class PublishInfo
    {
        private static double m_publish = 0;            // set in RuleSets
        public static double Publish { get { return m_publish; } set { m_publish = value; } }

        /*	http://www.uoguide.com/Publishes
		 * Publishes are changes to a Shard's programming to fix bugs or add content. 
		 * Publishes may or may not be announced by the development team, depending on the type. 
		 * A major game update will always be announced (i.e. Publish 25), however, publishes may also occur invisibly during a 
		 * shard's maintenance to fix exploits and bugs.
		 * Originally, major publishes were known simply by the date on which they were released, 
		 * but in the time leading up to the announcement of UO:R, The Six Month Plan was announced. This laid out the future goal 
		 * of releasing large, regularly scheduled publishes. Publishes began being numbered internally, and the practice caught 
		 * on until publishes began being publically known by their number. The first publish to be mentioned by its number 
		 * publically was Publish 10, and the first publish to be officially titled by its number was the massive Publish 15.
		 * 
		 * Implementation note:
		 * note on version dates.
		 * a publish number if 64.0.2 is folded to 64.02
		 */

        public static DateTime PublishDate
        {
            get
            {
                // 2010
                if (m_publish == 68.3) return new DateTime(2010, 10, 28);
                // Halloween 2010, 13th Anniversary, Message in a Bottle changes, bug fixes
                if (m_publish == 68.2) return new DateTime(2010, 10, 15);
                // Fishing quest and ship fixes
                if (m_publish == 68.1) return new DateTime(2010, 10, 14);
                // Bug fixes
                if (m_publish == 68) return new DateTime(2010, 10, 12);
                // High Seas booster release, level 7 treasure maps and smooth sailing animation introduction
                if (m_publish == 67) return new DateTime(2010, 8, 4);
                // Treasure Map location randomization and new loot, Lockpicking and Bard Masteries changes.
                if (m_publish == 66.2) return new DateTime(2010, 6, 22);
                // In the Shadow of Virtue live event begun, addition of Endless Decanter of Water
                if (m_publish == 66) return new DateTime(2010, 5, 19);
                // More Human-to-Gargoyle weapon conversions, Bard Mastery System introduced, Player Memorials introduced, additional Advanced Character Templates, Throwing skill changes, bug fixes
                if (m_publish == 65) return new DateTime(2010, 4, 5);
                // Mysticism revamp, Gargoyle Racial Abilities update, Titles customization, additional Artifacts
                if (m_publish == 64.02) return new DateTime(2010, 3, 4);
                // Server crash and Faction fixes.
                if (m_publish == 64.01) return new DateTime(2010, 2, 12);
                // Valentines 2010, Seed Trading Box and Sarah the Exotic Goods Trader removed.
                if (m_publish == 64) return new DateTime(2010, 2, 10);
                // Substantial Item Insurance changes, new Gardening resources, Stygian Abyss encounter buffs, Seed Trading Box introduced, Mysticism changes.
                if (m_publish == 63.00) return new DateTime(2010, 1, 12); // mysteriously out of order Publish number
                                                                          // Introduced skin-rehue NPC vendor, fixed vendor skill/stat exploit.
                                                                          // 2009
                if (m_publish == 63.2) return new DateTime(2009, 12, 18);
                // Bug fixes for Paroxymus Swamp Dragons, auto-rezzing at login, various character appearance issues
                if (m_publish == 63.1) return new DateTime(2009, 12, 18);
                // Bug fixes for chicken coops and body-changing forms being stuck.
                if (m_publish == 63) return new DateTime(2009, 12, 17);
                // Imbuing changes, Holiday 2009, rennovated global Chat system
                if (m_publish == 62.37) return new DateTime(2009, 12, 7);
                // Several significant changes to the Imbuing skill.
                if (m_publish == 62.3) return new DateTime(2009, 11, 18);
                // Thanksgiving 2009 mini-event, general bug fixes
                if (m_publish == 62.2) return new DateTime(2009, 10, 29);
                // Halloween 2009 bug fixes
                if (m_publish == 62) return new DateTime(2009, 10, 22);
                // Halloween 2009 content, general bug fixes
                if (m_publish == 61.1) return new DateTime(2009, 10, 15);
                // Bug fixes for Stygian Abyss
                if (m_publish == 61) return new DateTime(2009, 10, 7);
                // Several bug fixes for Stygian Abyss, new Veteran Rewards, 12th Anniversary Gifts
                if (m_publish == 60.1) return new DateTime(2009, 9, 18);
                // Several bug fixes for Stygian Abyss
                if (m_publish == 60) return new DateTime(2009, 9, 8);
                // Stygian Abyss expansion launch
                if (m_publish == 59) return new DateTime(2009, 7, 14);
                // Ghost cam fixes, Shadowlord events, bug fixes.
                if (m_publish == 58.8) return new DateTime(2009, 6, 19);
                // Treasures of Tokuno re-activated, Quiver of Rage to be fixed.
                if (m_publish == 58) return new DateTime(2009, 3, 19);
                // Trial account limitations, various Champion Spawn bug fixes, reverted the Bag of Sending nerf from Publish 48, reduced weight of gold and silver.
                // 2008
                if (m_publish == 57) return new DateTime(2008, 12, 18);
                // New stealables for thieves, Scroll of Transcendence drops at Champion Spawns, Holiday 2008 gifts, Lumber skill requirement changes for Carpentry and several bug fixes.
                if (m_publish == 56) return new DateTime(2008, 10, 29);
                // War of Shadows and Halloween 2008 event content, new Magincia quests, new stackables, Factions updates, miscellaneous fixes
                if (m_publish == 55) return new DateTime(2008, 9, 10);
                // Spellweaving changes, new Veteran Rewards, additional Seed types, end of Spring Cleaning 2008
                if (m_publish == 54) return new DateTime(2008, 7, 11);
                // Faction bug fixes, Spring Cleaning 2008: Phase III, miscellaneous bug fixes
                if (m_publish == 53) return new DateTime(2008, 6, 10);
                // House resizing, various item and creature bug fixes, Spring Cleaning 2008: Phase II, ramping up of current events, misc. bug fixes
                if (m_publish == 52) return new DateTime(2008, 5, 6);
                // Faction fixes/changes, house placement and IDoC changes, Spring Cleaning 2008: Phase I, commas in checks, misc. bug fixes
                if (m_publish == 51) return new DateTime(2008, 3, 27);
                // Pet ball changes, pet AI improvements, various bug fixes
                if (m_publish == 50) return new DateTime(2008, 2, 14);
                // Improved runic intensities and BoD chances, greater dragons, BoS and Salvage Bag fixes, disabled Halloween 2007, activated Valentine's Day 2008 activities
                if (m_publish == 49) return new DateTime(2008, 1, 23);
                // Changes to the character database
                // 2007
                if (m_publish == 48) return new DateTime(2007, 11, 27);
                // Salvage Bag, Doom Gauntlet changes, Faction changes, Blood Oath fixes, Bag of Sending nerf, various monster strength/loot buffs
                if (m_publish == 47) return new DateTime(2007, 9, 25);
                // 10th Anniversary legacy dungeon drop system, 10th Anniversary gifts
                if (m_publish == 46) return new DateTime(2007, 8, 10);
                // PvP balances/changes, KR crafting menu functionality revamps, new KR macro functionality, resource randomization, various bug fixes
                if (m_publish == 45) return new DateTime(2007, 5, 25);
                // Stat gain changes and other miscellaneous changes
                if (m_publish == 44) return new DateTime(2007, 4, 30);
                // Discontinuation of the 3d client, New Player Experience, destruction of Old Haven, emergence of New Haven, Arms Lore changes
                // 2006
                if (m_publish == 43) return new DateTime(2006, 10, 26);
                // Contained 9th Anniversary additions, Evasion balancing, and Stealth/Detect Hidden changes
                if (m_publish == 42) return new DateTime(2006, 9, 1);
                // Added new Veteran Rewards, capped regeneration properties, and nerfed hit leech properties
                if (m_publish == 41) return new DateTime(2006, 6, 30);
                // Added the 4 Personal Attendants
                if (m_publish == 40) return new DateTime(2006, 5, 18);
                // Added Buff Bar, Targeting System, and various PvP related changes
                if (m_publish == 39) return new DateTime(2006, 2, 16);
                // New player improvements, 8x8 removal, and Spring D�cor Collection items added
                // 2005
                if (m_publish == 38) return new DateTime(2005, 12, 16);
                // Name/Gender Change Tokens, Holiday 2005 gifts, stats capped individually at 150
                if (m_publish == 37) return new DateTime(2005, 11, 3);
                // Many, many, many bug fixes for Mondain's Legacy and other long standing bugs
                if (m_publish == 36) return new DateTime(2005, 9, 22);
                // 8th Age anniversary items added and various Mondain's Legacy bug fixes
                if (m_publish == 35) return new DateTime(2005, 8, 10);
                // Mondain's Legacy support and Gamemaster support tool updates
                if (m_publish == 34) return new DateTime(2005, 7, 28);
                // Mondain's Legacy support
                if (m_publish == 33) return new DateTime(2005, 6, 21);
                // Change to buying Advanced Character Tokens, Evil Home Decor support, and shuts off Treasures of Tokuno
                if (m_publish == 32) return new DateTime(2005, 5, 2);
                // Necromancy potions and Exorcism, Alliance/Guild chat, end of The Britain Invasion, Special Moves fixes
                if (m_publish == 31) return new DateTime(2005, 3, 17);
                // Treasures of Tokuno I turn in begins, promo tokens, soulstone fragments, magery summons fixes, craftable spellbook properties, various bug fixes
                if (m_publish == 30) return new DateTime(2005, 2, 9);
                // Treasures of Tokuno I begins, item fixes, craftable Necromancy Scrolls and magic spellbooks, Yamandon gas attack, bug fixes
                if (m_publish == 29) return new DateTime(2005, 1, 20);
                // Damage numbers, pet fixes, archery/bola/lightning strike fixes, slayer changes
                // 2004
                if (m_publish == 28) return new DateTime(2004, 11, 23);
                // SE fixes, instanced corpses, 160.0 bard difficulty cap, fishing fixes, fame changes, new marties
                if (m_publish == 27) return new DateTime(2004, 9, 14);
                // Paragon and Minor Artifact systems
                if (m_publish == 26) return new DateTime(2004, 8, 17);
                // Loot changes, bardable creature changes, necromancer form fixes
                if (m_publish == 25) return new DateTime(2004, 7, 13);
                // Bug fixes, PvP balance changes, Archery fixes
                if (m_publish == 24) return new DateTime(2004, 5, 13);
                // Housefighting balances, reds in Fel guard zones, Valor spam fix, overloading fixes
                if (m_publish == 23) return new DateTime(2004, 3, 25);
                // The Character Transfer system
                // 2003
                if (m_publish == 22) return new DateTime(2003, 12, 17);
                // Holiday 2003 gifts, Sacred Journey fixes, bonded pet fixes, assorted other bug fixes
                if (m_publish == 21) return new DateTime(2003, 11, 25);
                // Housing/Vendor fixes, NPC economics, Factions fixes, Bulk Order Deed lockdown fix, various other fixes
                if (m_publish == 20) return new DateTime(2003, 10, 6);
                // New Vendor system, Bulletin Boards, Housing Runestones, the death of in-game HTML and UBWS bows
                if (m_publish == 19) return new DateTime(2003, 7, 30);
                // Quick Self-looting, BoD books, special move fixes/changes, housing lockdown fixes, pet and faction tweaks
                if (m_publish == 18) return new DateTime(2003, 5, 28);
                // AoS launch gifts, new wearables, customized housing fixes, Paladin/Necromancer balances
                if (m_publish == 17) return new DateTime(2003, 2, 11);
                // AoS launch publish and miscellaneous related changes
                // 2002
                if (m_publish == 16) return new DateTime(2002, 7, 12);
                // Resource/Crafting changes, Felucca Champion Spawns and Powerscrolls, Barding/Taming changes, Felucca enhancements/changes, House Ownership changes, the GGS system, the Justice Virtue, Siege Perilous ruleset changes
                if (m_publish == 15) return new DateTime(2002, 1, 9); // 
                /* http://www.uoguide.com/List_of_BNN_Articles_(2002)#Scenario_4:_Plague_of_Despair
				 * Scenario 5: When Ants Attack
				 *	Workers - October 3
				 *	Scientific Discussion - September 26
				 *	Orcs and Bombs - September 19
				 *	Crazy Miggie - September 12
				 *	I Think, Therefore I Dig - September 5
				 * Scenario 4: Plague of Despair
				 *	Epilogue - May 30
				 *	Plague of Despair - May 16
				 *	Preparations - May 9
				 *	Symptoms - May 2
				 *	Seeds - April 25
				 *	The Casting - April 18
				 *		(Dragon Scale Armor was introduced into the game during the first week of Scenerio Four.)
				 *		(http://noctalis.com/dis/uo/n-smit3.shtml)
				 *	Enemies and Allies - April 11 
				 * Scenario 3: Blackthorn's Damnation
				 *	The Watcher - January 25
				 *	Downfall to Power - January 17
				 *	Change - January 10
				 *	Inferno - January 2
				 */
                // Housing/Lockdown fixes, NPC and hireling fixes, various skill gump fixes, Faction updates, miscellaneous localizations
                // 2001
                if (m_publish == 14) return new DateTime(2001, 11, 30);
                // New Player Experience, context sensitive menus, Blacksmithing BoD system, Animal Lore changes, Crafting overhaul, Factions updates
                if (m_publish == 13.6) return new DateTime(2001, 10, 25);
                // Publish 13.6 (Siege Perilous Shards Only) - October 25, 2001
                if (m_publish == 13.5) return new DateTime(2001, 10, 11);
                // Commodity Deeds, Repair Contracts, Secure House Trades
                if (m_publish == 13) return new DateTime(2001, 8, 19);
                // Treasure map changes, tutorial/Haven changes, combat changes, with power hour changes and player owned barkeeps as later additions
                if (m_publish == 12) return new DateTime(2001, 7, 24);
                // Veteran Rewards, vendor changes, skill modification changes, GM rating tool, miscellaneous bug fixes and changes
                if (m_publish == 11) return new DateTime(2001, 3, 14);
                // The Ilshenar landmass, taxidermy kits, hair stylist NPCs, Item Identification changes, creatures vs. negative karma, vendor changes, various fixes and preparations for UO:TD
                if (m_publish == 10) return new DateTime(2001, 2, 1);
                // Henchman for Noble NPCs, disabling Hero/Evil, magic in towns, facet menus for public moongates, karma locking, faction fixes, spawn changes
                // 2000
                if (m_publish == 9) return new DateTime(2000, 12, 15);
                // T2A transport, Holiday gifts/tree activation, lockdown changes, default desktops, house add-on changes
                if (m_publish == 8) return new DateTime(2000, 12, 6);
                // The Factions System, stablemaster changes, monster movement changes
                if (m_publish == 7) return new DateTime(2000, 9, 3);
                // New Player Experience changes, lockdown/secure changes, comm. crystal changes, dungeon Khaldun, vendor customization
                if (m_publish == 6) return new DateTime(2000, 8, 1);
                // Looting rights changes, lockdown changes, stuck player options
                if (m_publish == 5) return new DateTime(2000, 4, 27);
                // Ultima Online: Renaissance, various updates and fixes for UO:R
                if (m_publish == 4) return new DateTime(2000, 3, 8);
                // Skill gain changes, Power Hour, sea serpents in fishing, bank checks, tinker traps, shopkeeper changes
                if (m_publish == 3) return new DateTime(2000, 2, 23);
                // Trade window changes, monsters trapped in houses, guild stone revamp, moonstones, secure pet/house trading, dex and healing
                if (m_publish == 2) return new DateTime(2000, 1, 24);
                // Escort and Taming changes, invalid house placement, land surveying tool, the death of precasting, Clean Up Britannia Phase III, item decay on boats
                // 1999
                if (m_publish == 1) return new DateTime(1999, 11, 23);
                // Co-owners, Maker's Mark, Perma-reds, Skill management, Clean Up Britannia Phase II, Bank box weight limit removal, Runebooks, Potion Kegs, other changes

                // Publish - September 22, 1999
                // Smelting, Unraveling, pet changes, Chaos/Order changes, armoire fix
                // UO Live Access Patch - August 25, 1999
                // Companion program, "Young" status, arm/disarm, last target, next target, TargetSelf macros, Ultima Messenger, various bug fixes
                // Publish - May 25, 1999
                // Difficulty-based Tinkering, "all follow me" etc., cut-up leather, boards from logs, other skill changes, dry-docking boats, various fixes
                // Publish - April 14, 1999
                // Targetting distance changes, trade window scam prevention, "I must consider my sins"
                // Publish - March 28, 1999
                // Long-term murder counts, Fishing resources, sunken treasure, craftable musical instruments, jewelcrafting, no more casting while hidden, new Stealth rules, ability to sell house deeds, house and boat optimizations
                // Publish - February 24, 1999
                // The Stealth and Remove Trap skills, changes to Detect Hidden and Forensic Evaluation, the Thieves Guild, new skill titles, tying Evaluating Intelligence to spell damage, dungeon treasure chests and dungeon traps, trash barrels, pet "orneriness," miscellaneous fixes and changes
                // Publish - February 2, 1999
                // Colored ore, granting karma, macing weapons destroying armor, Anatomy damage bonus, the Meditation skill, lockdown commands, blacksmith NPC guild, miscellaneous fixes
                // Publish - January 19, 1999
                // New Carpentry items, Fire Field banned from towns, Treasure Maps, Tailoring becomes difficulty-based, no more "a scroll," miscellaneous fixes
                // 1997 - 1998

                // not sure what the default should be, but assum it's 'new' therefore likely excluding stuff we are un sure about.
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Inclusive
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>true if this publish is active</returns>
        public static bool CheckPublish(double from, double to)
        {   // Note: this test is inclusive. The 'to' date needs be the last valid publish for this test.
            // For example. If purple wisps were only valid from publish 5 - 7, then the CheckPublish(5,7) would return true
            //	The 'to' date should NOT be the publish in which something became invalid.
            return Publish >= from && Publish <= to;
        }

        /// <summary>
        /// Is the named publish active?
        /// </summary>
        /// <param name="pub"></param>
        /// <returns>true if this publish is active</returns>
        public static bool CheckPublish(double pub)
        {
            return CheckPublish(pub, pub);
        }
    }
}