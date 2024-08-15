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

/* Scripts/Mobiles/PlayerMobile.cs
 * ChangeLog:
 *  5/6/2024, Adam (OnLogin)
 *      Set the player's HideHidden status to false.
 *  9/10/23, Adam, (one-time Faction System Murder Reprieve)
 *      When a player joins factions, a) for the first time, and b) for the first month of factions, they get a one-time Faction System Murder Reprieve.
 *      This Murder Reprieve:
 *      Saves their current short and long term murder counts, and sets their counts to the lesser of their current counts or 4 (making them blue)
 *      In the case where they murder again, their Reprieve is Rescinded, and these previous counts are restored.
 *      Publish 8: https://uo.com/wiki/ultima-online-wiki/technical/previous-publishes/2000-2/2000-publish-08-5th-december/
 *  9/7/2023, Adam (IsInCombat())
 *      Add new function IsInCombat()
 *      Check IsInCombat() in the RTT. If the player IsInCombat, do not issue the RTT challenge.
 *  7/31/2023, Adam
 *      Move massive skill gain systems to PlayerMobileSkills.cs
 *  7/31/2023, Adam (SkillGainEpoch)
 *      Remove the bonus hour I was affording players after a server restart, or the RoT clock rolls over.
 *      It was intended to make gaining after one of these events assured, but it messes with Razor interface
 *      functions for precise 'next skill gain' calcs.
 *  7/26/2023, Adam: (RotInfo)
 *      Add a player command that allows the player (or staff) to dump the pertinent RoT info
 *  6/3/2023, Adam (LootType.UnLootable)
 *      reds, regardless if they can keep newbied items, can always keep UnLootable
 *      i.e., (item.LootType != LootType.Newbied && (item.LootType & LootType.UnLootable) != 0) 
 *          Keep it
 *  5/13/23, Yoar
 *      Merge FastWalk prevention with RunUO - Added server-side delay to turning
 *  5/2/23, Yoar
 *      Now properly registering our MoveReq throttler to prevent speedhack (PacketHandlers.RegisterThrottler)
 *  4/30/23, Yoar
 *      Added AlignmentTitleDisplay which overrides the global alignment title display option
 *  4/30/23: Adam (EventSink_Connected)
 *      We no longer block players from entering the shard if they have break a login rule, like too many IP addresses.
 *      We now take them out of society by tossing their cheating asses in Prison.
 *  4/25/23: Adam (Exploit)
 *      if the user double clicks an explosion potion, cancels the target, then tries to place it on the ground, in town,
 *          disallow the placement. We hope it blows up in your backpack BTW
 *  4/14/23, Yoar
 *      Added support for new Alignment system.
 * 4/13/23, Adam (AllowRedsToKeepNewbieItems)
 *      Set this to false. Not sure why it was set to true in the first place.
 * 12/1/22, Adam
 *      Serialize RoT state (RoT servers only, and if the data has been initialized.)
 * 11//29/22, Adam
 *      Add a new m_specialRoTStatGainTime which allows stats to gain even after max skills have been 
 *      reached for the day. The specialRoTStatGainTime interval is the same as for skill gains.
 * 11/27/22, Adam (RoT)
 *      o) increase RoTStatGainToday from 6 to 15
 *      o) Call new RotStatGainChance() for a stat gain when usual RoT skill is being blocked
 *          due to either points per time limit or daily point max reached.
 * 11/23/22, Adam (RoT System)
 *  overhaul Rot skill gain so that it behaves like this:
 *  Skills between 70.0 � 79.9 can gain 0.1 every 5 minutes, for a total of 5.0 per day.
 *  Skills between 80.0 � 89.9 can gain 0.1 every 8 minutes, for a total of 4.0 per day.
 *  Skills between 90.0 � 99.9 can gain 0.1 every 12 minutes, for a total of 3.0 per day.
 *  Skills between 100.0 � 109.9 can gain 0.1 every 15 minutes, for a total of 3.0 per day.
 *  Skills between 110.0 � 120.0 can gain 0.1 every 15 minutes, for a total of 2.0 per day.
 *  http://uothief.com/faq/7pt5.html
 * 9/20/22, Adam (JumpList and WorldLocation)
 *  Add support for WorldLocation in JumpList
 *  The return value from GetWorldLocation() is now  a WorldLocation (point3D + map)
 * 8/31/22, Yoar (Factions)
 *   Now properly displaying faction title in OnSingleClick.
 * 8/26/22, Adam (SiegeBless)
 *   Add toggle for siege bless.
 * 8/20/22, Adam (Siege Bless)
 *      1. Add several functions for handling of Siege Bless
 *      2. Add an override for the mobile.TrackedItemParentChanging(). This ensures the parent is of the blessed item is notified if it changes hands.
 *      3. Added the notion of LocalCommands(). Local commands are commands a player can speak, and they are handled by that same player.
 *          Similar to the region commands like "Ii wish to lock this down", LocalCommands need no region. 
 *          Currently, the supported command is "i wish to bless this"
 *      4. one new (conditionally) serialized property: m_siegeBlessedItem
 * 8/16/22, Adam (OnLogin)
 *  When a Mortal dies:
 *      if the server goes down before the Delete sequence finishes, we'll finish it here.
 * 8/15/22, Adam (GUIDs/OnDeath)
 *  Every player now gets a Globally Unique ID (GUID)
 *  In OnBeforeDeath, EventSink.InvokeBeforePlayerDeath(new BeforePlayerDeathEventArgs(this.GUID,this.Account as Account, DateTime.UtcNow, GetFirstHouse(),Followers)); 
 *      This is handled in Account for the player and records: the guid, the account, the time of death, and the number of followers the player had at death.
 *  Add the 'gas sound' when a mortal dies - spirit leaving the body
 *  Added MakeGUID() function. Called whenever a PlayerMobile is created. (it's also called from the patcher in Main.cs)
 * 8/12/22, Adam (OnDeath)
 *  Mortalis: When moving bankbox items to temp storage for transfer to the next player, maintain bankbox packing structure.
 *      However, if they have a 'prison issue' backpack (because they were sent to prison,) unpack that and delete the prison issue backpack.  
 *  8/9/22, Adam
 *      Add the account's notion of HardwareHashAcquired to viewable properties.
 *      Useful for debugging. Can later be removed.
 *  8/3/22, Adam
 *      Mortalis death:
 *      the killed player's bankbox is saved and given to the new character created on the account.
 *  8/2/22, Adam
 *      Add HardwareHash to the PlayerMobile
 *      gets/sets the player's account HardwareHash
 *      useful for testing
 *  7/31/22, Adam
 *      Add the notion of MinimumSentence. 
 *      This is for sending someone to prison that has no murder counts.
 *      (We do this on Mortalis instead of a guard whack.)
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *  3/18/22, Yoar
 *      Changes to stat gain. Added stat gain point system, similar to the skill gain point system.
 *  2/28/22, Adam (GetPublishingPoints)
 *      When a player becomes a Composer (joins the Bard guild,) we call MusicBox.GetPublishingPoints()
 *      to seed the NPC Guild points with the composers past accomplishments
 *  2/26/22, Adam
 *      Add a new AI special 'skill' for Composers
 *  1/26/21, Yoar
 *      Now serializing skill gain points.
 *  12/29/21, Adam (ShareBank)
 *      Add a property for bankbox sharing within account. 
 *      Each player that wants to share his bank, must enroll in the system
 *  12/10/21, Adam (Music System)
 *      Move the 'play music' system from PlayerMobile down to Mobile. 
 *      We therefore replaces most all instances of PlayerMobile with Mobile.
 *      This was done so that NPC can eventually 'play music'.
 *  11/28/21, Adam (CheckReversePickpocket)
 *      Add a memory object which prevents thieves from a ReversePickpocket overload exploit.
 *      We limit the 'plant' to one per hour on the same mobile
 *  11/25/21, Yoar
 *      Added StamDrain for stamina drain scaling.
 *  10/28/21, Yoar
 *      Moved skill gain code for AI to SkillGain.cs
 *  10/14/21, Yoar
 *      Bulk Order System overhaul:
 *      - Removed m_NextSmithBulkOrder, m_NextTailorBulkOrder. These are now stored in BulkOrderContext.
 *      - Added m_BulkOrderBankingSetting.
 *  7/20/21, Pix
 *      Added 'cooldown' option for explosion potion
 *  7/18/2021, Craig
 *  	o Removed insta-logout Test Center functionality for Core.UOBETA
 *	3/9/2016, Adam
 *		o ApplyMagic() is now called called from PlayerMobile.OnLogin() to active magic properties of clothing and jewelry.
 *	3/8/2016, Adam
 *		o Add EventScore property for keeping track of players score throughout this event.
 *			We still need to come up with a zeroing system.
 *	2/14/11, Adam
 *		o Login hidden on UO Mortalis
 *		o Bank contents now drop to the corpse on UOMO
 *	2/13/11, Adam
 *		Don't clean the backpack of players logging on Angel Island unless this is UOAI
 *		DecayKills() is accelerated only for UOAI 
 * 2/1/11, Pix.
 *      Fixed below-70 ROT skillgain.
 * 1/16/11, Pix
 *      First version of Siege RoT. - skill & stat.
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	5/13/10, adam
 *		When you take damage and your explosion potion is set off, gibe the use a message
 *			"Your explosive potion is jostled thus setting it off!"
 *	4/1/10, adam
 *		Undo CheckSkill() changes of 3/14/10 since you cannot do the skill if checkskill is fails.
 *		(We didn't want to block the skill, only the skill gain.)
 *		Add a check to prevent taming from within a house in AnimalTaming.cs
 *	3/17/10, adam
 *		OnLogin
 *		(1) remove old dismount code (hold over from when we tried slow mounts)
 *		(2) If a player is in prison and they have been logged out for 3+ hours, reinitialize their backpack to near original state.
 *			basically throw away any stuff they may be muling for another char on that or a related account.
 *	3/14/10, adam
 *		Modify CheckSkill() to disallow a gain if you are using skill "Animal Taming" and passive "Animal Lore" from a house
 *	3/12/10, Adam
 *		Add CriminalCounts for sending players to prison
 *	3/10/10, adam
 *		add the jump list (m_JumpList) initial implementation
 *		See also: [next
 *	07/06/09, plasma
 *		Add forwarder to Stealing for reverse pickpocket in CheckNonlocalDrop()
 *	3/22/09, Adam
 *		In RTTResult, Randomize next test to reduce predictability.
 *	1/7/09, Adam
 *		Add a missing Packet Acquire/Release for SendGuildChat
 *	09/25/08, Adam
 *		Dismount players OnLogin
 *	09/24/08, Adam
 *		Add a new system for calculating player movement speed.
 *		We added a new non-serialized date time variable m_LastTimeMark to track the last marker passed.
 *		please the Server.Items.MarkTime object
 *	09/23/08, Adam
 *		You must be AccessLevel > AccessLevel.Player to get mounted speed in ComputeMovementSpeed()
 *	07/30/08, weaver
 *		Fixed spirit cohesion so that it uses LastDeathTime instead of damage entries (which don't
 *		appear to work anymore).
 *	07/27/08, weaver
 *		Correctly remove gumps from the NetState object on CloseGump() (integrated RunUO fix).
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 3 loops updated.
 *	7/17/08, Adam
 *		Add new ZCode Mini Game save/restore
 *	4/24/08, Adam
 *		Change public virtual TimeSpan ComputeMovementSpeed( Direction dir ) to public override TimeSpan ComputeMovementSpeed( Direction dir )
 *		As I believe this was the intent (to override the mobile version.)
 *	4/2/08, Pix
 *		Added LeatherEmbroidering bit.
 *	2/26/08, Adam
 *		Remove breedting test logic as it will mess up server wars (which now has TC enabled):
 *			// BREEDING TEST! TC ONLY!
 * 			if (TestCenter.Enabled)
 * 				FollowersMax = 500;
 *	2/24/08, plasma
 *		Added IOBRealAlignment prop
 *  1/22/08, Adam
 *		merge Shopkeeper skill with NpcGuild guild system (MerchantGuild)
 *      - new I/O optimization system
 *      - Add NPCGuild stuffs to I/O optimization system
 *	1/20/08, Adam
 *		Add support for the new Shopkeeper skill
 *  03/01/07, plasma
 *			Remove all duel challenge system related code
 *  12/4/07, Pix
 *      Added LastDeathTime variable/property.
 *      Reworked SpiritCohesive() to use this property instead of the damageentries.
 *  12/3/07, Pix
 *      Added IOBAlignement.Healer for kin-healers instead of overloading Outcast
 *  11/29/07, Pix
 *      Fixed OnKinBeneficial() to use the right variable.
 *	11/21/07, Adam
 *		Change the BaseWeapon damage bonus (based on strength) to take into account the new mobile.STRBonusCap.
 *		This new STRBonusCap allows playerMobiles to have super STR while 'capping' the STR bonus whereby preventing one-hit killing.
 *		The STRBonusCap for PlayerMobiles is defaulted to 140 (100 max STR + legal buffs.)
 *		Note: Only new PlayerMobiles will default to the 140 cap, existing players will be set to 'no cap' or zero (0)
 *	08/26/07, Pix
 *		Changes for RTT.
 *		Changes for Duel Challenge System.
 *	08/01/07, Pix
 *		Added consequences for blue-healers with the Fight Brokers.
 *	7/28/07, Adam
 *		Ghost Blindness:
 *		if the ghost shouldGoBlind BUT the RegionAllowsSight, reschedule blindness
 *	6/16/07, Pix
 *		Make sure the GoBlind() call was the result of the latest death, not a previous death.
 *  5/23/07, Adam
 *      Make sure we don't GoBlind() if the player has resed.
 *  5/21/07, Adam
 *      - Filter boats during ghost blindness
 *      - Allow pets to be seen
 *  5/21/07, Adam
 *      Add Ghost blindness
 *          - After your body decays you will see the message "You feel yourself slipping into the ethereal world."
 *          - once you enter the ethereal world, you will not see other corporeal life except for the NPC healer
 *          - once you enter the ethereal world, you will see no corpses but your own
 *          - sight is restored with resurrection
 *	4/03/07, Pix
 *		Tweak to RTT to set the time-to-next-test when the test is taken instead of when the 
 *		response is given.  This fixes an accidental closing/disconnect where they don't 
 *		answer.
 *  03/20/07, plasma
 *      Overrode new BoneDecayTime property to extend delay if a ship captain (corpse has key)
 *	03/27/07, Pix
 *		Implemented RTT for AFK resource gathering thwarting.
 *  03/12/07, plasma,
 *      Changed OnDroppedItemToWorld to prevent dropping stuff next to a TillerMan
 *  2/26/07, Adam
 *      StopMRCapture may be called with CommandEventArgs == null
 *      Add protection.
 *	2/05/07 Taran Kain
 *		Added temporary MovementReqLogger class, added packet throttling flexibility
 *  2/5/07, Adam
 *      Remove ProcessItem override (for guildstones)
 *  1/08/07 Taran Kain
 *      Moved anti-macro code, GSGG logic here
 *      Added in PlayerMobile-specific skillcheck logic
 *  01/07/07, Kit
 *      Re-enabled context menus, after accidental disabling.
 *  01/07/07, Kit
 *      Added netstate check to report, reporting a offline player would crash shard.
 *  12/21/06, Adam
 *      Don't invoke Use() on FakeContainers when a user logs in
 *      We clear the ReadyState in OnLogin()
 *	12/08/06 Taran Kain
 *		Added same TC-only code to default PlayerMobile ctor
 *	12/07/06 Taran Kain
 *		Added TC-only code to reset FollowersMax to 500.
 *	11/25/06, Pix
 *		Added staff-announce for characters of watched accounts logging in.
 *  11/22/06, Rhiannon
 *      Added target's IP to report log header.
 *	11/20/06 Taran Kain
 *		Overrode StrMax, DexMax, IntMax to return 100 - standard human statcaps.
 *		Added PlayerMobile StamRegenRate logic.
 *	11/19/06, Pix
 *		Changes for fixing guild and ally chat colors.
 *	11/19/06, Pix
 *		Removed test code.  Sorry!
 *	11/19/06, Pix
 *		Watchlist enhancements
 *  11/18/06, Adam
 *      Comment out some justice award crap
 *	10/17/06, Adam,
 *		- pixie: Add check for login-on-preview house-exploit
 *		- Add call to Cheater() logging system
 *	9/25/06, Adam
 *		Remove all unused code from context menu:
 *			We don't have insurance, we don't allow house-exit, and we don't have Justice Protectors
 *	9/25/06, Pix
 *		Added ability for players to remove themselves from a house via context menu.
 *  9/02/06, Kit
 *		Added additional checks to IsIsolated due to crash of 9/2, added try/catch. 
 *  8/19/06, Kit
 *		Added Check to CanBeHarmful to prevent harmful actions to players in a
 *		NoExternalHarmful enabled DRDT region.
 *		Added CanSee and IsIsolated routines for hiding items/multis with DRDT Isolation zones.
 *  8/20/06, Rhiannon
 *		Added override for PlaySound to allow for control of music via [FilterMusic.
 *  8/13/06, Rhiannon
 *		Added location to report log header.
 *  8/05/06, Rhiannon
 *		Added PlayList, Playing, and FilterMusic properties.
 *	8/03/06, weaver
 *		Added LastLagTime.
 *		Reformatted comments.
 *	7/24/06, Rhiannon
 *		Changed test for lockdown message to display to Administrators and Owners.
 *	7/23/06, Pix
 *		O/C guilds always display kin type.
 *		Single-clicking self while hidden no longer displays kin type.
 * 	7/18/06, Rhiannon
 *		Added serial numbers to report log header.
 *	7/10/06, Pix
 *		Removed penalty from harming Hires of the same kin.
 *	7/5/06, Pix
 *		Removed non-aligned blue healer turning outcast for healing a kin player - this 
 *		was allowing people to PK kin-aligned people without possibility of getting a murder count.
 *	7/4/06, Pix
 *		Made kin alignment show only when guild titles are displayed.
 *	7/01/06, Pix
 *		Now OnBeneficialAction will turn a non-kin outcast when he heals/etc any PC kin
 *		Overrode OnSingleClick() to show kin alignment.
 *	6/24/06, Pix
 *		Fixed OnBeneficialAction exception.
 *	6/22/06, Pix
 *		Changed outcast flagging for beneficial actions to ignore combat with other players and pets/summons
 *	6/19/06, Pix
 *		Added KinAggression() call when PM does a beneficial action on someone involved in combat with his kin.
 *	6/18/06, Pix
 *		Added KinAggression 'timer' for new OutCast Kin Type.
 *	06/09/06, Pix
 *		Fixed IOBAlignment PROPERTY for recently-unguilded people.
 *	06/06/06, Pix
 *		Changes for Kin System
 *	5/03/06, weaver
 *		Added logging of players logging in and out of game.
 *	5/02/06, weaver
 *		Added IsIsolatedFrom() check to CanBeHarmful() override.
 *	4/30/06, weaver
 *		Added IsIsolatedFrom() to handle custom region based isolation.
 *		Added IsIsolatedFrom() check to CanSee() override.
 *	3/8/06, Pix
 *		Make sure that [report logging files never have illegal characters.
 *	3/1/06, Adam
 *		Remove LastStealTime as we already have LastStoleAt
 *		PS. LastStealTime was not being used.
 *	2/28/06, weaver
 *		Added LastStealTime to store last time player targetted to steal.
 *	2/26/06, Pix
 *		Added call to mobile.RemoveGumps() to remove all ressurectiongumps
 *		when we're alive and we see that there's a ressurectiongump in our gumplist
 *		(in the same code that prevents a dead PM from walking if they have a ressurectiongump)
 *	2/10/06, Adam
 *		Add new override ProcessItem() to process (in a generic way) an item the player is carrying.
 *		This facility us used for placement of a guild stone that is now carried on the player instead
 *		of in deed form (as the FreezeDry system and guild deeds were not compatable.)
 *	01/09/06 Taran Kain
 *		Added Speech recording capabilities.
 *	01/03/06 - Pix
 *		Implemented SendAlliedChat for Allied guild chat.
 *	12/01/05, Pix
 *		Added WatchList PlayerFlag and staff notification when WatchListed player logs in.
 *	11/29/05, weaver
 *		Altered utilisation of SavageKinPaintExpiration time so that only HueMod is adapted for
 *		the effect, not BodyMod.
 *	11/20/05, Pix
 *		Commented out extraneous DecayKills() call on Serialize();
 *	11/19/05, Kit
 *		Removed SavageKinPaintExperation from OnDeath, added check to Resurrect() if savagekinpaint experation not 0
 *		set bodyvalue/hue for savagepaint.
 *	11/07/05, Kit
 *		Moved HasAbilityReady variable and timer for special weapon moves to Mobile.cs for AI use.
 *	11/06/05 Taran Kain
 *		Changed Message() to MortalDeathMessage() for code clarity
 *	10/10/05 TK
 *		Changed some ints to doubles for more of a floating-point math pipeline
 *	10/08/05 Taran Kain
 *		Changed RemoveAllStatMods property to use ClearStatMods() function
 *	9/23/05, Pix
 *		bugfix: now offline decay resets when you get a murdercount.
 *	9/08/05, weaver
 *		Added Embroidering & Etching bitflags.
 *	09/02/05 TK
 *		Added a bit more blood to Mortal deaths
 *	08/28/05 TK
 *		Changed Mortal flag to be part of PlayerFlags, to save some ram
 *		Made sure Mortal players can't be ressurected - 10sec period where they're a ghost
 *		Made sure if AccessLevel > Player, go thru with everything except delete
 *	08/27/05 TK
 *		Added PlayerMobile.Mortal, the Permadeath flag
 *		Added checks in OnDeath to delete player if Mortal=true
 *	8/02/05, Pix
 *		Added check in addition to InitialInnocent check to see whether the basecreature is controled
 *	7/28/05, Pix
 *		Now, if the player has a deathrobe already on resurrect, use that instead of creating a new one.
 *	7/26/05, weaver
 *		Automated removal of AoS resistance related function calls. 32 lines removed.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *	7/21/05, weaver
 *		Removed some code referencing resistance variables & redundant resist orientated functions
 *	7/7/05, weaver
 *		Added storage of LastResurrectTime for use with Spirit Cohesion checks.
 *		Made SpiritCohesion &  LastResurrectTime accessible via [props.
 *	7/6/05, weaver
 *		Fixed Spirit Cohesion delays so accessed via CoreAI & fixed
 *		FindDamageEntryFor() call to pass this.LastKiller.
 *	7/5/05, Pix
 *		Added new Guild Chat functionality.
 *	6/16/05, Adam
 *		Removed the line "NOTICE:" from non-activated accounts.
 *		It's not really needed.
 *	6/15/05, Pix
 *		Added pester message on login for Profile activation.
 *	6/13/05, weaver
 *		Added CohesionBaseDelay + removed static initialization (now
 *		controlled through core management console).
 *	6/8/05, weaver
 *		Added SpiritCohesion property and SpiritCohesive() function.
 *	5/30/05, Kit
 *		Added overrided PlaySound() to send sounds players make to monsters withen radius
 *	5/02/05, Kit
 *		Added LastRegion to playermobile for use with DRDT system.
 *	4/30/05, Pix
 *		Made Alchemist reduction linearly dependent on the amount of alchemy you have.
 *		You only get the full reduction if you're at GM alchemy.
 *	04/27/05, weaver
 *		Added read-only Counselor access to DecayTimeShort & DecayTimeLong
 *	4/23/05, Pix
 *		Added CoreAI.ExplosionPotionAlchemyReduction
 *	04/20/05, Pix
 *		Fixed the 'bonus' for having alchemy re: purple pots exploding in your pack.
 *	04/19/05, Pix
 *		Now DecayTimeShort property uses the min of online decay time and offline decay time
 *		Fixed offline decay time messing up online decay time :-O
 *	04/19/05, Pix
 *		Now uses CoreAI.OfflineShortsDecayHours
 *	04/18/05, Pix
 *		Added offline short term murder decay (only if it's turned on).
 *		Added potential exploding of carried explosion potions.
 *	03/09/05, weaver
 *		Added WoodEngraving bitflag.
 *	02/28/05, Adam
 *		remove references to 'PayedInsurance' (no more insurance)
 *		reuse the  flag as 'Fixed' (item.cs)
 *	02/25/05, Adam
 *		remove references to 'Insured' (no more insurance)
 *		reuse the  flag as 'PlayerCrafted' (item.cs)
 *	02/19/05, weaver
 *		Added LastSkillUsed & LastSkillTime for use with new [FindSkill command.
 *	2/16/05, Pixie
 *		Tweaks to make armor work in 1.0.0
 *	02/15/05, Pixie
 *		CHANGED FOR RUNUO 1.0.0 MERGE.
 *	01/10/05, Pix
 *		Replaced NextMurderCountTime with KillerTimes arraylist for controlling repeated counting.
 *	01/10/05, Pix
 *		Added variable to store last lockpick used time.
 *	01/05/05, Pix
 *		Changed IOB requirement from 36 hours to 10 days
 *		Added IOBJoinRestrictionTime.
 *	12/30/04, Pix
 *		Removed AggressiveAction code put in 2 days ago.
 *	12/28/04, Pix
 *		Fixed compiler warning.
 *	12/28/04, Pix
 *		Added AggressiveAction override so we can check for a player attacking another player
 *		who is wearing an IOB of the same type.
 *	12/26/04, Pix
 *		Fix for controlslot change with rank change: followers gets re-calculated on
 *		login.
 *	12/24/04, Pix
 *		Fixed display of IOBRankTime in [props
 *	12/24/04, Adam
 *		Hack Removed.
 *	12/24/04, Adam
 *		Add HACK to insure IOBRankTime gets updated without the player having to do anything.
 *			I added a hack to DoGlobalDecayKills() which is called every 15 minutes for Murder Counts
 *			This code should be removed and replaced with the "right answer"
 *	12/21/04, Pixie
 *		Now resets the iobalignment if we're out of iobtime and not wearing iob
 *	12/20/04, Pixie
 *		Added IOBStartedWearing and IOBRank time to keep track of the IOB Ranks
 *	12/01/04, Pixie
 *		In OnBeforeDeath(), made sure that the player isn't holding anything on their cursor that might
 *		not get dropped.
 *	11/07/04, Pixie
 *		Fixed a problem with short and long timers getting truncated to 4/20 hours instead of being
 *		calculated to what they should be.
 *	11/07/04, Pigpen
 *		Changed it so if a player is wearing an IOB and they die, the timer is set to 36 hours. This is
 *		changed so that players cannot get out of there IOB group by any means other than intended.
 *	11/05/04, Pigpen
 *		Added m_IOBAlignment; Added IOBEquipped; Added IOBTimer. Needed for the new IOBSystem, Enum Values
 *		for IOBAlignment contained in Engines\IOBSystem\IOBAlignEnum.cs
 *	10/16/04, Darva
 *		Added m_LastStoleAt as the timer to control banking after stealing.
 *		This is -not- serialized, as if the theif manages to log, your item
 *		is gone anyway.
 *	9/25/04 - Pix
 *		Added m_LastResynchTime to facilitate 2-minute time period between
 *		uses of the [resynch command.
 *	9/16/04 Pixie
 *		Added static DoGlobalDecayKills() which the Heartbeat system uses.
 *		It Decays kills on every PlayerMobile.
 *	8/30/04 smerX
 *		Reinstated visRemove command..
 *	8/27/04, Adam
 *		Backout smerx's vislist stuff. To recover, revert this file to rev 1.27
 *	8/26/04, Pix
 *		Changed so that reds keep their newbie items.  Also added a switch for this: AllowRedsToKeepNewbieItems
 *	8/7/04, mith
 *		Added temporary NextMurderCountTime variable until I can fix notoriety/flagging.
 *	7/11/04, Pixie
 *		Added 2 properties to give some insight/control over StatMods (from jewelry/clothing)
 *		StatModCount shows the number of mods the player is currently under (3 for bless/curse, 1 for others)
 *		RemoveAllStatMods if set to true will remove all effects the player is under.
 *	7/8/04, Pixie
 *		Fixed murder count decay code in Resurrect (was adding time to reds when it shouldn't have)
 *		Made sure when toggling the Inmate flag that we only add/subtract the time once.
 *	6/16/04, Pixie
 *		Added GSGG factor.
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/26/04, mith
 *		Modified Resurrect() and OnBeforeDeath() to modify murder count decay times to prevent people from macroing counts on AI as ghosts.
 *	5/13/04, mith
 *		Added RetainPackLocsOnDeath property, overrriding the property from Mobile.
 *		This was returning Core.AOS as the true/false value. Set it to always be true (no more messy packs on resurrection).
 *	5/2/04, Pixie
 *		Cleaned up the way we modify MurderCount timers based on whether the player is an Inmate or not.
 *		Added DecayKills method which we call from various places to reset our count timers.
 *		Added DecayTimeLong and DecayTimeShort properties to debug problems easier. Displays the amount of time until the next countis decayed.
 *		Added code to decay kills when player says "i must consider my sins" in addition to code that already existed in serialize. This way, if time between saves are increased
 *			players can still decay their counts at the appropriate time by simply checking how many counts they have left.
 *	4/29/04, mith
 *		Modified code that sets murder count decay timers to also check if Inmates are Alive or not
 *		If Inmates are sitting around as ghosts, their counts decay as if they were not on Angel Island (8/40).
 *	4/24/04, adam
 *		Commented out "bool gainedPath = false;"
 *	4/24/04, mith
 *		Commented Justice award in OnDeath() since virtues are disabled.
 *	4/10/04 change by Pixie
 *		Added ReduceKillTimersByHours(double hours) for the ParoleOfficer
 *	4/10/04 change by mith
 *		Fixed a typo in Serialize with m_LongTermElapse.
 *	4/9/04 changes by mith
 *		Added code to reset count decay time based on Inmate flag for Serialize() event.
 *	4/1/04, changes by mith
 *		Testing values for ShortTerm and LongTerm count decay. Must test next time I have 4 hours of free time.
 *	3/29/04, changes by mith
 *		Added PlayerFlag.Inmate, to be modified on entrance/exit of Angel Island.
 */

#pragma warning disable 429, 162

using MaxMind.GeoIP2.Responses;
using Server.Accounting;
using Server.Commands;
using Server.ContextMenus;
using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Alignment;
using Server.Engines.BetaRewardSystem;
using Server.Engines.BulkOrders;
using Server.Engines.Quests;
using Server.Factions;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Spells.Fifth;
using Server.Spells.Seventh;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using static Server.Accounting.Account;
using static Server.Accounting.AccountIPManager;
using static Server.Item;
using static Server.Utility;

namespace Server.Mobiles
{
    [Flags]
    public enum PlayerBoolTable : uint          // First 16 bits are reserved for default-distro use, start custom flags at 0x00010000
    {
        None = 0x00000000,
        Glassblowing = 0x00000001,
        Masonry = 0x00000002,
        SandMining = 0x00000004,
        StoneMining = 0x00000008,
        ToggleMiningStone = 0x00000010,
        KarmaLocked = 0x00000020,
        AutoRenewInsurance = 0x00000040,
        UseOwnFilter = 0x00000080,
        PublicMyRunUO = 0x00000100,
        PagingSquelched = 0x00000200,
        PrisonInmate = 0x00010000,              // inmate at Angel Island
        MurderReprieve = 0x00020000,            // Adam - one-time Faction System Murder Reprieve (Never unset)
        WoodEngraving = 0x00040000,             // weaver - added to allow perma prop.
        Mortal = 0x00080000,                    // TK - Permadeath
        Embroidering = 0x00100000,              // weaver - added to allow perma prop.
        Etching = 0x00200000,                   // weaver - added to allow perma prop.
        Watched = 0x00400000,                   // Pix: added for staff watch list,
        LeatherEmbroidering = 0x00800000,       // Pix - leather embroidery
        ShareBank = 0x01000000,                 // does this player wish to share his bank with others on the same account?
        PrisonVisitor = 0x02000000,             // Player is on a prison quest
        GrandfatheredHerding = 0x04000000,      // gets AI style Herding bonus for stable slots
        DebugRot = 0x08000000,                  // Changes delay between skill gains to 2 minutes
        MurderReprieveRescinded = 0x10000000,   // Adam - one-time Faction System Murder Reprieve Rescinded (Never unset)
        InMemoriam = 0x20000000,                // Adam - Player passed away :(
        OneYearVeteranReward = 0x40000000,      // player has already received the One Year Veteran Reward
    }

    public enum NpcGuild
    {
        None,
        MagesGuild,
        WarriorsGuild,
        ThievesGuild,
        RangersGuild,
        HealersGuild,
        MinersGuild,
        MerchantsGuild,
        TinkersGuild,
        TailorsGuild,
        FishermensGuild,
        BardsGuild,
        BlacksmithsGuild
    }

    public partial class PlayerMobile : Mobile
    {
        #region Kill Queue
        public Queue<BaseCreature> KillQueue = null;  // not serialized. used by the controller system
        #endregion Kill Queue
        #region event notifications
        public override void WeaponStatus(BaseWeapon weapon, object o)
        {   // probably: Your equipment is severely damaged.
            if (o is string name && !string.IsNullOrEmpty(name))
                LocalOverheadMessage(MessageType.Regular, 0x3B2, true, name);
            else
                LocalOverheadMessage(MessageType.Regular, 0x3B2, (int)o);   // probably a Cliloc we don't know
        }
        public override void ArmorStatus(BaseArmor armor, object o)
        {
            // probably: Your equipment is severely damaged.
            if (o is string name && !string.IsNullOrEmpty(name))
                LocalOverheadMessage(MessageType.Regular, 0x3B2, true, name);
            else
                LocalOverheadMessage(MessageType.Regular, 0x3B2, (int)o);   // probably a Cliloc we don't know
        }
        public override void ClothingStatus(BaseClothing armor, object o)
        {
            // probably: Your clothing is severely damaged.
            if (o is string name && !string.IsNullOrEmpty(name))
                LocalOverheadMessage(MessageType.Regular, 0x3B2, true, name);
            else
                LocalOverheadMessage(MessageType.Regular, 0x3B2, (int)o);   // probably a Cliloc we don't know
        }
        public override void Destroyed(Item item)
        {
            LocalOverheadMessage(MessageType.Regular, 0x3B2, true, string.Format("{0} Destroyed!", item.GetType().Name));
        }
        public override void AmmoStatus(Item ammo)
        {
        }
        #endregion 
        #region RunUO2.6 Port compatibility functions
        public bool Young
        {
            get { return false; }
            set {; }
        }
        #endregion RunUO2.6 Port compatibility functions
        #region Event Score
        private ushort m_EventScore = 0;
        [CommandProperty(AccessLevel.Counselor)]
        public ushort EventScore
        {
            get { return m_EventScore; }
            set { m_EventScore = value; }
        }
        #endregion
        #region Save Flags (optimize read/write data)
        [Flags]
        private enum SaveFlag
        {
            None = 0x0,             // none
            NPCGuild = 0x01,        // save npc guild releated data if set
            ZCodeMiniGame = 0x02,   // Does this player have a saved ZCode Mini Game?
            EthicPoints = 0x04,     // Does this player have ethic pre enrollment points?
            ExpireStates = 0x08,    // Does this player have expiring states?
            EventScore = 0x10,      // Does this player have an event score?
            JumpList = 0x20,        // Does this player have a jump list?
            SiegeBlessedItem = 0x40,// Does this player have a Siege Blessed Item?
            RoTData = 0x80,         // has RotData been initialized?
            MinimumSentence = 0x100,// have they a Minimum Sentence they need to serve in prison?
            MurderReprieve = 0x200, // They are in factions and got a Murder Reprieve
            GMHistory = 0x400,      // player had at least one GM under ROT
        }
        private void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
                flags |= toSet;
        }

        private bool GetSaveFlag(SaveFlag flags, SaveFlag toGet)
        {
            return ((flags & toGet) != 0);
        }

        #endregion
        #region Ghost Blindness
        private DateTime m_SightExpire = DateTime.MaxValue;
        private bool m_Blind = false;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Blind
        {
            get
            {
                return m_Blind;
            }
            set
            {
                if (m_Blind != value)
                {
                    m_Blind = value;

                    try
                    {
                        if (Map != null)
                        {
                            Packet p = null;

                            IPooledEnumerable eable = Map.GetObjectsInRange(Location);

                            foreach (object ob in eable)
                            {
                                if (ob == null)
                                    continue;

                                // if we cannot see those (because we are blind), remove them from view
                                if (!this.CanSee(ob))
                                {
                                    if (p == null)
                                        if (ob is Item)
                                            p = (ob as Item).RemovePacket;
                                        else if (ob is Mobile)
                                            p = (ob as Mobile).RemovePacket;

                                    this.Send(p);
                                    p = null;
                                }
                                else
                                {
                                    if (ob is Mobile)
                                    {
                                        this.Send(new MobileIncoming(this, (ob as Mobile)));

                                        if (ObjectPropertyList.Enabled)
                                            this.Send(OPLPacket);
                                    }

                                    if (ob is Item && this.NetState != null)
                                        (ob as Item).SendInfoTo(this.NetState);
                                }
                            }

                            eable.Free();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                    }
                }
            }
        }
        private bool RegionAllowsSight()
        {
            #region Static Region
            StaticRegion sr = StaticRegion.FindStaticRegion(this);
            if (sr != null && !sr.GhostBlindness)
                return true;
            #endregion
            return false;
        }
        // called on a timer
        private void GoBlind()
        {   // no blindness on other shards
            if (Core.RuleSets.AllowGhostBlindness())
            {
                // process rules
                // make sure the player is dead and the timer has not been reset
                bool result = true;
                result = result && this.Alive == false;                 // we're not alive now
                result = result && m_SightExpire != DateTime.MaxValue;  // the timer hasn't been reset
                result = result && DateTime.UtcNow >= m_SightExpire;       // this call is a result of the last sightexpire time
                bool shouldGoBlind = result;                            // record if we shouldGoBlind
                bool GhostSight = RegionAllowsSight();                  // does this region allow sight for ghosts
                result = result && GhostSight == false;                 // rule result

                // okay, looks like we should make the player blind         
                if (result == true)
                {
                    Blind = true;                       // go blind
                    m_SightExpire = DateTime.MaxValue;  // kill timer
                    SendMessage("You feel yourself slipping into the ethereal world.");
                }
                // if the ghost shouldGoBlind BUT the RegionAllowsSight, reschedule blindness
                else if (shouldGoBlind == true && GhostSight == true)
                    Timer.DelayCall(TimeSpan.FromMinutes(1), new TimerCallback(GoBlind));
            }
        }
        #endregion
        #region Angel Island NPC points system (Shorthand for NpcGuildPoints)
        // [view shopkeeper points through the NPCGuildPoints]
        public double ShopkeeperPoints
        {
            get { return m_NpcGuildPoints; }
            set { m_NpcGuildPoints = value; }
        }
        // [view this property through the NpcGuild property]
        public bool Shopkeeper
        {
            get { return NpcGuild == NpcGuild.MerchantsGuild; }
        }
        public double ComposerPoints
        {
            get { return m_NpcGuildPoints; }
            set { m_NpcGuildPoints = value; }
        }
        // [view this property through the NpcGuild property]
        public bool Composer
        {
            get { return NpcGuild == NpcGuild.BardsGuild; }
        }
        #endregion Angel Island NPC points system
        #region JUMP_NEXT
        // object list of things to jump to.
        // see the [next command
        private ArrayList m_JumpList;
        public ArrayList JumpList { get { return m_JumpList; } set { m_JumpList = value; } }
        private int m_JumpIndex;
        public int JumpIndex { get { return m_JumpIndex; } set { m_JumpIndex = value; } }
        #endregion JUMP_NEXT
        #region Minimum Sentence
        // In addition to murder counts and criminal counts, prison now supports the notion of a minimum sentence.
        // We use this we imprison players for various bad behavior when they may not be a murderer.
        DateTime m_MinimumSentence = DateTime.MinValue;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public DateTime MinimumSentence
        {   // when the player will get out of prison
            get { return m_MinimumSentence; }
            set { m_MinimumSentence = value; }
        }
        #endregion Minimum Sentence
        #region CriminalCounts
        private UInt32 m_CriminalCounts = 0;
        public UInt32 CriminalCounts
        {
            get { return m_CriminalCounts; }
            set { m_CriminalCounts = value; }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public UInt32 ShortTermCriminalCounts
        {
            get { return m_CriminalCounts & 0x0000ffff; }
            //                        (turn off low bits------------) OR(value trimmed of high bits)
            set { m_CriminalCounts = ((m_CriminalCounts & 0xffff0000) | (value & 0x0000ffff)); }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public UInt32 LongTermCriminalCounts
        {
            get { return (m_CriminalCounts & 0xffff0000) >> 16; }
            //                        (turn off high bits-----------) OR(value shifted to hiword)
            set { m_CriminalCounts = ((m_CriminalCounts & 0x0000ffff) | (value << 16)); }
        }
        #endregion CriminalCounts
        #region Craft Context
        private object m_lastCraftObject;
        public object LastCraftObject { get { return m_lastCraftObject; } set { m_lastCraftObject = value; } }
        #endregion
        #region Secure Trade
        public override bool CheckTrade(Mobile to, Item item, SecureTradeContainer cont, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            int msgNum = 0;

            if (cont == null)
            {
                if (to.Holding != null)
                    msgNum = 1062727; // You cannot trade with someone who is dragging something.
                else if (this.HasTrade)
                    msgNum = 1062781; // You are already trading with someone else!
                else if (to.HasTrade)
                    msgNum = 1062779; // That person is already involved in a trade
            }

            if (msgNum == 0)
            {
                if (cont != null)
                {
                    plusItems += cont.TotalItems;
                    plusWeight += cont.TotalWeight;
                }

                if (this.Backpack == null || !this.Backpack.CheckHold(this, item, false, checkItems, plusItems, plusWeight))
                    msgNum = 1004040; // You would not be able to hold this if the trade failed.
                else if (to.Backpack == null || !to.Backpack.CheckHold(to, item, false, checkItems, plusItems, plusWeight))
                    msgNum = 1004039; // The recipient of this trade would not be able to carry this.
                else
                    msgNum = CheckContentForTrade(item);
            }

            if (msgNum != 0)
            {
                if (message)
                    this.SendLocalizedMessage(msgNum);

                return false;
            }

            return true;
        }
        private static int CheckContentForTrade(Item item)
        {
            if (item is TrapableContainer && ((TrapableContainer)item).TrapType != TrapType.None)
                return 1004044; // You may not trade trapped items.

            //if (SkillHandlers.StolenItem.IsStolen(item))
            //return 1004043; // You may not trade recently stolen items.

            if (item is Container)
            {
                foreach (Item subItem in item.Items)
                {
                    int msg = CheckContentForTrade(subItem);

                    if (msg != 0)
                        return msg;
                }
            }

            return 0;
        }
        #endregion Secure Trade
        #region UseSkill
        // 9/28/2021, Adam
        //  We now inform our pets's AI what skill we are using.
        //  This is useful for pets which are familiars, like the Basilisk.
        public override void OnAfterUseSkill(SkillName name)
        {
            base.OnAfterUseSkill(name);

            if (FollowerCount > 0)
            {   // locate our followers
                List<BaseCreature> list = new List<BaseCreature>();
                IPooledEnumerable eable = this.Map.GetMobilesInRange(this.Location, 13/*tiles*/);
                foreach (Mobile m in eable)
                    if (m is BaseCreature bc)
                        if (bc.ControlMaster == this)
                            list.Add(bc);

                eable.Free();
                // tell each one what skill we have used.
                foreach (BaseCreature bc in list)
                    if (bc.AIObject != null)
                        bc.AIObject.OnMasterUseSkill(name);
            }
        }
        public override void OnAfterUseSpell(Type name)
        {
            base.OnAfterUseSpell(name);

            if (FollowerCount > 0)
            {   // locate our followers
                List<BaseCreature> list = new List<BaseCreature>();
                IPooledEnumerable eable = this.Map.GetMobilesInRange(this.Location, 13/*tiles*/);
                foreach (Mobile m in eable)
                    if (m is BaseCreature bc)
                        if (bc.ControlMaster == this)
                            list.Add(bc);

                eable.Free();
                // tell each one what skill we have used.
                foreach (BaseCreature bc in list)
                    if (bc.AIObject != null)
                        bc.AIObject.OnMasterUseSpell(name);
            }
        }
        #endregion UseSkill
        #region AccountHardwareHash
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public uint HardwareHash
        {
            get { return (this.Account is Account ? (uint)((Account)this.Account).HardwareHashRaw : 0); }
            set
            {
                if (this.Account is Account)
                    ((Account)this.Account).HardwareHash = (int)value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime HardwareHashAcquired
        {
            get { return (this.Account is Account ? ((Account)this.Account).HardwareHashAcquired : DateTime.MinValue); }
        }
        #endregion AccountHardwareHash
        #region Siege Bless
        public Mobile CalcBlessedFor(Item item)
        {
            if ((Core.RuleSets.SiegeStyleRules()) && CanBlessFor(item))
            {
                SiegeUnBlessItem();
                return SiegeBlessItem(item);
            }
            else
                return null;

            return null;
        }
        public override void TrackedItemParentChanging(Item item)
        {
            if (Core.RuleSets.SiegeStyleRules())
                if (item.BlessedFor == this && SiegeBlessedItem == item)
                    SiegeUnBlessItem();
            return;
        }
        public bool CanBlessFor(Item item)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                if (Alive)
                    if (item.LootType == LootType.Regular)
                        return true;
            }
            return false;
        }
        public Mobile SiegeBlessItem(Item item)
        {
            if ((Core.RuleSets.SiegeStyleRules()) && CanBlessFor(item))
            {
                item.BlessedFor = this;
                SiegeBlessedItem = item;

                // SiegeBlessed can only be set programmatically, not by a GM with [props
                //  ApiKey ensures this
                SiegeBlessedItem.LootType = LootType.ApiKey | LootType.SiegeBlessed;

                return item.BlessedFor;
            }
            return null;
        }
        public void SiegeUnBlessItem()
        {
            if ((Core.RuleSets.SiegeStyleRules()) && SiegeBlessedItem != null)
            {
                SiegeBlessedItem.BlessedFor = null;
                SiegeBlessedItem.LootType = LootType.Regular;
                SiegeBlessedItem = null;
            }
        }
        private Item m_siegeBlessedItem;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item SiegeBlessedItem
        {
            get { return m_siegeBlessedItem; }
            set
            {
                if (Core.RuleSets.SiegeStyleRules())
                {
                    if (value != null)
                        Utility.ConsoleWriteLine("Setting:{0} as Siege Blessed", ConsoleColor.Red, value);
                    else
                        Utility.ConsoleWriteLine("Unsetting:{0} as Siege Blessed", ConsoleColor.Red, m_siegeBlessedItem);

                    m_siegeBlessedItem = value;
                }
            }
        }


        #endregion Siege Bless
        #region Alignment

        [CommandProperty(AccessLevel.GameMaster)]
        public AlignmentType GuildAlignment
        {
            get { return AlignmentSystem.Find(this); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AlignmentPlayer AlignmentPlayer
        {
            get { return AlignmentPlayer.Find(this, false); }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsAlignmentTraitor
        {
            get { return AlignmentSystem.IsTraitor(this); }
            set
            {
                if (value)
                    AlignmentSystem.ResetTraitor(this);
                else
                    AlignmentSystem.ClearTraitor(this);
            }
        }

        private TitleDisplay m_AlignmentTitleDisplay = TitleDisplay.Default;

        [CommandProperty(AccessLevel.GameMaster)]
        public TitleDisplay AlignmentTitleDisplay
        {
            get { return m_AlignmentTitleDisplay; }
            set { m_AlignmentTitleDisplay = value; }
        }

        #endregion
        #region Player Bool Table
        public bool GetPlayerBool(PlayerBoolTable flag)
        {
            return ((m_Flags & flag) != 0);
        }
        public void SetPlayerBool(PlayerBoolTable flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }
        #endregion Player Bool Table

        [CommandProperty(AccessLevel.Owner)]
        public bool InMemoriam
        {
            get { return GetPlayerBool(PlayerBoolTable.InMemoriam); }
            set
            {
                SetPlayerBool(PlayerBoolTable.InMemoriam, value);

                this.Blessed = value;
                this.CantWalkLand = value;                              // just in case, heh
                this.Frozen = value;
                this.Direction = Direction.South;
                if (value == true)
                    this.MoveToWorld(this.LogoutLocation, Map.Felucca); // will need to update if we ever support other facets
                else if (NetState == null)
                    this.MoveToWorld(this.LogoutLocation, Map.Internal);
            }
        }

        private int m_RewardHue = 0;
        [CommandProperty(AccessLevel.Owner)]
        public int RewardHue { get { return m_RewardHue; } set { m_RewardHue = value; } }

        private const int STRBonusCapDefault = 140; // 100 STR + legal buffs
        public DateTime m_LastResynchTime;
        public DateTime LastLogin
        {
            get { return (this.Account is Account ? ((Account)this.Account).LastLogin : DateTime.MinValue); }
        }
        public bool IsStaff
        {
            get { return this.AccessLevel > AccessLevel.Player; }
        }

        private bool AllowRedsToKeepNewbieItems
        {
            get { return false; }
        }

        private bool m_FilterMusic = false;

        public bool FilterMusic
        {
            get { return m_FilterMusic; }
            set { m_FilterMusic = value; }
        }

        private struct SpeechRecordEntry
        {
            public DateTime Time;
            public string Speech;

            public SpeechRecordEntry(string text)
            {
                Speech = text;
                Time = DateTime.UtcNow;
            }
        }

        private Queue m_SpeechRecord;

        public Queue SpeechRecord
        {
            get
            {
                return m_SpeechRecord;
            }
        }

        private DateTime m_Reported;
        private LogHelper m_ReportLogger;
        private Timer m_ReportLogStopper;
        private TimeSpan ReportTime { get { return TimeSpan.FromMinutes(5); } }

        private DesignContext m_DesignContext;

        private Region LastRegion = null;

        private DateTime m_LastGuildChange;
        private IOBAlignment m_LastGuildIOBAlignment;

        public override void OnGuildChange(Server.Guilds.BaseGuild oldGuild)
        {
            //InvalidateMyRunUO();

            m_LastGuildChange = DateTime.UtcNow;
            Guilds.Guild og = oldGuild as Guilds.Guild;
            if (og != null)
            {
                m_LastGuildIOBAlignment = og.IOBAlignment;
            }
            else
            {
                m_LastGuildIOBAlignment = IOBAlignment.None;
            }

            base.OnGuildChange(oldGuild);
        }

        #region Ethics
        /// <summary>
        /// EthicKillsLog tracks the pre ethic enrollment kills.
        /// When a player that is not in the ethics system kills an Evil player, they gain EthicPoints.
        /// When this player has N points, they become Good and the EthicKillsLog is deleted.
        /// </summary>
        public class EthicKillsLog
        {
            private Serial m_serial;
            private DateTime m_killed;
            public Serial Serial { get { return m_serial; } }
            public DateTime Killed { get { return m_killed; } }
            public bool Expired { get { return m_killed + TimeSpan.FromDays(5) < DateTime.UtcNow; } }  // 5 day decay per kill
            public EthicKillsLog(Serial serial, DateTime killed)
            {
                m_serial = serial;
                m_killed = killed;
            }
        }

        private List<EthicKillsLog> m_EthicKillsLogList = new List<EthicKillsLog>();
        public List<EthicKillsLog> EthicKillsLogList { get { return m_EthicKillsLogList; } }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int EthicPoints
        {
            get
            {
                int valid = 0;
                for (int ix = 0; ix < EthicKillsLogList.Count; ix++)
                {
                    if (EthicKillsLogList[ix].Expired)
                        continue;
                    else
                        valid++;
                }
                return valid;
            }
            set
            {
                EthicKillsLogList.Clear();
                for (int ix = EthicPoints; ix < value; ix++)
                {   // phony up some kills for test purposes
                    EthicKillsLogList.Add(new EthicKillsLog(Serial.MinusOne, DateTime.UtcNow));
                }
            }
        }
        public override void OnFlagChange(ExpirationFlag es, bool set)
        {
            switch (es.FlagID)
            {
                case ExpirationFlagID.EvilNoto:
                    if (set == true)
                        this.SendLocalizedMessage(501093);  // Evil players can now attack you at will.
                    else
                        this.SendLocalizedMessage(501092);  // You are no longer attackable by evil players.
                    break;

                case ExpirationFlagID.MonsterIgnore:
                    if (set == true)
                        this.SendLocalizedMessage(503326); // You are now ignored by monsters.
                    else
                        this.SendLocalizedMessage(503325);  // You are no longer ignored by monsters.
                    break;
            }
        }
        #endregion

        #region IOB stuff

        /*private double m_IOBKillPoints;
		private double m_KinSoloPoints;
		private double m_KinTeamPoints;

		[CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
		public double KinPowerPoints
		{
			get { return m_IOBKillPoints; }
			set { m_IOBKillPoints = value; }
		}
		[CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
		public double KinSoloPoints
		{
			get { return m_KinSoloPoints; }
			set { m_KinSoloPoints = value; }
		}
		
		[CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
		public double KinTeamPoints
		{
			get { return m_KinTeamPoints; }
			set { m_KinTeamPoints = value; }
		}

		public double AwardKinPowerPoints(double points)
		{
			double awarded = 0;
			if (Engines.IOBSystem.KinSystemSettings.PointsEnabled)
			{
				if ((KinPowerPoints + points) >= 100.0)
				{
					awarded = 100.0 - KinPowerPoints;
					KinPowerPoints = 100.0;
				}
				else
				{
					awarded = points;
					KinPowerPoints += points;
				}
			}
			return awarded;
		}

		[CommandProperty(AccessLevel.Counselor)]
		public bool IsInStatloss
		{
			get
			{
				if (Engines.IOBSystem.KinSystemSettings.StatLossEnabled)
				{
					if (this.StatModCount > 0)
					{
						foreach (object o in this.StatMods)
						{
							if (o is Engines.IOBSystem.KinStatlossSkillMod
								|| o is Engines.IOBSystem.KinHealerStatlossSkillMod)
							{
								return true;
							}
						}
					}
				}
				return false;
			}
		}

		public void RemoveStatlossSkillMods()
		{
			this.RemoveSkillModsOfType(typeof(Engines.IOBSystem.KinStatlossSkillMod));
			this.RemoveSkillModsOfType(typeof(Engines.IOBSystem.KinHealerStatlossSkillMod));
		}
		*/
        //private IOBAlignment m_IOBAlignment;
        private bool m_IOBEquipped;

        private DateTime m_KinAggressionTime = DateTime.MinValue;
        private DateTime m_KinBeneficialTime = DateTime.MinValue;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime KinAggressionTime
        {
            get { return m_KinAggressionTime; }
            set { m_KinAggressionTime = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public DateTime KinBeneficialTime
        {
            get { return m_KinBeneficialTime; }
            set { m_KinBeneficialTime = value; }
        }

        public void OnKinAggression()
        {
            KinAggressionTime = DateTime.UtcNow + TimeSpan.FromMinutes(5.0/*Engines.IOBSystem.KinSystemSettings.KinAggressionMinutes*/);
        }

        public void OnKinBeneficial()
        {
            //if (Engines.IOBSystem.KinSystemSettings.KinNameHueEnabled)
            {
                if (KinBeneficialTime < DateTime.UtcNow)
                {
                    //If we're currently NOT outcast due to healing:
                    this.SendMessage("You have done a beneficial action on a kin, you are now participating in the kin system.");
                    this.SendMessage("You are freely attackable by everyone in the kin system.");
                    if (false /*Engines.IOBSystem.KinSystemSettings.StatLossEnabled*/)
                    {
                        this.SendMessage("If you die to other kin system participants, you will suffer stat loss.");
                        this.SendMessage("This will be in effect for {0:0.00} minutes from the last beneficial action to kin that you perform.", 1440.0);
                    }
                }

                KinBeneficialTime = DateTime.UtcNow + TimeSpan.FromMinutes(1440.0);    //default: one day (24 * 60)
            }
        }

        //[CommandProperty(AccessLevel.GameMaster)]
        public IOBAlignment IOBAlignment
        {
            get
            {
                if (KinAggressionTime > DateTime.UtcNow)
                {
                    return IOBAlignment.OutCast;
                }
                if (KinBeneficialTime > DateTime.UtcNow)
                {
                    return IOBAlignment.Healer;
                }

                return IOBRealAlignment;
            }
        }

        //[CommandProperty(AccessLevel.GameMaster)]
        public IOBAlignment IOBRealAlignment
        {
            get
            {
                if (this.Guild != null)
                {
                    Guilds.Guild g = this.Guild as Guilds.Guild;
                    if (g != null)
                    {
                        return g.IOBAlignment;
                    }
                }
                if (this.m_LastGuildIOBAlignment != IOBAlignment.None)
                {
                    if (this.m_LastGuildChange + TimeSpan.FromDays(7.0) > DateTime.UtcNow)
                    {
                        return this.m_LastGuildIOBAlignment;
                    }
                }
                return IOBAlignment.None;
            }
        }

        public bool IsRealFactioner
        {
            get
            {
                if (IOBAlignment == IOBAlignment.None)
                {
                    return false;
                }

                if (IOBAlignment == IOBAlignment.OutCast || IOBAlignment == IOBAlignment.Healer)
                {
                    if (this.Guild != null)
                    {
                        Guilds.Guild g = this.Guild as Guilds.Guild;
                        if (g != null && g.IOBAlignment == IOBAlignment.None)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (this.m_LastGuildIOBAlignment != IOBAlignment.None
                            && this.m_LastGuildChange.AddDays(7.0) > DateTime.UtcNow)
                        {
                            return true;
                        }
                        else
                        {
                            //if no guild and outcast, then we're not really aligned.
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public bool OnEquippedIOBItem(IOBAlignment iobalignment)
        {
            if (this.IOBEquipped == false)
            {
                this.IOBEquipped = true;
            }

            return IOBEquipped;
        }

        private TimeSpan m_IOBRankTime;
        private DateTime m_IOBStartedWearing;

        private DateTime m_IOBJoinRestrictionTime;

        //[CommandProperty(AccessLevel.GameMaster)]
        public DateTime IOBJoinRestrictionTime
        {
            get { return m_IOBJoinRestrictionTime; }
            set { m_IOBJoinRestrictionTime = value; }
        }

        //[CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan IOBRankTime
        {
            get
            {
                if (IOBEquipped && m_IOBStartedWearing > DateTime.MinValue)
                {
                    return (m_IOBRankTime + (DateTime.UtcNow - m_IOBStartedWearing));
                }
                return m_IOBRankTime;
            }
            set { m_IOBRankTime = value; }
        }

        //[CommandProperty(AccessLevel.GameMaster)]
        public IOBRank IOBRank
        {
            get
            {
                if (IOBAlignment != IOBAlignment.None)
                {
                    TimeSpan totalRankTime = m_IOBRankTime; //this is to keep track of "running rank time" - basically ranktime + time logged-in and wearing
                    if (this.m_IOBStartedWearing > DateTime.MinValue)
                    {
                        totalRankTime += (DateTime.UtcNow - m_IOBStartedWearing);
                    }

                    if (totalRankTime > TimeSpan.FromHours(72.0))
                    {
                        return IOBRank.SecondTier;
                    }
                    else if (totalRankTime > TimeSpan.FromHours(36.0))
                    {
                        return IOBRank.FirstTier;
                    }
                    else
                    {
                        return IOBRank.None;
                    }
                }
                else
                {
                    return IOBRank.None;
                }
            }
        }

        public void ResetIOBRankTime()
        {
            m_IOBRankTime = TimeSpan.FromHours(0.0);
            if (IOBEquipped)
                m_IOBStartedWearing = DateTime.UtcNow;
        }

        //[CommandProperty(AccessLevel.GameMaster)]
        public bool IOBEquipped
        {
            get { return m_IOBEquipped; }
            set
            {
                if (value == false && m_IOBEquipped == true) //if we're going from true->false
                {
                    if (m_IOBStartedWearing > DateTime.MinValue) //make sure it's a valid value
                    {
                        m_IOBRankTime += (DateTime.UtcNow - m_IOBStartedWearing);
                        m_IOBStartedWearing = DateTime.MinValue;
                    }
                }
                else if (value == true && m_IOBEquipped == false) //if we're going from false->true
                {
                    m_IOBStartedWearing = DateTime.UtcNow;
                }

                m_IOBEquipped = value;
            }
        }

        #endregion

        private string m_WatchReason = "";
        [CommandProperty(AccessLevel.Counselor)]
        public string WatchReason
        {
            get { return m_WatchReason; }
            set { m_WatchReason = value; }
        }

        private DateTime m_WatchExpire;
        [CommandProperty(AccessLevel.Counselor)]
        public DateTime WatchExpire
        {
            get { return m_WatchExpire; }
            set { m_WatchExpire = value; }
        }

        private PlayerBoolTable m_Flags;
        private int m_StepsTaken;
        private int m_Profession;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Mortal
        {
            get { return GetPlayerBool(PlayerBoolTable.Mortal); }
            set { SetPlayerBool(PlayerBoolTable.Mortal, value); }
        }

        public Region LastRegionIn
        {
            get { return LastRegion; }
            set { LastRegion = value; }
        }

        private SkillName m_LastSkillUsed;  // wea: For recording last skill
        public SkillName LastSkillUsed
        {                                   // used & time for [FindSkill
            get
            {                               // ||---
                return m_LastSkillUsed;
            }
            set
            {
                m_LastSkillUsed = value;
            }
        }

        private DateTime m_LastSkillTime;
        public DateTime LastSkillTime
        {
            get
            {
                return m_LastSkillTime;
            }
            set
            {
                m_LastSkillTime = value;
            }
        }                                   // ------||

        // wea: Keeps check of last resurrect date/time
        private DateTime m_LastResurrectTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastResurrectTime
        {
            get
            {
                return m_LastResurrectTime;
            }
            set
            {
                m_LastResurrectTime = value;
            }
        }

        // wea: Added to control SpiritCohesion and how it affects
        // resurrection
        private int m_SpiritCohesion;

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpiritCohesion
        {
            get
            {
                return m_SpiritCohesion;
            }
            set
            {
                m_SpiritCohesion = value;
            }
        }

        // Pix - modified to use LastDeathTime property of PlayerMobile
        // Note - a return of true means they can ressurect
        //        a return of false means they can't resurrect
        public bool SpiritCohesive()
        {
            try
            {
                // Decrement SpiritCohesion according to LastResurrectTime
                if (SpiritCohesion > 0)
                {
                    SpiritCohesion -= (int)((DateTime.UtcNow - LastResurrectTime).TotalSeconds / CoreAI.CohesionLowerDelay);
                    if (SpiritCohesion < 0)
                    {
                        SpiritCohesion = 0;
                    }
                }

                TimeSpan TimeSinceDeath = (DateTime.UtcNow - LastDeathTime);
                TimeSpan CohesionTime = TimeSpan.FromSeconds(CoreAI.CohesionBaseDelay + (SpiritCohesion * CoreAI.CohesionFactor));

                if (TimeSinceDeath < CohesionTime)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Logging exception");
                LogHelper.LogException(e);
            }

            return true;
        }

        // wea: Keeps track of lag report times
        private DateTime m_LastLagTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastLagTime
        {
            get
            {
                return m_LastLagTime;
            }
            set
            {
                m_LastLagTime = value;
            }
        }


        // Temporary variable until we get flagging system working properly.
        //private DateTime m_NextMurderCountTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Profession
        {
            get { return m_Profession; }
            set { m_Profession = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StatModCount
        {
            get { return StatMods.Count; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RemoveAllStatMods
        {
            get { return false; }
            set
            {
                if (value)
                {
                    ClearStatMods();
                }
            }
        }

        public int StepsTaken
        {
            get { return m_StepsTaken; }
            set { m_StepsTaken = value; }
        }

        private double m_StamDrain; // Yoar: Accumulate stamina drain - not serialized

        public double StamDrain
        {
            get { return m_StamDrain; }
            set { m_StamDrain = value; }
        }

        #region ZCodeMiniGames
        private int m_ZCodeMiniGameID;
        public int ZCodeMiniGameID { get { return m_ZCodeMiniGameID; } set { m_ZCodeMiniGameID = value; } }
        private byte[] m_ZCodeMiniGameData;
        public byte[] ZCodeMiniGameData { get { return m_ZCodeMiniGameData; } set { m_ZCodeMiniGameData = value; } }
        #endregion

        #region NpcGuild
        private NpcGuild m_NpcGuild;
        private DateTime m_NpcGuildJoinTime;
        private TimeSpan m_NpcGuildGameTime;
        private double m_NpcGuildPoints;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public double NpcGuildPoints
        {
            get { return m_NpcGuildPoints; }
            set { m_NpcGuildPoints = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public NpcGuild NpcGuild
        {
            get { return m_NpcGuild; }
            set { m_NpcGuild = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NpcGuildJoinTime
        {
            get { return m_NpcGuildJoinTime; }
            set { m_NpcGuildJoinTime = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NpcGuildGameTime
        {
            get { return m_NpcGuildGameTime; }
            set { m_NpcGuildGameTime = value; }
        }
        public void OnNpcGuildJoin(NpcGuild newGuild)
        {
            // set membership
            m_NpcGuildPoints = 0;
            NpcGuild = newGuild;
            NpcGuildJoinTime = DateTime.UtcNow;
            NpcGuildGameTime = GameTime;

            if (NpcGuild == NpcGuild.BardsGuild)
            {   // Award player for work already done
                int sales, plays, publications;
                m_NpcGuildPoints = MusicBox.GetPublishingPoints(this, out sales, out plays, out publications);
            }
        }
        // called by the NPC guild master when a player resigns
        public void OnNpcGuildResign()
        {
            // clear membership
            m_NpcGuildPoints = 0;
            NpcGuild = NpcGuild.None;
            NpcGuildJoinTime = DateTime.MinValue;
            NpcGuildGameTime = TimeSpan.Zero;
        }
        #endregion NpcGuild

        [CommandProperty(AccessLevel.Administrator)]
        public PlayerBoolTable Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PagingSquelched
        {
            get { return GetPlayerBool(PlayerBoolTable.PagingSquelched); }
            set { SetPlayerBool(PlayerBoolTable.PagingSquelched, value); }
        }
        #region Factions
        /* Publish 8: https://uo.com/wiki/ultima-online-wiki/technical/previous-publishes/2000-2/2000-publish-08-5th-december/
         * Faction System Murder Reprieve
         * The faction murder reprieve will be a one-time reduction in the various types of murder counts that a murderer may have accumulated. This reprieve will be given only when a murderer who has not previously received a faction-related reprieve first joins a faction. The reprieve will carry the consequence of regaining all of your prior murder counts should the character become labeled as a murderer once again (i.e.: they get 5 or more short-term counts). This reprieve will only be offered for one month after it is published, after which the reprieve code will be disabled.
         * To get the reprieve, the murder must join the faction (as described in the faction system document) through the faction stone. Note this means murderers will not be able to join the Council of Mages or True Britannians factions unless they are part of a guild whose guild leader is able to get to the faction stone and join the faction.
         * Murderers who have more than five short-term or long-term murder counts who receive a reprieve will be set to four short-term and long-term murder counts and automatically become �blue�. Murderers whose �ping-pong� counter is 4 or greater will have that counter set to four. The system will retain the previous short-term, long-term, and ping-pong for these characters and should a reprieved murderer become a murderer again, all of these counts will be set to their original numbers.
         */
        public bool MurderReprieve
        {
            get { return GetPlayerBool(PlayerBoolTable.MurderReprieve); }
            set { SetPlayerBool(PlayerBoolTable.MurderReprieve, value); }
        }
        private int m_ReprieveLongTermMurders = 0;
        private int m_ReprieveShortTermMurders = 0;
        public int ReprieveShortTermMurders { get { return m_ReprieveShortTermMurders; } set { m_ReprieveShortTermMurders = value; } }
        public int ReprieveLongTermMurders { get { return m_ReprieveLongTermMurders; } set { m_ReprieveLongTermMurders = value; } }
        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Glassblowing
        {
            get { return GetPlayerBool(PlayerBoolTable.Glassblowing); }
            set { SetPlayerBool(PlayerBoolTable.Glassblowing, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Masonry
        {
            get { return GetPlayerBool(PlayerBoolTable.Masonry); }
            set { SetPlayerBool(PlayerBoolTable.Masonry, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SandMining
        {
            get { return GetPlayerBool(PlayerBoolTable.SandMining); }
            set { SetPlayerBool(PlayerBoolTable.SandMining, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool StoneMining
        {
            get { return GetPlayerBool(PlayerBoolTable.StoneMining); }
            set { SetPlayerBool(PlayerBoolTable.StoneMining, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool WoodEngraving
        {
            get { return GetPlayerBool(PlayerBoolTable.WoodEngraving); }
            set { SetPlayerBool(PlayerBoolTable.WoodEngraving, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Embroidering
        {
            get { return GetPlayerBool(PlayerBoolTable.Embroidering); }
            set { SetPlayerBool(PlayerBoolTable.Embroidering, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool LeatherEmbroidering
        {
            get { return GetPlayerBool(PlayerBoolTable.LeatherEmbroidering); }
            set { SetPlayerBool(PlayerBoolTable.LeatherEmbroidering, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShareBank
        {
            get { return GetPlayerBool(PlayerBoolTable.ShareBank); }
            set { SetPlayerBool(PlayerBoolTable.ShareBank, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Account.AccountInfraction InfractionStatus
        {
            get
            {
                Accounting.Account acct = this.Account as Accounting.Account;
                return acct != null ? acct.InfractionStatus : new Accounting.Account.AccountInfraction();
            }
            set
            {
                Accounting.Account acct = this.Account as Accounting.Account;
                if (acct != null)
                    acct.InfractionStatus = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Etching
        {
            get { return GetPlayerBool(PlayerBoolTable.Etching); }
            set { SetPlayerBool(PlayerBoolTable.Etching, value); }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public bool WatchList
        {
            get { return GetPlayerBool(PlayerBoolTable.Watched); }
            set { SetPlayerBool(PlayerBoolTable.Watched, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ToggleMiningStone
        {
            get { return GetPlayerBool(PlayerBoolTable.ToggleMiningStone); }
            set { SetPlayerBool(PlayerBoolTable.ToggleMiningStone, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool KarmaLocked
        {
            get { return GetPlayerBool(PlayerBoolTable.KarmaLocked); }
            set { SetPlayerBool(PlayerBoolTable.KarmaLocked, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AutoRenewInsurance
        {
            get { return GetPlayerBool(PlayerBoolTable.AutoRenewInsurance); }
            set { SetPlayerBool(PlayerBoolTable.AutoRenewInsurance, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseOwnFilter
        {
            get { return GetPlayerBool(PlayerBoolTable.UseOwnFilter); }
            set { SetPlayerBool(PlayerBoolTable.UseOwnFilter, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PublicMyRunUO
        {
            get { return GetPlayerBool(PlayerBoolTable.PublicMyRunUO); }
            set { SetPlayerBool(PlayerBoolTable.PublicMyRunUO, value); /*InvalidateMyRunUO();*/ }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PrisonInmate
        {
            get { return GetPlayerBool(PlayerBoolTable.PrisonInmate); }
            set
            {
                bool bWasInmate = GetPlayerBool(PlayerBoolTable.PrisonInmate);
                SetPlayerBool(PlayerBoolTable.PrisonInmate, value);
                if (value && !bWasInmate)
                {
                    //Going from non-inmate to inmate, make sure our counts are reduced
                    //reduce shorttermelapse to a max of 4 hours from now
                    if ((m_ShortTermElapse - GameTime) > TimeSpan.FromHours(4))
                        if (this.Alive)
                            m_ShortTermElapse = GameTime + TimeSpan.FromHours(4);
                        else
                            m_ShortTermElapse = GameTime + TimeSpan.FromHours(8);
                    //reduce longtermelapse to a max of 20 hours from now
                    if ((m_LongTermElapse - GameTime) > TimeSpan.FromHours(20))
                        if (this.Alive)
                            m_LongTermElapse = GameTime + TimeSpan.FromHours(20);
                        else
                            m_LongTermElapse = GameTime + TimeSpan.FromHours(40);
                }
                else if (!value && bWasInmate)
                {
                    //going from inmate to non-inmate,
                    //add back on the difference in long and short-term
                    //count times for non-inmates.
                    m_ShortTermElapse += TimeSpan.FromHours(4);
                    m_LongTermElapse += TimeSpan.FromHours(20);
                }
                else
                {
                    //setting flag to the same thing, don't touch anything
                }

                //InvalidateMyRunUO();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool PrisonVisitor
        {
            get { return GetPlayerBool(PlayerBoolTable.PrisonVisitor); }
            set { SetPlayerBool(PlayerBoolTable.PrisonVisitor, value); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public TimeSpan DecayTimeShort
        {
            get
            {
                if (CoreAI.OfflineShortsDecay != 0)
                {
                    //if we're using offline shortterm decay, return the min of online and offline decay
                    TimeSpan onlineDecay = m_ShortTermElapse - GameTime;
                    TimeSpan offlineDecay = (this.m_LastShortDecayed.AddHours(CoreAI.OfflineShortsDecayHours) - DateTime.UtcNow);

                    if (onlineDecay < offlineDecay)
                    {
                        return onlineDecay;
                    }
                    else
                    {
                        return offlineDecay;
                    }
                }
                else
                {
                    //not using offline decay ... just return online decay time
                    return m_ShortTermElapse - GameTime;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public TimeSpan DecayTimeLong
        {
            get { return m_LongTermElapse - GameTime; }
        }

        //public DateTime NextMurderCountTime
        //{
        //	get{ return m_NextMurderCountTime; }
        //	set{ m_NextMurderCountTime = value; }
        //}

        public ArrayList KillerTimes;

        public static Direction GetDirection4(Point3D from, Point3D to)
        {
            int dx = from.X - to.X;
            int dy = from.Y - to.Y;

            int rx = dx - dy;
            int ry = dx + dy;

            Direction ret;

            if (rx >= 0 && ry >= 0)
                ret = Direction.West;
            else if (rx >= 0 && ry < 0)
                ret = Direction.South;
            else if (rx < 0 && ry < 0)
                ret = Direction.East;
            else
                ret = Direction.North;

            return ret;
        }

        public override bool OnDroppedItemToWorld(Item item, Point3D location)
        {
            if (!base.OnDroppedItemToWorld(item, location))
                return false;

            //plasma, 03/12/07
            //Check here to see if we are trying to drop on an adjacent location
            //to a TillerMan, and if so prevent the drop.
            else if (!BaseBoat.DropFitResult(location, Map, Z))
                return false;
#if false
            // 4/25/23: Adam (Exploit)
            //  if the user canceled the target, then tries to place it on the ground,in town,
            //      disallow the placement. We hope it blows up in your backback BTW
            if (item is BaseExplosionPotion bep && bep.Armed)
                if (bep.InTown(this))
                {
                    this.SendMessage("You may not place that here."/* dickhead */);
                    return false;
                }
#endif
            BounceInfo bi = item.GetBounce();

            if (bi != null)
            {
                Type type = item.GetType();

                if (type.IsDefined(typeof(FurnitureAttribute), true) || type.IsDefined(typeof(DynamicFlipingAttribute), true))
                {
                    object[] objs = type.GetCustomAttributes(typeof(FlipableAttribute), true);

                    if (objs != null && objs.Length > 0)
                    {
                        FlipableAttribute fp = objs[0] as FlipableAttribute;

                        if (fp != null)
                        {
                            int[] itemIDs = fp.ItemIDs;

                            Point3D oldWorldLoc = bi.m_WorldLoc;
                            Point3D newWorldLoc = location;

                            if (oldWorldLoc.X != newWorldLoc.X || oldWorldLoc.Y != newWorldLoc.Y)
                            {
                                Direction dir = GetDirection4(oldWorldLoc, newWorldLoc);

                                if (itemIDs.Length == 2)
                                {
                                    switch (dir)
                                    {
                                        case Direction.North:
                                        case Direction.South: item.ItemID = itemIDs[0]; break;
                                        case Direction.East:
                                        case Direction.West: item.ItemID = itemIDs[1]; break;
                                    }
                                }
                                else if (itemIDs.Length == 4)
                                {
                                    switch (dir)
                                    {
                                        case Direction.South: item.ItemID = itemIDs[0]; break;
                                        case Direction.East: item.ItemID = itemIDs[1]; break;
                                        case Direction.North: item.ItemID = itemIDs[2]; break;
                                        case Direction.West: item.ItemID = itemIDs[3]; break;
                                    }
                                }
                            }
                        }
                    }
                }

            }

            return true;
        }

        public DesignContext DesignContext
        {
            get { return m_DesignContext; }
            set { m_DesignContext = value; }
        }

        public new static void Initialize()
        {
            PacketHandlers.RegisterThrottler(0x02, new ThrottlePacketCallback(MovementThrottle_Callback));
            Server.CommandSystem.Register("RoTStatus", AccessLevel.Player, new CommandEventHandler(RoTStatus));
            Server.CommandSystem.Register("RoTInfo", AccessLevel.Player, new CommandEventHandler(RoTInfo));
            Server.CommandSystem.Register("CheckSkill", AccessLevel.Player, new CommandEventHandler(CheckSkill));
            Server.CommandSystem.Register("CheckTargetSkill", AccessLevel.Player, new CommandEventHandler(CheckTargetSkill));
            Server.CommandSystem.Register("DeleteAccount", AccessLevel.Player, new CommandEventHandler(DeleteAccount));
            EventSink.Login += new LoginEventHandler(OnLogin);
            EventSink.Logout += new LogoutEventHandler(OnLogout);
            EventSink.Connected += new ConnectedEventHandler(EventSink_Connected);
            EventSink.Disconnected += new DisconnectedEventHandler(EventSink_Disconnected);
            TargetCommands.Register(new PlayerMobile.HideHiddenCommand());
        }

        private int m_LastGlobalLight = -1, m_LastPersonalLight = -1;

        public override void OnNetStateChanged()
        {
            m_LastGlobalLight = -1;
            m_LastPersonalLight = -1;
        }

        public override void ComputeBaseLightLevels(out int global, out int personal)
        {
            global = LightCycle.ComputeLevelFor(this);
            personal = this.LightLevel;
        }

        public override void CheckLightLevels(bool forceResend)
        {
            NetState ns = this.NetState;

            if (ns == null)
                return;

            int global, personal;

            ComputeLightLevels(out global, out personal);

            if (!forceResend)
                forceResend = (global != m_LastGlobalLight || personal != m_LastPersonalLight);

            if (!forceResend)
                return;

            m_LastGlobalLight = global;
            m_LastPersonalLight = personal;

            ns.Send(GlobalLightLevel.Instantiate(global));
            ns.Send(new PersonalLightLevel(this, personal));
        }
        private static void OnLogin(LoginEventArgs e)
        {
            Mobile from = e.Mobile;
            PlayerMobile pm = from as PlayerMobile;
            Accounting.Account acct = from.Account as Accounting.Account;

            #region HideHidden
            if (pm != null)
                pm.SetMobileBool(MobileBoolTable.HideHidden, false);
            #endregion HideHidden

            #region GUID
            // link players to their account GUID
            if (pm != null && acct != null)
                pm.GUID = acct.GUID;
            #endregion GUID

            #region BETA Rewards
            if (Core.ReleasePhase < ReleasePhase.Production)
                BetaRewardSystem.RecordBetaLogin(pm, acct);
            else
                BetaRewardSystem.RewardBetaTester(pm, acct);
            #endregion BETA Rewards

            #region Staff login with light
            if (pm != null && pm.AccessLevel > AccessLevel.Player)
                pm.LightLevel = 100;
            #endregion Staff login with light

            #region Logging
            // wea: log the fact that they've logged in
            if (e.Mobile != null)
                Server.Commands.CommandLogging.LogChangeClient(e.Mobile, true);
            #endregion Logging

            #region invoke magic properties of clothing and jewelry
            // adam: invoke magic clothing and jewelry on OnLogon
            if (pm != null)
            {
                try
                {
                    Item[] items = new Item[21];
                    items[0] = pm.FindItemOnLayer(Layer.Shoes);
                    items[1] = pm.FindItemOnLayer(Layer.Pants);
                    items[2] = pm.FindItemOnLayer(Layer.Shirt);
                    items[3] = pm.FindItemOnLayer(Layer.Helm);
                    items[4] = pm.FindItemOnLayer(Layer.Gloves);
                    items[5] = pm.FindItemOnLayer(Layer.Neck);
                    items[6] = pm.FindItemOnLayer(Layer.Waist);
                    items[7] = pm.FindItemOnLayer(Layer.InnerTorso);
                    items[8] = pm.FindItemOnLayer(Layer.MiddleTorso);
                    items[9] = pm.FindItemOnLayer(Layer.Arms);
                    items[10] = pm.FindItemOnLayer(Layer.Cloak);
                    items[11] = pm.FindItemOnLayer(Layer.OuterTorso);
                    items[12] = pm.FindItemOnLayer(Layer.OuterLegs);
                    items[13] = pm.FindItemOnLayer(Layer.InnerLegs);
                    items[14] = pm.FindItemOnLayer(Layer.Bracelet);
                    items[15] = pm.FindItemOnLayer(Layer.Ring);
                    items[16] = pm.FindItemOnLayer(Layer.Earrings);
                    items[17] = pm.FindItemOnLayer(Layer.OneHanded);
                    items[18] = pm.FindItemOnLayer(Layer.TwoHanded);
                    items[19] = pm.FindItemOnLayer(Layer.Hair);
                    items[20] = pm.FindItemOnLayer(Layer.FacialHair);
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items[i] != null)
                            MagicEquipment.OnAdded(pm, items[i]);
                    }
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Caught non-fatal exception in PlayerMobile.OnLogin: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
            }
            #endregion

            #region empty prisoners backpack
            // if this player is in prison and they have been logged out for 3 hours, empty their backpack
            //	this attempts to thwart fully loaded prison mules from being viable.
            //	Note: players can still stash stuff around the prison.
            if ((Core.RuleSets.AngelIslandRules()) && pm != null)
            {
                try
                {
                    // report how long this player has been offline
                    //Console.WriteLine("{1} has been offline since {0}.", pm.LastDisconnect, pm);
                    if (pm.AccessLevel == AccessLevel.Player && pm.Region != null && pm.Region.IsAngelIslandRules)
                    {
                        if (pm.Backpack != null)
                        {
                            TimeSpan ts = DateTime.UtcNow - pm.LastDisconnect;
                            if (ts.TotalHours >= 3)
                            {
                                int starting_count = pm.Backpack.FindAllItems().Count;
                                AIPUtils.RolledInYourSleep(pm);
                                if (starting_count > pm.Backpack.FindAllItems().Count)
                                {
                                    if (Utility.RandomBool())
                                        pm.SendMessage("You were rolled in your sleep and what was once yours is now theirs!");
                                    else
                                        pm.SendMessage("While you slept one of the guards went through your possessions and confiscated the contraband.");
                                }
                            }
                        }
                        else
                            System.Console.WriteLine("Player logged in with -null- backpack. Serial: " + pm.Serial.ToString());
                    }
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Caught non-fatal exception in PlayerMobile.OnLogin: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
            }
            #endregion

            #region Players on UOMortalis login hidden
            //	Players on UOMortalis login hidden to prevent camping of inns
            //	we require that the player has been logged out to at least
            if (Core.RuleSets.MortalisRules() && from is PlayerMobile)
            {
                try
                {
                    if (pm.AccessLevel == AccessLevel.Player)
                    {
                        TimeSpan ts = DateTime.UtcNow - pm.LastDisconnect;
                        if (ts >= Region.DefaultLogoutDelay + TimeSpan.FromSeconds(20.0))
                        {
                            pm.Hidden = true;
                        }
                    }
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Caught non-fatal exception in PlayerMobile.OnLogin: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
            }
            #endregion

            #region Players on UOMortalis login dead
            // if the server goes down before the Delete sequence finishes, we'll finish it here.
            if (Core.RuleSets.MortalisRules() && pm != null && pm.Alive == false && pm.Mortal)
            {
                pm.Frozen = true;
                Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(pm.MortalDeathMessage));
                if (pm.AccessLevel == AccessLevel.Player)
                    Timer.DelayCall(TimeSpan.FromSeconds(10.0), new TimerCallback(pm.Delete));
            }
            #endregion

            #region InmateLastDeathTime & LastResynchTime
            if (pm != null)
            {
                pm.m_LastResynchTime = DateTime.UtcNow;
                pm.m_InmateLastDeathTime = DateTime.UtcNow; //have to set this to now, otherwise it'd be exploitable.
            }
            #endregion

            #region recalculate follower control slots
            //recalculate follower control slots
            try
            {
                int slots = 0;
                Mobile master = from;

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is BaseCreature)
                    {
                        BaseCreature bc = (BaseCreature)m;

                        if ((bc.Controlled && bc.ControlMaster == master) || (bc.Summoned && bc.SummonMaster == master))
                        {
                            slots += bc.ControlSlots;
                        }
                    }
                }
                pm.FollowerCount = slots;
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Caught non-fatal exception in PlayerMobile.OnLogin: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
            #endregion

            #region Tell staff that a watchlist player has logged in
            //Tell staff that a watchlist player has logged in
            if (pm != null && pm.WatchList)
            {
                if (pm.WatchExpire > DateTime.UtcNow)
                {
                    // 6/8/2024, Adam: this isn't a case of wrong doing and the notice pops up often while I'm shooting a video.
                    if (CoreAI.WatchListShowExceptions)
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor,
                            0x482,
                            string.Format("WatchListed player {0} has logged in.", pm.Name));
                }
                else
                {
                    //clean up watching
                    pm.WatchList = false;
                }
            }

            if (acct != null && acct.Watched)
            {
                if (acct.WatchExpire > DateTime.UtcNow)
                {
                    // 6/8/2024, Adam: this isn't a case of wrong doing and the notice pops up often while I'm shooting a video.
                    if (CoreAI.WatchListShowExceptions)
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor,
                            0x482,
                            string.Format("WatchListed account {0} (char: {1}) has logged in.", acct.Username, pm.Name));
                }
                else
                {
                    //clean up watching
                    acct.Watched = false;
                }
            }
            #endregion

            #region Trying to use the 'preview house' exploit
            try
            {
                if (pm.Map != null)
                {
                    Sector s = pm.Map.GetSector(pm);
                    foreach (BaseMulti mul in s.Multis)
                    {
                        if (mul == null)
                            continue;

                        if (mul is PreviewHouse)
                        {
                            if (mul.Contains(pm))
                            {
                                RecordCheater.Cheater(pm, "Trying to use the 'preview house' exploit", true);
                                Server.Point3D jail = new Point3D(5295, 1174, 0);
                                pm.MoveToWorld(jail, Map.Felucca);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }
            #endregion

            #region Your guild's township has {0:0.00} days left in its fund before the township is demolished.
            if (pm != null)
            {
                try
                {
                    foreach (TownshipStone ts in TownshipStone.AllTownshipStones)
                    {
                        if (ts.IsMember(pm) && ts.RLDaysLeftInFund < 7.0)
                        {
                            string tsMessage = string.Format("Your guild's township has {0:0.00} days left in its fund before the township is demolished.", ts.RLDaysLeftInFund);
                            from.SendGump(new NoticeGump(1060637, 30720, tsMessage, 0xFFC000, 300, 140, null, null));
                        }
                    }
                }
                catch (Exception tse)
                {
                    LogHelper.LogException(tse, "Pixie: township 7- day warning");
                }
            }
            #endregion

            #region The server is currently under lockdown
            if (AccountHandler.LockdownLevel > AccessLevel.Player)
            {
                string notice;

                if (acct == null || !acct.HasAccess(from.NetState))
                {
                    if (from.AccessLevel == AccessLevel.Player)
                        notice = "The server is currently under lockdown. No players are allowed to log in at this time.";
                    else
                        notice = "The server is currently under lockdown. You do not have sufficient access level to connect.";

                    Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Disconnect), from);
                }
                else if (from.AccessLevel >= AccessLevel.Administrator)
                {
                    notice = "The server is currently under lockdown. As you are an administrator (or owner), you may change this from the [Admin gump.";
                }
                else
                {
                    notice = "The server is currently under lockdown. You have sufficient access level to connect.";
                }

                from.SendGump(new NoticeGump(1060637, 30720, notice, 0xFFC000, 300, 140, null, null));
            }
            #endregion

            #region Your account is not yet activated.
            Accounting.Account account = from.Account as Accounting.Account;
            if (account != null)
            {
                if (account.AccountActivated == false)
                {
                    from.SendMessage(0x35, "Your account is not yet activated.");
                    from.SendMessage(0x35, "Password recovery is not possible without account activation.");
                    from.SendMessage(0x35, "Please type [profile to activate your account.");
                }
            }
            #endregion

            #region Valid Client
#if false
            // now handled in Hardware Info
            if (acct != null && pm != null && pm.NetState != null)
            {
                if (acct.ValidClient == false)
                {
                    //pm.SendMessage(0x35, "Your client is out of date. Please update your client with the Launcher.");
                    Utility.ConsoleOut(string.Format("Invalid client detected for: {0}: Account '{1}'",
                        pm.NetState, acct.Username), ConsoleColor.Red);
                }
                else
                {
                    Utility.ConsoleOut(string.Format("Valid client detected for: {0}: Account '{1}'",
                        pm.NetState, acct.Username), ConsoleColor.Yellow);
                }

            }
#endif
            #endregion Valid Client
        }

        private bool m_NoDeltaRecursion;

        public void ValidateEquipment()
        {
            if (m_NoDeltaRecursion || Map == null || Map == Map.Internal)
                return;

            if (this.Items == null)
                return;

            m_NoDeltaRecursion = true;
            Timer.DelayCall(TimeSpan.Zero, new TimerCallback(ValidateEquipment_Sandbox));
        }

        private void ValidateEquipment_Sandbox()
        {
            try
            {
                if (Map == null || Map == Map.Internal)
                    return;

                List<Item> items = this.Items;

                if (items == null)
                    return;

                bool moved = false;

                int str = this.Str;
                int dex = this.Dex;
                int intel = this.Int;

                #region Factions
                int factionItemCount = 0;
                #endregion

                Mobile from = this;
                Container pack = from.Backpack;

                #region Ethics
                Ethics.Ethic ethic = Ethics.Ethic.Find(from);
                #endregion

                for (int i = items.Count - 1; i >= 0; --i)
                {
                    if (i >= items.Count)
                        continue;

                    Item item = (Item)items[i];

                    #region Ethics
                    if ((item.SavedFlags & 0x100) != 0)
                    {
                        if (item.Hue != Ethics.Ethic.Hero.Definition.PrimaryHue)
                        {
                            item.SavedFlags &= ~0x100;
                        }
                        else if (ethic != Ethics.Ethic.Hero)
                        {
                            from.AddToBackpack(item);
                            moved = true;
                            continue;
                        }
                    }
                    else if ((item.SavedFlags & 0x200) != 0)
                    {
                        if (item.Hue != Ethics.Ethic.Evil.Definition.PrimaryHue)
                        {
                            item.SavedFlags &= ~0x200;
                        }
                        else if (ethic != Ethics.Ethic.Evil)
                        {
                            from.AddToBackpack(item);
                            moved = true;
                            continue;
                        }
                    }
                    #endregion

                    if (item is BaseWeapon)
                    {
                        BaseWeapon weapon = (BaseWeapon)item;

                        bool drop = false;

                        if (dex < weapon.DexRequirement)
                            drop = true;
                        else if (str < AOS.Scale(weapon.StrRequirement, 100 - weapon.GetLowerStatReq()))
                            drop = true;
                        else if (intel < weapon.IntRequirement)
                            drop = true;

                        if (drop)
                        {
                            string name = weapon.Name;

                            if (name == null)
                                name = string.Format("#{0}", weapon.LabelNumber);

                            from.SendLocalizedMessage(1062001, name); // You can no longer wield your ~1_WEAPON~
                            from.AddToBackpack(weapon);
                            moved = true;
                        }
                    }
                    else if (item is BaseArmor)
                    {
                        BaseArmor armor = (BaseArmor)item;

                        bool drop = false;

                        if (!armor.AllowMaleWearer && from.Body.IsMale && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else if (!armor.AllowFemaleWearer && from.Body.IsFemale && from.AccessLevel < AccessLevel.GameMaster)
                        {
                            drop = true;
                        }
                        else
                        {
                            double strBonus = armor.ComputeStatBonus(StatType.Str, this);
                            double dexBonus = armor.ComputeStatBonus(StatType.Dex, this);
                            double intBonus = armor.ComputeStatBonus(StatType.Int, this);

                            int strReq = armor.ComputeStatReq(StatType.Str);
                            int dexReq = armor.ComputeStatReq(StatType.Dex);
                            int intReq = armor.ComputeStatReq(StatType.Int);

                            if (dex < dexReq || (dex + dexBonus) < 1)
                                drop = true;
                            else if (str < strReq || (str + strBonus) < 1)
                                drop = true;
                            else if (intel < intReq || (intel + intBonus) < 1)
                                drop = true;
                        }

                        if (drop)
                        {
                            string name = armor.Name;

                            if (name == null)
                                name = string.Format("#{0}", armor.LabelNumber);

                            if (armor is BaseShield)
                                from.SendLocalizedMessage(1062003, name); // You can no longer equip your ~1_SHIELD~
                            else
                                from.SendLocalizedMessage(1062002, name); // You can no longer wear your ~1_ARMOR~

                            from.AddToBackpack(armor);
                            moved = true;
                        }
                    }

                    #region Factions
                    FactionItem factionItem = FactionItem.Find(item);

                    if (factionItem != null)
                    {
                        bool drop = false;

                        Faction ourFaction = Faction.Find(this);

                        if (ourFaction == null || ourFaction != factionItem.Faction)
                            drop = true;
                        else if (++factionItemCount > FactionItem.GetMaxWearables(this))
                            drop = true;

                        if (drop)
                        {
                            if (Core.RuleSets.MLRules() && pack != null)
                                pack.DropItem(item);
                            else
                                from.AddToBackpack(item);

                            moved = true;
                        }
                    }
                    #endregion

                    #region Alignment
                    if (AlignmentSystem.Enabled && !AlignmentSystem.ValidateEquipment(this, item))
                    {
                        if (Core.RuleSets.MLRules() && pack != null)
                            pack.DropItem(item);
                        else
                            from.AddToBackpack(item);

                        moved = true;
                    }
                    #endregion
                }

                if (moved)
                    from.SendLocalizedMessage(500647); // Some equipment has been moved to your backpack.
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine(e);
            }
            finally
            {
                m_NoDeltaRecursion = false;
            }
        }

        public override void Delta(MobileDelta flag)
        {
            base.Delta(flag);

            if ((flag & MobileDelta.Stat) != 0)
                ValidateEquipment();

            //if ((flag & (MobileDelta.Name | MobileDelta.Hue)) != 0)
            //InvalidateMyRunUO();
        }

        private static void Disconnect(object state)
        {
            NetState ns = ((Mobile)state).NetState;

            if (ns != null)
                ns.Dispose();
        }

        private static void OnLogout(LogoutEventArgs e)
        {
        }

        private static void DeleteAccount(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || pm.Deleted)
                return;
            if (Core.ReleasePhase < ReleasePhase.Production)
            {
                if (pm.AccessLevel > AccessLevel.Player)
                {
                    pm.SendMessage("Staff may not delete accounts in this way.");
                    return;
                }
                if (pm.Account != null && pm.Account is Server.Accounting.Account acct)
                {
                    pm.SendMessage("Bye.");
                    acct.Delete();
                }
            }
            else
            {
                pm.SendMessage("You cannot run this command on a production shard.");
            }
        }
        private static void EventSink_Connected(ConnectedEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;

            if (pm != null)
            {
                Server.Accounting.Account acct = (pm.Account != null) ? pm.Account as Server.Accounting.Account : null;

                pm.m_SessionStart = DateTime.UtcNow;

                if (pm.m_Quest != null)
                    pm.m_Quest.StartTimer();

                if (pm.IOBEquipped)
                    pm.m_IOBStartedWearing = DateTime.UtcNow;

                pm.BedrollLogout = false;

                #region AccountInfraction
                if (acct != null && pm.NetState != null)
                {   // clear the infraction if they are just logging in the same character on the same account
                    bool relogin = IPAddressToPlayerLookup.ContainsKey(pm.NetState.Address) && IPAddressToPlayerLookup[pm.NetState.Address] == pm;
                    if (relogin && acct.InfractionStatus == AccountInfraction.concurrentIPLimiter)
                        acct.InfractionStatus = AccountInfraction.none;

                    if (acct.InfractionStatus != Accounting.Account.AccountInfraction.none)
                    {   // handle the bad boys here
                        Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(BadLogin), new object[] { pm, acct });
                    }
                }
                #endregion AccountInfraction

                #region IP Info
                // our IPInfo is collected asynchronously so we need to collect as soon as possible after logging in
                Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(SeedIpInfo), new object[] { pm, acct });
                #endregion IP Info

                #region LastLoggedinMobile
                if (pm.NetState != null)
                {
                    // Don't fail the login if they are just logging the same character as was just logged in on this account/IP.
                    //  A different account logging in on this IP will fail
                    //  This is part of our concurrent IP checking logic.
                    // see: Accounting.Account.AccountInfraction.concurrentIPLimiter
                    if (!IPAddressToPlayerLookup.ContainsKey(pm.NetState.Address))
                        IPAddressToPlayerLookup.Add(pm.NetState.Address, pm);
                    else
                        IPAddressToPlayerLookup[pm.NetState.Address] = pm;
                }
                #endregion LastLoggedinMobile
            }
        }
        public static void SeedIpInfo(object state)
        {
            object[] aState = (object[])state;
            if (aState == null || aState.Length != 2)
                return;

            PlayerMobile pm = aState[0] as PlayerMobile;
            NetState ns = pm.NetState;
            if (aState[1] != null && aState[1] is Server.Accounting.Account acct && ns != null)
            {
                if (ServerList.IsPrivateNetwork(ns.Address))
                {
                    Utility.ConsoleWriteLine("Ignoring IPInfo for private network {0}", ConsoleColor.Yellow, ns.Address);
                    return;
                }
                List<string> reason = null;
                CityResponse response = GetIpInfo(pm, ref reason);
                if (response != null)
                {
                    acct.SetFlag(IPFlags.IsAnonymous, response.Traits.IsAnonymous);
                    acct.SetFlag(IPFlags.IsAnonymousProxy, response.Traits.IsAnonymousProxy);
                    acct.SetFlag(IPFlags.IsAnonymousVpn, response.Traits.IsAnonymousVpn);
                    acct.SetFlag(IPFlags.IsHostingProvider, response.Traits.IsHostingProvider);
                    acct.SetFlag(IPFlags.IsLegitimateProxy, response.Traits.IsLegitimateProxy);
                    acct.SetFlag(IPFlags.IsPublicProxy, response.Traits.IsPublicProxy);
                    acct.SetFlag(IPFlags.IsResidentialProxy, response.Traits.IsResidentialProxy);
                    acct.SetFlag(IPFlags.IsSatelliteProvider, response.Traits.IsSatelliteProvider);
                    acct.SetFlag(IPFlags.IsTorExitNode, response.Traits.IsTorExitNode);

                    if (acct.IPFlagsSession != IPFlags.None)
                        pm.HandleIpCheater();
                }
                else
                {   // database busy, retry
                    if (reason != null && reason.Count > 0)
                    {
                        foreach (var line in reason)
                            Utility.ConsoleWriteLine("{0}: {1}", ConsoleColor.Yellow, ns.Address, line);
                        //Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(SeedIpInfo), new object[] { pm, acct });
                    }
                    else
                    {
                        Utility.ConsoleWriteLine("IpInfo database unavailable for:{0}: Account '{1}'.", ConsoleColor.Yellow, ns.Address, acct.Username);
                        //Utility.ConsoleOut("IpInfo database unavailable for:{0}: Account '{1}'. Retrying...", ConsoleColor.Yellow, ns.Address, acct.Username);
                        //Timer.DelayCall(TimeSpan.FromSeconds(2.0), new TimerStateCallback(SeedIpInfo), new object[] { pm, acct });
                    }
                }
            }
        }
        public bool HandleIpCheater()
        {   // called here from SeedIpInfo and in Cron every 2 minutes.
            bool handled = false;
            NetState ns = this.NetState;
            if (ns != null && ns.Mobile != null && ns.Mobile.Account != null)
            {
                Accounting.Account acct = ns.Mobile.Account as Accounting.Account;
                PlayerMobile pm = ns.Mobile as PlayerMobile;
                string reason = string.Empty;
                if (!World.IsSystemAcct(ns.Address))
                {
                    if (acct.GetFlag(Accounting.Account.IPFlags.IsTorExitNode))
                    {   // getting here on a IsTorExitNode is immediate prison. Not mortal, cause we don't want them just dying and crating another account

                        if (pm.Region != null && pm.Region.IsAngelIslandRules)
                            // they're already in prison
                            return false;

                        // First, set their login infraction. This is what the Warden checks to select your custom welcome message
                        acct.InfractionStatus = Accounting.Account.AccountInfraction.TorExitNode;

                        #region Mute all messages to mobile
                        // we temporarily mute all messages to the mobile.
                        //  We do this because landing in prison is a big deal, and we want them 
                        //  focused on what the warden has to say. (The warden will remove the lock)
                        pm.MuteMessages = true;
                        #endregion Mute all messages to mobile

                        #region Imprison them
                        reason = string.Format("Player blackholed with IP Flags {0}.", acct.IPFlagsPermanent | acct.IPFlagsSession);
                        Point3D px = Spawner.GetSpawnPosition(Map.Felucca, new Point3D(354, 836, 20), 2, SpawnFlags.None, pm);
                        pm.MinimumSentence = DateTime.UtcNow + TimeSpan.FromDays(91.6);    // 3 months
                        Prison.PrisonPlayer prison = new Prison.PrisonPlayer(null, pm, px, reason, sentence: 0);
                        prison.GoToPrison();
                        #endregion Imprison them

                        #region tell staff
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to prison. ({1})", pm, reason));
                        #endregion tell staff

                        #region Guild them
                        // add them to The Outcast guild but only if they are not guilded
                        if (pm.Guild != null)
                        {
                            foreach (BaseGuild g in BaseGuild.List.Values)
                            {
                                Guild guild = g as Guild;
                                if (guild != null && guild.NoResign)
                                {
                                    if (guild.Abbreviation.Contains("ToC", StringComparison.OrdinalIgnoreCase))
                                    {   // add him to the guild (located in prison)
                                        pm.Guild = guild;
                                        guild.AddMember(pm);
                                        pm.AlignmentTitleDisplay = TitleDisplay.Overhead;
                                        break;
                                    }
                                }
                            }
                        }
                        #endregion Guild them
                    }
                    else if ((acct.IPFlagsSession & ~Accounting.Account.IPFlags.IsLegitimateProxy) != 0)
                    {
                        Accounting.Account.IPFlags temp = acct.IPFlagsSession & ~Accounting.Account.IPFlags.IsLegitimateProxy;
                        Utility.ConsoleWriteLine("{0}: Account '{1}'. logging in from ({2})", ConsoleColor.Red, ns.Address, acct.Username, temp.ToString());

                        #region FYI
                        /* FYI
                        public enum IPFlags
                        {
                            None = 0x0,
                            IsAnonymous         = 0x01,
                            IsAnonymousProxy    = 0x02,
                            IsAnonymousVpn      = 0x04,
                            IsHostingProvider   = 0x08,
                            IsLegitimateProxy   = 0x10,
                            IsPublicProxy       = 0x20,
                            IsResidentialProxy  = 0x40,
                            IsSatelliteProvider = 0x80,
                            IsTorExitNode       = 0x100,
                        } */
                        #endregion FYI

                        RefreshWatch(acct, string.Format("{2}{0}: {1}", ns.Address, temp.ToString(), !string.IsNullOrEmpty(acct.WatchReason) ? ", " : ""));
                    }
                    else if (ClientException.IsException(ns.Address))
                    {
                        RefreshWatch(acct, string.Format("{2}{0}: {1}", ns.Address, "Client has IP Exception", !string.IsNullOrEmpty(acct.WatchReason) ? ", " : ""));
                    }
                }
            }
            return true;
        }
        private void RefreshWatch(Accounting.Account acct, string text)
        {
            if (acct.WatchReason == null || acct.WatchReason.Length > 128)
                // set/reset
                acct.WatchReason = string.Empty;

            acct.Watched = true;
            acct.WatchReason += text;
            acct.WatchExpire = DateTime.UtcNow + TimeSpan.FromDays(30.0);
        }
        private static Dictionary<IPAddress, PlayerMobile> IPAddressToPlayerLookup = new();
        private static void BadLogin(object state)
        {
            object[] aState = (object[])state;
            PlayerMobile pm = aState[0] as PlayerMobile;
            if (aState[1] != null && aState[1] is Server.Accounting.Account acct && acct.InfractionStatus != Accounting.Account.AccountInfraction.none && pm.PrisonInmate == false)
            {
                string reason = string.Empty;
                if (acct.InfractionStatus == Accounting.Account.AccountInfraction.totalIPLimiter ||
                    acct.InfractionStatus == Accounting.Account.AccountInfraction.totalHardwareLimiter ||
                    acct.InfractionStatus == Accounting.Account.AccountInfraction.concurrentIPLimiter)
                {
                    #region Mute all messages to mobile
                    // we temporarily mute all messages to the mobile.
                    //  We do this because landing in prison is a big deal, and we want them 
                    //  focused on what the warden has to say. (The warden will remove the lock)
                    pm.MuteMessages = true;
                    #endregion Mute all messages to mobile

                    #region Resurrect
                    // since we will be setting them to Mortal, we will need to resurrect them first
                    if (pm.Dead)
                        pm.Resurrect();
                    #endregion Resurrect

                    #region Make the Mortal
                    #region Offenses
                    /*
                     * The supported login errors are 
                     *  accountConcurrentIPLimiter,
                     *  accountTotalIPLimiter,
                     *  accountHardwareLimiter,
                     *  IPStillHot,
                     */
                    #endregion Offenses
                    // only for the most egregious offenders
                    if (acct.InfractionStatus != Accounting.Account.AccountInfraction.concurrentIPLimiter)
                        pm.Mortal = true;
                    #endregion Make the Mortal

                    #region Imprison them
                    reason = string.Format("Player blackholed with LoginStatus {0}.", acct.InfractionStatus);
                    Point3D px = Spawner.GetSpawnPosition(Map.Felucca, new Point3D(354, 836, 20), 2, SpawnFlags.None, pm);
                    if (acct.InfractionStatus != Accounting.Account.AccountInfraction.concurrentIPLimiter)
                        pm.MinimumSentence = DateTime.UtcNow + TimeSpan.FromDays(91.6);    // 3 months
                    Prison.PrisonPlayer prison = new Prison.PrisonPlayer(null, pm, px, reason, sentence: 0);
                    prison.GoToPrison();
                    #endregion Imprison them

                    #region tell staff
                    Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                    Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to prison. ({1})", pm, reason));
                    #endregion tell staff

                    #region Guild them
                    if (acct.InfractionStatus != Accounting.Account.AccountInfraction.concurrentIPLimiter)
                    {
                        // add them to The Outcast guild but only if they are not guilded
                        if (pm.Guild == null)
                        {
                            foreach (BaseGuild g in BaseGuild.List.Values)
                            {
                                Guild guild = g as Guild;
                                if (guild != null && guild.NoResign)
                                {
                                    if (guild.Abbreviation.Contains("ToC", StringComparison.OrdinalIgnoreCase))
                                    {   // add him to the guild (located in prison)
                                        pm.Guild = guild;
                                        guild.AddMember(pm);
                                        pm.AlignmentTitleDisplay = TitleDisplay.Overhead;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    #endregion Guild them
                }
                else if (acct.InfractionStatus == Accounting.Account.AccountInfraction.TorExitNode)
                {
                    if (pm != null && pm.NetState != null && pm.NetState.Running)
                    {
                        #region Mute all messages to mobile
                        // we temporarily mute all messages to the mobile.
                        //  We do this because landing in prison is a big deal, and we want them 
                        //  focused on what the warden has to say. (The warden will remove the lock)
                        pm.MuteMessages = true;
                        #endregion Mute all messages to mobile

                        #region Imprison them
                        reason = string.Format("Player blackholed with LoginStatus {0}.", acct.InfractionStatus);
                        Point3D px = Spawner.GetSpawnPosition(Map.Felucca, new Point3D(354, 836, 20), 2, SpawnFlags.None, pm);
                        Prison.PrisonPlayer prison = new Prison.PrisonPlayer(null, pm, px, reason, sentence: 0);
                        prison.GoToPrison();
                        #endregion Imprison them

                        #region tell staff
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to prison. ({1})", pm, reason));
                        #endregion tell staff
                    }
                }
                else if (acct.InfractionStatus == Accounting.Account.AccountInfraction.IPStillHot)
                {
                    if (pm != null && pm.NetState != null && pm.NetState.Running)
                    {   // probably not a problem for Siege where they only have one character.
                        //  This 'kick' is to prevent players from trying to log in another account to help one of
                        //  their other accounts
                        pm.SendMessage("Your IP Address is still too hot. Try logging again in two minutes.");
                        pm.Say("I've been kicked!");
                        Utility.ConsoleWriteLine("{0} kicked, IPStillHot", ConsoleColor.Red, pm.Name);
                        LogHelper logger = new LogHelper("IPStillHot.log", false, true);
                        logger.Log(LogType.Mobile, pm, "kicked, IPStillHot");
                        logger.Finish();
                        //pm.NetState.Dispose();
                        Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(KickTick), new object[] { pm });
                        #region Clear State
                        acct.InfractionStatus = Accounting.Account.AccountInfraction.none;
                        #endregion  Clear State
                    }
                }
                #region log the fact
                if (reason != string.Empty)
                {
                    LogHelper Logger = new LogHelper("LoginStatusPrison.log", false, true);
                    Logger.Log(LogType.Mobile, pm, reason);
                    Logger.Finish();
                }
                #endregion log the fact
            }
        }
        private static void KickTick(object state)
        {
            object[] aState = (object[])state;
            PlayerMobile pm = aState[0] as PlayerMobile;
            if (pm != null && pm.NetState != null && pm.NetState.Running)
            {
                pm.NetState.Dispose();
            }
        }
        private static void EventSink_Disconnected(DisconnectedEventArgs e)
        {
            Mobile from = e.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client disconnected
				 *  - Remove design context
				 *  - Eject client from house
				 */

                // Remove design context
                DesignContext.Remove(from);

                // Eject client from house
                from.RevealingAction();

                from.MoveToWorld(context.Foundation.BanLocation, context.Foundation.Map);
            }

            PlayerMobile pm = e.Mobile as PlayerMobile;

            if (pm != null)
            {
                pm.m_GameTime += (DateTime.UtcNow - pm.m_SessionStart);

                if (pm.m_Quest != null)
                    pm.m_Quest.StopTimer();


                if (pm.IOBEquipped)
                {
                    if (pm.m_IOBStartedWearing > DateTime.MinValue)
                    {
                        pm.m_IOBRankTime += (DateTime.UtcNow - pm.m_IOBStartedWearing);
                    }
                }
                pm.m_IOBStartedWearing = DateTime.MinValue; //always set this to minvalue when logged out

                // record when this player went off line 
                pm.LastDisconnect = DateTime.UtcNow;
            }

            // wea: log the fact that they've disconnected
            Server.Commands.CommandLogging.LogChangeClient(e.Mobile, false);
        }

        public override void RevealingAction()
        {
            if (m_DesignContext != null)
                return;

            Spells.Sixth.InvisibilitySpell.RemoveTimer(this);

            base.RevealingAction();
        }

        public override void OnSubItemAdded(Item item)
        {
            if (AccessLevel < AccessLevel.GameMaster && item.IsChildOf(this.Backpack))
            {
                int maxWeight = WeightOverloading.GetMaxWeight(this);
                int curWeight = Mobile.BodyWeight + this.TotalWeight;

                if (curWeight > maxWeight)
                    this.SendLocalizedMessage(1019035, true, string.Format(" : {0} / {1}", curWeight, maxWeight));
            }
        }

        public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            // wea: added call to IsIsolatedFrom to prevent harmful actions in their entirety if mobile is isolated from *this*
            if (m_DesignContext != null || ((target is PlayerMobile) && (((PlayerMobile)target).m_DesignContext != null || ((PlayerMobile)target).IsIsolatedFrom(this))))
                return false;

            #region Static Region
            if (target != null && this.Region != target.Region)
            {
                StaticRegion sr = StaticRegion.FindStaticRegion(target);
                if (sr != null && sr.NoExternalHarmful && this.AccessLevel == AccessLevel.Player)
                {
                    this.SendMessage("You cannot harm them in that area.");
                    return false;
                }
            }
            #endregion

            //Adam: we no longer look at base type an instead check the IsInvulnerable flag directly
            //if ((target is BaseVendor && ((BaseVendor)target).IsInvulnerable) || target is PlayerVendor || target is TownCrier)
            if (target.IsInvulnerable)
            {
                bool immune = ((target is BaseVendor && ((BaseVendor)target).IsInvulnerable) || target is PlayerVendor || target is TownCrier);
                if (message)
                {
                    if (immune)
                    {
                        if (target.Title == null)
                            SendMessage("{0} the vendor cannot be harmed.", target.Name);
                        else
                            SendMessage("{0} {1} cannot be harmed.", target.Name, target.Title);
                    }
                    else
                        SendMessage("this {0} cannot be harmed.", target.GetType().Name);
                }

                return false;
            }

            return base.CanBeHarmful(target, message, ignoreOurBlessedness);
        }

        public override bool CanBeBeneficial(Mobile target, bool message, bool allowDead)
        {
            if (m_DesignContext != null || (target is PlayerMobile && ((PlayerMobile)target).m_DesignContext != null))
                return false;

            return base.CanBeBeneficial(target, message, allowDead);
        }

        public override bool CheckContextMenuDisplay(IEntity target)
        {
            return (m_DesignContext == null);
        }

        public override void OnItemAdded(Item item)
        {
            base.OnItemAdded(item);

            if (item is BaseArmor || item is BaseWeapon)
            {
                Hits = Hits; Stam = Stam; Mana = Mana;
            }

            if (item is BaseWeapon)
                this.HasAbilityReady = false;

            //InvalidateMyRunUO();

            if (AlignmentSystem.Enabled)
                AlignmentSystem.HandleEquip(this, item);
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            if (item is BaseArmor || item is BaseWeapon)
            {
                Hits = Hits; Stam = Stam; Mana = Mana;
            }

            if (item is BaseWeapon)
                this.HasAbilityReady = false;

            //InvalidateMyRunUO();

            if (AlignmentSystem.Enabled)
                AlignmentSystem.HandleUnequip(this, item);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override double ArmorRating
        {
            get
            {
                BaseArmor ar;
                double rating = 0.0;

                ar = NeckArmor as BaseArmor;
                if (ar != null)
                    rating += ar.ArmorRatingScaled;

                ar = HandArmor as BaseArmor;
                if (ar != null)
                    rating += ar.ArmorRatingScaled;

                ar = HeadArmor as BaseArmor;
                if (ar != null)
                    rating += ar.ArmorRatingScaled;

                ar = ArmsArmor as BaseArmor;
                if (ar != null)
                    rating += ar.ArmorRatingScaled;

                ar = LegsArmor as BaseArmor;
                if (ar != null)
                    rating += ar.ArmorRatingScaled;

                ar = ChestArmor as BaseArmor;
                if (ar != null)
                    rating += ar.ArmorRatingScaled;

                ar = ShieldArmor as BaseArmor;
                if (ar != null)
                    rating += ar.ArmorRatingScaled;

                return VirtualArmor + VirtualArmorMod + rating;
            }
        }

        public override int StrMax
        {
            get
            {
                return 100;
            }
            set
            {
            }
        }

        public override int IntMax
        {
            get
            {
                return 100;
            }
            set
            {
            }
        }

        public override int DexMax
        {
            get
            {
                return 100;
            }
            set
            {
            }
        }

        public override int HitsMax
        {
            get
            {
                int strBase;
                double strOffs = GetStatOffset(StatType.Str);

                if (Core.RuleSets.AOSRules())
                {
                    strBase = this.Str;
                    strOffs += AosAttributes.GetValue(this, AosAttribute.BonusHits);
                }
                else
                {
                    strBase = this.RawStr;
                }

                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish >= 13)
                {
                    // Hit Point Calculation
                    //	The following change will be made to the manner in which hit points are calculated for players.
                    //	Hit Points = (str/2) + 50
                    // Note: Any spells or effects that modify strength will also modify the target�s maximum hit points equally. For example, under the new formula, a player with 80 strength will have 90 hit points. However, if they drink a greater strength potion, their strength will be 80+20 (100) and their maximum hit points will be 90+20 (110).
                    return 50 + (strBase / 2) + (int)strOffs;
                }
                else
                {
                    return strBase + (int)strOffs;
                }
            }
        }

        public override int StamMax
        {
            get { return base.StamMax + AosAttributes.GetValue(this, AosAttribute.BonusStam); }
        }

        public override TimeSpan StamRegenRate
        {
            get
            {
                double maxFoodBonus = 3.5; //Seconds maximum quicker to gain stamina

                TimeSpan foodbonus = TimeSpan.FromSeconds(maxFoodBonus * Hunger / 20);

                if (foodbonus > TimeSpan.FromSeconds(maxFoodBonus))
                {
                    foodbonus = TimeSpan.FromSeconds(maxFoodBonus);
                }

                return base.StamRegenRate - foodbonus;
            }
        }

        public override int ManaMax
        {
            get { return base.ManaMax + AosAttributes.GetValue(this, AosAttribute.BonusMana); }
        }

        public override bool Move(Direction d)
        {
            NetState ns = this.NetState;

            if (ns != null)
            {
                //GumpCollection gumps = ns.Gumps;
                List<Gump> gumps = new List<Gump>(ns.Gumps);

                for (int i = 0; i < gumps.Count; ++i)
                {
                    if (gumps[i] is ResurrectGump)
                    {
                        if (Alive)
                        {
                            CloseGumps(typeof(ResurrectGump));
                        }
                        else
                        {
                            SendLocalizedMessage(500111); // You are frozen and cannot move.
                            return false;
                        }
                    }
                }
            }

            TimeSpan speed = ComputeMovementSpeed(d);

            if (!base.Move(d))
                return false;

            m_NextMovementTime += speed;
            return true;
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            DesignContext context = m_DesignContext;

            if (context == null)
                return base.CheckMovement(d, out newZ);

            HouseFoundation foundation = context.Foundation;

            newZ = foundation.Z + HouseFoundation.GetLevelZ(context.Level);

            int newX = this.X, newY = this.Y;
            Movement.Movement.Offset(d, ref newX, ref newY);

            int startX = foundation.X + foundation.Components.Min.X + 1;
            int startY = foundation.Y + foundation.Components.Min.Y + 1;
            int endX = startX + foundation.Components.Width - 1;
            int endY = startY + foundation.Components.Height - 2;

            return (newX >= startX && newY >= startY && newX < endX && newY < endY && Map == foundation.Map);
        }

        public override bool AllowItemUse(Item item)
        {
            #region Dueling
            if (m_DuelContext != null && !m_DuelContext.AllowItemUse(this, item))
                return false;
            #endregion

            return DesignContext.Check(this);
        }

        public override bool AllowSkillUse(SkillName skill)
        {
            #region Dueling
            if (m_DuelContext != null && !m_DuelContext.AllowSkillUse(this, skill))
                return false;
            #endregion

            return DesignContext.Check(this);
        }
        private Memory m_reversePickpocketTarget = new Memory(); // don't bother to Serialize this
        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            bool baseResult = base.CheckNonlocalDrop(from, item, target);
            if (!baseResult)
            {
                //Reverse pickpocket code
                //Only check this if the base fails, this makes the amount of times it is called greatly reduced
                if (SkillHandlers.Stealing.CheckReversePickpocket(from, item, target, m_reversePickpocketTarget))
                    return true;
            }
            return baseResult;
        }

        private bool m_LastProtectedMessage;
        private int m_NextProtectionCheck = 10;

        public virtual void RecheckTownProtection()
        {
            m_NextProtectionCheck = 10;

            Regions.GuardedRegion reg = this.Region as Regions.GuardedRegion;
            bool isProtected = (reg != null && reg.IsGuarded);

            if (isProtected != m_LastProtectedMessage)
            {
                if (isProtected)
                {
                    if (Criminal || (Red && !Core.RedsInTown))
                        SendLocalizedMessage(502275); // Guards can now be called on you!
                    else
                        SendLocalizedMessage(500112); // You are now under the protection of the town guards.
                }
                else
                {
                    if (Criminal || (Red && !Core.RedsInTown))
                        SendMessage("You are now scot-free!");
                    else
                        SendLocalizedMessage(500113); // You have left the protection of the town guards.
                }

                m_LastProtectedMessage = isProtected;
            }
        }

        public override void MoveToWorld(Point3D loc, Map map)
        {
            base.MoveToWorld(loc, map);

            RecheckTownProtection();
        }

        public override void SetLocation(Point3D loc, bool isTeleport)
        {
            base.SetLocation(loc, isTeleport);

            if (isTeleport || --m_NextProtectionCheck == 0)
                RecheckTownProtection();
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);
            /*
			 * Adam: Remove all this unused code.
			 * We don't have insurance, we don't allow house-exit, and we don't have Justice Protectors
			 * 
						if ( from == this )
						{
							if ( m_Quest != null )
								m_Quest.GetContextMenuEntries( list );

							if ( Alive && InsuranceEnabled )
							{
								list.Add( new CallbackEntry( 6201, new ContextCallback( ToggleItemInsurance ) ) );

								if ( AutoRenewInsurance )
									list.Add( new CallbackEntry( 6202, new ContextCallback( CancelRenewInventoryInsurance ) ) );
								else
									list.Add( new CallbackEntry( 6200, new ContextCallback( AutoRenewInventoryInsurance ) ) );
							}

							// TODO: Toggle champ titles

							BaseHouse house = BaseHouse.FindHouseAt( this );
							if( house == null ) //Pix: additional check for house
							{
								Region reg = this.Region;
								if( reg != null && reg is HouseRegion )
								{
									house = ((HouseRegion)reg).House;
								}
							}

							if ( house != null ) //&& house.IsAosRules )
								list.Add( new CallbackEntry( 6207, new ContextCallback( LeaveHouse ) ) );


							if ( m_JusticeProtectors.Count > 0 )
								list.Add( new CallbackEntry( 6157, new ContextCallback( CancelProtection ) ) );
						}
			*/
        }

        private void CancelProtection()
        {
            for (int i = 0; i < m_JusticeProtectors.Count; ++i)
            {
                Mobile prot = (Mobile)m_JusticeProtectors[i];

                string args = string.Format("{0}\t{1}", this.Name, prot.Name);

                prot.SendLocalizedMessage(1049371, args); // The protective relationship between ~1_PLAYER1~ and ~2_PLAYER2~ has been ended.
                this.SendLocalizedMessage(1049371, args); // The protective relationship between ~1_PLAYER1~ and ~2_PLAYER2~ has been ended.
            }

            m_JusticeProtectors.Clear();
        }

        private void ToggleItemInsurance()
        {
            if (!CheckAlive())
                return;

            BeginTarget(-1, false, TargetFlags.None, new TargetCallback(ToggleItemInsurance_Callback));
            SendLocalizedMessage(1060868); // Target the item you wish to toggle insurance status on <ESC> to cancel
        }

        private bool CanInsure(Item item)
        {
            if (item is Container)
                return false;

            if (item is Spellbook || item is Runebook || item is PotionKeg)
                return false;

            if (item.Stackable)
                return false;

            if (Item.IsLootTypeSet(item, LootType.Cursed))
                return false;

            if (item.ItemID == 0x204E) // death shroud
                return false;

            return true;
        }

        private void ToggleItemInsurance_Callback(Mobile from, object obj)
        {
            if (!CheckAlive())
                return;

            Item item = obj as Item;

            if (item == null || !item.IsChildOf(this))
            {
                BeginTarget(-1, false, TargetFlags.None, new TargetCallback(ToggleItemInsurance_Callback));
                SendLocalizedMessage(1060871, "", 0x23); // You can only insure items that you have equipped or that are in your backpack
            }
            // Adam: no more insurance
            /*else if ( item.Insured )
			{
				item.Insured = false;

				SendLocalizedMessage( 1060874, "", 0x35 ); // You cancel the insurance on the item

				BeginTarget( -1, false, TargetFlags.None, new TargetCallback( ToggleItemInsurance_Callback ) );
				SendLocalizedMessage( 1060868, "", 0x23 ); // Target the item you wish to toggle insurance status on <ESC> to cancel
			}*/
            else if (!CanInsure(item))
            {
                BeginTarget(-1, false, TargetFlags.None, new TargetCallback(ToggleItemInsurance_Callback));
                SendLocalizedMessage(1060869, "", 0x23); // You cannot insure that
            }
            else if (Item.IsLootTypeSet(item, LootType.Blessed | LootType.Newbied) || item.BlessedFor == from)
            {
                BeginTarget(-1, false, TargetFlags.None, new TargetCallback(ToggleItemInsurance_Callback));
                SendLocalizedMessage(1060870, "", 0x23); // That item is blessed and does not need to be insured
                SendLocalizedMessage(1060869, "", 0x23); // You cannot insure that
            }
            else
            {
                // Adam: no more insurance
                /*if ( !item.PayedInsurance )
				{
					if ( Banker.Withdraw( from, 600 ) )
					{
						SendLocalizedMessage( 1060398, "600" ); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
						item.PayedInsurance = true;
					}
					else
					{
						SendLocalizedMessage( 1061079, "", 0x23 ); // You lack the funds to purchase the insurance
						return;
					}
				}*/

                // Adam: no more insurance
                //item.Insured = true;

                SendLocalizedMessage(1060873, "", 0x23); // You have insured the item

                BeginTarget(-1, false, TargetFlags.None, new TargetCallback(ToggleItemInsurance_Callback));
                SendLocalizedMessage(1060868, "", 0x23); // Target the item you wish to toggle insurance status on <ESC> to cancel
            }
        }

        private void AutoRenewInventoryInsurance()
        {
            if (!CheckAlive())
                return;

            SendLocalizedMessage(1060881, "", 0x23); // You have selected to automatically reinsure all insured items upon death
            AutoRenewInsurance = true;
        }

        private void CancelRenewInventoryInsurance()
        {
            if (!CheckAlive())
                return;

            SendLocalizedMessage(1061075, "", 0x23); // You have cancelled automatically reinsuring all insured items upon death
            AutoRenewInsurance = false;
        }

        // TODO: Champ titles, toggle

        private void LeaveHouse()
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            if (house == null) //Pix: additional check for house
            {
                Region reg = this.Region;
                if (reg != null && reg is HouseRegion)
                {
                    house = ((HouseRegion)reg).House;
                }
            }

            if (house != null)
                this.Location = house.BanLocation;
        }

        private delegate void ContextCallback();

        private class CallbackEntry : ContextMenuEntry
        {
            private ContextCallback m_Callback;

            public CallbackEntry(int number, ContextCallback callback)
                : this(number, -1, callback)
            {
            }

            public CallbackEntry(int number, int range, ContextCallback callback)
                : base(number, range)
            {
                m_Callback = callback;
            }

            public override void OnClick()
            {
                if (m_Callback != null)
                    m_Callback();
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (this == from && !Warmode)
            {
                IMount mount = Mount;

                if (mount != null && !DesignContext.Check(this))
                    return;
            }

            base.OnDoubleClick(from);
        }

        public override void DisplayPaperdollTo(Mobile to)
        {
            if (DesignContext.Check(this))
                base.DisplayPaperdollTo(to);
        }

        private static bool m_NoRecursion;

        public override bool CheckEquip(Item item)
        {
            if (!base.CheckEquip(item))
                return false;

            #region Dueling
            if (m_DuelContext != null && !m_DuelContext.AllowItemEquip(this, item))
                return false;
            #endregion

            #region Factions
            FactionItem factionItem = FactionItem.Find(item);

            if (factionItem != null)
            {
                Faction faction = Faction.Find(this);

                if (faction == null)
                {
                    SendLocalizedMessage(1010371); // You cannot equip a faction item!
                    return false;
                }
                else if (faction != factionItem.Faction)
                {
                    SendLocalizedMessage(1010372); // You cannot equip an opposing faction's item!
                    return false;
                }
                else
                {
                    int maxWearables = FactionItem.GetMaxWearables(this);

                    for (int i = 0; i < Items.Count; ++i)
                    {
                        Item equiped = (Item)Items[i];

                        if (item != equiped && FactionItem.Find(equiped) != null)
                        {
                            if (--maxWearables == 0)
                            {
                                SendLocalizedMessage(1010373); // You do not have enough rank to equip more faction items!
                                return false;
                            }
                        }
                    }
                }
            }
            #endregion

            #region Alignment
            if (AlignmentSystem.Enabled && !AlignmentSystem.CheckEquip(this, item))
                return false;
            #endregion

            if (this.AccessLevel < AccessLevel.GameMaster && item.Layer != Layer.Mount && this.HasTrade)
            {
                BounceInfo bounce = item.GetBounce();

                if (bounce != null)
                {
                    if (bounce.m_Parent is Item)
                    {
                        Item parent = (Item)bounce.m_Parent;

                        if (parent == this.Backpack || parent.IsChildOf(this.Backpack))
                            return true;
                    }
                    else if (bounce.m_Parent == this)
                    {
                        return true;
                    }
                }

                SendLocalizedMessage(1004042); // You can only equip what you are already carrying while you have a trade pending.
                return false;
            }

            return true;
        }

        protected override void OnLocationChange(Point3D oldLocation)
        {
            CheckLightLevels(false);

            #region Dueling
            if (m_DuelContext != null)
                m_DuelContext.OnLocationChanged(this);
            #endregion

            LastRegionIn = this.Region;

            WinterEventSystem.HandleLocationChance(this, oldLocation);

            DesignContext context = m_DesignContext;

            if (context == null || m_NoRecursion)
                return;

            m_NoRecursion = true;

            HouseFoundation foundation = context.Foundation;

            int newX = this.X, newY = this.Y;
            int newZ = foundation.Z + HouseFoundation.GetLevelZ(context.Level);

            int startX = foundation.X + foundation.Components.Min.X + 1;
            int startY = foundation.Y + foundation.Components.Min.Y + 1;
            int endX = startX + foundation.Components.Width - 1;
            int endY = startY + foundation.Components.Height - 2;

            if (newX >= startX && newY >= startY && newX < endX && newY < endY && Map == foundation.Map)
            {
                if (Z != newZ)
                    Location = new Point3D(X, Y, newZ);

                m_NoRecursion = false;
                return;
            }

            Location = new Point3D(foundation.X, foundation.Y, newZ);
            Map = foundation.Map;

            m_NoRecursion = false;
        }

        public override bool OnMoveOver(Mobile m)
        {
            #region Dueling
            if (Region.IsPartOf(typeof(Engines.ConPVP.SafeZone)) && m is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)m;

                if (pm.DuelContext == null || pm.DuelPlayer == null || !pm.DuelContext.Started || pm.DuelContext.Finished || pm.DuelPlayer.Eliminated)
                    return true;
            }
            #endregion

            return base.OnMoveOver(m);
        }

        protected override void OnMapChange(Map oldMap)
        {
            if ((Map != Faction.Facet && oldMap == Faction.Facet) || (Map == Faction.Facet && oldMap != Faction.Facet))
                InvalidateProperties();

            #region Dueling
            if (m_DuelContext != null)
                m_DuelContext.OnMapChanged(this);
            #endregion

            WinterEventSystem.HandleMapChange(this, oldMap);

            DesignContext context = m_DesignContext;

            if (context == null || m_NoRecursion)
                return;

            m_NoRecursion = true;

            HouseFoundation foundation = context.Foundation;

            if (Map != foundation.Map)
                Map = foundation.Map;

            m_NoRecursion = false;
        }

        public override void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {
            try
            {
                if (amount > (Core.RuleSets.AOSRules() ? 25 : 0))
                {
                    BandageContext c = BandageContext.GetContext(this);

                    if (c != null)
                        c.Slip();
                }

                if (this.BlockDamage == false)
                    WeightOverloading.FatigueOnDamage(this, amount);

                base.OnDamage(amount, from, willKill, source_weapon);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        // records the damage dealt to whomever you are fighting
        //	this is implemented for debugging low-damage complaints.
        //	See implemention in Mobile
        public override void OnGaveDamage(int amount, Mobile to, bool willKill, object source_weapon)
        {   // we delivered this much damage on our last strke or spell
            DamageTracker(amount, to, source_weapon);
        }

        #region DAMAGE TRACKER
        DateTime DamageTracker_start = DateTime.UtcNow;
        double DamageTracker_damage = 0;
        bool clock_started = false;
        public void DamageTracker(int amount, Mobile to, object source_weapon)
        {
            int DamageAbsorbed_amount = 0;

            if (Core.DAMAGE == 0)           // off
                return;                     // if not turned on, ignore

            if (to == null)                 // anything's possible
                return;

            if (Core.DAMAGE == 2)           // reset
            {
                Core.DAMAGE = 1;            // resume on state
                clock_started = false;
                DamageTracker_damage = 0;
            }
            if (clock_started == false)     // start the clock on first damage
            {
                clock_started = true;
                DamageTracker_start = DateTime.UtcNow;
            }

            // Aquire Virtual Armor Absorbtion
            // lets get the damage absorbed from my strike. (Base Weapon records this)
            BaseWeapon bw = source_weapon as BaseWeapon;    // it's a baseweapon
            if (bw != null && bw.Parent == this)            // and it's me
                DamageAbsorbed_amount = bw.DamageAbsorbed;  // record the absorbed damage

            DamageTracker_damage += amount;     // total actual damage done thus far
            amount += DamageAbsorbed_amount;    // how much damage we delivered before absorbtion
            TimeSpan ts = DateTime.UtcNow - DamageTracker_start;
            string damage_over_time = string.Format("This damage: {0} -{1} damage absorbed for {2}/{3} total damage over time",
                amount,
                DamageAbsorbed_amount,
                DamageTracker_damage,
                (int)ts.TotalSeconds);
            SendMessage(damage_over_time);
            string filename = string.Format("DamageTracker-{0}.log", this.Name);
            LogHelper logger = new LogHelper(filename, false, true);
            logger.Log(LogType.Text, damage_over_time);
            logger.Finish();
        }

        #endregion

        public static int ComputeSkillTotal(Mobile m)
        {
            int total = 0;

            for (int i = 0; i < m.Skills.Length; ++i)
                total += m.Skills[i].BaseFixedPoint;

            return (total / 10);
        }

        public override void Resurrect()
        {
            bool wasAlive = this.Alive;

            if (Mortal && AccessLevel == AccessLevel.Player)
            {
                SendMessage("Thy soul was too closely intertwined with thy flesh - thou'rt unable to incorporate a new body.");
                return;
            }

            base.Resurrect();

            // Savage kin paint re-application

            if (this.SavagePaintExpiration != TimeSpan.Zero)
            {
                // Ai uses HUE value and not the BodyMod as there is no sitting graphic
                if (!Core.RuleSets.AnyAIShardRules())
                    this.BodyMod = (this.Female ? 184 : 183);
                else
                    this.HueMod = 0;
            }

            if (this.Alive && !wasAlive)
            {
                // restore sight to blinded ghosts
                Blind = false;                          // we can see again
                m_SightExpire = DateTime.MaxValue;      // kill timer

                bool bNewDeathrobe = true;
                if (this.Backpack != null)
                {
                    Item oldDeathrobe = this.Backpack.FindItemByType(typeof(DeathRobe), false);
                    if (oldDeathrobe != null)
                    {
                        bNewDeathrobe = false;
                        EquipItem(oldDeathrobe);
                    }
                }
                if (bNewDeathrobe)
                {
                    Item deathRobe = new DeathRobe();

                    if (!EquipItem(deathRobe))
                        deathRobe.Delete();
                }

                if (PrisonInmate)
                {
                    //When resrrecting, make sure our counts are reduced

                    TimeSpan deadtime = TimeSpan.FromMinutes(0.0);
                    if (m_InmateLastDeathTime == DateTime.MinValue)
                    {
                        //effectively 0 deadtime if it's set to minvalue
                    }
                    else
                    {
                        deadtime = DateTime.UtcNow - m_InmateLastDeathTime;
                    }

                    //reduce short term by 4 hours minus half the time spent dead (modulo 8 hours)
                    m_ShortTermElapse -= (TimeSpan.FromHours(4.0) - TimeSpan.FromSeconds((deadtime.TotalSeconds % 28800) / 2));
                    //reduce long term by 20 hours minus half the time spent dead (modulo 40 hours)
                    m_LongTermElapse -= (TimeSpan.FromHours(20.0) - TimeSpan.FromSeconds((deadtime.TotalSeconds % 144000) / 2));
                }

                //InvalidateMyRunUO();
            }
        }

        /// <summary>
        /// If an evil kills an innocent, the items stay on the innocent.
        /// </summary>
        public override bool KeepsItemsOnDeath
        {
            get
            {
                bool blessed = false;
                /* Disabled due to severe exploit potential
				 * PS. this code needs testing if it is ever enabled.
				if (!this.Evil && !this.Hero)
				{	// I do not belong to this hero/evil system
					for (int i = 0; i < this.Aggressors.Count; i++)
					{	// look for Aggressors that are evil and attacking me was criminal (i.e., I didn't attack them)
						Mobile attacker = this.Aggressors[i].Attacker;
						if (attacker is PlayerMobile)
						{	// if my killer was Evil and it was a criminal action
							if (attacker.Evil && this.Aggressors[i].CriminalAggression)
							{
								blessed = true;
								break;
							}
						}
					}
				}*/

                return base.KeepsItemsOnDeath || blessed;
            }
        }

        public override void OnReportedForMurder(Mobile from)
        {
            #region Ethics
            if (Hero)
            {
                // If a hero is reported for murder, all lifeforce and sphere immediately vanishes, and you become a fallen hero. 
                EthicPlayer.Power = EthicPlayer.History = 0;
                // set the FallenHero state which make you gray to evil for 5 minutes (what does it mean for a hero to be gray to an evil?)
                this.ExpirationFlags.Add(new Mobile.ExpirationFlag(this, Mobile.ExpirationFlagID.FallenHero, TimeSpan.FromMinutes(5)));
            }
            #endregion Ethics

            #region Factions
            #region INFO
            /* Publish 8: https://uo.com/wiki/ultima-online-wiki/technical/previous-publishes/2000-2/2000-publish-08-5th-december/
             * Faction System Murder Reprieve
             * The faction murder reprieve will be a one-time reduction in the various types of murder counts that a murderer may have accumulated. This reprieve will be given only when a murderer who has not previously received a faction-related reprieve first joins a faction. The reprieve will carry the consequence of regaining all of your prior murder counts should the character become labeled as a murderer once again (i.e.: they get 5 or more short-term counts). This reprieve will only be offered for one month after it is published, after which the reprieve code will be disabled.
             * To get the reprieve, the murder must join the faction (as described in the faction system document) through the faction stone. Note this means murderers will not be able to join the Council of Mages or True Britannians factions unless they are part of a guild whose guild leader is able to get to the faction stone and join the faction.
             * Murderers who have more than five short-term or long-term murder counts who receive a reprieve will be set to four short-term and long-term murder counts and automatically become �blue�. Murderers whose �ping-pong� counter is 4 or greater will have that counter set to four. The system will retain the previous short-term, long-term, and ping-pong for these characters and should a reprieved murderer become a murderer again, all of these counts will be set to their original numbers.
             */
            #endregion INFO

            if (this.GetPlayerBool(PlayerBoolTable.MurderReprieve) && !this.GetPlayerBool(PlayerBoolTable.MurderReprieveRescinded))
            {
                LogHelper logger = new LogHelper("Murder Reprieve.log", false, true);
                logger.Log(string.Format("{0} : Your reprieve has been lost for returning to your murderous ways", this));
                logger.Log(string.Format("{0} : old STM:{1}, new STM:{2}", this, this.ShortTermMurders, this.m_ReprieveShortTermMurders));
                logger.Log(string.Format("{0} : old LTM:{1}, new LTM:{2}", this, this.LongTermMurders, this.m_ReprieveLongTermMurders));
                logger.Finish();
                // and should a reprieved murderer become a murderer again, all of these counts will be set to their original numbers.
                this.SetPlayerBool(PlayerBoolTable.MurderReprieveRescinded, true);
                this.ShortTermMurders = this.m_ReprieveShortTermMurders;
                this.LongTermMurders = this.m_ReprieveLongTermMurders;
                this.SendLocalizedMessage(1042856); // Your reprieve has been lost for returning to your murderous ways.
            }
            #endregion
        }

        // wea: Added to perform SpiritCohesion update on resurrect
        public override void OnAfterResurrect()
        {
            // Set last res time so we know how long they've had alive
            LastResurrectTime = DateTime.UtcNow;

#if false
            // 8/16/2023, Adam: never true - what are we trying to do here?
            if (LastDeathTime == null)
            {
                SpiritCohesion = 0;
                return;
            }
#endif
            TimeSpan TimeSinceDeath = (DateTime.UtcNow - LastDeathTime);

            if (TimeSinceDeath < TimeSpan.FromSeconds(CoreAI.CohesionLowerDelay))
            {
                SpiritCohesion++;
            }
            else
            {
                SpiritCohesion = 0;
            }

            return;
        }

#if THIS_IS_NOT_USED
		private Mobile m_InsuranceAward;
		private int m_InsuranceCost;
		private int m_InsuranceBonus;
#endif

        private DateTime m_InmateLastDeathTime;

        public bool IsInCombat(bool player = false)
        {
            List<AggressorInfo> list = new();
            if (Aggressors != null)
                list.AddRange(Aggressors);
            if (Aggressed != null)
                list.AddRange(Aggressed);

            foreach (AggressorInfo info in list)
                if (info.Attacker != null && info.Defender != null && !info.Expired)
                    if (!player || (info.Attacker.Player && info.Defender.Player))
                        return true;

            return false;
        }

        public override bool OnBeforeDeath()
        {

            if (Core.RuleSets.MortalisRules())
                EventSink.InvokeBeforePlayerDeath(new BeforePlayerDeathEventArgs(this, this.GUID, this.Account as Account, DateTime.UtcNow, GetFirstHouse(), FollowerCount));
            else
                EventSink.InvokeBeforePlayerDeath(new BeforePlayerDeathEventArgs(this, this.GUID, this.Account as Account, DateTime.UtcNow, Serial.Zero, FollowerCount));

            #region Notify the killers of their deed
            List<Mobile> aggressors_list = new List<Mobile>();
            foreach (AggressorInfo info in Aggressors)
            {
                if (info.Attacker != null)
                    aggressors_list.Add(info.Attacker);
            }
            foreach (Mobile mx in aggressors_list)
                if (mx is BaseCreature)
                    (mx as BaseCreature).OnKilledBy(this, aggressors_list);
            #endregion
#if THIS_IS_NOT_USED
			m_InsuranceCost = 0;
			m_InsuranceAward = base.FindMostRecentDamager( false );

			if ( m_InsuranceAward != null && !m_InsuranceAward.Player )
				m_InsuranceAward = null;

			if ( m_InsuranceAward is PlayerMobile )
				((PlayerMobile)m_InsuranceAward).m_InsuranceBonus = 0;
#endif
            if (PrisonInmate)
            {
                m_InmateLastDeathTime = DateTime.UtcNow;

                // If they die as an Inmate, reset their kill timers to 8/40
                m_ShortTermElapse += TimeSpan.FromHours(4);
                m_LongTermElapse += TimeSpan.FromHours(20);

                //InvalidateMyRunUO();
            }

            //make sure that the player isn't holding anything...
            try
            {
                Item held = Holding;
                if (held != null)
                {
                    held.ClearBounce();
                    if (Backpack != null)
                    {
                        Backpack.DropItem(held);
                    }
                }
                Holding = null;
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            return base.OnBeforeDeath();
        }
        private Serial GetFirstHouse()
        {
            if (this.Account != null && this.Account is Account acct && acct != null)
            {   // save the guy's house if he has one
                ArrayList list = BaseHouse.GetHouses(this);
                if (list != null && list.Count > 0)
                    return (list[0] as BaseHouse).Serial;
            }
            return Serial.Zero;
        }

        private bool IsSameRealIOB(Mobile target)
        {
            bool bReturn = false;

            if (target != null)
            {
                IOBAlignment ta = IOBAlignment.None;
                if (target is BaseCreature)
                {
                    BaseCreature bc = target as BaseCreature;
                    ta = bc.IOBAlignment;
                }
                else if (target is PlayerMobile)
                {
                    PlayerMobile pma = target as PlayerMobile;
                    ta = pma.IOBAlignment;
                }

                IOBAlignment myRealAlignment = this.IOBAlignment;
                if (this.IOBAlignment == IOBAlignment.OutCast || this.IOBAlignment == IOBAlignment.Healer)
                {
                    Guilds.Guild g = this.Guild as Guilds.Guild;
                    if (g != null)
                    {
                        myRealAlignment = g.IOBAlignment;
                    }
                }

                if (ta == myRealAlignment)
                {
                    bReturn = true;
                }
            }

            return bReturn;
        }

        public override void OnBeneficialAction(Mobile target, bool isCriminal)
        {
            try
            {
                if (this.IOBAlignment != IOBAlignment.None && this != target)
                {
                    bool bFound = false; //saves processing of Aggressors if we find one in Aggressed

                    //Check those the target has aggressed
                    for (int i = 0; i < target.Aggressed.Count; i++)
                    {
                        Mobile a = ((AggressorInfo)target.Aggressed[i]).Defender;
                        if (a is PlayerMobile)
                        {
                            //ignore actions between players
                        }
                        else if (a is BaseCreature && (((BaseCreature)a).Summoned || ((BaseCreature)a).Tamable))
                        {
                            //ignore summons and tames
                        }
                        else if (a is BaseHire)
                        {
                            //ignore hires
                        }
                        else if (IsSameRealIOB(a))
                        {
                            this.OnKinAggression();
                            bFound = true;
                            break;
                        }
                    }

                    if (!bFound)
                    {
                        //Check those that have aggressed the target
                        for (int i = 0; i < target.Aggressors.Count; i++)
                        {
                            Mobile a = ((AggressorInfo)target.Aggressors[i]).Attacker;
                            if (a is PlayerMobile)
                            {
                                //ignore actions between players
                            }
                            else if (a is BaseCreature && (((BaseCreature)a).Summoned || ((BaseCreature)a).Tamable))
                            {
                                //ignore summons and tames
                            }
                            else if (a is BaseHire)
                            {
                                //ignore hires
                            }
                            else if (IsSameRealIOB(a))
                            {
                                this.OnKinAggression();
                                bFound = true;
                                break;
                            }
                        }
                    }
                }
                //Pix 7/5/06 - removed due to problems
                //else if( this.IOBAlignment == IOBAlignment.None )
                //{
                //	if( target is PlayerMobile )
                //	{
                //		PlayerMobile pmt = target as PlayerMobile;
                //		if( pmt.IOBAlignment != IOBAlignment.None )
                //		{
                //			this.OnKinAggression();
                //		}
                //	}
                //}
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Problem with PM.OnBeneficialAction - Tell PIXIE:");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            //Deal with faction-healers
            try
            {
                if (this != target)
                {
                    if (this.IsRealFactioner == false) //if we're NOT a real factioner
                    {
                        if (target is PlayerMobile)
                        {
                            PlayerMobile pmTarget = target as PlayerMobile;
                            if (pmTarget.IOBAlignment != IOBAlignment.None)
                            {
                                this.OnKinBeneficial();
                            }
                        }
                    }
                }
            }
            catch (Exception fhe)
            {
                LogHelper.LogException(fhe);
            }

            //Deal with fightbroker interferers.
            try
            {
                if (this != target)
                {
                    if (!FightBroker.IsAlreadyRegistered(this)
                        &&
                        (FightBroker.IsAlreadyRegistered(target) || FightBroker.IsHealerInterferer(target)))
                    {
                        FightBroker.AddHealerInterferer(this);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }

            if (AlignmentSystem.Enabled)
                AlignmentSystem.HandleBeneficialAction(this, target);

            base.OnBeneficialAction(target, isCriminal);
        }

        /// <summary>
        /// Overridable. Event invoked when the Mobile <see cref="DoHarmful">does a harmful action</see>.
        /// </summary>
        public override void OnHarmfulAction(Mobile target, bool isCriminal, object source = null)
        {
            base.OnHarmfulAction(target, isCriminal, source);

            // Attacking evils makes you gray to all evils for two minutes. In addition, you are lootable if killed. 
            //	You do not get this flag for attacking an evil player that just attacked an innocent. 

            // are they non criminal evil and I'm not in the system?
            if (target.Criminal == false && target.Evil && !this.Hero && !this.Evil)
            {   // yes, I am now an EvilNoto for attacking them
                this.ExpirationFlags.Add(new ExpirationFlag(this, ExpirationFlagID.EvilNoto, TimeSpan.FromMinutes(2)));
            }

            // Monster ignore	Velgo K'balc
            // This ability makes even aggressive monsters ignore the evil player for a time, unless they were already engaged in combat with them. 
            // Attacking or stealing from a monster will shatter the spell.
            if (this.Evil && this.CheckState(ExpirationFlagID.MonsterIgnore))
                this.RemoveState(ExpirationFlagID.MonsterIgnore);
        }

        private bool CheckInsuranceOnDeath(Item item)
        {
            // Adam: no more insurance
            /*
			if ( InsuranceEnabled && item.Insured )
			{
                #region Dueling
				if ( m_DuelPlayer != null && m_DuelContext != null && m_DuelContext.Registered && m_DuelContext.Started && !m_DuelPlayer.Eliminated )
					return true;
                #endregion

				if ( AutoRenewInsurance )
				{
					int cost = ( m_InsuranceAward == null ? 600 : 300 );

					if ( Banker.Withdraw( this, cost ) )
					{
						m_InsuranceCost += cost;
						item.PayedInsurance = true;
					}
					else
					{
						SendLocalizedMessage( 1061079, "", 0x23 ); // You lack the funds to purchase the insurance
						item.PayedInsurance = false;
						item.Insured = false;
					}
				}
				else
				{
					item.PayedInsurance = false;
					item.Insured = false;
				}

				if ( m_InsuranceAward != null )
				{
					if ( Banker.Deposit( m_InsuranceAward, 300 ) )
					{
						if ( m_InsuranceAward is PlayerMobile )
							((PlayerMobile)m_InsuranceAward).m_InsuranceBonus += 300;
					}
				}

				return true;
			}

			*/
            return false;
        }

        public override DeathMoveResult GetParentMoveResultFor(Item item)
        {
            /*
			if (this.IsInChallenge)
				return DeathMoveResult.RemainEquiped;
			*/
            if (CheckInsuranceOnDeath(item))
                return DeathMoveResult.MoveToBackpack;

            if (AllowRedsToKeepNewbieItems && item.GetFlag(LootType.Newbied))
                return DeathMoveResult.MoveToBackpack;

            return base.GetParentMoveResultFor(item);
        }

        public override DeathMoveResult GetInventoryMoveResultFor(Item item)
        {
            /*
			if (this.IsInChallenge)
				return DeathMoveResult.RemainEquiped;
			*/
            if (CheckInsuranceOnDeath(item))
                return DeathMoveResult.MoveToBackpack;

            if (AllowRedsToKeepNewbieItems && item.GetFlag(LootType.Newbied))
                return DeathMoveResult.MoveToBackpack;

            // reds, regardless if they can keep newbied items, can always keep UnLootable
            if (item.LootType != LootType.Newbied && item.GetFlag(LootType.UnLootable))
                return DeathMoveResult.MoveToBackpack;

            return base.GetInventoryMoveResultFor(item);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            //Deal with any death-effects for factioners
            //Engines.IOBSystem.KinSystem.OnDeath(this);

            // ghosts now go blind after their body decays
            m_SightExpire = DateTime.UtcNow + CorpseDecayTime();
            m_SightExpire += BoneDecayTime();
            Timer.DelayCall(m_SightExpire - DateTime.UtcNow, new TimerCallback(GoBlind));

            HueMod = -1;
            NameMod = null;
            //SavagePaintExpiration = TimeSpan.Zero;

            SetHairMods(-1, -1);

            PolymorphSpell.StopTimer(this);
            IncognitoSpell.StopTimer(this);
            DisguiseGump.StopTimer(this);

            EndAction(typeof(PolymorphSpell));
            EndAction(typeof(IncognitoSpell));

            MeerMage.StopEffect(this, false);

            if (m_PermaFlags.Count > 0)
            {
                m_PermaFlags.Clear();

                if (c is Corpse)
                    ((Corpse)c).Criminal = true;

                if (SkillHandlers.Stealing.ClassicMode)
                    Criminal = true;
            }

            #region Justice
#if THIS_IS_NOT_USED
			if ( this.Kills >= 5 && false /*DateTime.UtcNow >= m_NextJustAward*/ )
			{
				Mobile m = FindMostRecentDamager( false );

				if ( m != null && m.Player )
				{
					// bool gainedPath = false;

					int theirTotal = ComputeSkillTotal( m );
					int ourTotal = ComputeSkillTotal( this );

					int pointsToGain = 1 + ((theirTotal - ourTotal) / 50);

					if ( pointsToGain < 1 )
						pointsToGain = 1;
					else if ( pointsToGain > 4 )
						pointsToGain = 4;

					/*					if ( VirtueHelper.Award( m, VirtueName.Justice, pointsToGain, ref gainedPath ) )
					 *					{
					 *						if ( gainedPath )
					 *							m.SendLocalizedMessage( 1049367 ); // You have gained a path in Justice!
					 *						else
					 *							m.SendLocalizedMessage( 1049363 ); // You have gained in Justice.
					 *
					 *						m.FixedParticles( 0x375A, 9, 20, 5027, EffectLayer.Waist );
					 *						m.PlaySound( 0x1F7 );
					 *
					 *						m_NextJustAward = DateTime.UtcNow + TimeSpan.FromMinutes( pointsToGain * 2 );
					 *					}
					 */
					this.Aggressors.Clear();
				}
			}
#endif
            #endregion

            #region Insurance
#if THIS_IS_NOT_USED
			if ( m_InsuranceCost > 0 )
				SendLocalizedMessage( 1060398, m_InsuranceCost.ToString() ); // ~1_AMOUNT~ gold has been withdrawn from your bank box.

			if ( m_InsuranceAward is PlayerMobile )
			{
				PlayerMobile pm = (PlayerMobile)m_InsuranceAward;

				if ( pm.m_InsuranceBonus > 0 )
					pm.SendLocalizedMessage( 1060397, pm.m_InsuranceBonus.ToString() ); // ~1_AMOUNT~ gold has been deposited into your bank box.
			}
#endif
            #endregion

            #region Mortal
            if (Mortal)
            {
                #region Logging
                LogHelper logger = new LogHelper("Mortal Death.log", false, false);
                LogPrisonerInfo(logger);
                //List<string> list = Utility.GetObjectProperties(this);
                //foreach (string s in list)
                //logger.Log(LogType.Text, string.Format(s));
                logger.Finish();
                #endregion Logging
                Effects.SendBoltEffect(this, false, 100);
                PlaySound(586);     // spirit speak
                PlaySound(0x231);   // gas sound - spirit leaving the body
                for (int i = 0; i < 3; i++)
                {
                    Point3D p = new Point3D(Location);
                    p.X += Utility.RandomMinMax(-1, 1);
                    p.Y += Utility.RandomMinMax(-1, 1);
                    new Blood(Utility.Random(0x122A, 5), 120.0).MoveToWorld(p, Map);
                }

                this.Frozen = true;
                Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(MortalDeathMessage));
                if (AccessLevel == AccessLevel.Player)
                    Timer.DelayCall(TimeSpan.FromSeconds(10.0), new TimerCallback(Delete));

                if (Core.RuleSets.MortalisRules())
                {   // the killed player's bankbox is saved and given to the new character created on the account.
                    if (BankBox != null && BankBox.Items != null && BankBox.Items.Count > 0 && Account != null && Account is Account acct)
                    {
                        // can't use a bankbox here since it requires an owner, and it will be deleted on world load if the owner is null
                        StrongBackpack newBox = new StrongBackpack();   // create a new bankbox to store the items
                        acct.BankBox = newBox.Serial;                   // save the serial
                        RecursiveMove(newBox, this.BankBox);            // recursively move all container contents to storage container
                        newBox.MoveToIntStorage();                  // move to a safe place
                    }
                }
            }
            #endregion

            Mobile killer = this.FindMostRecentDamager(true);

            if (Core.OldEthics)
                Ethics.Ethic.HandleDeath(this, killer);

            if (m_DuelContext == null || !m_DuelContext.Registered || !m_DuelContext.Started || m_DuelPlayer == null || m_DuelPlayer.Eliminated)
                Faction.HandleDeath(this, killer);

            if (AlignmentSystem.Enabled)
            {
                if (m_DuelContext == null || !m_DuelContext.Registered || !m_DuelContext.Started || m_DuelPlayer == null || m_DuelPlayer.Eliminated)
                    AlignmentSystem.HandleDeath(this, killer);
            }

            // todo
            // Server.Guilds.Guild.HandleDeath(this, killer);

            #region Dueling
            if (m_DuelContext != null)
                m_DuelContext.OnDeath(this, c);
            #endregion

            this.LastDeathTime = DateTime.UtcNow;
            this.ClearDamageEntries();
        }
        public void LogPrisonerInfo(LogHelper logger)
        {
            logger.Log(LogType.Mobile, this, string.Format($"Server Time: {AdjustedDateTime.ServerTime}"));
            if (this.FindMostRecentDamager(true) != null)
            {
                if (this.Dead)
                    logger.Log(LogType.Mobile, this, string.Format("Killer: {0}", this.FindMostRecentDamager(true)));
                else
                    logger.Log(LogType.Mobile, this, string.Format("FindMostRecentDamager: {0}", this.FindMostRecentDamager(true)));
            }
            logger.Log(LogType.Text, string.Format("Inmate: {0}, Mortal: {1}", this.PrisonInmate, this.Mortal));
            logger.Log(LogType.Text, string.Format("Infraction: {0}, Sentence: {1}",
                this.InfractionStatus, (this.MinimumSentence != DateTime.MinValue ? this.MinimumSentence : "none")));
            logger.Log(LogType.Text, string.Format("Long term murder counts: {0}, Short term murder counts: {1}", this.LongTermMurders, this.ShortTermMurders));
            logger.Log(LogType.Text, string.Format("Long term criminal counts: {0}, Short term criminal counts: {1}", this.LongTermCriminalCounts, this.ShortTermCriminalCounts));
        }
        #region Mortalis OnDeath
        private void RecursiveMove(Container to, Container from)
        {
            List<Item> list = new List<Item>();             // make a copy of the items
            foreach (Item item in from.Items)
                list.Add(item);

            foreach (Item item in list)                     // move the items from the old bankbox to the new
            {
                if (from.Items.Contains(item))
                {   // we'll keep user defined packs in the player's bank, but 'prison issue' backpacks, get unpacked.
                    if (item is Backpack bp && bp.IsDynamicFeatureSet(Server.Items.Backpack.FeatureBits.PrisonIssue))
                        RecursiveMove(to, item as Container);
                    else if (FilterTransferItems(item))
                    {
                        from.RemoveItem(item);
                        to.AddItem(item);
                    }
                }
            }
        }
        private bool FilterTransferItems(Item item)
        {
            /*
             * Add your item filtering and taxation here
             */
            //if (Server.Engines.ResourcePool.ResourcePool.IsPooledResource(item.GetType(), true))
            //return false;
            //if (item.LootType != LootType.Regular)
            //item.LootType = LootType.Regular;
            //if (item.GetType() == typeof(TentBag))
            //return false;
            //if (item.GetType() == typeof(Bandage))
            //return false;
            return true;
        }
        #endregion Mortalis OnDeath
        //adam: New system to gauge player mobile travel speed as they move over MarkTime objects
        //	please the Server.Items.MarkTime object
        private DateTime m_LastTimeMark = DateTime.MinValue;
        public DateTime LastTimeMark { get { return m_LastTimeMark; } set { m_LastTimeMark = value; } }

        //Pix: note that this doesn't need to be serialized
        private DateTime m_LastDeathTime = DateTime.MinValue;
        [CommandProperty(AccessLevel.Counselor)]
        public DateTime LastDeathTime
        {
            get
            {
                return m_LastDeathTime;
            }
            set
            {
                m_LastDeathTime = value;
            }
        }

        //pla: Override the bone decay value
        public override TimeSpan BoneDecayTime()
        {
            //If this is a commander on his boat then extend the bone decay delay
            BaseBoat boat;
            boat = BaseBoat.FindBoatAt(this);
            if (boat != null && (boat.HasKey(this) || boat.CorpseHasKey(this)))
                return TimeSpan.FromMinutes(20.0);
            else
                return base.BoneDecayTime();
        }

        public void MortalDeathMessage()
        {
            this.SendMessage(0x22, "Thou art dead. Fear thy fate not; pale Death with impartial tread beats at the poor man's cottage door and at the palaces of kings.");
        }

        // Store where items were OnDeath and keep them there, rather than makingthe pack a mess.
        public override bool RetainPackLocsOnDeath { get { return true; } }

        private ArrayList m_PermaFlags;
        private ArrayList m_VisList;
        private TimeSpan m_GameTime;
        private TimeSpan m_ShortTermElapse;
        private TimeSpan m_LongTermElapse;
        private DateTime m_SessionStart;
        private DateTime m_LastEscortTime;
        private DateTime m_SavagePaintExpiration;
        private SkillName m_Learning = (SkillName)(-1);

        private DateTime m_LastShortDecayed;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastShortDecayed
        {
            get { return m_LastShortDecayed; }
        }

        public void ReduceKillTimersByHours(double hours)
        {
            m_ShortTermElapse -= TimeSpan.FromHours(hours);
            m_LongTermElapse -= TimeSpan.FromHours(hours);

            DecayKills();
        }

        public static int DoGlobalDecayKills()
        {
            int count = 0;
            try
            {
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerMobile)
                    {
                        ((PlayerMobile)m).DecayKills();
                        count++;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Error in DoGlobalDecayKills");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            return count;
        }

        /*
         * 6/23/2021, adamL moved to cron scheduler.
         * public static int DoGlobalCleanKillerTimes()
        {
            int count = 0;

            //clean up KillerTimes
            try
            {
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerMobile)
                    {
                        count++;
                        PlayerMobile pm = (PlayerMobile)m;
                        if (pm.KillerTimes != null)
                        {
                            for (int i = pm.KillerTimes.Count - 1; i >= 0; i--)
                            {
                                if (DateTime.UtcNow - ((ReportMurdererGump.KillerTime)pm.KillerTimes[i]).Time > TimeSpan.FromMinutes(5.0))
                                {
                                    pm.KillerTimes.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Exception Caught in DoGlobalCleanKillerTimes removal code: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }

            return count;
        }*/

        public void DecayKills()
        {
            if (m_ShortTermElapse < this.GameTime || ((CoreAI.OfflineShortsDecay != 0) && ((DateTime.UtcNow - m_LastShortDecayed) > TimeSpan.FromHours(CoreAI.OfflineShortsDecayHours))))
            {
                m_LastShortDecayed = DateTime.UtcNow;

                if ((Core.RuleSets.AngelIslandRules()) && PrisonInmate && Alive)
                {
                    m_ShortTermElapse = this.GameTime + TimeSpan.FromHours(4);
                }
                else
                {
                    m_ShortTermElapse = this.GameTime + TimeSpan.FromHours(8);
                }

                if (ShortTermMurders > 0)
                    --ShortTermMurders;

                if (ShortTermCriminalCounts > 0)
                    --ShortTermCriminalCounts;
            }

            if (m_LongTermElapse < this.GameTime)
            {
                if ((Core.RuleSets.AngelIslandRules()) && PrisonInmate && Alive)
                {
                    m_LongTermElapse = this.GameTime + TimeSpan.FromHours(20);
                }
                else
                {
                    m_LongTermElapse = this.GameTime + TimeSpan.FromHours(40);
                }

                if (LongTermMurders > 0)
                    --LongTermMurders;
            }
        }

        public SkillName Learning
        {
            get { return m_Learning; }
            set { m_Learning = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan SavagePaintExpiration
        {
            get
            {
                TimeSpan ts = m_SavagePaintExpiration - DateTime.UtcNow;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set
            {
                m_SavagePaintExpiration = DateTime.UtcNow + value;
            }
        }

        public DateTime LastEscortTime
        {
            get { return m_LastEscortTime; }
            set { m_LastEscortTime = value; }
        }

        public PlayerMobile()
        {
            this.STRBonusCap = STRBonusCapDefault;      // have player mobiles start out with a capped STR bonus when using weapons
            m_VisList = new ArrayList();
            m_PermaFlags = new ArrayList();
            m_BOBFilter = new Engines.BulkOrders.BOBFilter();
            m_GameTime = TimeSpan.Zero;

            if (PrisonInmate && Alive)
            {
                m_ShortTermElapse = TimeSpan.FromHours(4.0);
                m_LongTermElapse = TimeSpan.FromHours(20.0);
            }
            else
            {
                m_ShortTermElapse = TimeSpan.FromHours(8.0);
                m_LongTermElapse = TimeSpan.FromHours(40.0);
            }

            m_JusticeProtectors = new ArrayList();

            m_LastSkillUsed = new SkillName();      // wea: for [FindSkill
            m_LastSkillTime = new DateTime();       //

            m_SpiritCohesion = 0;                       // wea: for Spirit Cohesion
            m_LastResurrectTime = new DateTime();

            m_SpeechRecord = new Queue();               // TK: [report command
            m_Reported = DateTime.MinValue;

            //InvalidateMyRunUO();
        }

        public override bool MutateSpeech(ArrayList hears, ref string text, ref object context)
        {
            if (Alive)
                return false;

            if (Core.RuleSets.AOSRules())
            {
                for (int i = 0; i < hears.Count; ++i)
                {
                    object o = hears[i];

                    if (o != this && o is Mobile && ((Mobile)o).Skills[SkillName.SpiritSpeak].Value >= 100.0)
                        return false;
                }
            }

            return base.MutateSpeech(hears, ref text, ref context);
        }

        public void RecordSpeech(Mobile speaker, string text, string note)
        {
            if (!(speaker is PlayerMobile))
                return;

            string msg = "[" + DateTime.UtcNow.ToString("HH:mm:ss") + "] " + speaker.Name + " " + "(acct " + ((Account)speaker.Account).Username + ") " + (note != null ? "(" + note + ") " : "") + ": " + text;
            m_SpeechRecord.Enqueue(new SpeechRecordEntry(msg));
            while (((SpeechRecordEntry)m_SpeechRecord.Peek()).Time < DateTime.UtcNow - TimeSpan.FromMinutes(5.0))
                m_SpeechRecord.Dequeue();

            if (m_ReportLogger != null)
            {
                m_ReportLogger.Log(LogType.Text, msg);
            }
        }

        public void Report(Mobile from)
        {
            if (m_ReportLogger != null)
            {
                m_ReportLogger.Log(LogType.Text, "\n**** Reported again by " + from.Name + " ****\n");
                m_ReportLogger.Finish();
                if (m_ReportLogStopper != null)
                    m_ReportLogStopper.Stop();
            }

            m_Reported = DateTime.UtcNow;
            m_ReportLogger = new LogHelper(GetReportLogName(m_Reported.ToString("MM-dd-yyyy HH-mm-ss")));
            m_ReportLogger.Log(LogType.Text, string.Format("{0} (acct {1}, SN {2}, IP {3}) reported by {4} (acct {5}, SN {6}) at {7}, at {8}.\r\n\r\n",
                this.Name, ((Account)this.Account).Username, this.Serial, ((this.NetState != null) ? this.NetState.ToString() : ""), from.Name, ((Account)from.Account).Username, from.Serial, DateTime.UtcNow, from.Location));
            //Console.WriteLine("{0} (acct {1}, SN {2}, IP {3}) reported by {4} (acct {5}, SN {6}) at {7}, at {8}.\r\n\r\n",
            //    this.Name, ((Account)this.Account).Username, this.Serial, this.NetState.ToString(), from.Name, ((Account)from.Account).Username, from.Serial, DateTime.UtcNow, from.Location);

            while (m_SpeechRecord.Count > 0)
                m_ReportLogger.Log(LogType.Text, ((SpeechRecordEntry)m_SpeechRecord.Dequeue()).Speech);

            m_ReportLogStopper = Timer.DelayCall(ReportTime, new TimerCallback(EndReport));
        }

        private string GetReportLogName(string datestring)
        {
            string filename = string.Format("{0} {1}.log", datestring, this.Name);

            char[] illegalcharacters = { '\\', '/', ':', '*', '?', '\"', '<', '>', '|' };

            if (filename.IndexOfAny(illegalcharacters) != -1)
            {
                for (int i = 0; i < illegalcharacters.Length; i++)
                {
                    filename = filename.Replace(illegalcharacters[i], '_');
                }
            }

            return filename.Trim();
        }

        private void EndReport()
        {
            if (m_ReportLogger != null)
            {
                m_ReportLogger.Finish();
                m_ReportLogger = null;
            }
            if (m_ReportLogStopper != null)
            {
                m_ReportLogStopper.Stop();
                m_ReportLogStopper = null;
            }
        }

        public override void OnSaid(SpeechEventArgs e)
        {
            base.OnSaid(e);

            // this is the case when text is spoken via Razor
            //  the other case is in RazorInstrument when text is spoken via RSM or other RazorInstrument
            Engines.MusicRecorder.Record(e.Mobile, e.Speech);

            RecordSpeech(e.Mobile, e.Speech, (e.Blocked ? "blocked" : null));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (m_ReportLogger != null && from != this)
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (e.Mobile == this)
                LocalCommand(e);

            RecordSpeech(e.Mobile, e.Speech, null);

            if (e.Mobile.Player)
            {
                string filename = Utility.ValidFileName(string.Format("{0}", e.Mobile)) + ".log";
                LogHelper logger = new LogHelper(string.Format("Logs/Players/{0}", filename), false, true, true);
                logger.Log(e.Speech);
                logger.Finish();
            }

            if (e.Mobile == this)
                return;
        }
        private void LocalCommand(SpeechEventArgs e)
        {
            if (e != null && !string.IsNullOrEmpty(e.Speech))
            {
                if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SiegeBless))
                    if ((Core.RuleSets.SiegeStyleRules()) && e.Speech.ToLower().Contains("i wish to bless this"))
                    {
                        if (e.Mobile.Target != null)
                            // do we need to worry about players using this command to cancel a spell?
                            e.Mobile.Target.Cancel(e.Mobile, TargetCancelType.Canceled);
                        e.Mobile.Target = new GetItemTarget();
                        return;
                    }
            }
        }

        public class GetItemTarget : Target
        {
            public GetItemTarget()
                : base(8, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Item item && ValidTarget(from, item) && (from as PlayerMobile).CanBlessFor(item))
                {
                    item.BlessedFor = (from as PlayerMobile).CalcBlessedFor(item);
                }
                else
                {
                    if (target is Item && Item.IsLootTypeSet((target as Item), LootType.UnLootable | LootType.UnStealable))
                        from.SendLocalizedMessage(1045113, "", 0x482); // That item is already blessed
                    else
                        from.SendLocalizedMessage(1045114, "", 0x482); // You cannot bless that item

                    //from.SendMessage("That is not an item that can be blessed.");
                }
            }
            private bool ValidTarget(Mobile from, Item item)
            {
                if (item != null && item.Deleted == false)
                    if (from != null && from is PlayerMobile/*add any other mobile restrictions here*/)
                    {
                        // backpack
                        if (from.Backpack is Container bp && bp.Items != null && bp.Items.Count > 0)
                            if (bp.Items.Contains(item))
                                return true;

                        // bank
                        if (from.BankBox is Container bb && bb.Items != null && bb.Items.Count > 0)
                            if (bb.Items.Contains(item))
                                return true;

                        // worn
                        foreach (Layer layer in Enum.GetValues(typeof(Layer)))
                        {
                            if (layer == Layer.Invalid || layer == Layer.FirstValid || layer == Layer.Unused_xF || layer == Layer.FacialHair || layer == Layer.Backpack || layer >= Layer.LastUserValid)
                                continue;

                            if (from.FindItemOnLayer(layer) == item)
                                return true;
                        }
                    }

                return false;
            }
        }

        public override void SendAlliedChat(string text)
        {
            Server.Guilds.Guild g = this.Guild as Server.Guilds.Guild;
            if (g != null)
            {
                //g.AlliedMessage( string.Format("[Ally][{0} [{1}]]: {2}", this.Name, g.Abbreviation, text ) );
                g.AlliedChat(text, this);

                //Let GM+ overhear
                Packet p = null;
                IPooledEnumerable eable = this.GetClientsInRange(8);
                foreach (NetState ns in eable)
                {
                    Mobile mob = ns.Mobile;

                    if (mob != null
                        && mob.AccessLevel >= AccessLevel.GameMaster
                        && mob.AccessLevel > this.AccessLevel)
                    {
                        if (p == null)
                            p = Packet.Acquire(new UnicodeMessage(this.Serial, this.Body, MessageType.Regular, this.SpeechHue, 3, this.Language, this.Name, string.Format("[Allied]: {0}", text)));

                        ns.Send(p);
                    }
                }
                eable.Free();
                //end GM+ overhear

                Packet.Release(p);

                // record speech
                RecordSpeech(this, text, "allied");
            }
            else
            {
                this.SendMessage(76, "You can't send a message to your allies if you don't belong to a guild.");
            }
        }

        public override void SendGuildChat(string text)
        {
            Server.Guilds.Guild g = this.Guild as Server.Guilds.Guild;
            if (g != null)
            {
                //g.GuildMessage( string.Format("[Guild][{0}]: {1}", this.Name, text) );
                g.GuildChat(text, this);

                //Let GM+ overhear
                Packet p = null;
                IPooledEnumerable eable = this.GetClientsInRange(8);
                foreach (NetState ns in eable)
                {
                    Mobile mob = ns.Mobile;

                    if (mob != null
                        && mob.AccessLevel >= AccessLevel.GameMaster
                        && mob.AccessLevel > this.AccessLevel)
                    {
                        if (p == null)
                            p = Packet.Acquire(new UnicodeMessage(this.Serial, this.Body, MessageType.Regular, this.SpeechHue, 3, this.Language, this.Name, string.Format("[Guild]: {0}", text)));

                        ns.Send(p);
                    }
                }
                eable.Free();
                //end GM+ overhear

                Packet.Release(p);

                // record speech
                RecordSpeech(this, text, "guild");

                if (true)
                {
                    string filename = Utility.ValidFileName("[Alliance][" + this + "]: ") + ".log";
                    LogHelper logger = new LogHelper(string.Format("Logs/Players/{0}", filename), false, true, true);
                    logger.Log(text);
                    logger.Finish();
                }
            }
            else
            {
                this.SendMessage(76, "You can't send a message to your guild if you don't belong to one.");
            }
        }
        public override void OnHit(Mobile attacker, Mobile defender)
        {
            if (attacker == this)
                MissTracker(attacker, defender, false);
        }
        public override void OnMiss(Mobile attacker, Mobile defender)
        {
            if (attacker == this)
                MissTracker(attacker, defender, true);
        }

        #region MISS TRACKER
        double MissTracker_miss = 0;
        double MissTracker_hit = 0;
        public void MissTracker(Mobile attacker, Mobile defender, bool miss)
        {
            if (Core.HITMISS == 0)         // off
                return;                     // if not turned on, ignore

            if (Core.HITMISS == 2)         // reset
            {
                Core.HITMISS = 1;          // resume on state
                MissTracker_miss = MissTracker_hit = 0;
                return;
            }
            if (miss) MissTracker_miss++; else MissTracker_hit++;
            double average = (MissTracker_hit / (MissTracker_miss + MissTracker_hit)) * 100.0;
            string message = string.Format("Misses: {0}, Hits:{1}. Hit average {2}%", MissTracker_miss, MissTracker_hit, (int)average);
            attacker.SendMessage(message);
            string filename = string.Format("MissTracker-{0}.log", this.Name);
            LogHelper logger = new LogHelper(filename, false, true);
            logger.Log(LogType.Text, message);
            logger.Finish();
        }
        #endregion

        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            //if ( Spells.Necromancy.EvilOmenSpell.CheckEffect( this ) )
            //amount = (int)(amount * 1.25);

            //Mobile oath = Spells.Necromancy.BloodOathSpell.GetBloodOath( from );
            /*
					if ( oath == this )
					{
						amount = (int)(amount * 1.1);
						from.Damage( amount, from );
					}
			*/

            base.Damage(amount, from, source_weapon);

            //Explosion Potion Check
            if (amount >= CoreAI.ExplosionPotionSensitivityLevel)
            {
                if (this.Backpack != null)
                {
                    Item[] explosionPotions = this.Backpack.FindItemsByType(typeof(Server.Items.BaseExplosionPotion), true);
                    for (int i = 0; i < explosionPotions.Length; i++)
                    {
                        double chance = CoreAI.ExplosionPotionChance;
                        double alchyskill = this.Skills[SkillName.Alchemy].Value;

                        //NOTE: chance will ALWAYS be 0 for a GM alchemist
                        chance *= ((100.0 - alchyskill) / 100.0);

                        if (Utility.RandomDouble() < chance)
                        {
                            SendMessage("Your explosive potion is jostled thus setting it off!");
                            ((Server.Items.BaseExplosionPotion)explosionPotions[i]).Explode(this, false, this.Location, this.Map);
                        }
                    }
                }
            }

        }

        public override ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
        {
            if (!Alive)
                return ApplyPoisonResult.Immune;

            //if ( Spells.Necromancy.EvilOmenSpell.CheckEffect( this ) )
            //return base.ApplyPoison( from, PoisonImpl.IncreaseLevel( poison ) );

            return base.ApplyPoison(from, poison);
        }

        public PlayerMobile(Serial s)
            : base(s)
        {
            m_VisList = new ArrayList();
            m_SpeechRecord = new Queue();
            m_Reported = DateTime.MinValue;
            //InvalidateMyRunUO();
        }

        public ArrayList VisibilityList
        {
            get { return m_VisList; }
        }

        public void RemoveVis(int indexnum) //added 08/30/04 smerX
        {
            if (m_VisList.Count >= indexnum)
            {
                m_VisList.RemoveAt(indexnum);
            }
        }

        public ArrayList PermaFlags
        {
            get { return m_PermaFlags; }
        }

        public override int Luck { get { return AosAttributes.GetValue(this, AosAttribute.Luck); } }

        public override bool IsHarmfulCriminal(Mobile target)
        {
            if (SkillHandlers.Stealing.ClassicMode && target is PlayerMobile && ((PlayerMobile)target).m_PermaFlags.Count > 0)
            {
                int noto = Notoriety.Compute(this, target);

                if (noto == Notoriety.Innocent)
                    target.Delta(MobileDelta.Noto);

                return false;
            }

            if (target is BaseCreature
                && ((BaseCreature)target).InitialInnocent
                && ((BaseCreature)target).Controlled == false)
            {
                return false;
            }

            return base.IsHarmfulCriminal(target);
        }

        public override bool ReportableForMurder(Mobile aggressor, bool criminal)
        {
            /*
			 * When evils kill innocents that they attacked, all the evil's stats and skills fall by 50% for five minutes . 
			 * They also lose lifeforce . If the evil has 0 lifeforce, then the evil can also be reported for murder. 
			 */

            Ethics.Player aggressorEPL = Server.Ethics.Player.Find(aggressor, true);
            Ethics.Player victimEPL = Server.Ethics.Player.Find(this);
            bool aggressorIsPowerfulEvil = (aggressorEPL != null && aggressorEPL.Ethic == Ethics.Ethic.Evil && aggressorEPL.Power > 0);
            bool victimIsinnocent = (victimEPL == null);

            if (aggressorIsPowerfulEvil && victimIsinnocent)
                return false;
            else
                return criminal;
        }

        private void RevertHair()
        {
            SetHairMods(-1, -1);
        }

        private Engines.BulkOrders.BOBFilter m_BOBFilter;

        public Engines.BulkOrders.BOBFilter BOBFilter
        {
            get { return m_BOBFilter; }
        }

        private SaveFlag ReadSaveBits(GenericReader reader, int currentVersion, int firstVersion)
        {
            if (currentVersion < firstVersion)
                return SaveFlag.None;
            else
                return (SaveFlag)reader.ReadInt();
        }

        private SaveFlag WriteSaveBits(GenericWriter writer)
        {   // calculate save flags
            SaveFlag flags = SaveFlag.None;
            SetSaveFlag(ref flags, SaveFlag.NPCGuild, NpcGuild != NpcGuild.None);
            SetSaveFlag(ref flags, SaveFlag.ZCodeMiniGame, ZCodeMiniGameID != 0);
            SetSaveFlag(ref flags, SaveFlag.EthicPoints, EthicKillsLogList.Count > 0 && EthicPlayer == null);   // don't save if an EthicPlayer
            SetSaveFlag(ref flags, SaveFlag.EventScore, EventScore > 0);
            SetSaveFlag(ref flags, SaveFlag.JumpList, VerifyJumpList());
            SetSaveFlag(ref flags, SaveFlag.SiegeBlessedItem, SiegeBlessedItem != null);
            SetSaveFlag(ref flags, SaveFlag.RoTData, m_SkillGainEpoch != DateTime.MinValue);
            SetSaveFlag(ref flags, SaveFlag.MinimumSentence, m_MinimumSentence != DateTime.MinValue);
            SetSaveFlag(ref flags, SaveFlag.MurderReprieve, GetPlayerBool(PlayerBoolTable.MurderReprieve) && !GetPlayerBool(PlayerBoolTable.MurderReprieveRescinded));
            SetSaveFlag(ref flags, SaveFlag.GMHistory, m_GMHistoryTable != SkillNameFlags._None);
            writer.Write((int)flags);
            return flags;
        }
        public override void Serialize(GenericWriter writer)
        {
            SkillGainSystem.CleanupAntiMacroTable(this);
            base.Serialize(writer);
            int version = 48;
            writer.Write(version);                      // write the version    
            SaveFlag saveFlags = WriteSaveBits(writer); // calculate and write the bits that describe what we will write now / read later

            ///////////////////////////////////////////////////
            // put all normal serialization below this line
            ///////////////////////////////////////////////////

            // version 48
            if (GetSaveFlag(saveFlags, SaveFlag.GMHistory) == true)
                writer.Write((ulong)m_GMHistoryTable);

            // version 47
            writer.WriteEncodedInt(m_RewardHue);

            // version 46: BOD overhaul #3

            // version 45
            if (GetSaveFlag(saveFlags, SaveFlag.MurderReprieve) == true)
            {
                writer.WriteEncodedInt(m_ReprieveShortTermMurders);
                writer.WriteEncodedInt(m_ReprieveLongTermMurders);
            }

            // version 44 - Adam: prison sentence
            if (GetSaveFlag(saveFlags, SaveFlag.MinimumSentence) == true)
                writer.Write(m_MinimumSentence);

            // Yoar: v43: Customizable alignment title display
            writer.Write((byte)m_AlignmentTitleDisplay);

            // v42
            if (GetSaveFlag(saveFlags, SaveFlag.RoTData) == true)
            {
                writer.Write(SkillInfo.Table.Length);
                for (int ix = 0; ix < SkillInfo.Table.Length; ix++)
                    writer.Write(m_DailyRoTSkillGainLimits[ix]);
                for (int ix = 0; ix < SkillInfo.Table.Length; ix++)
                    writer.Write(m_LastRoTSkillGainTime[ix]);
                writer.Write(m_DailyRoTTotalStatIncrease);
                writer.Write(m_SkillGainEpoch);
                writer.Write(m_DailyRoTSysGainLimitTime);
            }

            // v41
            if (GetSaveFlag(saveFlags, SaveFlag.JumpList) == true)
            {
                writer.Write(m_JumpIndex);
                writer.Write(m_JumpList.Count);
                foreach (object o in m_JumpList)
                {
                    if (o is Item i)
                    {
                        writer.Write(1);
                        writer.Write(i);
                    }
                    else if (o is Mobile m)
                    {
                        writer.Write(2);
                        writer.Write(m);
                    }
                    else if (o is Point2D p2d)
                    {
                        writer.Write(3);
                        writer.Write(p2d);
                    }
                    else if (o is Point3D p3d)
                    {
                        writer.Write(4);
                        writer.Write(p3d);
                    }
                    else if (o is WorldLocation wl)
                    {
                        writer.Write(5);
                        writer.Write(wl);
                        writer.Write(wl.M);
                    }
                    else
                    {
                        Utility.ConsoleWriteLine("Error: Unknown type in jumplist for {0}", ConsoleColor.Red, this);
                        writer.Write(0);    // unknown type
                    }
                }
            }

            // Adam: v40: save your siege blessed item
            if (GetSaveFlag(saveFlags, SaveFlag.SiegeBlessedItem) == true)
                writer.Write(m_siegeBlessedItem);

            // Yoar: v39: Stat gain points
            writer.Write((float)m_StrGainPoints);
            writer.Write((float)m_DexGainPoints);
            writer.Write((float)m_IntGainPoints);

            // Yoar: v38: Now serializing skill gain points
            if (m_SkillGainPoints != null)
            {
                writer.Write((bool)true);
                m_SkillGainPoints.Serialize(writer);
            }
            else
            {
                writer.Write((bool)false);
            }

            // Yoar: v36: BOD overhaul #1

            if (version <= 40)
            {
                // Adam: v35: save my jumplist for '[next' command
                if (GetSaveFlag(saveFlags, SaveFlag.JumpList) == true)
                {
                    writer.Write(m_JumpIndex);
                    writer.Write(m_JumpList.Count);
                    if (m_JumpList.Count > 0)
                    {
                        if (JumpList[0] is Item)
                        {
                            writer.Write(1);
                            foreach (Item ix in m_JumpList)
                                writer.Write(ix);
                        }
                        else if (JumpList[0] is Mobile)
                        {
                            writer.Write(2);
                            foreach (Mobile mx in m_JumpList)
                                writer.Write(mx);
                        }
                        else if (JumpList[0] is Point2D)
                        {
                            writer.Write(3);
                            foreach (Point2D location2D in m_JumpList)
                                writer.Write(location2D);
                        }
                        else if (JumpList[0] is Point3D)
                        {
                            writer.Write(4);
                            foreach (Point3D location3D in m_JumpList)
                                writer.Write(location3D);
                        }
                        else
                            writer.Write(0);    // unknown type
                    }
                }
            }

            // Adam: v34: save any event score
            if (GetSaveFlag(saveFlags, SaveFlag.EventScore) == true)
            {
                writer.Write(EventScore);
            }

            // Adam: v33: Ethic pre enrollment points
            if (GetSaveFlag(saveFlags, SaveFlag.EthicPoints) == true)
            {
                writer.Write(EthicPoints);  // non expired kills
                for (int ix = 0; ix < EthicKillsLogList.Count; ix++)
                {
                    if (EthicKillsLogList[ix].Expired == false)
                    {
                        writer.Write(EthicKillsLogList[ix].Serial);
                        writer.Write(EthicKillsLogList[ix].Killed);
                    }
                }
            }

            //Adam: v32 add mini game ID and save data.
            if (GetSaveFlag(saveFlags, SaveFlag.ZCodeMiniGame) == true)
            {   // assert (record) this case 
                if (Misc.Diagnostics.Assert(m_ZCodeMiniGameData != null && m_ZCodeMiniGameData.Length > 0, "In PlayerMobile.cs the following is NOT true: m_ZCodeMiniGameData != null && m_ZCodeMiniGameData.Length > 0", Utility.FileInfo()))
                {
                    writer.Write(m_ZCodeMiniGameID);                                    // hash code of the string naming the mini game
                    writer.Write(m_ZCodeMiniGameData.Length);                           // buffer size
                    writer.Write(m_ZCodeMiniGameData, 0, m_ZCodeMiniGameData.Length);   // saved game
                }
            }

            //Adam: v31 Add in new points tracker for NPCGuilds
            if (GetSaveFlag(saveFlags, SaveFlag.NPCGuild) == true)
                writer.Write(m_NpcGuildPoints);

            //Adam: v.30 Dummy version, removed NPCGuild vars when not needed

            //Pla: v.29 - dummy version for duel system removal

            //Pix: v.28 - Kin Faction Stuff
            writer.Write(0.0/*m_KinSoloPoints*/);
            writer.Write(0.0/*m_KinTeamPoints*/);

            //Pix: v.27 - Challenge Duel
            //Pla: No longer used as of v.29
            //writer.Write(m_iChallengeDuelWins);
            //writer.Write(m_iChallengeDuelLosses);

            //Adam: v.26
            writer.Write(m_Blind);
            writer.Write(m_SightExpire);

            //Pix: v.25 Watchlist enhancements
            writer.Write(m_WatchReason);
            writer.Write(m_WatchExpire);

            //Rhi: [FilterMusic
            writer.Write(m_FilterMusic);

            //PIX: new IOB funcionality
            writer.Write(0.0/*m_IOBKillPoints*/);
            writer.Write((int)m_LastGuildIOBAlignment);
            writer.Write(this.m_LastGuildChange);

            // tk - [report
            writer.Write(m_Reported);

            //Pix: Offline short count decay
            writer.Write(m_LastShortDecayed);

            //Pix: TimeSpan for RANK of bretheren
            TimeSpan ranktime = m_IOBRankTime;
            if (IOBEquipped && m_IOBStartedWearing > DateTime.MinValue)
            {
                ranktime += (DateTime.UtcNow - m_IOBStartedWearing);
            }
            writer.Write(ranktime);

            //Pix: 3/26/06 - changes to IOB system
            // no longer store IOBAlignment or IOBTimer in PMs
            //writer.Write( (int) m_IOBAlignment );
            //writer.Write( IOBTimer );
            writer.Write((bool)m_IOBEquipped);

            QuestSerializer.Serialize(m_Quest, writer);

            if (m_DoneQuests == null)
            {
                writer.WriteEncodedInt((int)0);
            }
            else
            {
                writer.WriteEncodedInt((int)m_DoneQuests.Count);

                for (int i = 0; i < m_DoneQuests.Count; ++i)
                {
                    QuestRestartInfo restartInfo = (QuestRestartInfo)m_DoneQuests[i];

                    QuestSerializer.Write((Type)restartInfo.QuestType, QuestSystem.QuestTypes, writer);
                    writer.Write((DateTime)restartInfo.RestartTime);
                }
            }

            writer.WriteEncodedInt((int)m_Profession);

            writer.WriteDeltaTime(m_LastDisconnect);

            writer.WriteEncodedInt(m_CompassionGains);

            if (m_CompassionGains > 0)
                writer.WriteDeltaTime(m_NextCompassionDay);

            m_BOBFilter.Serialize(writer);

            bool useMods = (m_HairModID != -1 || m_BeardModID != -1);

            writer.Write(useMods);

            if (useMods)
            {
                writer.Write((int)m_HairModID);
                writer.Write((int)m_HairModHue);
                writer.Write((int)m_BeardModID);
                writer.Write((int)m_BeardModHue);
            }

            writer.Write(SavagePaintExpiration);

            // Adam: Version 30 optimization: Only write values if we belong to a guild
            if (GetSaveFlag(saveFlags, SaveFlag.NPCGuild) == true)
            {
                writer.Write((int)m_NpcGuild);
                writer.Write((DateTime)m_NpcGuildJoinTime);
                writer.Write((TimeSpan)m_NpcGuildGameTime);
            }

            writer.WriteMobileList(m_PermaFlags, true);

            // Yoar: BOD overhaul
#if old
            writer.Write(NextTailorBulkOrder);

            writer.Write(NextSmithBulkOrder);
#endif

            writer.WriteDeltaTime(m_LastJusticeLoss);
            writer.WriteMobileList(m_JusticeProtectors, true);

            writer.WriteDeltaTime(m_LastSacrificeGain);
            writer.WriteDeltaTime(m_LastSacrificeLoss);
            writer.Write(m_AvailableResurrects);

            writer.Write((uint)m_Flags);

            writer.Write(m_LongTermElapse);
            writer.Write(m_ShortTermElapse);
            writer.Write(this.GameTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            SaveFlag saveFlags = ReadSaveBits(reader, version, 30); // read bits that describe what was written before and we will read now

            ///////////////////////////////////////////////////
            // put all normal serialization below this line
            ///////////////////////////////////////////////////

            switch (version)
            {
                case 48:
                    {
                        if (GetSaveFlag(saveFlags, SaveFlag.GMHistory) == true)
                            m_GMHistoryTable = (SkillNameFlags)reader.ReadULong();
                        goto case 47;
                    }
                case 47:
                    {
                        m_RewardHue = reader.ReadEncodedInt();
                        goto case 46;
                    }
                case 46: // Yoar: BOD overhaul #3
                case 45:
                    {   // Adam: prison sentence
                        if (GetSaveFlag(saveFlags, SaveFlag.MurderReprieve) == true)
                        {
                            m_ReprieveShortTermMurders = reader.ReadEncodedInt();
                            m_ReprieveLongTermMurders = reader.ReadEncodedInt();
                        }

                        goto case 44;
                    }
                case 44:
                    {   // Adam: prison sentence
                        if (GetSaveFlag(saveFlags, SaveFlag.MinimumSentence) == true)
                            m_MinimumSentence = reader.ReadDateTime();

                        goto case 43;
                    }
                case 43: // Yoar: v43: Customizable alignment title display
                    {
                        m_AlignmentTitleDisplay = (TitleDisplay)reader.ReadByte();

                        goto case 42;
                    }
                case 42: // Adam: v42: RoT System
                    {
                        if (GetSaveFlag(saveFlags, SaveFlag.RoTData) == true)
                        {
                            int tableSize = reader.ReadInt();
                            m_DailyRoTSkillGainLimits = new byte[tableSize];   // individual skill daily gain caps
                            for (int ix = 0; ix < tableSize; ix++)
                                m_DailyRoTSkillGainLimits[ix] = reader.ReadByte();

                            m_LastRoTSkillGainTime = new ushort[tableSize];    // individual skill daily gain delays
                            for (int ix = 0; ix < tableSize; ix++)
                                m_LastRoTSkillGainTime[ix] = reader.ReadUShort();

                            m_DailyRoTTotalStatIncrease = reader.ReadByte();

                            m_SkillGainEpoch = reader.ReadDateTime();

                            m_DailyRoTSysGainLimitTime = reader.ReadDateTime();
                        }
                        goto case 41;
                    }
                case 41:
                    {
                        if (GetSaveFlag(saveFlags, SaveFlag.JumpList) == true)
                        {
                            m_JumpList = new ArrayList();
                            m_JumpIndex = reader.ReadInt();
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                int type = reader.ReadInt();
                                switch (type)
                                {
                                    // item
                                    case 1: m_JumpList.Add(reader.ReadItem()); break;
                                    // mobile
                                    case 2: m_JumpList.Add(reader.ReadMobile()); break;
                                    // point 2d
                                    case 3: m_JumpList.Add(reader.ReadPoint2D()); break;
                                    // point 3d
                                    case 4: m_JumpList.Add(reader.ReadPoint3D()); break;
                                    // WorldLocation (point 3d + map)
                                    case 5: m_JumpList.Add(new WorldLocation(reader.ReadPoint3D(), reader.ReadByte())); break;
                                    default:
                                        {
                                            Utility.ConsoleWriteLine("Error: Unknown type in jumplist for {0}", ConsoleColor.Red, this);
                                            reader.ReadInt();
                                            break;
                                        }
                                }
                            }
                        }
                        goto case 40;
                    }
                case 40: // Adam: v40: siege blessed item
                    {
                        if (GetSaveFlag(saveFlags, SaveFlag.SiegeBlessedItem) == true)
                            m_siegeBlessedItem = reader.ReadItem();
                        goto case 39;
                    }
                case 39: // Yoar: v39: Stat gain points
                    {
                        m_StrGainPoints = reader.ReadFloat();
                        m_DexGainPoints = reader.ReadFloat();
                        m_IntGainPoints = reader.ReadFloat();
                        goto case 38;
                    }
                case 38: // Yoar: Now serializing skill gain points
                    {
                        if (reader.ReadBool())
                        {
                            SkillGainPoints skillGainPoints = new SkillGainPoints();
                            skillGainPoints.Deserialize(reader);
                            if (!skillGainPoints.IsEmpty())
                                m_SkillGainPoints = skillGainPoints;
                        }
                        goto case 37;
                    }
                case 37: // Yoar: BOD overhaul #2
                    {
                        if (version < 46)
                            reader.ReadByte(); // bulk order banking setting - forget
                        goto case 36;
                    }
                case 36: // Yoar: BOD overhaul #1
                case 35:
                    {
                        if (version <= 40)
                        {
                            if (GetSaveFlag(saveFlags, SaveFlag.JumpList) == true)
                            {
                                m_JumpList = new ArrayList();
                                m_JumpIndex = reader.ReadInt();
                                int count = reader.ReadInt();
                                int type = reader.ReadInt();
                                if (type == 1)  // item
                                {
                                    for (int ix = 0; ix < count; ix++)
                                        m_JumpList.Insert(ix, reader.ReadItem());
                                }
                                else if (type == 2) // mobile
                                {
                                    for (int ix = 0; ix < count; ix++)
                                        m_JumpList.Insert(ix, reader.ReadMobile());
                                }
                                else if (type == 3) // point2D
                                {
                                    for (int ix = 0; ix < count; ix++)
                                        m_JumpList.Insert(ix, reader.ReadPoint2D());
                                }
                                else if (type == 4) // point3D
                                {
                                    for (int ix = 0; ix < count; ix++)
                                        m_JumpList.Insert(ix, reader.ReadPoint3D());
                                }
                                else
                                {   // unknown type
                                    //  We didn't serialize anything, so we won't try to read.
                                    //  If you want to read back the jump list, you need to serialize
                                    //  the proper type.
                                }
                            }
                        }
                        goto case 34;
                    }
                case 34:
                    {
                        if (GetSaveFlag(saveFlags, SaveFlag.EventScore) == true)
                            EventScore = reader.ReadUShort();
                        goto case 33;
                    }
                case 33:
                    {
                        if (GetSaveFlag(saveFlags, SaveFlag.EthicPoints) == true)
                        {
                            int count = reader.ReadInt();
                            if (count > 0)
                            {
                                for (int ix = 0; ix < count; ix++)
                                {
                                    EthicKillsLogList.Add(new EthicKillsLog((Serial)reader.ReadInt(), reader.ReadDateTime()));
                                }
                            }
                        }
                        goto case 32;
                    }
                case 32:
                    {
                        //Adam: v32 add mini game ID and save data.
                        if (GetSaveFlag(saveFlags, SaveFlag.ZCodeMiniGame) == true)
                        {
                            m_ZCodeMiniGameID = reader.ReadInt();               // hash code of the string naming the mini game
                            int size = reader.ReadInt();                        // saved game size
                            m_ZCodeMiniGameData = new byte[size];               // allocate a new game buffer
                            for (int ix = 0; ix < size; ix++)
                                m_ZCodeMiniGameData[ix] = reader.ReadByte();    // saved game
                        }
                        goto case 31;
                    }
                case 31:
                    {
                        if (GetSaveFlag(saveFlags, SaveFlag.NPCGuild) == true)
                            m_NpcGuildPoints = reader.ReadDouble();

                        goto case 30;
                    }
                case 30: // Adam: v.30 Dummy version, removed NPCGuild vars when not needed
                    {
                        goto case 29;
                    }
                case 29: //Pla: Dummy version, removed duel system vars
                    {
                        goto case 28;
                    }
                case 28: //Pix: Kin Faction additions
                    {
                        /*m_KinSoloPoints = */
                        reader.ReadDouble();
                        /*m_KinTeamPoints = */
                        reader.ReadDouble();

                        goto case 27;
                    }
                case 27: //Pix: challenge duel system
                    {
                        //pla: not used anymore
                        if (version < 29)
                        {
                            //m_iChallengeDuelWins = reader.ReadInt();
                            //m_iChallengeDuelLosses = reader.ReadInt();
                            reader.ReadInt();
                            reader.ReadInt();
                        }

                        goto case 26;
                    }
                case 26: //Adam: ghost blindness
                    {
                        m_Blind = reader.ReadBool();
                        m_SightExpire = reader.ReadDateTime();
                        if (!Core.RuleSets.AllowGhostBlindness())
                        {
                            m_Blind = false;
                            m_SightExpire = DateTime.MaxValue;
                        }
                        else if (m_SightExpire != DateTime.MaxValue)
                        {
                            if (m_SightExpire <= DateTime.UtcNow)
                                Timer.DelayCall(TimeSpan.Zero, new TimerCallback(GoBlind));
                            else
                                Timer.DelayCall(m_SightExpire - DateTime.UtcNow, new TimerCallback(GoBlind));
                        }

                        goto case 25;
                    }
                case 25: //Pix: WatchList enhancements
                    {
                        m_WatchReason = reader.ReadString();
                        m_WatchExpire = reader.ReadDateTime();
                        goto case 24;
                    }
                case 24: // Rhi: FilterMusic
                    {
                        m_FilterMusic = reader.ReadBool();
                        goto case 23;
                    }
                case 23: // Pix: IOB System changes
                    {
                        /*m_IOBKillPoints = */
                        reader.ReadDouble();
                        m_LastGuildIOBAlignment = (IOBAlignment)reader.ReadInt();
                        m_LastGuildChange = reader.ReadDateTime();

                        goto case 22;
                    }
                case 22:
                    {
                        m_Reported = reader.ReadDateTime();
                        if (m_Reported > DateTime.UtcNow - ReportTime)
                        {
                            m_ReportLogger = new LogHelper(GetReportLogName(m_Reported.ToString("MM-dd-yyyy HH-mm-ss")), false);
                            m_ReportLogStopper = Timer.DelayCall(ReportTime - (DateTime.UtcNow - m_Reported), new TimerCallback(EndReport));
                        }
                        goto case 21;
                    }
                case 21:
                    {
                        LastRegion = Region.Find(this.Location, this.Map);
                        goto case 20;
                    }
                case 20: //Pix: Offline short count decay
                    {
                        m_LastShortDecayed = reader.ReadDateTime();
                        goto case 19;
                    }
                case 19: //Pix - for IOB Ranks
                    {
                        m_IOBRankTime = reader.ReadTimeSpan();
                        goto case 18;
                    }
                case 18: //Pigpen - Addition for IOB Sytem
                    {
                        if (version < 23)
                        {
                            //m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                            //IOBTimer = reader.ReadTimeSpan();
                            reader.ReadInt();
                            reader.ReadTimeSpan();
                        }
                        m_IOBEquipped = reader.ReadBool();
                        goto case 16;
                    }
                case 17: // changed how DoneQuests is serialized
                case 16:
                    {
                        m_Quest = QuestSerializer.DeserializeQuest(reader);

                        if (m_Quest != null)
                            m_Quest.From = this;

                        int count = reader.ReadEncodedInt();

                        if (count > 0)
                        {
                            m_DoneQuests = new ArrayList();

                            for (int i = 0; i < count; ++i)
                            {
                                Type questType = QuestSerializer.ReadType(QuestSystem.QuestTypes, reader);
                                DateTime restartTime;

                                if (version < 17)
                                    restartTime = DateTime.MaxValue;
                                else
                                    restartTime = reader.ReadDateTime();

                                m_DoneQuests.Add(new QuestRestartInfo(questType, restartTime));
                            }
                        }

                        m_Profession = reader.ReadEncodedInt();
                        goto case 15;
                    }
                case 15:
                    {
                        m_LastDisconnect = reader.ReadDeltaTime();
                        goto case 14;
                    }
                case 14:
                    {
                        m_CompassionGains = reader.ReadEncodedInt();

                        if (m_CompassionGains > 0)
                            m_NextCompassionDay = reader.ReadDeltaTime();

                        goto case 13;
                    }
                case 13: // just removed m_PayedInsurance list
                case 12:
                    {
                        m_BOBFilter = new Engines.BulkOrders.BOBFilter(reader);
                        goto case 11;
                    }
                case 11:
                    {
                        if (version < 13)
                        {
                            ArrayList payed = reader.ReadItemList();
                            // Adam: no more insurance
                            //for ( int i = 0; i < payed.Count; ++i )
                            //((Item)payed[i]).PayedInsurance = true;
                        }

                        goto case 10;
                    }
                case 10:
                    {
                        if (reader.ReadBool())
                        {
                            m_HairModID = reader.ReadInt();
                            m_HairModHue = reader.ReadInt();
                            m_BeardModID = reader.ReadInt();
                            m_BeardModHue = reader.ReadInt();

                            // We cannot call SetHairMods( -1, -1 ) here because the items have not yet loaded
                            Timer.DelayCall(TimeSpan.Zero, new TimerCallback(RevertHair));
                        }

                        goto case 9;
                    }
                case 9:
                    {
                        SavagePaintExpiration = reader.ReadTimeSpan();

                        if (SavagePaintExpiration > TimeSpan.Zero)
                        {
                            // Ai uses HUE value and not the BodyMod as there is no sitting graphic
                            if (!Core.RuleSets.AnyAIShardRules())
                                BodyMod = (Female ? 184 : 183);
                            else
                                HueMod = 0;
                        }

                        goto case 8;
                    }
                case 8:
                    {
                        if (version < 30)
                        {
                            m_NpcGuild = (NpcGuild)reader.ReadInt();
                            m_NpcGuildJoinTime = reader.ReadDateTime();
                            m_NpcGuildGameTime = reader.ReadTimeSpan();
                        }
                        else if (GetSaveFlag(saveFlags, SaveFlag.NPCGuild) == true)
                        {
                            m_NpcGuild = (NpcGuild)reader.ReadInt();
                            m_NpcGuildJoinTime = reader.ReadDateTime();
                            m_NpcGuildGameTime = reader.ReadTimeSpan();
                        }
                        goto case 7;
                    }
                case 7:
                    {
                        m_PermaFlags = reader.ReadMobileList();
                        goto case 6;
                    }
                case 6:
                    {
                        if (version < 36)
                            BulkOrderSystem.SetNextBOD(this, BulkOrderType.Tailor, DateTime.UtcNow + reader.ReadTimeSpan());
                        goto case 5;
                    }
                case 5:
                    {
                        if (version < 36)
                            BulkOrderSystem.SetNextBOD(this, BulkOrderType.Smith, DateTime.UtcNow + reader.ReadTimeSpan());
                        goto case 4;
                    }
                case 4:
                    {
                        m_LastJusticeLoss = reader.ReadDeltaTime();
                        m_JusticeProtectors = reader.ReadMobileList();
                        goto case 3;
                    }
                case 3:
                    {
                        m_LastSacrificeGain = reader.ReadDeltaTime();
                        m_LastSacrificeLoss = reader.ReadDeltaTime();
                        m_AvailableResurrects = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_Flags = (PlayerBoolTable)reader.ReadUInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_LongTermElapse = reader.ReadTimeSpan();
                        m_ShortTermElapse = reader.ReadTimeSpan();
                        m_GameTime = reader.ReadTimeSpan();
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }

            if (m_PermaFlags == null)
                m_PermaFlags = new ArrayList();

            if (m_JusticeProtectors == null)
                m_JusticeProtectors = new ArrayList();

            if (m_BOBFilter == null)
                m_BOBFilter = new Engines.BulkOrders.BOBFilter();

            //ArrayList list = this.PetStable;

            //for (int i = 0; i < list.Count; ++i)
            //{
            //    BaseCreature bc = list[i] as BaseCreature;

            //    if (bc != null)
            //        bc.IsStabled = true;
            //}

            //Pix: this is for safety... to make sure it's set
            m_InmateLastDeathTime = DateTime.MinValue;

            //Pix: make sure this is set to minvalue for loading
            m_IOBStartedWearing = DateTime.MinValue;

            //wea: SpiritCohesion is not persistent across saves
            m_SpiritCohesion = 0;

            //wea: For spirit cohesion, last resurrect time
            m_LastResurrectTime = DateTime.MinValue;
        }


        private bool VerifyJumpList()
        {
            if (m_JumpList == null || m_JumpList.Count == 0)
                return false;
            else
            {
                foreach (object o in m_JumpList)
                {
                    if (o is Item)
                        continue;       // known type
                    else if (o is Mobile)
                        continue;       // known type
                    else if (o is Point2D)
                        continue;       // known type
                    else if (o is Point3D)
                        continue;       // known type
                    else
                        return false;   // unknown type
                }

                return true;            // all objects are known types
            }
        }
        public void ResetKillTime()
        {
            if (PrisonInmate && Alive)
            {
                m_ShortTermElapse = this.GameTime + TimeSpan.FromHours(4);
                m_LongTermElapse = this.GameTime + TimeSpan.FromHours(20);
            }
            else
            {
                m_ShortTermElapse = this.GameTime + TimeSpan.FromHours(8);
                m_LongTermElapse = this.GameTime + TimeSpan.FromHours(40);
            }

            //also reset last short decay (for offline decay)
            m_LastShortDecayed = DateTime.UtcNow;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public TimeSpan GameTime
        {
            get
            {
                if (NetState != null)
                    return m_GameTime + (DateTime.UtcNow - m_SessionStart);
                else
                    return m_GameTime;
            }

            set
            {   // for testing purposes only
                m_GameTime = value;
            }
        }

        // wea: check region info access level and determine isolation

        public bool IsIsolatedFrom(Mobile m)
        {
            if (m == null || m.Deleted || m == this || AccessLevel > AccessLevel.Player || m.Region == this.Region)
                return false;

            #region Static Region
            StaticRegion sr = StaticRegion.FindStaticRegion(this);
            if (sr != null && sr.IsIsolated)
            {
                StaticRegion other = StaticRegion.FindStaticRegion(m);
                if (other == null || !other.IsIsolated)
                    return true;
                else
                    return false;
            }
            #endregion

            return false;
        }

        public bool IsIsolatedFrom(Item item)
        {
            if (item == null || item.Deleted || item.RootParent == this || AccessLevel > AccessLevel.Player || Region.Find(item.GetWorldLocation(), item.Map) == this.Region)
                return false;

            #region Static Region
            StaticRegion sr = StaticRegion.FindStaticRegion(this);
            if (sr != null && sr.IsIsolated)
            {
                StaticRegion other = StaticRegion.FindStaticRegion(item);
                if (other == null || !other.IsIsolated)
                    return true;
                else
                    return false;
            }
            #endregion

            return false;
        }

        public override bool CanSee(Mobile m)
        {
            // wea: if we're isolated from someone, we can't see them
            if (IsIsolatedFrom(m))
                return false;

            if (m is PlayerMobile && ((PlayerMobile)m).m_VisList.Contains(this))
                return true;

            #region Dueling
            if (m_DuelContext != null && m_DuelPlayer != null && !m_DuelContext.Finished && m_DuelContext.m_Tournament != null && !m_DuelPlayer.Eliminated)
            {
                Mobile owner = m;

                if (owner is BaseCreature)
                {
                    BaseCreature bc = (BaseCreature)owner;

                    Mobile master = bc.GetMaster();

                    if (master != null)
                        owner = master;
                }

                if (m.AccessLevel == AccessLevel.Player && owner is PlayerMobile && ((PlayerMobile)owner).DuelContext != m_DuelContext)
                    return false;
            }
            #endregion

            #region Ghost Blindness
            // if we are blind (mobiles)
            if (m_Blind == true && this.AccessLevel == AccessLevel.Player)
            {
                if (!(m == this || m is BaseHealer ||                                       // IF NOT: me or a healer
                      (m is BaseCreature && (m as BaseCreature).ControlMaster == this)))    // or one of my pets 
                    return false;
            }
            #endregion

            return base.CanSee(m);
        }

        public override bool CanSee(Item item)
        {
            if (IsIsolatedFrom(item))
                return false;

            if (m_DesignContext != null && m_DesignContext.Foundation.IsHiddenToCustomizer(item))
                return false;

            #region Ghost Blindness
            // blind filtering
            if (m_Blind == true && this.AccessLevel == AccessLevel.Player)
            {
                // if we are blind (corpses)
                if (item is Corpse && (item as Corpse).Owner != this) // if not my corpse
                    return false;

                // if we are blind (boats)
                // we do not see boats but our own
                if (item is BaseBoat && BaseBoat.FindBoatAt(this) != item) // if not my boat
                    return false;

                // if we are blind, check for actionable boat parts not counted as part of the multi
                // we do not see boat parts but our own
                BaseBoat boatAtItem = BaseBoat.FindBoatAt(item);
                if (boatAtItem != null && (                                 // boat at item
                    boatAtItem.PPlank.ItemID == item.ItemID ||              // there 4 facings for all of these boat parts!
                    boatAtItem.SPlank.ItemID == item.ItemID ||              //  this is why we pull them from the boat instead of 
                    boatAtItem.TillerMan.ItemID == item.ItemID ||           //  checking the ItemIDs directally
                    boatAtItem.Hold.ItemID == item.ItemID)                 //  ----
                    && BaseBoat.FindBoatAt(this) != boatAtItem)             // and if not my boat
                    return false;
            }
            #endregion

            return base.CanSee(item);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            #region Factions
            Faction faction = Faction.Find(this);

            if (faction != null)
                faction.RemoveMember(this);
            #endregion
        }

        #region Quest stuff
        private QuestSystem m_Quest;
        private ArrayList m_DoneQuests;

        [CommandProperty(AccessLevel.Administrator)]
        public QuestSystem Quest
        {
            get { return m_Quest; }
            set { m_Quest = value; }
        }

        public ArrayList DoneQuests
        {
            get { return m_DoneQuests; }
            set { m_DoneQuests = value; }
        }
        #endregion

        public override void OnSkillChange(SkillName skill, double oldBase, double newBase)
        {   // When we GM a skill under ROT, we never need to go back to ROT for that skill
            //  (allows players to change-up their template and return somewhat reasonably.)
            if (Core.RuleSets.RoTShardEnabled())
                if (newBase >= 100.0)
                    m_GMHistoryTable |= SkillNameFlagsHelper.GetFlag(skill);
        }

        public override bool UseROTForSkill(Skill skill)
        {   // we already GM'ed this skill, so we use normal skill gain from now on
            if ((m_GMHistoryTable & SkillNameFlagsHelper.GetFlag(skill.SkillName)) != 0)
                return false;

            return true;
        }
#if false
        #region MyRunUO Invalidation
        private bool m_ChangedMyRunUO;

        public bool ChangedMyRunUO
        {
            get { return m_ChangedMyRunUO; }
            set { m_ChangedMyRunUO = value; }
        }

        public void InvalidateMyRunUO()
        {
            if (!Deleted && !m_ChangedMyRunUO)
            {
                m_ChangedMyRunUO = true;
                Engines.MyRunUO.MyRunUO.QueueMobileUpdate(this);
            }
        }

        public override void OnKillsChange(int oldValue)
        {
            InvalidateMyRunUO();
        }

        public override void OnGenderChanged(bool oldFemale)
        {
            InvalidateMyRunUO();
        }

        public override void OnGuildTitleChange(string oldTitle)
        {
            InvalidateMyRunUO();
        }

        public override void OnKarmaChange(int oldValue)
        {
            InvalidateMyRunUO();
        }

        public override void OnFameChange(int oldValue)
        {
            InvalidateMyRunUO();
        }

        public override void OnSkillChange(SkillName skill, double oldBase)
        {
            InvalidateMyRunUO();
        }

        public override void OnAccessLevelChanged(AccessLevel oldLevel)
        {
            InvalidateMyRunUO();
        }

        public override void OnRawStatChange(StatType stat, int oldValue)
        {

            if (this.AccessLevel < AccessLevel.GameMaster)
            {
                if (this.StatCap > 225)
                    this.StatCap = 225;
                if (this.RawDex > 100)
                    this.RawDex = 100;
                if (this.RawInt > 100)
                    this.RawInt = 100;
                if (this.RawStr > 100)
                    this.RawStr = 100;
            }

            InvalidateMyRunUO();
        }

        public override void OnDelete()
        {
            InvalidateMyRunUO();
        }
        #endregion
#endif
        private bool m_BedrollLogout;

        public bool BedrollLogout
        {
            get { return m_BedrollLogout; }
            set { m_BedrollLogout = value; }
        }

        #region Ethics
        private Ethics.Player m_EthicPlayer;

        [CommandProperty(AccessLevel.GameMaster)]
        public Ethics.Player EthicPlayer
        {
            get { return m_EthicPlayer; }
            set { m_EthicPlayer = value; }
        }

        public override bool Evil
        {
            get
            {
                // players my now be evil and this has notoriety consequences. To remain consistent, we need access in the Mobile class
                return m_EthicPlayer != null && m_EthicPlayer.Ethic == Ethics.Ethic.Evil;
            }
        }

        public override bool Hero
        {
            get
            {
                // players my now be hero and this has notoriety consequences. To remain consistent, we need access in the Mobile class
                return m_EthicPlayer != null && m_EthicPlayer.Ethic == Ethics.Ethic.Hero;
            }
        }
        #endregion

        #region Factions
        private PlayerState m_FactionPlayerState;

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerState FactionPlayerState
        {
            get { return m_FactionPlayerState; }
            set { m_FactionPlayerState = value; }
        }
        #endregion

        #region Dueling
        private Engines.ConPVP.DuelContext m_DuelContext;
        private Engines.ConPVP.DuelPlayer m_DuelPlayer;

        public Engines.ConPVP.DuelContext DuelContext
        {
            get { return m_DuelContext; }
        }

        public Engines.ConPVP.DuelPlayer DuelPlayer
        {
            get { return m_DuelPlayer; }
            set
            {
                bool wasInTourny = (m_DuelContext != null && !m_DuelContext.Finished && m_DuelContext.m_Tournament != null);

                m_DuelPlayer = value;

                if (m_DuelPlayer == null)
                    m_DuelContext = null;
                else
                    m_DuelContext = m_DuelPlayer.Participant.Context;

                bool isInTourny = (m_DuelContext != null && !m_DuelContext.Finished && m_DuelContext.m_Tournament != null);

                if (wasInTourny != isInTourny)
                    SendEverything();
            }
        }
        #endregion

        public override string ApplyNameSuffix(string suffix)
        {
            if (false /*Young*/) // no young status
            {
                if (suffix.Length == 0)
                    suffix = "(Young)";
                else
                    suffix = string.Concat(suffix, " (Young)");
            }

            #region Ethics
            if (m_EthicPlayer != null)
            {
                if (Hero && (EthicPlayer.Power == 0 || EthicPlayer.History == 0))
                {   // fallen hero
                    if (suffix.Length == 0)
                        suffix = "(Fallen Hero)";
                    else
                        suffix = string.Concat(suffix, " (Fallen Hero)");
                }
                else
                {
                    if (suffix.Length == 0)
                        suffix = m_EthicPlayer.Ethic.Definition.Adjunct.String;
                    else
                        suffix = string.Concat(suffix, " ", m_EthicPlayer.Ethic.Definition.Adjunct.String);
                }
            }
            #endregion

            return base.ApplyNameSuffix(suffix);
        }

        public override TimeSpan GetLogoutDelay()
        {
            if (/*Young ||*/ BedrollLogout)
            {
                return TimeSpan.Zero;
            }

            return base.GetLogoutDelay();
        }

        // this fast walk code is from  Ingvarr on the runuo.com boards
        //	http://www.runuo.com/forums/script-support/46364-speed-hack-detection-help-2.html
        //	not yet tested
        #region Fastwalk Prevention (RUNUO)
        /*
		private static bool FastwalkPrevention = true; // Is fastwalk prevention enabled?
		private static TimeSpan FastwalkThreshold = TimeSpan.FromSeconds(0.095);

		private DateTime m_NextMovementTime;

		public virtual bool UsesFastwalkPrevention { get { return (AccessLevel < AccessLevel.GameMaster); } }

		public virtual TimeSpan ComputeMovementSpeed(Direction dir)
		{
			if ((dir & Direction.Mask) != (this.Direction & Direction.Mask))
				return TimeSpan.Zero;

			bool running = ((dir & Direction.Running) != 0);

			bool onHorse = (this.Mount != null);

			if (onHorse)
				return (running ? TimeSpan.FromSeconds(0.1) : TimeSpan.FromSeconds(0.2)) - TimeSpan.FromSeconds(0.005);

			return (running ? TimeSpan.FromSeconds(0.2) : TimeSpan.FromSeconds(0.4)) - TimeSpan.FromSeconds(0.005);
		}

		public static bool MovementThrottle_Callback(NetState ns)
		{
			PlayerMobile pm = ns.Mobile as PlayerMobile;

			if (pm == null || !pm.UsesFastwalkPrevention)
				return true;

			if (pm.m_NextMovementTime == DateTime.MinValue)
			{
				// has not yet moved
				pm.m_NextMovementTime = DateTime.UtcNow;
				return true;
			}

			TimeSpan ts = pm.m_NextMovementTime - DateTime.UtcNow;

			if (ts < TimeSpan.Zero)
			{
				// been a while since we've last moved
				pm.m_NextMovementTime = DateTime.UtcNow;
				return true;
			}

			return (ts < FastwalkThreshold);
		}
		*/
        #endregion

        #region Fastwalk Prevention
        public static bool FastwalkPrevention = true; // Is fastwalk prevention enabled?
        public static TimeSpan FastwalkThreshold = TimeSpan.FromSeconds(0.4); // Fastwalk prevention will become active after 0.4 seconds
        public static AccessLevel FastWalkAccessOverride = AccessLevel.GameMaster;

        private DateTime m_NextMovementTime;

        public virtual bool UsesFastwalkPrevention { get { return (AccessLevel < FastWalkAccessOverride); } }

        public override TimeSpan ComputeMovementSpeed(Direction dir, bool checkTurning)
        {
            if (checkTurning && (dir & Direction.Mask) != (this.Direction & Direction.Mask))
                return SpeedRunMount; // We are not actually moving, just a direction change

            bool running = ((dir & Direction.Running) != 0);

            bool onHorse;
            if (Core.RuleSets.AngelIslandRules())
            {
                onHorse = (AccessLevel > AccessLevel.Player) && (this.Mount != null);
            }
            else
            {
                onHorse = (this.Mount != null);
            }

            if (onHorse)
                return (running ? SpeedRunMount : SpeedWalkMount);

            return (running ? SpeedRunFoot : SpeedWalkFoot);
        }

        public static int ThrottleCallThreshold = 10;
        public static int ThrottleRunWarningThreshold = 10;
        public static TimeSpan ThrottleCountPeriod = TimeSpan.FromSeconds(1.0);

        public static bool MovementThrottle_Callback(NetState ns)
        {
            if (!FastwalkPrevention)
                return true;

            PlayerMobile pm = ns.Mobile as PlayerMobile;

            if (pm == null || !pm.UsesFastwalkPrevention)
                return true;

            MovementReqCapture.HitMR(pm);

            if (pm.m_NextMovementTime == DateTime.MinValue)
            {
                // has not yet moved
                pm.m_NextMovementTime = DateTime.UtcNow;

                return true;
            }

            TimeSpan ts = pm.m_NextMovementTime - DateTime.UtcNow;

            if (ts < TimeSpan.Zero)
            {
                // been a while since we've last moved
                pm.m_NextMovementTime = DateTime.UtcNow;

                return true;
            }

            if (ts <= FastwalkThreshold)
            {
                return true;
            }
            else
            {
                if (CoreAI.JailFastwalkers && !pm.PrisonInmate && CheckTrap(ts, pm))
                {
                    //HackerTrap(pm, Accounting.Account.AccountInfraction.fastwalk);
                    Utility.ConsoleWriteLine("Jail", ConsoleColor.Red);
                    //pm.DebugOut("Jailed");
                    //Console.WriteLine("Jailed");
                }
                //Console.WriteLine("throttled");
                return false; // this packet is being throttled
            }
        }
        private static Memory TrapMemory = new Memory();
        private static bool CheckTrap(TimeSpan ts, PlayerMobile pm)
        {
            Console.WriteLine("checking");
            if (ts > FastwalkThreshold * CoreAI.FastwalkTrapFactor)
            {
                // if the player exceeds N throttles in Y time, trigger trap
                Memory.ObjectMemory ox;
                if ((ox = TrapMemory.Recall((object)pm)) != null)
                {   // only count if they heading in the same direction
                    if (((Direction)ox.Context) == pm.Direction && pm.FastwalkInfractionCount++ >= CoreAI.FastwalkInfractionThreshold)
                    {
                        Utility.ConsoleWriteLine("Triggered", ConsoleColor.Red);
                        //Console.WriteLine("Triggered");
                        pm.FastwalkInfractionCount = 0;
                        return true;
                    }

                    if (((Direction)ox.Context) != pm.Direction)
                    {   // don't count if they are changing direction
                        pm.FastwalkInfractionCount = (pm.FastwalkInfractionCount > 0 ? pm.FastwalkInfractionCount - 1 : 0);
                        ox.Context = pm.Direction;
                    }
                }
                else
                {   // remember them for this long
                    Utility.ConsoleWriteLine("Remembering", ConsoleColor.Red);
                    //Console.WriteLine("Remembering");
                    TrapMemory.Remember(pm, pm.Direction, CoreAI.FastwalkTrapMemory);
                    pm.FastwalkInfractionCount = 0;
                    return false;
                }
            }
            return false;
        }
        private int m_fastwalkInfractionCount;
        public int FastwalkInfractionCount
        {
            get { return m_fastwalkInfractionCount; }
            set { m_fastwalkInfractionCount = value; }
        }
        #endregion

        public static void HackerTrap(PlayerMobile pm, Accounting.Account.AccountInfraction state)
        {
#if false
            if (state == AccountInfraction.fastwalk)
            {
                pm.InfractionStatus = AccountInfraction.fastwalk;
                pm.FastwalkInfractionCount = 0;
                string reason;
                #region Imprison them
                reason = string.Format("Player imprisoned with LoginStatus {0}.", state);
                Point3D px = Spawner.GetSpawnPosition(Map.Felucca, new Point3D(354, 836, 20), 2, SpawnFlags.None, pm);
                pm.MinimumSentence = DateTime.UtcNow + TimeSpan.FromHours(Core.RuleSets.TestCenterRules() ? 0 : 3);
                Prison.PrisonPlayer prison = new Prison.PrisonPlayer(null, pm, px, reason, sentence: 0);
                prison.GoToPrison();
                #endregion Imprison them

                #region tell staff
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to prison. ({1})", pm, reason));
                #endregion tell staff
            }
#endif
        }

        #region Enemy of One
        private Type m_EnemyOfOneType;
        private bool m_WaitingForEnemy;

        public Type EnemyOfOneType
        {
            get { return m_EnemyOfOneType; }
            set
            {
                Type oldType = m_EnemyOfOneType;
                Type newType = value;

                if (oldType == newType)
                    return;

                m_EnemyOfOneType = value;

                DeltaEnemies(oldType, newType);
            }
        }

        public bool WaitingForEnemy
        {
            get { return m_WaitingForEnemy; }
            set { m_WaitingForEnemy = value; }
        }

        private void DeltaEnemies(Type oldType, Type newType)
        {
            IPooledEnumerable eable = this.GetMobilesInRange(18);
            foreach (Mobile m in eable)
            {
                Type t = m.GetType();

                if (t == oldType || t == newType)
                    Send(new MobileMoving(m, Notoriety.Compute(this, m)));
            }
            eable.Free();
        }
        #endregion

        #region Hair and beard mods
        private int m_HairModID = -1, m_HairModHue;
        private int m_BeardModID = -1, m_BeardModHue;

        public void SetHairMods(int hairID, int beardID)
        {
            if (hairID == -1)
                InternalRestoreHair(true, ref m_HairModID, ref m_HairModHue);
            else if (hairID != -2)
                InternalChangeHair(true, hairID, ref m_HairModID, ref m_HairModHue);

            if (beardID == -1)
                InternalRestoreHair(false, ref m_BeardModID, ref m_BeardModHue);
            else if (beardID != -2)
                InternalChangeHair(false, beardID, ref m_BeardModID, ref m_BeardModHue);
        }

        private Item CreateHair(bool hair, int id, int hue)
        {
            if (hair)
                return Server.Items.Hair.CreateByID(id, hue);
            else
                return Server.Items.Beard.CreateByID(id, hue);
        }

        private void InternalRestoreHair(bool hair, ref int id, ref int hue)
        {
            if (id == -1)
                return;

            Item item = FindItemOnLayer(hair ? Layer.Hair : Layer.FacialHair);

            if (item != null)
                item.Delete();

            if (id != 0)
                AddItem(CreateHair(hair, id, hue));

            id = -1;
            hue = 0;
        }

        private void InternalChangeHair(bool hair, int id, ref int storeID, ref int storeHue)
        {
            Item item = FindItemOnLayer(hair ? Layer.Hair : Layer.FacialHair);

            if (item != null)
            {
                if (storeID == -1)
                {
                    storeID = item.ItemID;
                    storeHue = item.Hue;
                }

                item.Delete();
            }
            else if (storeID == -1)
            {
                storeID = 0;
                storeHue = 0;
            }

            if (id == 0)
                return;

            AddItem(CreateHair(hair, id, 0));
        }
        #endregion

        #region Virtue stuff
        private DateTime m_LastSacrificeGain;
        private DateTime m_LastSacrificeLoss;
        private int m_AvailableResurrects;

        public DateTime LastSacrificeGain { get { return m_LastSacrificeGain; } set { m_LastSacrificeGain = value; } }
        public DateTime LastSacrificeLoss { get { return m_LastSacrificeLoss; } set { m_LastSacrificeLoss = value; } }
        public int AvailableResurrects { get { return m_AvailableResurrects; } set { m_AvailableResurrects = value; } }

        //private DateTime m_NextJustAward;
        private DateTime m_LastJusticeLoss;
        private ArrayList m_JusticeProtectors;

        public DateTime LastJusticeLoss { get { return m_LastJusticeLoss; } set { m_LastJusticeLoss = value; } }
        public ArrayList JusticeProtectors { get { return m_JusticeProtectors; } set { m_JusticeProtectors = value; } }

        private DateTime m_LastDisconnect;
        private DateTime m_NextCompassionDay;
        private int m_CompassionGains;

        public DateTime LastDisconnect { get { return m_LastDisconnect; } set { m_LastDisconnect = value; } }
        public DateTime NextCompassionDay { get { return m_NextCompassionDay; } set { m_NextCompassionDay = value; } }
        public int CompassionGains { get { return m_CompassionGains; } set { m_CompassionGains = value; } }
        #endregion

        public override void OnSingleClick(Mobile from)
        {
            if ((Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) && AccessLevel == AccessLevel.Player && DisableHiddenSelfClick && Hidden && from == this)
                return;

            // incognito/disguise
            if (NameMod != null)
            {
                int hue;

                if (NameHue != -1)
                    hue = NameHue;
                else
                {
                    int notoriety = Notoriety.Compute(from, this);
                    hue = Notoriety.GetHue(notoriety);

                    //PIX: if they're looking innocent, see if there
                    // are any ill-effects from beneficial actions
                    if (notoriety == Notoriety.Innocent)
                    {
                        int namehue = Notoriety.GetBeneficialHue(from, this);
                        if (namehue != 0)
                        {
                            hue = namehue;
                        }
                    }
                }

                PrivateOverheadMessage(MessageType.Label, hue, AsciiClickMessage, Name, from.NetState);
            }
            else
            {
                #region IOB
                if (Core.RuleSets.KinSystemEnabled())
                {
                    //if (Engines.IOBSystem.KinSystemSettings.ShowKinSingleClick)
                    {
                        if (this.Guild != null && (this.DisplayGuildTitle || this.Guild.ForceGuildTitle))
                        {
                            if (this.IOBAlignment != IOBAlignment.None)
                            {
                                string text = string.Format("[{0}]", Server.Engines.IOBSystem.IOBSystem.GetIOBName(this.IOBAlignment));
                                PrivateOverheadMessage(MessageType.Regular, SpeechHue, true, text, from.NetState);
                            }
                        }
                    }

                    /*if (Engines.IOBSystem.KinSystemSettings.ShowStatloss)
                    {
                        if (this.IsInStatloss)
                        {
                            PrivateOverheadMessage(MessageType.Regular, SpeechHue, true, "[STATLOSS]", from.NetState);
                        }
                    }*/
                }
                #endregion

                bool hasFactionTitle = false;

                #region Factions
                if (Core.Factions)
                {
                    if (Map == Faction.Facet)
                    {
                        PlayerState pl = PlayerState.Find(this);

                        if (pl != null)
                        {
                            string text;
                            bool ascii = false;

                            Faction faction = pl.Faction;

                            if (faction.Commander == this)
                                text = string.Concat(this.Female ? "(Commanding Lady of the " : "(Commanding Lord of the ", faction.Definition.FriendlyName, ")");
                            else if (pl.Sheriff != null)
                                text = string.Concat("(The Sheriff of ", pl.Sheriff.Definition.FriendlyName, ", ", faction.Definition.FriendlyName, ")");
                            else if (pl.Finance != null)
                                text = string.Concat("(The Finance Minister of ", pl.Finance.Definition.FriendlyName, ", ", faction.Definition.FriendlyName, ")");
                            else
                            {
                                ascii = true;

                                if (pl.MerchantTitle != MerchantTitle.None)
                                    text = string.Concat("(", MerchantTitles.GetInfo(pl.MerchantTitle).Title.String, ", ", faction.Definition.FriendlyName, ")");
                                else
                                    text = string.Concat("(", pl.Rank.Title.String, ", ", faction.Definition.FriendlyName, ")");
                            }

                            int hue = (Faction.Find(from) == faction ? 98 : 38);

                            PrivateOverheadMessage(MessageType.Label, hue, ascii, text, from.NetState);

                            hasFactionTitle = true;
                        }

                        // Hero/Evil
                        if (Core.OldEthics)
                        {
                            Ethics.Player myEPL = Server.Ethics.Player.Find(this);
                            if (myEPL != null && from == this)
                            {
                                int notoriety = Notoriety.Compute(from, this);
                                int hue = Notoriety.GetHue(notoriety);
                                // I use history here since it represents the greatest life force acquired.
                                //	not sure if this is right :/
                                PrivateOverheadMessage(MessageType.Label, hue, true, string.Format("You have mastered sphere {0} and have {1} units of life force remaining.", myEPL.History / 10, myEPL.Power), from.NetState);
                            }
                        }
                    }
                }
                #endregion

                #region Alignment
                if (AlignmentSystem.Enabled && AlignmentSystem.GetTitleDisplay(this).HasFlag(TitleDisplay.Overhead) && !hasFactionTitle)
                {
                    AlignmentType alignment = AlignmentSystem.Find(this);

                    if (alignment != AlignmentType.None)
                    {
                        string text = string.Format("({0}, {1})", AlignmentSystem.GetRankTitle(this), AlignmentSystem.GetName(alignment));

                        AlignmentType observerAlignment = AlignmentSystem.Find(from);

                        int hue;

                        if (observerAlignment == alignment)
                            hue = 98;
                        else if (observerAlignment != AlignmentType.None)
                            hue = 38;
                        else
                            hue = SpeechHue;

                        PrivateOverheadMessage(MessageType.Label, hue, true, text, from.NetState);
                    }
                }
                #endregion

                base.OnSingleClick(from);
            }
        }

        #region Reverse Turing Test for AFK-checking
        private int m_RTTFailures = 0;
        private DateTime m_RTTNextTest = DateTime.MinValue;
        private double m_MinutesUntilNextTest = 5.0;
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public int RTTFailures
        {
            get
            {
                return m_RTTFailures;
            }
            set
            {
                m_RTTFailures = value;
            }
        }
        public int RTTFailureLimit
        {
            get
            {
                if (Core.Debug || TestCenter.Enabled)
                    return 2;
                else
                    return 10;
            }
        }
        public void RTTResult(bool passed)
        {
            if (passed)
            {
                RTTFailures = 0;
                if (m_MinutesUntilNextTest < 40.0)
                {   // Randomize to reduce predictability.
                    m_MinutesUntilNextTest *= 2;
                    m_MinutesUntilNextTest += Utility.RandomList(-3, -2, -1, 1, 2, 3);
                }
            }
            else
            {   // Randomize to reduce predictability.
                m_MinutesUntilNextTest = 5.0;
                m_MinutesUntilNextTest += Utility.RandomList(0, 1, 2, 3);
            }

            //hard check to ensure max of 40.0
            if (m_MinutesUntilNextTest >= 40.0)
            {   // Randomize to reduce predictability.
                m_MinutesUntilNextTest = 40.0;
                m_MinutesUntilNextTest += Utility.RandomList(-13, -12, -11, 11, 12, 13);
            }

            if (passed)
            {
                m_RTTNextTest = DateTime.UtcNow.AddMinutes(m_MinutesUntilNextTest);
            }
        }
        public bool RTT(string notification)
        {
            return RTT(notification, false);
        }
        public bool RTT(string notification, bool bForced)
        {
            return RTT(notification, bForced, 0, "");
        }
        public bool RTT(string notification, bool bForced, int mode, string strSkillName)
        {
            bool bDoTest = false;

            if (m_RTTNextTest == DateTime.MinValue)
            {
                m_RTTNextTest = DateTime.UtcNow.AddMinutes(5.0);
            }
            else if (DateTime.UtcNow > m_RTTNextTest && !IsInCombat())
            {
                m_RTTNextTest = DateTime.UtcNow.AddMinutes(5.0);
                bDoTest = true;
            }

            bool bReturn = (RTTFailures == 0);

            //Safety-hit to make sure the counter is reset with failures
            if (RTTFailures > 1) m_MinutesUntilNextTest = 5.0;

            if (RTTFailures > RTTFailureLimit)  // 10 for production, 2 in DEBUG mode
            {
                try
                {
                    //10+ failures in a row, assume we've got an AFK macroer - auto [macroer him!
                    PJUM.MacroerCommand.ReportAsMacroer(World.GetSystemAcct(), this);
                }
                catch (Exception exc)
                {
                    Server.Diagnostics.LogHelper.LogException(exc);
                }
            }

            if (bForced)
            {
                bDoTest = true;
                m_RTTNextTest = DateTime.UtcNow.AddMinutes(m_MinutesUntilNextTest);
            }

            if (bDoTest)
            {
                switch (mode)
                {
                    case 2:
                        this.SendGump(new RTT.SmallPagedRTTGump(this, notification, strSkillName));
                        break;
                    default:
                        this.SendGump(new RTT.RTTGump(this, notification, strSkillName));
                        break;
                }
                RTTFailures++;
            }

            return bReturn;
        }

        #endregion

        #region throwing timing - initially only for explosion potions

        private DateTime m_nextThrowTime = DateTime.MinValue;

        public bool CanThrowNow()
        {
            if (DateTime.UtcNow > m_nextThrowTime)
            {
                return true;
            }
            return false;
        }

        public void SetThrowDelay(double delay)
        {
            if (DateTime.UtcNow.AddSeconds(delay) > m_nextThrowTime)
            {
                m_nextThrowTime = DateTime.UtcNow.AddSeconds(delay);
            }
        }

        #endregion

        public override void OnRegionChange(Region oldR, Region newR)
        {
            base.OnRegionChange(oldR, newR);

            #region Dueling
            bool oldSafezone = (oldR != null && oldR.IsPartOf(typeof(Engines.ConPVP.SafeZone)));
            bool newSafezone = (newR != null && newR.IsPartOf(typeof(Engines.ConPVP.SafeZone)));

            if (oldSafezone && !newSafezone)
                SendMessage("You have left a dueling safezone. Combat is now unrestricted.");
            else if (!oldSafezone && newSafezone)
                SendMessage("You have entered a dueling safezone. No combat other than duels are allowed in this zone.");
            #endregion
        }
        #region Commands

        #region CheckSkill Command
        public static void CheckTargetSkill(CommandEventArgs e)
        {
            PlayerMobile from = e.Mobile as PlayerMobile;
            double minskill;
            double maxskill;
            if (!Core.Debug)
            { from.SendMessage("You cannot CheckSkill on this shard."); return; }
            if (string.IsNullOrEmpty(e.ArgString) || e.Arguments.Length != 3) { CheckSkillUsage(from); return; }

            SkillName index;
            try
            {
                int score;
                string real_name = IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), e.Arguments[0], out score);
                if (score > 5) // a 'distance' of > 5 is likely bad input (5 is a heuristically derived value)
                {
                    CheckSkillUsage(from);
                    return;
                }
                index = (SkillName)Enum.Parse(typeof(SkillName), real_name, true);
            }
            catch
            {

                CheckSkillUsage(from);
                return;
            }

            try
            {
                minskill = double.Parse(e.Arguments[1]);
                maxskill = double.Parse(e.Arguments[2]);
            }
            catch
            {

                CheckSkillUsage(from);
                return;
            }

            Skill skill = from.Skills[index];

            if (skill != null)
            {
                e.Mobile.SendMessage("Target object to gain on.");
                e.Mobile.Target = new ObjectTarget(skill.SkillName, minskill, maxskill);
            }
            else
            {
                CheckSkillUsage(from);
            }
        }
        public class ObjectTarget : Target
        {
            SkillName m_skillName;
            double m_minSkill;
            double m_maxSkill;
            public ObjectTarget(SkillName skillName, double minSkill, double maxSkill)
                : base(17, true, TargetFlags.None)
            {
                m_skillName = skillName;
                m_minSkill = minSkill;
                m_maxSkill = maxSkill;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                from.CheckTargetSkill(m_skillName, target, m_minSkill, m_maxSkill, new object[2] /*contextObj*/);
                from.SendMessage("CheckTargetSkill for {0}.", m_skillName);
            }
        }
        public static void CheckSkill(CommandEventArgs e)
        {
            PlayerMobile from = e.Mobile as PlayerMobile;
            double minskill;
            double maxskill;
            if (!Core.Debug)
            { from.SendMessage("You cannot CheckSkill on this shard."); return; }
            if (string.IsNullOrEmpty(e.ArgString) || e.Arguments.Length != 3) { CheckSkillUsage(from); return; }

            SkillName index;
            try
            {
                int score;
                string real_name = IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), e.Arguments[0], out score);
                if (score > 5) // a 'distance' of > 5 is likely bad input (5 is a heuristically derived value)
                {
                    CheckSkillUsage(from);
                    return;
                }
                index = (SkillName)Enum.Parse(typeof(SkillName), real_name, true);
            }
            catch
            {

                CheckSkillUsage(from);
                return;
            }

            try
            {
                minskill = double.Parse(e.Arguments[1]);
                maxskill = double.Parse(e.Arguments[2]);
            }
            catch
            {

                CheckSkillUsage(from);
                return;
            }

            Skill skill = from.Skills[index];

            if (skill != null)
            {
                from.CheckSkill(skill.SkillName, minskill, maxskill, contextObj: new object[2]);
                from.SendMessage("CheckSkill for {0}.", skill.SkillName);
            }
            else
            {
                CheckSkillUsage(from);
            }
        }
        private static void CheckSkillUsage(Mobile from)
        {
            from.SendMessage("You have specified an invalid skill to track.");
            from.SendMessage("Usage [CheckSkill <skill to bump> minskill maxskill.");
            return;
        }
        #endregion

        #endregion Commands

        #region Hot Items (Stealing)

        public override bool OnDroppedItemInto(Item item, Container container, Point3D loc)
        {
            BankBox bankBox = FindBankNoCreate();

            if (bankBox != null && container.IsChildOf(bankBox) && SkillHandlers.Stealing.IsHotItem(item, this))
            {
                SendMessage("You cannot place recently stolen items in your bank box.");
                return false;
            }

            return base.OnDroppedItemInto(item, container, loc);
        }

        #endregion

        #region Commands
        private class HideHiddenCommand : BaseCommand
        {
            public HideHiddenCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllNPCs;
                Commands = new string[] { "HideHidden" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "HideHidden";
                Description = "Toggles staff's ability to see hidden objects.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is PlayerMobile target && target.AccessLevel > AccessLevel.Player)
                {
                    bool visible;
                    target.SetMobileBool(MobileBoolTable.HideHidden, !(visible = target.GetMobileBool(MobileBoolTable.HideHidden)));
                    target.SendMessage("You {0} see hidden objects", !visible ? "cannot" : "can now");
                    if (visible)
                        target.SendEverything();
                    else
                        target.HideHidden();
                }
                else
                    LogFailure("That is not a staff member.");
            }
        }
        #endregion Commands
    }
    public class MovementReqCapture
    {
        private static bool m_Capturing = false;
        private static Dictionary<PlayerMobile, MemoryStream> m_Table = null;
        private static DateTime m_Started = DateTime.MinValue;
        private static int m_Count = 0;

        public static void Initialize()
        {
            CommandSystem.Register("beginmrcapture", AccessLevel.Administrator, new CommandEventHandler(BeginMRCapture));
            CommandSystem.Register("stopmrcapture", AccessLevel.Administrator, new CommandEventHandler(StopMRCapture));
            CommandSystem.Register("mrcapturestatus", AccessLevel.GameMaster, new CommandEventHandler(MRCaptureStatus));
        }

        public static void BeginMRCapture(CommandEventArgs e)
        {
            m_Capturing = true;
            m_Table = new Dictionary<PlayerMobile, MemoryStream>();
            m_Started = DateTime.UtcNow;
            m_Count = 0;

            Packet p = new AsciiMessage(Serial.MinusOne, -1, MessageType.Regular, 0, 3, "System", "Beginning MovementReq capture. Halting after 30 minutes or 1,000,000 hits.");
            foreach (NetState n in NetState.Instances)
            {
                if (n.Mobile != null && n.Mobile.AccessLevel >= AccessLevel.GameMaster)
                    n.Send(p);
            }
        }

        // Adam: may be called with CommandEventArgs == null
        public static void StopMRCapture(CommandEventArgs e)
        {
            try
            {
                using (FileStream fs = new FileStream("MRCapture.dat", FileMode.Create, FileAccess.Write))
                {
                    using (BinaryWriter writer = new BinaryWriter(fs))
                    {
                        foreach (PlayerMobile pm in m_Table.Keys)
                        {
                            writer.Write((string)pm.Name);
                            writer.Write((int)pm.Serial);
                            writer.Write((string)(((Account)pm.Account).Username));
                            writer.Write((long)m_Table[pm].Length);
                            writer.Write((byte[])m_Table[pm].ToArray());
                        }

                        writer.Close();
                    }

                    fs.Close();
                }

                if (e != null && e.Mobile != null)
                    e.Mobile.SendMessage("Capture data written to MRCapture.dat in the main directory.");
            }
            catch (Exception ex)
            {
                if (e != null && e.Mobile != null)
                    e.Mobile.SendMessage(ex.Message);
            }

            m_Capturing = false;
            m_Table = null;
            m_Started = DateTime.MinValue;
            m_Count = 0;

            Packet p = new AsciiMessage(Serial.MinusOne, -1, MessageType.Regular, 0, 3, "System", "Ended MovementReq capture.");
            foreach (NetState n in NetState.Instances)
            {
                if (n.Mobile != null && n.Mobile.AccessLevel >= AccessLevel.GameMaster)
                    n.Send(p);
            }
        }

        public static void MRCaptureStatus(CommandEventArgs e)
        {
            if (m_Capturing)
                e.Mobile.SendMessage("MR Capture has been running for {0} minutes, with {1} hits.", (DateTime.UtcNow - m_Started).Minutes, m_Count);
            else
                e.Mobile.SendMessage("MR Capture not running.");
        }

        public static void HitMR(PlayerMobile pm)
        {
            if (!m_Capturing)
                return;

            if (m_Started + TimeSpan.FromMinutes(30.0) <= DateTime.UtcNow)
            {
                StopMRCapture(null);
                return;
            }

            if (!m_Table.ContainsKey(pm))
                m_Table.Add(pm, new MemoryStream());

            m_Table[pm].Write(BitConverter.GetBytes(DateTime.UtcNow.ToBinary()), 0, 8);
            m_Count++;
        }
    }
}