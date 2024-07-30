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

/* Scripts/Engines/DRDT/CustomRegionControl.cs
 * CHANGELOG:
 *  1/3//23, Yoar
 *      Changed default ItemID to 0xBF6
 *  9/22/22, Yoar
 *      Readded Registered getter/setter
 *  9/19/22, Yoar
 *      Now keeping a list of CustomRegionControl instances
 *  9/19/22, Yoar
 *      Renamed to "CustomRegionControl"
 *      CustomRegionControl now derives from StaticRegionControl
 *  9/15/22, Yoar
 *      Moved all functionality to parent class StaticRegion
 *  9/13/22, Yoar
 *      Removed calls to CloseGump so that we may view multiple region controllers
 *      at the same time
 *  9/11/22, Yoar (Custom Region Overhaul)
 *      Completely overhauled custom region system
 *  4/14/22, Yoar
 *      Removed Name override. Let staff do what they want :).
 *  11/22/21, Adam (CheckVendorAccess)
 *      Override CheckVendorAccess in custome region and let the controller set whether players can use vendors
 *  9/19/21, Adam
 *      1) Add OnSingleClick handling (now displays the RegionName)
 *      2) On Deserialize, patch all Name properties.
 *      The Name of the region controller is "Region Controller", not the name of the region.
 *      3) I also override Name to disallow setting of the name by exposing no [CommandProperty]
 *      Staff were confused and not knowing which value to set (Name and/or RegionName)
 *      In many cases, both were set, and often not matching, yet similar.
 *      4) Auto hue the region controller red if no murder counts can be given, green otherwise.
 *	10/13/10, Adam
 *		Begin adding the sensory system 
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	05/07/09, plasma
 *		Add explict FALSE AllowGate and AllowRecall flags for FeluccaDungeons.
 *	04/26/09, plasma
 *		Added "Moongates" to the IsGuardedRegion method
 *		Fixed copy/paste error, put "keep" on the end of "terra" :)
 *		Added new custom region priority, GreenAcres 0x1
 *	04/24/09, plasma
 *		Add support for reading / writing 3d rects
 *	04/23/09, plasma
 *		- Added the ability to load/upgrade from a region in the XML file									 
 *		- Refactored the clone prop into method so it can be called from elsewhere
 *		- Added cloning of 3D regions as well as 2D regions
 *		- Created new methods that will determine if a region being cloned should be guarded / dungeon stuff in the case
 *			it was loaded from XML and therefore just a base "Region"
 *	4/9/09, Adam
 *		Add BlockLooting rule for the region
 *	6/30/08, Adam
 *		Added a 'fixup' in Deserialize for the m_RestrictedSkills and m_RestrictedSpells BitArrays if the underlying Tables have changed.
 *	2/24/08, plasma
 *		Added factions Capture Area
 *	1/27/08, Adam
 *		- Add the following Region Priorities: MoongateHouseblockers, TownPriorityLow
 *		- Add and/or serialize the following variables: Map m_TargetMap, m_GoLocation, m_MinZ, m_MaxZ,
 *		- Use the Items SendMessage for sending status to staf
 *		- Convert IsDungeonRules from a bool to a bitflag
 *  12/03/07, plasma
 *    Finish off custom region cloning from both custom and normal regions 
 *	7/28/07, Adam
 *		- Add GhostBlindness property so that regions can override ghost blindness for special
 *		events.
 *		- Add support for Custom Regions that are a HousingRegion superset
 *	01/11/07, Pix
 *		Changes so we can subclass RegionControl.
 *  01/08/07, Kit
 *      Final exception handling protection.
 *  01/07/07, Kit
 *      Added specific protection into IsRestrictedSpell/Skill
 *	01/05/07 - Pix
 *		Added protection around IsRestrictedSpell() and IsRestrictedSkill().
 *		Added Exception logging to try/catches.
 * 11/06/06, Kit
 *		Added Enabled/Disabled bool for controller.
 * 10/30/06, Pix
 *		Added protection for crash relating to BitArray index.
 *  8/19/06, Kit
 *		Added NoExternalHarmful, if set to true, prevent any harmful attacks from out of region.
 *  7/29/06, Kit
 *		Added ArrayList RestrictedTypes for use of blocking equiping or use of any types specified.
 *  6/26/06, Kit
 *		Added AllowTravelSpellsInRegion flag and OverrideMaxFollowers flag and int for followers number change.
 *  6/25/06, Kit
 *		Added RestrictCreatureMagic flag and MagicMsgFailure string for overrideing default cant cast msg.
 *  6/24/06, Kit
 *		Added IsMagicIsolated flag to prevent the casting of any spells into the area from outside.
 *  5/02/06, Kit
 *		Added Music flag for playing of region music on enter/exit.
 *	04/30/06, weaver
 *		Added IsIsolated flag to control visibility of mobiles outside of the region to those within.
 *	05/03/05, Kit
 *		Added LogOutDelay for Inn's
 *		Added Inn Support
 *	05/02/05, Kit
 *		Added toggle for showing iob zone messages, as well as indivitual gate/recall travel disallows
 *		Added ISIOBStronghold flag
 *	04/30/05, Kit
 *		Added EnterArea() function call for entering regions via x/y vs mouse
 *		Added AllowTravel and initial IOB region support
 *	04/29/05, Kitaras
 *		Initial system
 */

using Server.Gumps;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    [TypeAlias("Server.Items.RegionControl")]
    public class CustomRegionControl : Item, IRegionControl
    {
        private static readonly List<CustomRegionControl> m_Instances = new List<CustomRegionControl>();

        public static List<CustomRegionControl> Instances { get { return m_Instances; } }

        public override string DefaultName { get { return "Custom Region Controller"; } }

        private CustomRegion m_Region; // may never be null

        public override Item Dupe(int amount)
        {   // when duping a CustomRegionControl, we don't actually want the region itself as it's already
            //  been 'registered' with its own UId.
            // The region carries all the following info, which we will need for our dupe
            CustomRegionControl new_crc = new();
            if (m_Region != null)
            {
                Utility.CopyProperties(new_crc.CustomRegion, m_Region);
                new_crc.CustomRegion.Coords = new(m_Region.Coords);
            }
            return base.Dupe(new_crc, amount);
        }

        [CopyableAttribute(CopyType.DoNotCopy)] // see Dupe()
        [CommandProperty(AccessLevel.GameMaster)]
        public CustomRegion CustomRegion
        {
            get { return m_Region; }
            set { }
        }

        Region IRegionControl.Region { get { return CustomRegion; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Registered
        {
            get { return m_Region.Registered; }
            set { m_Region.Registered = value; }
        }

        [Constructable]
        public CustomRegionControl()
            : base(0xBF6) // sign
        {
            Movable = false;
            Visible = false;

            m_Region = CreateRegion(this);

            UpdateHue();

            m_Instances.Add(this);
        }

        public virtual CustomRegion CreateRegion(CustomRegionControl rc)
        {
            return new CustomRegion(rc);
        }

        public virtual void UpdateHue()
        {
            if (m_Region == null || !m_Region.Registered)
                this.Hue = 53; // yellow
            else if (m_Region.NoMurderCounts)
                this.Hue = 33; // red
            else
                this.Hue = 70; // green
        }

        public CustomRegionControl(Serial serial)
            : base(serial)
        {
            m_Region = CreateRegion(this);

            m_Instances.Add(this);
        }

        #region Serialization

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)18); // version

            m_Region.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            bool forceRegistry = false;

            switch (version)
            {
                case 18:
                case 17:
                    {
                        m_Region.Deserialize(reader);

                        if (version < 18)
                            forceRegistry = reader.ReadBool(); // enabled

                        break;
                    }
                #region Legacy
                case 16:
                    {
                        int vendorAccess = reader.ReadInt();

                        switch (vendorAccess)
                        {
                            case 0: m_Region.VendorAccess = RegionVendorAccess.NobodyAccess; break;
                            case 1: m_Region.VendorAccess = RegionVendorAccess.AnyoneAccess; break;
                            case 2: m_Region.VendorAccess = RegionVendorAccess.UseDefault; break;
                        }

                        goto case 15;
                    }
                case 15:
                    {
                        // read the keyword database
                        int kwdb_count = reader.ReadInt();
                        for (int ix = 0; ix < kwdb_count; ix++)
                        {
                            string key = reader.ReadString();
                            int obj_count = reader.ReadInt();
                            List<object> list = new List<object>();
                            for (int jx = 0; jx < obj_count; jx++)
                            {
                                switch (reader.ReadInt())
                                {
                                    case 0x1: // Char
                                        list.Add((Char)reader.ReadChar());
                                        continue;

                                    case 0x0: // String
                                        list.Add(reader.ReadString());
                                        continue;
                                }
                            }
                            //m_KeywordDatabase.Add(key, list.ToArray());
                        }

                        // read the item database
                        int idb_count = reader.ReadInt();
                        for (int ix = 0; ix < idb_count; ix++)
                        {
                            Item key = reader.ReadItem();
                            int obj_count = reader.ReadInt();
                            List<object> list = new List<object>();
                            for (int jx = 0; jx < obj_count; jx++)
                            {
                                int field = reader.ReadInt();
                                list.Add(field);
                                switch (field)
                                {
                                    case 0x1: // Track
                                        continue;

                                    case 0x0: // Name
                                        list.Add(reader.ReadString());
                                        jx++;
                                        continue;
                                }
                            }
                            //m_ItemDatabase.Add(key, list.ToArray());
                        }
                        goto case 14;
                    }
                case 14:
                    {
                        //version 14
                        m_Region.Coords.AddRange(ReadRect3DArray(reader));
                        m_Region.InnBounds.AddRange(ReadRect3DArray(reader));
                        goto case 13;
                    }
                case 13:
                    {
                        m_Region.Map = reader.ReadMap();
                        m_Region.GoLocation = reader.ReadPoint3D();
                        m_Region.MinZ = reader.ReadInt();
                        m_Region.MaxZ = reader.ReadInt();
                        goto case 12;
                    }
                case 12:
                    {
                        forceRegistry = reader.ReadBool();
                        goto case 11;
                    }
                case 11:
                    {
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                            m_Region.RestrictedItems.Add(reader.ReadString());
                        goto case 10;
                    }
                case 10:
                    {
                        m_Region.MaxFollowerSlots = reader.ReadInt();
                        goto case 9;
                    }
                case 9:
                    {
                        m_Region.RestrictedMagicMessage = reader.ReadString();
                        goto case 8;
                    }
                case 8:
                    {
                        m_Region.Music = (MusicName)reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        m_Region.InnBounds.AddRange(Region.ConvertTo3D(ReadRect2DArray(reader)));
                        m_Region.InnLogoutDelay = reader.ReadTimeSpan();
                        goto case 6;
                    }
                case 6:
                    {
                        m_Region.DefaultLogoutDelay = reader.ReadTimeSpan();
                        goto case 5;
                    }
                case 5:
                    {
                        if (version < 13)
                            m_Region.UseDungeonRules = reader.ReadBool();
                        goto case 4;
                    }
                case 4:
                    {
                        m_Region.IOBAlignment = (IOBAlignment)reader.ReadInt();
                        goto case 3;
                    }
                case 3:
                    {
                        m_Region.LightLevel = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        goto case 1;
                    }
                case 1:
                case 0:
                    {
                        if (version >= 1)
                            m_Region.Coords.AddRange(Region.ConvertTo3D(ReadRect2DArray(reader)));
                        else
                            m_Region.Coords.Add(Region.ConvertTo3D(reader.ReadRect2D()));

                        if (version >= 1)
                            m_Region.PriorityType = (RegionPriorityType)reader.ReadInt();

                        BitArray restrictedSpells = ReadBitArray(reader);

                        for (int i = 0; i < restrictedSpells.Length; i++)
                        {
                            if (restrictedSpells[i])
                                m_Region.RestrictedSpells.Add(i);
                        }

                        BitArray restrictedSkills = ReadBitArray(reader);

                        for (int i = 0; i < restrictedSkills.Length; i++)
                        {
                            if (restrictedSkills[i])
                                m_Region.RestrictedSkills.Add(i);
                        }

                        int iflags = reader.ReadInt();

                        if ((iflags & 0x00040000) != 0)
                            iflags &= ~0x00040000;
                        else
                            m_Region.MaxFollowerSlots = -1;

                        if ((iflags & 0x00200000) != 0)
                        {
                            m_Region.UseHouseRules = true;
                            iflags &= ~0x00200000;
                        }
                        else if ((iflags & 0x00400000) != 0)
                        {
                            m_Region.UseDungeonRules = true;
                            iflags &= ~0x00400000;
                        }
                        else if ((iflags & 0x02000000) != 0)
                        {
                            m_Region.UseJailRules = true;
                            iflags &= ~0x02000000;
                        }
                        else if ((iflags & 0x04000000) != 0)
                        {
                            m_Region.UseGreenAcresRules = true;
                            iflags &= ~0x04000000;
                        }
                        else if ((iflags & 0x08000000) != 0)
                        {
                            m_Region.UseAngelIslandRules = true;
                            iflags &= ~0x08000000;
                        }

                        m_Region.Flags = (RegionFlag)iflags;
                        m_Region.Name = reader.ReadString();
                        break;
                    }
                    #endregion
            }

            m_Region.Registered = (forceRegistry || m_Region.WasRegistered);
        }

        [Obsolete]
        protected static void WriteBitArray(GenericWriter writer, BitArray ba)
        {
            writer.Write(ba.Length);
            for (int i = 0; i < ba.Length; i++)
            {
                writer.Write(ba[i]);
            }
            return;
        }

        protected static BitArray ReadBitArray(GenericReader reader)
        {
            int size = reader.ReadInt();
            BitArray newBA = new BitArray(size);
            for (int i = 0; i < size; i++)
            {
                newBA[i] = reader.ReadBool();
            }
            return newBA;
        }

        [Obsolete]
        protected static void WriteRect2DArray(GenericWriter writer, ArrayList ary)
        {
            //create a temp list and clean up 
            ArrayList temp = new ArrayList(ary);
            for (int i = temp.Count - 1; i >= 0; --i)
            {
                if (!(temp[i] is Rectangle2D))
                {
                    temp.RemoveAt(i);
                }
            }
            writer.Write(temp.Count);
            for (int i = 0; i < temp.Count; i++)
            {
                writer.Write((Rectangle2D)temp[i]); //Rect2D
            }
            return;
        }

        protected static List<Rectangle2D> ReadRect2DArray(GenericReader reader)
        {
            int size = reader.ReadInt();
            List<Rectangle2D> newAry = new List<Rectangle2D>();
            for (int i = 0; i < size; i++)
            {
                newAry.Add(reader.ReadRect2D());
            }
            return newAry;
        }

        [Obsolete]
        protected static void WriteRect3DArray(GenericWriter writer, ArrayList ary)
        {
            //create a temp list and clean up 
            ArrayList temp = new ArrayList(ary);
            for (int i = temp.Count - 1; i >= 0; --i)
            {
                if (!(temp[i] is Rectangle3D))
                {
                    temp.RemoveAt(i);
                }
            }
            writer.Write(temp.Count);
            for (int i = 0; i < temp.Count; i++)
            {
                //Write the two 3d points
                writer.Write(((Rectangle3D)temp[i]).Start);
                writer.Write(((Rectangle3D)temp[i]).End);
            }
            return;
        }

        protected static List<Rectangle3D> ReadRect3DArray(GenericReader reader)
        {
            int size = reader.ReadInt();
            List<Rectangle3D> newAry = new List<Rectangle3D>();
            for (int i = 0; i < size; i++)
            {
                newAry.Add(new Rectangle3D(reader.ReadPoint3D(), reader.ReadPoint3D()));
            }
            return newAry;
        }

        #endregion

        public virtual void OnEnter(Mobile m)
        {
            return;
        }

        public virtual void OnExit(Mobile m)
        {
            return;
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_Region.Map == null)
                m_Region.Map = this.Map;
        }

        public override void OnSingleClick(Mobile m)
        {
            if (m_Region.Name != null)
                LabelTo(m, m_Region.Name);
            else
                base.OnSingleClick(m);
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                m.SendGump(new RegionControlGump(m_Region));
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Region.Registered = false;

            m_Instances.Remove(this);
        }
    }
}