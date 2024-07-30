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

/* Scripts\Engines\AI\AI\BaseAI\BaseAI.cs
 * CHANGELOG
 *  6/20/2024, Adam (DidBustStuff())
 *      BlockDamage creatures will proactively bust stuff (energy fields, paralyze fields,) before failing the move
 * 3/26/2024, Adam,
 *    Complete rewrite. of the NavStar system. Smaller (works,) and easier to understand
 *  1/21/2024, Adam
 *      1. Don't assume the control master is an aggressor for any non-direct damage to the pet (fire trap for example.) 
 *      2. each pet command now executes KissAndMakup(control master) 
 *          this allows the pet to immediately forgive all aggression from the control master
 *  1/8/2024, Adam (All attack, all kill)
 *      Disallow creatures that cannot be damaged from executing this code.
 *      Background: we now have an invulnerable hireable. This mobile should never follow instructions to attack.
 *  12/30/2023, Adam (WalkRandomInHome, evenIfStationed)
 *      Add evenIfStationed to WalkRandomInHome, i.e., WalkRandomInHome(2, 2, 1, evenIfStationed: true)
 *      This forces a mobile to move even if 'stationed', i.e., they are Home.
 *      evenIfStationed is true when the player issues the "move" request.
 *  9/24/2023, Adam (CurrentSpeedAdjust())
 *      Replace AdjustSpeed() with CurrentSpeedAdjust()
 *      CurrentSpeedAdjust() Adjusts only CurrentSpeed up/down without affecting the other speeds.
 *  9/22/2023, Adam (DoOrderAttack())
 *      When we have killed whatever, and there are no more targets, set the creatures TargetLocation to that of the master.
 *      Here, we just *wander* back, slowly, and without purpose
 *  9/21/2023m, Adam (OnCurrentOrderChanged() / AdjustSpeed())
 *      In order to get the 'follow' speed players want, we now AdjustSpeed() for Come/Guard/Follow, then reset it
 *          For all other modes.
 *  9/20/2023, Adam 
 *      Allow running to catch up to master in Guard mode (was there for come and follow)
 *      Add ShouldRun(): ShouldRun KEEPS the pet running (via memory) if they pet has started running until he reaches his master.
 *          This stops the run one tile, walk one tile, run one tile madness.
 *  9/20/2023, Adam (turret bug fix)
 *      We check to see if our combatant is an Aggressor to our master : IsCombatantAnAggressor()
 *      If our master simply double-clicked someone in war mode, this is not enough.
 *  7/28/2023, Adam (CheckMove())
 *      Experimental: If a pet is under Control Order Follow, m_NextMove time is halved.
 *          This essentially ensures that a pet that is following should rarely be denied a move because it is 'too soon'
 *  5/10/23, Adam (ConstantFocus => PreferredFocus)
 *      Recast ConstantFocus as PreferredFocus. The old ConstantFocus model was just over-the-top. It would 
 *          stick around after a player died, and that just causes more problems than it solves.
 *      AcquireFocusMob now clears the PreferredFocus on death
 *  4/27/23, Adam (DoOrderGuard())
 *      Rewrite DoOrderGuard() to use new prioritization scheme.
 *  4/24/23, Adam (PrioritizeCombatant())
 *      In Think() Prioritize the Combatant.
 *      I made this a virtual in case some derived AIs need special behaviors.
 *  4/20/23, Adam
 *      - DoMove: cleanup logic, add rules, instrument with DebugMode.Movement output statements
 *      - herding: Add a timeout if we cannot get to our point destination in a reasonable amount of time
 *      - HotPursuit: If we are pathing after someone, reset the combat timers periodically so that they do not expire mid chase
 *      - GetCombatantInfo: We now pass a GetCombatantInfo class into DoActionCombat.
 *          Virtually all of our combat AI extracts the same info, dead? too far away?, etc. etc. GetCombatantInfo calculates
 *          all of these rules and passes them to DoActionCombat in a common and consistent manner.
 *  4/15/23, Adam (DoActionCombat())
 *      -- THIS CHANGE WAS REVERTED IN FAVOR OF THE ABOVE --
 *  4/13/23, Yoar
 *      Controlled mobs now run to their owner in Come/Follow mode.
 *  4/12/23 (AITimer)
 *      Restore timer priority to FiftyMS from TwoFiftyMS
 *      This TwoFiftyMS timer priority was causing very sluggish pet following.
 *  4/11/23, Adam (MoveTo)
 *  rewrote much of MoveTo:
 *      1. Add check to see if the goal has changed. If so, clear the path (so that it can be reset if need be)
 *      2. Require if the Z of the mobile and the target are greater than 2. DoMove alone seems to have real trouble otherwise
 *      3. Break out of pathing if: 1) if we are close enough, and 2) if our target is inLOS
 *      4. Don't keep recreating the path! If we have a path, use it until the other variables above have changed.
 *  3/22/23, Adam (DoIntelligentDialog())
 *      AnimalTrainer.FindMyPet() now called from BaseAI.DoIntelligentDialog(). Animal trainers can now locate your lost pet. 
 *      Basically a poor man's TreeOfKnowledge (For Siege)
 *  1/9/22, Adam
 *      Break out AcquireFocusMob and InvestigativeAI AI into their own 'partial' classes
 *  12/21/21, Adam (Do*Locate)
 *  	Have DoGenericLocate() run in it's own thread.
 *  12/12/21, Adam (Do*Locate)
 *      move the Do*Locate() functions to the Intelligent Dialog System
 *  12/12/21, Adam (DoLocate)
 *      Update DoLocate to use SpawnerCache QuickTables.
 *      DoLocate() is part of the intelligent Dialog system and accepts user queries and must be fast.
 *      Some queries are very expensive, and the looping over the spawner cache was killing us.
 *      The QuickTables took us from ~2 seconds for a complex query to ~0.01 seconds.
 * 12/4/21, Yoar
 *      Added ElfPeasant AI.
 * 10/20/21, Yoar: Breeding System overhaul
 *      Added two calls to BaseCreature.DoActionOverride. This method lets creatures override AI actions/orders.
 * 8/30/21, Adam: (pet Release handling)
 *      you shouldn't need a Loyalty check to release a pet. 
 * 8/24/21, Adam: OrderType.None is entered automatically and should not cause a reveal.
 *      This happens for instance after your summon kills a monster. This should not reveal you.
 * 8/21/21, Adam:
 * 	Add special callback �AcquireFocusMobCallback()� to BaseAI (overridden in GuardAI). This callback allows special AIs like GuardAI to perform highly specific �Acquire Focus Mob� processing 
 * 	    that otherwise does not fit into the normal mobile acquisition model. 
 * 	For instance, guards now dispatch monsters when they are in a guarded region, they are not controlled, and they have an aggressive AI. 
 * 	    Most of this could be covered by the standard AI with the exception of the inside/outside region constraints. Also the target FightMode may also be clumsy to specify. 
 * 	    It�s not that these things can�t be tested and acted upon in AcquireFocusMob(), but rather AcquireFocusMob doesn�t have the FightMode flags to specify such a specific target.
 * 	    Therefore, we override the �AcquireFocusMobCallback()� in the GuardAI to facilitate such a check.
 * 8/9/21, Adam: allguardbug fix - i do believe (AcquireFocusMobWorker)
 *  Scenario: pets are in guard mode (control order Guard.) So, the m_Mobile.ControlTarget is rightly the tamer.
 *  If the tamer is then targeted by a monster, the old logic would incorrectly set the focus mob to ControlTarget (the tamer.)
 *  Notes: ControlTarget gets set to an adversary when an "all kill" command is given, and the tamer targets a monster.
 *  Root of the problem: It was bad design for RunUO to overload the ControlTarget as both friend and foe... :/
 * 7/30/2021, Adam
 *      Remove the check in AcquireFocusMobWorker for accesslavel > player. if a staff member is not blessed, they should be attackable. 
 *      This makes testing easier, and will require special considerations when putting on events (for the better.)
 * 7/24/21, Adam
 *      Add calls to m_Mobile.GoingHome() and m_Mobile.ImHome() to allow individual mobiles to decide what they do when they are going home.
 *      Seems wrong to ugly up the AI with such mobile specific behavior
 * 6/30/21, Adam
 *	Add functions and logic to allow players to "ask" NPCs for training, then tell them to train them for a specific amount of gold.
 *		(old style only allowed you to drop gold on them.) This was necessary because we have moved to a modle where is no longer starter gold for newbies (exploit potential.)
 *		We now start players with an Unemployment Check which can be redemed for goods and services/training (no cashing of it.)
 *	8/8/10, Adam
 *		Add: OldFlee switch
 *			BaseAI seems to have a bug where they clear FocusMob in OnActionChanged().Flee
 *	7/11/10, adam
 *		o major reorganization of AI
 *			o push most smart-ai logic from the advanced magery classes down to baseAI so that we can use potions and bandages from 
 *				the new advanced melee class
 *	6/30/10, adam
 *		Add AI_Guard support
 *	6/23/10, adam
 *		We now call IsGuardCandidate() instead of just checking if Murderer. This adds Criminals and returns false if a guard is already
 *		on them
 *	6/21/10, adam
 *		o Vendors and other townspeople now LookAround() as part of their wandering. I.e:
 *		remember the mobiles around me so that if they are then hidden and release a spell while hidden, I can reasonably call guards.
 *		If a crime is committed around me, the guards may ask me if i've seen anyone and if I have, then they will go into action.
 *		o We also call guards now in LookAround() if appropriate
 *	5/12/10, adam
 *		fix ScaredOfScaryThings to actually work in cases other than "all kill"
 *	3/13/10, adam
 *		(1) Add DoorExploit() check
 *		we are within 1 tile of our target, but we are also at the same location as a door.
 *		the player is probably trying to stand behind an open door so that they can hit us, but we cannot hit them.
 *		(2) remove all of plasma's ConstantFocus logic as we're not using factions, and I'm unsure it's been throughly tested
 *			and we're seeing some odd AI behavior. Better safe than sorry.
 *		(3) fix gate-pet-to-give-counts bug by making the LOS check in DoOrderAttack()
 *			we already check hidden, why not check LOS?!!
 *			anywho, I put the check in the CoreCommandConsole. PetNeedsLOS. (default is NO CHECK)
 *	06/07/09 plasma
 *		Improve preferred focus logic
 *	05/25/09, plasma
 *		Re-implemented the ConstantFocus property of basecreature properly.
 *		If you set something as the preferred focus, the mob will now focus entirely on that
 *		Even if they are hidden.  There is additional logic to break this if they are not within 20 squares, dead, etc.
 *	1/13/09, Adam
 *		Fix a bad cast in MoveTo
 *	1/10/09, Adam
 *		Total rewrite of 'reveal' implementation
 *	1/9/09, Adam
 *		Make CanReveal available to BaseAI
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *	12/31/08, Adam
 *		- Add Bandage support		
 *		- Add generic DequipWeapon procedure
 *	12/30/08, Adam
 *		- Move the bandage stuff into it's own region
 *		- Add IsDamaged and IsPoisoned properties
 *	12/19,08, Adam
 *		** total rewrite of AcquireFocusMob() **
 *		We split the FightMode into two different bitmasks: 
 *		(1) The TYPE of creature to focus on (murderer, Evil, Aggressor, etc.)
 *		(2) The SELECTION parameters (closest, smartest, strongest, etc.)
 *		We then enumerate each value contained in the TYPE bitmask and pass each one to the
 *		AcquireFocusMobWorker() function along with the SELECTION mask.
 *		AcquireFocusMobWorker() will perform a similar enumeration over the SELECTION mask
 *		to build a sorted list of compound selection criteria, for instance Closest and Strongest. 
 *		Differences from OSI: Most creatures will act the same as on OSI; and if they don�t, we probably
 *		set the FightMode flags wrong for that creature. The real difference is the flexibility to do things
 *		not supported on OSI like creating compound aggression formulas like: 
 *		�Focus on all Evil and Criminal players while attacking the Weakest first with the highest Intelligence�
 *	12/09/08, Adam
 *		In AcquireFocusMob() we need to check if bitmack >= 1 not simply > 1
 *			if (((int)acqType & (int)m_FightModeValues[ix]) >= 1)
 *	12/07/08, Adam
 *		Redesign AcquireFocusMob() to loop through the ON bits in the FightMode parameter and passing them onto 
 *		AcquireFocusMobWorker(). This change allows us to create mobs with like Strongest | Closest | Evil FightModes
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	7/1/08, weaver
 *		Added ActionType.Chase and empty base handling definition.
 *	4/22/08. Adam
 *		Better handling of the CurrentWayPoint.Deleted case.
 *	9/1/07, Adam
 *		In OnTick() check to see if the AI has changed, if so, stop the timer and return
 *		i.e if (m_Owner.m_Mobile.AIObject != m_Owner)
 *	7/15/06, Adam
 *		Added new creature memory heap. This heap is used by the new AcquireFocusMobFromMemory() function
 *		to return creatures previously seen and filtered by AcquireFocus(), but which may now be hidden (or in another state.)
 *		the AcquireFocusMobFromMemory() is build upon a low-level simple heap-memory API which is implemented as a hash table
 *		and has a built-in short memory (cleanup) system. The .Value paramater of the heap is currently unused, but will come 
 *		in handy for future needs. Just be sure to update AcquireFocusMobFromMemory() accordingly.
 *  06/18/07 Taran Kain
 *		Added AI_Dragon
 *	03/28/07 Taran Kain
 *		Added ChickenAI
 *	3/6/07, Pix
 *		Reverted last change, which wasn't fully tested and was checked in accidentally.
 *	02/01/07, Pix
 *		Fixed bug in which pets don't attack when in guard mode and master isn't around (dead or gone).
 *		Fixed bug preventing pets in guard mode to not use spells (missing target processing logic).
 *  12/21/06, Kit
 *      And I say let the untamed ghost pets end! Added in missing isbonded = false to release command.
 *  10/10/06, Kit
 *		Revert B1 completly.
 *  8/31/06, Kit
 *		Made sector deactivation clear hidden mob memory list of any AI's.
 *  8/22/06, Kit
 *		Fixed bug with provoked creature on a player not breaking when player is dead.
 *  8/13/06, Kit
 *		Various tweaks to use new Creature flags.
 *  7/26/06, Kit
 *		Fixed accidental merge of code in WalkRandomHome, causeing creatures to not return to home locations of 0.
 *  7/20/06, Kit
 *		Fixed bug with FightMode evil controlled creatures wandering instead of attacking
 *		other aggressors.
 *  7/02/06, Kit
 *		Made hiding, cause lost of bard target and provoke.
 *	6/14/06, Adam
 *		Eliminate call to Dupicate IsEnemy() function in BaceCreature as it was mistanking being called when
 *			the other one was supposed to be called.
 *	6/11/06, Adam
 *		Convert NavStar console output to DebugSay
 *  06/07/06, Kit
 *		Fixed bug with beacons/navstar crashing server when mob remembered a null beacon.
 *  05/16/06, Kit
 *		Removed old bondedrelease gump replaced with new BondedPetReleaseGump
 *		Made it so that releaseing a dead bonded pet deletes the pet.
 *		removed old pet confusion code
 *  05/15/06 Taran Kain
 *		Integrated SectorPathAlgorithm into NavStar AI.
 *  04/30/06, Kit
 *		Removed previous BondedPetCanAttack logic/function.
 *  04/29/06, Kit
 *		Added new BondedPetCanAttack function for new all kill and guard mode pet logic.
 *		Set exception in TransformDelay to allow dead bonded pets to move at full normal speed.
 *  04/22/06, Kit
 *		Added check to follow logic that if creature CanRun to use running or walking acording to masters state.
 *		Rewrote ArmWeaponXXX routines/Add Generic EquipWeapon routine.
 *		Fixed direction bug with MoveTo causeing creatures if in range to not face enemy.
 *  04/17/06, Kit
 *		Added Bool Variable UsesRegs for if creature needs reagents to cast.
 *  04/15/06, Kit
 *		Modified Follow logic via WalkMobileRange to allow mobiles to follow master onboard ships.
 *	4/8/06, Pix
 *		Uses new CoreAI.IOBJoinEnabled bit.
 *	04/06/06, weaver
 *		Added logging of any attacking commands issued to have pets attack their owner.
 *	04/04/06, weaver
 *		Added logging of any attacking commands issued by tamers to have their pets attack each other.
 *  01/06/06, Kit
 *		Added playertype bandage use, useitembytype, ArmWeaponByType()
 *  12/28/05, Kit
 *		Added fightmode Player logic to AI_SORT, added IsScaryCOndition check to scary logic check.
 *	12/16/05, Adam
 *		Comment out debug logic
 *  12/10/05, Kit
 *		Added Vampire_AI.
 *  12/05/05, Kit
 *		NavStar Changes
 *  11/29/05, Kit
 *		Added MoveToNavPoint(), NavStar FSM state code, and checks for playerrangesensitive for use with NavStar.
 *	11/23/05, Adam
 *		Taran added code to have the mobile 'turn to the direction' set in the spawner
 *		as per the SR. 
 *		Adjustment: have movile read the m_Mobile.Spawner.MobileDirection instead of m_Mobile.Spawner.Direction
 *  11/07/05, Kit
 *		Added ARM/DISARM functions for weapons, moved GetPackItems function from HumanMageAI to here for use with ARM/DISARM
 *	10/06/05, erlein
 *		Altered confusion text to reflect anger at owner.
 *	10/05/05, erlein
 *		Added angered sound effect, animation chance and text feedback to owner on command issue to confused pets.
 *	10/02/05, erlein
 *		Removed aggressor list flush on "stop" command to moderate tamer control over their pets.
 *		Added confusion check on command interpretation via context menus and speech.
 *	9/19/05, Adam
 *		a. I think it was a bug in the RunUO to reset the 'bCheckIt' flag even when we are confronted with
 *		and ememy. Just because something is Aggressor, Evil, or Criminal should not mean that they should
 *		ignore their enemies.
 *		We now differentiate between generic enimies, and OppositionGroups. OppositionGroups are now considered
 *		regardless of fight mode.
 *		b. remove the early exit from AcquireFocusMob for the FightMode.Aggressor condition
 *		c. add debug thingie to AcquireFocusMob(): search for bDebug
 *	8/11/05, erlein
 *		Added flushing of aggressor list on "stop" command.
 *  6/04/05, Kit
 *		Added CouncilMemberAI
 *  5/30/05, Kit
 *		Added HumanMageAI, added checks for if mobile can run, and if damage should slow it
 *		Added initial support for new FSM Hunt state, Added MoveToSound() function,
 *		updated Movement functions to check if monster is running, and if so to set Running Flag for movement
 *	5/05/05, Kit
 *		Evil Mage AI support added
 *	4/30/05, Kit
 *		Added in support for Kin attacking players/kin in custom regions(DRDT) defined as a IOB region
 *	4/28/05, Kit
 *		Added back in getValuefrom check in Acquire function for default creatures to resolve problems with
 *		creatures attacking tamer doing all kill vs pet
 *	4/27/05. Kit
 *		Added in support for AI that has a prefeered type of mob to attack
 *		Fixed problems were fightmodes did not actually choose their first mob accordingly
 *	4/26/05, Pix
 *		Removed caster and guild invulnerability from non-controlled summons.
 *	4/21/05, Adam
 *		Remove special code that prevents controls masters from sicing
 *		their pets and summons on aligned players/kin
 *		See: EndPickTarget
 *	4/03/05, Kit
 *		Added AI_Genie Type
 *	3/31/05, Pix
 *		Change IOBStronghold to IOBRegion
 *	3/31/05, Pix
 *		Added check for IOB flagging/player attacking.
 *	03/31/05, erlein
 *		- Added calls to CheckHerding() in DoOrderNone, DoOrderCome and
 *		DoOrderGuard so can be herded in "stop", "come" and "guard" modes as
 *		well as "follow".
 *		- Added clearance of TargetLocation on command from creature's master
 *		to cancel the herding of creature.
 *  02/15/05, Pixie
 *		CHANGED FOR RUNUO 1.0.0 MERGE.
 * 01/13/05 - Taran Kain
 *		Fixed orcish IOB commands, didn't seem to work. Should now.
 * 01/12/05 - Taran Kain
 *		Added orcish commands "lat stai", "lat clomp" and "lat follow".
 * 01/03/05 - Pix
 *		Added AI_Suicide
 * 01/03/05 - Pix
 *		Made sure Tamables couldn't be made IOBFollowers.
 * 12/29/04, Pix
 *		Moved dismissal routines to BaseCreature.
 * 12/29/04, Pix
 *		Made it so non-IOBAligned mobs won't attack IOBFollowers.
 * 12/28/04, Pix
 *		Added a "grace distance" of +- 5 to dismissing IOBFollowers.
 *		Added message for successful dismissal.
 * 12/28/04, Pix
 *		Now IOBFollowers can't be dismissed unless they're close enough to their home.
 * 12/22/04, Pix
 *		Fixed dismissing control-slot issue.
 * 12/21/04, Pix
 *		Another fix for EndPickTarget.
 * 12/21/04, Pix
 *		Changed EndPickTarget so that IOB owners of pets can't target their own bretheren.
 * 12/20/04, Pix
 *		Added check for IOBAlignment == None
 * 12/20/04, Pix
 *		Added come/stay/stop commands for IOBFollowers.
 *		Added IOBEquipped check before processing command.
 * 12/14/04, Pix
 *		Fixed EV and BS so they attack again.
 * 12/09/04, Pix
 *		First TEST!! code checkin for IOB Factions.
 *  9/11/04, Pix
 *		Added check for guild for summoned creatures (BS/EV) which find their own targets.  They now
 *		won't attack guildmates.
 *	7/14/04, mith
 *		DoOrderAttack(): Added check to verify that target can be seen by the creature.
 *	7/13/04 smerX
 *		Added AIType.AI_Council
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.ContextMenus;
using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Quests;
using Server.Engines.Quests.Necro;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Spells;
using Server.Spells.Second;
using Server.Targeting;
using Server.Targets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Server.Utility;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.Mobiles
{
    public enum AIType
    {
        AI_Use_Default,
        AI_Melee,
        AI_Animal,
        AI_Archer,
        AI_Healer,
        AI_Vendor,
        AI_Mage,
        AI_Berserk,
        AI_Predator,
        AI_Thief,
        AI_Council,
        AI_Robot,
        AI_Genie,
        AI_HumanMage,
        AI_BaseHybrid,
        AI_CouncilMember,
        AI_Vamp,
        AI_Chicken,
        AI_Dragon,
        AI_Hybrid,
        AI_Guard,
        AI_HumanMelee,
        AI_TaxCollector,
        AI_Basilisk,
        AI_ElfPeasant,
    }

    public enum ActionType
    {
        Wander,
        Combat,
        Guard,
        Hunt,
        NavStar,
        Flee,
        Backoff,
        Interact,
        Chase,
    }

    public enum WeaponArmStatus
    {
        NotFound,
        Success,
        HandFull,
        AlreadyArmed
    }

    public abstract partial class BaseAI : SerializableObject, IEntity
    {
        #region IEntity
        public Serial Serial
        {
            get
            {
                return m_Mobile == null ? Serial.MinusOne : m_Mobile.Serial;
            }
        }
        public Point3D Location
        {
            get
            {
                return m_Mobile == null ? Point3D.Zero : m_Mobile.Location;
            }
            set
            {
                if (m_Mobile != null)
                    m_Mobile.Location = value;
            }
        }
        public Map Map
        {
            get
            {
                return m_Mobile == null ? null : m_Mobile.Map;
            }
        }
        public bool Deleted
        {
            get
            {
                return m_Mobile == null ? true : m_Mobile.Deleted;
            }
        }
        public void Delete()
        {
            if (m_Mobile != null && !m_Mobile.Deleted)
                m_Mobile.Delete();
        }
        public void ProcessDelta()
        {
            if (m_Mobile != null && !m_Mobile.Deleted)
                m_Mobile.ProcessDelta();
        }
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int X
        {
            get { return Location.m_X; }
            set { Location = new Point3D(value, Location.m_Y, Location.m_Z); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Y
        {
            get { return Location.m_Y; }
            set { Location = new Point3D(Location.m_X, value, Location.m_Z); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Z
        {
            get { return Location.m_Z; }
            set { Location = new Point3D(Location.m_X, Location.m_Y, value); }
        }
        public DateTime Created { get { return m_Mobile.Created; } }
        #region CompareTo(...)
        public int CompareTo(IEntity other)
        {
            if (other == null)
                return -1;

            return Serial.CompareTo(other.Serial);
        }

        public int CompareTo(Mobile other)
        {
            return this.CompareTo((IEntity)other);
        }

        public int CompareTo(object other)
        {
            if (other == null || other is IEntity)
                return this.CompareTo((IEntity)other);

            throw new ArgumentException();
        }
        #endregion CompareTo(...)
        #region Notify
        public virtual void Notify(Notification notification, params object[] args)
        {
            switch (notification)
            {
                case Notification.AmmoStatus:
                    AmmoStatus(args[0] as Item);
                    break;
                default:
                    return;
            }
        }
        public virtual void AmmoStatus(Item weapon)
        {
        }
        #endregion Notify
        #endregion IEntity
        public AITimer m_Timer;
        protected ActionType m_Action;
        private DateTime m_NextStopGuard;
        private DateTime m_NextStopHunt;
        public BaseCreature m_Mobile;
        private Memory m_LongTermMemory = new Memory();     // memory used to remember we a saw a player in the area
        protected Memory LongTermMemory { get { return m_LongTermMemory; } }
        private Memory m_ShortTermMemory = new Memory();    // short term battle memory
        public Memory ShortTermMemory { get { return m_ShortTermMemory; } }
        private static ArrayList m_FightModeValues = new ArrayList(Enum.GetValues(typeof(FightMode)));
        private Point3D[] m_teleportTable = null;
        public Point3D[] TeleportTable { get { return m_teleportTable; } set { m_teleportTable = value; } }

        public bool HoldingWeapon()
        {
            Item weapon = m_Mobile.Weapon as Item;

            if (weapon != null && weapon.Parent == m_Mobile && !(weapon is Fists))
                return true;

            return false;
        }

        public virtual bool SmartAI
        {
            get { return (m_Mobile is BaseVendor || m_Mobile is BaseEscortable); }
        }

        #region running
        public virtual void Run(Direction d)
        {
            if ((m_Mobile.Spell != null && m_Mobile.Spell.IsCasting) || m_Mobile.Paralyzed || m_Mobile.Frozen || m_Mobile.DisallowAllMoves)
                return;

            m_Mobile.Direction = d | Direction.Running;

            if (!DoMove(m_Mobile.Direction, true))
                OnFailedMove();
        }
        public virtual void RunTo(Mobile m, bool Run)
        {
            if (!SmartAI)
            {
                if (!MoveTo(m, Run, m_Mobile.RangeFight))
                    OnFailedMove();

                return;
            }

            if (m.Paralyzed || m.Frozen)
            {
                if (m_Mobile.InRange(m, 1))
                    RunFrom(m);
                else if (!m_Mobile.InRange(m, m_Mobile.RangeFight > 2 ? m_Mobile.RangeFight : 2) && !MoveTo(m, Run, 1))
                    OnFailedMove();
            }
            else
            {
                RunTo(m.Location, Run);
            }
        }

        public void RunTo(Point3D px, bool Run)
        {
            if (!m_Mobile.InRange(px, m_Mobile.RangeFight))
            {
                if (!MoveTo(px, Run, 1))
                    OnFailedMove();
            }
            else if (m_Mobile.InRange(px, m_Mobile.RangeFight - 1))
            {
                RunFrom(px);
            }
        }

        public virtual void RunFrom(Mobile m)
        {
            Run((m_Mobile.GetDirectionTo(m) - 4) & Direction.Mask);
        }

        public void RunFrom(Point3D px)
        {
            Run((m_Mobile.GetDirectionTo(px) - 4) & Direction.Mask);
        }
        public virtual void OnFailedMove()
        {
        }
        #endregion

        #region Bandages
        public int BandageCount
        {
            get
            {
                Container pack = m_Mobile.Backpack;
                if (pack == null) return 0;
                Item bandage = pack.FindItemByType(typeof(Bandage));
                return bandage != null ? bandage.Amount : 0;
            }
        }
        public Bandage GetBandage()
        {
            Container pack = m_Mobile.Backpack;
            if (pack == null) return null;
            return pack.FindItemByType(typeof(Bandage)) as Bandage;
        }
        private BandageContext m_Bandage;
        private DateTime m_BandageStart;
        public DateTime BandageTime
        {
            get { return m_BandageStart; }
            set { m_BandageStart = value; }
        }

        public virtual TimeSpan TimeUntilBandage
        {
            get
            {
                if (m_Bandage != null && m_Bandage.Timer == null)
                    m_Bandage = null;

                if (m_Bandage == null)
                    return TimeSpan.MaxValue;

                TimeSpan ts = (m_BandageStart + m_Bandage.Timer.Delay) - DateTime.UtcNow;

                if (ts < TimeSpan.FromSeconds(-1.0))
                {
                    m_Bandage = null;
                    return TimeSpan.MaxValue;
                }

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
        }

        public bool StartBandage(Mobile from, Mobile to)
        {
            if (BandageCount >= 1)
            {
                m_Bandage = null;
                m_Bandage = BandageContext.BeginHeal(from, to);
                m_BandageStart = DateTime.UtcNow;
                if (m_Bandage != null)
                {   // alls well, consume the bandage!
                    Bandage bandage = GetBandage();
                    if (bandage != null)
                        bandage.Consume();
                    return true;
                }
            }
            return false;
        }
        #endregion Bandages

        #region Potions

        public int HealPotCount
        {
            get
            {
                Container pack = m_Mobile.Backpack;
                if (pack == null) return 0;
                Item[] p = pack.FindItemsByType(typeof(BaseHealPotion));
                return (p != null) ? p.Length : 0;
            }
        }

        public BaseHealPotion GetHealPot()
        {
            Container pack = m_Mobile.Backpack;
            if (pack == null) return null;
            return pack.FindItemByType(typeof(BaseHealPotion)) as BaseHealPotion;
        }

        public int CurePotCount
        {
            get
            {
                Container pack = m_Mobile.Backpack;
                if (pack == null) return 0;
                Item[] p = pack.FindItemsByType(typeof(BaseCurePotion));
                return (p != null) ? p.Length : 0;
            }
        }

        public BaseCurePotion GetCurePot()
        {
            Container pack = m_Mobile.Backpack;
            if (pack == null) return null;
            return pack.FindItemByType(typeof(BaseCurePotion)) as BaseCurePotion;
        }

        public int RefreshPotCount
        {
            get
            {
                Container pack = m_Mobile.Backpack;
                if (pack == null) return 0;
                Item[] p = pack.FindItemsByType(typeof(BaseRefreshPotion));
                return (p != null) ? p.Length : 0;
            }
        }

        public BaseRefreshPotion GetRefreshPot()
        {
            Container pack = m_Mobile.Backpack;
            if (pack == null) return null;
            return pack.FindItemByType(typeof(BaseRefreshPotion)) as BaseRefreshPotion;
        }

        public int AgilityPotCount
        {
            get
            {
                Container pack = m_Mobile.Backpack;
                if (pack == null) return 0;
                Item[] p = pack.FindItemsByType(typeof(BaseAgilityPotion));
                return (p != null) ? p.Length : 0;
            }
        }

        public BaseAgilityPotion GetAgilityPot()
        {
            Container pack = m_Mobile.Backpack;
            if (pack == null) return null;
            return pack.FindItemByType(typeof(BaseAgilityPotion)) as BaseAgilityPotion;
        }

        public int StrengthPotCount
        {
            get
            {
                Container pack = m_Mobile.Backpack;
                if (pack == null) return 0;
                Item[] p = pack.FindItemsByType(typeof(BaseStrengthPotion));
                return (p != null) ? p.Length : 0;
            }
        }

        public BaseStrengthPotion GetStrengthPot()
        {
            Container pack = m_Mobile.Backpack;
            if (pack == null) return null;
            return pack.FindItemByType(typeof(BaseStrengthPotion)) as BaseStrengthPotion;
        }

        public virtual bool DrinkCure(Mobile from)
        {
            if (CurePotCount >= 1)
            {
                bool requip = false;
                BaseCurePotion Pot = GetCurePot();
                if (Pot.RequireFreeHand && BasePotion.HasFreeHand(m_Mobile) == false)
                    requip = DequipWeapon();
                Pot.Drink(from);
                if (requip)
                    EquipWeapon();
                if (Pot.Deleted == true)    // it won't be deleted if we were not poisoned.
                    return true;
                else
                    return false;
            }
            else
                return false;

        }
        public virtual bool DrinkHeal(Mobile from)
        {
            if (HealPotCount >= 1)
            {
                bool requip = false;
                BaseHealPotion Pot = GetHealPot();
                if (Pot.RequireFreeHand && BasePotion.HasFreeHand(m_Mobile) == false)
                    requip = DequipWeapon();
                Pot.Drink(from);
                if (requip)
                    EquipWeapon();
                if (Pot.Deleted == true)    // it won't be deleted if we tried to drink it too soon.
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        public virtual bool DrinkRefresh(Mobile from)
        {
            if (RefreshPotCount >= 1)
            {
                bool requip = false;
                BaseRefreshPotion Pot = GetRefreshPot();
                if (Pot.RequireFreeHand && BasePotion.HasFreeHand(m_Mobile) == false)
                    requip = DequipWeapon();
                Pot.Drink(from);
                if (requip)
                    EquipWeapon();
                if (Pot.Deleted == true)    // it won't be deleted if we are at full stam.
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        public virtual bool DrinkAgility(Mobile from)
        {
            if (AgilityPotCount >= 1)
            {
                bool requip = false;
                BaseAgilityPotion Pot = GetAgilityPot();
                if (Pot.RequireFreeHand && BasePotion.HasFreeHand(m_Mobile) == false)
                    requip = DequipWeapon();
                Pot.Drink(from);
                if (requip)
                    EquipWeapon();
                if (Pot.Deleted == true)    // it won't be deleted if you are already under a similar effect
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        public virtual bool DrinkStrength(Mobile from)
        {
            if (StrengthPotCount >= 1)
            {
                bool requip = false;
                BaseStrengthPotion Pot = GetStrengthPot();
                if (Pot.RequireFreeHand && BasePotion.HasFreeHand(m_Mobile) == false)
                    requip = DequipWeapon();
                Pot.Drink(from);
                if (requip)
                    EquipWeapon();
                if (Pot.Deleted == true)    // it won't be deleted if you are already under a similar effect
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        #endregion Potions

        #region Priority Target
        protected bool PriorityTarget(Mobile c, out Mobile pt)
        {
            pt = null;

            if (c == null)
                return false;

            FightMode acqFlags = (FightMode)((uint)this.m_Mobile.FightMode & 0xFFFF0000);
            ArrayList list = new ArrayList();

            Mobile mx;
            for (int a = 0; a < m_Mobile.Aggressors.Count; ++a)
            {
                mx = (m_Mobile.Aggressors[a] as AggressorInfo).Attacker;
                if (mx != null && !mx.Deleted && mx.Alive && !mx.Hidden && !mx.IsDeadBondedPet && m_Mobile.CanBeHarmful(mx, false) && !(m_Mobile.Aggressors[a] as AggressorInfo).Expired)
                    list.Add(mx);
            }

            if (list.Count == 0)
                return false;

            // where is c?
            int StartingIndex = list.IndexOf(c);

            // we don't prioritize non-aggressors
            if (StartingIndex == -1)
                return false;

            // now sort our list based on the fight AI flags, weakest,closest,strongest,etc
            if (acqFlags > 0)
                // for each enum value, check the passed-in acqType for a match
                for (int ix = 0; ix < m_FightModeValues.Count; ix++)
                {   // if this fight-mode-flag exists in the acqFlags
                    if (((int)acqFlags & (int)m_FightModeValues[ix]) >= 1)
                    {   // sort the list N times to percolate the 'best fit' values to the head of the list
                        SortDirection direction = (((FightMode)m_FightModeValues[ix] & (FightMode.Weakest | FightMode.Closest)) > 0) ? SortDirection.Ascending : SortDirection.Descending;
                        list.Sort(new AI_Sort(direction, m_Mobile, (FightMode)m_FightModeValues[ix]));
                    }
                }

            // where is c now?
            //	this test is important to prevent gratuitous target switching
            int EndingIndex = list.IndexOf(c);

            // top priority
            pt = list[0] as Mobile;

            // if the priority has changed
            return (pt != null && c != pt && StartingIndex != EndingIndex);
        }
        #endregion

        #region pouches
        protected virtual Pouch FindPouch(Mobile from)
        {
            ArrayList list = GetPackItems(from);
            Pouch pouch;

            foreach (Item item in list)
            {
                if (item is Pouch)
                {
                    pouch = (Pouch)item;
                    return pouch;
                }
            }

            return null;
        }

        protected virtual void UseTrapPouch(Mobile from)
        {
            Pouch pouch = FindPouch(from);
            if (pouch != null && pouch.TrapType == TrapType.MagicTrap)
                pouch.ExecuteTrap(from);
        }

        protected virtual void TrapPouch(Mobile from)
        {
            Spell spell = null;
            Pouch pouch = FindPouch(from);
            if (pouch != null && pouch.TrapType == TrapType.None)
            {
                spell = new MagicTrapSpell(from, null);
                spell.Cast();
            }
        }
        #endregion

        public virtual bool IsDamaged
        {
            get { return (m_Mobile.Hits < m_Mobile.HitsMax); }
        }

        public virtual bool IsPoisoned
        {
            get { return m_Mobile.Poisoned; }
        }

        public bool IsAllowed(FightStyle flag)
        {
            return ((m_Mobile.FightStyle & flag) == flag);
        }

        public bool CrossHeals
        {
            get { return m_Mobile.GetCreatureBool(CreatureBoolTable.CrossHeals); }
            set { m_Mobile.SetCreatureBool(CreatureBoolTable.CrossHeals, value); }
        }

        public bool UsesBandages
        {
            get { return m_Mobile.GetCreatureBool(CreatureBoolTable.UsesBandages); }
            set { m_Mobile.SetCreatureBool(CreatureBoolTable.UsesBandages, value); }
        }

        public bool UsesPotions
        {
            get { return m_Mobile.GetCreatureBool(CreatureBoolTable.UsesPotions); }
            set { m_Mobile.SetCreatureBool(CreatureBoolTable.UsesPotions, value); }
        }

        public bool CanRunAI
        {
            get { return m_Mobile.GetCreatureBool(CreatureBoolTable.CanRunAI); }
            set { m_Mobile.SetCreatureBool(CreatureBoolTable.CanRunAI, value); }
        }

        public bool CanReveal
        {
            get { return m_Mobile.GetCreatureBool(CreatureBoolTable.CanReveal); }
            set { m_Mobile.SetCreatureBool(CreatureBoolTable.CanReveal, value); }
        }

        public bool UsesRegs
        {
            get { return m_Mobile.GetCreatureBool(CreatureBoolTable.UsesRegeants); }
            set { m_Mobile.SetCreatureBool(CreatureBoolTable.UsesRegeants, value); }
        }

        public ArrayList GetPackItems(Mobile from)
        {
            ArrayList list = new ArrayList();

            foreach (Item item in from.Items)
            {
                if (item.Movable && item != from.Backpack)
                    list.Add(item);
            }

            if (from.Backpack != null)
            {
                list.AddRange(from.Backpack.Items);
            }

            return list;
        }

        public bool FindWeapon(Mobile m)
        {
            Container pack = m.Backpack;

            if (pack == null)
                return false;

            Item weapon = pack.FindItemByType(typeof(BaseWeapon));
            Item weaponOne = m.FindItemOnLayer(Layer.OneHanded);
            Item weaponTwo = m.FindItemOnLayer(Layer.TwoHanded);
            if (weapon == null && weaponOne == null && weaponTwo == null)
                return false;

            return true;
        }

        public virtual bool EquipWeapon()
        {
            Container pack = m_Mobile.Backpack;

            if (pack == null)
                return false;

            Item weapon = pack.FindItemByType(typeof(BaseWeapon));

            if (weapon == null)
                return false;

            return m_Mobile.EquipItem(weapon);
        }

        public virtual bool DequipWeapon()
        {
            Container pack = m_Mobile.Backpack;

            if (pack == null)
                return false;

            Item weapon = m_Mobile.Weapon as Item;

            if (weapon != null && weapon.Parent == m_Mobile && !(weapon is Fists))
            {
                pack.DropItem(weapon);
                return true;
            }

            return false;
        }

        public WeaponArmStatus ArmWeaponByType(Type type)
        {
            Container pack = m_Mobile.Backpack;

            if (pack == null)
                return WeaponArmStatus.NotFound;

            Item weapon = m_Mobile.Weapon as Item;

            if (weapon.GetType() == type)
                return WeaponArmStatus.AlreadyArmed;

            Item item = pack.FindItemByType(type);

            bool FoundWeapon = false;
            bool HandBlocked = false;

            if (item == null)
                return WeaponArmStatus.NotFound;
            else
                FoundWeapon = true;

            if (weapon.Layer == Layer.OneHanded || weapon.Layer == Layer.TwoHanded)
                HandBlocked = true;

            if (m_Mobile.EquipItem(item))
            {
                return WeaponArmStatus.Success;
            }

            if (FoundWeapon && HandBlocked)
                return WeaponArmStatus.HandFull;

            return WeaponArmStatus.NotFound;

        }


        public WeaponArmStatus ArmOneHandedWeapon()
        {
            Item weapon = m_Mobile.Weapon as Item;
            if (weapon.Layer == Layer.OneHanded)
                return WeaponArmStatus.AlreadyArmed;

            bool HandBlocked = false;
            bool FoundWeapon = false;

            if (weapon.Layer == Layer.TwoHanded)
                HandBlocked = true;

            ArrayList list = GetPackItems(m_Mobile);
            foreach (Item item in list)
            {
                if (item is BaseWeapon && item.Layer == Layer.OneHanded)
                {
                    FoundWeapon = true;
                    if (m_Mobile.EquipItem(item))
                    {
                        return WeaponArmStatus.Success;
                    }
                }
            }
            if (!FoundWeapon)
                return WeaponArmStatus.NotFound;
            if (FoundWeapon && HandBlocked)
                return WeaponArmStatus.HandFull;

            return WeaponArmStatus.NotFound;
        }

        public WeaponArmStatus ArmTwoHandedWeapon()
        {
            Item weapon = m_Mobile.Weapon as Item;
            if (weapon.Layer == Layer.TwoHanded)
                return WeaponArmStatus.AlreadyArmed;

            bool HandBlocked = false;
            bool FoundWeapon = false;

            Item IsArmed = m_Mobile.FindItemOnLayer(Layer.TwoHanded);

            if (weapon.Layer == Layer.OneHanded || IsArmed is BaseShield)
                HandBlocked = true;

            ArrayList list = GetPackItems(m_Mobile);
            foreach (Item item in list)
            {
                if (item is BaseWeapon && item.Layer == Layer.TwoHanded)
                {
                    FoundWeapon = true;
                    if (m_Mobile.EquipItem(item))
                    {
                        return WeaponArmStatus.Success;
                    }
                }
            }
            if (!FoundWeapon)
                return WeaponArmStatus.NotFound;
            if (FoundWeapon && HandBlocked)
                return WeaponArmStatus.HandFull;

            return WeaponArmStatus.NotFound;
        }

        public bool ArmShield()
        {
            Item IsArmed = m_Mobile.FindItemOnLayer(Layer.TwoHanded);
            if (IsArmed is BaseShield)
                return true;

            ArrayList list = GetPackItems(m_Mobile);
            foreach (Item item in list)
            {
                if (item is BaseShield && item.Layer == Layer.TwoHanded)
                {
                    if (m_Mobile.EquipItem(item))
                        return true;
                }
            }

            return false;
        }

        public void DisarmOneHandedWeapon()
        {
            m_Mobile.ClearHand(m_Mobile.FindItemOnLayer(Layer.OneHanded));
        }

        public void DisarmTwoHandedWeapon()
        {
            Item shield = m_Mobile.FindItemOnLayer(Layer.TwoHanded);
            if (shield != null && shield is BaseShield)
                return;

            m_Mobile.ClearHand(m_Mobile.FindItemOnLayer(Layer.TwoHanded));
        }

        public void DisarmShield()
        {
            Item shield = m_Mobile.FindItemOnLayer(Layer.TwoHanded);
            if (shield != null && shield is BaseShield)
                m_Mobile.ClearHand(m_Mobile.FindItemOnLayer(Layer.TwoHanded));

        }

        public bool CheckIfHaveItem(Type type)
        {
            Container pack = m_Mobile.Backpack;

            if (pack == null)
                return false;

            Item item = pack.FindItemByType(type);

            if (item == null)
                return false;

            return true;
        }

        public virtual bool UseItemByType(Type type)
        {
            Container pack = m_Mobile.Backpack;

            if (pack == null)
                return false;

            Item item = pack.FindItemByType(type);

            if (item == null)
                return false;

            item.OnDoubleClick(m_Mobile);

            return true;
        }


        public BaseAI(BaseCreature m)
        {
            m_Mobile = m;

            m_Timer = new AITimer(this);
            m_Timer.Start();

            Action = ActionType.Wander;
            // m_ControlOrder was set in deserialization, but we did not setup the necessary state variables.
            OnCurrentOrderChanged();
        }

        public ActionType Action
        {
            get
            {
                return m_Action;
            }
            set
            {
                ActionType oldAction = m_Action;
                m_Action = value;
                OnActionChanged(oldAction);
            }
        }

        public virtual bool WasNamed(string speech)
        {
            string name = m_Mobile.Name;

            return (name != null && Insensitive.StartsWith(speech, name));
        }

        private class InternalEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private BaseCreature m_Mobile;
            private BaseAI m_AI;
            private OrderType m_Order;

            public InternalEntry(Mobile from, int number, int range, BaseCreature mobile, BaseAI ai, OrderType order)
                : base(number, range)
            {
                m_From = from;
                m_Mobile = mobile;
                m_AI = ai;
                m_Order = order;

                if (mobile.IsDeadPet && (order == OrderType.Guard || order == OrderType.Attack || order == OrderType.Transfer || order == OrderType.Drop))
                    Enabled = false;
            }

            public override void OnClick()
            {
                if (!m_Mobile.Deleted && m_Mobile.Controlled && m_From == m_Mobile.ControlMaster && m_From.CheckAlive())
                {
                    switch (m_Order)
                    {
                        case OrderType.Follow:
                        case OrderType.Attack:
                        case OrderType.Transfer:
                            {
                                m_AI.BeginPickTarget(m_From, m_Order);
                                break;
                            }
                        case OrderType.Release:
                            {
                                if (m_Mobile.IOBFollower)
                                {
                                    m_Mobile.AttemptIOBDismiss();
                                }
                                else if (m_Mobile.Summoned)
                                    goto default;
                                else
                                    m_From.SendGump(new Gumps.ConfirmReleaseGump(m_From, m_Mobile));

                                break;
                            }

                        default:
                            {
                                if (m_Mobile.CheckControlChance(m_From))
                                    m_Mobile.ControlOrder = m_Order;

                                break;
                            }
                    }
                }
            }
        }

        public virtual void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            if (from.Alive && m_Mobile.Controlled && from == m_Mobile.ControlMaster && from.InRange(m_Mobile, 14) && !m_Mobile.IOBFollower)
            {
                list.Add(new InternalEntry(from, 6107, 14, m_Mobile, this, OrderType.Guard));  // Command: Guard
                list.Add(new InternalEntry(from, 6108, 14, m_Mobile, this, OrderType.Follow)); // Command: Follow

                if (!m_Mobile.Summoned)
                    list.Add(new InternalEntry(from, 6109, 14, m_Mobile, this, OrderType.Drop));   // Command: Drop

                list.Add(new InternalEntry(from, 6111, 14, m_Mobile, this, OrderType.Attack)); // Command: Kill
                list.Add(new InternalEntry(from, 6112, 14, m_Mobile, this, OrderType.Stop));   // Command: Stop
                list.Add(new InternalEntry(from, 6114, 14, m_Mobile, this, OrderType.Stay));   // Command: Stay

                if (!m_Mobile.Summoned)
                    list.Add(new InternalEntry(from, 6113, 14, m_Mobile, this, OrderType.Transfer)); // Transfer

                list.Add(new InternalEntry(from, 6118, 14, m_Mobile, this, OrderType.Release)); // Release
            }

            if (m_Mobile.IOBFollower && m_Mobile.IOBLeader == from && !m_Mobile.Tamable)
            {
                list.Add(new InternalEntry(from, 6129, 14, m_Mobile, this, OrderType.Release)); //Dismiss
            }
        }

        public virtual void BeginPickTarget(Mobile from, OrderType order)
        {
            if (m_Mobile.Deleted || !m_Mobile.Controlled || from != m_Mobile.ControlMaster || !from.InRange(m_Mobile, 14) || from.Map != m_Mobile.Map)
                return;

            if (from.Target == null)
            {
                if (order == OrderType.Transfer)
                    from.SendLocalizedMessage(502038); // Click on the person to transfer ownership to.

                from.Target = new AIControlMobileTarget(this, order);
            }
            else if (from.Target is AIControlMobileTarget)
            {
                AIControlMobileTarget t = (AIControlMobileTarget)from.Target;

                if (t.Order == order)
                    t.AddAI(this);
            }
        }

        public virtual void EndPickTarget(Mobile from, Mobile target, OrderType order)
        {
            // adam: sanity
            if (from == null)
            {
                Console.WriteLine("(from == null) in BaseAI::EndPickTarget");
                //return;
            }

            if (m_Mobile.Deleted || !m_Mobile.Controlled || from != m_Mobile.ControlMaster || !from.InRange(m_Mobile, 14) || from.Map != m_Mobile.Map || !from.CheckAlive())
                return;

            //Special case for if it's an iob follower!
            if (m_Mobile.IOBFollower && m_Mobile.IOBLeader == from)
            {
                if (target is BaseCreature)
                {
                    BaseCreature bc = (BaseCreature)target;
                    if (bc.IOBAlignment != IOBAlignment.None)
                    {
                        if (bc.IOBAlignment == m_Mobile.IOBAlignment)
                        {
                            //Won't attack same IOBAlignment
                        }
                        else
                        {
                            m_Mobile.ControlTarget = target;
                            m_Mobile.ControlOrder = order;
                        }
                    }
                    else
                    {
                        m_Mobile.SayTo(from, "Your follower refuses to attack that creature");
                    }
                }
                else if (target is PlayerMobile)
                {
                    if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.IOBShardWide)
                        || (Server.Engines.IOBSystem.IOBRegions.IsInIOBRegion(from)
                        && Server.Engines.IOBSystem.IOBRegions.IsInIOBRegion(target)))
                    {
                        PlayerMobile pm = (PlayerMobile)target;
                        if (pm.IOBAlignment == m_Mobile.IOBAlignment || pm.IOBAlignment == IOBAlignment.None)
                        {
                            //Won't attack same IOBAlignment
                        }
                        else
                        {
                            m_Mobile.ControlTarget = target;
                            m_Mobile.ControlOrder = order;
                        }
                    }
                    else
                    {
                        m_Mobile.SayTo(from, "Your follower refuses to attack that here.");
                    }
                }
                return;
            }

            if (order == OrderType.Attack && target is BaseCreature && (((BaseCreature)target).IsScaryToPets && ((BaseCreature)target).IsScaryCondition()) && m_Mobile.IsScaredOfScaryThings)
            {
                m_Mobile.SayTo(from, "Your pet refuses to attack this creature!");
                return;
            }

            if (m_Mobile.CheckControlChance(from))
            {
                m_Mobile.ControlTarget = target;
                m_Mobile.ControlOrder = order;
            }
        }

        public virtual bool HandlesOnSpeech(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                return true;

            if (from.Alive && m_Mobile.Controlled && m_Mobile.Commandable && from == m_Mobile.ControlMaster)
                return true;

            //Pix: This is needed for the "join me" command issued to bretheren
            if (from.Alive && m_Mobile.IOBAlignment != IOBAlignment.None)
                return true;

            return (from.Alive && from.InRange(m_Mobile.Location, 3) && m_Mobile.IsHumanInTown());
        }

        private static SkillName[] m_KeywordTable = new SkillName[]
            {
                SkillName.Parry,
                SkillName.Healing,
                SkillName.Hiding,
                SkillName.Stealing,
                SkillName.Alchemy,
                SkillName.AnimalLore,
                SkillName.ItemID,
                SkillName.ArmsLore,
                SkillName.Begging,
                SkillName.Blacksmith,
                SkillName.Fletching,
                SkillName.Peacemaking,
                SkillName.Camping,
                SkillName.Carpentry,
                SkillName.Cartography,
                SkillName.Cooking,
                SkillName.DetectHidden,
                SkillName.Discordance,//??
				SkillName.EvalInt,
                SkillName.Fishing,
                SkillName.Provocation,
                SkillName.Lockpicking,
                SkillName.Magery,
                SkillName.MagicResist,
                SkillName.Tactics,
                SkillName.Snooping,
                SkillName.RemoveTrap,
                SkillName.Musicianship,
                SkillName.Poisoning,
                SkillName.Archery,
                SkillName.SpiritSpeak,
                SkillName.Tailoring,
                SkillName.AnimalTaming,
                SkillName.TasteID,
                SkillName.Tinkering,
                SkillName.Veterinary,
                SkillName.Forensics,
                SkillName.Herding,
                SkillName.Tracking,
                SkillName.Stealth,
                SkillName.Inscribe,
                SkillName.Swords,
                SkillName.Macing,
                SkillName.Fencing,
                SkillName.Wrestling,
                SkillName.Lumberjacking,
                SkillName.Mining,
                SkillName.Meditation
            };
        public void OnCombatantChange()
        {
            if (m_Mobile.Deleted || m_Mobile.ControlMaster == null || m_Mobile.ControlMaster.Deleted)
                return;

            bool worthwhileCombatant = (m_Mobile.Combatant != null && m_Mobile.Combatant.Deleted == false && m_Mobile.InRange(m_Mobile.Combatant.Location, m_Mobile.RangePerception));
            switch (m_Mobile.ControlOrder)
            {
                case OrderType.Guard:   // Disengage if combatant
                    if (worthwhileCombatant)
                        // slow down - got a combatant
                        m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Disengage);
                    else
                        // speed back up - maybe walking back to master
                        m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Engage);
                    break;
                case OrderType.Attack:  // Disengage regardless
                    // slow down
                    m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Disengage);
                    break;
            }
        }
        public virtual void OnMasterUseSkill(SkillName name)
        {
            return;
        }
        public virtual void OnMasterUseSpell(Type name)
        {
            return;
        }

        public virtual void OnIntelligentSpeech(SpeechEventArgs e)
        {
            if (e.Handled)
                return;
            else
                DoIntelligentDialog(e);
        }
        public void DoIntelligentDialog(SpeechEventArgs e)
        {
            // first determine what kind of vendor we are
            try
            {
                switch (m_Mobile.GetType().Name.Replace("Guildmaster", "").ToLower())
                {
                    case "tailor":
                    case "weaver":
                        // okay, we're taking to a talor
                        // lets see if we can understand what is being asked of us
                        if (e.Speech.ToLower().Contains("cotton") && e.Handled == false)
                        {   // they are asking about cotton, show them where the cotton fields are.
                            e.Handled = true;
                            Type type = typeof(Server.Items.CottonPlant);
                            System.Console.WriteLine("DoItemLocate... ");
                            Utility.TimeCheck tc = new Utility.TimeCheck();
                            tc.Start();
                            IntelligentDialogue.DoItemLocate(m_Mobile, e, type, "Thou might find {0} fields near ");
                            tc.End();
                            System.Console.WriteLine("DoItemLocate finished in {0} seconds (main thread).", tc.TimeTaken);
                        }
                        else if (e.Speech.ToLower().Contains("make") && e.Speech.ToLower().Contains("cloth") && e.Handled == false)
                        {   // they are asking how to make cloth, tell them how!
                            e.Handled = true;
                            string text = string.Format("Bring your cotton here and spin it into thread on the spinning wheel.");
                            m_Mobile.SayTo(e.Mobile, text);
                            text = string.Format("Thou will then use the loom to weave cloth from the thread.");
                            m_Mobile.SayTo(e.Mobile, text);
                        }
                        else if (e.Handled == false)
                        {   // 3/14/23, Adam: don't set handled here.. allow processing to continue
                            //  otherwise "colton buy" will fail since we were not asking one of the special questions.
                            //e.Handled = true;
                            //m_Mobile.SayTo(e.Mobile, "I'm sorry, I do not know anything about that.");
                        }
                        break;
                    case "animaltrainer":
                        if (e.Handled == false && IntelligentDialogue.WhenToSpeakRules(m_Mobile, e))
                        {   // they are asking about X, show them where the X is/are.
                            e.Handled = true;
                            if (e.Speech.Contains("pet", StringComparison.OrdinalIgnoreCase))
                                AnimalTrainer.FindMyPet(m_Mobile, e);
                            else
                            {
                                System.Console.WriteLine("DoMobileLocate... ");
                                Utility.TimeCheck tc = new Utility.TimeCheck();
                                tc.Start();
                                IntelligentDialogue.DoMobileLocate(m_Mobile, e, "Thou might find {0} near ");
                                tc.End();
                                System.Console.WriteLine("DoMobileLocate finished in {0} seconds (main thread).", tc.TimeTaken);
                            }
                        }
                        break;
                    case "patrolguard":
                        if (e.Speech.ToLower() == "-IDSRegressionTest".ToLower() && e.Mobile.AccessLevel >= AccessLevel.Administrator)
                            IDSRegressionTest.IDS_OnCommand(new CommandEventArgs(e.Mobile, null, null, null));
                        else if (e.Handled == false && IntelligentDialogue.WhenToSpeakRules(m_Mobile, e))
                        {   // they are asking about X, show them where the X is/are.
                            e.Handled = true;
                            System.Console.WriteLine("DoGenericLocate... ");
                            Utility.TimeCheck tc = new Utility.TimeCheck();
                            tc.Start();
#if true // run as a background task
                            IntelligentDialogue.LoadPlayerCache(m_Mobile.Location);
                            IntelligentDialogue.LoadMobilesCache();
                            Console.Out.Flush();
                            Task.Factory.StartNew(() => IntelligentDialogue.DoGenericLocate(m_Mobile, e, true));
#else
                            IntelligentDialogue.DoGenericLocate(m_Mobile, e, false);
#endif
                            tc.End();
                            System.Console.WriteLine("DoGenericLocate finished in {0} seconds (main thread).", tc.TimeTaken);
                        }
                        break;

                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public virtual void OnSpeech(SpeechEventArgs e)
        {

            #region [Vendor] Buy Sell
            /// when a vendor enters battle, they switch from AI_Vendor to AI_Melee and will no longer talk to customers
            if (m_Mobile is BaseVendor && e.Mobile.InRange(m_Mobile, 4) && !e.Handled && m_Mobile.AI == AIType.AI_Melee &&
                (e.HasKeyword(0x14D) || /*vendor sell*/
                e.HasKeyword(0x3C) ||  // *vendor buy*
                (WasNamed(e.Speech) && (e.HasKeyword(0x177) || /*sell*/	e.HasKeyword(0x171)) /*buy*/)))
            {
                e.Handled = true;
                // I am too busy fighting to deal with thee!
                m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
            }
            #endregion [Vendor] Buy Sell

            #region Move Time, and Train
            else if (e.Mobile.Alive && e.Mobile.InRange(m_Mobile.Location, 3) && m_Mobile.IsHumanInTown())
            {
                if (e.HasKeyword(0x9D) && WasNamed(e.Speech)) // *move*
                {
                    if (m_Mobile.Combatant != null)
                    {
                        // I am too busy fighting to deal with thee!
                        m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                    }
                    else
                    {
                        // Excuse me?
                        m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501516);
                        WalkRandomInHome(2, 2, 1, evenIfStationed: true);
                    }

                    // Adam, guards and animal trainers have extended speech processing, so to prevent them from trying to process this
                    //  we set Handled. (Shouldn't this always be the case?)
                    if (m_Mobile is BaseGuard || m_Mobile is AnimalTrainer)
                        e.Handled = true;
                }       // *move*
                else if (e.HasKeyword(0x9E) && WasNamed(e.Speech)) // *time*
                {
                    if (m_Mobile.Combatant != null)
                    {
                        // I am too busy fighting to deal with thee!
                        m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                    }
                    else
                    {
                        int generalNumber;
                        string exactTime;

                        Clock.GetTime(m_Mobile, out generalNumber, out exactTime);

                        m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, generalNumber);
                    }
                }  // *time*
                else if (e.HasKeyword(0x6C) && WasNamed(e.Speech))      // *train
                {
                    if (m_Mobile.Combatant != null)
                    {
                        // I am too busy fighting to deal with thee!
                        m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                    }
                    else
                    {
                        bool foundSomething = false;

                        Skills ourSkills = m_Mobile.Skills;
                        Skills theirSkills = e.Mobile.Skills;

                        for (int i = 0; i < ourSkills.Length && i < theirSkills.Length; ++i)
                        {
                            Skill skill = ourSkills[i];
                            Skill theirSkill = theirSkills[i];

                            if (skill != null && theirSkill != null && skill.Base >= 60.0 && m_Mobile.CheckTeach(skill.SkillName, e.Mobile))
                            {
                                double toTeach = skill.Base / 3.0;

                                if (toTeach > 42.0)
                                    toTeach = 42.0;

                                if (toTeach > theirSkill.Base)
                                {
                                    int number = 1043059 + i;

                                    if (number > 1043107)
                                        continue;

                                    if (!foundSomething)
                                        m_Mobile.Say(1043058); // I can train the following:

                                    m_Mobile.Say(number);

                                    /* DEBUG - testing resolution of what thetrainer says he can teach and the 'real' skill name
									string lookup = "";
									bool ok = Server.Text.Cliloc.Lookup.TryGetValue(number, out lookup);
									if (ok)
									{
										Console.WriteLine(Server.Text.Cliloc.Lookup[number]);
										string sq = Utility.Levenshtein.BestEnumMatch(typeof(SkillName), lookup);
										//m_Mobile.Say("Corrected: {0}", sq);
										if (sq.ToLower() != lookup)
										Console.WriteLine("Corrected: {0}", sq);
									}*/

                                    foundSomething = true;
                                }
                            }
                        }

                        if (!foundSomething)
                            m_Mobile.Say(501505); // Alas, I cannot teach thee anything.
                    }
                }  // *train
                else if (e.Speech.IndexOf(" train ", StringComparison.CurrentCultureIgnoreCase) != -1)
                {   // expand on the limited keyword modle to allow for key phrases, like: Bobby teach animal lore 100
                    int index = 0;
                    if (WasNamed(e.Speech))
                    { // the first arg is the NPC's name
                        index++;
                    }
                    // break up our string into tokens
                    string[] tokens = e.Speech.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    // parse out the amount they want to spend
                    int amount = 0;
                    bool ok = int.TryParse(tokens[tokens.Length - 1], out amount);
                    // get the tokens
                    string skillname = "";
                    for (int jx = index + 1; jx < tokens.Length - ((ok) ? 1 : 0); jx++)
                    {
                        skillname += tokens[jx];
                    }
                    BaseCreature bc = this.m_Mobile as BaseCreature;
                    if (bc != null)
                    {
                        // didn't specify an amount. In that case, we'll just tell them how much it will cost
                        if (ok == false)
                        {   // lookup the actual skill name
                            int score;
                            string real_name = IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), skillname, out score);
                            if (real_name != null && real_name != "" && score <= 5)
                            { // found the skill they want to train
                              // We'll let Teach() explain the terms
                                bc.Teach((SkillName)Enum.Parse(typeof(SkillName), real_name, true), e.Mobile, 0, ok);
                            }
                        }
                        else
                        {
                            // lookup the actual skill name
                            int score;
                            string real_name = IntelligentDialogue.Levenshtein.BestEnumMatch(typeof(SkillName), skillname, out score);
                            if (real_name != null && real_name != "" && score <= 5)
                            { // found the skill they want to train
                              // calculate the cost
                                int cost = 0;
                                if (bc.CheckTeachSkills((SkillName)Enum.Parse(typeof(SkillName), real_name, true), e.Mobile,
                                    amount, ref cost, false) == BaseCreature.TeachResult.Success)
                                {
                                    // show me the money honey!
                                    Container cont = e.Mobile.Backpack;
                                    BaseVendor vendor = this.m_Mobile as BaseVendor;
                                    if (cont != null && vendor != null)
                                    {   // try to get it from their backpack first
                                        if (vendor.ConsumeTotal(e.Mobile, cont, typeof(UnemploymentCheck), cost) || cont.ConsumeTotal(typeof(Gold), cost))
                                        {
                                            // Teach
                                            bc.Teach((SkillName)Enum.Parse(typeof(SkillName), real_name, true), e.Mobile, amount, true);
                                        }
                                        else
                                        {
                                            vendor.SayTo(e.Mobile, 500192);//Begging thy pardon, but thou casnt afford that.
                                        }
                                    }
                                }
                                else
                                    // They know as much as I do. Just let Teach explain that to them
                                    bc.Teach((SkillName)Enum.Parse(typeof(SkillName), real_name, true), e.Mobile, 0, false);
                            }
                        }
                    }
                }
                else                                                   // *train <skill>
                {
                    SkillName toTrain = (SkillName)(-1);

                    for (int i = 0; toTrain == (SkillName)(-1) && i < e.Keywords.Length; ++i)
                    {
                        int keyword = e.Keywords[i];

                        if (keyword == 0x154)
                        {
                            toTrain = SkillName.Anatomy;
                        }
                        else if (keyword >= 0x6D && keyword <= 0x9C)
                        {
                            int index = keyword - 0x6D;

                            if (index >= 0 && index < m_KeywordTable.Length)
                                toTrain = m_KeywordTable[index];
                        }
                    }

                    if (toTrain != (SkillName)(-1) && WasNamed(e.Speech))
                    {
                        if (m_Mobile.Combatant != null)
                        {
                            // I am too busy fighting to deal with thee!
                            m_Mobile.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                        }
                        else
                        {
                            Skills skills = m_Mobile.Skills;
                            Skill skill = skills[toTrain];

                            if (skill == null || skill.Base < 60.0 || !m_Mobile.CheckTeach(toTrain, e.Mobile))
                            {
                                m_Mobile.Say(501507); // 'Tis not something I can teach thee of.
                            }
                            else
                            {
                                m_Mobile.Teach(toTrain, e.Mobile, 0, false);
                            }
                        }
                    }
                }                                                // *train <skill>
            }   // move, time train
            #endregion Move Time, and Train

            string heardspeech = e.Speech;

            #region Normal Commands
            if (m_Mobile.Controlled && m_Mobile.Commandable)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I listen");

                if (e.Mobile.Alive && e.Mobile == m_Mobile.ControlMaster)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "Its from my master");

                    // erl: clear herding attempt, if one exists
                    m_Mobile.TargetLocation = Point2D.Zero;

                    int[] keywords = e.Keywords;
                    string speech = e.Speech;

                    // First, check the all*
                    for (int i = 0; i < keywords.Length; ++i)
                    {
                        int keyword = keywords[i];

                        switch (keyword)
                        {
                            case 0x164: // all come
                                {
                                    if (m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Come;
                                    }

                                    return;
                                }
                            case 0x165: // all follow
                                {
                                    BeginPickTarget(e.Mobile, OrderType.Follow);
                                    return;
                                }
                            case 0x166: // all guard
                                {
                                    if (m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Guard;
                                    }
                                    return;
                                }
                            case 0x167: // all stop
                                {
                                    if (m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Stop;
                                    }
                                    return;
                                }
                            case 0x168: // all kill
                            case 0x169: // all attack
                                {
                                    if (m_Mobile.CanBeDamaged())
                                        BeginPickTarget(e.Mobile, OrderType.Attack);

                                    if (!m_Mobile.CanBeDamaged())
                                        m_Mobile.DebugSay(DebugFlags.AI, string.Format("I'm {0}, I can't do that!", m_Mobile.Blessed ? "blessed" : m_Mobile.IsInvulnerable ? "invulnerable" : "dead pet"));
                                    return;
                                }
                            case 0x16B: // all guard me
                                {
                                    if (m_Mobile.CanBeDamaged() && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = e.Mobile;
                                        m_Mobile.ControlOrder = OrderType.Guard;
                                    }
                                    if (!m_Mobile.CanBeDamaged())
                                        m_Mobile.DebugSay(DebugFlags.AI, string.Format("I'm {0}, I can't do that!", m_Mobile.Blessed ? "blessed" : m_Mobile.IsInvulnerable ? "invulnerable" : "dead pet"));
                                    return;
                                }
                            case 0x16C: // all follow me
                                {
                                    if (m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = e.Mobile;
                                        m_Mobile.ControlOrder = OrderType.Follow;
                                    }
                                    return;
                                }
                            case 0x170: // all stay
                                {
                                    if (m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Stay;
                                    }
                                    return;
                                }
                        }
                    }

                    // No all*, so check *command
                    for (int i = 0; i < keywords.Length; ++i)
                    {
                        int keyword = keywords[i];

                        switch (keyword)
                        {
                            case 0x155: // *come
                                {
                                    if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Come;
                                    }

                                    return;
                                }
                            case 0x156: // *drop
                                {
                                    if (!m_Mobile.IsDeadPet && !m_Mobile.Summoned && WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Drop;
                                    }

                                    return;
                                }
                            case 0x15A: // *follow
                                {
                                    if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                        BeginPickTarget(e.Mobile, OrderType.Follow);

                                    return;
                                }
                            case 0x15C: // *guard
                                {
                                    if (m_Mobile.CanBeDamaged() && !m_Mobile.IsDeadPet && WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Guard;
                                    }

                                    if (!m_Mobile.CanBeDamaged())
                                        m_Mobile.DebugSay(DebugFlags.AI, string.Format("I'm {0}, I can't do that!", m_Mobile.Blessed ? "blessed" : m_Mobile.IsInvulnerable ? "invulnerable" : "dead pet"));

                                    return;
                                }
                            case 0x15D: // *kill
                            case 0x15E: // *attack
                                {
                                    if (m_Mobile.CanBeDamaged() && !m_Mobile.IsDeadPet && WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                        BeginPickTarget(e.Mobile, OrderType.Attack);

                                    if (!m_Mobile.CanBeDamaged())
                                        m_Mobile.DebugSay(DebugFlags.AI, string.Format("I'm {0}, I can't do that!", m_Mobile.Blessed ? "blessed" : m_Mobile.IsInvulnerable ? "invulnerable" : "dead pet"));

                                    return;
                                }
                            case 0x15F: // *patrol
                                {
                                    if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Patrol;
                                    }

                                    return;
                                }
                            case 0x161: // *stop
                                {
                                    if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Stop;
                                    }

                                    return;
                                }
                            case 0x163: // *follow me
                                {
                                    if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = e.Mobile;
                                        m_Mobile.ControlOrder = OrderType.Follow;
                                    }

                                    return;
                                }
                            case 0x16D: // *release
                                {   // 1/8/2024, Adam: You cannot release township NPCs in this way
                                    //if (m_Mobile is not ITownshipNPC)
                                    // 8/30/21, Adam: you shouldn't need a Loyalty check to release a pet. 
                                    if (WasNamed(speech) /*&& m_Mobile.CheckControlChance(e.Mobile)*/)
                                    {
                                        if (!m_Mobile.Summoned)
                                        {
                                            e.Mobile.SendGump(new Gumps.ConfirmReleaseGump(e.Mobile, m_Mobile));
                                        }
                                        else
                                        {
                                            m_Mobile.ControlTarget = null;
                                            m_Mobile.ControlOrder = OrderType.Release;
                                        }
                                    }
                                    return;
                                }

                            case 0x16E: // *transfer
                                {
                                    if (!m_Mobile.IsDeadPet && WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        if (m_Mobile.Summoned)
                                            e.Mobile.SendLocalizedMessage(1005487); // You cannot transfer ownership of a summoned creature.
                                        else if (m_Mobile.MinTameSkill == 0.0)      // StrudyPackLlama and SturdyPackHorse
                                            e.Mobile.SendMessage("You cannot transfer ownership of this creature.");
                                        else
                                            BeginPickTarget(e.Mobile, OrderType.Transfer);
                                    }

                                    return;
                                }
                            case 0x16F: // *stay
                                {
                                    if (WasNamed(speech) && m_Mobile.CheckControlChance(e.Mobile))
                                    {
                                        m_Mobile.ControlTarget = null;
                                        m_Mobile.ControlOrder = OrderType.Stay;
                                    }

                                    return;
                                }
                        }
                    }
                }
            }       // m_Mobile.Controlled && m_Mobile.Commandable
            #endregion Normal Commands

            #region Custom  Commands
            if (m_Mobile.Controlled && m_Mobile.Commandable)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I listen");

                if (e.Mobile.Alive && e.Mobile == m_Mobile.ControlMaster)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "Its from my master");

                    // erl: clear herding attempt, if one exists
                    m_Mobile.TargetLocation = Point2D.Zero;

                    int[] keywords = e.Keywords;
                    string speech = e.Speech;

                    #region command extensions
                    {
                        string[] tokens = speech.ToLower().Split(new char[] { ' ' });
                        if (tokens.Length == 2 && WasNamed(e.Speech) || tokens[0] == "all")
                        {
                            switch (tokens[1])
                            {
                                case "mount":
                                    {
                                        if (m_Mobile.Body.IsHuman && m_Mobile.CommandMount())
                                            if (tokens[0] != "all")
                                                e.Handled = true;
                                        break;
                                    }
                                case "dismount":
                                case "unmount":
                                    {
                                        if (m_Mobile.Body.IsHuman && m_Mobile.CommandDismount())
                                            if (tokens[0] != "all")
                                                e.Handled = true;
                                        break;
                                    }
                            }
                        }
                    }

                    #region allow orcs to command in their own language
                    if (speech.ToLower() == "lat stai") // all stay
                    {
                        if (m_Mobile.CheckControlChance(e.Mobile))
                        {
                            m_Mobile.ControlTarget = null;
                            m_Mobile.ControlOrder = OrderType.Stay;
                        }
                        return;
                    }
                    if (speech.ToLower() == "lat clomp")
                    {
                        BeginPickTarget(e.Mobile, OrderType.Attack);
                        return;
                    }
                    if (speech.ToLower() == "lat follow")
                    {
                        BeginPickTarget(e.Mobile, OrderType.Follow);
                        return;
                    }
                    #endregion allow orcs to command in their own language

                    #endregion command extensions
                }
            }       // m_Mobile.Controlled && m_Mobile.Commandable
            #endregion Custom Commands

            #region Take control of any creature with Obey command
            else
            {
                if (e.Mobile.AccessLevel >= AccessLevel.GameMaster)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "Its from a GM");

                    if (m_Mobile.FindMyName(e.Speech, true))
                    {
                        string[] str = e.Speech.Split(' ');
                        int i;

                        for (i = 0; i < str.Length; i++)
                        {
                            string word = str[i];

                            if (Insensitive.Equals(word, "obey"))
                            {
                                m_Mobile.SetControlMaster(e.Mobile, force: true);

                                if (m_Mobile.Summoned)
                                    m_Mobile.SummonMaster = e.Mobile;

                                return;
                            }
                        }
                    }
                }
            }                                                   // everything else
            #endregion Take control of any creature with Obey command
        }

        //public virtual RefreshStam
        private bool HotPursuit()
        {
            return m_Mobile.Combatant != null && m_Path != null && m_Path.Success;
        }
        #region PrioritizeCombatant
        /// <summary>
        /// Prioritize our target based on damagers to us.
        /// One problem with AI is that we have like 20 different AI classes derived from BaseAI.
        /// My confidence that all of them handle switching Combatant in a consistent and 'correct' manner is low.
        /// PrioritizeCombatant addresses this shortcoming by preprocessing the Combatant before DoActionCombat is called.
        /// One possible problem is that some AIs may have specific ways to handle this. When this is the case, this virtual should be overridden.
        /// mobile.PrioritizeCombatant vs BaseAI.AcquireFocusMob
        ///     BaseAI.AcquireFocusMob() is called when we have no combatant
        ///     BaseAI.PrioritizeCombatant() is called when we have a combatant AND one or more damagers that should be considered. 
        /// </summary>
        /// <returns>true if we switched combatants</returns>
        public virtual bool PrioritizeCombatant(Mobile target = null)
        {
            // who we are prioritizing for
            if (target == null)
                target = m_Mobile;

            Mobile new_target = GetPriorityCombatant(target);
            if (new_target != null && new_target != target.Combatant)
            {   // if we are discovering for a BaseCreature, clear the path
                if (target is BaseCreature bc && bc.AIObject != null)
                {   // clear BaseCreature path
                    if (bc.AIObject.Path != null)
                    {   // stop who we were chasing
                        bc.AIObject.Path = null;
                        new_target.DebugSay(DebugFlags.Aggression, "Clearing my current path");
                    }
                }

                // new high priority target
                new_target.DebugSay(DebugFlags.Aggression, "{0} is a higher priority target. I will switch Combatant to {1}", new_target, new_target);
                target.Combatant = new_target;
                return true;
            }
            return false;
        }
        private void KissAndMakup(Mobile target)
        {
            if (target == null)
                return;

            // target (probably control master) was previously an aggressor, forgive and move on
            if (m_Mobile.DamageEntries != null && m_Mobile.DamageEntries.Count > 0)
            {
                // okay, loop through our damagers and put them in a list
                for (int i = m_Mobile.DamageEntries.Count - 1; i >= 0; --i)
                {
                    if (i >= m_Mobile.DamageEntries.Count)
                        continue;

                    DamageEntry de = (DamageEntry)m_Mobile.DamageEntries[i];

                    // expired
                    if (de.HasExpired)
                    {
                        m_Mobile.DamageEntries.RemoveAt(i);
                        continue;
                    }

                    if (de.Damager == target)
                    {
                        m_Mobile.DamageEntries.RemoveAt(i);
                        continue;
                    }
                }
            }

            if (m_Mobile.Aggressors != null && m_Mobile.Aggressors.Count > 0)
            {
                for (int i = m_Mobile.Aggressors.Count - 1; i >= 0; --i)
                {
                    if (i >= m_Mobile.Aggressors.Count)
                        continue;

                    AggressorInfo info = (AggressorInfo)m_Mobile.Aggressors[i];

                    if (info.Attacker == target)
                    {
                        m_Mobile.Aggressors.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
        public virtual Mobile GetPriorityCombatant(Mobile target = null)
        {   // who we are prioritizing for
            if (target == null)
                target = m_Mobile;

            // nothing to do
            if (target == null || target.Deleted)
                return null;

            Mobile new_combatant = null;
            Mobile master = m_Mobile.ControlMaster;

            #region First Priority Damagers
            if (target.DamageEntries != null && target.DamageEntries.Count > 0)
            {
                // sorted list of possible targets
                List<Tuple<Mobile, int>> list = new();
                int new_combatant_damage = 0;

                // get all the common info on our combatant
                MobileInfo info = GetMobileInfo(new MobileInfo(target.Combatant));
                if (info.available)
                {   // make sure our combatant is in the list
                    DamageEntry cde = target.FindDamageEntryFor(target.Combatant);
                    new_combatant_damage = cde != null ? cde.DamageGiven : 0;
                    list.Add(new Tuple<Mobile, int>(target.Combatant, new_combatant_damage));
                }

                // okay, loop through our damagers and put them in a list
                for (int i = target.DamageEntries.Count - 1; i >= 0; --i)
                {
                    if (i >= target.DamageEntries.Count)
                        continue;

                    DamageEntry de = (DamageEntry)target.DamageEntries[i];

                    // expired
                    if (de.HasExpired)
                    {
                        target.DamageEntries.RemoveAt(i);
                        continue;
                    }

                    // self-inflicted
                    if (de.Damager == target)
                        continue;

                    // get all the common info on our damager
                    MobileInfo damager_info = GetMobileInfo(new MobileInfo(de.Damager));

                    // unavailable? ignore them
                    if (damager_info.target == null || damager_info.gone || damager_info.dead || damager_info.hidden || damager_info.fled)
                        continue;
                    else if (de.Damager != target.Combatant) // already added
                        list.Add(new Tuple<Mobile, int>(de.Damager, de.DamageGiven));
                }

                // now sort the list on damage
                list.Sort((x, y) => y.Item2.CompareTo(x.Item2));

                // see if we should pick a better target. better == someone/something giving us more damage
                new_combatant = target.Combatant;
                foreach (var unit in list)
                {   // see if we have a higher priority combatant. Ping-pong keeps us from switching targets too often
                    if (unit.Item2 > new_combatant_damage && !PingPong(unit.Item1))
                    {   // see if we should switch combatants
                        bool clear_path = Spawner.ClearPathLand(target.Location, unit.Item1.Location, map: m_Mobile.Map);
                        if (clear_path || target.Location == unit.Item1.Location || target.InRange(unit.Item1.Location, 3))
                        {   // new high priority target
                            new_combatant = unit.Item1;
                            new_combatant_damage = unit.Item2;
                            break;
                        }
                    }
                }
            }
            #endregion First Priority Damagers

            #region Second Priority Aggressors
            if (new_combatant == null && target.Combatant == null)
                new_combatant = GetPriorityAggressor(target);
            #endregion Second Priority Aggressors

            if (new_combatant != target.Combatant)
                return new_combatant;
            else
                return null;
        }
        public virtual Mobile GetPriorityAggressor(Mobile target)
        {
            BaseCreature me = m_Mobile;
            List<AggressorInfo> aggressors = target.Aggressors;
            Mobile combatant = null;
            Mobile master = m_Mobile.ControlMaster;
            if (aggressors.Count > 0)
            {
                for (int i = 0; i < aggressors.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)aggressors[i];
                    Mobile attacker = info.Attacker;

                    if (attacker != null && !attacker.Deleted && attacker.GetDistanceToSqrt(me) <= me.RangePerception)
                    {
                        if (target.Alive)
                        {
                            if (combatant == null || attacker.GetDistanceToSqrt(target) < combatant.GetDistanceToSqrt(target))
                            {
                                if (me.CanSee(attacker) && attacker.Alive && attacker != master)
                                {
                                    combatant = attacker;
                                }
                            }
                        }
                        else
                        {
                            if (combatant == null || attacker.GetDistanceToSqrt(me) < combatant.GetDistanceToSqrt(me))
                            {
                                if (me.CanSee(attacker) && attacker.Alive && attacker != master)
                                {
                                    combatant = attacker;
                                }
                            }
                        }
                    }
                }

                if (combatant != null)
                {
                    //me.DebugSay(DebugFlags.AI, "Crap, my master has been attacked! I will atack one of those bastards!");
                    return combatant;
                }
            }

            return null;
        }
        Memory ping_pong = new();
        private bool PingPong(Mobile m)
        {
            // This limits how often we will switch targets
            if (ping_pong.Recall(m) == false)
            {   // remember this player for 2 minutes. 
                ping_pong.Remember(m, 1, 120);
                m_Mobile.DebugSay(DebugFlags.Aggression, "Ping-pong: remembering");
                return false;
            }
            else
            {   // we remember them, see how many times we have ping ponged.
                Memory.ObjectMemory ox = ping_pong.Recall((object)m);
                int count = (int)ox.Context;
                ox.Context = count + 1;
                if (count >= 2)
                {
                    m_Mobile.DebugSay(DebugFlags.Aggression, "Ping-pong: I will not switch targets");
                    return true;
                }
                else
                {
                    m_Mobile.DebugSay(DebugFlags.Aggression, "Ping-pong: I will switch targets");
                    return false;
                }
            }
        }
        #endregion PrioritizeCombatant
        public virtual bool Think()
        {
            try
            {
                if (m_Mobile == null || m_Mobile.Deleted)
                    return false;

                if (CheckFlee())
                    return true;

                if (m_Mobile.DoActionOverride(false))
                    return true;

                #region PrioritizeCombatant
                // leave control-masters/tamers in control of their pets
                //  things like EVs and BS aren't controlled so will fall under this repriority alignment
                if (m_Mobile.Controlled == false)
                    // pick a better combatant if appropriate
                    if (Action == ActionType.Combat || Action == ActionType.Guard)
                        if (PrioritizeCombatant() && Action == ActionType.Guard)
                            // short circuit the 'chicken dance'
                            Action = ActionType.Combat;
                #endregion PrioritizeCombatant

                // get all the common info on our combatant
                // if we're chasing someone, we want to increase our range
                MobileInfo info = GetMobileInfo(new MobileInfo(m_Mobile.Combatant));
                if (info.target != null)
                    info.range = info.path ? m_Mobile.InRange(info.target, m_Mobile.RangePerception * 2) : info.range;

                if (m_Mobile.Frozen == true)
                    m_Mobile.DebugSay(DebugFlags.AI, "I am frozen.");
                if (m_Mobile.Paralyzed == true)
                    m_Mobile.DebugSay(DebugFlags.AI, "I am paralyzed.");

                switch (Action)
                {
                    case ActionType.Wander:
                        m_Mobile.OnActionWander();
                        return DoActionWander();

                    case ActionType.Combat:
                        {
                            m_Mobile.OnActionCombat(info: info);

                            // Don't let the combatant timers expire if we are in hot pursuit (pathing)
                            if (HotPursuit())
                            {
                                if (Core.TickCount > m_Mobile.NextCombatReset)
                                {
                                    m_Mobile.DebugSay(DebugFlags.Combatant, "restarting combat timers");
                                    m_Mobile.RestartCombatTimers();
                                    // ExpireCombatantTimer timer has a 1 minute 'give up' time. So we subtract 3 seconds from that
                                    //  so every 57 seconds we restart the timers
                                    // Why? Because ExpireCombatantTimer sets the Combatant to null, which is unwanted when we are pathing after someone.
                                    m_Mobile.NextCombatReset = Core.TickCount + (60000 - 3000);
                                }
                                else
                                    m_Mobile.DebugSay(DebugFlags.Combatant, "letting combat timers run");
                            }

                            return DoActionCombat(info: info);
                        }

                    case ActionType.Guard:
                        m_Mobile.OnActionGuard();
                        return DoActionGuard();

                    case ActionType.Hunt:
                        m_Mobile.OnActionHunt();
                        return DoActionHunt();

                    case ActionType.NavStar:
                        m_Mobile.OnActionNavStar();
                        return DoActionNavStar();

                    case ActionType.Flee:
                        m_Mobile.OnActionFlee();
                        return DoActionFlee();

                    case ActionType.Interact:
                        m_Mobile.OnActionInteract();
                        return DoActionInteract();

                    case ActionType.Backoff:
                        m_Mobile.OnActionBackoff();
                        return DoActionBackoff();

                    // wea: new chasing action
                    case ActionType.Chase:
                        m_Mobile.OnActionChase(info: info);
                        return DoActionChase(info: info);

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                // this mobile's AI has crashed. We can no longer trust him
                if (m_Mobile != null && m_Mobile.Deleted == false)
                    //m_Mobile.Delete();
                    Utility.ConsoleWriteLine(string.Format("The AI for mobile {0} crashed at {1}. See exception log.", m_Mobile, m_Mobile.Location), ConsoleColor.Red);
                return false;
            }
        }
        public bool WeaponWearsOut(BaseWeapon weapon, Mobile attacker, Mobile defender)
        {
            return attacker != m_Mobile.ControlMaster && m_Mobile.UsesHumanWeapons;
        }
        public virtual void OnActionChanged(ActionType oldAction)
        {
            switch (Action)
            {
                case ActionType.Wander:
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.FocusMob = null;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    // 4/15/23, Adam: short-circuit the NextReacquireTime when switching to Wander mode from Combat mode
                    //  this prevents us from simply walking around a bunch of enemies after say killing one of them
                    if (oldAction == ActionType.Combat || oldAction == ActionType.Guard)
                        m_Mobile.ForceReAcquire();
                    break;

                case ActionType.Combat:
                    m_Mobile.Warmode = true;
                    m_Mobile.FocusMob = null;
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    break;

                case ActionType.Guard:
                    m_Mobile.Warmode = true;
                    m_Mobile.FocusMob = null;
                    m_Mobile.Combatant = null;
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    m_NextStopGuard = DateTime.UtcNow + TimeSpan.FromSeconds(10);
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    // 4/15/23, Adam: short-circuit the NextReacquireTime when switching to Guard mode from Combat mode
                    //  this prevents us from simply walking around a bunch of enemies after say killing one of them
                    if (oldAction == ActionType.Combat)
                        m_Mobile.ForceReAcquire();
                    break;

                case ActionType.Hunt:
                    m_Mobile.FocusMob = null;
                    m_Mobile.Combatant = null;
                    m_NextStopHunt = DateTime.UtcNow + TimeSpan.FromSeconds(20);
                    break;

                case ActionType.NavStar:
                    m_Mobile.Warmode = false;
                    m_Mobile.FocusMob = null;
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    break;

                case ActionType.Flee:
                    m_Mobile.Warmode = true;
                    if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.OldFlee)) // we think this is a bug, so we have it on a switch for now
                        m_Mobile.FocusMob = null;
                    m_Mobile.CurrentSpeed = m_Mobile.ActiveSpeed;
                    break;

                case ActionType.Interact:
                    m_Mobile.Warmode = false;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    break;

                case ActionType.Backoff:
                    m_Mobile.Warmode = false;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    break;

                case ActionType.Chase:
                    break;
            }
            /*
             * Any Change in Action 
             * 
             */
            m_Mobile.NextCombatReset = 0;
            m_Mobile.TargetLocationExpire = 0;

            // unset any path we had going. no longer valid
            if (m_Path != null) m_Path = null;
        }

        public virtual bool OnAtWayPoint()
        {
            return true;
        }
        private string MobileName(Mobile m)
        {
            return m.Name != null ? m.Name : m.GetType().Name;
        }
        public bool DidBustStuff()
        {   // BlockDamage creatures will proactively bust stuff (energy fields, paralyze fields,) before failing the move
            if (!m_Mobile.Deleted && m_Mobile.Map != null && m_Mobile.BlockDamage == true)
            {
                bool busted = false;
                IPooledEnumerable eable = m_Mobile.Map.GetObjectsInRange(m_Mobile.Location, 2);
                foreach (object obj in eable)
                {
                    if (obj is Item item && item.GetType().IsDefined(typeof(Server.Misc.DispellableFieldAttribute), false))
                    {
                        if (!(Math.Abs(item.Z - m_Mobile.Z) > 1))
                        {
                            DispelIt(item);
                            busted = true;
                        }
                    }
                    else if (obj is Mobile m && (m.Player || m is BaseCreature bc && bc.Controlled) && m != m_Mobile)
                        // Mission Critical + BlockDamage == Hulk Smash
                        if (m_Mobile.MissionCritical)      // we have somewhere to be, we won't be stopped (normally used with a way point)
                            if (!(Math.Abs(m.Z - m_Mobile.Z) > 1) && m_Mobile.GetDistanceToSqrt(m) < 2)
                                if (HulkSmash(m))
                                    busted = true;
                }
                eable.Free();

                string text = $"* {m_Mobile.SafeName} shrugs off your feeble attempt *";
                if (busted && m_Mobile.CheckString(text))
                    m_Mobile.Emote(text);

                return busted;
            }

            return false;
        }
        private bool HulkSmash(Mobile them)
        {
            if (them.AccessLevel != AccessLevel.Player || (them is BaseCreature bc && (bc.IsInvulnerable || bc.Blessed || bc.GetMobileBool(Mobile.MobileBoolTable.TrainingMobile))))
                return false;

            Mobile me = m_Mobile as Mobile;

            SpellHelper.Turn(me, them);

            if (me.Weapon is BaseWeapon weapon)
            {
                Mobile attacker = me;
                Mobile defender = them;
                weapon.PlaySwingAnimation(attacker);
                weapon.PlayHurtAnimation(defender);

                attacker.PlaySound(weapon.GetHitAttackSound(attacker, defender));
                defender.PlaySound(weapon.GetHitDefendSound(attacker, defender));
            }

            Blood blood = new Blood(Utility.Random(0x122A, 5), Utility.Random(15 * 60, 5 * 60));
            blood.MoveToWorld(them.Location, them.Map);

            Point3D point = me.Location;
            for (int count = 10; count > 0; count--)
            {
                point = Spawner.GetSpawnPosition(them.Map, them.Location, homeRange: 3, SpawnFlags.ClearPath, them);
                if (point == them.Location)
                {   // couldn't find a spawn point, so we will move the problem under us.
                    point = me.Location;
                    break;
                }
                // we don't want to move someone into an area/building they should not be in
                if (me.InLOS(point))
                    break;
            }

            Point3D from = them.Location;
            Point3D to = point;

            them.Location = to;
            them.ProcessDelta();

            if (them is BaseCreature pet && pet.ControlOrder == OrderType.Stay)
                pet.ControlOrder = OrderType.Stop;

            return true;
        }
        private void DispelIt(Item item)
        {
            SpellHelper.Turn(m_Mobile, item);

            Effects.SendLocationParticles(EffectItem.Create(item.Location, item.Map, EffectItem.DefaultDuration), 0x376A, 9, 20, 5042);
            Effects.PlaySound(item.GetWorldLocation(), item.Map, 0x201);

            item.Delete();
        }
        public virtual bool DoActionWander()
        {
            if (m_Mobile.Combatant == null)
                m_Mobile.DebugSay(DebugFlags.AI, "I am wandering");
            else
                m_Mobile.DebugSay(DebugFlags.AI, "I am wandering, but have my eye on {0}", MobileName(m_Mobile.Combatant));

            #region Don't block BlockDamage creatures
            // BlockDamage creatures will proactively bust stuff (energy fields, paralyze fields,) before failing the move
            if (DidBustStuff())
                m_Mobile.DebugSay(DebugFlags.AI, "Ya, I busted something");
            #endregion Don't block BlockDamage creatures

            // SomethingToInvestigate
            Direction sti = Direction.North;

            // NavStar
            if (!string.IsNullOrEmpty(((BaseCreature)m_Mobile).NavDestination))
                Action = ActionType.NavStar;

            // WayPoint
            if (m_Mobile.CurrentWayPoint != null && m_Mobile.CurrentWayPoint.Deleted)
                m_Mobile.CurrentWayPoint = null;

            if (CheckHerding())
            {
                if (m_Mobile.GetHerder() != null)
                    m_Mobile.DebugSay(DebugFlags.AI, "Praise the shepherd!");
                else
                    m_Mobile.DebugSay(DebugFlags.AI, "I was command to {0}", m_Mobile.TargetLocation);
            }
            else if (CheckMove() && SomethingToInvestigate(ref sti))
            {
                if (DoMove(sti) == false)
                {
                    // something is blocking our move. Oh well.
                }
            }
            else if (m_Mobile.CurrentWayPoint != null)
            {
                WayPoint point = m_Mobile.CurrentWayPoint;
                if ((point.X != m_Mobile.Location.X || point.Y != m_Mobile.Location.Y) && point.Map == m_Mobile.Map && point.Parent == null && !point.Deleted)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I will move towards my waypoint.");
                    DoMove(m_Mobile.GetDirectionTo(m_Mobile.CurrentWayPoint));
                }
                else if (OnAtWayPoint())
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I will go to the next waypoint");
                    m_Mobile.CurrentWayPoint = point.NextPoint;
                    if (point.NextPoint != null && point.NextPoint.Deleted)
                        m_Mobile.CurrentWayPoint = point.NextPoint = point.NextPoint.NextPoint;
                }
            }
            else if (m_Mobile.IsAnimatedDead)
            {
                // animated dead follow their master
                Mobile master = m_Mobile.SummonMaster;

                if (master != null && master.Map == m_Mobile.Map && master.InRange(m_Mobile, m_Mobile.RangePerception))
                    MoveTo(master, false, 1);
                else
                    WalkRandomInHome(2, 2, 1);
            }
            else if (CheckMove() && ProactiveHoming() != Point3D.Zero)
            {
                m_Mobile.DebugSay(DebugFlags.Homing | DebugFlags.Echo, "ProactiveHoming: checking...");
                int range = 2;
                BaseCreature bc = m_Mobile as BaseCreature;
                bool canRun = bc.GetCreatureBool(CreatureBoolTable.CanRunAI);
                Point3D px = ProactiveHoming();
                if (bc.GetDistanceToSqrt(px) <= range + 1)
                {
                    m_Mobile.DebugSay(DebugFlags.Homing | DebugFlags.Echo, "ProactiveHoming: Arrived at goal.");
                    // just a temporal state
                    bc.SetCreatureBool(CreatureBoolTable.ProactiveHoming, false);
                }
                else
                {
                    m_Mobile.DebugSay(DebugFlags.Homing | DebugFlags.Echo, "ProactiveHoming: moving toward goal...");
                    MoveToPoint(px, canRun, range);
                }
            }
            else if (CheckMove())
            {   // the random chance is to reduce the frequency and to introduce the notion that the mobile didn't see you because they weren't paying attention
                if (Utility.RandomChance(25))                               // adam: townspeople now remember the players around them for a short while
                    LookAround();                                           //	to make things like calling guards more realistic

                if (!m_Mobile.CheckIdle())
                    WalkRandomInHome(iChanceToNotMove: 2, iChanceToDir: 2, iSteps: 1);
            }

            if (m_Mobile.Combatant != null && !m_Mobile.Combatant.Deleted && m_Mobile.Combatant.Alive && !m_Mobile.Combatant.IsDeadBondedPet)
            {

                m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
            }

            return true;
        }
        private Point3D ProactiveHoming()
        {
            Point3D preferred = Point3D.Zero;
            if (m_Mobile.Spawner is Spawner spawner)
                preferred = spawner.Location;
            else if (m_Mobile.Home != Point3D.Zero)
                preferred = m_Mobile.Home;

            if (preferred != Point3D.Zero && m_Mobile is BaseCreature bc)
                if (bc.GetCreatureBool(CreatureBoolTable.ProactiveHoming))
                    return preferred;

            return Point3D.Zero;
        }
        /* 
		 * build a list of players we would like to kill. We then see if we can Recall() them. You Recall someone if you have 
		 * fought them before. Players are stored in this type of memory for a short while, maybe 10 seconds.
		 */
        protected Mobile FindHiddenTarget()
        {
            Mobile mx;
            List<Mobile> mobiles = new List<Mobile>();

            for (int a = 0; a < m_Mobile.Aggressors.Count; ++a)
            {
                mx = (m_Mobile.Aggressors[a] as AggressorInfo).Attacker;
                if (mx != null && mx.Deleted == false && mx.Alive && !mx.IsDeadBondedPet && m_Mobile.CanBeHarmful(mx, false) && (m_Mobile.Aggressors[a] as AggressorInfo).Expired == false)
                    mobiles.Add(mx);
            }
            for (int a = 0; a < m_Mobile.Aggressed.Count; ++a)
            {
                mx = (m_Mobile.Aggressed[a] as AggressorInfo).Defender;
                if (mx != null && mx.Deleted == false && mx.Alive && !mx.IsDeadBondedPet && m_Mobile.CanBeHarmful(mx, false) && (m_Mobile.Aggressed[a] as AggressorInfo).Expired == false)
                    mobiles.Add(mx);
            }

            for (int ix = 0; ix < mobiles.Count; ix++)
            {
                // if we have a someone we fought before and they are hidden, go to combat mode and try to reveal.
                //	We only remember someone via Recall for a few seconds, so we will give up then
                if (ShortTermMemory.Recall(mobiles[ix]) && mobiles[ix].Hidden && m_Mobile.GetDistanceToSqrt(mobiles[ix]) <= m_Mobile.RangePerception)
                {   // refresh our memory of this guy lest we forget
                    // ShortTermMemory.Refresh(mobiles[ix]);
                    return mobiles[ix];
                }
            }
            return null;
        }

        // remember the mobiles around me so that if they are then hidden and release a spell while hidden, I can reasonably 
        //	call guards.
        // If a crime is committed around me, the guards may ask me if I've seen anyone and if I have, then they will go into action.
        public void LookAround()
        {   // Many towns people have a RangePerception of 2ish for talking, but we can SEE about a screen away (13 tiles)
            LookAround(13);
        }
        public void LookAround(int range)
        {
            List<Mobile> list = new List<Mobile>();
            IPooledEnumerable eable = m_Mobile.Map.GetMobilesInRange(m_Mobile.Location, range);
            foreach (Mobile m in eable)
                if (m is not null && !m.Deleted && m != m_Mobile)
                    list.Add(m);
            eable.Free();

            // tell the base mobile we saw something
            //  OnSee() is raw vs See() which filters on players, alive, hidden, etc.
            foreach (Mobile t in list)
                if (m_Mobile.CanSee(t) || (m_Mobile.CanSeeGhosts && !t.Alive))
                    m_Mobile.OnSee(t);
                else
                    ; // debug

            // do See() processing
            See(list);
        }
        public void See(List<Mobile> list)
        {
            // don't always 'see' players in the same order. This is especially important when we want to
            //  talk to the players around us.
            Utility.Shuffle(list);

            // okay, our list now contains nearby players that we can see
            // We think about it for a second, and decide to call guards on anyone that is deserving.
            //	this logic handles the case where a murderer 'just appears' in town without having moved - like if they
            //	they were hidden and someone revealed them.
            foreach (Mobile t in list)
            {
                if (t is PlayerMobile && t.Alive && !t.Blessed && m_Mobile.CanSee(t))
                {
                    m_LongTermMemory.Remember(t, 35);       // we remember for 35 seconds because you can hold a spell hidden for 30
                    if (m_Mobile.InLOS(t))
                        m_Mobile.DebugSay(DebugFlags.See, "I see {0}", t.Name);
                    else
                        m_Mobile.DebugSay(DebugFlags.See, "I am aware of {0}", t.Name);
                    if (m_Mobile.IsHumanInTown() && (t.Region is GuardedRegion) && t.Region.IsGuarded && (t.Region as GuardedRegion).IsGuardCandidate(t) && !(this is GuardAI))
                    {   // busted - call the guards
                        m_Mobile.DebugSay(DebugFlags.AI, "I will call guards on {0}", t.Name);
                        (t.Region as GuardedRegion).CheckGuardCandidate(t, t.Player);
                    }
                }
            }
        }

        public bool Remembers(object o)
        {
            return m_LongTermMemory.Recall(o) != null;
        }

        public virtual bool DoActionCombat(MobileInfo info)
        {
            if (info.target == null || info.gone || info.dead || info.hidden || info.fled)
                Action = ActionType.Wander;
            else
                m_Mobile.Direction = m_Mobile.GetDirectionTo(info.target);

            return true;
        }

        public MobileInfo GetMobileInfo(MobileInfo info)
        {
            return Utility.GetMobileInfo(m_Mobile, m_Path, info);
        }

        public bool LostSightOfCombatant()
        {
            MobileInfo info = GetMobileInfo(new MobileInfo(m_Mobile.Combatant));

            if (info.available)
            {
                if (info.dead)
                    m_Mobile.DebugSay(DebugFlags.AI, "That's cool. I'm glad he's dead.");
                else if (info.fled)
                    m_Mobile.DebugSay(DebugFlags.AI, "My combatant has fled, so I am on guard");
                else if (info.gone)
                    m_Mobile.DebugSay(DebugFlags.AI, "my combatant is gone");
                else
                    m_Mobile.DebugSay(DebugFlags.AI, "I've lost sight of my combatant");
                return true;
            }

            return false;
        }
        public bool HandleLostSightOfCombatant()
        {
            if (GetFocusMob() && m_Mobile.Combatant != null)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am now going to fight {0}", MobileName(m_Mobile.Combatant));
                return true;
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "nothing is around. I am on guard.");
                m_Mobile.Combatant = m_Mobile.FocusMob = null;
                Action = ActionType.Guard;
                return true;
            }
        }
        public bool GetFocusMob()
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "I am going to attack {0}", MobileName(m_Mobile.FocusMob));

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
                return true;
            }

            return false;
        }

        public virtual bool DoActionGuard()
        {
            if (DateTime.UtcNow < m_NextStopGuard)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am on guard");
                m_Mobile.Turn(Utility.Random(0, 2) - 1);
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I stop be in Guard");
                Action = ActionType.Wander;
            }

            return true;
        }

        public virtual bool DoActionHunt()
        {
            if (DateTime.UtcNow < m_NextStopHunt)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am Hunting");
                WalkRandomInHome(2, 2, 1);
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I have Stopped Hunting");
                Action = ActionType.Wander;
            }

            return true;
        }
        public virtual bool DoActionNavStar()
        {
            if (m_Mobile is BaseCreature bc)
            {
                bool haveDest = !string.IsNullOrEmpty(bc.NavDestination);
                bool havePoint = bc.NavPoint != Point3D.Zero;
                try
                {
                    if (AcquireFocusMob(bc.RangePerception, bc.FightMode, false, false, true))
                    {   // top priority
                        bc.DebugSay(DebugFlags.AI, "I am going to attack {0}", bc.FocusMob.Name);
                        bc.Combatant = bc.FocusMob;
                        Action = ActionType.Combat;
                    }

                    else if (!haveDest && !havePoint)
                    {   // how did we get here?
                        Action = ActionType.Wander;
                    }

                    else if (!NavStar.AnyBeacons(bc, bc.NavDestination))
                    {   // mobile may have a NavDestination, but there are no beacons
                        Action = ActionType.Wander;
                    }

                    else if (haveDest && !havePoint)
                    {   // just getting started

                        bc.DebugSay(DebugFlags.NavStar, "NavStar: Initial beacon request");
                        NavStar.CreateRequest(bc, bc.NavDestination);   // initialize stack of destinations
                        NavBeacon current = NavStar.ProcessRequest(bc); // grab the first destination

                        // 3/28/2024, Adam: This SameIsland check fails frequently, even when the two points are clearly valid, maybe 10 tiles away.
                        //  Internally, when getting the next WayPoint within the SectorPathAlgorithm, SameIsland is not failing, or very rarely.
                        //  My hunch is that we are using SameIsland incorrectly. I.e., it doesn't mean what we think it means. Inconceivable!
                        if (current == null /*|| !SectorPathAlgorithm.SameIsland(bc.Location, bc.NavPoint) */)
                        {
                            if (current == null)
                                bc.DebugSay(DebugFlags.NavStar, "NavStar: Initial path check failed, no beacon, aborting");
                            else
                                bc.DebugSay(DebugFlags.NavStar, "NavStar: Initial path check failed, no path found to destination, aborting");

                            bc.NavDestination = null;
                            bc.NavPoint = Point3D.Zero;
                            bc.Beacon = null;

                            NavStar.RemoveRequest(bc);    // probably isn't one
                            Action = ActionType.Wander;
                        }
                        else
                        {
                            bc.DebugSay(DebugFlags.NavStar, "NavStar: Initial beacon {0}:Ring{1}",
                                bc.Beacon.Serial.ToString(),
                                bc.Beacon.Ring);
                        }

                    }

                    else if (haveDest && havePoint)
                    {   // we're walking baby!

                        if (CheckNavStuck())
                        {
                            NavStar.UnStuck(bc);
                            NavPath = null;
                        }

                        MoveToNavPoint(bc.NavPoint, this.CanRunAI);

                        bool there = false;
                        if (NavPath != null)
                        {
                            Point3D goal = NavPath.GetEndGoalLocation(0);
                            if (NavPath.Check(bc.Location, goal, range: 2))
                                there = true;
                        }

                        if (/*there ||*/ bc.InRange(bc.NavPoint, 6))
                        {   // arrived at the beacon we were searching for

                            bc.DebugSay(DebugFlags.NavStar, "NavStar: Arrived at beacon {0}:Ring{1}",
                                bc.Beacon.Serial.ToString(),
                                bc.Beacon.Ring);

                            NavBeacon previous = bc.Beacon;                 // for debug reporting purposes
                            NavBeacon current = NavStar.ProcessRequest(bc); // where we go next

                            if (current != null)
                            {   // okay, got a new beacon, keep walking

                                bc.DebugSay(DebugFlags.NavStar, "NavStar: Got new Beacon {0}:Ring{1}",
                                    bc.Beacon.Serial.ToString(),
                                    bc.Beacon.Ring);

                                // generate a new path
                                NavPath = null;
                            }
                            else
                            {   // no more beacons (our stack is empty.)

                                bc.DebugSay(DebugFlags.NavStar, "NavStar: Arrived at Destination {0}:Ring{1}",
                                    previous.Serial.ToString(),
                                    previous.Ring);

                                // cleanup: clear our notion of a destination, Remove the navigation request record,
                                //  wander, kill the NavPath, and make this our new home.
                                bc.NavDestination = null;
                                bc.NavPoint = Point3D.Zero;
                                bc.Beacon = null;

                                NavStar.RemoveRequest(bc);
                                Action = ActionType.Wander;
                                bc.Home = Location;
                                NavPath = null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
            }
            return true;
        }

        public class NavMemory
        {
            public byte stuck_counter = 0;
            public Point3D last_location = Point3D.Zero;
            public NavMemory(byte counter, Point3D location)
            {
                stuck_counter = counter;
                last_location = location;
            }
        }
        private byte stuck_counter = 0;                    // delete
        private Point3D last_location = Point3D.Zero;      // delete
        private static Memory StuckMemory = new();
        private bool CheckNavStuck()
        {
            // do we remember this pet?
            Memory.ObjectMemory om = StuckMemory.Recall(m_Mobile as object);
            if (om != null)
            {   // yes, we do remember him
                NavMemory nm = om.Context as NavMemory;

                if (nm.stuck_counter == 0)
                    nm.last_location = m_Mobile.Location;

                if (m_Mobile.InRange(nm.last_location, 3))
                    nm.stuck_counter++;
                else
                    nm.stuck_counter = 0;

                if (nm.stuck_counter > 110)
                {
                    nm.last_location = Point3D.Zero;
                    nm.stuck_counter = 0;
                    return true;
                }
            }
            else
            {   // don't remember him. Remember him now
                StuckMemory.Remember(m_Mobile, new NavMemory(counter: 1, m_Mobile.Location), seconds: 60);
            }
            return false;
        }
        public virtual bool DoActionFlee()
        {
            Mobile from = m_Mobile.FocusMob;

            if (from == null || from.Deleted || from.Map != m_Mobile.Map)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I have lost 'em");
                Action = ActionType.Guard;
                return true;
            }

            if (WalkMobileRange(from, 1, true, m_Mobile.RangePerception * 2, m_Mobile.RangePerception * 3))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I have fled");
                Action = ActionType.Guard;
                return true;
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am fleeing!");
            }

            return true;
        }

        public virtual bool DoActionInteract()
        {
            return true;
        }

        public virtual bool DoActionBackoff()
        {
            return true;
        }

        // 4/18/23, Adam: Add a common and reasonable implementation for chase
        public virtual bool DoActionChase(MobileInfo info)
        {
            try
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am chasing");
                Mobile combatant = info.target;
                if (combatant == null || info.gone || info.dead || info.hidden || info.fled)
                {
                    // Combatant is null, we go into guard mode
                    Action = ActionType.Guard;
                }
                else
                {
                    // Make sure they're still out of range and reset to combat mode if they're not
                    if (m_Mobile.InRange(combatant, 6))
                    {
                        // Reset to combat for now
                        Action = ActionType.Combat;
                        return true;
                    }

                    // Run to them! (uses pathing)
                    if (!MoveTo(combatant, CanRunAI, m_Mobile.RangeFight))
                        OnFailedMove();
                }
            }
            catch (Exception e)
            {
                // Log an exception
                EventSink.InvokeLogException(new LogExceptionEventArgs(e));
            }

            return true;
        }
        private bool CheckSpeedup()
        {
            if (m_Mobile.Deleted || m_Mobile.ControlMaster == null || m_Mobile.ControlMaster.Deleted)
                return false;

            switch (m_Mobile.ControlOrder)
            {
                case OrderType.Come:
                case OrderType.Guard:
                case OrderType.Follow:
                    // speed back up
                    return true;
            }
            return false;
        }
        public void OnMountChanged()
        {
            if (CheckSpeedup())
                m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Engage);
        }
        #region Current Speed Manager
        private bool SpeedKnown = false;    // do we have a table entry for this creature?
        private bool SpeedChecked = false;  // have we already checked this creature?
        double DefaultActiveSpeed = 0;      // this creatures normal active speed (not serialized)
        double DefaultPassiveSpeed = 0;     // this creatures normal passive speed (not serialized)
        public enum Clutch
        {
            Engage,
            Disengage
        }
        private double CurrentSpeedAdjust(Clutch gear)
        {
            double newSpeed = m_Mobile.ActiveSpeed;
            if (SpeedChecked == false && SpeedKnown == false)
            {
                SpeedKnown = SpeedInfo.GetSpeeds(m_Mobile, ref DefaultActiveSpeed, ref DefaultPassiveSpeed);
                SpeedChecked = true;
                if (SpeedKnown == false)
                    m_Mobile.DebugSay(DebugFlags.Speed, string.Format("No speed info for {0}.", m_Mobile.GetType().Name));
            }

            // don't set speedy if we don't know how to reset it
            if (SpeedKnown == true)
                if (gear == Clutch.Engage)
                {
                    // use lightning speed
                    double activeSpeedOverride = m_Mobile.Body.IsHuman && m_Mobile.Mounted ? CoreAI.ActiveMountedSpeedOverride : CoreAI.ActiveSpeedOverride;
                    if (m_Mobile.CurrentSpeed != activeSpeedOverride)
                        m_Mobile.DebugSay(DebugFlags.Speed, string.Format("Switching to lightning{1}speed: {0:#,0.000}", activeSpeedOverride, m_Mobile.Mounted ? " (mounted) " : " "));
                    newSpeed = activeSpeedOverride;
                }
                // the creature may be in one of the EnguageModes, but has entered combat, in which case we resume normal speed.
                else
                {   // normal speed
                    if (m_Mobile.CurrentSpeed != DefaultActiveSpeed)
                        m_Mobile.DebugSay(DebugFlags.Speed, string.Format("Switching to normal speed: {0:#,0.000}", DefaultActiveSpeed));
                    newSpeed = DefaultActiveSpeed;
                }

            return newSpeed;
        }
        #endregion Current Speed Manager
        public virtual bool Obey()
        {
            if (m_Mobile.Deleted)
                return false;

            if (m_Mobile.DoActionOverride(true))
                return true;

            switch (m_Mobile.ControlOrder)
            {
                case OrderType.None:
                    return DoOrderNone();

                case OrderType.Come:
                    return DoOrderCome();

                case OrderType.Drop:
                    return DoOrderDrop();

                case OrderType.Friend:
                    return DoOrderFriend();

                case OrderType.Guard:
                    return DoOrderGuard();

                case OrderType.Attack:
                    return DoOrderAttack();

                case OrderType.Patrol:
                    return DoOrderPatrol();

                case OrderType.Release:
                    return DoOrderRelease();

                case OrderType.Stay:
                    return DoOrderStay();

                case OrderType.Stop:
                    return DoOrderStop();

                case OrderType.Follow:
                    return DoOrderFollow();

                case OrderType.Transfer:
                    return DoOrderTransfer();

                default:
                    return false;
            }
        }

        public virtual void OnCurrentOrderChanged()
        {
            if (m_Mobile.Deleted || m_Mobile.ControlMaster == null || m_Mobile.ControlMaster.Deleted)
                return;

            m_Mobile.Enticer = null;

            switch (m_Mobile.ControlOrder)
            {
                case OrderType.None:
                    if (m_Mobile is not ITownshipNPC)
                        m_Mobile.Home = m_Mobile.Location;
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Come:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Engage);
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Guard:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Engage);
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = true;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Follow:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Engage);
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Drop:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = true;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Friend:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Attack:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Disengage);
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = true;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Patrol:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = CurrentSpeedAdjust(Clutch.Disengage);
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Release:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Stay:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Stop:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;

                case OrderType.Transfer:
                    KissAndMakup(m_Mobile.ControlMaster);
                    m_Mobile.ControlMaster.RevealingAction();
                    m_Mobile.CurrentSpeed = m_Mobile.PassiveSpeed;
                    m_Mobile.PlaySound(m_Mobile.GetIdleSound());
                    m_Mobile.Warmode = false;
                    m_Mobile.Combatant = null;
                    m_Mobile.PreferredFocus = null;
                    break;
            }
        }
        #region Order Handlers
        public virtual bool DoOrderNone()
        {
            if (CheckHerding())
            {
                if (m_Mobile.GetHerder() != null)
                    m_Mobile.DebugSay(DebugFlags.AI, "Praise the shepherd!");
                else
                    m_Mobile.DebugSay(DebugFlags.AI, "I was command to {0}", m_Mobile.TargetLocation);
                return true;
            }

            m_Mobile.DebugSay(DebugFlags.AI, "I have no order");
            WalkRandomInHome(3, 2, 1);

            if (m_Mobile.Combatant != null && !m_Mobile.Combatant.Deleted && m_Mobile.Combatant.Alive && !m_Mobile.Combatant.IsDeadBondedPet)
            {
                m_Mobile.Warmode = true;
                m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
            }
            else
            {
                m_Mobile.Warmode = false;
            }

            return true;
        }
        public virtual bool DoOrderCome()
        {
            if (CheckHerding())
            {
                if (m_Mobile.GetHerder() != null)
                    m_Mobile.DebugSay(DebugFlags.AI, "Praise the shepherd!");
                else
                    m_Mobile.DebugSay(DebugFlags.AI, "I was command to {0}", m_Mobile.TargetLocation);
                return true;
            }

            if (m_Mobile.ControlMaster != null && !m_Mobile.ControlMaster.Deleted)
            {
                if (!ShouldFollow(m_Mobile.ControlMaster))
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I have lost my master. I stay here");
                    m_Mobile.ControlTarget = null;
                    m_Mobile.ControlOrder = OrderType.None;
                }
                else
                {
                    // Not exactly OSI style, but better than nothing.
                    //bool bRun = (iCurrDist > 5);
                    bool bRun = ShouldRun(m_Mobile, m_Mobile.ControlMaster, 5); // once initiated, pet will keep running until the master is reached.

                    m_Mobile.DebugSay(DebugFlags.AI, "My master told me to come. I will {0}.", bRun ? "run" : "walk");

                    if (WalkMobileRange(m_Mobile.ControlMaster, 1, bRun, 0, 1))
                    {
                        if (m_Mobile.Combatant != null && !m_Mobile.Combatant.Deleted && m_Mobile.Combatant.Alive && !m_Mobile.Combatant.IsDeadBondedPet)
                        {
                            m_Mobile.Warmode = true;
                            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
                        }
                        else
                        {
                            m_Mobile.Warmode = false;
                        }
                    }
                }
            }

            return true;
        }
        public virtual bool DoOrderFollow()
        {
            Mobile control_master = m_Mobile.ControlMaster;
            MobileInfo control_master_info = GetMobileInfo(new MobileInfo(control_master));

            if (CheckHerding())
            {
                if (m_Mobile.GetHerder() != null)
                    m_Mobile.DebugSay(DebugFlags.AI, "Praise the shepherd!");
                else
                    m_Mobile.DebugSay(DebugFlags.AI, "I was command to {0}", m_Mobile.TargetLocation);
            }
            else if (m_Mobile.ControlTarget != null && !m_Mobile.ControlTarget.Deleted && m_Mobile.ControlTarget != m_Mobile)
            {
                if (!ShouldFollow())
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I have lost the one a follow. I stay here");
                    if (m_Mobile.Combatant != null && !m_Mobile.Combatant.Deleted && m_Mobile.Combatant.Alive && !m_Mobile.Combatant.IsDeadBondedPet)
                    {
                        m_Mobile.Warmode = true;
                        m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
                    }
                    else
                    {
                        m_Mobile.Warmode = false;
                    }
                }
                else
                {
                    // Not exactly OSI style, but better than nothing.
                    //bool bRun = (iCurrDist > 5);
                    bool bRun = ShouldRun(m_Mobile, m_Mobile.ControlTarget, 5); // once initiated, pet will keep running until the master is reached.

                    m_Mobile.DebugSay(DebugFlags.AI, "My master told me to follow: {0}. I will {1}.", m_Mobile.ControlTarget.Name, bRun ? "run" : "walk");

                    if (WalkMobileRange(m_Mobile.ControlTarget, 1, bRun, 0, 1))
                    {
                        if (m_Mobile.Combatant != null && !m_Mobile.Combatant.Deleted && m_Mobile.Combatant.Alive && !m_Mobile.Combatant.IsDeadBondedPet)
                        {
                            m_Mobile.Warmode = true;
                            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.Combatant);
                        }
                        else
                        {
                            m_Mobile.Warmode = false;
                        }
                    }
                    else
                        ;// debug
                }
            }

            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I have nobody to follow, lets relax");
                m_Mobile.ControlTarget = null;
                m_Mobile.ControlOrder = OrderType.None;
            }

            return true;
        }
        public virtual bool DoOrderGuard()
        {
            BaseCreature me = m_Mobile;
            BaseCreature my = m_Mobile;

            if (me.IsDeadPet)
                return true;

            if (CheckHerding())
            {
                if (me.GetHerder() != null)
                    me.DebugSay(DebugFlags.AI, "Praise the shepherd!");
                else
                    me.DebugSay(DebugFlags.AI, "I was command to {0}", m_Mobile.TargetLocation);
                return true;
            }

            Mobile control_master = my.ControlMaster;
            MobileInfo control_master_info = GetMobileInfo(new MobileInfo(control_master));

            if (control_master == null || control_master.Deleted)
                return true;

            // selfInflicted is the case where the master damages himself, like with a spell cast upon himself
            bool selfInflicted = m_Mobile.ControlMaster == m_Mobile.ControlMaster.Combatant;
            Mobile combatant = control_master_info.available && !selfInflicted ? m_Mobile.ControlMaster.Combatant : my.Combatant;

            // find out who is/was attacking our master
            if (control_master_info.available)
            {   // if there is no HIGHER PriorityCombatant, we return null, and you stick with combatant
                Mobile higher_priority_combatant = GetPriorityCombatant(control_master);
                // 9/20/2023, Adam: turret bug fix.
                //  We check to see if our combatant is an Aggressor to our master 
                //  If our master simply double-clicked someone in war mode, this is not enough.
                combatant = higher_priority_combatant != null ? higher_priority_combatant : control_master.IsAggressor(combatant) ? combatant : null;
                if (combatant != null)
                    me.DebugSay(DebugFlags.AI, "Crap, my master has been attacked! I will attack one of those bastards!");
                else if ((combatant = GetPriorityAggressor(me)) != null)
                {   // My master is standing here, but not being attacked, so defend myself!
                    // only fight stuff right on top of us (should we run after stuff? I think not)
                    combatant = my.InRange(combatant.Location, (my as BaseCreature).RangeFight) ? combatant : null;
                    higher_priority_combatant = GetPriorityCombatant(me);
                    combatant = higher_priority_combatant != null ? higher_priority_combatant : me.IsAggressor(combatant) ? combatant : null;
                    if (combatant != null)
                        me.DebugSay(DebugFlags.AI, "My master is standing here, but not being attacked, so defend myself!");
                    else
                        ; // maybe dead
                }
            }

            // if I am in guard mode, yet my master is nowhere to be found, protect myself
            if (control_master_info.unavailable)
            {
                Mobile higher_priority_combatant = GetPriorityCombatant(me);
                combatant = higher_priority_combatant != null ? higher_priority_combatant : combatant;
                if (combatant != null)
                    me.DebugSay(DebugFlags.AI, "I'm in guard mode, being attacked, but my master isn't around, so defend myself!");
            }

            // note for mages, we don't check hidden as some mages can reveal
            MobileInfo combatant_info = GetMobileInfo(new MobileInfo(combatant));
            bool standard_rule = combatant_info.target != me && combatant_info.target != control_master;
            bool mage_rule = MageRule(me, combatant_info);
            bool muggle_rule = MuggleRule(me, combatant_info);
            bool composit_rule = standard_rule && this is MageAI ? mage_rule : muggle_rule;
            if (composit_rule)
            {
                me.DebugSay(DebugFlags.AI, "Die! Die! Die!");

                my.Combatant = combatant;
                my.FocusMob = combatant;

                DoActionCombat(GetMobileInfo(new MobileInfo(combatant)));

                //PIX: We need to call Think() here or spellcasting mobs in guard
                // mode will never target their spells.
                Think();
            }
            else if (ShouldFollow())
            {
                m_Mobile.Warmode = false;

                // Not exactly OSI style, but better than nothing.
                //int iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.ControlMaster);
                //bool bRun = (iCurrDist > 5);
                bool bRun = ShouldRun(me, control_master, 5); // once initiated, pet will keep running until the master is reached.

                if (bRun)
                    m_Mobile.DebugSay(DebugFlags.AI, "My master told me to guard him, gotta run!");
                else
                    m_Mobile.DebugSay(DebugFlags.AI, "My master told me to guard him, but from what? Nobody knows! Sometimes I wonder..");

                WalkMobileRange(control_master, 1, bRun, 0, 1);
            }

            return true;
        }
        public virtual bool DoOrderAttack()
        {
            if (m_Mobile.IsDeadPet)
                return true;
            try
            {
                // adam: 3/13/10 - fix gate-pet-to-give-counts bug by making the LOS check.
                //	we already check hidden, why not check LOS?!!
                //	anywho, I put the check in the CoreCommandConsole. PetNeedsLOS. (default is NO CHECK)
                if (m_Mobile.ControlTarget == null || m_Mobile.ControlTarget.Deleted ||
                    m_Mobile.ControlTarget.Map != m_Mobile.Map || !m_Mobile.ControlTarget.Alive ||
                    m_Mobile.ControlTarget.IsDeadBondedPet || !m_Mobile.CanSee(m_Mobile.ControlTarget) ||
                    (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.PetNeedsLOS) && m_Mobile.Map.LineOfSight(m_Mobile, m_Mobile.ControlTarget) == false))
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I think he might be dead. He's not anywhere around here at least. That's cool. I'm glad he's dead.");

                    m_Mobile.ControlTarget = null;
                    m_Mobile.ControlOrder = OrderType.None;

                    #region Attack next Aggressor
                    if ((m_Mobile.FightMode & FightMode.All) > 0 || (m_Mobile.FightMode & FightMode.Aggressor) > 0)
                    {
                        Mobile newCombatant = null;
                        double newScore = 0.0;

                        List<AggressorInfo> list = m_Mobile.Aggressors;

                        for (int i = 0; i < list.Count; ++i)
                        {
                            Mobile aggr = ((AggressorInfo)list[i]).Attacker;

                            if (aggr.Map != m_Mobile.Map || !aggr.InRange(m_Mobile.Location, m_Mobile.RangePerception) || !m_Mobile.CanSee(aggr))
                                continue;

                            if (aggr.IsDeadBondedPet || !aggr.Alive)
                                continue;

                            double aggrScore = m_Mobile.GetValueFrom(aggr, FightMode.Closest, false);

                            if ((newCombatant == null || aggrScore > newScore) && m_Mobile.InLOS(aggr))
                            {
                                newCombatant = aggr;
                                newScore = aggrScore;
                            }
                        }

                        if (newCombatant != null)
                        {
                            m_Mobile.ControlTarget = newCombatant;
                            m_Mobile.ControlOrder = OrderType.Attack;
                            m_Mobile.Combatant = newCombatant;
                            m_Mobile.DebugSay(DebugFlags.AI, "But -that- is not dead. Here we go again...");
                            Think();
                        }
                    }
                    #endregion Attack next Aggressor
                }
                else
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I fight 'em!");
                    m_Mobile.Combatant = m_Mobile.ControlTarget;
                    Action = ActionType.Combat;
                    Think();
                }

                if (m_Mobile.ControlOrder == OrderType.None && m_Mobile.ControlMaster != null)
                {   // nothing going on, I'll wander back to my master
                    Point2D save = m_Mobile.TargetLocation;
                    m_Mobile.TargetLocation = new Point2D(m_Mobile.ControlMaster.Location.X, m_Mobile.ControlMaster.Location.Y);
                    if (CanGetThere(map: m_Mobile.Map) == false)
                        m_Mobile.TargetLocation = save;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            return true;
        }
        public virtual bool DoOrderDrop()
        {
            if (m_Mobile.IsDeadPet || m_Mobile.Summoned)
                return true;

            m_Mobile.DebugSay(DebugFlags.AI, "I drop my stuff for my master");

            Container pack = m_Mobile.Backpack;

            if (pack != null)
            {
                List<Item> list = pack.Items;

                for (int i = list.Count - 1; i >= 0; --i)
                    if (i < list.Count)
                        ((Item)list[i]).MoveToWorld(m_Mobile.Location, m_Mobile.Map);
            }

            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.None;

            return true;
        }
        public virtual bool DoOrderFriend()
        {
            m_Mobile.DebugSay(DebugFlags.AI, "This order is not yet coded");

            return true;
        }
        public virtual bool DoOrderPatrol()
        {
            m_Mobile.DebugSay(DebugFlags.AI, "This order is not yet coded");
            return true;
        }
        public virtual bool DoOrderRelease()
        {
            /*
			if(m_Mobile.IsBonded && !m_Mobile.IsDeadBondedPet )
			{
				m_Mobile.ControlMaster.SendGump(new ReleaseBondedGump(m_Mobile));
				return false;
			}
			*/

            m_Mobile.DebugSay(DebugFlags.AI, "My master release me");

            m_Mobile.PlaySound(m_Mobile.GetAngerSound());

            m_Mobile.SetControlMaster(null);
            m_Mobile.SummonMaster = null;

            m_Mobile.IsBonded = false;
            m_Mobile.BondingBegin = DateTime.MinValue;
            m_Mobile.OwnerAbandonTime = DateTime.MinValue;

            // remove the pet from the global pet cache
            if (Mobile.PetCache.Contains(m_Mobile))
                Mobile.PetCache.Remove(m_Mobile);

            m_Mobile.ControlMasterGUID = 0;

            if (m_Mobile.DeleteOnRelease || m_Mobile.IsDeadPet)
                m_Mobile.Delete();

            return true;
        }
        public virtual bool DoOrderStay()
        {
            if (CheckHerding())
            {
                if (m_Mobile.GetHerder() != null)
                    m_Mobile.DebugSay(DebugFlags.AI, "Praise the shepherd!");
                else if (m_Mobile.TargetLocation != Point2D.Zero)
                    m_Mobile.DebugSay(DebugFlags.AI, "I was command to {0}", m_Mobile.TargetLocation);
            }
            else
                m_Mobile.DebugSay(DebugFlags.AI, "My master told me to stay");

            //m_Mobile.Direction = m_Mobile.GetDirectionTo( m_Mobile.ControlMaster );

            return true;
        }
        public virtual bool DoOrderStop()
        {
            if (m_Mobile.ControlMaster == null || m_Mobile.ControlMaster.Deleted)
                return true;

            m_Mobile.DebugSay(DebugFlags.AI, "My master told me to stop.");

            m_Mobile.Direction = m_Mobile.GetDirectionTo(m_Mobile.ControlMaster);

            if (m_Mobile is not ITownshipNPC)
                m_Mobile.Home = m_Mobile.Location;

            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.None;

            return true;
        }
        public virtual bool DoOrderTransfer()
        {
            if (m_Mobile.IsDeadPet)
                return true;

            Mobile from = m_Mobile.ControlMaster;
            Mobile to = m_Mobile.ControlTarget;

            if (from != to && from != null && !from.Deleted && to != null && !to.Deleted && to.Player)
            {

                m_Mobile.DebugSay(DebugFlags.AI, "Begin transfer with {0}", to.Name);

                if (!m_Mobile.CanBeControlledBy(to))
                {
                    string args = String.Format("{0}\t{1}\t ", to.Name, from.Name);

                    from.SendLocalizedMessage(1043248, args); // The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                    to.SendLocalizedMessage(1043249, args); // The pet will not accept you as a master because it does not trust you.~3_BLANK~
                }
                else if (!m_Mobile.CanBeControlledBy(from))
                {
                    string args = String.Format("{0}\t{1}\t ", to.Name, from.Name);

                    from.SendLocalizedMessage(1043250, args); // The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                    to.SendLocalizedMessage(1043251, args); // The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                }
                else
                {
                    NetState fromState = from.NetState, toState = to.NetState;

                    if (fromState != null && toState != null)
                    {
                        if (from.HasTrade)
                        {
                            from.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                        }
                        else if (to.HasTrade)
                        {
                            to.SendLocalizedMessage(1010507); // You cannot transfer a pet with a trade pending
                        }
                        else
                        {
                            Container c = fromState.AddTrade(toState);
                            c.DropItem(new TransferItem(m_Mobile));
                        }
                    }
                }
            }

            m_Mobile.ControlTarget = null;
            m_Mobile.ControlOrder = OrderType.Stay;

            return true;
        }
        #endregion Order Handlers

        #region Tools
        public static bool ShouldAggress(Mobile m)
        {
            if (m is BaseCreature bc)
            {
                if (!bc.Controlled || bc.Controlled && (bc.ControlOrder == OrderType.Attack || bc.ControlOrder == OrderType.Guard))
                    return true;
            }
            return false;
        }
        private bool ShouldFollow()
        {
            if (m_Mobile != null && m_Mobile.ControlTarget != null)
                return ShouldFollow(m_Mobile.ControlTarget);

            return false;
        }
        private bool ShouldFollow(Mobile m)
        {
            if (m_Mobile != null && m != null)
            {
                int mult = m == m_Mobile.ControlMaster ? 2 : 1;
                int iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m);
                return (iCurrDist <= m_Mobile.RangePerception * mult);
            }

            return false;
        }
        private bool CanGetThere(Map map)
        {
            int x = m_Mobile.TargetLocation.X;
            int y = m_Mobile.TargetLocation.Y;
            int z = m_Mobile.Z;
            #region Allow herding up z, down z (currently unsupported)
#if false
            object surface = m_Mobile.Map.GetTopSurface(new Point3D(x, y, m_Mobile.Z + 25));
            if (surface is LandTile)
                z = m_Mobile.Map.GetAverageZ(x, y);
            else if (surface is StaticTile)
                z = ((StaticTile)surface).Z;
            else if (surface is Item)
                z = ((Item)surface).Z;
#endif
            #endregion Allow herding up z, down z (currently unsupported)

            return Spawner.ClearPathLand(m_Mobile.Location, new Point3D(x, y, z), map: map);
        }
        public virtual bool CheckHerding()
        {
            Point2D target = m_Mobile.TargetLocation;

            if (target == Point2D.Zero)
                return false; // Creature is not being herded

            double distance = m_Mobile.GetDistanceToSqrt(target);

            if (distance >= 2 && !CanGetThere(map: m_Mobile.Map))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I was command to {0}, but can't get there", m_Mobile.TargetLocation);
                m_Mobile.TargetLocation = Point2D.Zero;
                return false; // can't get there
            }

            if (distance < 1 || distance > 20)
            {
                #region DarkTidesQuest
                if (distance < 1 && target.X == 1076 && target.Y == 450 && (m_Mobile is HordeMinionFamiliar))
                {
                    PlayerMobile pm = m_Mobile.ControlMaster as PlayerMobile;

                    if (pm != null)
                    {
                        QuestSystem qs = pm.Quest;

                        if (qs is DarkTidesQuest)
                        {
                            QuestObjective obj = qs.FindObjective(typeof(FetchAbraxusScrollObjective));

                            if (obj != null && !obj.Completed)
                            {
                                m_Mobile.AddToBackpack(new ScrollOfAbraxus());
                                obj.Complete();
                            }
                        }
                    }
                }
                #endregion DarkTidesQuest

                if (distance < 1)
                    m_Mobile.DebugSay(DebugFlags.AI, "TargetLocation: I am there");
                else if (distance > 20)
                    m_Mobile.DebugSay(DebugFlags.AI, "TargetLocation: too far");

                m_Mobile.TargetLocation = Point2D.Zero;
                return false; // At the target or too far away
            }

            DoMove(m_Mobile.GetDirectionTo(target));

            #region TargetLocationExpire
            // see if it's time to give up on this target location
            if (m_Mobile.TargetLocationExpire == 0)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "TargetLocation timeout initialized");
                m_Mobile.TargetLocationExpire = Core.TickCount + (60000);   // 60 seconds?
            }
            else if (Core.TickCount > m_Mobile.TargetLocationExpire)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "TargetLocation timeout expired");
                m_Mobile.TargetLocationExpire = 0;
                m_Mobile.TargetLocation = Point2D.Zero;
            }

            #endregion TargetLocationExpire

            return true;
        }
        #region Should Run
        public class RunContext
        {
            public Mobile FollowTarget;
            public Mobile Pet;
            public OrderType PetOrder;
            public Mobile PetCombatant;
            public RunContext(Mobile followTarget, Mobile pet)
            {
                FollowTarget = followTarget;
                Pet = pet;
                PetOrder = (pet as BaseCreature).ControlOrder;
                PetCombatant = (pet as BaseCreature).Combatant;
            }
        }
        private Memory RunMemory = new Memory();
        private bool ShouldRun(Mobile pet, Mobile master, int startRun)
        {
            BaseCreature bcPet = pet as BaseCreature;
            if (bcPet == null || bcPet.Deleted)
                return false;

            int iCurrDist = (int)pet.GetDistanceToSqrt(master);
            bool bRun = (iCurrDist > startRun);

            // do we remember this pet?
            Memory.ObjectMemory om = RunMemory.Recall(pet as object);
            if (om == null)
            {   // don't remember him
                if (iCurrDist < 2 || !bRun)
                    return false;   // we're there

                RunMemory.Remember(pet, new RunContext(master, pet), seconds: 30);

                return true;
            }
            else
            {   // do remember him
                RunContext rc = om.Context as RunContext;
                if (iCurrDist < 2)
                {   // we're there
                    RunMemory.Forget(pet);
                    return false;
                }
                else if ((bcPet.ControlMaster != rc.FollowTarget && bcPet.ControlTarget != rc.FollowTarget) || bcPet.ControlOrder != rc.PetOrder || bcPet.Combatant != rc.PetCombatant)
                {   // something has changed
                    RunMemory.Forget(pet);
                    return false;
                }
                // keep flying
                return true;
            }
            return false;
        }
        public bool CanRun()
        {
            if (m_Mobile == null) return false;
            return
                // old model.
                //  The problem with the old model is that an AI is shared by, for instance, some creatures that can run, and others that cannot.
                //  We don't want a separate AI for every combination
                CanRunAI ||
                // new model
                m_Mobile.MyCharacteristics.HasFlag(BaseCreature.Characteristics.Run) || m_Mobile.MyCharacteristics.HasFlag(BaseCreature.Characteristics.Fly);
        }
        #endregion Should Run
        private bool MageRule(BaseCreature bc, MobileInfo combatant_info)
        {
            if (bc.CanReveal)
                // it's ok if they are hidden
                return !(combatant_info.target == null || combatant_info.gone || combatant_info.dead || combatant_info.fled);
            else
                // only if they are visible
                return !combatant_info.hidden && !(combatant_info.target == null || combatant_info.gone || combatant_info.dead || combatant_info.fled);
        }
        private bool MuggleRule(BaseCreature bc, MobileInfo combatant_info)
        {
            return !(combatant_info.target == null || combatant_info.gone || combatant_info.dead || combatant_info.hidden || combatant_info.fled);
        }
        #endregion Tools

        private class BondedPetDeathTimer : Timer
        {
            private Mobile owner;


            public BondedPetDeathTimer(Mobile target)
                : base(TimeSpan.FromSeconds(1.0))
            {
                owner = target;
                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                if (owner != null)
                {
                    owner.BoltEffect(0);
                    owner.Kill();
                    owner.BoltEffect(0);

                }
            }
        }
        public void ReleaseBondedPet()
        {
            if (m_Mobile != null)
            {
                m_Mobile.Frozen = true;
                m_Mobile.DebugSay(DebugFlags.AI, "My master release me");

                m_Mobile.PlaySound(41);

                m_Mobile.SetControlMaster(null);
                m_Mobile.SummonMaster = null;

                m_Mobile.IsBonded = false;
                m_Mobile.BondingBegin = DateTime.MinValue;
                m_Mobile.OwnerAbandonTime = DateTime.MinValue;

                m_Mobile.BoltEffect(0);

                if (m_Mobile.DeleteOnRelease || m_Mobile.IsDeadPet)
                    m_Mobile.Delete();
                else
                {
                    BondedPetDeathTimer t = new BondedPetDeathTimer(m_Mobile);
                    t.Start();
                }

            }
        }
        private class TransferItem : Item
        {
            private BaseCreature m_Creature;

            public TransferItem(BaseCreature creature)
                : base(ShrinkTable.Lookup(creature))
            {
                m_Creature = creature;

                Movable = false;
                Name = creature.Name;
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

                if (Deleted || m_Creature == null || m_Creature.Deleted || m_Creature.ControlMaster != from || !from.CheckAlive() || !to.CheckAlive())
                    return false;

                if (from.Map != m_Creature.Map || !from.InRange(m_Creature, 14))
                    return false;

                if (accepted && !m_Creature.CanBeControlledBy(to))
                {
                    string args = String.Format("{0}\t{1}\t ", to.Name, from.Name);

                    from.SendLocalizedMessage(1043248, args); // The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                    to.SendLocalizedMessage(1043249, args); // The pet will not accept you as a master because it does not trust you.~3_BLANK~
                    return false;
                }
                else if (accepted && !m_Creature.CanBeControlledBy(from))
                {
                    string args = String.Format("{0}\t{1}\t ", to.Name, from.Name);

                    from.SendLocalizedMessage(1043250, args); // The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                    to.SendLocalizedMessage(1043251, args); // The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                }
                else if (accepted && (to.FollowerCount + m_Creature.ControlSlots) > to.FollowersMax)
                {
                    to.SendLocalizedMessage(1049607); // You have too many followers to control that creature.

                    return false;
                }

                return true;
            }

            public override void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
            {
                if (Deleted)
                    return;

                Delete();

                if (m_Creature == null || m_Creature.Deleted || m_Creature.ControlMaster != from || !from.CheckAlive() || !to.CheckAlive())
                    return;

                if (from.Map != m_Creature.Map || !from.InRange(m_Creature, 14))
                    return;

                if (accepted)
                {
                    //normal test for not bonded transfer
                    if (m_Creature.SetControlMaster(to))
                    {
                        if (m_Creature.Summoned)
                            m_Creature.SummonMaster = to;

                        m_Creature.ControlTarget = to;
                        m_Creature.ControlOrder = OrderType.Follow;

                        m_Creature.BondingBegin = DateTime.MinValue;
                        m_Creature.OwnerAbandonTime = DateTime.MinValue;
                        m_Creature.IsBonded = false;

                        m_Creature.PlaySound(m_Creature.GetIdleSound());

                        string args = String.Format("{0}\t{1}\t{2}", from.Name, m_Creature.Name, to.Name);

                        from.SendLocalizedMessage(1043253, args); // You have transferred your pet to ~3_GETTER~.
                        to.SendLocalizedMessage(1043252, args); // ~1_NAME~ has transferred the allegiance of ~2_PET_NAME~ to you.
                    }

                }
            }
        }
        public virtual bool DoBardPacified()
        {
            if (DateTime.UtcNow < m_Mobile.BardEndTime)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am pacified, I wait");
                m_Mobile.Combatant = null;
                m_Mobile.Warmode = false;

            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I no more Pacified");
                m_Mobile.BardPacified = false;
            }

            return true;
        }
        public virtual bool DoBardProvoked()
        {
            if (DateTime.UtcNow >= m_Mobile.BardEndTime && (m_Mobile.BardMaster == null || m_Mobile.BardMaster.Deleted || m_Mobile.BardMaster.Map != m_Mobile.Map || m_Mobile.GetDistanceToSqrt(m_Mobile.BardMaster) > m_Mobile.RangePerception))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I have lost my provoker");
                m_Mobile.BardProvoked = false;
                m_Mobile.BardMaster = null;
                m_Mobile.BardTarget = null;

                m_Mobile.Combatant = null;
                m_Mobile.Warmode = false;
            }
            else
            {
                if (m_Mobile.BardTarget == null || m_Mobile.BardTarget.Deleted || m_Mobile.BardTarget.Map != m_Mobile.Map || m_Mobile.GetDistanceToSqrt(m_Mobile.BardTarget) > m_Mobile.RangePerception || m_Mobile.BardTarget.Hidden || !m_Mobile.BardTarget.Alive)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I have lost my provoke target");
                    m_Mobile.BardProvoked = false;
                    m_Mobile.BardMaster = null;
                    m_Mobile.BardTarget = null;

                    m_Mobile.Combatant = null;
                    m_Mobile.Warmode = false;
                }
                else
                {
                    m_Mobile.Combatant = m_Mobile.BardTarget;
                    Action = ActionType.Combat;

                    m_Mobile.OnThink();
                    Think();
                }
            }

            return true;
        }
        public virtual bool MoveToSound(Point3D px, bool run, int Range)
        {
            return MoveToPoint(px, run, Range);
        }
        public bool MoveToPoint(Point3D px, bool run, int Range)
        {
            if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || px == Point3D.Zero)
                return false;

            if (m_Mobile.InRange(px, Range))
            {
                m_Path = null;
                return true;
            }

            if (m_Path != null && m_Path.Goal == (IPoint3D)px)
            {
                if (m_Path.Follow(run, Range))
                {
                    m_Path = null;
                    return true;
                }
            }

            else
            {
                m_Path = new PathFollower(m_Mobile, (IPoint3D)px);
                m_Path.Mover = new MoveMethod(DoMoveImpl);

                if (m_Path.Follow(run, Range))
                {
                    m_Path = null;
                    return true;
                }
            }

            return false;
        }
        public virtual bool MoveToNavPoint(Point3D px, bool run)
        {
            bool result = false;
            if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || px == Point3D.Zero)
                return false;

            if (NavPath != null)
            {
                result = NavPath.Follow(run, 1);
            }
            else
            {
                NavPath = new PathFollower(m_Mobile, (IPoint3D)px);
                NavPath.Mover = new MoveMethod(DoMoveImpl);
                result = NavPath.Follow(run, 1);
            }

            return result;
        }
        public virtual void WalkRandom(int iChanceToNotMove, int iChanceToDir, int iSteps)
        {
            if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
                return;

            for (int i = 0; i < iSteps; i++)
            {
                if (Utility.Random(8 * iChanceToNotMove) <= 8)
                {
                    int iRndMove = Utility.Random(0, 8 + (9 * iChanceToDir));

                    switch (iRndMove)
                    {
                        case 0:
                            DoMove(Direction.Up);
                            break;
                        case 1:
                            DoMove(Direction.North);
                            break;
                        case 2:
                            DoMove(Direction.Left);
                            break;
                        case 3:
                            DoMove(Direction.West);
                            break;
                        case 5:
                            DoMove(Direction.Down);
                            break;
                        case 6:
                            DoMove(Direction.South);
                            break;
                        case 7:
                            DoMove(Direction.Right);
                            break;
                        case 8:
                            DoMove(Direction.East);
                            break;
                        default:
                            DoMove(m_Mobile.Direction);
                            break;
                    }
                }
            }
        }
        public class BondedPetReleaseGump : Gump
        {
            private BaseCreature m_Pet;

            public BondedPetReleaseGump(BaseCreature pet)
                : base(50, 50)
            {

                bool bStatLoss = true;

                m_Pet = pet;

                AddPage(0);

                AddBackground(10, 10, 265, bStatLoss ? 275 : 140, 0x242C);

                AddItem(205, 30, 0x4);
                AddItem(227, 30, 0x5);

                AddItem(180, 68, 0xCAE);
                AddItem(195, 80, 0xCAD);
                AddItem(218, 85, 0xCB0);

                AddHtml(30, 30, 150, 75, "<div align=CENTER>Wilt thou sanctify the release of:</div>", false, false); // <div align=center>Wilt thou sanctify the resurrection of:</div>
                AddHtml(30, 70, 150, 25, String.Format("<div align=CENTER>{0}</div>", pet.Name), true, false);

                if (bStatLoss)
                {
                    string statlossmessage = "By releasing your bonded companion, the spirit link between master and follower will be shattered. Alas, the companion's life will be lost in the process.";
                    AddHtml(30, 105, 195, 135, String.Format("<div align=CENTER>{0}</div>", statlossmessage), true, false);
                }

                AddButton(40, bStatLoss ? 245 : 105, 0x81A, 0x81B, 1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, bStatLoss ? 245 : 105, 0x819, 0x818, 0, GumpButtonType.Reply, 0); // Cancel
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                if (m_Pet.Deleted || !m_Pet.IsBonded)
                    return;

                PlayerMobile pm = state.Mobile as PlayerMobile;

                if (pm == null)
                    return;

                pm.CloseGump(typeof(BondedPetReleaseGump));

                if (info.ButtonID == 1) // continue
                {
                    m_Pet.AIObject.ReleaseBondedPet();
                }
                else
                {
                    pm.SendMessage("You decide not to release your companion.");
                }
            }
        }

        public double TransformMoveDelay(double delay)
        {
            bool isPassive = (delay == m_Mobile.PassiveSpeed);
            bool isControlled = (m_Mobile.Controlled || m_Mobile.Summoned);

            if (delay == 0.2)
                delay = 0.3;
            else if (delay == 0.25)
                delay = 0.45;
            else if (delay == 0.3)
                delay = 0.6;
            else if (delay == 0.4)
                delay = 0.9;
            else if (delay == 0.5)
                delay = 1.05;
            else if (delay == 0.6)
                delay = 1.2;
            else if (delay == 0.8)
                delay = 1.5;

            if (isPassive)
                delay += 0.2;

            if (!isControlled)
            {
                delay += 0.1;
            }
            else if (m_Mobile.Controlled)
            {
                if (m_Mobile.ControlOrder == OrderType.Follow && m_Mobile.ControlTarget == m_Mobile.ControlMaster)
                    delay *= 0.5;

                delay -= 0.075;
            }

            if (m_Mobile.MyCharacteristics.HasFlag(BaseCreature.Characteristics.DamageSlows)/* || m_Mobile.IsSubdued*/)
            {
                double offset = (double)m_Mobile.Hits / m_Mobile.HitsMax;

                if (offset < 0.0)
                    offset = 0.0;
                else if (offset > 1.0)
                    offset = 1.0;

                offset = 1.0 - offset;

                delay += (offset * 0.8);
            }

            if (delay < 0.0)
                delay = 0.0;

            if (double.IsNaN(delay))
            {
                using (StreamWriter op = new StreamWriter("nan_transform.txt", true))
                {
                    op.WriteLine(String.Format("NaN in TransformMoveDelay: {0}, {1}, {2}, {3}", DateTime.UtcNow, this.GetType().ToString(), m_Mobile == null ? "null" : m_Mobile.GetType().ToString(), m_Mobile.HitsMax));
                }

                return 1.0;
            }

            return delay;
        }
        public double TransformMoveBoost(double delay)
        {
            // not an actual speedup, but rather a reduction in the movement timer delays
            MobileInfo CombatantInfo = GetMobileInfo(new MobileInfo(m_Mobile.Combatant));
            // pet is following its master.
            if (m_Mobile.Controlled && (
                // always fast when following...
                (m_Mobile.ControlOrder == OrderType.Follow && m_Mobile.ControlMaster == m_Mobile.ControlTarget) ||
                // always fast when coming...
                m_Mobile.ControlOrder == OrderType.Come ||
                // Only fast if we don't have a combatant, or they are out of range, etc.
                (m_Mobile.ControlOrder == OrderType.Guard && m_Mobile.ControlMaster == m_Mobile.ControlTarget && CombatantInfo.unavailable)))
            {   // quick time following PETS
                if (delay > 0.0)
                {
                    delay *= CoreAI.RunningPetFactor;
                    m_Mobile.DebugSay(DebugFlags.Delay, "TransformMoveDelay: {0} bonus", CoreAI.RunningPetFactor);
                }
                else
                    m_Mobile.DebugSay(DebugFlags.Delay, "TransformMoveDelay: no bonus, delay already 0");
            }
            /// they are running/chasing. 
            else if (!m_Mobile.Controlled && m_Mobile.CurrentSpeed <= m_Mobile.ActiveSpeed && CanRun()
                // We are careful here not to allow the reduction if they are in fight range
                && CombatantInfo.available && !m_Mobile.InRange(CombatantInfo.target, m_Mobile.RangeFight + 1))
            {   // quick time for humanoid and flying creatures that are WILD
                if (delay > 0.0)
                {
                    if (m_Mobile.Body.IsHuman)
                    {
                        delay *= CoreAI.RunningHumFactor;
                        m_Mobile.DebugSay(DebugFlags.Delay, "TransformMoveDelay: {0} bonus", CoreAI.RunningHumFactor);
                    }
                    else
                    {
                        delay *= CoreAI.RunningWildFactor;
                        m_Mobile.DebugSay(DebugFlags.Delay, "TransformMoveDelay: {0} bonus", CoreAI.RunningWildFactor);
                    }
                }
                else
                    m_Mobile.DebugSay(DebugFlags.Delay, "TransformMoveDelay: no bonus, delay already 0");
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.Delay, "TransformMoveDelay: no bonus");
            }

            return delay;
        }

        private long m_NextMove;

        public long NextMove
        {
            get { return m_NextMove; }
            set { m_NextMove = value; }
        }

        public virtual bool CheckMove()
        {

            bool result = (Core.TickCount - m_NextMove >= 0);

            if (result == false)
                m_Mobile.DebugSay(DebugFlags.Speed, "CheckMove: too soon");

            return result;
        }

        public virtual bool DoMove(Direction d)
        {
            return DoMove(d, false);
        }
        public virtual bool DoMove(Direction d, bool badStateOk)
        {
            MoveResult res = DoMoveImpl(d);
            return (res == MoveResult.Success || res == MoveResult.SuccessAutoTurn || (badStateOk && res == MoveResult.BadState));
        }
        private static Queue m_Obstacles = new Queue();
        private bool MovableObstaclesNearBy()
        {
            IPooledEnumerable eable = m_Mobile.Map.GetItemsInRange(m_Mobile.Location, 1);
            foreach (Item item in eable)
                if (item != null && !item.Deleted && item.Movable == true)
                    if (Math.Abs(item.Z - m_Mobile.Z) <= 2)
                    {
                        eable.Free();
                        return true;
                    }
            eable.Free();
            return DidBustStuff();
        }

        public virtual MoveResult DoMoveImpl(Direction d)
        {
            if (m_Mobile.Deleted || m_Mobile.Frozen || m_Mobile.Paralyzed || (m_Mobile.Spell != null && m_Mobile.Spell.IsCasting) || m_Mobile.DisallowAllMoves)
                return MoveResult.BadState;
            else if (!CheckMove())
            {
                return MoveResult.BadState;
            }

            // This makes them always move one step, never any direction changes
            m_Mobile.Direction = d;

            double delay = TransformMoveDelay(m_Mobile.CurrentSpeed);   // standard RunUO transform
            delay = TransformMoveBoost(delay);                          // special GMN boost for pets following
            int iDelay = (int)(delay * 1000);

            m_NextMove += iDelay + m_Mobile.SlowMovement();

            if (Core.TickCount - m_NextMove > 0)
                m_NextMove = Core.TickCount;

            m_Mobile.DebugSay(DebugFlags.Speed, "My current speed is {0}, next move in {1:0.00}ms", m_Mobile.CurrentSpeed, TimeSpan.FromMilliseconds(iDelay).TotalMilliseconds);

            m_Mobile.Pushing = false;

            MoveImpl.IgnoreMovableImpassables = (m_Mobile.CanMoveOverObstacles && !m_Mobile.CanDestroyObstacles);

            if ((m_Mobile.Direction & Direction.Mask) != (d & Direction.Mask))
            {
                bool v = m_Mobile.Move(d);

                MoveImpl.IgnoreMovableImpassables = false;
                return (v ? MoveResult.Success : MoveResult.Blocked);
            }
            else if (!m_Mobile.Move(d))
            {
                bool wasPushing = m_Mobile.Pushing;

                bool blocked = true;

                bool canOpenDoors = m_Mobile.CanOpenDoors;
                bool canDestroyObstacles = m_Mobile.CanDestroyObstacles;

                if (canOpenDoors || canDestroyObstacles)
                {
                    if (m_Mobile.CantWalkLand)
                        m_Mobile.DebugSay(DebugFlags.AI, "CantWalkLand.");
                    else
                        m_Mobile.DebugSay(DebugFlags.AI, "My movement was blocked.");
                    if (MovableObstaclesNearBy())
                        m_Mobile.DebugSay(DebugFlags.AI, "I will try to clear some obstacles.");

                    Map map = m_Mobile.Map;

                    if (map != null)
                    {
                        int x = m_Mobile.X, y = m_Mobile.Y;
                        Movement.Movement.Offset(d, ref x, ref y);

                        int destroyables = 0;

                        IPooledEnumerable eable = map.GetItemsInRange(new Point3D(x, y, m_Mobile.Location.Z), 1);

                        foreach (Item item in eable)
                        {
                            if (canOpenDoors && item is BaseDoor && (item.Z + item.ItemData.Height) > m_Mobile.Z && (m_Mobile.Z + 16) > item.Z)
                            {
                                if (item.X != x || item.Y != y)
                                    continue;

                                BaseDoor door = (BaseDoor)item;

                                if (!door.Locked || !door.UseLocks())
                                    m_Obstacles.Enqueue(door);

                                if (!canDestroyObstacles)
                                    break;
                            }
                            else if (canDestroyObstacles && item.Movable && item.ItemData.Impassable && (item.Z + item.ItemData.Height) > m_Mobile.Z && (m_Mobile.Z + 16) > item.Z)
                            {
                                if (!m_Mobile.InRange(item.GetWorldLocation(), 1))
                                    continue;

                                m_Obstacles.Enqueue(item);
                                ++destroyables;
                            }
                        }

                        eable.Free();

                        if (destroyables > 0)
                            Effects.PlaySound(new Point3D(x, y, m_Mobile.Z), m_Mobile.Map, 0x3B3);

                        if (m_Obstacles.Count > 0)
                            blocked = false; // retry movement

                        while (m_Obstacles.Count > 0)
                        {
                            Item item = (Item)m_Obstacles.Dequeue();

                            if (item is BaseDoor)
                            {
                                m_Mobile.DebugSay(DebugFlags.AI, "Little do they expect, I've learned how to open doors. Didn't they read the script??");
                                m_Mobile.DebugSay(DebugFlags.AI, "*twist*");

                                ((BaseDoor)item).Use(m_Mobile);
                            }
                            else
                            {

                                m_Mobile.DebugSay(DebugFlags.AI, "Ugabooga. I'm so big and tough I can destroy it: {0}", item.GetType().Name);

                                if (item is Container)
                                {
                                    Container cont = (Container)item;

                                    for (int i = 0; i < cont.Items.Count; ++i)
                                    {
                                        Item check = (Item)cont.Items[i];

                                        if (check.Movable && check.ItemData.Impassable && (item.Z + check.ItemData.Height) > m_Mobile.Z)
                                            m_Obstacles.Enqueue(check);
                                    }

                                    cont.Destroy();
                                }
                                else
                                {
                                    item.Delete();
                                }
                            }
                        }

                        if (!blocked)
                            blocked = !m_Mobile.Move(d);
                    }
                }

                if (blocked)
                {
                    int offset = (Utility.RandomDouble() >= 0.6 ? 1 : -1);

                    for (int i = 0; i < 2; ++i)
                    {
                        m_Mobile.TurnInternal(offset);

                        if (m_Mobile.Move(m_Mobile.Direction))
                        {
                            MoveImpl.IgnoreMovableImpassables = false;
                            return MoveResult.SuccessAutoTurn;
                        }
                    }

                    MoveImpl.IgnoreMovableImpassables = false;
                    return (wasPushing ? MoveResult.BadState : MoveResult.Blocked);
                }
                else
                {
                    MoveImpl.IgnoreMovableImpassables = false;
                    return MoveResult.Success;
                }
            }

            MoveImpl.IgnoreMovableImpassables = false;
            return MoveResult.Success;
        }
        public virtual void WalkRandomInHome(int iChanceToNotMove, int iChanceToDir, int iSteps, bool evenIfStationed = false)
        {
            if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
                return;

            if (m_Mobile.Home == Point3D.Zero)
            {
                WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
            }
            else
            {
                for (int i = 0; i < iSteps; i++)
                {
                    if (m_Mobile.RangeHome != 0)
                    {
                        int iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m_Mobile.Home);

                        if (iCurrDist < m_Mobile.RangeHome * 2 / 3)
                        {
                            WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                            m_Mobile.IAmHome();      // 7/24/21, Adam: allow individual mobiles to decide what they do when they get home.
                        }
                        else if (iCurrDist > m_Mobile.RangeHome)
                        {
                            DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
                            m_Mobile.GoingHome();   // 7/24/21, Adam: allow individual mobiles to decide what they do when they are going home.
                        }
                        else
                        {
                            if (Utility.Random(10) > 5)
                            {
                                DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
                            }
                            else
                            {
                                WalkRandom(iChanceToNotMove, iChanceToDir, 1);
                            }
                        }
                    }
                    else
                    {
                        if (m_Mobile.Location != m_Mobile.Home)
                        {
                            DoMove(m_Mobile.GetDirectionTo(m_Mobile.Home));
                            m_Mobile.GoingHome();   // 7/24/21, Adam: allow individual mobiles to decide what they do when they are going home.
                        }
                        else
                        {
                            // default behavior of mobiles that have a spawner is to face the direction specified in that spawner
                            m_Mobile.IAmHome();      // 7/24/21, Adam: allow individual mobiles to decide what they do when they get home.

                            // 13/30/2023, Adam if you tell a guard to move from their station, we gotta move
                            //  evenIfStationed is set to true when the player issues the "move" request
                            if (evenIfStationed)
                                WalkRandom(iChanceToNotMove, iChanceToDir, iSteps);
                        }
                    }
                }
            }
        }
        public virtual bool CheckFlee()
        {
            if (m_Mobile.CheckFlee())
            {
                Mobile combatant = m_Mobile.Combatant;

                if (combatant == null)
                {
                    WalkRandom(1, 2, 1);
                }
                else
                {
                    Direction d = combatant.GetDirectionTo(m_Mobile);

                    d = (Direction)((int)d + Utility.RandomMinMax(-1, +1));

                    m_Mobile.Direction = d;
                    m_Mobile.Move(d);
                }

                return true;
            }

            return false;
        }
        protected PathFollower m_Path;
        public PathFollower Path
        {   // see BaseCreature.ConsiderCombatantChange()
            get { return m_Path; }
            set { m_Path = value; }
        }
        public PathFollower NavPath;
        public virtual void OnTeleported()
        {
            if (m_Path != null)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "Teleported; repathing");
                m_Path.ForceRepath();
            }
        }
        public virtual bool MoveTo(Mobile m, bool run, int range)
        {
            if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves || m == null || m.Deleted)
                return false;

            return MoveTo(m.Location, run, range);
        }
        public bool DoorExploit(Point3D px, Direction dirTo)
        {
            bool exploit = false;
            int x = m_Mobile.X, y = m_Mobile.Y;
            IPooledEnumerable eable = m_Mobile.Map.GetItemsInRange(new Point3D(x, y, m_Mobile.Location.Z), 1);
            bool canOpenDoors = m_Mobile.CanOpenDoors;  // are we sure we need this check?
            Movement.Movement.Offset(dirTo, ref x, ref y);

            foreach (Item item in eable)
            {
                if (canOpenDoors && item is BaseDoor && (item.Z + item.ItemData.Height) > m_Mobile.Z && (m_Mobile.Z + 16) > item.Z)
                {
                    // we are within 1 tile of our target, but we are also at the same location as a door.
                    //	the player is probably trying to stand behind an open door so that they can hit us, but we cannot hit them.
                    if (item.X == m_Mobile.X && item.Y == m_Mobile.Y)
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, "Probable door exploit, try something else...");
                        exploit = true;
                        break;
                    }

                }
            }

            eable.Free();

            return exploit;
        }
        public bool MoveTo(Point3D px, bool run, int range)
        {
            Direction dirTo;

            dirTo = m_Mobile.GetDirectionTo(px);
            // Add the run flag
            if (run)
                dirTo = dirTo | Direction.Running;

            #region DoorExploit
            //	the door exploit is where the player is probably standing behind an open door so that they can hit us, but we cannot hit them.
#if false
            if (false && /* adam: not sure this is even working*/ m_Mobile.InRange(px, range) && !DoorExploit(px, dirTo))
            {
                m_Mobile.Direction = dirTo; //make sure we point toward our enemy
                m_Path = null;
                return true;
            }
#endif
            #endregion DoorExploit

            // Don't bother with DoMove if we're needing to run up a flight of stairs turn left/right etc.. Just too many fails
            bool bad_Z = Math.Abs(m_Mobile.Z - px.Z) > 2;
            // did our goal move?
            bool goal_moved = (m_Path != null && new Point3D(m_Path.Goal.X, m_Path.Goal.Y, m_Path.Goal.Z) != px);
            // are we close enough
            bool close_enough = !goal_moved && !bad_Z && m_Mobile.InRange(px, m_Mobile.RangeFight) && m_Mobile.InLOS(px);
            bool need_pathing = bad_Z || goal_moved;
            bool move_failed = false;

            //  If we have a path, use it. if there is too great of a Z delta, use pathing, else, try to move.
            if ((need_pathing && !close_enough) || !(move_failed = DoMove(dirTo, true)))
            {
                if (move_failed)
                    m_Mobile.DebugSay(DebugFlags.Movement, "MoveTo: DoMove failed");

                m_Mobile.DebugSay(DebugFlags.Movement, "MoveTo: I'm using pathing");
                if (m_Path == null || goal_moved)
                {
                    m_Path = new PathFollower(m_Mobile, px);
                    m_Path.Mover = new MoveMethod(DoMoveImpl);
                }

                if (m_Path.Follow(run, 1))
                {

                    if (m_Path.Check(m_Mobile.Location, px, range))
                        m_Mobile.DebugSay(DebugFlags.Movement, "MoveTo: I have arrived!");

                    m_Path = null;
                    return true;
                }

                if (m_Path != null && !m_Path.Success)
                {
                    m_Mobile.DebugSay(DebugFlags.Movement, "MoveTo: having trouble pathing...");
                    return false;
                }
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.Movement, "MoveTo: I can move normally");
                m_Path = null;
                return true;
            }

            return false;
        }

        /*
		 *  Walk at range distance from mobile
		 *
		 *	iSteps : Number of steps
		 *	bRun   : Do we run
		 *	iWantDistMin : The minimum distance we want to be
		 *  iWantDistMax : The maximum distance we want to be
		 *
		 */
        public virtual bool WalkMobileRange(Mobile m, int iSteps, bool bRun, int iWantDistMin, int iWantDistMax)
        {
            if (m_Mobile.Deleted || m_Mobile.DisallowAllMoves)
                return false;

            Map map = m_Mobile.Map;

            if (m != null)
            {
                //if were a human mob that can run show that to the world walk/run accordingly
                if (CanRunAI)
                {
                    if ((m.Direction & Direction.Running) != 0)
                        bRun = true;
                    else
                        bRun = false;
                }

                for (int i = 0; i < iSteps; i++)
                {
                    // Get the current distance
                    int iCurrDist = (int)m_Mobile.GetDistanceToSqrt(m);

                    if (iCurrDist < iWantDistMin || iCurrDist > iWantDistMax)
                    {
                        bool needCloser = (iCurrDist > iWantDistMax);
                        bool needFurther = !needCloser;

                        if (needCloser && m_Path != null && m_Path.Goal == m)
                        {

                            if (m_Path.Follow(bRun, 1))
                                m_Path = null;
                        }
                        else
                        {
                            Direction dirTo;

                            if (iCurrDist > iWantDistMax)
                                dirTo = m_Mobile.GetDirectionTo(m);
                            else
                                dirTo = m.GetDirectionTo(m_Mobile);

                            // Add the run flag
                            if (bRun)
                                dirTo = dirTo | Direction.Running;

                            if (!DoMove(dirTo, true) && needCloser)
                            {
                                IPooledEnumerable eable = map.GetItemsInRange(m_Mobile.Location, 10);
                                foreach (Item item in eable)
                                {
                                    if (item is BaseBoat && ((BaseBoat)item).Contains(m) && (((BaseBoat)item).PPlank.IsOpen || ((BaseBoat)item).SPlank.IsOpen) && !((BaseBoat)item).Contains(m_Mobile))
                                    {
                                        if (((BaseBoat)item).PPlank.IsOpen)
                                            ((BaseBoat)item).PPlank.OnDoubleClick(m_Mobile);
                                        else
                                            ((BaseBoat)item).SPlank.OnDoubleClick(m_Mobile);
                                    }
                                }
                                eable.Free();

                                m_Path = new PathFollower(m_Mobile, m);
                                m_Path.Mover = new MoveMethod(DoMoveImpl);

                                if (m_Path.Follow(bRun, 1))
                                    m_Path = null;
                            }
                            else
                            {
                                m_Path = null;
                            }
                        }
                    }
                    else
                    {
                        return true;
                    }
                }

                // Get the current distance
                int iNewDist = (int)m_Mobile.GetDistanceToSqrt(m);

                if (iNewDist >= iWantDistMin && iNewDist <= iWantDistMax)
                    return true;
                else
                    return false;
            }

            return false;
        }
        public virtual void Deactivate()
        {
            if (m_Mobile.CanDeactivate)
            {
                m_Timer.Flush();
                m_Timer.Stop();
            }
        }
        public bool Active { get { return m_Timer != null ? m_Timer.Running : false; } }
        public virtual void Activate()
        {
            if (!m_Timer.Running)
            {
                MuxRegister(this, delay: TimeSpan.Zero, interval: TimeSpan.FromSeconds(Math.Max(0.0, m_Mobile.CurrentSpeed)));
                MuxStart(this);
            }
        }

        /*
		 *  The mobile changed it speed, we must adjust the timer
		 */
        public virtual void OnCurrentSpeedChanged()
        {
            MuxRegister(this, delay: TimeSpan.FromSeconds(Utility.RandomDouble()), interval: TimeSpan.FromSeconds(Math.Max(0.0, m_Mobile.CurrentSpeed)));
            MuxStart(this);
        }

        /*
		 *  The Timer object
		 */
        public class AITimer
        {
            private BaseAI m_Owner;
            public AITimer(BaseAI owner)
            {
                m_Owner = owner;

                MuxRegister(owner, TimeSpan.FromSeconds(Utility.RandomDouble()), TimeSpan.FromSeconds(Math.Max(0.0, owner.m_Mobile.CurrentSpeed)));
                MuxStart(owner);
            }
            public void Flush()
            {
                ;
            }
            public bool Running
            {
                get
                {
                    return MuxRunning(m_Owner);
                }
            }
            public void Start()
            {
                MuxStart(m_Owner);
            }
            public void Stop()
            {
                MuxStop(m_Owner);
            }

            public void OnTick()
            {
                if (m_Owner.m_Mobile == null || m_Owner.m_Mobile.Deleted)
                {
                    Stop();
                    return;
                }
                else if (m_Owner.m_Mobile.AIObject != m_Owner)
                {   // ai was changed, yet there was still a tick in the queue
                    Stop();
                    return;
                }
                else if (m_Owner.m_Mobile.Map == null || m_Owner.m_Mobile.Map == Map.Internal)
                {

                    return;
                }
                else
                {
                    Sector sect = m_Owner.m_Mobile.Map.GetSector(m_Owner.m_Mobile);
                    if (!sect.Active)
                    {
                        if (m_Owner.m_Mobile.Debug)
                        {
                            if (m_Owner.m_Mobile.Hits < m_Owner.m_Mobile.HitsMax)
                                m_Owner.m_Mobile.DebugSay(DebugFlags.AI, "My sector has been deactivated, but I am healing {0} hits", m_Owner.m_Mobile.HitsMax - m_Owner.m_Mobile.Hits);
                            else
                                m_Owner.m_Mobile.DebugSay(DebugFlags.AI, "My sector has been deactivated, but why am I still alive?");
                        }

                        /* 4/4/23, Adam: rework the way mobile deactivation works.
                         * Background: BaseCreature already has a very clean model for this: OnSectorDeactivate() / OnSectorActivate()
                         * The RunUO guys got lazy and also added mobile deactivation here. It made for some nasty debugging and obliterated 
                         * any chance of overriding mobile deactivation in a clean way. There are a number of cases where overriding mobile deactivation
                         * makes sense: 1) If the player is kiting (ducking in and out of a teleporter), 2) you have a champion that needs to keep
                         * active to heal, regen mana, etc.
                         * What has changed: Camps, Spawners, and Champ Engines all now deactivate the spawned creature if the sector is inactive.
                         * It is then the responsibility of the virtual OnSectorDeactivate() / OnSectorActivate() to handle creature activation/deactivation.
                         * Old code for reference:
                         *  m_Owner.Deactivate();
                         *  return;
                         */
                    }
                }

                m_Owner.m_Mobile.OnThink();

                if (m_Owner.m_Mobile.Deleted)
                {
                    Stop();
                    return;
                }
                else if (m_Owner.m_Mobile.Map == null || m_Owner.m_Mobile.Map == Map.Internal)
                {
                    return;
                }

                if (m_Owner.m_Mobile.BardPacified)
                {
                    m_Owner.DoBardPacified();
                }
                else if (m_Owner.m_Mobile.BardProvoked)
                {
                    m_Owner.DoBardProvoked();
                }
                else
                {
                    if (!m_Owner.m_Mobile.Controlled)
                    {
                        if (!m_Owner.Think())
                        {
                            Stop();
                            return;
                        }
                    }
                    else
                    {
                        if (!m_Owner.Obey())
                        {
                            Stop();
                            return;
                        }
                    }
                }
            }
        }

        public virtual void OnAggressiveAction(Mobile aggressor)
        {
            if (m_Mobile.Controlled)
                if (aggressor is BaseCreature && (((BaseCreature)aggressor).IsScaryToPets && ((BaseCreature)aggressor).IsScaryCondition()) && m_Mobile.IsScaredOfScaryThings)
                    m_Mobile.DebugSay(DebugFlags.AI, "I am ScaredOfScaryThings and {0} is Scary!", aggressor.Name);
        }

        public virtual void OnSignal(Mobile.SignalType signal)
        {
            switch (signal)
            {
                case Mobile.SignalType.None:
                    break;
                case Mobile.SignalType.COMBAT_TIMER_TICK:
                    break;
                case Mobile.SignalType.COMBAT_TIMER_EXPIRED:
                    break;
            }
            return;
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize

        #region Commands
        public static void Initialize()
        {
            CommandSystem.Register("SetAction", AccessLevel.GameMaster, new CommandEventHandler(SetAction_OnCommand));
            CommandSystem.Register("TrapNullCombatant", AccessLevel.GameMaster, new CommandEventHandler(TrapNullCombatant_OnCommand));
            InitializeMux();
        }
        #region SetAction
        [Usage("SetAction")]
        [Description("Sets the next Think() Action.")]
        public static void SetAction_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the Mobil you wish to effect.");
            e.Mobile.Target = new GetMobileTarget(e.ArgString);
        }
        public class GetMobileTarget : Target
        {
            private string m_newActionString;
            private ActionType m_newActionType;
            public GetMobileTarget(object o) : base(12, true, TargetFlags.None) { m_newActionString = (string)o; }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                string clipboard = string.Empty;
                if (mt == null)
                {
                    from.SendMessage("That is not a mobile.");
                    return;
                }
                try
                {
                    m_newActionType = (ActionType)Enum.Parse(typeof(ActionType), m_newActionString, true);
                    if (mt is BaseCreature bc && bc.AIObject != null)
                        bc.AIObject.Action = m_newActionType;
                }
                catch
                {
                    from.SendMessage("That is not a valid Action.");
                }
            }
        }
        #endregion SetAction

        #region TrapNullCombatant
        [Usage("TrapNullCombatant")]
        [Description("Allows setting of a breakpoint when combatant is set to null.")]
        public static void TrapNullCombatant_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the Mobil you wish to effect.");
            e.Mobile.Target = new GetTrapTarget();
        }
        public class GetTrapTarget : Target
        {
            public GetTrapTarget() : base(12, true, TargetFlags.None) {; }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                string clipboard = string.Empty;
                if (mt == null)
                {
                    from.SendMessage("That is not a mobile.");
                    return;
                }
                try
                {
                    if (!mt.CombatantChangeMonitor.Contains(mt.Serial))
                        mt.CombatantChangeMonitor.Add(mt.Serial);
                    else
                        mt.CombatantChangeMonitor.Remove(mt.Serial);

                    if (mt.CombatantChangeMonitor.Contains(mt.Serial))
                        from.SendMessage("Trap target added for {0}.", mt);
                    else
                        from.SendMessage("Trap target removed for {0}.", mt);
                }
                catch
                {
                    from.SendMessage("Something went wrong.");
                }
            }
        }
        #endregion TrapNullCombatant
        #endregion Commands
    }
}