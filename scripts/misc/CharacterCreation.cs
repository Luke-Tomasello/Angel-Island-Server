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

/* Scripts/Misc/CharacterCreation.cs
 * ChangeLog
 *  10/29/2026, Adam (MusicLibrary)
 *      MusicLibrary.ReconnectPlayer (the music library moved out of MusicBox).
 *  7/29/2024, Adam
 *      1. Make the first administrator account AccessLevel.Owner
 *      2. Add a CoreManagementConsole and ClientManagementConsole to accounts >= AccessLevel.Administrator
 *  7/7/2023, Adam (temp code)
 *      Add a free ticket to "Galois: Live in Concert" for any characters created today.
 *      We can remove this code after 7/9 (it will auto-disable after this date.)
 *  5/1/23, Yoar
 *      Removed non-standard starting equipment for Siege Perilous
 *      Disabled Siege starting gear altogether. OSI did this in Pub16.
 *      Removed the UOR check prior to adding the standard loot. This way, we'll default to standard loot on Siege.
 *      Moved starter spyglass from standard loot to the AI/MO cases.
 *      Added pre-Pub13 skill items based on https://web.archive.org/web/20000308061402fw_/http://uo.stratics.com/skills.htm
 *  4/28/23, Yoar
 *      Added non-standard starting equipment for Siege Perilous
 *  4/25/23, Yoar
 *      In code, there's a distinction between the base starting loot and the skill-specific starting loot.
 *      Added additional checks to disable skill-specific starting loot on Siege.
 *  11/30/22, Adam
 *      Update new player starting loot based on 
 *      https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
 *  9/28/22, Adam (NewPlayerStartingArea)
 *      Everyone starts in the new NewPlayerStartingArea if that's the server setting. Otherwise NewCharacterSpawnLocation()
 *      Note: we may want this NewPlayerStartingArea to be based on (IP address?) and account age etc.
 *  9/4/22, Yoar (WorldZone)
 *      Rewrote starting area stuff.
 *      Added support for world zone starting cities.
 *  8/15/22, Adam
 *      Applies to SP and MO
 *      - return newbied things to being newbied. The https://www.uoguide.com/Siege_Perilous document seems to conflict with Jonathon Blade's comment.
 *      - remove a bag-of-reagents as default loot fo ALL characters.
 *      Character cleanup
 *           1. [all shards] save a list of things we want on our character
 *           2. [all shards] remove all duplicate items from character
 *           3. [mortalis] transfer account bankbox to new character
 *           4. [mortalis] remove items that exceed their budget (they have them stashed elsewhere.(bankbox, house, public containers, even packies)
 *           5. [mortalis] reequip them from their stashed items from the 'wants' list (again, ignoring duplicates.)
 *           6. TODO: handle stacked items (eg. ingots), and bags of stacked items (reagents.)
 *      More colorizing of Console text. Yellow for all things to do with Connecting/logging in/character creation, etc.
 *      Moved all this special logic to Scripts\Misc\MortalisCharacterFinalizations.cs
 *  8 /12/22, Adam
 *      Mortalis: Version 1 of new item budgeting
 *  8/11/22, Adam
 *     Core.RuleSets.MortalisRules(): since bankboxes are carried over, don't add stuff they already have in their carryover bankbox
 *  8/5/22, Adam
 *      Add an admin backdoor for Login Server administration.
 *      Any character created on this special account is automatically made administrator (Login Server only.).
 *  8/3/22, Adam
 *      Mortalis allows the player's bankbox to be transfered to the next character created on the account after death
 *      - we saved the bankbox serial on death
 *      - when creating a new character, we restore that bankbox for the player
 *  7/30/22, Yoar
 *      Added Mortalis starting gear (AddMortalItems)
 *  7/29/22, Yoar
 *      Updates for Mortalis
 *      - Now always do the standard character setup (SetStats, SetSkills)
 *      - A Mortalis server will additionally do the special Mortalis character setup (SetMortalStats, SetMortalSkills)
 *      - Reworked SetMortalStats, SetMortalSkills. Mortalis character setup now takes into account any chosen stats/skills.
 *  7/28/22, Adam 
 *      Various updates for Mortalis
 *      a. stats
 *      b. skills
 *      c. starting equip
 *  12/31/21, Adam (CreateMobile(Account a))
 *      Make Mobile CreateMobile() public to facilitate the creation of dummy accounts on the fly for testing
 *  11/29/21, Adam (SetSkills)
 *      1. do a better job at checking for invalid incoming skills (Necromancy, etc.) and log and send message to player.
 *      2. Default unsupported starting professions to one of the supported professions.
 * 9/6/2021, Adam (Tailoring)
 *      PackItem(new Scissors());
 *      Add code to packItem to not place duplicates in starting packs unless
 *      explicitly allowed for.
 *	3/6/11, Adam
 *		Add faction silver
 *	2/3/11, Adam
 *		Make characters for Core.UOIS mortal
 *	4/4/08, Adam
 *		Make several static functions public so we can build 'potion bags' for the potion stone
 *	2/26/08, Adam
 *		Remove setting of stat and skill caps from FillBankBox
 *  1/4/08, Adam
 *      Remove some old commented code (New Guild stuff)
 *	12/11/07, Pix
 *		Changed the new new char spawn spot to 5731, 953, 0 in Trammel.
 *	10/8/07, Adam
 *		- Return to spawning new players at WBB due to our very low pop.
 *		- We need to get pixie's new player starting area deco'ed before we can use it.
 *	8/26/07 - Pix
 *		Added new player starting area (dependent on NewPlayerStartingArea feature bit)
 *	8/20/06, Pix
 *		Re-enabled town selection on character creation.
 *		Left random spawn point if Britain is selected.
 *		Removed Oc'Nivelle rune.
 *	07/22/06, weaver
 *		Added newbie (regular) tent bag on character creation.
 *		Reformatted invalidly formatted changelog entries.
 *	05/30/06, Adam
 *		no more blessed reagents
 *	04/24/06, Kit
 *		Added random new character position spawn logic.
 *	09/14/05 Taran Kain
 *		Uncommented call to FillBankbox, added check for TC functionality
 *	9/12/05 - Pix
 *		Safeguarded against Paladin/Necro/etc creation.
 *	6/22/04, Old Salty
 * 		Added starting loot of TinkerTools for tinkers.
 *	6/19/04, Adam
 *		1. add the starting gold of 204 Pieces
 *		2. Add Bedroll for campers
 *		3. Add a SpyGlass to new players starting loot. 
 *		4. Comment out calls to FillBankbox
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/23/04, Pixie
 *		Switched it so all characters start at West Brit Bank.
 *	5/20/04, pixie
 *		Switched it so when user can choose which city to start in
 *		(assuming he/she chooses the "Advanced" character instead of smith/warrior/mage)
 *	5/3/04, mith
 *		Added rune to Oc'Nivelle in the AddBackpack() method.
 *		Added 60,000 of each leather to bankbox.
 *	4/13/04, mith
 *		Streamlined the creation of bankbox items. Fixed bug where they weren't newbied.
 *	3/30/04
 *		commented out lines 143 and 269 to remove fletcher tools
 *	3/28/04, Sambo
 *		added lines 55-342 added various items
 */

using Scripts.Misc;
using Server.Accounting;
using Server.Commands;
using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Consoles;
using Server.Engines.DataRecorder;
using Server.Engines.Seasons;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis.Deeds;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Server.Misc
{
    public partial class CharacterCreation
    {
        private static TimerStateCallback OnDeleteCharacter_Callback = new TimerStateCallback(OnDeleteCharacter);
        public enum Profession
        {
            None = 0,           // no template (error?)
            Warrior = 1,        // Core
            Magician = 2,       // Core
            Blacksmith = 3,     // Core
            Necromancer = 4,    // AOS+
            Paladin = 5,        // AOS+
            Samurai = 6,        // SE+
            Ninja = 7,          // SE+
            Custom = 255        // Custom
        }

        public static void Initialize()
        {
            // Register our event handler
            EventSink.CharacterCreated += new CharacterCreatedEventHandler(EventSink_CharacterCreated);
        }

        private static void EventSink_CharacterCreated(CharacterCreatedEventArgs args)
        {
            Mobile? newChar = CreateMobile(args.Account as Account);
            if (newChar == null)
            {
                Utility.Monitor.WriteLine("Login: {0}: Character creation failed, account full", ConsoleColor.Yellow,
                    args.State);
                return;
            }

            args.Mobile = newChar;
            m_Mobile = newChar;

            // Resolve Account and PlayerMobile once at the top
            Account? acct = args.Account as Account;
            PlayerMobile? player = newChar as PlayerMobile;

            if (acct == null || player == null)
            {
                Utility.Monitor.WriteLine(
                    $"Login: {args.State}: Character creation failed - missing account or PlayerMobile",
                    ConsoleColor.Red);
                return;
            }

            // create a special role for Login Server administrator
            bool LoginAdmin = Core.RuleSets.LoginServerRules() && args.State?.Address != null &&
                              Server.Commands.OwnerTools.IsOwnerIP(args.State.Address);

            if (LoginAdmin)
            {
                Utility.Monitor.WriteLine("Administrative account ({0}) logging into Login Server", ConsoleColor.Red,
                    args.State.Address);
                // exempt this account from infractions like total IP limiter
                acct.InfractionStatus = Account.AccountInfraction.None;
            }

            player.Player = true;

            // Setup owner / access level. First admin account created.
            player.AccessLevelInternal = LoginAdmin ? AccessLevel.Owner : acct.AccessLevel;

            player.Female = args.Female;
            player.Body = player.Female ? 0x191 : 0x190;
            player.Hue = Utility.ClipSkinHue(args.Hue & 0x3FFF) | 0x8000;
            player.Hunger = 20;

            // Adam: Mortalis - dangerous world
            if (Core.RuleSets.MortalisRules() && player.AccessLevel == AccessLevel.Player)
            {
                player.Mortal = true;
                player.Hidden = true;
            }

            player.Profession = args.Profession;

            // Pre-AOS shards do not support newer profession templates
            if (player.Profession > (int)Profession.Blacksmith && player.Profession < (int)Profession.Custom)
            {
                player.Profession = (int)Profession.Warrior;
                QueueWelcomeMessage(player,
                    "Your selected profession is not available on this shard and has been changed to Warrior.");
            }
            // Season 2 disables player magic
            else if (Core.RuleSets.SeasonsRules(2) && player.Profession == (int)Profession.Magician)
            {
                player.Profession = (int)Profession.Warrior;
                QueueWelcomeMessage(player,
                    "Player magic is disabled for this season. Your profession has been changed to Warrior.");
            }

            SetName(newChar, args.Name);
            Utility.AddBackpack(newChar);

            if (newChar.Name == DefaultName)
            {
                PersonalNameChangeDeed deed = new PersonalNameChangeDeed();
                deed.OwnerSerial = newChar.Serial;
                PackItem(deed, unique: true);
            }

            SetStats(newChar, args.Str, args.Dex, args.Int);
            SetSkills(newChar, args.Skills, args.Profession);
            AddStartingLoot(newChar);

            // Make all character items unique (prefer equipped over backpack)
            CleanupDuplicateItems(newChar, orEquivalent: true);

            AddHair(newChar, args.HairID, Utility.ClipHairHue(args.HairHue & 0x3FFF));
            AddBeard(newChar, args.BeardID, Utility.ClipHairHue(args.BeardHue & 0x3FFF));

            if (!Core.RuleSets.AOSRules() || (args.Profession != 4 && args.Profession != 5))
            {
                AddShirt(newChar, args.ShirtHue);
                AddPants(newChar, args.PantsHue);
                AddShoes(newChar);
            }

            #region Mortalis

            if (Core.RuleSets.MortalisRules())
            {
                #region BANKBOX

                // Mortalis carryover bankbox retrieval (GUID/OwnerGUID model)
                StrongBackpack? bbox = null;
                uint guid = Utility.MakeGuid(acct.Username, acct.Username);
                if (guid != 0)
                {
                    foreach (Item item in World.Items.Values)
                    {
                        if (item is StrongBackpack candidate && !candidate.Deleted && candidate.OwnerGUID == guid)
                        {
                            bbox = candidate;
                            break;
                        }
                    }
                }

                if (bbox != null)
                {
                    int fails = 0;
                    if (player.BankBox != null)
                    {
                        List<Item> list = new List<Item>(bbox.Items);
                        foreach (Item thing in list)
                        {
                            bbox.RemoveItem(thing);
                            if (!player.BankBox.TryDropItem(player, thing, sendFullMessage: false))
                            {
                                thing.Delete();
                                fails++;
                            }
                        }

                        if (fails > 0)
                        {
                            WelcomeTimer.ReplaceFor(player, new[]
                            {
                                "Your bank box is full and could not hold your carryover items."
                            });
                        }
                    }

                    bbox.Delete();
                }

                #endregion BANKBOX

                #region Guild

                if (acct.GuildStone != Serial.MinusOne)
                {
                    Guildstone? stone = World.FindItem(acct.GuildStone) as Guildstone;
                    if (stone != null)
                    {
                        if (stone.Guild.Leader == World.GetSystemAcct())
                        {
                            stone.Guild.AddMember(player);
                            stone.Guild.Leader = player;
                            stone.Guild.RemoveMember(World.GetSystemAcct());
                        }
                        else
                        {
                            stone.Guild.AddMember(player);
                        }
                    }

                    acct.GuildStone = Serial.MinusOne;
                }

                #endregion

                #region Notoriety

                // Mortalis carries soul’s reputation forward
                player.LongTermMurders = acct.LongTermMurders;
                player.ShortTermMurders = acct.ShortTermMurders;
                player.Karma = acct.Karma;
                player.Fame = acct.Fame;

                #endregion

                #region Music

                MusicLibrary.ReconnectPlayer(player);

                #endregion

                #region Relink Bounty Board and Restore Player Legacy Records

                Substitute? match = Substitute.Find(player.AcctGUID);

                if (match != null)
                {
                    Debug.Assert(match.Alive);

                    BountyKeeper.ReplaceAllPlacer(oldPlacer: match, newPlacer: player);
                    BountyKeeper.ReplaceAllWanted(oldWanted: match, newWanted: player);

                    DataRecorder.Replace(match, player);

                    Debug.Assert(!Substitute.InUseFor(player).Any());

                    if (!match.Deleted)
                        match.Delete();
                }

                #endregion

                #region Hair

                if (acct.HairHue != 0)
                {
                    Item? hair = newChar.FindItemOnLayer(Layer.Hair);
                    if (hair is Hair)
                        hair.Hue = acct.HairHue & 0x3FFF;
                }

                if (acct.BeardHue != 0)
                {
                    Item? beard = newChar.FindItemOnLayer(Layer.FacialHair);
                    if (beard is Beard)
                        beard.Hue = acct.BeardHue & 0x3FFF;
                }

                #endregion

                #region Backpack

                if (acct.BackpackHue != 0 && newChar.Backpack != null)
                    newChar.Backpack.Hue = acct.BackpackHue;

                #endregion
            }

            #endregion Mortalis

            // Fill bank box
            if (TestCenterServer.Enabled && !Core.GMNBETA_CFG)
            {
                if (Core.RuleSets.MortalisRules())
                {
                    if (newChar.BankBox == null || newChar.BankBox.Empty)
                        FillBankbox(newChar);
                }
                else
                {
                    FillBankbox(newChar);
                }
            }

            #region Reward Hue

            player.RewardHue = Utility.RewardHue(player);

            #endregion

            #region Mortalis style shards - starting weapon

            if (Core.RuleSets.MortalisStyleRules())
            {
                List<Item> equipped = Utility.GetEquippedItems(newChar);
                List<Item> backpackContents = Utility.GetContainerItems(newChar.Backpack, true);
                List<Item> allItems = new List<Item>(equipped.Count + backpackContents.Count);
                allItems.AddRange(equipped);
                allItems.AddRange(backpackContents);

                allItems.RemoveAll(i => i == null || i.Deleted);
                allItems = allItems.GroupBy(i => i.Serial).Select(g => g.First()).ToList();

                bool hasNonDaggerWeapon = allItems.OfType<BaseWeapon>().Any(w => !(w is Dagger));

                if (!hasNonDaggerWeapon)
                {
                    Item starterWeapon = newChar.Skills[SkillName.Swords].Base > newChar.Skills[SkillName.Fencing].Base
                        ? new Katana()
                        : new Kryss();

                    PackItem(starterWeapon, unique: false);
                }
            }

            #endregion

            #region Flag starting loot (Mortalis)

            if (Core.RuleSets.MortalisStyleRules())
            {
                List<Item> equipped = Utility.GetEquippedItems(newChar);
                List<Item> backpackContents = Utility.GetContainerItems(newChar.Backpack, true);
                List<Item> allItems = new List<Item>(equipped.Count + backpackContents.Count);
                allItems.AddRange(equipped);
                allItems.AddRange(backpackContents);

                allItems.RemoveAll(i => i == null || i.Deleted);
                allItems = allItems.GroupBy(i => i.Serial).Select(g => g.First()).ToList();

                foreach (Item item in allItems)
                {
                    item.FamilyID = KeyGenerator.Alloc();
                    acct.AddStarterFamilyId(item.FamilyID);
                }
            }

            #endregion

            #region Seasons 2 / Upheaval - finalize character

            if (Core.RuleSets.SeasonsRules() || Core.RuleSets.UpheavalRules())
            {
                List<Item> equipped = Utility.GetEquippedItems(newChar);
                List<Item> backpackContents = Utility.GetContainerItems(newChar.Backpack, true);

                List<Item> allItems = new List<Item>(equipped.Count + backpackContents.Count);
                allItems.AddRange(equipped);
                allItems.AddRange(backpackContents);

                allItems.RemoveAll(item => item == null || item.Deleted);

                allItems = allItems
                    .GroupBy(item => item.Serial)
                    .Select(group => group.First())
                    .ToList();

                int patched = 0;

                // Common seasonal startup:
                // reinforce starting equipment and top up bandages.
                foreach (Item item in allItems)
                {
                    if (BoostStartingDurability(item, 10))
                        patched++;
                }

                int existingBandages = Utility
                    .GetContainerItems(newChar.Backpack, true)
                    .OfType<Bandage>()
                    .Sum(bandage => bandage.Amount);

                if (existingBandages < 200)
                    PackItem(new Bandage(200 - existingBandages), unique: false);

                if (patched > 0)
                {
                    QueueWelcomeMessage(
                        player,
                        "Your starting equipment has been reinforced to last longer.");
                }

                // Season 2 only:
                // player magic is disabled.
                if (Core.RuleSets.SeasonsRules(2))
                {
                    int removed = 0;

                    foreach (Item item in allItems)
                    {
                        if (item.Deleted)
                            continue;

                        if (item is Spellbook || item is SpellScroll || item is ScribesPen)
                        {
                            item.Delete();
                            removed++;
                        }
                    }

                    if (removed > 0)
                    {
                        QueueWelcomeMessage(
                            player,
                            "Player magic is disabled for this season. Spellbooks and scrolls have been removed from your starting equipment.");
                    }
                }

                // provide and enforce the OrcishKinMask.
                if ((Core.RuleSets.SeasonsRules(2) || Core.RuleSets.UpheavalRules()))
                {
                    Item? helm = newChar.FindItemOnLayer(Layer.Helm);

                    if (helm is BaseHat && helm is not OrcishKinMask)
                        newChar.Backpack?.DropItem(helm);

                    if (newChar.FindItemOnLayer(Layer.Helm) is not OrcishKinMask)
                    {
                        OrcishKinMask mask = new OrcishKinMask
                        {
                            LootType = LootType.Newbied
                        };

                        // Upheaval: the mask is created after the starting equipment
                        // was reinforced above, so reinforce it the same way.
                        if (Core.RuleSets.UpheavalRules())
                            BoostStartingDurability(mask, 10);

                        if (!newChar.EquipItem(mask))
                        {
                            newChar.Backpack?.DropItem(mask);

                            if (acct.IsOrcEnemy)
                            {
                                QueueWelcomeMessage(
                                    player,
                                    $"The occupation forces know your face. Your disguise will not fool them for another {Account.FormatOrcEnemyDuration(acct)}.");
                            }
                            else
                            {
                                QueueWelcomeMessage(
                                    player,
                                    "You were given an orcish mask, but it could not be equipped. Please contact staff.");
                            }
                        }
                    }
                }
            }

            #endregion Seasons 2 / Upheaval - finalize character

            #region Starting Area
            string spawnTag = String.Empty;
            Point3D spawnLoc = Point3D.Zero;
            Map? spawnMap = null;
            if (CharacterCreationExceededAccountLimit(acct, player))
            {
                spawnTag = "Prison";
                spawnLoc = new Point3D(354, 836, 20);
                spawnMap = Map.Felucca;
                if (player.AccessLevel <= AccessLevel.Player)
                {
                    acct.InfractionStatus = Account.AccountInfraction.TooManyCharacters;
                    var aip = new Prison.PrisonPlayer(from: (PlayerMobile)World.GetAdminAcct(),
                        player,
                        Point3D.Zero, "Character imprisoned on login (too many characters)",
                        sentence: 3);
                    aip.GoToPrison();
                    Timer.DelayCall(TimeSpan.FromMinutes(5), OnDeleteCharacter_Callback, player);
                }
            }
            else
            {
                // Default spawn logic
                bool doDefaultSpawn = true;

                if (Core.RuleSets.MortalisRules() && IsJailOrPrison(newChar))
                    doDefaultSpawn = false;

                if (doDefaultSpawn)
                {
                    if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.NewPlayerStartingArea))
                    {
                        spawnTag = "New Player Starting Area";
                        spawnLoc = new Point3D(5731, 953, 0);
                        spawnMap = Map.Trammel;
                    }
                    else
                    {
                        NewCharacterSpawnLocation(args, newChar, out spawnTag, out spawnLoc, out spawnMap);
                    }
                }

                if (Core.RuleSets.MortalisRules())
                {
                    if (acct.HarmfulOffenses > 0)
                        GrieferEscalation(newChar);

                    RemoveDuplicateFromBackpackIfBanked<BaseBook>(newChar);
                    ApplyAccountHouseBans(player);
                    ApplyJailOrPrison(newChar);
                }

                if (doDefaultSpawn)
                    newChar.MoveToWorld(spawnLoc, spawnMap);
            }

            #endregion

            Utility.Monitor.WriteLine($"Login: {args.State}: New character being created (account={acct.Username})", ConsoleColor.Yellow);
            Utility.Monitor.WriteLine($" - Character: {newChar.Name} (serial={newChar.Serial})", ConsoleColor.Yellow);
            Utility.Monitor.WriteLine($" - Started: {spawnTag} {spawnLoc} {spawnMap}", ConsoleColor.Yellow);

            WelcomeTimer.SendGreeting(newChar, WelcomeTimer.GreetingType.NewPlayer);

            #region Test Center Gump
            if (Core.RuleSets.TestCenterRules())
                Timer.DelayCall(TimeSpan.FromSeconds(5), new TimerStateCallback(LaunchTestCenterGump), player);
            #endregion Test Center Gump

        }
        private static void LaunchTestCenterGump(object state)
        {
            if (state is PlayerMobile pm)
                pm.SendGump(new TestCenterGump(pm));
        }
        // 5/5/23, Yoar: Siege starter gear customization. New characters always start with pre-Pub13 skill gear.
        public static bool CustomSiegeGear { get { return Core.RuleSets.SiegeRules(); } }

        /* In Pub13.6, standard Siege gear was added.
         * https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
         * 
         * Standard Siege gear seemed to be abolished at some point, but I'm not sure when...
         * For now, I'm setting Pub16 to mark the end of standard Siege gear.
         */
        public static bool StandardSiegeGear
        {
            get
            {
                // deviation from era accuracy: we don't start with standard Siege gear
                if (CustomSiegeGear)
                    return false;

                return (Core.RuleSets.SiegeRules() && PublishInfo.Publish >= 13.6 && PublishInfo.Publish < 16.0);
            }
        }

        /* In Pub13, the amount of skill gear you started with was generally increased (especially for mages)
         * https://www.uoguide.com/Publish_13
         */
        public static bool Pub13SkillItems
        {
            get
            {
                // deviation from era accuracy: we always start with pre-Pub13 gear
                if (CustomSiegeGear)
                    return false;

                return (PublishInfo.Publish >= 13.0);
            }
        }

        public static Item MakeNewbie(Item item) //started editing here
        {
            item.LootType = LootType.Newbied;

            return item;
        }
        public static void PlaceItemIn(Container parent, int x, int y, Item item)
        {
            parent.AddItem(item);
            item.Location = new Point3D(x, y, 0);
        }
        public static Item MakePotionKeg(PotionEffect type, int hue)
        {
            PotionKeg keg = new PotionKeg();

            keg.Held = 100;
            keg.Type = type;
            keg.Hue = hue;

            return MakeNewbie(keg);
        }
        public static void FillBankbox(Mobile m)
        {   // adam: only called for test center characters
            BankBox bank = m.BankBox;

            if (bank == null)
                return;

            Container cont;

            // Begin box of money
            cont = new WoodenBox();
            cont.ItemID = 0xE7D;
            cont.Hue = 0x489;

            PlaceItemIn(cont, 16, 51, new BankCheck(1000000)); //edited by sam
            PlaceItemIn(cont, 28, 51, new BankCheck(250000)); //edited by sam
            PlaceItemIn(cont, 40, 51, new BankCheck(125000)); //edited by sam
            PlaceItemIn(cont, 52, 51, new BankCheck(75000)); //edited by sam
            PlaceItemIn(cont, 64, 51, new BankCheck(32500)); //edited by sam

            if (Core.Factions)
                PlaceItemIn(cont, 16, 115, new Factions.Silver(9000));

            PlaceItemIn(cont, 34, 115, MakeNewbie(new Gold(60000)));

            PlaceItemIn(bank, 18, 169, cont);
            // End box of money


            // Begin bag of potion kegs
            cont = new Backpack();
            cont.Name = "Various Potion Kegs";

            PlaceItemIn(cont, 45, 149, MakePotionKeg(PotionEffect.CureGreater, 0x2D));
            PlaceItemIn(cont, 69, 149, MakePotionKeg(PotionEffect.HealGreater, 0x499));
            PlaceItemIn(cont, 93, 149, MakePotionKeg(PotionEffect.PoisonDeadly, 0x46));
            PlaceItemIn(cont, 117, 149, MakePotionKeg(PotionEffect.RefreshTotal, 0x21));
            PlaceItemIn(cont, 141, 149, MakePotionKeg(PotionEffect.ExplosionGreater, 0x74));

            PlaceItemIn(cont, 93, 82, MakeNewbie(new Bottle(1000)));

            PlaceItemIn(bank, 53, 169, cont);
            // End bag of potion kegs


            // Begin bag of tools
            cont = new Bag();
            cont.Name = "Tool Bag";

            PlaceItemIn(cont, 30, 35, MakeNewbie(new TinkerTools(60000)));
            PlaceItemIn(cont, 90, 35, MakeNewbie(new DovetailSaw(60000)));
            PlaceItemIn(cont, 30, 68, MakeNewbie(new Scissors()));
            PlaceItemIn(cont, 45, 68, MakeNewbie(new MortarPestle(60000)));
            PlaceItemIn(cont, 75, 68, MakeNewbie(new ScribesPen(60000)));
            PlaceItemIn(cont, 90, 68, MakeNewbie(new SmithHammer(60000)));
            PlaceItemIn(cont, 30, 118, MakeNewbie(new TwoHandedAxe()));
            PlaceItemIn(cont, 90, 118, MakeNewbie(new SewingKit(60000)));

            PlaceItemIn(bank, 118, 169, cont);
            // End bag of tools


            // Begin bag of archery ammo
            cont = new Bag();
            cont.Name = "Bag Of Archery Ammo";

            PlaceItemIn(cont, 48, 76, MakeNewbie(new Arrow(60000)));
            PlaceItemIn(cont, 72, 76, MakeNewbie(new Bolt(60000)));

            PlaceItemIn(bank, 118, 124, cont);
            // End bag of archery ammo


            // Begin bag of treasure maps
            cont = new Bag();
            cont.Name = "Bag Of Treasure Maps";

            PlaceItemIn(cont, 30, 35, MakeNewbie(new TreasureMap(1, Map.Felucca)));
            PlaceItemIn(cont, 45, 35, MakeNewbie(new TreasureMap(2, Map.Felucca)));
            PlaceItemIn(cont, 60, 35, MakeNewbie(new TreasureMap(3, Map.Felucca)));
            PlaceItemIn(cont, 75, 35, MakeNewbie(new TreasureMap(4, Map.Felucca)));
            PlaceItemIn(cont, 90, 35, MakeNewbie(new TreasureMap(5, Map.Felucca)));

            PlaceItemIn(cont, 30, 50, MakeNewbie(new TreasureMap(1, Map.Felucca)));
            PlaceItemIn(cont, 45, 50, MakeNewbie(new TreasureMap(2, Map.Felucca)));
            PlaceItemIn(cont, 60, 50, MakeNewbie(new TreasureMap(3, Map.Felucca)));
            PlaceItemIn(cont, 75, 50, MakeNewbie(new TreasureMap(4, Map.Felucca)));
            PlaceItemIn(cont, 90, 50, MakeNewbie(new TreasureMap(5, Map.Felucca)));

            PlaceItemIn(cont, 55, 100, MakeNewbie(new Lockpick(60000)));
            PlaceItemIn(cont, 60, 100, MakeNewbie(new Pickaxe()));

            PlaceItemIn(bank, 98, 124, cont);
            // End bag of treasure maps


            // Begin bag of raw materials
            cont = new Bag();
            cont.Hue = 0x835;
            cont.Name = "Raw Materials Bag";

            PlaceItemIn(cont, 30, 35, MakeNewbie(new DullCopperIngot(60000)));
            PlaceItemIn(cont, 37, 35, MakeNewbie(new ShadowIronIngot(60000)));
            PlaceItemIn(cont, 44, 35, MakeNewbie(new CopperIngot(60000)));
            PlaceItemIn(cont, 51, 35, MakeNewbie(new BronzeIngot(60000)));
            PlaceItemIn(cont, 58, 35, MakeNewbie(new GoldIngot(60000)));
            PlaceItemIn(cont, 65, 35, MakeNewbie(new AgapiteIngot(60000)));
            PlaceItemIn(cont, 72, 35, MakeNewbie(new VeriteIngot(60000)));
            PlaceItemIn(cont, 79, 35, MakeNewbie(new ValoriteIngot(60000)));
            PlaceItemIn(cont, 86, 35, MakeNewbie(new IronIngot(60000)));

            PlaceItemIn(cont, 29, 55, MakeNewbie(new Leather(60000)));
            PlaceItemIn(cont, 44, 55, MakeNewbie(new SpinedLeather(60000)));
            PlaceItemIn(cont, 59, 55, MakeNewbie(new HornedLeather(60000)));
            PlaceItemIn(cont, 74, 55, MakeNewbie(new BarbedLeather(60000)));
            PlaceItemIn(cont, 35, 100, MakeNewbie(new Cloth(60000)));
            PlaceItemIn(cont, 67, 89, MakeNewbie(new Board(60000)));
            PlaceItemIn(cont, 88, 91, MakeNewbie(new BlankScroll(60000)));

            PlaceItemIn(bank, 98, 169, cont);
            // End bag of raw materials


            // Begin bag of spell casting stuff
            cont = new Backpack();
            cont.Hue = 0x480;
            cont.Name = "Spell Casting Stuff";

            PlaceItemIn(cont, 45, 105, new Spellbook(UInt64.MaxValue));

            Runebook runebook = new Runebook(10);
            runebook.CurCharges = runebook.MaxCharges;
            PlaceItemIn(cont, 105, 105, runebook);

            Item toHue = new BagOfReagents(65000);
            toHue.Hue = 0x2D;
            PlaceItemIn(cont, 45, 150, toHue);

            for (int i = 0; i < 9; ++i)
                PlaceItemIn(cont, 45 + (i * 10), 75, MakeNewbie(new RecallRune()));

            PlaceItemIn(bank, 78, 169, cont);
            // End bag of spell casting stuff
        }
        private static void AddShirt(Mobile m, int shirtHue)
        {
            int hue = Utility.ClipDyedHue(shirtHue & 0x3FFF);

            switch (Utility.Random(3))
            {
                case 0: EquipItem(new Shirt(hue), true); break;
                case 1: EquipItem(new FancyShirt(hue), true); break;
                case 2: EquipItem(new Doublet(hue), true); break;
            }
        }
        private static void AddPants(Mobile m, int pantsHue)
        {
            int hue = Utility.ClipDyedHue(pantsHue & 0x3FFF);

            if (m.Female)
            {
                switch (Utility.Random(2))
                {
                    case 0: EquipItem(new Skirt(hue), true); break;
                    case 1: EquipItem(new Kilt(hue), true); break;
                }
            }
            else
            {
                switch (Utility.Random(2))
                {
                    case 0: EquipItem(new LongPants(hue), true); break;
                    case 1: EquipItem(new ShortPants(hue), true); break;
                }
            }
        }
        private static void AddShoes(Mobile m)
        {
            EquipItem(new Shoes(Utility.RandomYellowHue()), true);
        }
        private static void AddHair(Mobile m, int itemID, int hue)
        {
            Item item;

            switch (itemID & 0x3FFF)
            {
                case 0x2044: item = new Mohawk(hue); break;
                case 0x2045: item = new PageboyHair(hue); break;
                case 0x2046: item = new BunsHair(hue); break;
                case 0x2047: item = new Afro(hue); break;
                case 0x2048: item = new ReceedingHair(hue); break;
                case 0x2049: item = new TwoPigTails(hue); break;
                case 0x204A: item = new KrisnaHair(hue); break;
                case 0x203B: item = new ShortHair(hue); break;
                case 0x203C: item = new LongHair(hue); break;
                case 0x203D: item = new PonyTail(hue); break;
                default: return;
            }

            m.AddItem(item);
        }
        private static void AddBeard(Mobile m, int itemID, int hue)
        {
            if (m.Female)
                return;

            Item item;

            switch (itemID & 0x3FFF)
            {
                case 0x203E: item = new LongBeard(hue); break;
                case 0x203F: item = new ShortBeard(hue); break;
                case 0x2040: item = new Goatee(hue); break;
                case 0x2041: item = new Mustache(hue); break;
                case 0x204B: item = new MediumShortBeard(hue); break;
                case 0x204C: item = new MediumLongBeard(hue); break;
                case 0x204D: item = new Vandyke(hue); break;
                default: return;
            }

            m.AddItem(item);
        }
        public static Mobile? CreateMobile(Account a)
        {
            for (int i = 0; i < 5; ++i)
                if (a[i] == null || a[i].Deleted)
                    return (a[i] = new PlayerMobile(a.GUID));

            return null;
        }

        private static void _StandardLoot(Mobile newChar)
        {
            PackItem(new RedBook("a book", newChar.Name, 20, true), unique: true);

            PackItem(new Gold(100), unique: true);

            PackItem(new Candle(), unique: true);

            PackItem(new Dagger(), unique: true);
        }
        private static void AddStartingLoot(Mobile newChar)
        {
            if (newChar.AccessLevel >= AccessLevel.Administrator)
            {
                PackItem(new CoreManagementConsole(), unique: true);
                PackItem(new ClientManagementConsole(), unique: true);
            }

            if (Core.RuleSets.AngelIslandRules())
            {
                _StandardLoot(newChar);

                // character farming for gold mitigation. 
                //  We should probably just limit new account creation to one a week or something. (Family member issue?)
                RemoveGold(newChar);

                // Can only be cached by assignee in most shops
                CivicBond check = new CivicBond(newChar.Serial, 1000, CivicBondKeys.UnemploymentCheck, 0x4CC, "unemployment check");
                PackItem(check, unique: true);

                #region [NEW] Tower
                /*
                // [NEW] Tower
                RecallRune ocRune = new RecallRune();
                ocRune.Target = new Point3D(1688, 1422, 0);
                ocRune.TargetMap = Map.Felucca;
                ocRune.Description = "[NEW] Tower";
                ocRune.Marked = true;
                PackItem(ocRune);

                // add a book describing new
                PackItem(NewBook());
                */
                #endregion [NEW] Tower

                PackItem(new Spyglass(), unique: true);
                PackItem(new TentBag(), unique: true);
            }
            else if (StandardSiegeGear)
            {
                /*
                 * Newbie & Training
                 * Starting equipment for new characters on Siege Perilous will be:
                 * Clothing, as normal (�newbie items�)
                 * 1 dagger (�newbie items�)
                 * 100 gold coins
                 * 25 ingots
                 * 1 shovel
                 * 1 bag, with 10 of each reagent
                 * 1 katana
                 * https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
                 */
                PackItem(new Dagger(), unique: true);
                PackItem(new Gold(100), unique: true);
                PackItem(new IronIngot(25), unique: true);
                PackItem(new Shovel(), unique: true);
                PackItem(new BagOfReagents(10), unique: true);
                PackItem(new Katana(), unique: true);
            }
            else if (Core.RuleSets.MortalisStyleRules())
            {
                _StandardLoot(newChar);

                if (Core.RuleSets.MortalisRules())
                    // character farming for gold mitigation. 
                    RemoveGold(newChar);

                // add a CivicBond. Can only be cached by assignee for training
                if (!(Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules()))
                {
                    CivicBond voucher = new CivicBond(newChar.Serial, 1000, CivicBondKeys.TrainingVoucher, 0x4CC,
                        "training voucher");
                    voucher.OwnerSerial = newChar.Serial;
                    PackItem(voucher, unique: true);
                }

                // ai special loot

                PackItem(new Spyglass(), unique: true);

                //rolled up tent
                PackItem(new TentBag(), unique: true);

                //bandages
                PackItem(new Bandage(30), unique: true);

                //food
                PackItem(new Ham(), unique: true);
                PackItem(new CheeseWheel(), unique: true);
                PackItem(new Pitcher(BeverageType.Water), unique: true);

                //scissors
                PackItem(new Scissors(), unique: true);

                // Yoar: Class specific items are given to the mobile in 'AddMortalItems'
            }
            else
            {
                _StandardLoot(newChar);
            }
        }
        private static void RemoveGold(Mobile newChar)
        {
            List<Item> BackPacklist = Utility.GetBackpackItems(newChar);

            foreach (Item item1 in BackPacklist)
            {
                if (item1 == null || item1.Deleted)
                    continue;
                if (item1.GetType() == typeof(Gold))
                    item1.Delete();
            }
        }

        public static void NewCharacterSpawnLocation(CharacterCreatedEventArgs? args, Mobile m, out string spawnTag, out Point3D spawnLoc, out Map spawnMap)
        {
            // Default: normal spawn at West Britain Bank
            spawnTag = "West Britain Bank";
            spawnMap = Map.Felucca;
            var acct = m.Account as Server.Accounting.Account;
            bool grifer = Core.RuleSets.MortalisRules() ? (acct != null && acct.HarmfulOffenses > 0) : false;

            // WBB rectangle
            Rectangle2D spawnRect = new Rectangle2D(
                new Point2D(1417, 1693),
                new Point2D(1442, 1700)); // WBB

            // Default to WBB center; loop below may refine this
            spawnLoc = new Point3D(1420, 1698, 0);

            // If not Mortalis or Seasons, or args is null, or not a PlayerMobile, or player is a murderer,
            // keep the existing behavior (WBB / Mortalis murderer spawn).
            bool selectStartingCity = Core.RuleSets.MortalisRules() || Core.RuleSets.SeasonsRules() || Core.RuleSets.UpheavalRules();
            PlayerMobile? pm = m as PlayerMobile;

            if (!selectStartingCity || args == null || pm == null || (pm.IsMurderer || grifer))
            {
                // Mortalis murderer special case
                if (selectStartingCity && pm != null && (pm.IsMurderer || grifer))
                {
                    // Centered at (2727, 2133, 5), roughly same footprint as WBB rect
                    spawnTag = "Mortalis Murderer Spawn";
                    spawnMap = Map.Felucca;

                    spawnRect = new Rectangle2D(
                        new Point2D(2714, 2129),  // 2727 - 13, 2133 - 4
                        new Point2D(2740, 2137)); // 2727 + 13, 2133 + 4

                    spawnLoc = new Point3D(2727, 2133, 5);
                }

                // Try to find a valid spawn point inside the chosen rectangle
                for (int i = 0; i < 20; i++)
                {
                    int x = spawnRect.X + Utility.Random(spawnRect.Width);
                    int y = spawnRect.Y + Utility.Random(spawnRect.Height);

                    if (spawnMap.CanSpawnLandMobile(x, y, 0))
                    {
                        spawnLoc = new Point3D(x, y, 0);
                        break;
                    }

                    int z = spawnMap.GetAverageZ(x, y);

                    if (spawnMap.CanSpawnLandMobile(x, y, z))
                    {
                        spawnLoc = new Point3D(x, y, z);
                        break;
                    }
                }

                return;
            }

            // Mortalis, args not null, non-murderer: spawn at the selected starting city.
            CityInfo city = args.City;

            if (city != null)
            {
                // Use city data directly
                spawnMap = city.Map ?? Map.Felucca;
                spawnLoc = city.Location;
                spawnTag = city.Building; // or City?

                // Small radius around the city location so players do not stack on a single tile.
                const int radiusX = 4;
                const int radiusY = 4;

                spawnRect = new Rectangle2D(
                    new Point2D(spawnLoc.X - radiusX, spawnLoc.Y - radiusY),
                    new Point2D(spawnLoc.X + radiusX, spawnLoc.Y + radiusY));

                for (int i = 0; i < 20; i++)
                {
                    int x = spawnRect.X + Utility.Random(spawnRect.Width);
                    int y = spawnRect.Y + Utility.Random(spawnRect.Height);

                    if (spawnMap.CanSpawnLandMobile(x, y, 0))
                    {
                        spawnLoc = new Point3D(x, y, 0);
                        break;
                    }

                    int z = spawnMap.GetAverageZ(x, y);

                    if (spawnMap.CanSpawnLandMobile(x, y, z))
                    {
                        spawnLoc = new Point3D(x, y, z);
                        break;
                    }
                }
            }
            else
            {
                // Fallback: no city info for some reason, use existing WBB behavior.
                for (int i = 0; i < 20; i++)
                {
                    int x = spawnRect.X + Utility.Random(spawnRect.Width);
                    int y = spawnRect.Y + Utility.Random(spawnRect.Height);

                    if (spawnMap.CanSpawnLandMobile(x, y, 0))
                    {
                        spawnLoc = new Point3D(x, y, 0);
                        break;
                    }

                    int z = spawnMap.GetAverageZ(x, y);

                    if (spawnMap.CanSpawnLandMobile(x, y, z))
                    {
                        spawnLoc = new Point3D(x, y, z);
                        break;
                    }
                }
            }
        }

        private static void FixStats(ref int str, ref int dex, ref int intel)
        {
            int vStr = str - 10;
            int vDex = dex - 10;
            int vInt = intel - 10;

            if (vStr < 0)
                vStr = 0;

            if (vDex < 0)
                vDex = 0;

            if (vInt < 0)
                vInt = 0;

            int total = vStr + vDex + vInt;

            if (total == 0 || total == 50)
                return;

            double scalar = 50 / (double)total;

            vStr = (int)(vStr * scalar);
            vDex = (int)(vDex * scalar);
            vInt = (int)(vInt * scalar);

            FixStat(ref vStr, (vStr + vDex + vInt) - 50);
            FixStat(ref vDex, (vStr + vDex + vInt) - 50);
            FixStat(ref vInt, (vStr + vDex + vInt) - 50);

            str = vStr + 10;
            dex = vDex + 10;
            intel = vInt + 10;
        }

        private static void FixStat(ref int stat, int diff)
        {
            stat += diff;

            if (stat < 0)
                stat = 0;
            else if (stat > 50)
                stat = 50;
        }

        private static void SetStats(Mobile m, int str, int dex, int intel)
        {
            FixStats(ref str, ref dex, ref intel);

            if (str < 10 || str > 60 || dex < 10 || dex > 60 || intel < 10 || intel > 60 || (str + dex + intel) != 80)
            {
                str = 10;
                dex = 10;
                intel = 10;
            }

            m.InitStats(str, dex, intel);
        }
        public const string DefaultName = "Generic Player";
        private static void SetName(Mobile m, string name)
        {
            if (m == null)
                return;

            name = (name ?? string.Empty).Trim();

            bool allowDigits = !Core.RuleSets.MortalisStyleRules();
            if (!NameVerification.Validate(name, 2, 16, true, allowDigits, true, 1,
                    NameVerification.SpaceDashPeriodQuote))
            {
                name = DefaultName;
            }

            // Mortalis/Seasons
            if (Core.RuleSets.MortalisStyleRules())
            {
                var acct = m.Account as Server.Accounting.Account;
                if (acct == null)
                {
                    name = DefaultName;
                }
                else if (name != DefaultName && NameVerification.IsNameConflict(m, name, out var self))
                {
                    string requestedName = name;

                    if (self)
                        QueueWelcomeMessage(m, $"The name '{requestedName}' is already in use by another character on your account.");
                    else
                        QueueWelcomeMessage(m, $"The name '{requestedName}' is already reserved by another account.");

                    name = DefaultName;
                }
                else if (name != DefaultName && !string.IsNullOrEmpty(name))
                {
                    // Assign or reaffirm reservation in the shared reserved-name registry.
                    ReservedNames.Add(acct.Username, name);
                }
            }

            // Ensure this account has a valid seasonal leaderboard identity.
            // If the seasonal account already exists, this will populate DisplayName
            // once the player has chosen a real name.
            if (Core.RuleSets.SeasonsRules())
                SeasonManager.EnsureSeasonalAccount(m.AcctGUID, name);

            m.Name = name;
        }

        private static bool ValidSkills(SkillNameValue[] skills)
        {
            int total = 0;

            for (int i = 0; i < skills.Length; ++i)
            {
                if (skills[i].Value < 0 || skills[i].Value > 50)
                    return false;

                total += skills[i].Value;

                for (int j = i + 1; j < skills.Length; ++j)
                {
                    if (skills[j].Value > 0 && skills[j].Name == skills[i].Name)
                        return false;
                }
            }

            return (total == 100);
        }

        private static Mobile m_Mobile;

        private static void SetSkills(Mobile m, SkillNameValue[] skills, int prof)
        {
            Profession profession = (Profession)prof;

            switch (profession)
            {
                case Profession.Warrior:
                case Profession.Paladin:        // defaults to Warrior on non-AOS
                    skills = new SkillNameValue[]
                    {
                new SkillNameValue(SkillName.Anatomy, 30),
                new SkillNameValue(SkillName.Healing, 45),
                new SkillNameValue(SkillName.Swords, 35),
                new SkillNameValue(SkillName.Tactics, 50)
                    };
                    break;

                case Profession.Magician:
                case Profession.Necromancer:    // defaults to Magician on non-AOS
                    skills = new SkillNameValue[]
                    {
                new SkillNameValue(SkillName.EvalInt, 30),
                new SkillNameValue(SkillName.Wrestling, 30),
                new SkillNameValue(SkillName.Magery, 50),
                new SkillNameValue(SkillName.Meditation, 50)
                    };
                    break;

                case Profession.Blacksmith:
                    skills = new SkillNameValue[]
                    {
                new SkillNameValue(SkillName.Mining, 30),
                new SkillNameValue(SkillName.ArmsLore, 30),
                new SkillNameValue(SkillName.Blacksmith, 50),
                new SkillNameValue(SkillName.Tinkering, 50)
                    };
                    break;

                default:
                    if (!ValidSkills(skills))
                        return;
                    break;
            }

            bool addSkillItems = true;

            // Apply profession-specific starting gear
            switch (profession)
            {
                case Profession.Warrior:
                    if (!StandardSiegeGear)
                        EquipItem(new LeatherChest());
                    break;
            }

            // === Skill Validation & Application ===
            bool hasBadSkill = false;
            var badSkills = new HashSet<SkillName>
            {
                SkillName.RemoveTrap,
                SkillName.Stealth,
                SkillName.Fletching
            };

            if (Core.RuleSets.SeasonsRules(2))
                badSkills.Add(SkillName.Inscribe);

            for (int i = 0; i < skills.Length; ++i)
            {
                SkillNameValue snv = skills[i];

                if (!Enum.IsDefined(typeof(SkillName), snv.Name))
                {
                    LogBadSkill(m, snv.Name.ToString());
                    hasBadSkill = true;
                    continue;
                }

                if (snv.Value <= 0)
                    continue;

                Skill skill = m.Skills[snv.Name];

                if (badSkills.Contains(skill.SkillName))
                {
                    LogBadSkill(m, skill.SkillName.ToString());
                    hasBadSkill = true;
                    continue;
                }

                // Apply valid skill
                skill.BaseFixedPoint = snv.Value * 10;

                if (!StandardSiegeGear && addSkillItems)
                {
                    if (Pub13SkillItems)
                        AddSkillItems(snv.Name, skills);
                    else
                        AddSkillItemsPrePub13(snv.Name);
                }
            }

            if (hasBadSkill)
            {
                QueueWelcomeMessage(m, "You selected one or more skills not supported on this server.");
                QueueWelcomeMessage(m, "Please contact a GM and we will reimburse you those skill points.");
            }
        }

        private static void LogBadSkill(Mobile m, string skillName)
        {
            LogHelper logger = new LogHelper("New Player Skill Unavailable.log", false);
            try
            {
                logger.Log(LogType.Mobile, m, skillName);
            }
            finally
            {
                logger.Finish();
            }
        }

        private static void EquipItem(Item item, bool mustEquip = false, bool unique = false)
        {
            // for now, all shards get newbied loot (if Newbieable)
            if (Newbieable(item))
                item.LootType = LootType.Newbied;

            // if you pick two skills that have the same item (veterinary and tailoring: Scissors) 
            //  don't pack the duplicate item
            // Note: duplicates are allowed if you set unique = false
            if (unique && HasItemOfType(item.GetType()))
            {
                item.Delete();
                return;
            }

            if (m_Mobile != null && m_Mobile.EquipItem(item))
                return;

            Container pack = m_Mobile.Backpack;

            if (!mustEquip && pack != null)
                pack.DropItem(item);
            else
                item.Delete();
        }
        private static bool Newbieable(Item? item)
        {
            if (item == null || item.Deleted)
                return false;
            // we don't care about starting gold as our stacking rules prevent
            //  stacking to create more newbie gold. In fact, this applies to all stackables.
            if (item is Container)
                // bag or reagents
                return false;

            return true;
        }
        private static void PackItem(Item item, bool unique = true, LootType lootType = LootType.Unspecified)
        {
            if (item == null || item.Deleted)
                return;

            if (lootType == LootType.Unspecified)
            {   // for now, all shards get newbied loot (if Newbieable)
                if (Newbieable(item))
                    item.LootType = LootType.Newbied;
            }
            else
                item.LootType = lootType;

            Container pack = m_Mobile.Backpack;
            if (pack == null)
            {
                item.Delete();
                return;
            }

            // if you pick two skills that have the same item (veterinary and tailoring: Scissors) 
            //  don't pack the duplicate item
            // Note: duplicates are allowed if you set unique = false
            if (unique && HasItemOfType(item.GetType()))
            {
                item.Delete();
                return;
            }

            pack.DropItem(item);
        }
        private static bool HasItemOfType(Type type)
        {
            Container pack = m_Mobile.Backpack;

            if (pack != null)
            {
                foreach (Item item in pack.Items)
                {
                    if (item.GetType() == type)
                        return true;
                }
            }

            foreach (Item item in m_Mobile.Items)
            {
                if (item.GetType() == type)
                    return true;
            }

            return false;
        }
        private static void PackInstrument()
        {
            switch (Utility.Random(6))
            {
                case 0: PackItem(new Drums(), unique: true); break;
                case 1: PackItem(new Harp(), unique: true); break;
                case 2: PackItem(new LapHarp(), unique: true); break;
                case 3: PackItem(new Lute(), unique: true); break;
                case 4: PackItem(new Tambourine(), unique: true); break;
                case 5: PackItem(new TambourineTassel(), unique: true); break;
            }
        }
        private static readonly int[] m_SpellIDs = new int[]
            {
                0, 1, 2, 3, 4, 5, 6, 7
            };
        private static void PackScroll(int circle, int stacks = 1)
        {
            Utility.Shuffle(m_SpellIDs);

            if (stacks > m_SpellIDs.Length)
                stacks = m_SpellIDs.Length;

            for (int i = 0; i < stacks; i++)
                PackScrollByID(m_SpellIDs[i] + circle * 8);
        }
        private static void PackScrollByID(int id)
        {
            switch (id)
            {
                case 0: PackItem(new ClumsyScroll(), unique: true); break;
                case 1: PackItem(new CreateFoodScroll(), unique: true); break;
                case 2: PackItem(new FeeblemindScroll(), unique: true); break;
                case 3: PackItem(new HealScroll(), unique: true); break;
                case 4: PackItem(new MagicArrowScroll(), unique: true); break;
                case 5: PackItem(new NightSightScroll(), unique: true); break;
                case 6: PackItem(new ReactiveArmorScroll(), unique: true); break;
                case 7: PackItem(new WeakenScroll(), unique: true); break;
                case 8: PackItem(new AgilityScroll(), unique: true); break;
                case 9: PackItem(new CunningScroll(), unique: true); break;
                case 10: PackItem(new CureScroll(), unique: true); break;
                case 11: PackItem(new HarmScroll(), unique: true); break;
                case 12: PackItem(new MagicTrapScroll(), unique: true); break;
                case 13: PackItem(new MagicUnTrapScroll(), unique: true); break;
                case 14: PackItem(new ProtectionScroll(), unique: true); break;
                case 15: PackItem(new StrengthScroll(), unique: true); break;
                case 16: PackItem(new BlessScroll(), unique: true); break;
                case 17: PackItem(new FireballScroll(), unique: true); break;
                case 18: PackItem(new MagicLockScroll(), unique: true); break;
                case 19: PackItem(new PoisonScroll(), unique: true); break;
                case 20: PackItem(new TelekinisisScroll(), unique: true); break;
                case 21: PackItem(new TeleportScroll(), unique: true); break;
                case 22: PackItem(new UnlockScroll(), unique: true); break;
                case 23: PackItem(new WallOfStoneScroll(), unique: true); break;
            }
        }
        private static Item NecroHue(Item item)
        {
            item.Hue = 0x2C3;

            return item;
        }
        private static void HeaviestWeapon()
        {
            List<Item> best = new() { new Kryss(), new Katana(), new Club(), new Longsword() };
            best.Sort((p, q) => q.Weight.CompareTo(p.Weight));
            Item? selected = null;
            foreach (Item item in best)
                if (item.CanEquip(m_Mobile))
                {
                    selected = item;
                    break;

                }

            foreach (Item item in best)
                if (item == selected)
                    continue;
                else
                    item.Delete();

            EquipItem(selected);
        }
        private static readonly int[] m_ReagentIDs = new int[]
            {
                0, 1, 2, 3, 4, 5, 6, 7
            };
        private static void PackReagent(int stacks, int amount)
        {
            Utility.Shuffle(m_ReagentIDs);

            if (stacks > m_ReagentIDs.Length)
                stacks = m_ReagentIDs.Length;

            for (int i = 0; i < stacks; i++)
                PackReagentByID(m_ReagentIDs[i], amount);
        }
        private static void PackReagentByID(int id, int amount)
        {
            switch (id)
            {
                case 0: PackItem(new BlackPearl(amount), unique: true); break;
                case 1: PackItem(new Bloodmoss(amount), unique: true); break;
                case 2: PackItem(new Garlic(amount), unique: true); break;
                case 3: PackItem(new Ginseng(amount), unique: true); break;
                case 4: PackItem(new MandrakeRoot(amount), unique: true); break;
                case 5: PackItem(new Nightshade(amount), unique: true); break;
                case 6: PackItem(new SulfurousAsh(amount), unique: true); break;
                case 7: PackItem(new SpidersSilk(amount), unique: true); break;
            }
        }
        private static void PackPracticeWeapon()
        {
            switch (Utility.Random(8))
            {
                case 0: PackItem(new ShepherdsCrook(), unique: false); break;
                case 1: PackItem(new Bow(), unique: false); break;
                case 2: PackItem(new GnarledStaff(), unique: false); break;
                case 3: PackItem(new Spear(), unique: false); break;
                case 4: PackItem(new Hatchet(), unique: false); break;
                case 5: PackItem(new Mace(), unique: false); break;
                case 6: PackItem(new Longsword(), unique: false); break;
                case 7: PackItem(new SkinningKnife(), unique: false); break;
            }
        }
        private static void PackPotion()
        {
            // 5/1/23, Yoar: Let's not give them an explosion potion...
            switch (Utility.Random(7))
            {
                case 0: PackItem(new AgilityPotion(), unique: false); break;
                case 1: PackItem(new LesserCurePotion(), unique: false); break;
                case 2: PackItem(new LesserHealPotion(), unique: false); break;
                case 3: PackItem(new LesserPoisonPotion(), unique: false); break;
                case 4: PackItem(new NightSightPotion(), unique: false); break;
                case 5: PackItem(new RefreshPotion(), unique: false); break;
                case 6: PackItem(new StrengthPotion(), unique: false); break;
            }
        }
        private static void PackRandomTinkerComponent()
        {
            switch (Utility.Random(7))
            {
                case 0: PackItem(new Gears(), unique: true); break;
                case 1: PackItem(new ClockParts(), unique: true); break;
                case 2: PackItem(new BarrelTap(), unique: true); break;
                case 3: PackItem(new Springs(), unique: true); break;
                case 4: PackItem(new SextantParts(), unique: true); break;
                case 5: PackItem(new BarrelHoops(), unique: true); break;
                case 6: PackItem(new Hinge(), unique: true); break;
            }
        }
        private static void AddSkillItems(SkillName skill, SkillNameValue[] skills)
        {
            switch (skill)
            {
                case SkillName.Alchemy:
                    {
                        PackItem(new Bottle(4), unique: true);
                        PackItem(new MortarPestle(), unique: true);
                        EquipItem(new Robe(Utility.RandomPinkHue()));
                        break;
                    }
                case SkillName.Anatomy:
                    {
                        PackItem(new Bandage(3), unique: true);
                        PackItem(new Scissors(), unique: true);
                        EquipItem(new Robe(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.AnimalLore:
                    {
                        EquipItem(new ShepherdsCrook());
                        EquipItem(new Robe(Utility.RandomBlueHue()));
                        break;
                    }
                case SkillName.Archery:
                    {
                        PackItem(new Arrow(25), unique: true);
                        EquipItem(new Bow());
                        break;
                    }
                case SkillName.ArmsLore:
                    {
                        HeaviestWeapon();
                        break;
                    }
                case SkillName.Begging:
                    {
                        EquipItem(new GnarledStaff());
                        break;
                    }
                case SkillName.Blacksmith:
                    {
                        PackItem(new Tongs(), unique: true);
                        PackItem(new Pickaxe(), unique: true);
                        PackItem(new Pickaxe(), unique: true);
                        PackItem(new IronIngot(50), unique: true);
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Fletching:
                    {
                        PackItem(new Board(14), unique: true);
                        PackItem(new Feather(5), unique: true);
                        PackItem(new Shaft(5), unique: true);
                        break;
                    }
                case SkillName.Camping:
                    {
                        // Adam: Add the new Bedroll for campers
                        PackItem(new Bedroll(), unique: true);
                        PackItem(new Kindling(5), unique: true);
                        break;
                    }
                case SkillName.Carpentry:
                    {
                        PackItem(new Board(10), unique: true);
                        PackItem(new Saw(), unique: true);
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Cartography:
                    {
                        PackItem(new BlankMap(), unique: false);
                        PackItem(new BlankMap(), unique: false);
                        PackItem(new BlankMap(), unique: false);
                        PackItem(new BlankMap(), unique: false);
                        PackItem(new Sextant(), unique: true);
                        break;
                    }
                case SkillName.Cooking:
                    {
                        PackItem(new Kindling(2), unique: true);
                        PackItem(new RawLambLeg(), unique: true);
                        PackItem(new RawChickenLeg(), unique: true);
                        PackItem(new RawFishSteak(), unique: true);
                        PackItem(new SackFlour(), unique: true);
                        PackItem(new Pitcher(BeverageType.Water), unique: true);
                        break;
                    }
                case SkillName.DetectHidden:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Discordance:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Fencing:
                    {
                        EquipItem(new Kryss());
                        break;
                    }
                case SkillName.Fishing:
                    {
                        EquipItem(new FishingPole());
                        EquipItem(new FloppyHat(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Healing:
                    {
                        PackItem(new Bandage(50), unique: true);
                        PackItem(new Scissors(), unique: true);
                        break;
                    }
                case SkillName.Herding:
                    {
                        EquipItem(new ShepherdsCrook());
                        break;
                    }
                case SkillName.Hiding:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Inscribe:
                    {
                        PackItem(new BlankScroll(2), unique: true);
                        PackItem(new BlueBook(), unique: true);
                        break;
                    }
                case SkillName.ItemID:
                    {
                        EquipItem(new GnarledStaff());
                        break;
                    }
                case SkillName.Lockpicking:
                    {
                        PackItem(new Lockpick(20), unique: true);
                        break;
                    }
                case SkillName.Lumberjacking:
                    {
                        EquipItem(new Hatchet());
                        break;
                    }
                case SkillName.Macing:
                    {
                        EquipItem(new Club());
                        break;
                    }
                case SkillName.Magery:
                    {
                        BagOfReagents regs = new BagOfReagents(30);

                        PackItem(regs, unique: true);

                        PackScroll(0);
                        PackScroll(1);
                        PackScroll(2);

                        Spellbook book = new Spellbook((ulong)0x382A8C38);

                        EquipItem(book);

                        EquipItem(new Robe(Utility.RandomBlueHue()));
                        EquipItem(new WizardsHat());

                        break;
                    }
                case SkillName.Mining:
                    {
                        PackItem(new Pickaxe(), unique: true);
                        break;
                    }
                case SkillName.Musicianship:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Parry:
                    {
                        EquipItem(new WoodenShield());
                        break;
                    }
                case SkillName.Peacemaking:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Poisoning:
                    {
                        PackItem(new LesserPoisonPotion(), unique: false);
                        PackItem(new LesserPoisonPotion(), unique: false);
                        break;
                    }
                case SkillName.Provocation:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Snooping:
                    {
                        PackItem(new Lockpick(20), unique: true);
                        break;
                    }
                case SkillName.SpiritSpeak:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Stealing:
                    {
                        PackItem(new Lockpick(20), unique: true);
                        break;
                    }
                case SkillName.Swords:
                    {
                        EquipItem(new Katana());
                        break;
                    }
                case SkillName.Tactics:
                    {
                        switch (GetHighestWeaponSkill(skills))
                        {
                            case SkillName.Swords:
                                EquipItem(new Katana());
                                break;
                            case SkillName.Macing:
                                EquipItem(new Mace());
                                break;
                            case SkillName.Archery:
                                EquipItem(new Bow());
                                break;
                            case SkillName.Fencing:
                                EquipItem(new Kryss());
                                break;
                        }
                        // no weapon for no weapon skill
                        break;
                    }
                case SkillName.Tailoring:
                    {
                        PackItem(new BoltOfCloth(), unique: true);
                        PackItem(new SewingKit(), unique: true);
                        PackItem(new Scissors(), unique: true);
                        break;
                    }
                case SkillName.Tinkering:
                    {
                        PackItem(new TinkerTools(), unique: true);
                        break;
                    }
                case SkillName.Tracking:
                    {
                        if (m_Mobile != null)
                        {
                            Item? shoes = m_Mobile.FindItemOnLayer(Layer.Shoes);

                            if (shoes != null)
                                shoes.Delete();
                        }

                        EquipItem(new Boots(Utility.RandomYellowHue()));
                        EquipItem(new SkinningKnife());
                        break;
                    }
                case SkillName.Veterinary:
                    {
                        PackItem(new Bandage(5), unique: true);
                        PackItem(new Scissors(), unique: true);
                        break;
                    }
                case SkillName.Wrestling:
                    {
                        EquipItem(new LeatherGloves());
                        break;
                    }
            }
        }

        private static SkillName GetHighestWeaponSkill(SkillNameValue[] skills)
        {
            // Define which are considered weapon skills
            var weaponSkills = new[]
            {
                SkillName.Macing,
                SkillName.Fencing,
                SkillName.Archery,
                SkillName.Swords
            };

            SkillName best = SkillName.Swords; // default fallback
            int bestVal = -1;

            foreach (var snv in skills)
            {
                if (weaponSkills.Contains(snv.Name) && snv.Value > bestVal)
                {
                    bestVal = snv.Value;
                    best = snv.Name;
                }
            }

            return best;
        }

        // 5/1/23, Yoar: Added pre-Pub13 skill items
        // https://web.archive.org/web/20000308061402fw_/http://uo.stratics.com/skills.htm
        private static void AddSkillItemsPrePub13(SkillName skill)
        {
            switch (skill)
            {
                case SkillName.Alchemy:
                    {
                        PackReagent(4, 3);
                        PackItem(new Bottle(5), unique: true);
                        PackItem(new MortarPestle(), unique: true);
                        EquipItem(new Robe(Utility.RandomPinkHue()));
                        break;
                    }
                case SkillName.Anatomy:
                    {
                        PackItem(new Bandage(3), unique: true);
                        EquipItem(new Robe());
                        break;
                    }
                case SkillName.AnimalLore:
                    {
                        EquipItem(new ShepherdsCrook());
                        EquipItem(new Robe(Utility.RandomGreenHue()));
                        break;
                    }
                case SkillName.Archery:
                    {
                        PackItem(new Arrow(25), unique: true);
                        EquipItem(new Bow());
                        break;
                    }
                case SkillName.ArmsLore:
                    {
                        PackPracticeWeapon();
                        break;
                    }
                case SkillName.Begging:
                    {
                        EquipItem(new GnarledStaff());
                        break;
                    }
                case SkillName.Blacksmith:
                    {
                        PackItem(new Tongs(), unique: true);
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Fletching:
                    {
                        PackItem(new Board(14), unique: true);
                        PackItem(new Feather(5), unique: true);
                        PackItem(new Shaft(5), unique: true);
                        break;
                    }
                case SkillName.Camping:
                    {
                        PackItem(new Bedroll(), unique: true);
                        PackItem(new Kindling(5), unique: true);
                        break;
                    }
                case SkillName.Carpentry:
                    {
                        PackItem(new Board(10), unique: true);
                        PackItem(new Saw(), unique: true);
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Cartography:
                    {
                        PackItem(new BlankMap(), unique: false);
                        PackItem(new BlankMap(), unique: false);
                        PackItem(new BlankMap(), unique: false);
                        PackItem(new BlankMap(), unique: false);
                        PackItem(new Sextant(), unique: true);
                        break;
                    }
                case SkillName.Cooking:
                    {
                        PackItem(new Kindling(2), unique: true);
                        PackItem(new RawBird(3), unique: true);
                        PackItem(new SackFlour(), unique: true);
                        PackItem(new Pitcher(BeverageType.Water), unique: true);
                        break;
                    }
                case SkillName.DetectHidden:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Discordance:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Fencing:
                    {
                        EquipItem(new Spear());
                        break;
                    }
                case SkillName.Fishing:
                    {
                        EquipItem(new FishingPole());
                        EquipItem(new FloppyHat(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Healing:
                    {
                        PackItem(new Bandage(5), unique: true);
                        PackItem(new Scissors(), unique: true);
                        break;
                    }
                case SkillName.Herding:
                    {
                        EquipItem(new ShepherdsCrook());
                        break;
                    }
                case SkillName.Hiding:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Inscribe:
                    {
                        PackItem(new BlankScroll(2), unique: true);
                        break;
                    }
                case SkillName.ItemID:
                    {
                        EquipItem(new GnarledStaff());
                        break;
                    }
                case SkillName.Lockpicking:
                    {
                        PackItem(new Lockpick(5), unique: true);
                        break;
                    }
                case SkillName.Lumberjacking:
                    {
                        EquipItem(new Hatchet());
                        break;
                    }
                case SkillName.Macing:
                    {
                        EquipItem(new Mace());
                        break;
                    }
                case SkillName.Magery:
                    {
                        PackReagent(8, 3);
                        PackScroll(0, 3);
                        EquipItem(new Spellbook(), unique: true);
                        break;
                    }
                case SkillName.Meditation:
                    {
                        EquipItem(new Robe(Utility.RandomBlueHue()));
                        break;
                    }
                case SkillName.Mining:
                    {
                        PackItem(new Pickaxe(), unique: true);
                        break;
                    }
                case SkillName.Musicianship:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Parry:
                    {
                        EquipItem(new WoodenShield());
                        break;
                    }
                case SkillName.Peacemaking:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.Poisoning:
                    {
                        PackItem(new LesserPoisonPotion(), unique: false);
                        PackItem(new LesserPoisonPotion(), unique: false);
                        break;
                    }
                case SkillName.Provocation:
                    {
                        PackInstrument();
                        break;
                    }
                case SkillName.MagicResist:
                    {
                        EquipItem(new Spellbook(), unique: true);
                        break;
                    }
                case SkillName.Snooping:
                    {
                        PackItem(new Lockpick(4), unique: true);
                        break;
                    }
                case SkillName.SpiritSpeak:
                    {
                        EquipItem(new Cloak(0x455));
                        break;
                    }
                case SkillName.Stealing:
                    {
                        PackItem(new Lockpick(4), unique: true);
                        break;
                    }
                case SkillName.Swords:
                    {
                        EquipItem(new Longsword());
                        break;
                    }
                case SkillName.Tailoring:
                    {
                        PackItem(new Cloth(4), unique: true);
                        PackItem(new SewingKit(), unique: true);
                        break;
                    }
                case SkillName.TasteID:
                    {
                        PackPotion();
                        PackPotion();
                        PackPotion();
                        break;
                    }
                case SkillName.Tinkering:
                    {
                        PackRandomTinkerComponent();
                        PackItem(new TinkerTools(), unique: true);
                        EquipItem(new HalfApron(Utility.RandomYellowHue()));
                        break;
                    }
                case SkillName.Tracking:
                    {
                        if (m_Mobile != null)
                        {
                            Item? shoes = m_Mobile.FindItemOnLayer(Layer.Shoes);

                            if (shoes != null)
                                shoes.Delete();
                        }

                        EquipItem(new Boots(Utility.RandomYellowHue()));
                        EquipItem(new SkinningKnife());
                        break;
                    }
                case SkillName.Veterinary:
                    {
                        PackItem(new Bandage(5), unique: true);
                        PackItem(new Scissors(), unique: true);
                        break;
                    }
                case SkillName.Wrestling:
                    {
                        EquipItem(new LeatherGloves());
                        break;
                    }
            }
        }
    }
}