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

/* Scripts/Multis/BaseHouse.cs
 * ChangeLog
 *  3/14/2024, Adam (Pack Up House)
 *      For BaseAddon, we need to stuff the addon hue into the deed. This will be reflected when the addon is instantiated.
 *  11/26/2023, Adam (Instances)
 *      1. Add Instances for BaseHouse. This dictionary also us a bool 'owned by player' as the Value
 *      2. Cap DecayMinutesStored to MaxHouseDecayTime.TotalMinutes
 *  11/2/2023, Adam (OnAfterDelete)
 *      For Siege, we now redeed all redeedable addons.
 *  9/2/2023, Adam (IsFriend)
 *      Add optional parameter IsFriend(from, checkInside: true)
 *      My suspicion is that most calls should be setting this true. 
 *  8/15/2023, Adam (OnSteps)
 *      Disallow placing strong boxes or trash barrels on steps
 *  8/14/2023, Adam
 *      Add Scales to the list of locked down objects that may be accessed by Friends
 *      Normalize lock-down mechanism for SeedBox and Library: 
 *          SeedBox box(1) + seeds / 5
 *          Library library(1) + books / 2
 *  8/2/2023, Adam
 *      Add Spyglass and MapItem to the list of locked down objects that may be accessed by Friends
 *  7/27/2023, Adam (AddEastDoor)
 *  In AddEastDoor, allow the facing to be specified. 
 *      This is needed for towers so that 2nd and 3rd floor doors are situated correctly.
 *  7/8/2023, Adam (CheckSecureAccess)
 *      Add a case for when the access to a VendorRecoveryBox
 *  6/25/2023, Adam (Find())
 *      BaseHouse.Find was simply calling BaseBulti.Find which would return any BaseMulti and not necessarily a 'house'.
 *      Add BaseHouse.Find() such that:
 *      1. It finds a 'basehouse'
 *      2. the BaseHouse is in a HouseRegion
 *  6/24/2023, Adam (OnOpened)
 *      Siege: Add an OnOpened callback when a house door is opened. We then determine if it is an exterior door,
 *      if a murderer or flagged criminal opens the door to a house in which a blue player has banned players/monsters, the ban list for the house will be cleared.
 *      https://uo.stratics.com/content/basics/siege_archive.shtml
 *  4/19/23, Yoar
 *      Can no longer eject player barkeepers from the house
 *  8/30/22, (StashHouse)
 *      Bug fix having to do with things not being on the correct map at the time of stash/recovery.
 *      general code cleanup
 *  8/22/22, Adam
 *      Split up setting IsIntMapStorage = true and the actual move.
 *      This is because moving somethings will move other things with them and our check for not moving things twice 
 *          would prevent IsIntMapStorage from being set.
 *      Example: you move the House first, and the sign goes with it, but the sign has not been marked as IsIntMapStorage
 *  8/16/22, Adam (CalcAccountCode())
 *      -Rewrite CalcAccountCode()
 *      -Make GetHousesInheritance() more robust
 *      The old CalcAccountCode() was only a hash of the owner's account name. This is weak. Should we ever delete the original account, another
 *      player account with the same account name could claim the house.
 *      Now: CalcAccountCode() uses not only owner's account name, but also the creation date of the house.
 *      Additionally, GetHousesInheritance() now checks back with the players account to check the 'house recorded' as an Inheritance.
 *      If all these checks pass, the house is inherited.
 *  8/15/22, Adam (FindAllItems())
 *      Add FindAllItems() to locate all items within the house.
 *  8/10/22, Adam
 *      Add StashHouse() to stash a house and all fixtures on the internal map.
 *  8/1/22, Yoar
 *      Cleanups related to Mortalis house inheritance.
 *  6/10/22, Yoar
 *      - Added 'HashSet<Item> GetFixedItems()' returns a set of all fixed house items. These include doors,
 *        addons, lockdowns, secures. HouseFoundation additionally returns the sign haner, sign post and all
 *        fixtures the HouseFoundation's fixtures list.
 *      - Rewrote OnLocationChange again. This time, we make absolutely sure we move *all* house fixtures
 *        *only once*. Here, we use 'GetFixedItems()' to get the house's fixed items.
 *  5/31/22, Yoar
 *      - OnMapChange handler: Added Item.Deleted checks + included secures.
 *      - OnLocationChange fix: My previous OnLocationChange code did not work for houses with Map == null.
 *        This occurs when houses are being constructed.
 *        This caused fixtures/doors ended up in the top-left corner of whichever map the house was placed on.
 *        Solution: Added two methods:
 *        1. MoveContents: Explicitly offsets all fixtures to the house's new location (old behavior).
 *        2. MoveContents_Sandbox: Dynamically offsets all items/mobiles in the house to the house's new location (new behavior).
 *        The logic in OnLocationChange is changed accordingly:
 *        o When the house's map is null, the method 'MoveContents' is called (old behavior).
 *        o Otherwise, 'MoveContents_Sandbox' is called (new behavior).
 *  5/9/22, Yoar
 *      Rewrote OnLocationChange handler. It now handles *all* items/mobiles.
 *  1/29/22, Yoar
 *      For the overhead labels locked down/secured/no longer locked down labels:
 *      Changed calls from PublicOverheadMessage to SendMessageTo.
 *  1/18/22, Adam
 *      Add MusicBox to the list of containers excluded from the lockbox limit
 *  12/7/21, Yoar
 *      Added "(locked down)" and "(secured)" labels.
 *	11/14/21, Yoar
 *      Renamed board game related BaseBoard to BaseGameBoard.
 *  11/6/21, Adam (Deserialize)
 *      Check for member == null before adding to the database
 *	11/5/21, Yoar
 *      1. Disabled storage tax credits. Additional lockboxes no longer require upkeep in the form of tax credits.
 *      2. Removed the cap of additional lockboxes. You can now purchase as many +1 lockbox deeds for your house as you wish.
 *	11/5/21, Yoar
 *	    Redid the data structure of lockboxes in BaseHouse. Instead of storing:
 *	    1. MaxLockboxes    : what is our current lockbox cap, including upgrades?
 *	    2. LockboxLimitMin : what is our base lockbox cap?
 *	    3. LockboxLimitMax : what is our fully upgraded lockbox cap?
 *	    we now store
 *	    1. MaxLockboxes    : what is our base lockbox cap?
 *	    2. BonusLockboxes  : how many *bonus* lockboxes can we place beyond the base lockbox cap?
 *	10/31/21, Yoar
 *	    Lockbox system cleanup. Added two switches:
 *      1. BaseHouse.LockboxSystem: enables/disables the lockbox system
 *      2. BaseHouse.TaxCreditSystem: enables/disables the tax credit system
 *  10/25/21, Adam (CheckAccessibility)
 *      Give Members access to forges for smelting ore.
 *      interestingly, smelting ore works differently than smelting an item. Smelting ore requires an Accessibility Check 
 *      where smelting only requires you are near and anvil and forge. 
 *  10/17/21, Adam (Members-Only, e.g, Mining Cooperative)
 *      Add support for Members-Only housing.
 *      Membership is similar to Friends and Coowners, but does not grant any of the privileges of those other classes.
 *      Membership simply states that you can enter the house and use the facilities. Non-members cannot enter at all.
 *      Members also get a virtualized strongbox (25 items, no weight limit.) It is virtualized since a house can have any 
 *      number of members, and strongbox management would otherwise not scale. Membership is currently permanent, but that could change.
 *  8/25/21 Adam, (ProximityToDamage & TargetExploitCheck)
 *      ProximityToDamage we use this function to cap the amount of damage EarthQuake can do in a house. Until this was added, 
 *      earth quake would hit about 5 tiles into a house, which means in a 7x7, you have no safe spot to hide. 
 *      I remember on OSI, EQ would only reach 2 tiles into the house, that's what this function provides.
 *      ProximityToDamage is interesting because it takes into account that both victim and the caster can own two walls
 *      at the same time. This happes when one or both are in/on a corner of the property.
 *      TargetExploitCheck: Razor and other macro programs allow you to record targets in a target queue. So you can basically 
 *      save a valid target, then run to another location and cast a spell, then reuse that once valid target.
 *      The probelm is this is exploited in house killing. Basucally no spot is safe against Meteor Swarm / Chain Lightning.
 *      This function applies another LOC check at the time the spell is applied (late binding) and rejects the damage of LOS fails.
 *  8/17/21, Adam (CheckAccessibility() / VendorRentalContract)
 *      I got a bug report that VendorRentalContracts were not working. I found that VendorRentalContract was missing from all the special
 *      cases handled in CheckAccessibility() with no comment.
 *      In any case, I added this back as this is the correct place for this check.
 *  6/28/21, Adam
 *		Reinstitute annexation and add a notion of a short waiting period for the house being placed to be demolished
 *			if Core.UOBETA is set to allow testing of this system
 *	3/17/16, Adam
 *		Add the special tent logic to IsInside() - (see those comments 3/16/16, 2/27/10.)
 *		This is called directly from placing a vendorTrntalContract in a tent.
 *		It's probably called from a number of other places.
 *	3/16/16, Adam
 *		FindHouseAt() - Put back the change I put in on 2/27/10 (see those comments.)
 *		Why was it reverted? 
 *		Anyway, since tents have mcl lists widths and heights of 0, you cannot use the traditional IsInside logic.
 *	2/15/11, adam
 *		Fixup AccountCode for version < 21
 *	2/14/11, Adam
 *		UOMO: Add Inheritance mechanism that allows a new character on an account to Inherit the house previously owned 
 *			on that account
 *	11/15/10, Adam
 *		Stop announcing IDOCs on TC for Siege
 *	2/28/10, Adam
 *		Prevent the owner from Demolishing or Transfering a house of the ManagedDemolishion flag is set on the house.
 *		The ManagedDemolishion flag is set on the house when this house annexed one of more tents. 
 *	2/27/10, adam
 *		WRT FindHouseAt(): the width and height of MultiComponentList Components for tents are all 0, so we can't use the IsInside() check
 *		when checking to see of the point (item or mobile) inside or outside the tent, we there PASS the test as TRUE.
 *		This change came about while modifying HousePlacement to allow houses to annex tents.
 *	9/29/08, Adam
 *		Add location to IDOC listing
 *	7/20/08, Pix
 *		De-coupled township stones from houses.
 *	5/11/08, Adam
 *		Performance Conversion: Regions now use HashTables instead of ArrayLists
 *	4/1/08, Adam
 *		Add a new 32 bit m_NPCData variable to hold the MaximumBarkeepCount in byte 4
 *		The other 3 bytes are available for other NPC data
 *  12/28/07 Taran Kain
 *      Added Addons to the list of things adjusted in OnLocationChange, OnMapChange
 *      Clarified variable names in OnLocationChange
 *  12/17/07, Adam
 *      Add the new [GetDeed command to get a house deed from a house sign
 *      (useful for the new static houses)
 *	11/29/07, Adam
 *		Limit IDOC email warnings to test center
 *	11/29/07, Adam
 *		Add an email warning to owners of houses which are about to go IDOC (2 day warning)
 *	9/2/07, Adam
 *		Add a auto-resume-decay system so that we can feeze for a set amount of time.
 *	8/27/07, Adam
 *		don't list staff owned houses to the console because it lags the game (probably) because console IO is so slow.
 *		(not a problem until we open the preview neighborhood)
 *	8/26/07, Adam
 *		Remove TC only code from CheckLockboxDecay that was preventing decay while we recovered 
 *		lost lockboxes.
 *	8/23/07, Adam
 *		Make SetLockdown() public so that [Nuke may access it to release containers
 *	7/29/07, Adam
 *		Change the Property method name from m_DecayTime to StructureDecayTime
 *		Update LockBoxCount property to recalc the true LockBoxCount
 *			In Serialize, use the LockBoxCount property to make sure we write the correct number
 *	7/27/07, Adam
 *		- Add SuppressRegion property to turn on region suppression 
 *		- Added 32bit flags (move all bools here)
 *  6/12/07, Adam
 *      Added AdminTransfer() to allow unilateral annexation of a house.
 *      See Also: [TakeOwnership
 *  06/08/07, plasma
 *      Commented redundant HouseFoundation check in IsInside()
 *  6/7/07, Adam
 *      When serializing containers, cancel the Freeze Timer after setting IsSecure = true, IsLockeddown = true
 *  5/19/07, Adam
 *      Add support for lockable private houses
 *  5/8/07, Adam
 *      Updates to make use of data packing functions. e.g. Utility.GetUIntRight16()
 *  5/7/07, Adam
 *      Fix public houses so that lockboxes are accessable by anyone
 *  5/6/07, Adam
 *      - Add support for purchasable lockboxes
 *      - Move house Decay timer logic from Heartbeat
 *      - allow public houses to have lockboxes
 *  4/4/07, Adam
 *      Add CleanHouse() on IDOC to remove gold, resources, stackables, etc..
 *      Add an exception for necro regs.
 *  3/6/07, Adam
 *      Add public RemoveDoor() method so we can 'chop' doors to destroy them.
 *  01/07/07, Kit
 *      Reverted change!
 *      Added call to fakecontainers ManageLockDowns() in house deserialization, 
 *      for reseting of a items IslockedDown status.
 *  12/29/06, Kit
 *      Changed Decay state to virtual.
 *  12/18/06, Kit
 *      Added access rights to librarys
 *  12/11/06, Kit
 *      Added in support for Library archives
 *  11/23/06, Rhiannon
 *      Removed code that supposedly banned all characters on the account of a player whose character was banned from a house.
 *	10/16/06, Adam
 *		Add global override for SecurePremises
 *			i.e., CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SecurePremises)
 *	9/19/06, Pix
 *		Added check to make sure the container isn't already secured or locked down when trying to secure a container.
 *	8/7/06, Adam
 *		Add a public method to allow us to enum the multis
 *	8/25/06, Adam
 *		Undo change of 8/14/06(weaver), and comment out the *other* debug code.
 *	8/23/06, Pix
 *		Added check for sign == null in CheckDecay()
 *	8/22/06, Pix
 *		Excluded announcements of IDOC tents.
 *	8/19/06, weaver
 *		Removed console debug message re: decayability of houses checked.
 *	8/14/06, weaver
 *		Modified lockdown check to treat containers as single items.
 *	8/14/06, Pix
 *		Added more safety-checking to CheckAllHouseDecay.  Now doesn't assume there's an owner or a sign.
 *	8/03/06, weaver
 *		Removed Contains() check from FindPlayerVendor() as it was pointless (region already verified)
 *		and messing up tents.
 *	7/12/06, weaver
 *		Virtualized Refresh() and RefreshHouseOneDay() to allow overridable 
 *		Added public accessor for town crier IDOC thing decay handling.
 *	7/6/06, Adam
 *		- Change default of m_SecurePremises to false from true;
 *			We will leave the flag for GM access in case of another exploit
 *		- Add a set SetSecurity() function that will change the state of m_SecurePremises on all houses
 *	5/02/06, weaver
 *		Added GetAccountHouses to retrieve all houses on passed mobile's account.
 *	3/21/06, weaver
 *		Added check for trash barrels to exclude them from lockbox count.
 *	2/21/06, Pix
 *		Added SecurePremises flag.
 *	2/20/06, Adam
 *		Add method FindPlayer() to find players in a house.
 *		We no longer allow a house to be deeded when players are inside (on roof) 
 *		This was used as an exploit.
 *	2/11/06, Adam
 *		Clear the Town Crier in OnDelete()
 *  1/1/06, Adam
 *		Swap HouseCheck for LastHouseCheckMinutes in the IDOC announce logic
 *		I also rewrote LastHouseCheck in Heartbeat.cs to do the right thing
 *	12/27/05, Adam
 *		Add IDOC logger
 *	12/15/05, Adam
 *		Reverse the change of 6/24/04, (Pix)
 *		This code no longer applies as you cannot lock down containers
 *		that are not 'deco' in a public building. Furthermore, you cannot lock down
 *		items inside of a container.
 *  12/11/05, Kit
 *		Added EternalEmbers to accessability list.
 *	11/25/05, erlein
 *		Added function + calls from targets to handle command logging.
 *  10/30/05 Taran Kain
 *		Fixed minor bug with securing containers near the lockdown limit
 *	09/3/05, Adam
 *		a. Reformat console output for never decaying houses
 *		b. add new GlobalNeverDecay() function - World crisis mode :\
 *			(controlled from Core Management Console)
 *  08/28/05, Adam
 *		minor tweak in IDOC announcement: 
 *		Change "at" to "near" for sextant coordinates
 *  08/27/05, Taran Kain
 *		Changed IDOC announcement to vaguely describe location in sextant coordinates.
 *  08/25/05, Taran Kain
 *		Added IDOC Announcement system.
 *	8/16/05, Pix
 *		Fixed house refresh problem.
 *	8/4/05, Pix
 *		Change to house decay.
 *	7/30/05, Pix
 *		Outputs message to console to log never-decay houses.
 *	7/29/05, Pix
 *		Now DecayState() returns "This structure is in perfect condition." if house is set to not decay.
 *	7/05/05, Pix
 *		Now NeverDecay maintains the stored time for a house instead of setting it at HouseDecayDelay
 *	6/11/05, Pix
 *		Added SetBanLocation because teh BanLocation property doesn't function like I need.
 *	06/04/05, Pix
 *		Upped Friends List Max to 150 (from 50).
 *	05/06/05, Kit
 *		Added support for SeedBoxs.
 *	04/20/05, Pix
 *		Increased the max timed storable to 3X (90 days)
 *	02/23/05, Pixie
 *		Made other characters on same account count as owners of the house.
 *  02/15/05, Pixie
 *		CHANGED FOR RUNUO 1.0.0 MERGE.
 *	11/09/04, Pixie
 *		Made it so you can't lock down items inside containers (to curb exploit).
 *	10/23/04, Darva
 *			Checked if house is public before doing ChangeLocks on transfer.
 *	9/24/04, Adam
 *		Make checks against BaseContainer now that we've pushed the deco support all the way down
 *		(Decorative containers)
 *	9/21/04, Adam
 *		Create mechanics for Decorative containers that do not count against lockboxes
 *			1. Add IsExceptionContainer(): Add all exceptions to this function
 *			2. SetLockdown( ... ) now checks against IsExceptionContainer() when calculating lockboxes counts
 *			3. LockDown( ... ) now checks against IsExceptionContainer() when deciding whether to lockdown
 *			4. Add HouseDecoTarget : Target. This mechanism is invoked from one of the commands to in HouseRegion
 *			to make a container decorative or not.
 *			5. make sure the container is not locked down before changing it's state
 *		See Also: HouseRegion.cs and various containers
 *	9/19/04, Adam
 *		Revert the privious change due to a bug.
 *	9/19/04, mith
 *		 SetLockdown(): Added functioanlity for Decorative Containers
 *	9/16/04, Pix
 *		Changed so House Decay uses the Heartbeat system.
 *	7/14/04, mith
 *		Ban(): added check to see if target is BaseGuard (in with the AccessLevel checking)
 *	7/4/04, Adam
 *		CheckAccessibility
 *			Change: Keys are FriendAccess, KeyRings are CoOwnerAccess
 *			Change: Containers in a Public House are AnyoneAccess
 *		CheckAccessible
 *			Comment out the code that provides locked-down containers CoOwnerAccess.
 *			Now that lockboxes are accessible by 'anyone', this would seem inappropriate.
 *	6/24/04, Pix
 *		KeyRing change - now they don't count as lockboxes.
 *		Lockbox contents decay fix.
 *	6/14/04, Pix
 *		Changes for House decay
 *	6/12/04, mith
 *		Modified roles of friends/co-owners with respect locking down items.
 *		Clear Access, Bans, Friends, and CoOwners arrays OnAfterDelete().
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/30/04, mith
 *		Modified banning rules so that Aggressors/Criminals can not ban from their house.
 *		Set Public() property to always be true; no private housing allowed.
 */

using Server.Accounting;				// emailer
using Server.Commands;
using Server.ContextMenus;
using Server.Diagnostics;
using Server.Engines.Plants;
using Server.Gumps;
using Server.Items;
using Server.Misc;						// TestCenter
using Server.Mobiles;
using Server.Multis.Deeds;
using Server.Multis.StaticHousing;
using Server.Network;
using Server.Regions;
using Server.SMTP;								// core SMTP engine
using Server.Targeting;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Server.Multis.StaticHousing.StaticHouseHelper;

namespace Server.Multis
{
    public abstract class BaseHouse : BaseMulti
    {
        public static bool NewVendorSystem { get { return Core.RuleSets.AOSRules(); } } // Is new player vendor system enabled?
        public static Dictionary<BaseHouse, bool> Instances = new();

        #region BoolTable
        [Flags]
        public enum BaseHouseBoolTable
        {
            None = 0x00000000,
            SuppressRegion = 0x00000001,
            ManagedDemolition = 0x00000002,
            IsPackedUp = 0x00000004,
        }
        private BaseHouseBoolTable m_BoolTable;

        public void SetBaseHouseBool(BaseHouseBoolTable flag, bool value)
        {
            if (value)
                m_BoolTable |= flag;
            else
                m_BoolTable &= ~flag;
        }

        public bool GetBaseHouseBool(BaseHouseBoolTable flag)
        {
            return ((m_BoolTable & flag) != 0);
        }
        #endregion BoolTable

        #region GetDeed

        public static void Initialize()
        {
            Server.CommandSystem.Register("GetDeed", AccessLevel.Administrator, new CommandEventHandler(OnGetDeed));
        }

        [Usage("GetDeed")]
        [Description("Gets the deed for this house.")]
        private static void OnGetDeed(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Get a deed for which house?.");
            e.Mobile.SendMessage("Please target the house sign.");
            e.Mobile.Target = new GetDeedTarget();
        }

        public class GetDeedTarget : Target
        {
            public GetDeedTarget()
                : base(8, false, TargetFlags.None)
            {

            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is HouseSign && (target as HouseSign).Structure != null)
                {
                    HouseSign sign = target as HouseSign;
                    /*
					if (sign.Owner is StaticHouse == false)
					{
						from.SendMessage("You may only wipe regions on a StaticHouse.");
						return;
					}*/

                    BaseHouse bh = sign.Structure as BaseHouse;
                    if (bh != null)
                    {
                        HouseDeed hd = bh.GetDeed();
                        if (hd == null)
                        {
                            from.SendMessage("There is no deed for this house.");
                            return;
                        }
                        from.AddToBackpack(hd);
                        from.SendMessage("The deed has been added to your backpack.");
                        return;
                    }
                    from.SendMessage("Error getting house deed.");
                }
                else
                {
                    from.SendMessage("That is not a house sign.");
                }
            }
        }
        #endregion GetDeed

        #region MyDistanceToPerimeter
        protected static Dictionary<Direction, int> CloestWalls(Rectangle2D rect, Mobile m)
        {
            Point3D target = m.Location;
            // debug
            int west = target.X - rect.X;                   //Console.WriteLine("how far {1} is from the west wall(x): {0}", west, m.Name);
            int north = target.Y - rect.Y;                  //Console.WriteLine("how far {1} is from the north wall (y): {0}", north, m.Name);
            int south = rect.Y + rect.Height - target.Y;    //Console.WriteLine("how far {1} is from the south wall(y): {0}", south, m.Name);
            int east = rect.X + rect.Width - target.X;      //Console.WriteLine("how far {1} is from the east wall(x): {0}", east, m.Name);

            Dictionary<Direction, int> dic = new Dictionary<Direction, int>();

            // add all our distance-to-walls here
            dic.Add(Direction.North, north);
            dic.Add(Direction.South, south);
            dic.Add(Direction.East, east);
            dic.Add(Direction.West, west);

            // our output table
            Dictionary<Direction, int> output = new Dictionary<Direction, int>();

            // sort shortest distance to highest
            foreach (KeyValuePair<Direction, int> direction in dic.OrderBy(key => key.Value))
            {
                // always add the first one
                if (output.Count == 0)
                {
                    output.Add(direction.Key, direction.Value);
                    continue;
                }

                // only save the two best
                if (output.Count == 2)
                    break;

                // only save if it is the same as the first best (same distance)
                //  this happens when the caster is right at the corner, then they own two directions
                if (output.ContainsValue(direction.Value))
                    output.Add(direction.Key, direction.Value);
            }

            return output;
        }
        public static int ProximityToDamage(Mobile caster, Mobile m)
        {   // always return zero if not in a house
            BaseHouse hm = FindHouseAt(m);
            if (hm == null)
                return 0;

            Point2D p = new Point2D(m.Location);
            foreach (Rectangle3D rect3D in hm.Region.Coords)
            {
                if (rect3D.Contains(m.Location))
                {
                    Rectangle2D rect = new Rectangle2D(rect3D.Start, rect3D.End);
                    Dictionary<Direction, int> myWalls, castersWalls;
                    int distance = 100;                                 // no chance of getting hit
                    myWalls = CloestWalls(rect, m);                     // number of walls I am near (1or2)
                    castersWalls = CloestWalls(rect, caster);           // number of walls he is near (1or2)
                                                                        // if he shares a wall with me, I could get hurt.
                    foreach (KeyValuePair<Direction, int> houseWalls in myWalls)
                    {
                        if (castersWalls.ContainsKey(houseWalls.Key))
                            distance = houseWalls.Value;
                    }

                    return distance;
                }
            }
            // should never get here
            return 100;
        }
        #endregion MyDistanceToPerimeter

        #region TargetExploitCheck
        public enum ExploitType
        {
            None,
            LOS,
            Distance
        }
        public static ExploitType TargetExploitCheck(string logName, Mobile caster, Mobile m, Point3D target_loc)
        {
            return TargetExploitCheck(logName, caster, m, target_loc, false);
        }
        public static ExploitType TargetExploitCheck(string logName, Mobile caster, Mobile m, Point3D target_loc, bool jail)
        {   // Note: Don't allow staff to circumvent this test, since using staff accounts is the only good way to test this logic.
            Point3D obj_loc = (m != null) ? m.Location : target_loc;
            Server.Multis.BaseHouse hm = Server.Multis.BaseHouse.FindHouseAt(obj_loc, Map.Felucca, 16);
            if (hm == null)
                return ExploitType.None;

            // our logfile name
            string rootName = logName;
            logName += "Exploit.log";

            Point2D p = new Point2D((m != null) ? m.Location : target_loc);
            foreach (Rectangle3D rect3D in hm.Region.Coords)
            {
                if (rect3D.Contains(obj_loc))
                {
                    Rectangle2D rect = new Rectangle2D(rect3D.Start, rect3D.End);

                    #region logging
                    // TargetExploitCheck of player
                    LogHelper Logger = new LogHelper(logName, false);
                    Point3D target;
                    // DistanceToTarget target of cast
                    target = target_loc;
                    Logger.Log(LogType.Text, string.Format("Attacker {0} distance to target: {1}", caster, caster.GetDistanceToSqrt(target)));
                    if (m != null)
                        Logger.Log(LogType.Text, string.Format("Victim {0} distance to target: {1}", m, m.GetDistanceToSqrt(target)));

                    if (Utility.LineOfSight(caster.Map, caster, target_loc, true))
                        Logger.Log(LogType.Text, string.Format("Caster see: {0}", target_loc));
                    else
                        Logger.Log(LogType.Text, string.Format("Caster cannot see: {0}", target_loc));

                    // record *what* was targeted at this location
                    IPooledEnumerable eable = Map.Felucca.GetObjectsInRange(target_loc, 0);
                    foreach (object obj in eable)
                    {
                        Logger.Log(LogType.Text, string.Format("Targeted: {0}", obj));
                    }
                    eable.Free();

                    Logger.Finish();
                    #endregion logging

                    // must be able to see what you are targeting
                    if (Utility.LineOfSight(caster.Map, caster, target_loc, true) == false)
                    {   // Target can not be seen.

                        if (!jail)
                            caster.SendLocalizedMessage(500237, null, 0x482); // Target can not be seen.
                        else
                        {
                            string crime = string.Format("They somehow circumvented the LOS checks in {0}.", rootName);
                            // exploit! They somehow circumvented the LOS checks. Probably done on the client, and and then fudged packets on the way back to the server.
                            //  We double down on the LOS checks here, server-side
                            Logger = new LogHelper(logName, false);
                            Logger.Log(LogType.Mobile, caster, string.Format("exploit! {0}.", crime));
                            Logger.Finish();
                            // jail time - we need to delay the jailing here since we may be in the middle of a pooled enumerable loop
                            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Tick), new object[] { caster, string.Format("exploit! {0}.", crime) });
                            CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                            CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. {1}", caster as PlayerMobile, crime));
                        }
                        return ExploitType.LOS;
                    }

                    bool problem = false;
                    if (m != null)
                        // 12 is the max range on the spell, 2 is the area effect from the targeted item (like MeteorSwarm/ChainLightning)
                        problem = caster.GetDistanceToSqrt(target) + m.GetDistanceToSqrt(target) > (12 + 2);
                    else
                        // spells like EV and BS don't have an area effect, so we can skip that part of the equation
                        problem = caster.GetDistanceToSqrt(target) > (12);

                    if (problem)
                    {   // That is too far away.
                        if (!jail)
                            caster.SendLocalizedMessage(500446, null, 0x482); // That is too far away.
                        else
                        {
                            string crime = string.Format("They somehow circumvented the range checks in {0}.", rootName);
                            // exploit! They somehow circumvented the range checks. Probably done on the client, and and then fudged packets on the way back to the server.
                            //  We double down on the range checks here, server-side
                            Logger = new LogHelper(logName, false);
                            Logger.Log(LogType.Mobile, caster, string.Format("exploit! {0}.", crime));
                            Logger.Finish();
                            // jail time - we need to delay the jailing here since we may be in the middle of a pooled enumerable loop
                            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Tick), new object[] { caster, string.Format("exploit! {0}.", crime) });
                            CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                            CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. {1}", caster as PlayerMobile, crime));
                        }
                        return ExploitType.Distance;
                    }
                }
            }

            return ExploitType.None;
        }
        private static void Tick(object state)
        {
            object[] aState = (object[])state;
            Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(aState[0] as Mobiles.PlayerMobile, 3, aState[1] as string, false);
            jt.GoToJail();
        }
        #endregion TargetExploitCheck

        public static bool ValidateOwnership(Mobile from)
        {
            BaseHouse bh = null;
            return (bh = BaseHouse.Find(from.Location, from.Map)) != null && bh.Owner == from;
        }
        public static BaseHouse GetHouse(Mobile from)
        {
            BaseHouse bh = null;
            if ((bh = BaseHouse.Find(from.Location, from.Map)) != null)
                return bh;
            else
                return null;
        }
        public static Mobile GetOwnership(Mobile from)
        {
            BaseHouse bh = null;
            if ((bh = BaseHouse.Find(from.Location, from.Map)) != null)
                return bh.Owner;
            else
                return null;
        }

        #region PackUpHouse
        public void PackUpHouse(Mobile from, LogHelper logger)
        {
            if (this is BaseHouse house && !IsPackedUp())
            {
                logger.Log(string.Format("--packing up house for {0}/{1}.--", house, house.Owner));
                logger.Log(LogType.Mobile, from);

                #region Lists
                List<Item> ignore_list = Utility.AllTownshipItems();        // should really be specific to THIS township
                List<MoveEntry> master_list = GetAllPackableObjects();      // overreaches and picks up township items
                #region Cleanup lists

                // remove township items
                List<MoveEntry> remove_list = new();
                foreach (var temp_object in master_list)
                {
                    Item temp = temp_object.Entity as Item;
                    if (temp != null)
                        if (ignore_list.Contains(temp))
                            remove_list.Add(temp_object);

                }
                foreach (var remove in remove_list)
                    master_list.Remove(remove);

                // remove lockdowns
                remove_list = new();
                foreach (var temp_object in master_list)
                {
                    Item temp = temp_object.Entity as Item;
                    if (temp != null)
                        if (IsLockedDown(temp))
                            remove_list.Add(temp_object);
                }
                foreach (var remove in remove_list)
                    master_list.Remove(remove);

                // remove secures
                remove_list = new();
                foreach (var temp_object in master_list)
                {
                    Item temp = temp_object.Entity as Item;
                    if (temp != null)
                        if (IsSecure(temp))
                            remove_list.Add(temp_object);
                }
                foreach (var remove in remove_list)
                    master_list.Remove(remove);

                // remove addons
                remove_list = new();
                foreach (var temp_object in master_list)
                {
                    Item temp = temp_object.Entity as Item;
                    if (temp != null)
                        if (Addons.Contains(temp))
                            remove_list.Add(temp_object);
                }
                foreach (var remove in remove_list)
                    master_list.Remove(remove);

                // remove deleted (exploit)
                remove_list = new();
                foreach (var temp_object in master_list)
                {
                    Item i_temp = temp_object.Entity as Item;
                    if (i_temp != null && i_temp.Deleted)
                        remove_list.Add(temp_object);

                    Mobile m_temp = temp_object.Entity as Mobile;
                    if (m_temp != null && m_temp.Deleted)
                        remove_list.Add(temp_object);
                }
                foreach (var remove in remove_list)
                    master_list.Remove(remove);
                #endregion Cleanup lists

                List<Mobile> NPC_list = new();
                // rented vendor=PlayerVendor, PlayerVendor=Mobile, PlayerBarkeeper=BaseVendor, HouseSitter=BaseVendor
                foreach (var o in master_list)
                    if ((o.Entity is BaseVendor || o.Entity is PlayerVendor) && o.Entity is not ITownshipNPC)
                        if (o.Entity is Mobile td && !td.Deleted)
                            NPC_list.Add(o.Entity as Mobile);

                List<Item> Plant_list = new();
                foreach (var o in master_list)
                    if (o.Entity is PlantItem)
                        if (o.Entity is Item td && !td.Deleted)
                            Plant_list.Add(o.Entity as Item);

                List<Item> Addon_list = new();
                if (house.Addons != null)
                    foreach (var o in house.Addons)
                        if (o is Item td && !td.Deleted)
                            Addon_list.Add(o as Item);
                #endregion Lists

                List<HouseNPCRestorationDeed> NPCDeeds = new();
                #region pack up the NPCs
                {   // pack up the NPCs
                    Dictionary<Mobile, Item> logging = new();
                    if (NPC_list.Count > 0)
                    {
                        foreach (Mobile m in NPC_list)
                        {
                            if (m is not ITownshipNPC)
                            {
                                HouseNPCRestorationDeed deed = new HouseNPCRestorationDeed(house, m);
                                logging.Add(m, deed);
                                m.MoveToIntStorage();
                                NPCDeeds.Add(deed);
                            }
                        }

                        foreach (var unit in logging)
                            logger.Log(string.Format("Packing {0} into deed {1}", unit.Key, unit.Value));
                    }
                }
                #endregion pack up the NPCs

                List<HomeItemRestorationDeed> plantDeeds = new();
                SmallCrate seedBox = new SmallCrate();
                seedBox.Name = "(seeds)";
                #region Plants
                {
                    List<Item> toMove = new();
                    if (Plant_list.Count > 0)
                    {
                        seedBox.MaxItems = Plant_list.Count;  // ensure storage
                        foreach (var item in Plant_list)
                            if (item is PlantItem pi && !ignore_list.Contains(pi))
                            {
                                if (pi.PlantStatus == PlantStatus.DecorativePlant)
                                {   // grab the decorative plant
                                    toMove.Add(pi);
                                    ignore_list.Add(pi);
                                }
                                else if (pi.PlantStatus != PlantStatus.DeadTwigs)
                                {   // generate a seed
                                    Seed seed = new Seed(pi.PlantType, pi.PlantHue, showType: true);
                                    seed.Name = string.Format("{0} {1} seed", seed.PlantHue, seed.PlantType);
                                    logger.Log(string.Format("Generating seed {0} from {1}", seed, pi));
                                    if (!seedBox.TryDropItem(World.GetSystemAcct(), seed, sendFullMessage: false))
                                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store seeds in seed box:{1}.", seedBox.MaxItems, seedBox));
                                    logger.Log(string.Format("Dropping seed {0} into {1}", seed, seedBox));

                                    pi.Delete();
                                    ignore_list.Add(pi);
                                }
                                // release any of these we are handling
                                if (house.IsLockedDown(pi))
                                    house.Release(World.GetSystemAcct(), pi, checkWater: false);
                            }

                        if (toMove.Count > 0 || seedBox.Items.Count > 0)
                        {
                            foreach (Item item in toMove)
                                if (item.Deleted)
                                    throw new ApplicationException("Trying to deed an item that has already been deleted.");
                                else
                                    plantDeeds.Add(new HomeItemRestorationDeed(house, item, HomeItemRestorationDeed.LockdownType.Lockdown));

                            foreach (var unit in plantDeeds)
                                logger.Log(string.Format("Packing {0} into deed {1}", unit.Item, unit));

                            foreach (var seed in seedBox.Items)
                                logger.Log(string.Format("Packing seed {0} into seed box {1}", seed, seedBox));
                        }
                    }
                }
                #endregion Plants

                List<HomeItemRestorationDeed> addonDeeds = new();
                SmallCrate addonDeedBox = new();
                addonDeedBox.Name = "(deeds)";
                #region pack up the addons
                {   // pack up the addons

                    List<Item> toMove = new();
                    if (Addon_list.Count > 0)
                    {
                        addonDeedBox.MaxItems = Addon_list.Count;  // ensure storage
                        foreach (Item addon in Addon_list)
                        {
                            if (!ignore_list.Contains(addon))
                            {
                                if (addon is StaticHouseHelper.FixerAddon)
                                {
                                    ignore_list.Contains(addon);
                                    continue;
                                }

                                // unregister
                                if (house.Addons.Contains(addon))
                                    Addons.Remove(addon);

                                Item deed;
                                if (!Utility.IsRedeedableAddon(addon, out deed))
                                {   // no deed for this addon, we will just grab the naked addon
                                    toMove.Add(addon);
                                    ignore_list.Add(addon);
                                }
                                else
                                {   // add the deed for redeedable addon/trophy/wall hanger
                                    // For BaseAddon, we need to stuff the addon hue into the deed. This will be reflected when the addon is instantiated.
                                    if (deed is BaseAddonDeed) deed.Hue = addon.Hue;
                                    logger.Log(string.Format("Generating deed {0} from {1}", deed, addon));
                                    if (!addonDeedBox.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                        throw new ApplicationException(string.Format("Max items:{0} insufficient to store deeds in deed box:{1}.", addonDeedBox.MaxItems, addonDeedBox));
                                    logger.Log(string.Format("Dropping deed {0} into {1}", deed, addonDeedBox));

                                    addon.Delete();
                                    ignore_list.Add(addon);
                                }
                            }
                        }
                    }

                    if (toMove.Count > 0 || addonDeedBox.Items.Count > 0)
                    {
                        foreach (Item item in toMove)
                            if (item.Deleted)
                                throw new ApplicationException("Trying to deed an item that has already been deleted.");
                            else
                                addonDeeds.Add(new HomeItemRestorationDeed(house, item, HomeItemRestorationDeed.LockdownType.Addon));

                        foreach (var unit in addonDeeds)
                            logger.Log(string.Format("Packing {0} into deed {1}", unit.Item, unit));

                        foreach (var deed in addonDeedBox.Items)
                            logger.Log(string.Format("Packing deed {0} into deed box {1}", deed, addonDeedBox));
                    }
                }
                #endregion pack up the addons

                List<HomeItemRestorationDeed> lockdownDeeds = new();
                #region  pack up house lockdowns
                {
                    List<Item> toMove = new List<Item>();
                    List<Item> lockdowns = new(house.m_LockDowns.Cast<Item>().ToList());
                    foreach (object o in lockdowns)
                    {
                        if (o is Item item && !item.Deleted && !ignore_list.Contains(item))
                        {
                            house.Release(World.GetSystemAcct(), item, checkWater: false);
                            if (!house.IsLockedDown(item))
                            {
                                item.SetLastMoved();
                                toMove.Add(item);
                            }
                            else
                                ; // investigate
                            ignore_list.Add(item);
                        }
                    }

                    if (toMove.Count > 0)
                    {
                        foreach (Item item in toMove)
                            lockdownDeeds.Add(new HomeItemRestorationDeed(house, item,
                                IsLockbox(item) ? HomeItemRestorationDeed.LockdownType.Lockbox : HomeItemRestorationDeed.LockdownType.Lockdown));

                        foreach (var unit in lockdownDeeds)
                            logger.Log(string.Format("Packing {0} into deed {1}", unit.Item, unit));
                    }

                }
                #endregion pack up township lockdowns

                List<HomeItemRestorationDeed> secureDeeds = new();
                #region  pack up house secures
                {
                    List<Item> toMove = new List<Item>();
                    List<Item> secures = new();
                    foreach (var o in house.m_Secures)
                        if (o is SecureInfo si)
                            secures.Add(si.Item);

                    foreach (object o in secures)
                    {
                        if (o is Item item && !item.Deleted && !ignore_list.Contains(item))
                        {
                            house.Release(World.GetSystemAcct(), item, checkWater: false);
                            if (!house.IsSecure(item))  // need to check. strongboxes must be released by the owner
                            {
                                item.SetLastMoved();
                                toMove.Add(item);
                            }
                            else
                                ; // investigate
                            ignore_list.Add(item);
                        }
                    }

                    if (toMove.Count > 0)
                    {
                        foreach (Item item in toMove)
                            secureDeeds.Add(new HomeItemRestorationDeed(house, item, HomeItemRestorationDeed.LockdownType.Secure));

                        foreach (var unit in lockdownDeeds)
                            logger.Log(string.Format("Packing {0} into deed {1}", unit.Item, unit));
                    }

                }
                #endregion pack up township lockdowns

                List<MovingCrate> movingCrates = new();

                #region Moving Crate Key
                uint keyValue = Key.RandomValue();
                Key key = new Key(KeyType.Magic, keyValue);
                key.LootType = LootType.Blessed;
                key.Name = string.Format("a moving crate key for {0}/{1}", house.Owner.Name, house.Sign.Name);
                #endregion Moving Crate Key

                #region Build Moving Crates
                {
                    if (lockdownDeeds.Count > 0)
                    {
                        MovingCrate crate = new MovingCrate(lockdownDeeds.Count, Utility.RandomSpecialHue("lockdownDeeds"), house);
                        crate.Label = "(lockdown deeds)";
                        foreach (Item deed in lockdownDeeds)
                            if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", lockdownDeeds.Count));
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                        logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                    }
                    if (secureDeeds.Count > 0)
                    {
                        MovingCrate crate = new MovingCrate(secureDeeds.Count, Utility.RandomSpecialHue("secureDeeds"), house);
                        crate.Label = "(secure deeds)";
                        foreach (Item deed in secureDeeds)
                            if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", secureDeeds.Count));
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                        logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                    }
                    if (NPCDeeds.Count > 0)
                    {
                        MovingCrate crate = new MovingCrate(NPCDeeds.Count, Utility.RandomSpecialHue("NPCDeeds"), house);
                        crate.Label = "(NPC deeds)";
                        foreach (Item deed in NPCDeeds)
                            if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", NPCDeeds.Count));
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                        logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                    }
                    if (plantDeeds.Count > 0 || seedBox.Items.Count > 0)
                    {   // add one for the seedbox container itself
                        int item_count = plantDeeds.Count + seedBox.Items.Count + (seedBox.Items.Count > 0 ? 1 : 0);
                        MovingCrate crate = new MovingCrate(item_count, Utility.RandomSpecialHue("plantDeeds"), house);
                        crate.Label = "(seeds & decorative plants)";
                        // first the seedbox
                        if (seedBox.Items.Count > 0)
                        {
                            if (!crate.TryDropItem(World.GetSystemAcct(), seedBox, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", item_count));

                            logger.Log(string.Format("Adding {0} to moving crate {1} {2}", seedBox, crate, crate.Label));
                        }
                        // now the deeds
                        if (plantDeeds.Count > 0)
                        {
                            foreach (Item deed in plantDeeds)
                            {
                                if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                    throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", item_count));

                                logger.Log(string.Format("Adding {0} to moving crate {1} {2}", deed, crate, crate.Label));
                            }
                        }
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                    }
                    if (addonDeeds.Count > 0 || addonDeedBox.Items.Count > 0)
                    {
                        int item_count = addonDeeds.Count + addonDeedBox.Items.Count + (addonDeedBox.Items.Count > 0 ? 1 : 0);
                        MovingCrate crate = new MovingCrate(item_count, Utility.RandomSpecialHue("addonDeeds"), house);
                        crate.Label = "(addon deeds)";
                        // first the addonDeedBox
                        if (addonDeedBox.Items.Count > 0)
                        {
                            if (!crate.TryDropItem(World.GetSystemAcct(), addonDeedBox, sendFullMessage: false))
                                throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", item_count));

                            logger.Log(string.Format("Adding {0} to moving crate {1} {2}", addonDeedBox, crate, crate.Label));
                        }
                        // now the deeds
                        if (addonDeeds.Count > 0)
                        {
                            foreach (Item deed in addonDeeds)
                            {
                                if (!crate.TryDropItem(World.GetSystemAcct(), deed, sendFullMessage: false))
                                    throw new ApplicationException(string.Format("Max items:{0} insufficient to store items in moving crate.", addonDeeds.Count));

                                logger.Log(string.Format("Adding {0} to moving crate {1} {2}", deed, crate, crate.Label));
                            }
                        }
                        ConfigureLock(crate, keyValue);
                        movingCrates.Add(crate);
                        logger.Log(string.Format("Adding {0} to moving crate {1}", crate.Label, crate));
                    }

                }
                #endregion Build Moving Crates

                #region Home restoration deed
                {
                    if (movingCrates.Count > 0)
                    {
                        HouseRestorationDeed houseRestorationDeed = new HouseRestorationDeed(house, movingCrates, key);
                        switch (Utility.SecureGive(from, houseRestorationDeed))
                        {
                            case Backpack:
                                {
                                    from.SendMessage("A home restoration deed was placed in your backpack.");
                                    logger.Log(string.Format("A home restoration deed was placed in the backpack of {0}", from));
                                    break;
                                }
                            case BankBox:
                                {
                                    from.SendMessage("A home restoration deed was placed in your bank box.");
                                    logger.Log(string.Format("A home restoration deed was placed in the bank box of {0}", from));
                                    break;
                                }
                            default:
                                {
                                    from.SendMessage("A home restoration deed was dropped at your feet.");
                                    logger.Log(string.Format("A home restoration deed was placed at the feet of {0} (locked down)", from));
                                    break;
                                }
                        }
                        switch (Utility.SecureGive(from, key))
                        {
                            case Backpack:
                                {
                                    from.SendMessage("The key to your moving crates was placed in your backpack.");
                                    logger.Log(string.Format("A moving crate key was placed in the backpack of {0}", from));
                                    break;
                                }
                            case BankBox:
                                {
                                    from.SendMessage("The key to your moving crates was placed in your bank box.");
                                    logger.Log(string.Format("A moving crate key was placed in the bank box of {0}", from));
                                    break;
                                }
                            default:
                                {
                                    from.SendMessage("The key to your moving crates dropped at your feet.");
                                    logger.Log(string.Format("A moving crate key was placed at the feet of {0} (locked down)", from));
                                    break;
                                }
                        }
                        SetBaseHouseBool(BaseHouseBoolTable.IsPackedUp, true);
                    }
                    else
                        from.SendMessage("Nothing to pack up.");
                }
                #endregion Home restoration deed

                logger.Log(LogType.Mobile, from);
                logger.Log(string.Format("--home for {0}/{1} packed up.--", house, house.Owner));
            }
        }
        private static void ConfigureLock(LockableContainer c, uint keyValue)
        {
            // LockLevel of 0 means that the door can't be picklocked
            // LockLevel of -255 means it's magic locked
            c.Locked = true;
            c.MaxLockLevel = 0; // ?
            c.LockLevel = 0;
            c.KeyValue = keyValue;
        }
        public bool IsPackedUp()
        {
            // packed up, but crates have not yet been created
            if (GetBaseHouseBool(BaseHouseBoolTable.IsPackedUp) == true)
                return true;

            // If we have crates, they must be emptied
            foreach (Item item in World.Items.Values)
                if (item.Deleted == false && item is MovingCrate mc && mc.Property == this)
                    return true;

            return false;
        }
        #endregion PackUpHouse
        public bool ManagedDemolition
        {
            get
            {
                return GetBaseHouseBool(BaseHouseBoolTable.ManagedDemolition);
            }
            set
            {
                SetBaseHouseBool(BaseHouseBoolTable.ManagedDemolition, value);
            }
        }
        public bool SuppressRegion
        {
            get
            {
                return GetBaseHouseBool(BaseHouseBoolTable.SuppressRegion);
            }
            set
            {
                SetBaseHouseBool(BaseHouseBoolTable.SuppressRegion, value);
                UpdateRegionArea();
            }
        }

        private uint m_AccountCode;
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public uint AccountCode
        {
            get { return m_AccountCode; }
        }

        #region Decay Stuff
        private static bool HouseCollapsingEnabled = true;
        public static TimeSpan HouseDecayDelay = TimeSpan.FromDays(15.0);
        public static TimeSpan MaxHouseDecayTime = TimeSpan.FromDays(30.0 * 3); //virtual time bank max
        private static DateTime m_HouseDecayLast = DateTime.MinValue;   // not serialized
        private TimeSpan m_RestartDecay = TimeSpan.Zero;                // serialized
        private DateTime m_RestartDecayDelta = DateTime.MinValue;       // not serialized

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public TimeSpan RestartDecay
        {
            get
            {
                return m_RestartDecay;
            }
            set
            {
                m_RestartDecay = value;
            }
        }

        // town crier entry for telling the world our house is idoc
        private TownCrierEntry m_IDOC_Broadcast_TCE;

        // wea: added public accessor
        public TownCrierEntry IDOC_Broadcast_TCE
        {
            get
            {
                return m_IDOC_Broadcast_TCE;
            }

            set
            {
                m_IDOC_Broadcast_TCE = value;
            }
        }

        private bool m_NeverDecay;

        public bool NeverDecay
        {
            get { return m_NeverDecay; }
            set { m_NeverDecay = value; }
        }
        public DateTime StructureDecayTime
        {
            get
            {
                return DateTime.UtcNow + TimeSpan.FromMinutes(m_DecayMinutesStored);
            }
        }

        private double m_DecayMinutesStored;

        [CommandProperty(AccessLevel.GameMaster)]
        public double DecayMinutesStored
        {
            get
            {
                return m_DecayMinutesStored;
            }
            set
            {
                m_DecayMinutesStored = Math.Min(value, MaxHouseDecayTime.TotalMinutes);

                if (Core.RuleSets.AngelIslandRules())
                {
                    if (m_DecayMinutesStored > ONE_DAY_IN_MINUTES && m_IDOC_Broadcast_TCE != null)
                    {
                        GlobalTownCrierEntryList.Instance.RemoveEntry(m_IDOC_Broadcast_TCE);
                        m_IDOC_Broadcast_TCE = null;
                    }
                }
            }
        }

        public void RefreshNonDecayingHouse()
        {
            if (StructureDecayTime < DateTime.UtcNow)
            {
                Refresh();
            }
        }

        public virtual void TotalRefresh()
        {
            m_DecayMinutesStored = MaxHouseDecayTime.TotalMinutes;
        }

        public virtual void Refresh()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                m_DecayMinutesStored = HouseDecayDelay.TotalMinutes;

                if (m_DecayMinutesStored > ONE_DAY_IN_MINUTES && m_IDOC_Broadcast_TCE != null)
                {
                    GlobalTownCrierEntryList.Instance.RemoveEntry(m_IDOC_Broadcast_TCE);
                    m_IDOC_Broadcast_TCE = null;
                }
            }
            else
            {
                m_DecayMinutesStored = HouseDecayDelay.TotalMinutes;
            }
        }

        private bool GlobalNeverDecay()
        {
            return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FreezeHouseDecay);
        }

        public bool TownshipRestrictedRefresh
        {
            get
            {
                if (m_Owner != null)
                {
                    TownshipRegion tsr = TownshipRegion.GetTownshipAt(this.Location, this.Map);

                    if (tsr != null && tsr.TStone != null && !tsr.TStone.IsMember(m_Owner))
                    {
                        if (m_LastTraded > tsr.TStone.BuiltOn || m_BuiltOn > tsr.TStone.BuiltOn)
                        {
                            // if we're last traded after the township was placed,
                            // and we're not of the township's guild,
                            // then we can't refresh the house.
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        // See if we are to auto-resume a frozen decay
        public void CheckAutoResumeDecay()
        {
            if (m_RestartDecay > TimeSpan.Zero)             // is the system enabled?
            {
                if (m_RestartDecayDelta != DateTime.MinValue)
                {
                    TimeSpan temp = DateTime.UtcNow - m_RestartDecayDelta;
                    if (temp >= m_RestartDecay)
                    {
                        m_RestartDecay = TimeSpan.Zero;     // disable system
                        m_NeverDecay = false;               // resume decay
                    }
                    else
                        m_RestartDecay -= temp;             // we're this much closer
                }
                m_RestartDecayDelta = DateTime.UtcNow;
            }
        }

        //returns true if house decays
        public bool CheckDecay()
        {
            // first, see if we are to auto-resume a frozen decay
            CheckAutoResumeDecay();

            //if house is set to never decay, refresh it!
            // adam: also, don't consume tax credits if decay if frozen
            if (m_NeverDecay || GlobalNeverDecay())
            {
                RefreshNonDecayingHouse();
                return false;
            }

            // decay any lockboxes if needed
            if (LockboxSystem)
                CheckLockboxDecay();

            // if the owner annexed a tent to place this house, then Demolition Managed, i.e., they cannot transfer or demolish
            //	before 7 days. Clear the ManagedDemolishion flag if 7 days have passed.
            if (GetBaseHouseBool(BaseHouse.BaseHouseBoolTable.ManagedDemolition) == true)
            {
                DateTime ok_demo = BuiltOn + (Core.UOBETA_CFG ? TimeSpan.FromHours(1.0) : TimeSpan.FromDays(7.0));
                if (DateTime.UtcNow > ok_demo)
                    SetBaseHouseBool(BaseHouse.BaseHouseBoolTable.ManagedDemolition, false);
            }

            // calc time to IDOC
            double oldminutes = m_DecayMinutesStored;
            m_DecayMinutesStored -= LastHouseCheckMinutes;

            // check if we should email an idoc warning (2 days before IDOC)
            if (oldminutes > ONE_DAY_IN_MINUTES * 2 && m_DecayMinutesStored <= ONE_DAY_IN_MINUTES * 2)
                EmailWarning();

            // check if we should broadcast idoc
            if (oldminutes > ONE_DAY_IN_MINUTES && m_DecayMinutesStored <= ONE_DAY_IN_MINUTES)
                LogDecay();

            if (m_DecayMinutesStored < 0)
            {
                if (HouseCollapsingEnabled)
                {
                    if (Core.RuleSets.AngelIslandRules())
                        CleanHouse(); // remove checks, gold and other economy ruining items during idoc
                    Delete();
                }
                return true;
            }
            return false;
        }

        public bool CheckDemolition()
        {
            /* delete this method*/
            return true;
        }

        private void EmailWarning()
        {
            try
            {   // only on the production shard
                if (TestCenter.Enabled == false)
                    if (Owner != null && Owner.Account != null && Owner.Account as Accounting.Account != null)
                    {
                        Accounting.Account a = Owner.Account as Accounting.Account;
                        if (a.EmailAddress != null && a.EmailAddress.Length > 0 && SmtpDirect.CheckEmailAddy(a.EmailAddress, false) == true)
                        {
                            string subject = "Angel Island: Your house is in danger of collapsing";
                            string body = String.Format("\nThis message is to inform you that your house at {2} on the '{0}' account is in danger of collapsing (IDOC). If you do not return to refresh your house, it will fall on {1}.\n\nBest Regards,\n  The Angel Island Team\n\n", a.ToString(), DateTime.UtcNow + TimeSpan.FromMinutes(m_DecayMinutesStored), BanLocation);
                            Emailer mail = new Emailer();
                            mail.SendEmail(a.EmailAddress, subject, body, false);
                        }
                    }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private void LogDecay()
        {
            LogHelper Logger = new LogHelper("houseDecay.log", false);
            bool announced = false;
            if (this is Tent || this is SiegeTent)
            {
                //never announce tents
            }
            else if (Utility.RandomDouble() < CoreAI.IDOCBroadcastChance && (Core.RuleSets.AngelIslandRules()))
            {
                string[] lines = new string[1];
                lines[0] = String.Format("Lord British has condemned the estate of {0} near {1}.", this.Owner.Name, DescribeLocation());
                m_IDOC_Broadcast_TCE = new TownCrierEntry(lines, TimeSpan.FromMinutes(m_DecayMinutesStored), Serial.MinusOne);
                GlobalTownCrierEntryList.Instance.AddEntry(m_IDOC_Broadcast_TCE);
                announced = true;
            }

            try
            {
                // log it
                string temp = string.Format(
                    "Owner:{0}, Account:{1}, Name:{2}, Serial:{3}, Location:{4}, BuiltOn:{5}, StructureDecayTime:{6}, Type:{7}, Announced:{8}",
                    this.m_Owner,
                    this.m_Owner.Account,
                    ((this.m_Sign != null) ? this.m_Sign.Name : "NO SIGN"),
                    this.Serial,
                    this.Location,
                    this.BuiltOn,
                    this.StructureDecayTime,
                    this.GetType(),
                    announced.ToString()
                    );
                Logger.Log(LogType.Text, temp);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Logger.Finish();
            }
        }

        /*
		public override void Delete()
		{
			// now delete player placed Sensory Consoles
			Item[] scs = FindAllItems(typeof(SensoryConsole));
			foreach (SensoryConsole sc in scs)
			{	// if player owned, delete it
				if (sc.Owner != null && sc.Owner.AccessLevel == AccessLevel.Player)
				{
					sc.Delete();
				}
			}

			base.Delete();
		}*/

        private void CleanHouse()
        {
            LogHelper Logger = new LogHelper("IDOCCleanup.log", false);
            try
            {
                ArrayList list = new ArrayList();

                // process lockdowns
                if (m_LockDowns != null)
                    foreach (Item ix in m_LockDowns)
                    {
                        if (ix is Container == true)
                        {
                            ArrayList contents = (ix as Container).FindAllItems();
                            foreach (Item jx in contents)
                                if (IsIDOCManagedItem(jx))
                                    list.Add(jx);
                        }
                        else if (IsIDOCManagedItem(ix))
                            list.Add(ix);
                    }

                // now for secures
                if (m_Secures != null)
                    foreach (SecureInfo info in m_Secures)
                    {
                        if (info.Item is Container == true)
                        {
                            ArrayList contents = (info.Item as Container).FindAllItems();
                            foreach (Item jx in contents)
                                if (IsIDOCManagedItem(jx))
                                    list.Add(jx);
                        }
                        else if (IsIDOCManagedItem(info.Item))
                            list.Add(info.Item);
                    }

                // okay, now delete all IDOC managed items
                foreach (Item mx in list)
                {
                    if (mx == null || mx.Deleted == true)
                        continue;

                    // log it
                    LogIDOCManagedItem(Logger, mx);

                    // release it
                    if (IsLockedDown(mx))
                        SetLockdown(mx, false);
                    else if (IsSecure(mx))
                        ReleaseSecure(mx);

                    // delete it
                    mx.Delete();
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                Logger.Finish();
            }
        }

        // stuff you want to delete
        private bool IsIDOCManagedItem(Item ix)
        {
            if (ix == null || ix.Deleted == true)
                return false;

            // exceptions to the delete rules below
            if (IsIDOCManagedItemException(ix) == true)
                return false;

            if (ix.Stackable == true)
                return true;

            if (ix is CommodityDeed)
                return true;

            if (ix is BankCheck)
                return true;

            return false;
        }

        private void LogIDOCManagedItem(LogHelper Logger, Item ix)
        {
            // log all the items deleted at IDOC
            if (Logger == null)
                return;

            if (ix == null || ix.Deleted == true)
                return;

            if (ix.Stackable == true)
                Logger.Log(LogType.Item, ix, String.Format("Amount = {0}", ix.Amount));

            if (ix is CommodityDeed)
                Logger.Log(LogType.Item, ix, String.Format("Commodity = {0}, Amount = {1}", (ix as CommodityDeed).Commodity, (ix as CommodityDeed).CommodityAmount));

            if (ix is BankCheck)
                Logger.Log(LogType.Item, ix, String.Format("Amount = {0}", (ix as BankCheck).Worth));

            return;
        }

        private bool IsIDOCManagedItemException(Item ix)
        {
            if (ix == null || ix.Deleted == true)
                return false;

            // necro regs are an exception
            if (ix is BatWing || ix is GraveDust || ix is DaemonBlood || ix is NoxCrystal || ix is PigIron)
                return true;

            return false;
        }

        //This is called by the object that keeps track 
        //of steps.
        public const double ONE_DAY_IN_MINUTES = 60.0 * 24.0;
        public virtual void RefreshHouseOneDay()
        {
            if (TownshipRestrictedRefresh)
            {
                return;
            }

            if (m_DecayMinutesStored <= MaxHouseDecayTime.TotalMinutes)
            {
                if (m_DecayMinutesStored >= (MaxHouseDecayTime.TotalMinutes - ONE_DAY_IN_MINUTES))
                {
                    m_DecayMinutesStored = MaxHouseDecayTime.TotalMinutes;
                }
                else
                {
                    m_DecayMinutesStored += ONE_DAY_IN_MINUTES;
                }
            }

            if (m_DecayMinutesStored > ONE_DAY_IN_MINUTES && m_IDOC_Broadcast_TCE != null)
            {
                GlobalTownCrierEntryList.Instance.RemoveEntry(m_IDOC_Broadcast_TCE);
                m_IDOC_Broadcast_TCE = null;
            }

        }

        public virtual string DecayState()
        {
            TimeSpan decay = TimeSpan.FromMinutes(m_DecayMinutesStored);

            if (decay == BaseHouse.MaxHouseDecayTime || this.m_NeverDecay == true || GlobalNeverDecay() == true)
            {
                return "This structure is in perfect condition.";
            }
            else if (decay <= TimeSpan.FromDays(1.0)) //1 day
            {
                return "This structure is in danger of collapsing.";
            }
            else if (decay <= HouseDecayDelay - TimeSpan.FromDays(12.0)) //3days
            {
                return "This structure is greatly worn.";
            }
            else if (decay <= HouseDecayDelay - TimeSpan.FromDays(9.0)) //6 days
            {
                return "This structure is fairly worn.";
            }
            else if (decay <= HouseDecayDelay - TimeSpan.FromDays(5.0)) //10 days
            {
                return "This structure is somewhat worn.";
            }
            else if (decay <= HouseDecayDelay - TimeSpan.FromDays(1.0)) //14 days
            {
                return "This structure is slightly worn.";
            }
            else if (decay > HouseDecayDelay - TimeSpan.FromDays(1.0) &&
                decay < BaseHouse.MaxHouseDecayTime)
            {
                return "This structure is like new.";
            }
            else
            {
                return "This structure has problems.";
            }
        }

        // needed for decay check
        public static int LastHouseCheckMinutes
        {
            get
            {
                // see if it's been initialized yet
                if (m_HouseDecayLast == DateTime.MinValue)
                    return 0;

                // return the delta in minutes since last check
                TimeSpan sx = DateTime.UtcNow - m_HouseDecayLast;
                return (int)sx.TotalMinutes;
            }
        }

        public static DateTime HouseDecayLast
        {
            get { return m_HouseDecayLast; }
            set { m_HouseDecayLast = value; }
        }

        /*public static int CheckAllHouseDecay()
		{
			int numberchecked = 0;
			try
			{
				foreach (ArrayList list in BaseHouse.m_Table.Values)
				{
					for (int i = 0; i < list.Count; i++)
					{
						BaseHouse house = list[i] as BaseHouse;
						if (house != null)
						{
							if (house.m_NeverDecay)
							{
								Point3D loc = house.Location;
								if (house.Sign != null) loc = house.Sign.Location;
								Mobile owner = house.Owner;

								if (owner != null)
								{	// don't list staff owned houses
									if (owner.AccessLevel == AccessLevel.Player)
										Console.WriteLine("House: (Never Decays) owner: " + owner + " location: " + loc);
								}
								else
								{
									Console.WriteLine("House: (Never Decays) owner: NULL location: " + loc);
								}
							}

							house.CheckDecay();
							numberchecked++;
						}
					}
				}
			}
			catch (Exception e)
			{
				LogHelper.LogException(e);
				Console.WriteLine("Error in CheckAllHouseDecay(): " + e.Message);
				Console.WriteLine(e.StackTrace.ToString());
			}

			// Adam: record the 'last' check.
			m_HouseDecayLast = DateTime.UtcNow;

			return numberchecked;
		}*/

        #endregion //Decay stuff

        #region Stash House
        private List<object> m_StashComponents = new List<object>();
        public List<object> StashComponents
        {
            get { return m_StashComponents; }
        }
        #endregion Stash House

        public static int SetSecurity(bool state)
        {
            int numberchecked = 0;
            try
            {
                foreach (ArrayList list in BaseHouse.m_Table.Values)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        BaseHouse house = list[i] as BaseHouse;
                        if (house != null)
                        {
                            house.SecurePremises = state;
                        }
                        numberchecked++;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Error in BaseHouse.SetSecurity(): " + e.Message);
                Console.WriteLine(e.StackTrace.ToString());
            }
            return numberchecked;
        }

        public string DescribeLocation()
        {
            string location;
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            Point3D p = new Point3D(this.Location);
            p.X = (p.X + Utility.RandomMinMax(-70, 70));
            p.Y = (p.Y + Utility.RandomMinMax(-70, 70));

            bool valid = Sextant.Format(p, this.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = String.Format("{0}� {1}'{2}, {3}� {4}'{5}", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W");
            else
                location = "????";

            if (!valid)
                location = string.Format("{0} {1}", p.X, p.Y);

            if (this.Map != null)
            {
                if (this.Region != this.Map.DefaultRegion && this.Region.ToString() != "")
                {
                    location += (" in " + this.Region);
                }
            }

            return location;
        }

        public const int MaxCoOwners = 15;
        public const int MaxFriends = 150;
        public const int MaxBans = 50;

        private bool m_Public;
        private bool m_MembershipOnly = false;
        private bool m_SecurePremises = false;
        public bool SecurePremises
        {
            get
            {
                return m_SecurePremises || CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SecurePremises);
            }
            set { m_SecurePremises = value; }
        }

        private HouseRegion m_Region;
        private HouseSign m_Sign;
        private TrashBarrel m_Trash;
        private ArrayList m_Doors;

        private Mobile m_Owner;

        private ArrayList m_Access;
        private ArrayList m_Bans;
        private ArrayList m_CoOwners;
        private ArrayList m_Friends;
        private Dictionary<Mobile, DateTime> m_Memberships = new Dictionary<Mobile, DateTime>();
        public Dictionary<Mobile, DateTime> Memberships { get { return m_Memberships; } }
        private ArrayList m_LockDowns;
        private ArrayList m_Secures;

        private ArrayList m_Addons;

        private int m_MaxLockDowns;
        private int m_MaxSecures;

        private int m_BonusLockDowns;
        private int m_BonusSecures;

        private int m_Price;

        private int m_Visits;

        private uint m_UpgradeCosts;

        private DateTime m_BuiltOn, m_LastTraded;

        private static Hashtable m_Table = new Hashtable();

        public enum CooperativeType
        {
            None,   // default
            Blacksmith,
            Tailor,
            // add your Cooperative Types here
        }

        private CooperativeType m_cooperativeType = CooperativeType.None;
        public CooperativeType Cooperative { get { return m_cooperativeType; } set { m_cooperativeType = value; } }

        public bool MembershipOnly { get { return m_MembershipOnly; } set { m_MembershipOnly = value; } }
        public virtual bool IsAosRules { get { return Core.RuleSets.AOSRules(); } }

        [CommandProperty(AccessLevel.Owner)]
        public uint UpgradeCosts
        {   // refundable upgrade costs when redeeding house
            get { return m_UpgradeCosts; }
            set { m_UpgradeCosts = value; }
        }

        public static Hashtable Multis
        {
            get
            {
                return m_Table;
            }
        }

        public virtual HousePlacementEntry GetAosEntry()
        {
            return HousePlacementEntry.Find(this);
        }

        public virtual int GetAosMaxSecures()
        {
            HousePlacementEntry hpe = GetAosEntry();

            if (hpe == null)
                return 0;

            return hpe.Storage;
        }

        public virtual int GetAosMaxLockdowns()
        {
            HousePlacementEntry hpe = GetAosEntry();

            if (hpe == null)
                return 0;

            return hpe.Lockdowns;
        }

        public virtual int GetAosCurSecures(out int fromSecures, out int fromVendors, out int fromLockdowns)
        {
            fromSecures = 0;
            fromVendors = 0;
            fromLockdowns = 0;

            ArrayList list = m_Secures;

            for (int i = 0; list != null && i < list.Count; ++i)
            {
                SecureInfo si = (SecureInfo)list[i];

                fromSecures += si.Item.TotalItems;
            }

            if (m_LockDowns != null)
                fromLockdowns += m_LockDowns.Count;

            foreach (Mobile mx in m_Region.Mobiles.Values)
                if (mx is PlayerVendor)
                    if (mx.Backpack != null)
                        fromVendors += mx.Backpack.TotalItems;

            return fromSecures + fromVendors + fromLockdowns;
        }

        public virtual bool CanPlaceNewVendor()
        {
            if (!IsAosRules)
                return true;

            return CheckAosLockdowns(10);
        }

        #region BARKEEP SYSTEM
        /* NPCData, currently we only use byte 4 for the max barkeep count
		 * bytes 1,2&3 are currently unused and available
		 */
        private uint m_NPCData;
        public uint MaximumBarkeepCount
        {
            get { return Utility.GetUIntByte4(m_NPCData); }
            set { Utility.SetUIntByte4(ref m_NPCData, value); }
        }

        public virtual bool CanPlaceNewBarkeep()
        {
            int avail = (int)MaximumBarkeepCount;

            foreach (Mobile mx in m_Region.Mobiles.Values)
            {
                if (avail <= 0)
                    break;

                if (mx is PlayerBarkeeper)
                    --avail;
            }

            return (avail > 0);
        }
        #endregion BARKEEP SYSTEM

        public virtual bool CheckAosLockdowns(int need)
        {
            return ((GetAosCurLockdowns() + need) <= GetAosMaxLockdowns());
        }

        public virtual bool CheckAosStorage(int need)
        {
            int fromSecures, fromVendors, fromLockdowns;

            return ((GetAosCurSecures(out fromSecures, out fromVendors, out fromLockdowns) + need) <= GetAosMaxSecures());
        }

        public static void Configure()
        {
            Item.LockedDownFlag = 1;
            Item.SecureFlag = 2;
        }

        public virtual int GetAosCurLockdowns()
        {
            int v = 0;

            if (m_LockDowns != null)
                v += m_LockDowns.Count;

            if (m_Secures != null)
                v += m_Secures.Count;

            foreach (Mobile mx in m_Region.Mobiles.Values)
                if (mx is PlayerVendor)
                    v += 10;

            return v;
        }

        public static bool CheckLockedDown(Item item)
        {
            BaseHouse house = FindHouseAt(item);

            return (house != null && house.IsLockedDown(item));
        }

        public static bool CheckSecured(Item item)
        {
            BaseHouse house = FindHouseAt(item);

            return (house != null && house.IsSecure(item));
        }

        public static bool CheckLockedDownOrSecured(Item item)
        {
            BaseHouse house = FindHouseAt(item);

            return (house != null && (house.IsSecure(item) || house.IsLockedDown(item)));
        }

        public static ArrayList GetHouses(Mobile m)
        {
            ArrayList list = new ArrayList();

            if (m != null)
            {
                ArrayList exists = (ArrayList)m_Table[m];

                if (exists != null)
                {
                    for (int i = 0; i < exists.Count; ++i)
                    {
                        BaseHouse house = exists[i] as BaseHouse;

                        if (house != null && !house.Deleted && house.Owner == m)
                            list.Add(house);
                    }
                }
            }

            if (list.Count == 0)
                return GetHousesInheritance(m);

            return list;
        }

        public static ArrayList GetHousesInheritance(Mobile owner)
        {
            ArrayList list = new ArrayList();
            Multis.BaseHouse bh = null;
            if (owner != null && owner.Account != null && owner.Account is Accounting.Account acct)
            {
                if (acct.House != Server.Serial.Zero)
                    bh = World.FindItem(acct.House) as Multis.BaseHouse;
                if (bh != null)
                {
                    Utility.ConsoleWriteLine("Found Houses Inheritance for {0}", ConsoleColor.Green, owner);
                    list.Add(bh);
                }
            }
            return list;
        }

        // wea: added to retrieve all houses on mobile's account
        public static ArrayList GetAccountHouses(Mobile m)
        {
            ArrayList list = new ArrayList();

            Account a = m.Account as Account;

            if (a == null)
                return list;

            // loop characters
            for (int i = 0; i < 5; ++i)
            {
                if (a[i] != null)
                {
                    // loop houses
                    ArrayList exists = (ArrayList)m_Table[a[i]];
                    if (exists != null)
                        foreach (object o in exists)
                            list.Add(o);                    // add any found to master list
                }
            }

            if (list.Count == 0)
                return GetHousesInheritance(m);

            return list;
        }

        public static bool CheckHold(Mobile m, Container cont, Item item, bool message, bool checkItems)
        {
            BaseHouse house = FindHouseAt(cont);

            if (house == null || !house.IsAosRules)
                return true;

            object root = cont.RootParent;

            if (root == null)
                root = cont;

            if (root is Item && house.IsSecure((Item)root) && !house.CheckAosStorage(1 + item.TotalItems))
            {
                if (message)
                    m.SendLocalizedMessage(1061839); // This action would exceed the secure storage limit of the house.

                return false;
            }

            return true;
        }

        public static bool CheckAccessible(Mobile m, Item item)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return true; // Staff can access anything

            BaseHouse house = FindHouseAt(item);

            if (house == null)
                return true;

            SecureAccessResult res = house.CheckSecureAccess(m, item);

            switch (res)
            {
                case SecureAccessResult.Insecure: break;
                case SecureAccessResult.Accessible: return true;
                case SecureAccessResult.Inaccessible: return false;
            }

            // lockboxes are accessible by everyone
            if (LockboxSystem && house.IsLockedDown(item) && IsLockbox(item))
                return true;

            // Yoar: The following code is compatible with the lockbox system. But let's disable it anyways...
#if false
            if (house.IsLockedDown(item))
                return house.IsCoOwner( m ) && (item is Container);
#endif

            return true;
        }

        public static BaseHouse FindHouseAt(Mobile m)
        {
            if (m == null || m.Deleted)
                return null;

            return FindHouseAt(m.Location, m.Map, 16);
        }

        public static BaseHouse FindHouseAt(Item item)
        {
            if (item == null || item.Deleted)
                return null;

            return FindHouseAt(item.GetWorldLocation(), item.Map, item.ItemData.Height);
        }

        public new static BaseHouse Find(Point3D loc, Map map)
        {
            return FindHouseAt(loc, map, 16);
        }
        public static BaseHouse FindHouseAt(Point3D loc, Map map, int height)
        {
            if (map == null || map == Map.Internal)
                return null;

            Sector sector = map.GetSector(loc);

            // Adam: the width and height of MultiComponentList Components for tents are all 0, so we can't use the IsInside() check
            //  when checking to see of the point (item or mobile) inside or outside the tent
            foreach (BaseMulti mult in sector.Multis)
            {
                BaseHouse house = mult as BaseHouse;
                if (house != null)
                {
                    if (house.Region != null && house.Region is HouseRegion)
                    {
                        if (house is Tent || house is SiegeTent)
                        {
                            if (house.Region.Contains(loc))
                                return house;
                        }
                        else if (house.IsInside(loc, height))
                        {
                            return house;
                        }
                    }
                }
            }

            return null;
        }

        public bool IsInside(Mobile m)
        {
            if (m == null || m.Deleted || m.Map != this.Map)
                return false;

            return IsInside(m.Location, 16);
        }

        public bool IsInside(Item item)
        {
            if (item == null || item.Deleted || item.Map != this.Map)
                return false;

            return IsInside(item.Location, item.ItemData.Height);
        }

        public bool CheckAccessibility(Item item, Mobile from)
        {
            SecureAccessResult res = CheckSecureAccess(from, item);

            switch (res)
            {
                case SecureAccessResult.Insecure: break;
                case SecureAccessResult.Accessible: return true;
                case SecureAccessResult.Inaccessible: return false;
            }

            if (!IsLockedDown(item))
                return true;
            else if (IsLockedDown(item) && item is Forge && IsMember(from))
                return true;
            else if (from.AccessLevel >= AccessLevel.GameMaster)
                return true;
            else if (item is Runebook)
                return true;
            else if (item is SeedBox)
                return true;
            else if (item is Library)
                return true;
            else if (item is ISecurable)
                return HasSecureAccess(from, ((ISecurable)item).Level);

            else if (item is Key)
            {   // Adam: for complementary access, see KeyRing
                // from.Say("item is Key (IsFriend)");
                return IsFriend(from);
            }
            else if (item is KeyRing)
            {   // Adam: for complementary access, see Key
                //from.Say("item is Keyring (IsCoOwner)");
                return IsCoOwner(from);
            }
            else if ((item is Container) /*&& (m_Public == false)*/)
            {
                // Adam: lockboxes in ANY houses are accessible by anyone!
                //from.Say("item is Container (IsAnyOne)");
                return true;
            }

            else if (item is BaseLight)
                return IsFriend(from);
            else if (item is PotionKeg)
                return IsFriend(from);
            else if (item is BaseGameBoard)
                return true;
            else if (item is Dices)
                return true;
            else if (item is RecallRune)
                return true;
            else if (item is TreasureMap)
                return true;
            else if (item is Clock)
                return true;
            else if (item is BaseBook)
                return true;
            else if (item is BaseInstrument)
                return true;
            else if (item is Dyes || item is DyeTub)
                return true;
            else if (item is EternalEmbers)
                return true;
            else if (item is VendorRentalContract)
                return IsOwner(from);
            else if (item is MapItem)   // 8/2/2023, Adam
                return IsFriend(from);
            else if (item is Spyglass)  // 8/2/2023, Adam
                return IsFriend(from);
            else if (item is Scales)    // 8/14/2023, Adam
                return IsFriend(from);
            else if (item is BaseStatue)    // 10/31/2023, Yoar (Magic statues)
                return IsFriend(from);

            return false;
        }

        public virtual bool IsInside(Point3D p, int height)
        {
            if (Deleted)
                return false;

            // Adam: tents are special - cannot account for height though. Is this ever a problem?
            if ((this is Tent || this is SiegeTent) && this.Region != null)
            {
                if (this.Region.Contains(p))
                    return true;
            }

            MultiComponentList mcl = Components;

            int x = p.X - (X + mcl.Min.X);
            int y = p.Y - (Y + mcl.Min.Y);

            if (x < 0 || x >= mcl.Width || y < 0 || y >= mcl.Height)
                return false;

            /* //pla: don't want this anymore
		   if ( this is HouseFoundation && y < (mcl.Height-1) )
			 return true;
		   */

            StaticTile[] tiles = mcl.Tiles[x][y];

            for (int j = 0; j < tiles.Length; ++j)
            {
                StaticTile tile = tiles[j];
                int id = tile.ID & 0x3FFF;
                ItemData data = TileData.ItemTable[id];

                // Slanted roofs do not count; they overhang blocking south and east sides of the multi
                if ((data.Flags & TileFlag.Roof) != 0)
                    continue;

                // Signs and signposts are not considered part of the multi
                if ((id >= 0xB95 && id <= 0xC0E) || (id >= 0xC43 && id <= 0xC44))
                    continue;

                int tileZ = tile.Z + this.Z;

                if (p.Z == tileZ || (p.Z + height) > tileZ)
                    return true;
            }

            return false;
        }

        public SecureAccessResult CheckSecureAccess(Mobile m, Item item)
        {
            if (m_Secures == null || !(item is Container))
                return SecureAccessResult.Insecure;

            if (item is VendorRecoveryBox vrb && vrb.Owner == m)
                return SecureAccessResult.Accessible;

            for (int i = 0; i < m_Secures.Count; ++i)
            {
                SecureInfo info = (SecureInfo)m_Secures[i];

                if (info.Item == item)
                    return HasSecureAccess(m, info.Level) ? SecureAccessResult.Accessible : SecureAccessResult.Inaccessible;
            }

            return SecureAccessResult.Insecure;
        }

        public BaseHouse(int multiID, Mobile owner, int MaxLockDown, int MaxSecure, int MaxLockbox)
            : base(multiID | 0x4000)
        {
            //initialize decay
            m_DecayMinutesStored = HouseDecayDelay.TotalMinutes;

            m_BuiltOn = DateTime.UtcNow;
            m_LastTraded = DateTime.MinValue;

            m_Doors = new ArrayList();
            m_LockDowns = new ArrayList();
            m_Secures = new ArrayList();
            m_Addons = new ArrayList();

            m_CoOwners = new ArrayList();
            m_Friends = new ArrayList();
            m_Bans = new ArrayList();
            m_Access = new ArrayList();

            m_Region = new HouseRegion(this);
            Region.AddRegion(m_Region);

            m_Owner = owner;
            m_AccountCode = CalcAccountCode(owner);

            m_MaxLockDowns = MaxLockDown;
            m_MaxSecures = MaxSecure;

            m_LockboxCount = 0; // no lock boses yet
            m_MaxLockboxes = MaxLockbox; // base lockbox limit
            m_BonusLockboxes = 0; // no bonus lockboxes yet
            m_StorageTaxCredits = 0; // no storage tax credits lockboxes yet

            m_UpgradeCosts = 0; // no refundable upgrades yet

            MaximumBarkeepCount = 2; // default for new houses

            UpdateRegionArea();

            if (owner != null)
            {
                ArrayList list = (ArrayList)m_Table[owner];

                if (list == null)
                    m_Table[owner] = list = new ArrayList();

                list.Add(this);
            }

            Movable = false;

            //Decay Stuff:
            m_NeverDecay = false;
            Refresh();
        }

        // TODO: Generalize to return 'HashSet<IEntity>'?
        public HashSet<Item> GetFixedItems()
        {
            HashSet<Item> list = new HashSet<Item>();

            AddFixedItems(list);

            return list;
        }

        protected virtual void AddFixedItems(HashSet<Item> list)
        {
            if (m_Sign != null && !m_Sign.Deleted)
                list.Add(m_Sign);

            if (m_Trash != null && !m_Trash.Deleted)
                list.Add(m_Trash);

            foreach (Item item in m_Doors)
            {
                if (!item.Deleted)
                    list.Add(item);
            }

            foreach (Item item in m_LockDowns)
            {
                if (!item.Deleted)
                    list.Add(item);
            }

            foreach (SecureInfo info in m_Secures)
            {
                Item item = info.Item;

                if (!item.Deleted)
                    list.Add(item);
            }

            foreach (Item item in m_Addons)
            {
                if (!item.Deleted)
                    list.Add(item);
            }
        }

        public BaseHouse(Serial serial)
            : base(serial)
        {
        }

        public override void OnMapChange()
        {
            m_Region.Map = this.Map;

            foreach (Item item in GetFixedItems())
                item.Map = this.Map;
        }

        public virtual void ChangeSignType(int itemID)
        {
            if (m_Sign != null)
            {
                bool flipped = ((m_Sign.ItemID % 2) == 1);

                m_Sign.ItemID = itemID;

                if (flipped)
                    m_Sign.ItemID--;
            }
        }

        private static Rectangle2D[] m_AreaArray = new Rectangle2D[0];
        public virtual Rectangle2D[] Area { get { return m_AreaArray; } }

        public virtual void UpdateRegionArea()
        {
            Rectangle2D[] area = this.Area;

            m_Region.Coords.Clear();

            for (int i = 0; i < area.Length; ++i)
                m_Region.Coords.Add(Region.ConvertTo3D(new Rectangle2D(X + area[i].Start.X, Y + area[i].Start.Y, area[i].Width, area[i].Height)));
        }
        public List<Mobile> FindTownshipNPCs()
        {
            List<Mobile> list = new();
            MultiComponentList mcl = this.Components;
            foreach (object o in this.Map.GetMobilesInBounds(new Rectangle2D(this.Location.X + mcl.Min.X, this.Location.Y + mcl.Min.Y, mcl.Width, mcl.Height)))
                if (o is ITownshipNPC)
                    list.Add(o as Mobile);

            return list;
        }
        private Map StashDestMap(bool stash)
        {
            if (stash == true) return Map.Internal;
            else return Map.Felucca;
        }
        public void StashHouse(bool stash)
        {
            if (ValidMap(stash, this, false))
            {
                if (stash == true)
                {
                    List<MoveEntry> toMove = null;

                    // 1. Find all items/mobiles in the house that we want to move.
                    toMove = new List<MoveEntry>();
                    MultiComponentList mcl = this.Components;
                    foreach (object o in this.Map.GetObjectsInBounds(new Rectangle2D(this.Location.X + mcl.Min.X, this.Location.Y + mcl.Min.Y, mcl.Width, mcl.Height)))
                    {
                        if (o is IEntity && ValidMap(stash, o))
                        {
                            IEntity ent = (IEntity)o;
                            MoveEntry me = new MoveEntry(ent, ent.Location);
                            if (!toMove.Contains(me))
                                toMove.Add(me);
                            else
                                ;
                        }
                    }

                    // 2. Move all fixed items explicitly. This ensures that items outside of the MCL are also moved.
                    foreach (Item fe in GetFixedItems())
                    {
                        if (fe is IEntity && ValidMap(stash, fe))
                        {
                            IEntity ent = (IEntity)fe;
                            MoveEntry me = new MoveEntry(ent, ent.Location);
                            if (!toMove.Contains(me))
                                toMove.Add(me);
                            else
                                ;
                        }
                        else
                            ;
                    }

                    // 3. Move all BaseAddon components explicitly..
                    if (toMove != null)
                        foreach (MoveEntry me in toMove)
                        {   // handle each component of a BaseAddon
                            if (me.Entity is BaseAddon ba && ValidMap(stash, me.Entity))
                            {
                                foreach (AddonComponent c in ba.Components)
                                    if (c is Item item && ValidMap(stash, c))
                                        if (!toMove.Contains(me))
                                            toMove.Add(me);
                                        else
                                            ;
                            }
                        }


                    // 4. mark everything as IsIntMapStorage = true
                    //  We need to do this separately from the move, since moving some objects like the house will move other components
                    //  like the house sign.
                    if (toMove != null)
                        foreach (MoveEntry me in toMove)
                        {
                            if (ValidMap(stash, me.Entity))
                            {
                                if (me.Entity is Item item)
                                    item.IsIntMapStorage = true;
                                else if (me.Entity is Mobile m)
                                    m.IsIntMapStorage = true;
                                else if (me.Entity is BaseHouse bh)
                                    bh.IsIntMapStorage = true;
                                else
                                    ;
                            }
                        }

                    // 5. Move everything.
                    if (toMove != null)
                    {
                        foreach (MoveEntry me in toMove)
                        {
                            if (ValidMap(stash, me.Entity))
                            {
                                if (!m_StashComponents.Contains(me.Entity))
                                    m_StashComponents.Add(me.Entity);
                                else
                                    ;

                                // make sure we haven't moved this entity yet
                                if (me.Entity.Map == StashDestMap(stash))
                                    continue;

                                // we have to cast here since IEntity.Location has no setter
                                if (me.Entity is Item item)
                                    item.Map = StashDestMap(stash);
                                else if (me.Entity is Mobile m)
                                    m.Map = StashDestMap(stash);
                                else if (me.Entity is BaseHouse bh)
                                    bh.Map = StashDestMap(stash);
                                else
                                    ;
                            }
                        }
                    }

                }
                else
                {   // we are retrieving the house
                    // mark everything as IsIntMapStorage = false
                    if (m_StashComponents.Count > 0)
                        foreach (object o in m_StashComponents)
                        {   // handle each component of a BaseAddon
                            if (ValidMap(stash, o))
                            {
                                if (o is BaseAddon ba)
                                {
                                    foreach (AddonComponent c in ba.Components)
                                        if (c is Item item)
                                            item.IsIntMapStorage = false;
                                }
                                else
                                {   // all other components
                                    if (o is Item item)
                                        item.IsIntMapStorage = false;
                                    else if (o is Mobile m)
                                        m.IsIntMapStorage = false;
                                    else if (o is BaseHouse bh)
                                        bh.IsIntMapStorage = false;
                                    else
                                        ;
                                }
                            }
                        }

                    foreach (object o in m_StashComponents)
                    {
                        if (ValidMap(stash, o))
                        {
                            if (o is Item)
                                (o as Item).Map = StashDestMap(stash);
                            else if (o is Mobile)
                                (o as Mobile).Map = StashDestMap(stash);
                            else if (o is BaseHouse)
                                (o as BaseHouse).Map = StashDestMap(stash);
                            else
                                ;
                        }
                    }
                }
            }
        }
        private bool ValidMap(bool stash, object thing, bool sanity = true)
        {
            if (thing is Item item || thing is Mobile m)
            {
                if (stash == true && ThingMap(thing) == StashDestMap(stash))
                    return false;
                //else if (sanity)
                //Utility.ConsoleOut("Error: Stash: Bad map for item {0}", ConsoleColor.Red, thing);

                if (stash == false && ThingMap(thing) == StashDestMap(stash))
                    return false;

                return true;
            }
            else
                Utility.ConsoleWriteLine("Error: Stash: Bad object: {0}", ConsoleColor.Red, thing);

            return false;
        }
        private Map ThingMap(object thing)
        {
            if (thing is Item item)
                return item.Map;
            if (thing is Mobile m)
                return m.Map;
            return null;
        }
        public override void OnLocationChange(Point3D oldLocation)
        {
            int dx = this.Location.X - oldLocation.X;
            int dy = this.Location.Y - oldLocation.Y;
            int dz = this.Location.Z - oldLocation.Z;

            Map map = this.Map;

            UpdateRegionArea();

            m_Region.GoLocation = new Point3D(m_Region.GoLocation.X + dx, m_Region.GoLocation.Y + dy, m_Region.GoLocation.Z + dz);

            List<MoveEntry> toMove = null;

            // 1. Find all items/mobiles in the house that we want to move.
            if (map != null)
            {
                toMove = new List<MoveEntry>();

                MultiComponentList mcl = this.Components;

                foreach (object o in map.GetObjectsInBounds(new Rectangle2D(oldLocation.X + mcl.Min.X, oldLocation.Y + mcl.Min.Y, mcl.Width, mcl.Height)))
                {
                    if (o is BaseHouse)
                        continue; // don't move houses

                    if (o is IEntity)
                    {
                        IEntity ent = (IEntity)o;

                        toMove.Add(new MoveEntry(ent, ent.Location));
                    }
                }
            }

            // 2. Move all fixed items explicitly. This ensures that items outside of the MCL are also moved.
            foreach (Item item in GetFixedItems())
                item.Location = new Point3D(item.X + dx, item.Y + dy, item.Z + dz);

            // 3. Move all remaining items/mobiles that we haven't moved yet.
            if (toMove != null)
            {
                foreach (MoveEntry me in toMove)
                {
                    // make sure we haven't moved this entity yet
                    if (me.Entity.Location != me.OldLocation)
                        continue;

                    // we have to cast here since IEntity.Location has no setter
                    if (me.Entity is Item)
                    {
                        Item item = (Item)me.Entity;

                        item.Location = new Point3D(item.X + dx, item.Y + dy, item.Z + dz);
                    }
                    else if (me.Entity is Mobile)
                    {
                        Mobile m = (Mobile)me.Entity;

                        m.Location = new Point3D(m.X + dx, m.Y + dy, m.Z + dz);
                    }
                }
            }
        }

        public struct MoveEntry
        {
            public readonly IEntity Entity;
            public readonly Point3D OldLocation;

            public MoveEntry(IEntity entity, Point3D oldLocation)
            {
                Entity = entity;
                OldLocation = oldLocation;
            }
        }

        #region doors
        public BaseDoor AddEastDoor(int x, int y, int z)
        {
            return AddEastDoor(true, x, y, z);
        }

        public BaseDoor AddEastDoor(bool wood, int x, int y, int z, DoorFacing facing = DoorFacing.SouthCW)
        {
            BaseDoor door = MakeDoor(wood, facing);

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor AddSouthDoor(int x, int y, int z)
        {
            return AddSouthDoor(true, x, y, z);
        }

        public BaseDoor AddSouthDoor(bool wood, int x, int y, int z)
        {
            BaseDoor door = MakeDoor(wood, DoorFacing.WestCW);

            AddDoor(door, x, y, z);

            return door;
        }

        public BaseDoor AddEastDoor(int x, int y, int z, uint k)
        {
            return AddEastDoor(true, x, y, z, k);
        }

        public BaseDoor AddEastDoor(bool wood, int x, int y, int z, uint k)
        {
            BaseDoor door = MakeDoor(wood, DoorFacing.SouthCW);

            door.Locked = true;
            door.KeyValue = k;

            AddDoor(door, x, y, z);

            if (k != 0)
                Recorder(new Point3D(x, y, z), null);

            return door;
        }

        public BaseDoor AddSouthDoor(int x, int y, int z, uint k)
        {
            return AddSouthDoor(true, x, y, z, k);
        }

        public BaseDoor AddSouthDoor(bool wood, int x, int y, int z, uint k)
        {
            BaseDoor door = MakeDoor(wood, DoorFacing.WestCW);

            door.Locked = true;
            door.KeyValue = k;

            AddDoor(door, x, y, z);

            if (k != 0)
                Recorder(new Point3D(x, y, z), null);

            return door;
        }

        public BaseDoor[] AddSouthDoors(int x, int y, int z, uint k)
        {
            return AddSouthDoors(true, x, y, z, k);
        }

        public BaseDoor[] AddSouthDoors(bool wood, int x, int y, int z, uint k)
        {
            BaseDoor westDoor = MakeDoor(wood, DoorFacing.WestCW);
            BaseDoor eastDoor = MakeDoor(wood, DoorFacing.EastCCW);

            westDoor.Locked = true;
            eastDoor.Locked = true;

            westDoor.KeyValue = k;
            eastDoor.KeyValue = k;

            // westDoor.Link = eastDoor;
            // eastDoor.Link = westDoor;

            AddDoor(westDoor, x, y, z);
            AddDoor(eastDoor, x + 1, y, z);

            if (k != 0)
                Recorder(new Point3D(x, y, z), new Point3D(x + 1, y, z));

            return new BaseDoor[2] { westDoor, eastDoor };
        }
        public void Recorder(Point3D p1, Point3D? p2 = null)
        {
            // enable to code to scrape exterior door coords 
            //  We already have the exterior door coords for all old houses.
            // See Also: [Nuke LogExteriorDoors
#if false
            LogHelper logger = new LogHelper("House Construction.log", false, true, true);
            logger.Log(string.Format("{0} {1} {2}", GetType().Name, p1, p2));
            logger.Finish();
#endif
        }
        public uint CreateKeys(Mobile m)
        {
            uint value = Key.RandomValue();

            if (!IsAosRules)
            {
                Key packKey = new Key(KeyType.Gold);
                Key bankKey = new Key(KeyType.Gold);

                packKey.KeyValue = value;
                bankKey.KeyValue = value;

                //packKey.LootType = LootType.Newbied;
                //bankKey.LootType = LootType.Newbied;

                BankBox box = m.BankBox;

                if (box == null || !box.TryDropItem(m, bankKey, false))
                    bankKey.Delete();

                m.AddToBackpack(packKey);
            }

            return value;
        }

        public BaseDoor[] AddSouthDoors(int x, int y, int z)
        {
            return AddSouthDoors(true, x, y, z, false);
        }

        public BaseDoor[] AddSouthDoors(bool wood, int x, int y, int z, bool inv)
        {
            BaseDoor westDoor = MakeDoor(wood, inv ? DoorFacing.WestCCW : DoorFacing.WestCW);
            BaseDoor eastDoor = MakeDoor(wood, inv ? DoorFacing.EastCW : DoorFacing.EastCCW);

            // westDoor.Link = eastDoor;
            // eastDoor.Link = westDoor;

            AddDoor(westDoor, x, y, z);
            AddDoor(eastDoor, x + 1, y, z);

            return new BaseDoor[2] { westDoor, eastDoor };
        }

        public BaseDoor MakeDoor(bool wood, DoorFacing facing)
        {
            if (wood)
                return new DarkWoodHouseDoor(facing);
            else
                return new MetalHouseDoor(facing);
        }

        public void AddDoor(BaseDoor door, int xoff, int yoff, int zoff)
        {
            door.MoveToWorld(new Point3D(xoff + this.X, yoff + this.Y, zoff + this.Z), this.Map);
            m_Doors.Add(door);
        }

        public void RemoveDoor(BaseDoor door)
        {
            if (m_Doors != null && m_Doors.Contains(door))
                m_Doors.Remove(door);
        }

        public void OnDoorOpened(Mobile from, BaseDoor door)
        { /* Siege: Clear the ban house ban list:
           * In addition, if a murderer or flagged criminal opens the door to a house in which a blue player has banned players/monsters, the ban list for the house will be cleared.*
           * https://uo.stratics.com/content/basics/siege_archive.shtml
           * **More Info:**
           * bravata:
           * it means if you or anyone granted co-owner/friend of your home is red or criminal at the time, opens the door,  
           * the ban list is cleared, preventing players from using homes as safe bases to escape fights.
           * bravata:
           * also blues going criminal and trying to escape into a home they are friended to.
           */
            if (Core.RuleSets.SiegeStyleRules() && IsExteriorDoor(door))
            {
                //from.Say("Exterior door");
                if (IsFriend(from) && (from.Criminal || from.IsMurderer))
                    if (m_Bans != null)
                    {
                        m_Bans.Clear();
                        from.SendMessage("All bans have been lifted.");
                    }

            }
            //else
            //from.Say("Interior door");
        }

        private static Dictionary<Type, List<Point3D>> ExteriorDoorLookup = new() {
                { typeof(SmallOldHouse), new List<Point3D>() { new Point3D(0, 3, 7) } },
                { typeof(SmallShop), new List<Point3D>() { /*style 1*/new Point3D(-2, 0, 27), /*style 2*/new Point3D(-2, 0, 24) } },
                { typeof(SmallTower), new List<Point3D>() { new Point3D(3, 3, 6) } },
                { typeof(TwoStoryVilla), new List<Point3D>() { new Point3D(3, 1, 5), new Point3D(4, 1, 5) } },
                { typeof(SandStonePatio), new List<Point3D>() { new Point3D(-1, 3, 6) } },
                { typeof(LogCabin), new List<Point3D>() { new Point3D(1, 4, 8) } },
                { typeof(GuildHouse), new List<Point3D>() { new Point3D(-1, 6, 7), new Point3D(0, 6, 7) } },
                { typeof(TwoStoryHouse), new List<Point3D>() { new Point3D(-3, 6, 7), new Point3D(-2, 6, 7) } },
                { typeof(LargePatioHouse), new List<Point3D>() { new Point3D(-4, 6, 7), new Point3D(-3, 6, 7) } },
                { typeof(LargeMarbleHouse), new List<Point3D>() { new Point3D(-4, 3, 4), new Point3D(-3, 3, 4) } },
                { typeof(Tower), new List<Point3D>() { new Point3D(0, 6, 6), new Point3D(1, 6, 6) } },
                { typeof(Keep), new List<Point3D>() { new Point3D(0, 10, 6), new Point3D(1, 10, 6) } },
                { typeof(Castle), new List<Point3D>() { new Point3D(0, 15, 6), new Point3D(1, 15, 6) } },
            };
        public bool IsExteriorDoor(BaseDoor door)
        {
            MultiComponentList mcl = this.Components;
            /*
                SmallOldHouse (0, 3, 7) 
                SmallOldHouse (0, 3, 7) 
                SmallOldHouse (0, 3, 7) 
                SmallOldHouse (0, 3, 7) 
                SmallOldHouse (0, 3, 7) 
                SmallOldHouse (0, 3, 7) 
                SmallOldHouse (0, 3, 7) 
                SmallShop (-2, 0, 27) 
                SmallShop (-2, 0, 24) 
                SmallTower (3, 3, 6) 
                TwoStoryVilla (3, 1, 5) (4, 1, 5)
                SandStonePatio (-1, 3, 6) 
                LogCabin (1, 4, 8) 
                GuildHouse (-1, 6, 7) (0, 6, 7)
                TwoStoryHouse (-3, 6, 7) (-2, 6, 7)
                TwoStoryHouse (-3, 6, 7) (-2, 6, 7)
                LargePatioHouse (-4, 6, 7) (-3, 6, 7)
                LargeMarbleHouse (-4, 3, 4) (-3, 3, 4)
                Tower (0, 6, 6) (1, 6, 6)
                Keep (0, 10, 6) (1, 10, 6)
                Castle (0, 15, 6) (1, 15, 6)
             */

            int StartX = this.X + mcl.Min.X;
            int StartY = this.Y + mcl.Min.Y;
            bool Xok = false;
            bool Yok = false;
            if (ExteriorDoorLookup.ContainsKey(this.GetType()))
            {
                foreach (var px in ExteriorDoorLookup[this.GetType()])
                {
                    Xok = Math.Abs((StartX + px.X + mcl.Center.X) - door.X) < 2;
                    Yok = Math.Abs((StartY + px.Y + mcl.Center.Y) - door.Y) < 2;
                    if (Xok && Yok)
                        break;
                }
            }

            return (Xok && Yok);
        }
        #endregion doors

        private bool OnSteps(Mobile from)
        {
            StaticTile[] staticTiles = from.Map.Tiles.GetStaticTiles(from.X, from.Y, true);
            if (staticTiles.Length > 0)
                foreach (StaticTile t in staticTiles)
                    if (t.ID == 18233)  // add your other steps here
                        if (Math.Abs(from.Z - t.Z) < 3)
                            return true;
            return false;
        }
        public void AddTrashBarrel(Mobile from)
        {
            bool onSteps = OnSteps(from);
            for (int i = 0; m_Doors != null && i < m_Doors.Count; ++i)
            {
                BaseDoor door = m_Doors[i] as BaseDoor;
                Point3D p = door.Location;

                if (door.Open)
                    p = new Point3D(p.X - door.Offset.X, p.Y - door.Offset.Y, p.Z - door.Offset.Z);

                if ((from.Z + 16) >= p.Z && (p.Z + 16) >= from.Z)
                {
                    if (from.InRange(p, 1) || onSteps)
                    {
                        from.SendLocalizedMessage(502120); // You cannot place a trash barrel near a door or near steps.
                        return;
                    }
                }
            }

            if (m_Trash == null || m_Trash.Deleted)
            {
                m_Trash = new TrashBarrel();

                m_Trash.Movable = false;
                m_Trash.MoveToWorld(from.Location, from.Map);

                from.SendLocalizedMessage(502121); /* You have a new trash barrel.
													  * Three minutes after you put something in the barrel, the trash will be emptied.
													  * Be forewarned, this is permanent! */
            }
            else
            {
                m_Trash.MoveToWorld(from.Location, from.Map);
            }
        }

        public void SetSign(int xoff, int yoff, int zoff)
        {
            m_Sign = new HouseSign(this);
            m_Sign.MoveToWorld(new Point3D(this.X + xoff, this.Y + yoff, this.Z + zoff), this.Map);
        }

        public void SetLockdown(Item i, bool locked)
        {
            SetLockdown(i, locked, false);
        }

        private void SetLockdown(Item i, bool locked, bool checkContains)
        {
            if (m_LockDowns == null)
                return;

            i.Movable = !locked;
            i.IsLockedDown = locked;

            if (locked)
                m_LockDowns.Add(i);
            else
                m_LockDowns.Remove(i);

            if (LockboxSystem && IsLockbox(i))
                ToggleLockbox(i, locked, checkContains);

            if (!locked)
                i.SetLastMoved();

            if (locked)
                i.OnLockdown();
            else
                i.OnRelease();
        }

        public bool LockDown(Mobile m, Item item)
        {
            return LockDown(m, item, true);
        }

        public bool LockDown(Mobile m, Item item, bool checkIsInside)
        {
            if (!IsFriend(m))
                return false;

            if (item.Movable && !IsSecure(item))
            {
                int amt;
                if (item is SturdyFancyArmoire || item is SturdyArmoire)    // Adam: Sturdy Armoires only count 20% of items
                    amt = 1 + (item.TotalItems / 5);
                else if (item is BaseContainer)                             // wea: 14/Aug/2006 modified so containers are treated as a single item
                    amt = 1;
                else if (item is SeedBox seedBox)                           // 8/14/2023, Adam: Box + seeds / 5
                    amt = 1 + (seedBox.SeedCount() / 5);
                else if (item is Library)                                   // 8/14/2023, Adam: Library + books / 2
                    amt = 1 + (item.TotalItems / 2);
                else
                    amt = 1 + item.TotalItems;

                Item rootItem = item.RootParent as Item;
                Item parentItem = item.Parent as Item;

                if (checkIsInside && item.RootParent is Mobile)
                {
                    m.SendLocalizedMessage(1005525);//That is not in your house
                }
                else if (checkIsInside && !IsInside(item.GetWorldLocation(), item.ItemData.Height))
                {
                    m.SendLocalizedMessage(1005525);//That is not in your house
                }
                else if (Ethics.Ethic.IsImbued(item))
                {
                    m.SendLocalizedMessage(1005377);//You cannot lock that down
                }
                else if (IsSecure(rootItem))
                {
                    m.SendLocalizedMessage(501737); // You need not lock down items in a secure container.
                }
                // Yoar: The following code is compatible with the lockbox system. But let's disable it anyways...
#if false
                else if (LockboxSystem && rootItem != null && IsLockedDown(rootItem) && IsLockbox(rootItem))
                {
                    m.SendMessage("You cannot lock down items inside lockboxes.");
                }
                else if (parentItem != null && !IsLockedDown(parentItem))
                {
                    m.SendLocalizedMessage(501736); // You must lockdown the container first!
                }
#else
                //Pix: In order to eliminate an exploit where players can create non-movable objects anywhere
                // in the world, we'll make then not be able to lock down items inside containers.
                // If the item is not in a container, then the rootItem will be null.
                else if (rootItem != null)
                {
                    m.SendMessage("You cannot lock down items inside containers.");
                }
#endif
                else if (IsAosRules ? (!CheckAosLockdowns(amt) || !CheckAosStorage(amt)) : (this.SumLockDownSecureCount + amt) > this.MaxLockDowns)
                {
                    m.SendLocalizedMessage(1005379);//That would exceed the maximum lock down limit for this house
                }
                else if (item is BaseWaterContainer bwc && bwc.Quantity != 0)
                {
                    item.SendMessageTo(m, false, "That must be empty before you can lock it down.");
                }
                else if (!LockboxSystem || !IsLockbox(item) || CanMakeLockbox(m, item))
                {
                    item.SendMessageTo(m, false, 0x3B2, "(locked down)");
                    SetLockdown(item, true);
                    return true;
                }
            }
            else if (m_LockDowns.IndexOf(item) != -1)
            {
                m.SendLocalizedMessage(1005526);//That is already locked down
                return true;
            }
            else
            {
                m.SendLocalizedMessage(1005377);//You cannot lock that down
            }

            return false;
        }

        private class TransferItem : Item
        {
            private BaseHouse m_House;

            public TransferItem(BaseHouse house)
                : base(0x14F0)
            {
                m_House = house;

                Hue = 0x480;
                Movable = false;
                Name = "a house transfer contract";
            }

            public override void GetProperties(ObjectPropertyList list)
            {
                base.GetProperties(list);

                string houseName, owner, location;

                Item sign = (m_House == null ? null : m_House.Sign);

                if (sign == null || sign.Name == null || sign.Name == "a house sign")
                    houseName = "nothing";
                else
                    houseName = sign.Name;

                Mobile houseOwner = (m_House == null ? null : m_House.Owner);

                if (houseOwner == null)
                    owner = "nobody";
                else
                    owner = houseOwner.Name;

                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;

                bool valid = m_House != null && Sextant.Format(m_House.Location, m_House.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

                if (valid)
                    location = String.Format("{0}� {1}'{2}, {3}� {4}'{5}", yLat, yMins, ySouth ? "S" : "N", xLong, xMins, xEast ? "E" : "W");
                else
                    location = "????";

                list.Add(1061112, Utility.FixHtml(houseName)); // House Name: ~1_val~
                list.Add(1061113, owner); // Owner: ~1_val~
                list.Add(1061114, location); // Location: ~1_val~
            }

            public TransferItem(Serial serial)
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

            public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (!base.AllowSecureTrade(from, to, newOwner, accepted))
                    return false;
                else if (!accepted)
                    return true;

                if (Deleted || m_House == null || m_House.Deleted || !m_House.IsOwner(from) || !from.CheckAlive() || !to.CheckAlive())
                    return false;

                if (BaseHouse.HasAccountHouse(to))
                {
                    from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
                    return false;
                }

                TownshipRegion tsr = TownshipRegion.GetTownshipAt(from);

                if (tsr != null && tsr.TStone != null && !tsr.TStone.IsMember(to))
                {
                    from.SendMessage("You cannot tranfer a house within the {0} township to a person who is not a member of that guild.", tsr.TStone.GuildAbbreviation);
                    return false;
                }

                return m_House.CheckTransferPosition(from, to);
            }

            public override void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (Deleted || m_House == null || m_House.Deleted || !m_House.IsOwner(from) || !from.CheckAlive() || !to.CheckAlive())
                    return;

                Delete();

                if (!accepted)
                    return;

                from.SendLocalizedMessage(501338); // You have transferred ownership of the house.
                to.SendLocalizedMessage(501339); /* You are now the owner of this house.
													* The house's co-owner, friend, ban, and access lists have been cleared.
													* You should double-check the security settings on any doors and teleporters in the house.
													*/

                m_House.RemoveKeys(from);
                m_House.Owner = to;
                m_House.Bans.Clear();
                m_House.Friends.Clear();
                m_House.CoOwners.Clear();
                if (m_House.Public == false)
                    m_House.ChangeLocks(to);
            }
        }

        public bool CheckTransferPosition(Mobile from, Mobile to)
        {
            bool isValid = true;
            Item sign = m_Sign;
            Point3D p = (sign == null ? Point3D.Zero : sign.GetWorldLocation());

            if (from.Map != Map || to.Map != Map)
                isValid = false;
            else if (sign == null)
                isValid = false;
            else if (from.Map != sign.Map || to.Map != sign.Map)
                isValid = false;
            else if (IsInside(from))
                isValid = false;
            else if (IsInside(to))
                isValid = false;
            else if (!from.InRange(p, 2))
                isValid = false;
            else if (!to.InRange(p, 2))
                isValid = false;

            if (!isValid)
                from.SendLocalizedMessage(1062067); // In order to transfer the house, you and the recipient must both be outside the building and within two paces of the house sign.

            return isValid;
        }

        public void BeginConfirmTransfer(Mobile from, Mobile to)
        {
            if (Deleted || !from.CheckAlive() || !IsOwner(from))
                return;

            if (from == to)
            {
                from.SendLocalizedMessage(1005330); // You cannot transfer a house to yourself, silly.
            }
            else if (to.Player)
            {
                //SMD: township check addition
                bool bTownshipGuildCheckPassed = true;
                string guildname = "";

                try //safety
                {
                    TownshipRegion tsr = TownshipRegion.GetTownshipAt(from);
                    if (tsr != null && tsr.TStone != null && !tsr.TStone.IsMember(to))
                    {
                        guildname = tsr.TStone.GuildAbbreviation;
                        bTownshipGuildCheckPassed = false;
                    }
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                //SMD: end township check addition

                if (BaseHouse.HasAccountHouse(to))
                {
                    from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
                }
                //SMD: township check addition
                else if (!bTownshipGuildCheckPassed)
                {
                    from.SendMessage("You cannot tranfer a house within the {0} township to a person who is not a member of that guild.", guildname);
                }
                //SMD: end township check addition
                else if (CheckTransferPosition(from, to))
                {
                    from.SendLocalizedMessage(1005326); // Please wait while the other player verifies the transfer.
                    to.SendGump(new Gumps.HouseTransferGump(from, to, this));
                }
            }
            else
            {
                from.SendLocalizedMessage(501384); // Only a player can own a house!
            }
        }

        public void AdminTransfer(Mobile to)
        {
            Mobile from = Owner == null ? to : Owner;
            new TransferItem(this).OnSecureTrade(from, to, null, true);
        }

        public void EndConfirmTransfer(Mobile from, Mobile to)
        {
            if (Deleted || !from.CheckAlive() || !IsOwner(from))
                return;

            if (from == to)
            {
                from.SendLocalizedMessage(1005330); // You cannot transfer a house to yourself, silly.
            }
            else if (to.Player)
            {
                if (BaseHouse.HasAccountHouse(to))
                {
                    from.SendLocalizedMessage(501388); // You cannot transfer ownership to another house owner or co-owner!
                }
                else if (CheckTransferPosition(from, to))
                {
                    /*
					NetState fromState = from.NetState, toState = to.NetState;

					if ( fromState != null && toState != null )
					{
						Container c = fromState.TradeWith( toState );

						c.DropItem( new TransferItem( this ) );
					}
					*/
                    NetState fromState = from.NetState, toState = to.NetState;

                    if (fromState != null && toState != null)
                    {
                        if (from.HasTrade)
                        {
                            from.SendLocalizedMessage(1062071); // You cannot trade a house while you have other trades pending.
                        }
                        else if (to.HasTrade)
                        {
                            to.SendLocalizedMessage(1062071); // You cannot trade a house while you have other trades pending.
                        }
                        else
                        {
                            Container c = fromState.AddTrade(toState);

                            c.DropItem(new TransferItem(this));
                        }
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(501384); // Only a player can own a house!
            }
        }

        public void Release(Mobile m, Item item, bool checkWater = true)
        {
            if (!IsFriend(m))
                return;

            if (IsLockedDown(item))
            {
                if (checkWater && item is BaseWaterContainer bwc && bwc.Quantity != 0)
                {
                    item.SendMessageTo(m, false, "That must be empty before you can release it.");
                }
                else
                {
#if RunUO
                item.PublicOverheadMessage(Server.Network.MessageType.Label, 0x3B2, 501657);//[no longer locked down]
#else
                    item.SendLocalizedMessageTo(m, 501657); // (no longer locked down)
#endif
                    SetLockdown(item, false);
                    //TidyItemList( m_LockDowns );
                }
            }
            else if (IsSecure(item))
            {
                if (item is TentBackpack)
                    m.SendMessage("This is part of your tent and may not be managed in this way.");
                else
                    ReleaseSecure(m, item);
            }
            else
            {
                m.SendLocalizedMessage(501722);//That isn't locked down...
            }
        }

        public void AddSecure(Mobile m, Item item)
        {
            if (m_Secures == null || !IsCoOwner(m))
                return;

            if (!IsInside(item))
            {
                m.SendLocalizedMessage(1005525); // That is not in your house
            }
            else if (IsLockedDown(item))
            {
                m.SendLocalizedMessage(1010550); // This is already locked down and cannot be secured.
            }
            else if (!(item is Container))
            {
                LockDown(m, item);
            }
            else
            {
                SecureInfo info = null;

                for (int i = 0; info == null && i < m_Secures.Count; ++i)
                    if (((SecureInfo)m_Secures[i]).Item == item)
                        info = (SecureInfo)m_Secures[i];

                if (info != null)
                {
                    m.SendGump(new Gumps.SetSecureLevelGump(m_Owner, info));
                }
                else if (item.Parent != null)
                {
                    m.SendLocalizedMessage(1010423); // You cannot secure this, place it on the ground first.
                }
                else if (!item.Movable)
                {
                    m.SendLocalizedMessage(1010424); // You cannot secure this.
                }
                else if (!IsAosRules && SecureCount >= MaxSecures)
                {
                    // The maximum number of secure items has been reached : 
                    m.SendLocalizedMessage(1008142, true, MaxSecures.ToString());
                }
                else if (IsAosRules ? !CheckAosLockdowns(1) : ((SumLockDownSecureCount + 125) > MaxLockDowns))
                {
                    m.SendLocalizedMessage(1005379); // That would exceed the maximum lock down limit for this house
                }
                else if (IsAosRules && !CheckAosStorage(item.TotalItems))
                {
                    m.SendLocalizedMessage(1061839); // This action would exceed the secure storage limit of the house.
                }
                else
                {
                    item.SendMessageTo(m, false, 0x3B2, "(secured)");

                    info = new SecureInfo((Container)item, SecureLevel.CoOwners);

                    item.IsLockedDown = false;
                    item.IsSecure = true;

                    m_Secures.Add(info);
                    m_LockDowns.Remove(item);
                    item.Movable = false;

                    item.OnLockdown();

                    m.SendGump(new Gumps.SetSecureLevelGump(m_Owner, info));
                }
            }
        }

        public bool HasSecureAccess(Mobile m, SecureLevel level)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            switch (level)
            {
                case SecureLevel.Owner: return IsOwner(m);
                case SecureLevel.CoOwners: return IsCoOwner(m);
                case SecureLevel.Friends: return IsFriend(m);
                case SecureLevel.Anyone: return true;
            }

            return false;
        }

        public void ReleaseSecure(Mobile m, Item item)
        {
            if (m_Secures == null || !IsOwner(m) || item is StrongBox)
                return;

            for (int i = 0; i < m_Secures.Count; ++i)
            {
                SecureInfo info = (SecureInfo)m_Secures[i];

                if (info.Item == item && HasSecureAccess(m, info.Level))
                {
                    item.IsLockedDown = false;
                    item.IsSecure = false;
                    item.Movable = true;
                    item.SetLastMoved();
                    item.PublicOverheadMessage(Server.Network.MessageType.Label, 0x3B2, 501656);//[no longer secure]
                    m_Secures.RemoveAt(i);
                    item.OnRelease();
                    return;
                }
            }

            m.SendLocalizedMessage(501717);//This isn't secure...
        }

        public void ReleaseSecure(Item item)
        {
            if (m_Secures == null)
                return;

            for (int i = 0; i < m_Secures.Count; ++i)
            {
                SecureInfo info = (SecureInfo)m_Secures[i];

                if (info.Item == item)
                {
                    item.IsLockedDown = false;
                    item.IsSecure = false;
                    item.Movable = true;
                    item.SetLastMoved();
                    m_Secures.RemoveAt(i);
                    item.OnRelease();
                    return;
                }
            }
        }

        public override bool Decays
        {
            get
            {
                return false;
            }
        }

        public void AddStrongBox(Mobile from)
        {
            bool onSteps = OnSteps(from);
            if (!IsCoOwner(from))
                return;

            if (from == Owner)
            {
                from.SendLocalizedMessage(502109); // Owners don't get a strong box
                return;
            }

            if (IsAosRules ? !CheckAosLockdowns(1) : ((SumLockDownSecureCount + 1) > this.MaxLockDowns))
            {
                from.SendLocalizedMessage(1005379);//That would exceed the maximum lock down limit for this house
                return;
            }

            foreach (SecureInfo info in m_Secures)
            {
                Container c = info.Item;

                if (!c.Deleted && c is StrongBox && ((StrongBox)c).Owner == from)
                {
                    from.SendLocalizedMessage(502112);//You already have a strong box
                    return;
                }
            }

            for (int i = 0; m_Doors != null && i < m_Doors.Count; ++i)
            {
                BaseDoor door = m_Doors[i] as BaseDoor;
                Point3D p = door.Location;

                if (door.Open)
                    p = new Point3D(p.X - door.Offset.X, p.Y - door.Offset.Y, p.Z - door.Offset.Z);

                if ((from.Z + 16) >= p.Z && (p.Z + 16) >= from.Z)
                {
                    if (from.InRange(p, 1) || onSteps)
                    {
                        from.SendLocalizedMessage(502113); // You cannot place a strongbox near a door or near steps.
                        return;
                    }
                }
            }

            StrongBox sb = new StrongBox(from, this);
            sb.Movable = false;
            sb.IsLockedDown = false;
            sb.IsSecure = true;
            m_Secures.Add(new SecureInfo(sb, SecureLevel.CoOwners));
            sb.MoveToWorld(from.Location, from.Map);
        }

        public void Kick(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || m_Friends == null)
                return;
            // Murderers and criminals will not be able to kick/ban others from their home, nor will they be able to log out instantly
            // https://www.uoguide.com/Siege_Perilous
            if (from.IsMurderer && Core.RuleSets.SiegeStyleRules())
                from.SendMessage("A murderer cannot eject anyone from the house!");
            else if (from.Criminal && Core.RuleSets.SiegeStyleRules())
                from.SendMessage("A criminal cannot eject anyone from the house!");
            else if (targ.AccessLevel > AccessLevel.Player && from.AccessLevel <= targ.AccessLevel && targ != from)
            {
                from.SendLocalizedMessage(501346); // Uh oh...a bigger boot may be required!
            }
            else if (IsFriend(targ) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendLocalizedMessage(501348); // You cannot eject a friend of the house!
            }
            else if (targ is PlayerVendor || targ is PlayerBarkeeper)
            {
                from.SendLocalizedMessage(501351); // You cannot eject a vendor.
            }
            else if (!IsInside(targ))
            {
                from.SendLocalizedMessage(501352); // You may not eject someone who is not in your house!
            }
            else
            {
                targ.MoveToWorld(BanLocation, Map);

                from.SendLocalizedMessage(1042840, targ.Name); // ~1_PLAYER NAME~ has been ejected from this house.
                targ.SendLocalizedMessage(501341); /* You have been ejected from this house.
													  * If you persist in entering, you may be banned from the house.
													  */
            }
        }

        public void RemoveAccess(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || m_Access == null)
                return;

            if (m_Access.Contains(targ))
            {
                m_Access.Remove(targ);

                if (!HasAccess(targ) && IsInside(targ))
                {
                    targ.Location = BanLocation;
                    targ.SendLocalizedMessage(1060734); // Your access to this house has been revoked.
                }

                from.SendLocalizedMessage(1050051); // The invitation has been revoked.
            }
        }

        public void RemoveBan(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || m_Bans == null)
                return;

            if (m_Bans.Contains(targ))
            {
                m_Bans.Remove(targ);

                from.SendLocalizedMessage(501297); // The ban is lifted.
            }
        }

        public void Ban(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || m_Bans == null)
                return;

            // Murderers and criminals will not be able to kick/ban others from their home, nor will they be able to log out instantly
            // https://www.uoguide.com/Siege_Perilous
            if (from.IsMurderer && Core.RuleSets.SiegeStyleRules())
                from.SendMessage("A murderer cannot ban anyone from the house!");
            else if (from.Criminal && Core.RuleSets.SiegeStyleRules())
                from.SendMessage("A criminal cannot ban anyone from the house!");
            else if ((targ.AccessLevel > AccessLevel.Player && from.AccessLevel <= targ.AccessLevel) || targ is BaseGuard)
            {
                from.SendLocalizedMessage(501354); // Uh oh...a bigger boot may be required.
            }
            else if (TownshipNPCHelper.IsTownshipNPC(targ))
            {
                from.SendLocalizedMessage(1062040); // You cannot ban that.
            }
            else if (IsFriend(targ))
            {
                from.SendLocalizedMessage(501348); // You cannot eject a friend of the house!
            }
            else if (targ is PlayerVendor || targ is PlayerBarkeeper)
            {
                from.SendLocalizedMessage(501351); // You cannot eject a vendor.
            }
            else if (m_Bans.Count >= MaxBans)
            {
                from.SendLocalizedMessage(501355); // The ban limit for this house has been reached!
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501356); // This person is already banned!
            }
            else if (!IsInside(targ))
            {
                from.SendLocalizedMessage(501352); // You may not eject someone who is not in your house!
            }
            else if (!Public && IsAosRules)
            {
                from.SendLocalizedMessage(1062521); // You cannot ban someone from a private house.  Revoke their access instead.
            }
            else if (targ is BaseCreature && ((BaseCreature)targ).NoHouseRestrictions)
            {
                from.SendLocalizedMessage(1062040); // You cannot ban that.
            }
            else if (from.Aggressed.Count > 0)
            {
                bool allowBan = true;
                for (int i = 0; i < from.Aggressed.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)from.Aggressed[i];
                    if (info.Defender == targ)
                    {
                        allowBan = false;
                        break;
                    }
                }

                if (!allowBan)
                    from.SendMessage("You cannot ban someone while in the heat of battle!");
                else
                {
                    m_Bans.Add(targ);

                    from.SendLocalizedMessage(1042839, targ.Name); // ~1_PLAYER_NAME~ has been banned from this house.
                    targ.SendLocalizedMessage(501340); // You have been banned from this house.

                    targ.MoveToWorld(BanLocation, Map);
                }

            }
            else if (from.Criminal)
            {
                from.SendMessage("You cannot ban from your house while flagged criminal!");
            }
            else
            {
                m_Bans.Add(targ);

                from.SendLocalizedMessage(1042839, targ.Name); // ~1_PLAYER_NAME~ has been banned from this house.
                targ.SendLocalizedMessage(501340); // You have been banned from this house.

                targ.Location = BanLocation;
                targ.Map = Map;
            }
        }

        public PlayerMobile FindPlayer()
        {
            if (m_Region == null)
                return null;

            foreach (Mobile mx in m_Region.Players.Values)
            {
                PlayerMobile pm = mx as PlayerMobile;
                if (pm != null && Contains(pm))
                    return pm;
            }

            return null;
        }

        public PlayerVendor FindPlayerVendor()
        {
            if (m_Region == null)
                return null;

            //Console.WriteLine("Looping through mobiles in region... {0}", r);
            foreach (Mobile mx in m_Region.Mobiles.Values)
            {
                PlayerVendor pv = mx as PlayerVendor;
                //Console.WriteLine(list[i]);

                if (pv != null) // wea: removed Contains() check... we already know it's the right place
                    return pv;      // and this screws up tents
            }

            return null;
        }

        public void GrantAccess(Mobile from, Mobile targ)
        {
            if (!IsFriend(from) || m_Access == null)
                return;

            if (HasAccess(targ))
            {
                from.SendLocalizedMessage(1060729); // That person already has access to this house.
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(1060712); // That is not a player.
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501367); // This person is banned!  Unban them first.
            }
            else
            {
                m_Access.Add(targ);

                targ.SendLocalizedMessage(1060735); // You have been granted access to this house.
            }
        }

        public DateTime GetMembershipExpiration(Mobile m)
        {
            if (m == null || !IsMember(m))
                return DateTime.MinValue; // not a member
            // extend membership by ts.
            return m_Memberships[m];
        }
        public void ExtendMembership(Mobile m, TimeSpan ts)
        {
            if (m == null || !IsMember(m))
                return; // not a member
            // extend membership by ts.
            m_Memberships[m] += ts;
        }
        public void AddMember(Mobile m, TimeSpan ts)
        {
            if (m == null || IsMember(m))
                return; // already a member
            // initial membership is ts.
            m_Memberships.Add(m, DateTime.UtcNow + ts);
        }
        public void RemoveMember(Mobile m)
        {
            if (m == null || !IsMember(m))
                return;

            m_Memberships.Remove(m);

            // now cleanup storage
            Item[] items = FindAllItems(typeof(MultiUserStrongBox));
            if (items != null)
                foreach (Item item in items)
                    if (item is MultiUserStrongBox musb)
                    {   // currently we only support one MultiUserMemberStorage per house.
                        if (musb.FindMemberStorage(m) != null)
                            musb.RemoveMemberStorage(m, this);
                    }
        }

        public bool HasMembershipExpired(Mobile m)
        {
            if (m == null || !IsMember(m))
                return false;

            if (DateTime.UtcNow > m_Memberships[m])
                return true;
            return false;
        }

        public void AddCoOwner(Mobile from, Mobile targ)
        {
            if (!IsOwner(from) || m_CoOwners == null || m_Friends == null)
                return;

            if (IsOwner(targ))
            {
                from.SendLocalizedMessage(501360); // This person is already the house owner!
            }
            else if (m_Friends.Contains(targ))
            {
                from.SendLocalizedMessage(501361); // This person is a friend of the house. Remove them first.
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(501362); // That can't be a co-owner of the house.
            }
            //			else if ( HasAccountHouse( targ ) )
            //			{
            //				from.SendLocalizedMessage( 501364 ); // That person is already a house owner.
            //			}
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501367); // This person is banned!  Unban them first.
            }
            else if (m_CoOwners.Count >= MaxCoOwners)
            {
                from.SendLocalizedMessage(501368); // Your co-owner list is full!
            }
            else if (m_CoOwners.Contains(targ))
            {
                from.SendLocalizedMessage(501369); // This person is already on your co-owner list!
            }
            else
            {
                m_CoOwners.Add(targ);

                targ.Delta(MobileDelta.Noto);
                targ.SendLocalizedMessage(501343); // You have been made a co-owner of this house.
            }
        }

        public void RemoveCoOwner(Mobile from, Mobile targ)
        {
            if (!IsOwner(from) || m_CoOwners == null)
                return;

            if (m_CoOwners.Contains(targ))
            {
                m_CoOwners.Remove(targ);

                targ.Delta(MobileDelta.Noto);

                from.SendLocalizedMessage(501299); // Co-owner removed from list.
                targ.SendLocalizedMessage(501300); // You have been removed as a house co-owner.

                foreach (SecureInfo info in m_Secures)
                {
                    Container c = info.Item;

                    if (c is StrongBox && ((StrongBox)c).Owner == targ)
                    {
                        c.IsLockedDown = false;
                        c.IsSecure = false;
                        m_Secures.Remove(c);
                        c.Destroy();
                        break;
                    }
                }
            }
        }

        public void AddFriend(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || m_Friends == null || m_CoOwners == null)
                return;

            if (IsOwner(targ))
            {
                from.SendLocalizedMessage(501370); // This person is already an owner of the house!
            }
            else if (m_CoOwners.Contains(targ))
            {
                from.SendLocalizedMessage(501369); // This person is already on your co-owner list!
            }
            else if (!targ.Player)
            {
                from.SendLocalizedMessage(501371); // That can't be a friend of the house.
            }
            else if (IsBanned(targ))
            {
                from.SendLocalizedMessage(501374); // This person is banned!  Unban them first.
            }
            else if (m_Friends.Count >= MaxFriends)
            {
                from.SendLocalizedMessage(501375); // Your friends list is full!
            }
            else if (m_Friends.Contains(targ))
            {
                from.SendLocalizedMessage(501376); // This person is already on your friends list!
            }
            else
            {
                m_Friends.Add(targ);

                targ.Delta(MobileDelta.Noto);
                targ.SendLocalizedMessage(501337); // You have been made a friend of this house.
            }
        }

        public void RemoveFriend(Mobile from, Mobile targ)
        {
            if (!IsCoOwner(from) || m_Friends == null)
                return;

            if (m_Friends.Contains(targ))
            {
                m_Friends.Remove(targ);

                targ.Delta(MobileDelta.Noto);

                from.SendLocalizedMessage(501298); // Friend removed from list.
                targ.SendLocalizedMessage(1060751); // You are no longer a friend of this house.
            }
        }
        private enum StashTypes
        {
            mobile,
            item,
            baseHouse
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)27); // version

            writer.Write((int)m_BonusLockDowns);
            writer.Write((int)m_BonusSecures);

            // version 26 - Adam: Stashed house Components
            writer.Write(m_StashComponents.Count);
            foreach (object o in m_StashComponents)
            {
                if (o is Item)
                {
                    writer.Write((int)StashTypes.item);
                    writer.Write(o as Item);
                }
                else if (o is Mobile)
                {
                    writer.Write((int)StashTypes.mobile);
                    writer.Write(o as Mobile);
                }
                else if (o is BaseHouse)
                {
                    writer.Write((int)StashTypes.baseHouse);
                    writer.Write(o as Item);
                }
            }

            // version 25 - Yoar: Lockbox data restructure
            writer.Write((int)m_BonusLockboxes);
            writer.Write((int)m_StorageTaxCredits);

            // version 24
            writer.Write(m_MembershipOnly);
            writer.Write((int)m_Memberships.Count);
            foreach (KeyValuePair<Mobile, DateTime> kvp in m_Memberships)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Write((int)m_cooperativeType);

            // version 23 - adam
            //writer.Write((int)m_cooperativeType);

            // version 22 - Adam  (obsolete)
            // players with a house membership get a secure container, but no other rights
            //  In member only houses, only members can enter (as well as friends)
            //writer.Write(m_MembershipOnly);
            //writer.WriteMobileList(m_Memberships, true);

            // version 21 - Adam
            // record a unique id that ties this house to an account to support inheritance
            writer.Write(m_AccountCode);

            // version 20 - Adam
            writer.Write(m_NPCData);

            // version 19 - Adam
            writer.Write(m_RestartDecay);

            //version 18 - Adam
            writer.Write((System.UInt32)m_BoolTable);

            //version 17 - Adam
            writer.Write(m_UpgradeCosts);

            //version 16 - Adam
            //writer.Write(m_LockboxData);

            //version 15 - Pix
            writer.Write(m_SecurePremises);

            //version 14 - TK - store bool if IDOC Announcement is running
            writer.Write((bool)(m_IDOC_Broadcast_TCE != null));

            //version 13 - Pix. - store minutes instead of timespan
            writer.Write(m_DecayMinutesStored);

            //version 12 - Pix. - house decay variables
            //writer.WriteDeltaTime( StructureDecayTime );
            writer.Write(m_NeverDecay);
            //end version 12 additions

            writer.Write(m_MaxLockboxes);

            // use the Property to insure we have an accurate count
            writer.Write(LockBoxCount);

            writer.Write((int)m_Visits);

            writer.Write((int)m_Price);

            writer.WriteMobileList(m_Access);

            writer.Write(m_BuiltOn);
            writer.Write(m_LastTraded);

            writer.WriteItemList(m_Addons, true);

            writer.Write(m_Secures.Count);

            for (int i = 0; i < m_Secures.Count; ++i)
                ((SecureInfo)m_Secures[i]).Serialize(writer);

            writer.Write(m_Public);

            writer.Write(BanLocation);

            writer.Write(m_Owner);

            // Version 5 no longer serializes region coords
            /*writer.Write( (int)m_Region.Coords.Count );
			foreach( Rectangle2D rect in m_Region.Coords )
			{
				writer.Write( rect );
			}*/

            writer.WriteMobileList(m_CoOwners, true);
            writer.WriteMobileList(m_Friends, true);
            writer.WriteMobileList(m_Bans, true);

            writer.Write(m_Sign);
            writer.Write(m_Trash);

            writer.WriteItemList(m_Doors, true);
            writer.WriteItemList(m_LockDowns, true);
            //writer.WriteItemList( m_Secures, true );

            writer.Write((int)m_MaxLockDowns);
            writer.Write((int)m_MaxSecures);

            // Yoar: The following code is compatible with the lockbox system. But let's disable it anyways...
#if false
            // Items in locked down containers that aren't locked down themselves must decay!
            for (int i = 0; i < m_LockDowns.Count; ++i)
            {
                Item item = (Item)m_LockDowns[i];

                if ( item is Container && !(item is BaseBoard) && !IsLockbox(item))
                {
                    Container cont = (Container)item;
                    ArrayList children = cont.Items;

                    for ( int j = 0; j < children.Count; ++j )
                    {
                        Item child = (Item)children[j];

                        if ( child.Decays && !child.IsLockedDown && !child.IsSecure && (child.LastMoved + child.DecayTime) <= DateTime.UtcNow )
                            Timer.DelayCall( TimeSpan.Zero, new TimerCallback( child.Delete ) );
                    }
                }
            }
#endif
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            bool idocannc = false;
            m_Region = new HouseRegion(this);

            switch (version)
            {
                case 27:
                    {
                        m_BonusLockDowns = reader.ReadInt();
                        m_BonusSecures = reader.ReadInt();

                        goto case 26;
                    }
                case 26:
                    {
                        m_StashComponents.Clear();
                        int count = reader.ReadInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            StashTypes t = (StashTypes)reader.ReadInt();
                            switch (t)
                            {
                                case StashTypes.item:
                                    {
                                        m_StashComponents.Add(reader.ReadItem());
                                        break;
                                    }
                                case StashTypes.mobile:
                                    {
                                        m_StashComponents.Add(reader.ReadMobile());
                                        break;
                                    }
                                case StashTypes.baseHouse:
                                    {
                                        m_StashComponents.Add(reader.ReadItem());
                                        break;
                                    }
                            }
                        }
                        goto case 25;
                    }
                case 25:
                    {
                        m_BonusLockboxes = reader.ReadInt();
                        m_StorageTaxCredits = reader.ReadInt();
                        goto case 24;
                    }
                case 24:
                    {
                        // members only club?
                        m_MembershipOnly = reader.ReadBool();

                        // read the member list and expiration date
                        int count = reader.ReadInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            Mobile m = reader.ReadMobile();
                            DateTime dt = reader.ReadDateTime();
                            if (m != null)
                                m_Memberships.Add(m, dt);
                        }

                        // get the cooperative type - blacksmith, tailor, etc
                        m_cooperativeType = (CooperativeType)reader.ReadInt();

                        // we are opsoleting versions 23 and 22
                        goto case 21;
                    }
                case 23:
                    {
                        // get the cooperative type - blacksmith, tailor, etc
                        m_cooperativeType = (CooperativeType)reader.ReadInt();

                        goto case 22;
                    }
                case 22:
                    {
                        m_MembershipOnly = reader.ReadBool();
                        ArrayList obsolete = new ArrayList();
                        obsolete = reader.ReadMobileList();
                        foreach (Mobile m in obsolete)
                            if (m != null)
                                // all memberships start with a 90 day membership
                                m_Memberships.Add(m, DateTime.UtcNow + TimeSpan.FromDays(90));
                        goto case 21;
                    }
                case 21:
                    {
                        m_AccountCode = reader.ReadUInt();
                        goto case 20;
                    }
                case 20:
                    {
                        m_NPCData = reader.ReadUInt();
                        goto case 19;
                    }
                case 19:
                    {
                        m_RestartDecay = reader.ReadTimeSpan();
                        goto case 18;
                    }
                case 18:
                    {
                        m_BoolTable = (BaseHouseBoolTable)reader.ReadUInt();
                        goto case 17;
                    }
                case 17:
                    {
                        m_UpgradeCosts = reader.ReadUInt();
                        goto case 16;
                    }
                case 16:
                    {
                        if (version < 25)
                        {
                            uint lockboxData = reader.ReadUInt();
                            m_StorageTaxCredits = (int)Utility.GetUIntRight16(lockboxData);
                            uint lockboxLimitMax = Utility.GetUIntByte3(lockboxData);
                            m_MaxLockboxes = (int)Utility.GetUIntByte4(lockboxData);
                        }
                        goto case 15;
                    }
                case 15:
                    {
                        m_SecurePremises = reader.ReadBool();
                        goto case 14;
                    }
                case 14:
                    {
                        idocannc = reader.ReadBool();
                        goto case 13;
                    }
                case 13:
                    {
                        m_DecayMinutesStored = reader.ReadDouble();
                        m_NeverDecay = reader.ReadBool();
                        goto case 11; //note, this isn't a mistake - we want to skip 12
                    }
                case 12:
                    {
                        DateTime tempDT = reader.ReadDeltaTime();
                        //StructureDecayTime = reader.ReadDeltaTime();
                        m_DecayMinutesStored = (tempDT - DateTime.UtcNow).TotalMinutes;

                        m_NeverDecay = reader.ReadBool();
                        goto case 11;
                    }
                case 11:
                    {
                        if (version < 25)
                            m_BonusLockboxes = reader.ReadInt() - m_MaxLockboxes;
                        else
                            m_MaxLockboxes = reader.ReadInt();

                        m_LockboxCount = reader.ReadInt();

                        goto case 9;
                    }
                case 10: // just a signal for updates
                case 9:
                    {
                        m_Visits = reader.ReadInt();
                        goto case 8;
                    }
                case 8:
                    {
                        m_Price = reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        m_Access = reader.ReadMobileList();
                        goto case 6;
                    }
                case 6:
                    {
                        m_BuiltOn = reader.ReadDateTime();
                        m_LastTraded = reader.ReadDateTime();
                        goto case 5;
                    }
                case 5: // just removed fields
                case 4:
                    {
                        m_Addons = reader.ReadItemList();
                        goto case 3;
                    }
                case 3:
                    {
                        int count = reader.ReadInt();
                        m_Secures = new ArrayList(count);

                        for (int i = 0; i < count; ++i)
                        {
                            SecureInfo info = new SecureInfo(reader);

                            if (info.Item != null)
                            {
                                info.Item.IsSecure = true;
#if false
                                info.Item.CancelFreezeTimer();        // don't initiate for Deserialize
#endif
                                m_Secures.Add(info);
                            }
                        }

                        goto case 2;
                    }
                case 2:
                    {
                        m_Public = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Region.GoLocation = reader.ReadPoint3D();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 12)
                        {
                            Refresh();
                            m_NeverDecay = false;
                        }

                        if (version < 4)
                            m_Addons = new ArrayList();

                        if (version < 7)
                            m_Access = new ArrayList();

                        if (version < 8)
                            m_Price = DefaultPrice;

                        m_Owner = reader.ReadMobile();
                        int count;
                        if (version < 5)
                        {
                            count = reader.ReadInt();

                            for (int i = 0; i < count; i++)
                                reader.ReadRect2D();
                        }

                        UpdateRegionArea();

                        Region.AddRegion(m_Region);

                        m_CoOwners = reader.ReadMobileList();
                        m_Friends = reader.ReadMobileList();
                        m_Bans = reader.ReadMobileList();

                        m_Sign = reader.ReadItem() as HouseSign;
                        m_Trash = reader.ReadItem() as TrashBarrel;

                        m_Doors = reader.ReadItemList();
                        m_LockDowns = reader.ReadItemList();

                        for (int i = 0; i < m_LockDowns.Count; ++i)
                        {
                            Item item = m_LockDowns[i] as Item;
                            if (item != null)
                            {
                                item.IsLockedDown = true;
#if false
                                item.CancelFreezeTimer();        // don't initiate for Deserialize
#endif
                            }
                        }

                        if (version < 3)
                        {
                            ArrayList items = reader.ReadItemList();
                            m_Secures = new ArrayList(items.Count);

                            for (int i = 0; i < items.Count; ++i)
                            {
                                Container c = items[i] as Container;

                                if (c != null)
                                {
                                    c.IsSecure = true;
                                    m_Secures.Add(new SecureInfo(c, SecureLevel.CoOwners));
                                }
                            }
                        }

                        m_MaxLockDowns = reader.ReadInt();
                        m_MaxSecures = reader.ReadInt();

                        if ((Map == null || Map == Map.Internal) && Location == Point3D.Zero)
                            Delete();

                        if (m_Owner != null)
                        {
                            ArrayList list = (ArrayList)m_Table[m_Owner];

                            if (list == null)
                                m_Table[m_Owner] = list = new ArrayList();

                            list.Add(this);
                        }
                        break;
                    }
            }

            // add this instance
            bool player = m_Owner == null || m_Owner.AccessLevel == AccessLevel.Player;
            Instances.Add(this, player);

            // fixup the account code
            if (version < 21)
            {
                m_AccountCode = CalcAccountCode(m_Owner);
            }

            // patch m_NPCData to hold the default barkeep count
            if (version < 20)
                MaximumBarkeepCount = 2;

            if (version <= 1)
                ChangeSignType(0xBD2);//private house, plain brass sign

            if (version < 10)
            {
                /* NOTE: This can exceed the house lockdown limit. It must be this way, because
				 * we do not want players' items to decay without them knowing. Or not even
				 * having a chance to fix it themselves.
				 */

                Timer.DelayCall(TimeSpan.Zero, new TimerCallback(FixLockdowns_Sandbox));
            }

            if (idocannc) // idoc announcement was running when we saved, re-create it
            {
                string[] lines = new string[1];
                lines[0] = String.Format("Lord British has condemned the estate of {0} near {1}.", this.Owner.Name, DescribeLocation());
                m_IDOC_Broadcast_TCE = new TownCrierEntry(lines, TimeSpan.FromMinutes(m_DecayMinutesStored), Serial.MinusOne);
                GlobalTownCrierEntryList.Instance.AddEntry(m_IDOC_Broadcast_TCE);
            }
        }

        private void FixLockdowns_Sandbox()
        {
            ArrayList lockDowns = new ArrayList();

            for (int i = 0; m_LockDowns != null && i < m_LockDowns.Count; ++i)
            {
                Item item = (Item)m_LockDowns[i];

                if (item is Container)
                    lockDowns.Add(item);
            }

            for (int i = 0; i < lockDowns.Count; ++i)
                SetLockdown((Item)lockDowns[i], true, true);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                // owner change gets a new account code
                m_AccountCode = CalcAccountCode(value);

                if (m_Owner != null)
                {
                    ArrayList list = (ArrayList)m_Table[m_Owner];

                    if (list == null)
                        m_Table[m_Owner] = list = new ArrayList();

                    list.Remove(this);
                    m_Owner.Delta(MobileDelta.Noto);
                }

                m_Owner = value;

                if (m_Owner != null)
                {
                    ArrayList list = (ArrayList)m_Table[m_Owner];

                    if (list == null)
                        m_Table[m_Owner] = list = new ArrayList();

                    list.Add(this);
                    m_Owner.Delta(MobileDelta.Noto);
                }

                if (m_Sign != null)
                    m_Sign.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Visits
        {
            get { return m_Visits; }
            set { m_Visits = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Public
        {
            get
            {
                return m_Public;
            }
            set
            {
                if (m_Public != value)
                {
                    m_Public = value;

                    if (!m_Public)//privatizing the house, change to brass sign
                        ChangeSignType(0xBD2);

                    if (m_Sign != null)
                        m_Sign.InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxSecures
        {
            get { return m_MaxSecures + m_BonusSecures; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxSecuresRaw
        {
            get { return m_MaxSecures; }
            set { m_MaxSecures = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusSecures
        {
            get { return m_BonusSecures; }
            set { m_BonusSecures = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D BanLocation
        {
            get
            {
                return m_Region.GoLocation;
            }
            set
            {
                m_Region.GoLocation = new Point3D(m_Region.GoLocation.X + value.X, m_Region.GoLocation.Y + value.Y, m_Region.GoLocation.Z + value.Z);
            }
        }

        public void SetBanLocation(Point3D p)
        {
            m_Region.GoLocation = p;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockDowns
        {
            get { return m_MaxLockDowns + m_BonusLockDowns; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockDownsRaw
        {
            get
            {
                return m_MaxLockDowns;
            }
            set
            {
                m_MaxLockDowns = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusLockDowns
        {
            get { return m_BonusLockDowns; }
            set { m_BonusLockDowns = value; }
        }

        public Region Region { get { return m_Region; } }
        public ArrayList CoOwners { get { return m_CoOwners; } set { m_CoOwners = value; } }
        public ArrayList Friends { get { return m_Friends; } set { m_Friends = value; } }
        public ArrayList Access { get { return m_Access; } set { m_Access = value; } }
        public ArrayList Bans { get { return m_Bans; } set { m_Bans = value; } }
        public ArrayList Doors { get { return m_Doors; } set { m_Doors = value; } }

        public int SumLockDownSecureCount
        {
            get
            {
                int count = 0;

                if (m_LockDowns != null)
                    count += m_LockDowns.Count;                                 // all count for 1

                for (int i = 0; i < m_LockDowns.Count; ++i)
                {
                    Item item = (Item)m_LockDowns[i];

                    if (item is SturdyFancyArmoire || item is SturdyArmoire)    // SturdyArmoire counts only 20% of contents (must be BaseClothing)
                        count += (item.TotalItems / 5);
                    else if (item is SeedBox seedBox)
                    {
                        int seedcount = seedBox.SeedCount();                    // SeedBox counts only 20% of contents 
                        count += (seedcount / 5);
                    }
                    if (item is Library)                                        // Library count as themselves plus 50% books inside
                        count += (item.TotalItems / 2);
                }

                if (m_Secures != null)
                {
                    for (int i = 0; i < m_Secures.Count; ++i)
                    {
                        SecureInfo info = (SecureInfo)m_Secures[i];

                        if (info.Item.Deleted)
                            continue;
                        else if (info.Item is StrongBox)
                            count += 1;
                        else
                            count += 125;
                    }
                }

                return count;
            }
        }
        public int LockBoxCount
        {
            get
            {
                int count = 0;

                foreach (Item item in m_LockDowns)
                {
                    if (item != null && !item.Deleted && item.IsLockedDown && IsLockbox(item))
                        count++;
                }

                if (m_LockboxCount != count)
                {
                    LogHelper Logger = new LogHelper("PhantomLockboxCleanup.log", false);
                    Logger.Log(LogType.Item, this.Sign, String.Format("Adjusting LockBoxCount by {0}", m_LockboxCount - count));

                    m_LockboxCount = count;
                }

                return m_LockboxCount;
            }
        }
        public int AddonCount
        {
            get
            {
                int count = 0;

                if (m_Addons != null)
                    foreach (object o in m_Addons)
                        if (o is not FixerAddon)
                            count += 1;

                return count;
            }
        }
        public int LockDownCount
        {
            get
            {
                int count = 0;

                if (m_LockDowns != null)
                    count += m_LockDowns.Count;

                return count;
            }
        }
        public int StrongBoxCount
        {
            get
            {
                int count = 0;

                if (m_Secures != null)
                {
                    for (int i = 0; i < m_Secures.Count; i++)
                    {
                        SecureInfo info = (SecureInfo)m_Secures[i];

                        if (info.Item.Deleted)
                            continue;
                        else if ((info.Item is StrongBox))
                            count += 1;
                    }
                }

                return count;
            }
        }
        public int SecureCount
        {
            get
            {
                int count = 0;

                if (m_Secures != null)
                {
                    for (int i = 0; i < m_Secures.Count; i++)
                    {
                        SecureInfo info = (SecureInfo)m_Secures[i];

                        if (info.Item.Deleted)
                            continue;
                        else if (!(info.Item is StrongBox))
                            count += 1;
                    }
                }

                return count;
            }
        }
        private void DefragAddons()
        {
            List<object> list = new();
            foreach (object o in m_Addons)
                if (o is Item item && item.Deleted)
                    list.Add(item);

            foreach (object o in list)
                m_Addons.Remove(o);
        }
        private void DefragLockDowns()
        {
            List<object> list = new();
            foreach (object o in m_LockDowns)
                if (o is Item item && item.Deleted)
                    list.Add(item);

            foreach (object o in list)
                m_LockDowns.Remove(o);
        }
        private void DefragSecures()
        {
            List<object> list = new();
            foreach (object o in m_Secures)
                if (o is SecureInfo si && si.Item != null && si.Item.Deleted)
                    list.Add(si);

            foreach (object o in list)
                m_Secures.Remove(o);
        }
        public ArrayList Addons { get { DefragAddons(); return m_Addons; } set { m_Addons = value; } }
        public ArrayList LockDowns { get { DefragLockDowns(); return m_LockDowns; } }
        public ArrayList Secures { get { DefragSecures(); return m_Secures; } }
        public HouseSign Sign { get { return m_Sign; } set { m_Sign = value; } }

        public DateTime BuiltOn
        {
            get { return m_BuiltOn; }
            set { m_BuiltOn = value; }
        }

        public DateTime LastTraded
        {
            get { return m_LastTraded; }
            set { m_LastTraded = value; }
        }

        public override void OnDelete()
        {
            #region Township cleanup
            foreach (Mobile tsNPC in Mobiles.TownshipNPCHelper.GetHouseNPCs(this))
                tsNPC.Delete();

            //Pix: 7/13/2008 - Removing the requirement of a townshipstone to be in a house.
            //			TownshipStone tstone = this.FindTownshipStone();
            //			if (tstone != null)
            //			{
            //				tstone.Delete();
            //			}
            //END Township cleanup
            #endregion Township cleanup

            // stop announcing this house!!
            if (m_IDOC_Broadcast_TCE != null)
            {
                GlobalTownCrierEntryList.Instance.RemoveEntry(m_IDOC_Broadcast_TCE);
                m_IDOC_Broadcast_TCE = null;
            }

            new FixColumnTimer(this).Start();

            if (m_Region != null)
                Region.RemoveRegion(m_Region);

            base.OnDelete();
        }

        private class FixColumnTimer : Timer
        {
            private Map m_Map;
            private int m_StartX, m_StartY, m_EndX, m_EndY;

            public FixColumnTimer(BaseMulti multi)
                : base(TimeSpan.Zero)
            {
                m_Map = multi.Map;

                MultiComponentList mcl = multi.Components;

                m_StartX = multi.X + mcl.Min.X;
                m_StartY = multi.Y + mcl.Min.Y;
                m_EndX = multi.X + mcl.Max.X;
                m_EndY = multi.Y + mcl.Max.Y;
            }

            protected override void OnTick()
            {
                if (m_Map == null)
                    return;

                for (int x = m_StartX; x <= m_EndX; ++x)
                    for (int y = m_StartY; y <= m_EndY; ++y)
                        m_Map.FixColumn(x, y);
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Owner != null)
            {
                ArrayList list = (ArrayList)m_Table[m_Owner];

                if (list == null)
                    m_Table[m_Owner] = list = new ArrayList();

                list.Remove(this);
            }

            if (m_Access != null)
                for (int i = m_Access.Count - 1; i >= 0; i--)
                    m_Access.Remove(i);

            if (m_Bans != null)
                for (int i = m_Bans.Count - 1; i >= 0; i--)
                    m_Bans.Remove(i);

            if (m_CoOwners != null)
                for (int i = m_CoOwners.Count - 1; i >= 0; i--)
                    m_CoOwners.Remove(i);

            if (m_Friends != null)
                for (int i = m_Friends.Count - 1; i >= 0; i--)
                    m_Friends.Remove(i);


            Region.RemoveRegion(m_Region);

            if (m_Sign != null)
                m_Sign.Delete();

            if (m_Trash != null)
                m_Trash.Delete();

            if (m_Doors != null)
            {
                for (int i = 0; i < m_Doors.Count; ++i)
                {
                    Item item = (Item)m_Doors[i];

                    if (item != null)
                        item.Delete();
                }

                m_Doors.Clear();
            }

            #region MemberStorage cleanup
            List<MemberStorage> memberstorage = new List<MemberStorage>();
            Item[] items = FindAllItems(typeof(MemberStorage));
            if (items != null)
                foreach (Item item in items)
                    if (item is MemberStorage)
                        memberstorage.Add(item as MemberStorage);
            foreach (MemberStorage ms in memberstorage)
                ms.Delete();

            m_Memberships.Clear();
            #endregion MemberStorage cleanup

            if (m_LockDowns != null)
            {
                List<Item> released = new List<Item>();

                for (int i = 0; i < m_LockDowns.Count; ++i)
                {
                    Item item = (Item)m_LockDowns[i];

                    if (item != null)
                    {
                        item.IsLockedDown = false;
                        item.IsSecure = false;
                        item.Movable = true;
                        item.SetLastMoved();

                        released.Add(item);
                    }
                }

                m_LockDowns.Clear();

                foreach (Item item in released)
                    item.OnRelease();
            }

            if (m_Secures != null)
            {
                List<Item> released = new List<Item>();

                for (int i = 0; i < m_Secures.Count; ++i)
                {
                    SecureInfo info = (SecureInfo)m_Secures[i];

                    if (info.Item is StrongBox)
                    {
                        info.Item.Destroy();
                    }
                    else
                    {
                        info.Item.IsLockedDown = false;
                        info.Item.IsSecure = false;
                        info.Item.Movable = true;
                        info.Item.SetLastMoved();

                        released.Add(info.Item);
                    }
                }

                m_Secures.Clear();

                foreach (Item item in released)
                    item.OnRelease();
            }

            if (m_Addons != null)
            {
                for (int i = 0; i < m_Addons.Count; ++i)
                {
                    Item item = (Item)m_Addons[i];

                    if (item != null && item.Deleted == false)
                    {
                        if (!Core.RuleSets.AngelIslandRules())
                        {
                            Item deed = null;

                            if (BaseAddon.CanRedeed(item))
                                deed = BaseAddon.Redeed(item);

                            if (deed != null)
                                deed.MoveToWorld(item.Location, item.Map);

                            item.Delete();
                        }
                        else
                        {
                            // Angel Island deletes all these regardless
                            //  Players on Angel Island need to redeed first!
                            item.Delete();
                        }
                    }
                }

                m_Addons.Clear();
            }
        }

        public static bool HasHouse(Mobile m)
        {
            if (m == null)
                return false;

            ArrayList list = (ArrayList)m_Table[m];

            if (list == null)
                return false;

            for (int i = 0; i < list.Count; ++i)
            {
                BaseHouse h = (BaseHouse)list[i];

                if (!h.Deleted)
                    return true;
            }

            return false;
        }

        public static bool HasAccountHouse(Mobile m)
        {
            Account a = m.Account as Account;

            if (a == null)
                return false;

            for (int i = 0; i < 5; ++i)
                if (a[i] != null && HasHouse(a[i]))
                    return true;

            return false;
        }
        /// <summary>
        /// Check that both the accounts match, and the house scheduled for inheritance matches this one.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public bool CheckInheritance(Mobile m)
        {
            if (HasAccountHouse(m))
                return false;

            bool check1 = CalcAccountCode(m) == AccountCode;
            if (check1)
                Utility.ConsoleWriteLine("CheckInheritance: CalcAccountCode check passed", ConsoleColor.Green);
            else
                Utility.ConsoleWriteLine("CheckInheritance: CalcAccountCode check failed", ConsoleColor.Red);

            bool check2 = CheckAccountInheritance(m);
            if (check2)
                Utility.ConsoleWriteLine("CheckInheritance: CheckAccountInheritance check passed", ConsoleColor.Green);
            else
                Utility.ConsoleWriteLine("CheckInheritance: CheckAccountInheritance check failed", ConsoleColor.Red);

            return check1 && check2;
        }
        public bool CheckAccountInheritance(Mobile m)
        {
            if (m != null && m.Account is Account acct)
            {
                if (acct.House == this.Serial)
                    return true;
            }
            return false;
        }
        public uint CalcAccountCode(Mobile m)
        {
            if (m != null)
            {
                Account a = m.Account as Account;

                if (a != null)
                    return a.AccountCode;
            }

            return (uint)Utility.GetStableHashCode("(unowned)" + Created.ToString(), version: 1);
        }
        // only ever called by our internal patcher
        public void RefreshAccountCode()
        {
            if (Owner != null)
            {
                Account a = Owner.Account as Account;

                if (a != null)
                    m_AccountCode = a.AccountCode;
            }

            m_AccountCode = (uint)Utility.GetStableHashCode("(unowned)" + Created.ToString(), version: 1);
        }

        public bool CheckAccount(Mobile mobCheck, Mobile accCheck)
        {
            if (accCheck != null)
            {
                Account a = accCheck.Account as Account;

                if (a != null)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        if (a[i] == mobCheck)
                            return true;
                    }
                }
            }

            return false;
        }

        public bool IsMember(Mobile m)
        {
            if (m == null)
                return false;

            return m_Memberships.ContainsKey(m);
        }

        public bool IsOwner(Mobile m)
        {
            if (m == null)
                return false;

            if (m == m_Owner || m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            //return IsAosRules && CheckAccount( m, m_Owner );
            return CheckAccount(m, m_Owner);
        }

        public bool IsCoOwner(Mobile m)
        {
            if (m == null || m_CoOwners == null)
                return false;

            if (IsOwner(m) || m_CoOwners.Contains(m))
                return true;

            //return !IsAosRules && CheckAccount( m, m_Owner );
            return CheckAccount(m, m_Owner);
        }

        public void RemoveKeys(Mobile m)
        {
            if (m_Doors != null)
            {
                uint keyValue = 0;

                for (int i = 0; keyValue == 0 && i < m_Doors.Count; ++i)
                {
                    BaseDoor door = m_Doors[i] as BaseDoor;

                    if (door != null)
                        keyValue = door.KeyValue;
                }

                Key.RemoveKeys(m, keyValue);
            }
        }

        public void ChangeLocks(Mobile m)
        {
            uint keyValue = CreateKeys(m);

            if (m_Doors != null)
            {
                for (int i = 0; i < m_Doors.Count; ++i)
                {
                    BaseDoor door = m_Doors[i] as BaseDoor;

                    if (door != null)
                        door.KeyValue = keyValue;
                }
            }
        }

        public void RemoveLocks()
        {
            if (m_Doors != null)
            {
                for (int i = 0; i < m_Doors.Count; ++i)
                {
                    BaseDoor door = m_Doors[i] as BaseDoor;

                    if (door != null)
                    {
                        door.KeyValue = 0;
                        door.Locked = false;
                    }
                }
            }
        }

        public virtual HousePlacementEntry ConvertEntry { get { return null; } }
        public virtual int ConvertOffsetX { get { return 0; } }
        public virtual int ConvertOffsetY { get { return 0; } }
        public virtual int ConvertOffsetZ { get { return 0; } }

        public virtual int DefaultPrice { get { return 0; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Price { get { return m_Price; } set { m_Price = value; } }

        public virtual HouseDeed GetDeed()
        {
            return null;
        }

        public bool IsFriend(Mobile m, bool checkInside = false)
        {
            if (m == null || m_Friends == null)
                return false;

            return (IsCoOwner(m) || m_Friends.Contains(m)) && (checkInside ? this.Contains(m) : true);
        }

        public bool IsBanned(Mobile m)
        {
            if (m == null || m == Owner || m.AccessLevel > AccessLevel.Player || m_Bans == null)
                return false;

            for (int i = 0; i < m_Bans.Count; ++i)
            {
                Mobile c = (Mobile)m_Bans[i];

                if (c == m)
                    return true;

                // The following section purports to ban all characters on a player's account from a house, 
                // but it doesn't work right, and anyway, we don't want to do that.
                //Account a = c.Account as Account;

                //if ( a == null )
                //    continue;

                //for ( int j = 0; j < 5; ++j )
                //{
                //    if ( a[i] == m )
                //        return true;
                //}
            }

            return false;
        }

        public override bool HasAccess(Mobile m)
        {
            if (m == null)
                return false;

            if (m.AccessLevel > AccessLevel.Player || IsFriend(m) || (m_Access != null && m_Access.Contains(m)))
                return true;

            if (m is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)m;

                if (bc.NoHouseRestrictions)
                    return true;

                if (bc.Controlled || bc.Summoned)
                {
                    m = bc.ControlMaster;

                    if (m == null)
                        m = bc.SummonMaster;

                    if (m == null)
                        return false;

                    if (m.AccessLevel > AccessLevel.Player || IsFriend(m) || (m_Access != null && m_Access.Contains(m)))
                        return true;
                }
            }

            return false;
        }

        public new bool IsLockedDown(Item check)
        {
            if (check == null)
                return false;

            if (m_LockDowns == null)
                return false;

            return m_LockDowns.Contains(check);
        }

        public new bool IsSecure(Item item)
        {
            if (item == null)
                return false;

            if (m_Secures == null)
                return false;

            bool contains = false;

            for (int i = 0; !contains && i < m_Secures.Count; ++i)
                contains = (((SecureInfo)m_Secures[i]).Item == item);

            return contains;
        }

        public Item[] FindAllItems(Type itemType)
        {
            Map map = this.Map;

            if (map == null)
                return null;

            MultiComponentList mcl = Components;
            IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));
            List<Item> list = new List<Item>();
            foreach (Item item in eable)
                if (item.GetType() == itemType && Contains(item))
                    list.Add(item);

            eable.Free();
            return list.ToArray();
        }

        public Item[] FindAllItems()
        {
            Map map = this.Map;

            if (map == null)
                return null;

            MultiComponentList mcl = Components;
            IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));
            List<Item> list = new List<Item>();
            foreach (Item item in eable)
                if (Contains(item))
                    list.Add(item);

            eable.Free();
            return list.ToArray();
        }

        public virtual Guildstone FindGuildstone()
        {
            Map map = this.Map;

            if (map == null)
                return null;

            MultiComponentList mcl = Components;
            IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (Item item in eable)
            {
                if (item is Guildstone && Contains(item))
                {
                    eable.Free();
                    return (Guildstone)item;
                }
            }

            eable.Free();
            return null;
        }

        public virtual TownshipStone FindTownshipStone()
        {
            Map map = this.Map;

            if (map == null)
                return null;

            MultiComponentList mcl = Components;
            IPooledEnumerable eable = map.GetItemsInBounds(new Rectangle2D(X + mcl.Min.X, Y + mcl.Min.Y, mcl.Width, mcl.Height));

            foreach (Item item in eable)
            {
                if (item is TownshipStone && Contains(item))
                {
                    eable.Free();
                    return (TownshipStone)item;
                }
            }

            eable.Free();
            return null;
        }

        public void LogCommand(Mobile from, string command, object targeted)
        {
            CommandLogging.WriteLine(from, String.Format("{0} {1} ('{2}') used command '{3}' on '{4}'", from.AccessLevel, from, ((Account)from.Account).Username, command, targeted.ToString()));
        }

        #region Lockboxes

        // 1/18/2024, Adam: disable LockboxSystem until we figure out how it plays with the new upgrade contracts
        public const bool LockboxSystem = true; // enables/disables the lockbox system 
        public const bool TaxCreditSystem = false; // enables/disables the tax credit system

        public int MaxBonusLockboxes { get { return int.MaxValue; } }
        public static int MaxStorageTaxCredits { get { return 64000; } }

        private int m_LockboxCount; // how many lockboxes do we currently have?
        private int m_MaxLockboxes; // what is our base lockbox cap?
        private int m_BonusLockboxes; // how many *bonus* lockboxes can we place beyond the base lockbox cap?
        private int m_StorageTaxCredits; // how many storage tax credits do we have?

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockboxes
        {
            get { return m_MaxLockboxes + m_BonusLockboxes; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockboxesRaw
        {
            get { return m_MaxLockboxes; }
            set { m_MaxLockboxes = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BonusLockboxes
        {
            get { return m_BonusLockboxes; }
            set { m_BonusLockboxes = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int StorageTaxCredits
        {
            get { return m_StorageTaxCredits; }
            set { m_StorageTaxCredits = value; }
        }

        public bool CanMakeLockbox(Mobile m, Item item)
        {
            if (m_LockboxCount >= MaxLockboxes)
            {
                m.SendMessage("The maximum number of LockBoxes has been reached : {0}", MaxLockboxes.ToString());
                return false;
            }
            else if (TaxCreditSystem && m_LockboxCount >= this.MaxLockboxesRaw && m_StorageTaxCredits <= 0)
            {
                m.SendMessage("You do not have enough stored tax credits to lock that down.");
                return false;
            }

            return true;
        }

        public void ToggleLockbox(Item item, bool locked, bool checkContains)
        {
            if (locked)
            {
                if (!checkContains || !m_LockDowns.Contains(item))
                    m_LockboxCount += 1;
            }
            else
            {
                m_LockboxCount -= 1;
            }
        }

        private int m_LockboxDecayAccumulator = 0; // not serialized

        public void CheckLockboxDecay()
        {
            if (!TaxCreditSystem)
                return; // don't decay lockboxes

            m_LockboxDecayAccumulator += LastHouseCheckMinutes;
            if (m_LockboxDecayAccumulator > 60)
            {   // an hour has passed, consume a tax credit.
                m_LockboxDecayAccumulator = 0;
                // consume one credit per extra lockbox
                if (m_StorageTaxCredits > 0 && m_LockboxCount > this.MaxLockboxesRaw)
                {   // we loop to insure we never go below 0
                    for (int i = 0; i < m_LockboxCount - this.MaxLockboxesRaw; i++)
                        if (m_StorageTaxCredits > 0)
                            m_StorageTaxCredits--;
                }
                else
                {   // need to decay a lockbox!
                    LogHelper logger = null;
                    try
                    {
                        Item toRelease = null;
                        // if we have *extra* storage, release one
                        if (m_LockDowns != null && m_LockboxCount > this.MaxLockboxesRaw)
                            foreach (Item item in m_LockDowns)
                                if (IsLockedDown(item) && IsLockbox(item))
                                {
                                    toRelease = item;
                                    break;
                                }
                        if (toRelease != null)
                        {   // log it
                            logger = new LogHelper("LockboxCleanup.log", false);
                            logger.Log(LogType.Item, toRelease, "Releasing container.");
                            // release it
                            SetLockdown(toRelease, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                    }
                    finally
                    {
                        if (logger != null)
                            logger.Finish();
                    }
                }
            }
        }

        public static bool IsLockbox(Item item)
        {
            if (!(item is Container))
                return false;
            if (item is BaseGameBoard)
                return false;
            if (item is KeyRing)
                return false;
            if (item is BaseContainer && ((BaseContainer)item).Deco)
                return false;
            if (item is TrashBarrel)
                return false;
            if (item is MusicBox)
                return false;
            return true;
        }

        #endregion

        public List<MoveEntry> GetAllPackableObjects()
        {
            Map map = this.Map;

            List<MoveEntry> toMove = new List<MoveEntry>();
            List<Item> ignore_list = new List<Item>(TownshipItemHelper.AllTownshipItems.Cast<Item>().ToList());
            // 1. Find all items/mobiles in the house that we want to move.
            if (map != null)
            {
                MultiComponentList mcl = this.Components;

                foreach (object o in map.GetObjectsInBounds(new Rectangle2D(Location.X + mcl.Min.X, Location.Y + mcl.Min.Y, mcl.Width, mcl.Height)))
                {
                    if (o is BaseHouse)
                        continue; // don't move houses

                    // ignore things like township plants that get picked up by house packup
                    if (o is Item item && ignore_list.Contains(item))
                        continue;

                    if (o is IEntity)
                    {
                        IEntity ent = (IEntity)o;

                        toMove.Add(new MoveEntry(ent, ent.Location));
                    }
                }
            }

            return toMove;
        }
    }

    #region Secure Info
    public enum SecureAccessResult
    {
        Insecure,
        Accessible,
        Inaccessible
    }

    public enum SecureLevel
    {
        Owner,
        CoOwners,
        Friends,
        Anyone
    }

    public class SecureInfo : ISecurable
    {
        private Container m_Item;
        private SecureLevel m_Level;

        public Container Item { get { return m_Item; } }
        public SecureLevel Level { get { return m_Level; } set { m_Level = value; } }

        public SecureInfo(Container item, SecureLevel level)
        {
            m_Item = item;
            m_Level = level;
        }

        public SecureInfo(GenericReader reader)
        {
            m_Item = reader.ReadItem() as Container;
            m_Level = (SecureLevel)reader.ReadByte();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(m_Item);
            writer.Write((byte)m_Level);
        }
    }
    #endregion Secure Info

    #region Targets
    public class LockdownTarget : Target
    {
        private bool m_Release;
        private BaseHouse m_House;

        public LockdownTarget(bool release, BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_Release = release;
            m_House = house;
        }

        protected override void OnTargetNotAccessible(Mobile from, object targeted)
        {
            OnTarget(from, targeted);
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            if (targeted is Item)
            {
                if (m_Release)
                {
                    m_House.Release(from, (Item)targeted);
                    m_House.LogCommand(from, "I wish to release this", targeted);
                }
                else
                {
                    m_House.LockDown(from, (Item)targeted);
                    m_House.LogCommand(from, "I wish to lock this down", targeted);
                }
            }
            else
            {
                from.SendLocalizedMessage(1005377);//You cannot lock that down
            }
        }
    }

    public class SecureTarget : Target
    {
        private bool m_Release;
        private BaseHouse m_House;

        public SecureTarget(bool release, BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_Release = release;
            m_House = house;
        }

        protected override void OnTargetNotAccessible(Mobile from, object targeted)
        {
            OnTarget(from, targeted);
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsCoOwner(from))
                return;

            if (targeted is Item)
            {
                if (((Item)targeted).IsSecure)
                {
                    from.SendMessage("That container is already secure.");
                }
                else if (((Item)targeted).IsLockedDown)
                {
                    from.SendMessage("That container is already locked down.");
                }
                else
                {
                    if (m_Release)
                    {
                        m_House.ReleaseSecure(from, (Item)targeted);
                        m_House.LogCommand(from, "I wish to release this", targeted);
                    }
                    else
                    {
                        m_House.AddSecure(from, (Item)targeted);
                        m_House.LogCommand(from, "I wish to secure this", targeted);
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(1010424);//You cannot secure this
            }
        }
    }

    public class HouseKickTarget : Target
    {
        private BaseHouse m_House;

        public HouseKickTarget(BaseHouse house)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            if (targeted is Mobile)
            {
                m_House.Kick(from, (Mobile)targeted);
            }
            else
            {
                from.SendLocalizedMessage(501347);//You cannot eject that from the house!
            }
        }
    }

    public class HouseBanTarget : Target
    {
        private BaseHouse m_House;
        private bool m_Banning;

        public HouseBanTarget(bool ban, BaseHouse house)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Banning = ban;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            if (targeted is Mobile)
            {
                if (m_Banning)
                    m_House.Ban(from, (Mobile)targeted);
                else
                    m_House.RemoveBan(from, (Mobile)targeted);
            }
            else
            {
                from.SendLocalizedMessage(501347);//You cannot eject that from the house!
            }
        }
    }

    public class HouseDecoTarget : Target
    {
        private bool m_Deco;
        private object m_Caller;

        public HouseDecoTarget(bool deco, object caller)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_Caller = caller;
            m_Deco = deco;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive)
                return;

            // TODO: Access check?
            //if (!m_House.IsFriend(from))
            //    return;

            if (targeted is Container)
            {
                Container cont = (Container)targeted;

                if (cont.IsLockedDown || cont.IsSecure)
                {
                    from.SendMessage("You must release this item first.");
                    return;
                }

                if (cont.TotalItems > 0)
                {
                    from.SendMessage("That must be empty to make it decorative.");
                    return;
                }

                if (cont.Deco == true && m_Deco == true)
                {
                    from.SendMessage("That is already decorative.");
                    return;
                }

                if (cont.Deco == false && m_Deco == false)
                {
                    from.SendMessage("That is already functional.");
                    return;
                }

                cont.Deco = m_Deco;

                if (m_Deco)
                {
                    from.SendMessage("That container is now decorative.");

                    if (m_Caller is BaseHouse)
                        ((BaseHouse)m_Caller).LogCommand(from, "I wish to make this decorative", targeted);
                }
                else
                {
                    from.SendMessage("That container is now functional.");

                    if (m_Caller is BaseHouse)
                        ((BaseHouse)m_Caller).LogCommand(from, "I wish to make this functional", targeted);
                }
            }
            else
            {
                from.SendMessage("You cannot make that decorative.");
            }
        }
    }

    public class HouseAccessTarget : Target
    {
        private BaseHouse m_House;

        public HouseAccessTarget(BaseHouse house)
            : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsFriend(from))
                return;

            if (targeted is Mobile)
                m_House.GrantAccess(from, (Mobile)targeted);
            else
                from.SendLocalizedMessage(1060712); // That is not a player.
        }
    }

    public class CoOwnerTarget : Target
    {
        private BaseHouse m_House;
        private bool m_Add;

        public CoOwnerTarget(bool add, BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Add = add;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsOwner(from))
                return;

            if (targeted is Mobile)
            {
                if (m_Add)
                    m_House.AddCoOwner(from, (Mobile)targeted);
                else
                    m_House.RemoveCoOwner(from, (Mobile)targeted);
            }
            else if (targeted is CertificateOfIdentity)
            {
                CertificateOfIdentity coi = targeted as CertificateOfIdentity;
                if (coi.Mobile == null || coi.Mobile.Deleted)
                    from.SendMessage("That identity certificate does not represent a player.");
                else
                {
                    if (m_Add)
                        m_House.AddCoOwner(from, coi.Mobile);
                    else
                        m_House.RemoveCoOwner(from, coi.Mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(501362);//That can't be a coowner
            }
        }
    }

    public class HouseFriendTarget : Target
    {
        private BaseHouse m_House;
        private bool m_Add;

        public HouseFriendTarget(bool add, BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
            m_Add = add;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.Alive || !m_House.IsCoOwner(from))
                return;

            if (targeted is Mobile)
            {
                if (m_Add)
                    m_House.AddFriend(from, (Mobile)targeted);
                else
                    m_House.RemoveFriend(from, (Mobile)targeted);
            }
            else if (targeted is CertificateOfIdentity)
            {
                CertificateOfIdentity coi = targeted as CertificateOfIdentity;
                if (coi.Mobile == null || coi.Mobile.Deleted)
                    from.SendMessage("That identity certificate does not represent a player.");
                else
                {
                    if (m_Add)
                        m_House.AddFriend(from, coi.Mobile);
                    else
                        m_House.RemoveFriend(from, coi.Mobile);
                }
            }
            else
            {
                from.SendLocalizedMessage(501371); // That can't be a friend
            }
        }
    }

    public class HouseOwnerTarget : Target
    {
        private BaseHouse m_House;

        public HouseOwnerTarget(BaseHouse house)
            : base(12, false, TargetFlags.None)
        {
            CheckLOS = false;

            m_House = house;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Mobile)
                m_House.BeginConfirmTransfer(from, (Mobile)targeted);
            else
                from.SendLocalizedMessage(501384); // Only a player can own a house!
        }
    }

    public class SetSecureLevelEntry : ContextMenuEntry
    {
        private Item m_Item;
        private ISecurable m_Securable;

        public SetSecureLevelEntry(Item item, ISecurable securable)
            : base(6203, 6)
        {
            m_Item = item;
            m_Securable = securable;
        }

        public static ISecurable GetSecurable(Mobile from, Item item)
        {
            BaseHouse house = BaseHouse.FindHouseAt(item);

            if (house == null || !house.IsOwner(from) || !house.IsAosRules)
                return null;

            ISecurable sec = null;

            if (item is ISecurable)
            {
                bool isOwned = house.Doors.Contains(item);

                if (!isOwned)
                    isOwned = (house is HouseFoundation && ((HouseFoundation)house).IsFixture(item));

                if (!isOwned)
                    isOwned = house.IsLockedDown(item);

                if (isOwned)
                    sec = (ISecurable)item;
            }
            else
            {
                ArrayList list = house.Secures;

                for (int i = 0; sec == null && list != null && i < list.Count; ++i)
                {
                    SecureInfo si = (SecureInfo)list[i];

                    if (si.Item == item)
                        sec = si;
                }
            }

            return sec;
        }

        public static void AddTo(Mobile from, Item item, ArrayList list)
        {
            ISecurable sec = GetSecurable(from, item);

            if (sec != null)
                list.Add(new SetSecureLevelEntry(item, sec));
        }

        public override void OnClick()
        {
            ISecurable sec = GetSecurable(Owner.From, m_Item);

            if (sec != null)
                Owner.From.SendGump(new SetSecureLevelGump(Owner.From, sec));
        }

    }
    #endregion Targets
}