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

/* Scripts/Multis/Tent/Tent.cs
 * ChangeLog:
 *	6/28/21, Adam
 *		Reinstitute annexation and add a notion of a short waiting period for the house being placed to be demolished
 *			if Core.UOBETA is set to allow testing of this system
 *	3/17/16, Adam
 *		Disable public void Annex() by implementing rule #7 in HousePlacement.cs
 *	3/5/10, Adam
 *		Fix a count bug while coping the items from the tentpack to the new pack
 *	2/27/10, Adam
 *		Add the Annex function to delete the tent and place the owners stuffs in a claim ticket
 *	08/14/06, weaver
 *		Modified component construction to pass BaseHouse reference to the backpack.
 *	05/22/06, weaver
 *		- Added 5 day, refreshable decay timebank
 *		- Set default price to 0
 *	05/07/06, weaver
 *		- Made tent bedroll immovable
 *		- Made tent pack hue same as tent roof hue
 *	05/01/06, weaver
 *		Initial creation. 
 */

using Server.Items;
using Server.Mobiles;
using Server.Multis.Deeds;
using System;

namespace Server.Multis
{

    public class Tent : BaseHouse
    {
        // 5 day timebank
        public static TimeSpan m_TimeBankMax = TimeSpan.FromMinutes(60 * 24 * 5);
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-3, -3, 7, 7), new Rectangle2D(-1, 4, 3, 1) };
        public override Rectangle2D[] Area { get { return AreaArray; } }
        public override int DefaultPrice { get { return 0; } }
        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.TwoStoryFoundations[0]; } }

        private TentBedRoll m_TentBed;
        private TentBackpack m_TentPack;

        public TentBackpack TentPack
        {
            get
            {
                return m_TentPack;
            }
        }

        public TentBedRoll TentBed
        {
            get
            {
                return m_TentBed;
            }
        }

        private int m_RoofHue;

        public Tent(Mobile owner, int Hue)
            : base(0xFFE, owner, 270, 2, 2)
        {
            m_RoofHue = Hue;

            if (Core.RuleSets.SiegeStyleRules())
            {   // assume normal decay times for tents on Siege
                m_TimeBankMax = BaseHouse.MaxHouseDecayTime;
                DecayMinutesStored = BaseHouse.HouseDecayDelay.TotalMinutes;
            }
            else
            {
                // wea: this gets called after the base class, overriding it 
                DecayMinutesStored = m_TimeBankMax.TotalMinutes;
            }

            BanLocation = new Point3D(2, 4, 0);
        }

        public Tent(Serial serial)
            : base(serial)
        {
        }

        // Disabled until further understood.
        //	see rule #7 in 
        public void Annex(BaseHouse house)
        {
            // because we were annexed, the owner of the new house cannot Demolish it for 7 days
            if (house != null)
            {
                house.SetBaseHouseBool(BaseHouseBoolTable.ManagedDemolition, true);

                if (house.Owner != null)
                    if (Core.UOBETA_CFG == true)
                        house.Owner.SendMessage("Annexation of another property requires that you keep this house here for at least 1 hour.");
                    else
                        house.Owner.SendMessage("Annexation of another property requires that you keep this house here for at least 7 days.");
            }

            if (m_TentPack != null && this.Owner != null)
            {
                Backpack pack = new Backpack();

                // give them back their tent
                pack.AddItem(new TentBag());

                // Adam: Tents don't follow the rules :\ their regions don't list the mobiles (vendors)
                //	so we'll have to find them via enumeration in the rects of the region
                if (Region.Coords.Count != 0)
                {
                    System.Collections.ArrayList toDelete = new System.Collections.ArrayList();
                    foreach (Rectangle3D rect3D in Region.Coords)
                    {
                        Rectangle2D rect = new Rectangle2D(rect3D.Start, rect3D.End);
                        IPooledEnumerable eable;
                        eable = this.Map.GetMobilesInBounds(rect);
                        foreach (object obj in eable)
                        {
                            if (obj is PlayerVendor && (obj as PlayerVendor).Deleted == false && toDelete.Contains(obj) == false)
                            {
                                PlayerVendor pv = obj as PlayerVendor;

                                // give them back the vendor contract
                                pack.AddItem(new ContractOfEmployment());

                                // now give them the gold
                                int hold = pv.HoldGold;
                                pack.AddItem(new Gold(hold));

                                // now give them the loot
                                if (pv.Backpack != null && pv.Backpack.Items.Count > 0)
                                {
                                    for (int ix = 0; ix < pv.Backpack.Items.Count; ix++)
                                    {
                                        Item item = pv.Backpack.Items[0] as Item;
                                        pv.Backpack.RemoveItem(item);
                                        pack.AddItem(item);
                                    }
                                }

                                toDelete.Add(obj);
                            }
                        }

                        eable.Free();
                    }

                    // delete the vendors
                    for (int mx = 0; mx < toDelete.Count; mx++)
                    {
                        if ((toDelete[mx] as PlayerVendor).Deleted == false)
                            (toDelete[mx] as PlayerVendor).Delete();
                    }
                }

                // copy the items from the tentpack to the new pack
                if (m_TentPack.Items != null && m_TentPack.Items.Count > 0)
                {
                    while (m_TentPack.Items.Count > 0)
                    {
                        Item item = m_TentPack.Items[0] as Item;
                        m_TentPack.RemoveItem(item);
                        pack.AddItem(item);
                    }
                }

                // save the pack in a safe place
                pack.MoveToIntStorage();

                BankBox box = this.Owner.BankBox;
                if (box == null)
                    return;

                box.AddItem(new TentReimbursementDeed(pack));

                this.Owner.SendMessage("By decree of Lord British, your tent has been annexed.");
                this.Owner.SendMessage("You may recover your belongings by redeeming the deed in your bankbox.");

                this.Delete();
            }
        }

        public void GenerateTent()
        {
            TentWalls walls = new TentWalls(TentStyle.Newbie);
            TentRoof roof = new TentRoof(m_RoofHue);
            //TentTrim trim = new TentTrim();
            TentFloor floor = new TentFloor();

            walls.MoveToWorld(this.Location, this.Map);
            roof.MoveToWorld(this.Location, this.Map);
            //trim.MoveToWorld( this.Location, this.Map );
            floor.MoveToWorld(this.Location, this.Map);

            Addons.Add(walls);
            Addons.Add(roof);
            //Addons.Add( trim );
            Addons.Add(floor);

            // Create tent bed
            m_TentBed = new TentBedRoll(this);
            m_TentBed.MoveToWorld(new Point3D(this.X, this.Y + 1, this.Z), this.Map);
            m_TentBed.Movable = false;

            // Create secute tent pack within the tent
            m_TentPack = new TentBackpack(this);
            m_TentPack.MoveToWorld(new Point3D(this.X - 1, this.Y - 1, this.Z), this.Map);
            SecureInfo info = new SecureInfo((Container)m_TentPack, SecureLevel.Anyone);
            m_TentPack.IsSecure = true;
            this.Secures.Add(info);
            m_TentPack.Movable = false;
            m_TentPack.Hue = m_RoofHue;
        }

        public override void MoveToWorld(Point3D location, Map map, Mobile responsible = null)
        {
            base.MoveToWorld(location, map);
            GenerateTent();
        }

        public override void OnDelete()
        {
            if (m_TentBed != null)
                m_TentBed.Delete();
            if (m_TentPack != null)
                m_TentPack.Delete();

            base.OnDelete();
        }

        public override HouseDeed GetDeed()
        {
            return new TentBag();
        }

        // Override standard decay handling to provide 5 day timebank

        public override void Refresh()
        {
            if (Core.RuleSets.SiegeStyleRules())
                base.Refresh();
            else
            {
                // Refresh to max of 5 days (standard houses refresh to 15 out of 30)
                DecayMinutesStored = m_TimeBankMax.TotalMinutes;

                if (DecayMinutesStored > BaseHouse.ONE_DAY_IN_MINUTES && IDOC_Broadcast_TCE != null)
                {
                    GlobalTownCrierEntryList.Instance.RemoveEntry(IDOC_Broadcast_TCE);
                    IDOC_Broadcast_TCE = null;
                }
            }
        }

        public override void RefreshHouseOneDay()
        {
            if (Core.RuleSets.SiegeStyleRules())
                base.RefreshHouseOneDay();
            {
                if (DecayMinutesStored <= m_TimeBankMax.TotalMinutes)
                {
                    if (DecayMinutesStored >= (m_TimeBankMax.TotalMinutes - ONE_DAY_IN_MINUTES))
                    {
                        DecayMinutesStored = m_TimeBankMax.TotalMinutes;
                    }
                    else
                    {
                        DecayMinutesStored += ONE_DAY_IN_MINUTES;
                    }
                }

                if (DecayMinutesStored > ONE_DAY_IN_MINUTES && IDOC_Broadcast_TCE != null)
                {
                    GlobalTownCrierEntryList.Instance.RemoveEntry(IDOC_Broadcast_TCE);
                    IDOC_Broadcast_TCE = null;
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version

            writer.Write(m_TentBed);
            writer.Write(m_TentPack);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_TentBed = (TentBedRoll)reader.ReadItem();
            m_TentPack = (TentBackpack)reader.ReadItem();

            if (version == 0)
                BanLocation = new Point3D(2, 4, 0);
        }
    }
}