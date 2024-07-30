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

/* Scripts/Spells/Base/SpellHelper.cs
 *	ChangeLog:
 *	3/14/2024, Adam (CheckTravel)
 *	    Replace all the checks for staff (caster.AccessLevel > AccessLevel.Player) with caster.AccessLevel == AccessLevel.System.
 *	    Essentially, this says no one, not even staff, can subvert the travel restrictions. Otherwise, testing is much more difficult.
 *	    If staff needs to get somewhere, they either need to [go, or use [tele on a handmade recall rune (tele also works on a teleporter/gate).
 *	5/12/23, Yoar (OldResist)
 *      There seems to be a mismatch between resisting spells as described on the Stratics archives and RunUO.
 *      Perhaps resisting spells was overhauled early on. Not sure about the cut-off publish here. Let's use Pub14.
 * 
 *      The source only provides info about how resisting spells affect direct damage spell.
 *      No info is given regarding duration spells. Let's assume the duration is also halved, when resisted.
 * 
 *      Source: https://web.archive.org/web/20001214105700fw_/http://uo.stratics.com/resistance.shtml
 *	5/5/23, Adam (rules processing: IsFeluccaDungeon)
 *	    Convert IsFeluccaDungeon from static named dungeons, to a check for IsDungeonRules
 *	9/13/22, Adam (CheckTravel)
 *	    fix the 'double check' for recall to 
 *	    1. Add Mortalis
 *	    2. Allow staff
 *  9/2/22, Yoar (WorldZone)
 *      Added WorldZone check in order to contain players within the world zone.
 *	8/28/22, Yoar
 *	    Added travel rule for factions strongholds.
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	1/4/22, Adam (TravelRestrictions)
 *	    Add a list of rects where all travel to/from is blocked.
 *	    This list handles the case weather or not there is a region.
 *	    i.e., not a region consideration.
 *	5/25/2021, Adam
 *		allow marking by Admins+ in CheckTravel
 *	2/10/11, Adam
 *		Add the travel valadators and associated rules to and Initialize() function so that we can modify them based on the server we are launching,
 *			i.e., Angel Island, Siege Perilous, Mortalis, etc.
 *	12/28/10, adam
 *		Add FindValidSpawnLocation
 *		// http://www.google.com/codesearch/p?hl=en#biPgqLK3B_w/trunk/Scripts/Spells/Base/SpellHelper.cs&q=FindValidSpawnLocation&exact_package=http://runuomondains.googlecode.com/svn&sa=N&cd=1&ct=rc&l=502
 * 11/06/10, Pix
 *      Conditionalized out recall for IS in CheckTravel.
 *	6/18/10, Adam
 *		o Update region logic to reflect shift from static to new dynamic regions
 *		o Remove IsDungeonRules as BaseRegion now has that logic
 *	6/16/10, adam
 *		format source code
 *	6/10/10, Adam
 *		Add new IsDungeonRules() function.
 *		This is needed since we moved away from the static regions of RunUO
 *	10/9/08, Adam
 *		Make iStack public so we can use it in ExploitTracking.cs
 *		Have iStack free the PooleedEnumerable in ALL exit cases (we were leaking enumerables
 *  7/22/08, Adam
 *      Add TeleportTo and Mark restrictions involving stacks of movable items.
 *          1. You cannot teleport onto a stack of movable items which contain items on more than one Z plane
 *          2. You cannot mark a rune on a stack of movable items which contain items on more than one Z plane
 *  4/3/07, Adam
 *      update AdjustField to use Utility.CanFitFlags.requireSurface so that fields can't be cast on water
 *      Note: I believe this used to work but was broken when i rewrote the core CanFit interface
 *  3/25/07, Adam
 *      Replace some old src/dest checking logic for teleport with the new CheckParity() method.
 *      CheckParity takes a simplified approach that also includes bare multis and not just regions.
 *      This new approach is both simplier and also handles the PreviewHouse exploits.
 *  3/20/07, Adam
 *      Add two TeleportTo cases:
 *      - Try to teleport through front door (LOS)
 *      - Try to teleport over front door (Z Axis)
 *	02/01/07, Pix
 *		Added new AdjustField so we can use Map.CanFit's new ignore ghosts parameter when mobiles block.
 *  12/31/06, Kit
 *      Updated Spell Restriction checks to work with new invalid range logging, added Exception logging
 *      to generic Catchs()
 *	12/27/06, Pix
 *		Moved DefensiveSpell class to server.exe build.
 *	10/17/06, Adam
 *		Add trapping of tower->castle teleport exploit
 *			- multi sector / multi region (two houses)
 *			- overlaping regions (tower overhang, overhangs another house.)
 *			- cheater logging
 *  6/26/06, Kit
 *		Changed CheckTravel function and DRDT handleing of gates/moonstones, added in new DRDT check for 
 *		Allowing travel as long as its in the DRDT region and not in or out.
 *  6/25/06, Kit
 *		Changed IsStronghold to IsRansomChestArea, extended brigand ransom chest area.
 *  6/24/06, Kit
 *		Added drdt region spell restriction check to ValidIndirectTarget to prevent selection of targets
 *		in a drdt region when a spell is disallowed in that region.
 *  6/03/06, Kit
 *		Added creature CheckSpellImmunity call for damage modification based on spell damage type
 *		to damage function.
 *	2/21/06, Pix
 *		Added SecurePremises feature to CheckTravel().
 *	12/17/05, Pigpen
 *		Added Good IOBStronghold definition. Included entire island plus docks in vesper.
 *	9/23/05, Adam
 *		Add IsZooCage() to the travel checker to make sure players cannot
 *		tele/recall into and out of the animal cages.
 *	9/15/05, Adam
 *		speedup/optimize: teleport checking
 *		add the redundant If (src_house != null) check in TeleportTo to 
 *		eliminate the house enumeration	for the cases when it's not needed.
 *	9/14/05, Adam
 *		a. Fix the cross housing region teleport exploit
 *		make sure both source and destination housing regions are non-null 
 *		before checking to see if they are different
 *		b. fix bug with Dynamic region checking (after my cleanup.)
 *	9/12/05, Adam
 *		a. add check for cross housing region teleport exploit
 *			If you are in a housing region AND 
 *			your target is a housing region AND 
 *			source region != destination region THEN
 *			NO TELEPORT FOR YOU!
 *		b. cleanup custom regions code a tad (put in it's own IF block)
 *	5/11/05, Adam
 *		Rename IsSpecial ==> IsHedgeMaze
 *		Add IsAngelIsland
 *	5/04/05, Kit
 *		Added additional checks/catch for drdt checks
 *	4/30/05, Kit
 *		Added checks to CheckTravel to handel recalling/gateing/teleporting into custom regions
 *	4/9/05, Adam
 *		Added IsSpecial() (TravelValidator)
 *		Added restrictions for travel to/from 'special' locations
 *  3/25/05, Lego
 *      Fixed orcfort no recall zone.
 *	1/19/05, Pix
 *		Fixed the pirate stronghold numbers.
 *	1/2/05, Adam
 *		Increase Savage stronghold to the whole building
 *		Increase Pirate stronghold to the whole ship
 *		Increase Brigand stronghold to the whole building
 *	1/1/05, Adam
 *		Increase Orc's Yew stronghold to the whole fort
 *	9/30/04, smerX
 *		Added restrictions for travel to/from kin strongholds
 *	9/11/04, Pix
 *		Removed the guild check - fields and explosion pots will affect guildmates now
 *	7/13/04, Pix
 *		Set the m_Rules array correctly for dungeons, etc so people can't cast mark outside
 *		of a dungeon and run in and target a rune there.
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/12/04, mith
 *		Fixed ValidIndirectTarget() to affect un-guilded blues.
 *	5/8/04, mith
 *		modified ValidIndirectTarget() to not affect guildmates.
 *	3/25/04 changes by merXy:
 *		SpellHelper.cs DamageDelays are no longer referenced
 *		Ammended rules for ValidIndirectTarget
 *			Field spells now affect all mobiles
 *	3/25/04 code changes by mith:
 *		modified CheckSkill calls with max skill of 120 lowered to 100
 *	3/18/04 code changes by smerX:
 *		Edited Default DamageDelays
 *		Ammended travel rules for specific areas
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Guilds;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using Server.Spells.Fifth;
using Server.Spells.Fourth;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Third;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Spells
{
    public enum TravelCheckType
    {
        RecallFrom,
        RecallTo,
        GateFrom,
        GateTo,
        Mark,
        TeleportFrom,
        TeleportTo
    }

    public class SpellHelper
    {
        public static bool OldResist { get { return (PublishInfo.Publish < 14.0); } }

        private static TimeSpan AosDamageDelay = TimeSpan.FromSeconds(1.0);
        private static TimeSpan OldDamageDelay = TimeSpan.FromSeconds(0.5);

        private static bool[,] m_Rules;
        private static TravelValidator[] m_Validators;
        public static void Initialize()
        {
            // rules for all shards. For shard differences, like shards that don't have kin, you would modify the IsRansomChestArea to return false
            m_Rules = new bool[,]
                {
	/*					T2A(Fel)    Ilshenar	Wind(Tram)	Wind(Fel)	Dungeons(Fel)	Solen(Tram)	Solen(Fel)	CrystalCave(Malas)  FactionStronghold	Gauntlet(Malas),	Gauntlet(Ferry)	Stronghold(Fel)	Hedge Maze	Angel Island	Zoo     Yew Orc Fort*/
	/* Recall From */ { false,      false,      false,      false,      false,          false,      false,      false,              true,               false,              false,          false,          false,      false,          false,  true },
	/* Recall To */	  { false,      false,      false,      false,      false,          false,      false,      false,              false,              false,              false,          false,          false,      false,          false,  true },
	/* Gate From */	  { false,      false,      false,      false,      false,          false,      false,      false,              false,              false,              false,          false,          false,      false,          false,  true },
	/* Gate To */	  { false,      false,      false,      false,      false,          false,      false,      false,              false,              false,              false,          false,          false,      false,          false,  false },
	/* Mark In */	  { false,      false,      false,      false,      false,          false,      false,      false,              false,              false,              false,          true,           true,       false,          false,  false },
	/* Tele From */	  { true,       false,      false,      true,       true,           false,      false,      false,              false,              false,              false,          true,           true,       false,          false,  true },
	/* Tele To */	  { true,       false,      false,      true,       true,           false,      false,      false,              false,              false,              false,          true,           true,       false,          false,  true },
                };

            // It's ok if the Angel Island area is called out here for all shards as long as the rules above reflect the correct behavior.
            m_Validators = new TravelValidator[]
            {   // the order of this table must match the table above. I.e., 
                //  T2A(Fel), Ilshenar, Wind(Tram), etc.
                new TravelValidator( IsFeluccaT2A ),
                new TravelValidator( IsIlshenar ),
                new TravelValidator( IsTrammelWind ),
                new TravelValidator( IsFeluccaWind ),
                new TravelValidator( IsFeluccaDungeon ),
                new TravelValidator( IsTrammelSolenHive ),
                new TravelValidator( IsFeluccaSolenHive ),
                new TravelValidator( IsCrystalCave ),
                new TravelValidator( IsFactionStronghold ),
                new TravelValidator( IsDoomGauntlet ),
                new TravelValidator( IsDoomFerry ),
                new TravelValidator( IsKinArea ),
                new TravelValidator( IsHedgeMaze ),
                new TravelValidator( IsAngelIsland ),
                new TravelValidator( IsZooCage ),
                new TravelValidator( IsYewOrcFort ),

            };
        }

        private delegate bool TravelValidator(Map map, Point3D loc);
        public static bool CheckTravel(Mobile caster, TravelCheckType type)
        {
            return CheckTravel(caster, type, true);
        }

        public static bool CheckTravel(Mobile caster, TravelCheckType type, bool message)
        {
            if (CheckTravel(caster.Map, caster.Location, type, caster))
                return true;

            if (message)
                SendInvalidMessage(caster, type);
            return false;
        }

        public static bool CheckTravel(Mobile caster, Map map, Point3D loc, TravelCheckType type)
        {
            return CheckTravel(caster, map, loc, type, true);
        }

        public static bool CheckTravel(Mobile caster, Map map, Point3D loc, TravelCheckType type, bool message)
        {
            if (CheckTravel(map, loc, type, caster))
                return true;

            if (message)
                SendInvalidMessage(caster, type);
            return false;
        }

        public static bool CheckTravel(Map map, Point3D loc, TravelCheckType type, Mobile caster)
        {
            bool jail = false;
            return CheckTravel(map, loc, type, caster, out jail);
        }

        #region Rectangle based Travel Restrictions
        private static readonly List<TravelCheckType> AllTravel = new() {
            TravelCheckType.GateTo, TravelCheckType.GateFrom,
            TravelCheckType.RecallTo, TravelCheckType.RecallFrom,
            TravelCheckType.TeleportTo,TravelCheckType.TeleportFrom,
        };
        public class TravelRect
        {
            public Map m_Map;                       // map null applies to all maps
            public Rectangle2D m_Rect;              // rect to monitor
            public List<TravelCheckType> m_Types;   // restricted types
            public TravelRect(Map map, Rectangle2D rect, List<TravelCheckType> types)
            {
                m_Map = map;
                m_Rect = rect;
                m_Types = types;
            }
        }
        // travel restrictions
        public static List<TravelRect> TravelRestrictions = new List<TravelRect>()
        {
            // entrance to Fire dungeon
            new TravelRect(null, new Rectangle2D(5745, 2901, 39, 16),AllTravel),//(5745, 2901)+(39, 16)
            // entrance to Ice dungeon
            new TravelRect(null, new Rectangle2D(5196, 2310, 33, 32),AllTravel),//(5196, 2310)+(33, 32)
            // Dagger Island Trammel
            new TravelRect(Map.Trammel, Utility.World.DaggerIsland,
                new List<TravelCheckType>() {TravelCheckType.GateTo, TravelCheckType.GateFrom,TravelCheckType.RecallTo, TravelCheckType.RecallFrom,TravelCheckType.Mark }),
        };
        #endregion Rectangle based Travel Restrictions

        public static bool CheckTravel(Map map, Point3D loc, TravelCheckType type, Mobile caster, out bool jail)
        {
            jail = false;

            if (IsInvalid(map, loc)) // null, internal, out of bounds
                return false;

            if (caster == null)
                return false;

            #region Rectangle based Travel Restrictions
            foreach (var node in TravelRestrictions)
                if (caster.Player)
                    if (node.m_Types.Contains(type))
                        if (node.m_Map == null || caster.Map == node.m_Map) // map null applies to all maps
                            if (node.m_Rect.Contains(loc) || node.m_Rect.Contains(caster.Location))
                                return false;
            #endregion Rectangle based Travel Restrictions

            #region Siege Recall
            //double-check safety - if checking recall on siege, return false.
            //  We don't prohibit NPCs from recalling (healers and guards recall back to their spawner)
            if (caster.Player && (type == TravelCheckType.RecallFrom || type == TravelCheckType.RecallTo)
                && (Core.RuleSets.SiegeStyleRules()) && caster.AccessLevel != AccessLevel.System)
            {
                return false;
            }
            #endregion Siege Recall

            #region World Zone
            if ((type == TravelCheckType.RecallTo || type == TravelCheckType.GateTo || type == TravelCheckType.TeleportTo) && WorldZone.BlockTeleport(caster, loc, map))
                return false;
            #endregion

            #region Winter Event
            if (type == TravelCheckType.Mark && WinterEventSystem.Contains(loc, map))
                return false;
            #endregion

            #region Static Region
            StaticRegion srcReg = StaticRegion.FindStaticRegion(caster);
            StaticRegion trgReg = StaticRegion.FindStaticRegion(loc, map);
            if (srcReg != null && srcReg == trgReg && srcReg.AllowTravelSpellsInRegion)
            {
                // ignore regional travel restrictions
            }
            else
            {
                if (srcReg != null)
                {
                    if (type == TravelCheckType.RecallFrom && srcReg.IsRestrictedSpell(typeof(RecallSpell)) && caster.AccessLevel != AccessLevel.System)
                        return false;
                    if (type == TravelCheckType.GateFrom && srcReg.IsRestrictedSpell(typeof(GateTravelSpell)) && caster.AccessLevel != AccessLevel.System)
                        return false;
                    if (type == TravelCheckType.TeleportFrom && srcReg.IsRestrictedSpell(typeof(TeleportSpell)) && caster.AccessLevel != AccessLevel.System)
                        return false;
                    if (type == TravelCheckType.Mark && srcReg.IsRestrictedSpell(typeof(MarkSpell)) && caster.AccessLevel != AccessLevel.System)
                        return false;
                }
                if (trgReg != null)
                {
                    if (type == TravelCheckType.RecallTo && (trgReg.CannotEnter || trgReg.NoRecallInto) && caster.AccessLevel != AccessLevel.System)
                        return false;
                    if (type == TravelCheckType.GateTo && (trgReg.CannotEnter || trgReg.NoGateInto) && caster.AccessLevel != AccessLevel.System)
                        return false;
                    if (type == TravelCheckType.TeleportTo && trgReg.CannotEnter && caster.AccessLevel != AccessLevel.System)
                        return false;
                }
            }
            #endregion

            #region Housing SecurePremises flag
            //Deal with house SecurePremises flag
            if ((type == TravelCheckType.GateTo || type == TravelCheckType.RecallTo ||
                type == TravelCheckType.TeleportTo) && caster.AccessLevel != AccessLevel.System)
            {
                BaseHouse dst_house = null;
                Sector sector = map.GetSector(loc);
                foreach (BaseMulti mx in sector.Multis)
                {
                    BaseHouse _house = mx as BaseHouse;
                    if (_house == null)
                        continue;

                    if (_house != null && _house.Region.Contains(loc))
                    {
                        dst_house = _house;
                    }
                }

                if (dst_house != null)
                {
                    if (dst_house.SecurePremises)
                    {
                        if (!dst_house.IsFriend(caster))
                        {
                            return false;
                        }
                    }
                }

            }
            #endregion Housing SecurePremises flag

            #region PreviewHouse exploit
            // Gate inside a PreviewHouse exploit. Go directly to jail, don't collect $200.0
            if ((type == TravelCheckType.GateTo || type == TravelCheckType.RecallTo ||
                type == TravelCheckType.TeleportTo) && caster.AccessLevel != AccessLevel.System)
            {
                if (PreviewHouseAt(caster.Map, loc))
                {
                    RecordCheater.Cheater(caster, "Travel inside a PreviewHouse exploit.", true);
                    jail = true;
                    return false;
                }
            }
            #endregion PreviewHouse exploit

            #region Various Exploits
            // Teleport onto a stack of stuff =\
            if (type == TravelCheckType.TeleportTo && caster.AccessLevel != AccessLevel.System)
            {
                // if we are teleporting onto a stack of movable items, and at least 3 reside on different Z planes
                //  then it looks too much like an exploit to allow it.
                if (iStack(caster, loc, 2) == false)
                {
                    RecordCheater.Cheater(caster, "Tele onto a stack of movable items.", true);
                    return false;
                }
            }

            // mark on a stack of stuff =\
            // same as teleport above, but different limit and different message
            if (type == TravelCheckType.Mark && caster.AccessLevel != AccessLevel.System)
            {
                // if we are marking on a stack of movable items, and at least 2 reside on different Z planes
                //  then it looks too much like an exploit to allow it.
                if (iStack(caster, loc, 2) == false)
                {
                    RecordCheater.Cheater(caster, "Mark on a stack of movable items.", true);
                    return false;
                }
            }

            // disallow teleporting from outside the house into the inside if the doors are closed (no LOS)
            if ((type == TravelCheckType.TeleportTo) && caster.AccessLevel != AccessLevel.System)
            {
                BaseHouse dst_house = null;
                Sector sector = map.GetSector(loc);
                foreach (BaseMulti mx in sector.Multis)
                {
                    BaseHouse _house = mx as BaseHouse;
                    if (_house == null)
                        continue;

                    if (_house != null && _house.Region.Contains(loc))
                    {
                        dst_house = _house;
                    }
                }

                // is the user trying to shoot through a door without LOS?
                if (dst_house != null)
                {
                    if (!caster.InLOS(loc))
                    {
                        RecordCheater.Cheater(caster, "Try to teleport through front door (LOS)", true);
                        return false;
                    }
                }

                // is the user not standing on the ground?
                if (dst_house != null && dst_house.Region != null)
                {
                    if (!CanSpawnLandMobile(caster.Map, caster.Location.X, caster.Location.Y, caster.Location.Z, CanFitFlags.requireSurface))
                    {
                        RecordCheater.Cheater(caster, "Try to teleport over front door (Z Axis)", true);
                        return false;
                    }
                }

            }

            // special housing region/multi exploit - teleport from multi to castle courtyard
            // stop players from teleporting from one multi to another (includes PreviewHouses)
            if (type == TravelCheckType.TeleportTo && caster.AccessLevel != AccessLevel.System)
            {
                // if there is a house at the destination, then..
                //  if all the multis and regions do not match between src and dst, fail
                if (HouseAt(caster.Map, loc) && CheckParity(caster.Map, caster.Location, loc) == false)
                {
                    RecordCheater.Cheater(caster, "Multi to multi teleport exploit.", true);
                    return false;
                }
            }
            #endregion Various Exploits

            #region Dueling
            if (Region.Find(loc, map).IsPartOf(typeof(Engines.ConPVP.SafeZone)))
            {
                PlayerMobile pm = caster as PlayerMobile;

                bool tp = (type == TravelCheckType.TeleportTo || type == TravelCheckType.TeleportFrom);

                if (!tp || pm == null || pm.DuelPlayer == null || pm.DuelPlayer.Eliminated)
                    return false; // we may not travel from/to safe zones - exception: tp from/to while we are dueling
            }
            #endregion

            // okay, now check our locale rules table
            int v = (int)type;
            bool isValid = true;

            for (int i = 0; isValid && i < m_Validators.Length; ++i)
                isValid = (m_Rules[v, i] || !m_Validators[i](map, loc));

            if (!isValid && caster != null && caster.AccessLevel == AccessLevel.System)
            {
                if (type == TravelCheckType.Mark)
                    caster.SendMessage("{0} is a restricted spell, but your godly powers allow you to transcend the laws of Britannia.", type.ToString());
                else
                    caster.SendMessage("{0} this location is restricted, but your godly powers allow you to transcend the laws of Britannia.", type.ToString());
                return true;
            }

            return isValid;
        }

        public static TimeSpan GetDamageDelayForSpell(Spell sp)
        {
            if (!sp.DelayedDamage)
                return TimeSpan.Zero;

            return (Core.RuleSets.AOSRules() ? AosDamageDelay : OldDamageDelay);
        }

        public static bool CheckMulti(Point3D p, Map map)
        {
            return CheckMulti(p, map, true);
        }

        public static bool CheckMulti(Point3D p, Map map, bool houses)
        {
            if (map == null || map == Map.Internal)
                return false;

            Sector sector = map.GetSector(p.X, p.Y);

            foreach (BaseMulti multi in sector.Multis)
            {
                if (multi == null)
                    continue;

                if (multi is BaseHouse)
                {
                    if (houses && ((BaseHouse)multi).IsInside(p, 16))
                        return true;
                }
                else if (multi.Contains(p))
                {
                    return true;
                }
            }

            return false;
        }

        public static void Turn(Mobile from, object to)
        {
            IPoint3D target = to as IPoint3D;

            if (target == null)
                return;

            if (target is Item)
            {
                Item item = (Item)target;

                if (item.RootParent != from)
                    from.Direction = from.GetDirectionTo(item.GetWorldLocation());
            }
            else if (from != target)
            {
                from.Direction = from.GetDirectionTo(target);
            }
        }

        private static TimeSpan CombatHeatDelay = TimeSpan.FromSeconds(30.0);
        private static bool RestrictTravelCombat = true;

        public static bool CheckCombat(Mobile m)
        {
            if (!RestrictTravelCombat)
                return false;

            for (int i = 0; i < m.Aggressed.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)m.Aggressed[i];

                if (info.Defender.Player && (DateTime.UtcNow - info.LastCombatTime) < CombatHeatDelay)
                    return true;
            }

            return false;
        }

        public static bool AdjustField(ref Point3D p, Map map, int height, bool mobsBlock)
        {
            return AdjustField(ref p, map, height, mobsBlock, false);
        }

        public static bool AdjustField(ref Point3D p, Map map, int height, bool mobsBlock, bool ignoreDeadMobiles)
        {
            if (map == null)
                return false;

            for (int offset = 0; offset < 10; ++offset)
            {
                Point3D loc = new Point3D(p.X, p.Y, p.Z - offset);
                Utility.CanFitFlags flags = Utility.CanFitFlags.checkBlocksFit | Utility.CanFitFlags.requireSurface;
                if (mobsBlock) flags |= Utility.CanFitFlags.checkMobiles;
                if (ignoreDeadMobiles) flags |= Utility.CanFitFlags.ignoreDeadMobiles;
                if (Utility.CanFit(map, loc, height, flags))
                {
                    p = loc;
                    return true;
                }
            }

            return false;
        }

        public static void GetSurfaceTop(ref IPoint3D p)
        {
            if (p is Item)
            {
                p = ((Item)p).GetSurfaceTop();
            }
            else if (p is StaticTarget)
            {
                StaticTarget t = (StaticTarget)p;
                int z = t.Z;

                if ((t.Flags & TileFlag.Surface) == 0)
                    z -= TileData.ItemTable[t.ItemID & 0x3FFF].CalcHeight;

                p = new Point3D(t.X, t.Y, z);
            }
        }

        public static bool AddStatOffset(Mobile m, StatType type, int offset, TimeSpan duration)
        {
            if (offset > 0)
                return AddStatBonus(m, m, type, offset, duration);
            else if (offset < 0)
                return AddStatCurse(m, m, type, -offset, duration);

            return true;
        }

        public static bool AddStatBonus(Mobile caster, Mobile target, StatType type)
        {
            return AddStatBonus(caster, target, type, GetOffset(caster, target, type, false), GetDuration(caster, target, false));
        }

        public static bool AddStatBonus(Mobile caster, Mobile target, StatType type, int bonus, TimeSpan duration)
        {
            int offset = bonus;
            string name = String.Format("[Magic] {0} Offset", type);

            StatMod mod = target.GetStatMod(name);

            if (mod != null && mod.Offset < 0)
            {
                target.AddStatMod(new StatMod(type, name, mod.Offset + offset, duration));
                return true;
            }
            else if (mod == null || mod.Offset < offset)
            {
                target.AddStatMod(new StatMod(type, name, offset, duration));
                return true;
            }

            return false;
        }

        public static bool AddStatCurse(Mobile caster, Mobile target, StatType type)
        {
            return AddStatCurse(caster, target, type, GetOffset(caster, target, type, true), GetDuration(caster, target, true));
        }

        public static bool AddStatCurse(Mobile caster, Mobile target, StatType type, int curse, TimeSpan duration)
        {
            int offset = -curse;
            string name = String.Format("[Magic] {0} Offset", type);

            StatMod mod = target.GetStatMod(name);

            if (mod != null && mod.Offset > 0)
            {
                target.AddStatMod(new StatMod(type, name, mod.Offset + offset, duration));
                return true;
            }
            else if (mod == null || mod.Offset > offset)
            {
                target.AddStatMod(new StatMod(type, name, offset, duration));
                return true;
            }

            return false;
        }

        public static TimeSpan GetDuration(Mobile caster, Mobile target, bool harmful)
        {
            if (Core.RuleSets.AOSRules())
                return TimeSpan.FromSeconds(((6 * caster.Skills.EvalInt.Fixed) / 50) + 1);

            double seconds = caster.Skills[SkillName.Magery].Value * 1.2;

            // just a guess...
            if (OldResist && harmful)
                seconds -= target.Skills[SkillName.MagicResist].Value * 0.6;

            return TimeSpan.FromSeconds(seconds);
        }

        private static bool m_DisableSkillCheck;

        public static bool DisableSkillCheck
        {
            get { return m_DisableSkillCheck; }
            set { m_DisableSkillCheck = value; }
        }

        public static int GetOffset(Mobile caster, Mobile target, StatType type, bool curse)
        {
            if (Core.RuleSets.AOSRules())
            {
                if (!m_DisableSkillCheck)
                {
                    // CheckSkill call modified to lower max to 100.
                    caster.CheckSkill(SkillName.EvalInt, 0.0, 100.0, contextObj: new object[2]);

                    if (curse)
                        // CheckSkill call modified to lower max to 100.
                        target.CheckSkill(SkillName.MagicResist, 0.0, 100.0, contextObj: new object[2]);
                }

                double percent;

                if (curse)
                    percent = 8 + (caster.Skills.EvalInt.Fixed / 100) - (target.Skills.MagicResist.Fixed / 100);
                else
                    percent = 1 + (caster.Skills.EvalInt.Fixed / 100);

                percent *= 0.01;

                if (percent < 0)
                    percent = 0;

                switch (type)
                {
                    case StatType.Str: return (int)(target.RawStr * percent);
                    case StatType.Dex: return (int)(target.RawDex * percent);
                    case StatType.Int: return (int)(target.RawInt * percent);
                }
            }

            // cap at 100 so we can have high-end mobs debuf
            double skill = caster.Skills[SkillName.Magery].Value;
            return 1 + (int)((skill > 100.0 ? 100.0 : skill) * 0.1);
        }

        public static Guild GetGuildFor(Mobile m)
        {
            Guild g = m.Guild as Guild;

            if (g == null && m is BaseCreature)
            {
                BaseCreature c = (BaseCreature)m;
                m = c.ControlMaster;

                if (m != null)
                    g = m.Guild as Guild;

                if (g == null)
                {
                    m = c.SummonMaster;

                    if (m != null)
                        g = m.Guild as Guild;
                }
            }

            return g;
        }

        public static bool ValidIndirectTarget(Mobile from, Mobile to)
        {
            #region Static Region
            if (from != null && from.Spell != null && from.Region != to.Region)
            {
                StaticRegion sr = StaticRegion.FindStaticRegion(to);
                if (sr != null && (sr.IsRestrictedSpell(from.Spell) || sr.IsMagicIsolated) && from.AccessLevel == AccessLevel.Player)
                    return false;
            }
            #endregion

            if (to.Hidden && to.AccessLevel > from.AccessLevel)
                return false;

            #region Dueling
            BaseCreature bcFrom = from as BaseCreature;
            PlayerMobile pmFrom = from as PlayerMobile;
            PlayerMobile pmTarg = to as PlayerMobile;

            if (pmFrom == null && bcFrom != null && bcFrom.Summoned)
                pmFrom = bcFrom.SummonMaster as PlayerMobile;

            if (pmTarg == null && to is BaseCreature)
            {
                BaseCreature bcTarg = (BaseCreature)to;

                if (bcTarg.Summoned)
                    pmTarg = bcTarg.SummonMaster as PlayerMobile;
            }

            if (pmFrom != null && pmTarg != null)
            {
                if (pmFrom.DuelContext != null && pmFrom.DuelContext == pmTarg.DuelContext && pmFrom.DuelContext.Started && pmFrom.DuelPlayer != null && pmTarg.DuelPlayer != null)
                    return (pmFrom.DuelPlayer.Participant != pmTarg.DuelPlayer.Participant);
            }
            #endregion

            /*Pix: removed the guild check - fields and explosion pots will affect guildmates now*/
            /*Guild fromGuild = GetGuildFor(from);
            Guild toGuild = GetGuildFor(to);

            if (fromGuild != null && toGuild != null && (fromGuild == toGuild || fromGuild.IsAlly(toGuild)))
                return false;*/

            return true;
        }

        private static int[] m_Offsets = new int[]
            {
                -1, -1,
                -1,  0,
                -1,  1,
                0, -1,
                0,  1,
                1, -1,
                1,  0,
                1,  1
            };

        public static void Summon(BaseCreature creature, Mobile caster, int sound, TimeSpan duration, bool scaleDuration, bool scaleStats)
        {
            Map map = caster.Map;

            if (map == null)
                return;

            double scale = 1.0 + ((caster.Skills[SkillName.Magery].Value - 100.0) / 200.0);

            if (scaleDuration)
                duration = TimeSpan.FromSeconds(duration.TotalSeconds * scale);

            if (scaleStats)
            {
                creature.RawStr = (int)(creature.RawStr * scale);
                creature.Hits = creature.HitsMax;

                creature.RawDex = (int)(creature.RawDex * scale);
                creature.Stam = creature.StamMax;

                creature.RawInt = (int)(creature.RawInt * scale);
                creature.Mana = creature.ManaMax;
            }

            int offset = Utility.Random(8) * 2;

            for (int i = 0; i < m_Offsets.Length; i += 2)
            {
                int x = caster.X + m_Offsets[(offset + i) % m_Offsets.Length];
                int y = caster.Y + m_Offsets[(offset + i + 1) % m_Offsets.Length];

                if (map.CanSpawnLandMobile(x, y, caster.Z))
                {
                    BaseCreature.Summon(creature, caster, new Point3D(x, y, caster.Z), sound, duration);
                    return;
                }
                else
                {
                    int z = map.GetAverageZ(x, y);

                    if (map.CanSpawnLandMobile(x, y, z))
                    {
                        BaseCreature.Summon(creature, caster, new Point3D(x, y, z), sound, duration);
                        return;
                    }
                }
            }

            creature.Delete();
            caster.SendLocalizedMessage(501942); // That location is blocked.
        }

        public static bool FindValidSpawnLocation(Map map, ref Point3D p, bool surroundingsOnly)
        {
            if (map == null)       //sanity
                return false;

            if (!surroundingsOnly)
            {
                if (map.CanSpawnLandMobile(p))   //p's fine.
                {
                    p = new Point3D(p);
                    return true;
                }

                int z = map.GetAverageZ(p.X, p.Y);

                if (map.CanSpawnLandMobile(p.X, p.Y, z))
                {
                    p = new Point3D(p.X, p.Y, z);
                    return true;
                }
            }

            int offset = Utility.Random(8) * 2;

            for (int i = 0; i < m_Offsets.Length; i += 2)
            {
                int x = p.X + m_Offsets[(offset + i) % m_Offsets.Length];
                int y = p.Y + m_Offsets[(offset + i + 1) % m_Offsets.Length];

                if (map.CanSpawnLandMobile(x, y, p.Z))
                {
                    p = new Point3D(x, y, p.Z);
                    return true;
                }
                else
                {
                    int z = map.GetAverageZ(x, y);

                    if (map.CanSpawnLandMobile(x, y, z))
                    {
                        p = new Point3D(x, y, z);
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsSpecialRegion(Point3D location)
        {
            Region region = Server.Region.Find(location, Map.Felucca);
            if (region != null)
            {
                return region.IsJailRules || region.IsAngelIslandRules || region.IsGreenAcresRules;
            }
            return false;
        }
        public static bool IsNPC(Mobile caster)
        {
            return caster != null && !caster.Player;
        }
        public static void SendInvalidMessage(Mobile caster, TravelCheckType type)
        {
            if (type == TravelCheckType.RecallTo || type == TravelCheckType.GateTo)
                caster.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            else if (type == TravelCheckType.TeleportTo)
                caster.SendLocalizedMessage(501035); // You cannot teleport from here to the destination.
            else
                caster.SendLocalizedMessage(501802); // Thy spell doth not appear to work...
        }

        public static bool iStack(Mobile from, Point3D loc, int limit)
        {
            if (from != null && from.Map != null && from.Map != Map.Internal)
            {
                // if we are teleporting onto a stack of movable items, and at least 3 reside on different Z planes
                //  then it looks too much like an exploit to allow it.
                IPooledEnumerable to_list = from.Map.GetItemsInRange(loc, 0);
                int AverageZ = from.Map.GetAverageZ(loc.X, loc.Y);
                if (loc.Z > AverageZ)
                {   // only run this test if they are targeting high in the stack
                    Dictionary<int, int> bucket = new Dictionary<int, int>();
                    int different_z = 0;
                    foreach (Item ix in to_list)
                    {
                        if (ix is Item && ix.Movable == true)
                        {   // if our bucket does NOT contain the key, then it is at a different Z
                            if (bucket.ContainsKey(ix.Z) == false)
                            {
                                bucket[ix.Z] = ix.Z;
                                if (++different_z >= limit)
                                    break;
                            }
                        }
                    }

                    if (different_z >= limit)
                    {
                        to_list.Free();
                        return false;
                    }
                }

                to_list.Free();
                return true;
            }
            else
                return true;
        }

        public static bool HouseAt(Map map, Point3D pt)
        {
            if (map == null)
                return false;

            try
            {
                ArrayList reglist = Region.FindAll(pt, map);
                foreach (Region rx in reglist)
                    if (rx is HouseRegion)
                        return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return false;
            }

            return false;
        }

        public static bool PreviewHouseAt(Map map, Point3D pt)
        {
            if (map == null)
                return false;

            try
            {
                ArrayList Multlist = BaseMulti.FindAll(pt, map);
                foreach (BaseMulti rx in Multlist)
                    if (rx is PreviewHouse)
                        return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return false;
            }

            return false;
        }

        public static bool CheckParity(Map map, Point3D src, Point3D dst)
        {
            if (map == null)
                return false;

            try
            {
                ArrayList srcReglist = Region.FindAll(src, map);
                ArrayList dstReglist = Region.FindAll(dst, map);
                ArrayList srcMultlist = BaseMulti.FindAll(src, map);
                ArrayList dstMultlist = BaseMulti.FindAll(dst, map);
                //return srcReglist == dstReglist && srcMultlist == dstMultlist;
                bool size = srcReglist.Count == dstReglist.Count && srcMultlist.Count == dstMultlist.Count;
                bool contents = true;

                foreach (object ix in srcReglist)
                    if (dstReglist.Contains(ix) == false)
                    {
                        contents = false;
                        break;
                    }

                if (contents == true)
                    foreach (object mx in srcMultlist)
                        if (dstMultlist.Contains(mx) == false)
                        {
                            contents = false;
                            break;
                        }


                return size && contents;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return false;
            }
        }

        public static bool IsWindLoc(Point3D loc)
        {
            int x = loc.X, y = loc.Y;

            return (x >= 5120 && y >= 0 && x < 5376 && y < 256);
        }

        public static bool IsFeluccaWind(Map map, Point3D loc)
        {
            return (map == Map.Felucca && IsWindLoc(loc));
        }

        public static bool IsTrammelWind(Map map, Point3D loc)
        {
            return (map == Map.Trammel && IsWindLoc(loc));
        }

        public static bool IsIlshenar(Map map, Point3D loc)
        {
            return (map == Map.Ilshenar);
        }

        public static bool IsSolenHiveLoc(Map map, Point3D loc)
        {
            int x = loc.X, y = loc.Y;

            return (map == Map.Felucca && x >= 5640 && y >= 1776 && x < 5935 && y < 2039);
        }

        public static bool IsTrammelSolenHive(Map map, Point3D loc)
        {
            return (map == Map.Trammel && IsSolenHiveLoc(map, loc));
        }

        public static bool IsFeluccaSolenHive(Map map, Point3D loc)
        {
            return (map == Map.Felucca && IsSolenHiveLoc(map, loc));
        }

        public static bool IsFeluccaT2A(Map map, Point3D loc)
        {
            int x = loc.X, y = loc.Y;

            return (map == Map.Felucca && x >= 5120 && y >= 2304 && x < 6144 && y < 4096);
        }

        public static bool IsFeluccaDungeon(Map map, Point3D loc)
        {
            if (map == Map.Felucca)
            {
                Region r = Region.Find(loc, map);
                if (r != null && r.IsDungeonRules)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public static bool IsCrystalCave(Map map, Point3D loc)
        {
            if (map != Map.Malas)
                return false;

            int x = loc.X, y = loc.Y, z = loc.Z;

            bool r1 = (x >= 1182 && y >= 437 && x < 1211 && y < 470);
            bool r2 = (x >= 1156 && y >= 470 && x < 1211 && y < 503);
            bool r3 = (x >= 1176 && y >= 503 && x < 1208 && y < 509);
            bool r4 = (x >= 1188 && y >= 509 && x < 1201 && y < 513);

            return (z < -80 && (r1 || r2 || r3 || r4));
        }

        public static bool IsFactionStronghold(Map map, Point3D loc)
        {
            /*// Teleporting is allowed, but only for faction members
            if ( !Core.AOS && m_TravelCaster != null && (m_TravelType == TravelCheckType.TeleportTo || m_TravelType == TravelCheckType.TeleportFrom) )
            {
                if ( Factions.Faction.Find( m_TravelCaster, true, true ) != null )
                    return false;
            }*/

            return (Region.Find(loc, map).IsPartOf(typeof(Factions.StrongholdRegion)));
        }

        public static bool IsDoomFerry(Map map, Point3D loc)
        {
            if (map != Map.Malas)
                return false;

            int x = loc.X, y = loc.Y;

            if (x >= 426 && y >= 314 && x <= 430 && y <= 331)
                return true;

            if (x >= 406 && y >= 247 && x <= 410 && y <= 264)
                return true;

            return false;
        }

        public static bool IsDoomGauntlet(Map map, Point3D loc)
        {
            if (map != Map.Malas)
                return false;

            int x = loc.X - 256, y = loc.Y - 304;

            return (x >= 0 && y >= 0 && x < 256 && y < 256);
        }

        public static bool IsKinArea(Map map, Point3D loc)
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                int x = loc.X, y = loc.Y;

                bool r1 = (x >= 621 && y >= 1473 && x < 679 && y < 1510);   // yew orc fort			(orc) - ALL
                bool r2 = (x >= 937 && y >= 682 && x < 969 && y < 720);     // crypts stronghold	(savage) - ransom chest building
                bool r3 = (x >= 2565 && y >= 2198 && x < 2603 && y < 2248); // bucs stronghold		(pirate) - ransom chest ship
                bool r4 = (x >= 2873 && y >= 3405 && x < 2922 && y < 3426); // serps stronghold		(brigand) - ransom chest building
                bool r5 = (x >= 2946 && y >= 800 && x < 3048 && y < 859);   // vesper stronghold	(good) - Entire island plus docks that ransom chests sits on

                return (map == Map.Felucca && (r1 || r2 || r3 || r4 || r5));
            }
            else
                // no kin areas on siege etc.
                return false;
        }

        public static bool IsYewOrcFort(Map map, Point3D loc)
        {
            if (Core.RuleSets.AngelIslandRules())
            {   // handled by IsKinArea
                return false;
            }
            else
            {
                int x = loc.X, y = loc.Y;
                // orc fort area
                return (x >= 621 && y >= 1473 && x < 679 && y < 1510);
            }
        }

        public static bool IsHedgeMaze(Map map, Point3D loc)
        {
            int x = loc.X, y = loc.Y;

            bool r1 = (x >= 1030 && y >= 2157 && x < 1258 && y < 2305); // hedge maze

            return (map == Map.Felucca && (r1));
        }

        public static bool IsAngelIsland(Map map, Point3D loc)
        {
            int x = loc.X, y = loc.Y;

            bool r1 = (x >= 150 && y >= 700 && x < 400 && y < 860);     // Angel Island

            return (map == Map.Felucca && (r1));
        }

        public static bool IsZooCage(Map map, Point3D loc)
        {
            int x = loc.X, y = loc.Y;

            bool r01 = (x >= 4480 && y >= 1376 && x < 4491 && y < 1398);
            bool r02 = (x >= 4493 && y >= 1390 && x < 4504 && y < 1398);
            bool r03 = (x >= 4506 && y >= 1390 && x < 4517 && y < 1398);
            bool r04 = (x >= 4525 && y >= 1383 && x < 4535 && y < 1391);
            bool r05 = (x >= 4527 && y >= 1354 && x < 4535 && y < 1373);
            bool r06 = (x >= 4514 && y >= 1354 && x < 4525 && y < 1362);
            bool r07 = (x >= 4501 && y >= 1354 && x < 4512 && y < 1362);
            bool r08 = (x >= 4488 && y >= 1354 && x < 4499 && y < 1374);
            bool r09 = (x >= 4498 && y >= 1381 && x < 4503 && y < 1384);
            bool r10 = (x >= 4506 && y >= 1377 && x < 4518 && y < 1384);
            bool r11 = (x >= 4506 && y >= 1369 && x < 4518 && y < 1374);

            return (map == Map.Felucca &&
                (r01 || r02 || r03 || r04 || r05 || r06 || r07 || r08 || r09 || r10 || r11));
        }

        public static bool IsInvalid(Map map, Point3D loc)
        {
            if (map == null || map == Map.Internal)
                return true;

            int x = loc.X, y = loc.Y;

            return (x < 0 || y < 0 || x >= map.Width || y >= map.Height);
        }

        //towns
        public static bool IsTown(IPoint3D loc, Mobile caster)
        {
            if (loc is Item)
                loc = ((Item)loc).GetWorldLocation();

            return IsTown(new Point3D(loc), caster);
        }

        public static bool IsTown(Point3D loc, Mobile caster)
        {
            Map map = caster.Map;

            if (map == null)
                return false;

            #region Dueling
            Engines.ConPVP.SafeZone sz = (Engines.ConPVP.SafeZone)Region.Find(loc, map).GetRegion(typeof(Engines.ConPVP.SafeZone));

            if (sz != null)
            {
                PlayerMobile pm = (PlayerMobile)caster;

                if (pm == null || pm.DuelContext == null || !pm.DuelContext.Started || pm.DuelPlayer == null || pm.DuelPlayer.Eliminated)
                    return true;
            }
            #endregion

            GuardedRegion reg = Region.Find(loc, map) as GuardedRegion;

            return (reg != null && reg.IsGuarded);
        }

        public static bool CheckTown(IPoint3D loc, Mobile caster)
        {
            if (loc is Item)
                loc = ((Item)loc).GetWorldLocation();

            return CheckTown(new Point3D(loc), caster);
        }

        public static bool CheckTown(Point3D loc, Mobile caster)
        {
            #region Static Region
            if (caster != null && caster.Spell != null && caster.Region != Region.Find(loc, caster.Map))
            {
                StaticRegion sr = StaticRegion.FindStaticRegion(loc, caster.Map);
                if (sr != null && (sr.IsRestrictedSpell(caster.Spell) || sr.IsMagicIsolated) && caster.AccessLevel == AccessLevel.Player)
                    return false;
            }
            #endregion

            if (IsTown(loc, caster))
            {
                caster.SendLocalizedMessage(500946); // You cannot cast this in town!
                return false;
            }

            return true;
        }

        //magic reflection
        public static void CheckReflect(int circle, Mobile caster, ref Mobile target)
        {
            CheckReflect(circle, ref caster, ref target);
        }

        public static void CheckReflect(int circle, ref Mobile caster, ref Mobile target)
        {
            if (target.MagicDamageAbsorb > 0)
            {
                ++circle;

                target.MagicDamageAbsorb -= circle;

                // This order isn't very intuitive, but you have to nullify reflect before target gets switched

                bool reflect = (target.MagicDamageAbsorb >= 0);

                if (target is BaseCreature)
                    ((BaseCreature)target).CheckReflect(caster, ref reflect);

                if (target.MagicDamageAbsorb <= 0)
                {
                    target.MagicDamageAbsorb = 0;
                    DefensiveSpell.Nullify(target);
                }

                if (reflect)
                {
                    target.FixedEffect(0x37B9, 10, 5);

                    Mobile temp = caster;
                    caster = target;
                    target = temp;
                }
            }
            else if (target is BaseCreature)
            {
                bool reflect = false;

                ((BaseCreature)target).CheckReflect(caster, ref reflect);

                if (reflect)
                {
                    target.FixedEffect(0x37B9, 10, 5);

                    Mobile temp = caster;
                    caster = target;
                    target = temp;
                }
            }
        }

        public static void Damage(Spell spell, Mobile target, double damage)
        {
            TimeSpan ts = GetDamageDelayForSpell(spell);

            Damage(ts, target, spell.Caster, damage);
        }

        public static void Damage(TimeSpan delay, Mobile target, double damage)
        {
            Damage(delay, target, null, damage);
        }

        public static void Damage(TimeSpan delay, Mobile target, Mobile from, double damage)
        {
            double Moddamage = damage;
            ISpell i = from.Spell;
            if (i != null && target is BaseCreature)
            {
                Spell s = (Spell)i;
                ((BaseCreature)target).CheckSpellImmunity(s.DamageType, Moddamage, out Moddamage);
                //Console.WriteLine("Old Damage {0}, new Damage {1}",damage,Moddamage);
            }

            if (delay == TimeSpan.Zero)
                target.Damage((int)Moddamage, from, from);
            else
                new SpellDamageTimer(target, from, (int)Moddamage, delay).Start();

            if (target is BaseCreature && from != null && delay == TimeSpan.Zero)
                ((BaseCreature)target).OnDamagedBySpell(from);
        }

        public static void Damage(Spell spell, Mobile target, double damage, int phys, int fire, int cold, int pois, int nrgy)
        {
            TimeSpan ts = GetDamageDelayForSpell(spell);

            Damage(ts, target, spell.Caster, damage, phys, fire, cold, pois, nrgy);
        }

        public static void Damage(Spell spell, Mobile target, double damage, int phys, int fire, int cold, int pois, int nrgy, DFAlgorithm dfa)
        {
            TimeSpan ts = GetDamageDelayForSpell(spell);

            Damage(ts, target, spell.Caster, damage, phys, fire, cold, pois, nrgy, dfa);
        }

        public static void Damage(TimeSpan delay, Mobile target, double damage, int phys, int fire, int cold, int pois, int nrgy)
        {
            Damage(delay, target, null, damage, phys, fire, cold, pois, nrgy);
        }

        public static void Damage(TimeSpan delay, Mobile target, Mobile from, double damage, int phys, int fire, int cold, int pois, int nrgy)
        {
            Damage(delay, target, from, damage, phys, fire, cold, pois, nrgy, DFAlgorithm.Standard);
        }

        public static void Damage(TimeSpan delay, Mobile target, Mobile from, double damage, int phys, int fire, int cold, int pois, int nrgy, DFAlgorithm dfa)
        {
            double Moddamage = damage;
            ISpell i = from.Spell;
            if (i != null && target is BaseCreature)
            {
                Spell s = (Spell)i;
                ((BaseCreature)target).CheckSpellImmunity(s.DamageType, Moddamage, out Moddamage);
                //Console.WriteLine("Old Damage {0}, new Damage {1}",damage,Moddamage);
            }
            if (delay == TimeSpan.Zero)
            {
                WeightOverloading.DFA = dfa;
                AOS.Damage(target, from, (int)Moddamage, phys, fire, cold, pois, nrgy, from);
                WeightOverloading.DFA = DFAlgorithm.Standard;
            }
            else
            {
                new SpellDamageTimerAOS(target, from, (int)Moddamage, phys, fire, cold, pois, nrgy, delay, dfa).Start();
            }

            if (target is BaseCreature && from != null && delay == TimeSpan.Zero)
                ((BaseCreature)target).OnDamagedBySpell(from);
        }

        private class SpellDamageTimer : Timer
        {
            private Mobile m_Target, m_From;
            private int m_Damage;

            public SpellDamageTimer(Mobile target, Mobile from, int damage, TimeSpan delay)
                : base(delay)
            {
                m_Target = target;
                m_From = from;
                m_Damage = damage;

                Priority = TimerPriority.TwentyFiveMS;
            }

            protected override void OnTick()
            {
                m_Target.Damage(m_Damage, m_From);
            }
        }

        private class SpellDamageTimerAOS : Timer
        {
            private Mobile m_Target, m_From;
            private int m_Damage;
            private int m_Phys, m_Fire, m_Cold, m_Pois, m_Nrgy;
            private DFAlgorithm m_DFA;

            public SpellDamageTimerAOS(Mobile target, Mobile from, int damage, int phys, int fire, int cold, int pois, int nrgy, TimeSpan delay, DFAlgorithm dfa)
                : base(delay)
            {
                m_Target = target;
                m_From = from;
                m_Damage = damage;
                m_Phys = phys;
                m_Fire = fire;
                m_Cold = cold;
                m_Pois = pois;
                m_Nrgy = nrgy;
                m_DFA = dfa;

                Priority = TimerPriority.TwentyFiveMS;
            }

            protected override void OnTick()
            {
                WeightOverloading.DFA = m_DFA;
                AOS.Damage(m_Target, m_From, m_Damage, m_Phys, m_Fire, m_Cold, m_Pois, m_Nrgy, m_From);
                WeightOverloading.DFA = DFAlgorithm.Standard;

                if (m_Target is BaseCreature && m_From != null)
                    ((BaseCreature)m_Target).OnDamagedBySpell(m_From);
            }
        }
    }

    public class TransformationSpellHelper
    {
        #region Context Stuff
        private static Dictionary<Mobile, TransformContext> m_Table = new Dictionary<Mobile, TransformContext>();

        public static void AddContext(Mobile m, TransformContext context)
        {
            m_Table[m] = context;
        }

        public static void RemoveContext(Mobile m, bool resetGraphics)
        {
            TransformContext context = GetContext(m);

            if (context != null)
                RemoveContext(m, context, resetGraphics);
        }

        public static void RemoveContext(Mobile m, TransformContext context, bool resetGraphics)
        {
            if (m_Table.ContainsKey(m))
            {
                m_Table.Remove(m);

                List<ResistanceMod> mods = context.Mods;

                for (int i = 0; i < mods.Count; ++i)
                    m.RemoveResistanceMod(mods[i]);

                if (resetGraphics)
                {
                    m.HueMod = -1;
                    m.BodyMod = 0;
                }

                context.Timer.Stop();
                context.Spell.RemoveEffect(m);
            }
        }

        public static TransformContext GetContext(Mobile m)
        {
            TransformContext context = null;

            m_Table.TryGetValue(m, out context);

            return context;
        }

        public static bool UnderTransformation(Mobile m)
        {
            return (GetContext(m) != null);
        }

        public static bool UnderTransformation(Mobile m, Type type)
        {
            TransformContext context = GetContext(m);

            return (context != null && context.Type == type);
        }
        #endregion

        public static bool CheckCast(Mobile caster, Spell spell)
        {
            if (Factions.Sigil.ExistsOn(caster))
            {
                caster.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                return false;
            }
            else if (Engines.Alignment.TheFlag.ExistsOn(caster))
            {
                caster.SendMessage("You can't do that while carrying the flag.");
                return false;
            }
            else if (!caster.CanBeginAction(typeof(PolymorphSpell)))
            {
                caster.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
                return false;
            }
            //else if (false /*AnimalForm.UnderTransformation(caster) Ninja stuff*/)
            //{
            //caster.SendLocalizedMessage(1061091); // You cannot cast that spell in this form.
            //return false;
            //}

            return true;
        }

        public static bool OnCast(Mobile caster, Spell spell)
        {
            ITransformationSpell transformSpell = spell as ITransformationSpell;

            if (transformSpell == null)
                return false;

            if (Factions.Sigil.ExistsOn(caster))
            {
                caster.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
            }
            else if (Engines.Alignment.TheFlag.ExistsOn(caster))
            {
                caster.SendMessage("You can't do that while carrying the flag.");
            }
            else if (!caster.CanBeginAction(typeof(PolymorphSpell)))
            {
                caster.SendLocalizedMessage(1061628); // You can't do that while polymorphed.
            }
            //else if (false /*AnimalForm.UnderTransformation(caster) Ninja stuff*/)
            //{
            //caster.SendLocalizedMessage(1061091); // You cannot cast that spell in this form.
            //}
            else if (!caster.CanBeginAction(typeof(IncognitoSpell)) || (caster.IsBodyMod && GetContext(caster) == null))
            {
                spell.DoFizzle();
            }
            else if (spell.CheckSequence())
            {
                TransformContext context = GetContext(caster);
                Type ourType = spell.GetType();

                bool wasTransformed = (context != null);
                bool ourTransform = (wasTransformed && context.Type == ourType);

                if (wasTransformed)
                {
                    RemoveContext(caster, context, ourTransform);

                    if (ourTransform)
                    {
                        caster.PlaySound(0xFA);
                        caster.FixedParticles(0x3728, 1, 13, 5042, EffectLayer.Waist);
                    }
                }

                if (!ourTransform)
                {
                    List<ResistanceMod> mods = new List<ResistanceMod>();

                    if (transformSpell.PhysResistOffset != 0)
                        mods.Add(new ResistanceMod(ResistanceType.Physical, transformSpell.PhysResistOffset));

                    if (transformSpell.FireResistOffset != 0)
                        mods.Add(new ResistanceMod(ResistanceType.Fire, transformSpell.FireResistOffset));

                    if (transformSpell.ColdResistOffset != 0)
                        mods.Add(new ResistanceMod(ResistanceType.Cold, transformSpell.ColdResistOffset));

                    if (transformSpell.PoisResistOffset != 0)
                        mods.Add(new ResistanceMod(ResistanceType.Poison, transformSpell.PoisResistOffset));

                    if (transformSpell.NrgyResistOffset != 0)
                        mods.Add(new ResistanceMod(ResistanceType.Energy, transformSpell.NrgyResistOffset));

                    if (!((Body)transformSpell.Body).IsHuman)
                    {
                        Mobiles.IMount mt = caster.Mount;

                        if (mt != null)
                            mt.Rider = null;
                    }

                    caster.BodyMod = transformSpell.Body;
                    caster.HueMod = transformSpell.Hue;

                    for (int i = 0; i < mods.Count; ++i)
                        caster.AddResistanceMod(mods[i]);

                    transformSpell.DoEffect(caster);

                    Timer timer = new TransformTimer(caster, transformSpell);
                    timer.Start();

                    AddContext(caster, new TransformContext(timer, mods, ourType, transformSpell));
                    return true;
                }
            }

            return false;
        }
    }

    public interface ITransformationSpell
    {
        int Body { get; }
        int Hue { get; }

        int PhysResistOffset { get; }
        int FireResistOffset { get; }
        int ColdResistOffset { get; }
        int PoisResistOffset { get; }
        int NrgyResistOffset { get; }

        double TickRate { get; }
        void OnTick(Mobile m);

        void DoEffect(Mobile m);
        void RemoveEffect(Mobile m);
    }


    public class TransformContext
    {
        private Timer m_Timer;
        private List<ResistanceMod> m_Mods;
        private Type m_Type;
        private ITransformationSpell m_Spell;

        public Timer Timer { get { return m_Timer; } }
        public List<ResistanceMod> Mods { get { return m_Mods; } }
        public Type Type { get { return m_Type; } }
        public ITransformationSpell Spell { get { return m_Spell; } }

        public TransformContext(Timer timer, List<ResistanceMod> mods, Type type, ITransformationSpell spell)
        {
            m_Timer = timer;
            m_Mods = mods;
            m_Type = type;
            m_Spell = spell;
        }
    }

    public class TransformTimer : Timer
    {
        private Mobile m_Mobile;
        private ITransformationSpell m_Spell;

        public TransformTimer(Mobile from, ITransformationSpell spell)
            : base(TimeSpan.FromSeconds(spell.TickRate), TimeSpan.FromSeconds(spell.TickRate))
        {
            m_Mobile = from;
            m_Spell = spell;

            Priority = TimerPriority.TwoFiftyMS;
        }

        protected override void OnTick()
        {
            if (m_Mobile.Deleted || !m_Mobile.Alive || m_Mobile.Body != m_Spell.Body || m_Mobile.Hue != m_Spell.Hue)
            {
                TransformationSpellHelper.RemoveContext(m_Mobile, true);
                Stop();
            }
            else
            {
                m_Spell.OnTick(m_Mobile);
            }
        }
    }
}