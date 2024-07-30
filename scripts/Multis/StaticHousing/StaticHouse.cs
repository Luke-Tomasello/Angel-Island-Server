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

/* Scripts/Multis/StaticHousing/StaticHouse.cs
 *  Changelog:
 *  6/5/22, Yoar
 *      Rewrote completely:
 *      - Reorganized/renamed variables.
 *      - Added getters/setters for the house's region rectangles.
 *      - Static houses can now be constructed directly from a blueprint.
 *      - Removed old code.
 *  6/3/22, Yoar
 *      Added support for house doors
 *  9/17/21, Yoar
 *      Static housing revamp
 *	12/28/07 Taran Kain
 *		Added BuildFixerAddon() to take care of doubled-up tiles
 *	8/25/07, Adam
 *		Override CheckSignpost() to ensure we have one
 *	6/25/07, Adam
 *      Major changes, please SR/MR for full details
 *  6/11/07, Pix
 *	    Added GetDeed() override so you can demolish the house nicely and get a deed back.
 *		Added versioning to the Serialize/Deserialize.
 *  06/08/2007, plasma
 *      Initial creation
 */

using Server.Items;
using Server.Multis.Deeds;
using System;
using System.Collections.Generic;
using HouseBlueprint = Server.Multis.StaticHousing.StaticHouseHelper.HouseBlueprint;

namespace Server.Multis.StaticHousing
{
    public class StaticHouse : HouseFoundation
    {
        public override bool IsAosRules { get { return false; } }

        private string m_BlueprintID;
        private double m_BlueprintVersion;
        private DateTime m_CaptureDate;
        private string m_Description;
        private int m_DefaultPrice;
        private string m_DesignerName;
        private Serial m_DesignerSerial;
        private string m_DesignerAccount;
        private List<Rectangle2D> m_AreaList = new List<Rectangle2D>();

        [CommandProperty(AccessLevel.GameMaster)]
        public string HouseBlueprintID
        {
            get { return m_BlueprintID; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double BlueprintVersion
        {
            get { return m_BlueprintVersion; }
            set { m_BlueprintVersion = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime CaptureDate
        {
            get { return m_CaptureDate; }
            set { m_CaptureDate = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int DefaultPrice
        {
            get { return m_DefaultPrice; }
            set { m_DefaultPrice = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DesignerName
        {
            get { return m_DesignerName; }
            set { m_DesignerName = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial DesignerSerial
        {
            get { return m_DesignerSerial; }
            set { m_DesignerSerial = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DesignerAccount
        {
            get { return m_DesignerAccount; }
            set { m_DesignerAccount = value; }
        }

        public List<Rectangle2D> AreaList
        {
            get { return m_AreaList; }
        }

        #region Edit Area

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D AreaRect1
        {
            get { return GetRect(0); }
            set { SetRect(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D AreaRect2
        {
            get { return GetRect(1); }
            set { SetRect(1, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D AreaRect3
        {
            get { return GetRect(2); }
            set { SetRect(2, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D AreaRect4
        {
            get { return GetRect(3); }
            set { SetRect(3, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D AreaRect5
        {
            get { return GetRect(4); }
            set { SetRect(4, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D AreaRect6
        {
            get { return GetRect(5); }
            set { SetRect(5, value); }
        }

        private Rectangle2D GetRect(int index)
        {
            if (index >= 0 && index < m_AreaList.Count)
                return m_AreaList[index];

            return new Rectangle2D();
        }

        private void SetRect(int index, Rectangle2D value)
        {
            if (index < 0)
                return;

            if (index < m_AreaList.Count)
            {
                m_AreaList[index] = value;
            }
            else
            {
                if (value.Width == 0 || value.Height == 0)
                    return;

                m_AreaList.Add(value);
            }

            DefragAreaList();
            UpdateRegionArea();
        }

        public void DefragAreaList()
        {
            for (int i = m_AreaList.Count - 1; i >= 0; i--)
            {
                Rectangle2D rect = m_AreaList[i];

                if (rect.Width == 0 || rect.Height == 0)
                    m_AreaList.RemoveAt(i);
            }
        }

        #endregion

        public override Rectangle2D[] Area
        {
            get
            {
                if (m_AreaList.Count == 0)
                    return base.Area;

                return m_AreaList.ToArray();
            }
        }

        public StaticHouse(Mobile owner, HouseBlueprint blueprint)
            : base(owner, StaticHouseHelper.GetFoundationID(blueprint.Width, blueprint.Height), 270, 2, 2)
        {
            BuildDesign(blueprint);
            BuildFixerAddon(blueprint);
            BuildDoors(blueprint);
            TransferData(blueprint);
        }

        public StaticHouse(Serial serial)
            : base(serial)
        {
        }

        private void BuildDesign(HouseBlueprint blueprint)
        {
            DesignState design = new DesignState(this.CurrentState);

            MultiComponentList mcl = design.Components;

            // remove original tiles
            for (int i = mcl.List.Length - 1; i >= 0; i--)
            {
                MultiTileEntry mte = mcl.List[i];

                mcl.Remove(mte.m_ItemID, mte.m_OffsetX, mte.m_OffsetY, mte.m_OffsetZ);
            }

            // add blueprint tiles
            foreach (MultiTileEntry mte in StaticHouseHelper.BuildMCL(blueprint).List)
                mcl.Add(mte.m_ItemID, mte.m_OffsetX, mte.m_OffsetY, mte.m_OffsetZ, true);

            this.CurrentState = design;

            CheckSignpost();
        }

        private void BuildFixerAddon(HouseBlueprint blueprint)
        {
            BaseAddon fa = StaticHouseHelper.BuildFixerAddon(blueprint);

            fa.MoveToWorld(Location, this.Map);
            Addons.Add(fa);
            fa.OnAfterPlace(null, this);
        }

        private void BuildDoors(HouseBlueprint blueprint)
        {
            BaseDoor[] doors = StaticHouseHelper.BuildDoors(blueprint);

            foreach (BaseDoor door in doors)
                Doors.Add(door);
        }

        private void TransferData(HouseBlueprint blueprint)
        {
            m_BlueprintID = blueprint.ID;
            m_DefaultPrice = blueprint.Price;
            m_BlueprintVersion = blueprint.Version;
            m_CaptureDate = blueprint.Capture;
            m_Description = blueprint.Description;
            m_DesignerName = blueprint.OriginalOwnerName;
            m_DesignerSerial = blueprint.OriginalOwnerSerial;
            m_DesignerAccount = blueprint.OriginalOwnerAccount;

            if (SignHanger != null)
            {
                if (blueprint.SignHangerGraphic > 0)
                {
                    if (blueprint.UseSignLocation)
                        SignHanger.MoveToWorld(new Point3D(X + blueprint.SignLocation.X, Y + blueprint.SignLocation.Y, Z + blueprint.SignLocation.Z), Map);

                    SignHanger.ItemID = blueprint.SignHangerGraphic;
                }
                else
                {
                    SignHanger.Delete();
                }
            }

            if (Signpost != null)
            {
                if (blueprint.SignpostGraphic > 0)
                    Signpost.ItemID = blueprint.SignpostGraphic;
                else
                    Signpost.Delete();
            }

            if (Sign != null)
            {
                if (blueprint.UseSignLocation)
                    Sign.MoveToWorld(new Point3D(X + blueprint.SignLocation.X, Y + blueprint.SignLocation.Y, Z + blueprint.SignLocation.Z), Map);

                if (blueprint.SignGraphic > 0)
                    Sign.ItemID = blueprint.SignGraphic;
            }

            m_AreaList.Clear();

            MultiComponentList mcl = this.Components;

            foreach (Rectangle2D rect in blueprint.Area)
            {
                m_AreaList.Add(new Rectangle2D(
                    rect.X + this.X + mcl.Min.X,
                    rect.Y + this.Y + mcl.Min.Y,
                    rect.Width,
                    rect.Height));
            }

            DefragAreaList();
            UpdateRegionArea();
        }

        public override HouseDeed GetDeed()
        {
            if (!StaticHouseHelper.BlueprintExists(m_BlueprintID))
                return null;

            return new StaticDeed(m_BlueprintID);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)5); // version

            // verson 5
            writer.Write((DateTime)m_CaptureDate);

            // version 4
            writer.Write((int)m_AreaList.Count);
            for (int i = 0; i < m_AreaList.Count; i++)
                writer.Write((Rectangle2D)m_AreaList[i]);

            // version 3
            writer.Write((string)m_Description);
            writer.Write((string)m_DesignerName);
            writer.Write((string)m_DesignerAccount);
            writer.Write((int)m_DesignerSerial);

            // version 2
            writer.Write((int)m_DefaultPrice);
            writer.Write((double)m_BlueprintVersion);

            // version 1
            writer.Write((string)m_BlueprintID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    {
                        m_CaptureDate = reader.ReadDateTime();
                        goto case 4;
                    }
                case 4:
                    {
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                            m_AreaList.Add(reader.ReadRect2D());
                        UpdateRegionArea();
                        goto case 3;
                    }
                case 3:
                    {
                        m_Description = reader.ReadString();
                        m_DesignerName = reader.ReadString();
                        m_DesignerAccount = reader.ReadString();
                        m_DesignerSerial = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_DefaultPrice = reader.ReadInt();
                        m_BlueprintVersion = reader.ReadDouble();
                        goto case 1;
                    }
                case 1:
                    {
                        m_BlueprintID = reader.ReadString();
                        break;
                    }
            }
        }
    }
}