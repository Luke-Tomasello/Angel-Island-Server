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

/* Scripts/Engines/DRDT/CustomRegion.cs
 * CHANGELOG:
 *  9/15/22, Yoar
 *      Moved all functionality to parent class StaticRegion
 *  9/11/22, Yoar
 *      Added [GetRegion command
 *  9/11/22, Yoar (Custom Region Overhaul)
 *      Completely overhauled custom region system
 *  11/22/21, Adam (CheckVendorAccess)
 *      Override CheckVendorAccess and let the controller set whether players can use vendors
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	05/14/09, plasma
 *	add base OnSpeech() call in as was preventing players calling the guards!
 *	4/9/09, Adam
 *		Add missing FindDRDTRegion(Item from)
 *	6/26/08, weaver
 *		Don't make sparklies for anyone of higher access when moving into an isolated region...
 *	6/24/08, weaver
 *		Optimized isolation checks (added extra playermobile checks).
 *	6/23/08, weaver
 *		Fixed isolated regions so that all mobiles entering a region are made visible.
 *		Added sparklies to disappearing mobiles.
 *		Added sound effect on enter/exit of isolated region when mobiles disappear or appear.
 *	1/27/08, Adam
 *		Replace Dungeon prop with the IsDungeonRules property that uses 'flags'
 *	07/28/07, Adam
 *		Add support for Custom Regions that are a HousingRegion superset
 *	02/03/07, Pix
 *		Fixed FindDRDTRegion to just check on Point.Zero - the other checks didn't work.
 *		Also, fixed order of one combined null-checking.
 *		(Future use) Changed protection level of m_Controller to protected (from private) so subclasses could access.
 * 02/02/07, Kit
 *      Additional sanity checking to FindDRDTRegion still throwing exceptions here and there.
 *      Added deleted and point3d/map bounds checks
 *      Made FindDRDTRegion(Mobile) and FindDRDTRegion(Item) use common FindDRDTRegion(Map, Point3d)
 * 12/31/06, Kit
 *      Additional sanity checking to FindDRDTRegion still throwing exceptions here and there.
 *      Updated IsrestrictedSpell/Skill checks to pass mobile for invalid packet range logging.
 * 10/30/06, Pix
 *		Added protection for crash relating to BitArray index.
 *  9/02/06, Kit
 *		Added additional checking to FINDDRDTRegion(Item).
 *  8/27/06, Kit
 *		Added additional null checks to FindDRDTRegion for incase of sector retrival fail or invalid location.
 *  8/19/06, Kit
 *		Changed OnEnter/Exit Isolation functionality to handle the hiding of items/multis.
 *  7/29/06, Kit
 *		Added EquipItem region override that checks RestrictedTypes list. Added RestrictedType list
 *		checking to OnDoubleClick().
 *  6/26/06, Kit
 *		Addec checks to OnEnter/OnExit so that if OverrideMaxFollowers is set players entering
 *		will have there MaxFollowers rateing adjusted. 
 *  6/25/06, Kit
 *		Added checks for RestrictCreatureMagic and for useing non-generic Magic fail msg if
 *		MagicFailureMsg is set.
 *  6/24/06, Kit
 *		Added overload FindDRDTRegion that now accepts a point3d location.
 *	6/15/06, Pix
 *		Overrode new IsNoMurderZone property so that the server.exe could tell whether the region is a NoMurderZone.
 *	6/11/06, Pix
 *		Added warning on enter if the area is a No Count Zone
 *  5/02/06, Kit
 *		Added Check to OnEnter/OnExit to play/stop music if enabled and music track.
 *	30/4/06, weaver
 *		Added OnEnter() and OnExit() code to refresh region isolated mobile visibility (and invisibility...)
 *	2/3/06, Pix
 *		Enter message uses RegionName instead of Name. :D
 *	2/3/06, Pix
 *		Now the IOB attack message uses the region controller's name property when 
 *		announcing attackers.
 *	10/06/05, Pix
 *		Removed extraneous 's' on IOB message.
 *	10/04/05, Pix
 *		Changed OnEnter for to use new GetIOBName() function.
 *	9/20/05, Pix
 *		Fixed the enter messages for the Good IOB
 *	7/29/05, Pix
 *		Fixed grammar mistake: orc's vs orcs
 *  05/05/05, Kit
 *	Added fix for iob msgs sending for None alignment
 *  05/03/05, Kit
 *	Added FindDRDTRegion() to return any drdt's regions at the mobiles position even if in another higher priority region.
 *	Added GetLogOutDelay for dealing with inns'
 *  05/02/05, Kit
 *	Added toggle for IOB zone messages
 *  04/30/05, Kit
 *	Added IOB Support and kin messages when opposeing kin enter a opposeing iob zone.
 *  04/29/05, Kitaras
 *	 Initial system
 */

using Server.Items;
using Server.Mobiles;

namespace Server.Regions
{
    public class CustomRegion : StaticRegion
    {
        public override bool IsDynamicRegion { get { return true; } }

        private CustomRegionControl m_Controller; // may never be null

        // unsettable - only serves as a link to the controller
        [CommandProperty(AccessLevel.GameMaster)]
        public new CustomRegionControl Controller
        {
            get { return m_Controller; }
            set { }
        }

        public CustomRegion(CustomRegionControl rc)
            : base("", "Custom Region", null, typeof(WarriorGuard))
        {
            m_Controller = rc;
        }

        public override void OnEnter(Mobile m)
        {
            m_Controller.OnEnter(m);
            base.OnEnter(m);
        }

        public override void OnExit(Mobile m)
        {
            m_Controller.OnExit(m);
            base.OnExit(m);
        }

        public override void OnRegionRegistrationChanged()
        {
            m_Controller.UpdateHue();
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
        }
    }
}