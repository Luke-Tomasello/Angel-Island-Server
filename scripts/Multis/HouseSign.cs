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

/* Scripts\Multis\HouseSign.cs
 * CHANGELOG
 *  2/25/2024, Adam
 *      Don't allow staff to accidentally refresh the house by double clicking the sign
 *  8/1/22, Yoar
 *      Cleanups related to Mortalis house inheritance.
 *	5/9/22, Adam
 *		Globally refactor (rename) Owner ==> OriginalOwner 
 *  5/9/22, Yoar
 *      Added an empty setter to Structure so that we can navigate to the house's props via the sign's props.
 *  11/23/21, Adam
 *      Add single click functionality to display membership expiration.
 *  10/17/21, Adam (MembersOnly)
 *      Add support for declaring a house Members Only
 *	3/16/16, Adam
 *		Globally refactor (rename) both Owner ==> Structure, and  OriginalOwner ==> Owner
 *	2/14/11, Adam
 *		UOMO: Add Inheritance mechanism that allows a new character on an account to Inherit the house previously owned 
 *			on that account
 *	9/2/07, Adam
 *		Add a auto-resume-decay system so that we can feeze for a set amount of time.
 *	7/27/07, Adam
 *		Add SuppressRegion property to turn on region suppression 
 *	6/25/07, Adam
 *		make StaticHousingSign Constructable
 *	6/25/06, Adam
 *		Add StaticHousingSign for use on static build houses before they are captured
 *			and converted into a new StaticHouse
 *	2/21/06, Pix
 *		Added SecurePremises flag.
 *	8/28/05, Pix
 *		Made FreezeDecay property's logic actually correct ;-p
 *	8/25/05, Pix
 *		Change the NeverDecay property to FreezeDecay (made logic easier to see).
 *		Changed to be readable by councellor+ and settable by Admin.
 *	8/4/05, Pix
 *		Change to house decay.
 *	6/11/05, Pix
 *		Added BanLocation to set/see the Ban Location of a house.
 *	6/10/04, Pix
 *		Changes for House decay
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Gumps;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Multis
{

    public class HouseSign : Item
    {
        #region ListMembers
        public static void Initialize()
        {
            Server.CommandSystem.Register("ListMembers", AccessLevel.Administrator, new CommandEventHandler(ListMembers_OnCommand));
        }
        [Usage("ListMembers <target HouseSign>")]
        [Description("Lists the Memberships associated with this house.")]
        public static void ListMembers_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the HouseSign to query...");
            e.Mobile.Target = new HouseSignListTarget(); // Call our target
        }
        public class HouseSignListTarget : Target
        {
            public HouseSignListTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is HouseSign sign)
                {
                    if (sign.Structure != null)
                        foreach (KeyValuePair<Mobile, DateTime> kvp in sign.Structure.Memberships)
                        {
                            from.SendMessage(string.Format("{0}'s membership expires on {1}", kvp.Key, kvp.Value));
                        }
                }
                else
                    from.SendMessage("That is not a HouseSign.");
            }
        }
        #endregion ListMembers

        private BaseHouse m_Structure;
        private Mobile m_OriginalOwner;

        public HouseSign(BaseHouse owner)
            : base(0xBD2)
        {
            m_Structure = owner;
            m_OriginalOwner = m_Structure.Owner;
            Name = "a house sign";
            Movable = false;
        }

        public HouseSign(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                base.LabelTo(from, "[GM Info Only: Collapse Time: {0}]", DateTime.UtcNow + TimeSpan.FromMinutes(m_Structure.DecayMinutesStored));
            }

            base.LabelTo(from, m_Structure.DecayState());

            if (Structure != null)
                foreach (KeyValuePair<Mobile, DateTime> kvp in Structure.Memberships)
                {
                    if (kvp.Key != null && kvp.Key == from)
                        from.SendMessage(string.Format("Your membership expires on {0}", kvp.Value));
                }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseHouse.CooperativeType CooperativeType
        {
            get
            {
                if (m_Structure != null)
                    return m_Structure.Cooperative;
                return BaseHouse.CooperativeType.None;
            }
            set
            {
                if (m_Structure != null)
                    m_Structure.Cooperative = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool MembershipOnly
        {
            get
            {
                if (m_Structure != null)
                    return m_Structure.MembershipOnly;
                return false;
            }
            set
            {
                if (m_Structure != null)
                    m_Structure.MembershipOnly = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Addons
        {
            get
            {
                if (m_Structure != null && m_Structure.Addons != null)
                    return m_Structure.Addons.Count;
                else
                    return 0;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseHouse Structure
        {
            get
            {
                return m_Structure;
            }
            set { }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool SecurePremises
        {
            get
            {
                return m_Structure.SecurePremises;
            }
            set
            {
                m_Structure.SecurePremises = value;
            }
        }

        [CommandProperty(AccessLevel.Seer, AccessLevel.Seer)]
        public bool SuppressRegion
        {
            get
            {
                return m_Structure.SuppressRegion;
            }
            set
            {
                m_Structure.SuppressRegion = value;
            }
        }

        [CommandProperty(AccessLevel.Seer, AccessLevel.Seer)]
        public bool ManagedDemolishion
        {
            get
            {
                return m_Structure.ManagedDemolition;
            }
            set
            {
                m_Structure.ManagedDemolition = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool FreezeDecay
        {
            get
            {
                return m_Structure.NeverDecay;
            }
            set
            {
                m_Structure.NeverDecay = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public TimeSpan RestartDecay
        {
            get
            {
                return m_Structure.RestartDecay;
            }
            set
            {
                m_Structure.RestartDecay = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D BanLocation
        {
            get
            {
                return m_Structure.BanLocation;
            }
            set
            {
                m_Structure.SetBanLocation(value);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double HouseDecayMinutesStored
        {
            get
            {
                return m_Structure.DecayMinutesStored;
            }
            set
            {
                m_Structure.DecayMinutesStored = value;
            }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile OriginalOwner
        {
            get
            {
                return m_OriginalOwner;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Structure != null && !m_Structure.Deleted)
                m_Structure.Delete();
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1061638); // A House Sign
        }

        public override bool ForceShowProperties
        {
            get
            {
                return ObjectPropertyList.Enabled;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1061639, Name == "a house sign" ? "nothing" : Utility.FixHtml(Name)); // Name: ~1_NAME~
            list.Add(1061640, (m_Structure == null || m_Structure.Owner == null) ? "nobody" : m_Structure.Owner.Name); // Owner: ~1_OWNER~

            if (m_Structure != null)
                list.Add(m_Structure.Public ? 1061641 : 1061642); // This House is Open to the Public : This is a Private Home
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (m_Structure != null)
            {
                // Mortalis house inheritance - from one character to the next (of kin)
                if (Core.RuleSets.MortalisRules())
                {
                    if (m_Structure.Owner == null || m_Structure.Owner.Deleted)
                    {
                        if (m_Structure.CheckInheritance(m))
                        {
                            m_Structure.Owner = m;

                            if (m_Structure.Public == false)
                                m_Structure.ChangeLocks(m);
                        }
                    }

                    // fall through
                }
                // 2/25/2024, Adam: Don't allow staff to accidentally refresh in this way
                if (m_Structure.IsFriend(m) && m.AccessLevel == AccessLevel.Player || m == m_Structure.Owner)
                {
                    m.SendLocalizedMessage(501293); // Welcome back to the house, friend!

                    if (Core.RuleSets.SiegeStyleRules()) //refresh house
                    {
                        double dms = m_Structure.DecayMinutesStored;
                        m_Structure.Refresh();

                        //if we're more than one day (less than 14 days) from the max stored (15 days), 
                        //then tell the friend that the house is refreshed
                        if (dms < TimeSpan.FromDays(14.0).TotalMinutes)
                        {
                            m.SendMessage("You refresh the house.");
                        }
                    }
                }

                if (m_Structure.IsAosRules)
                    m.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Information, m, m_Structure));
                else
                    m.SendGump(new HouseGump(m, m_Structure));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Structure);
            writer.Write(m_OriginalOwner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                /*case 1:
				{

					goto case 0;
				}*/
                case 0:
                    {
                        m_Structure = reader.ReadItem() as BaseHouse;
                        m_OriginalOwner = reader.ReadMobile();

                        break;
                    }
            }
        }
    }

    public class StaticHouseSign : Item
    {
        private Mobile m_Owner;
        private DateTime m_BuiltOn;

        [Constructable]
        public StaticHouseSign()
            : base(0xBD2)
        {
            m_Owner = null;
            Name = "a static house sign";
            Movable = false;
            m_BuiltOn = DateTime.UtcNow;
        }

        public StaticHouseSign(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public DateTime BuiltOn
        {
            get { return m_BuiltOn; }
            set { m_BuiltOn = value; }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1061638); // A House Sign
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Owner);
            writer.Write(m_BuiltOn);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        m_BuiltOn = reader.ReadDateTime();
                        break;
                    }
            }
        }
    }
}