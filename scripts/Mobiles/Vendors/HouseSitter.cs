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

/* Mobiles/Vendors/HouseSitter.cs
 * CHANGELOG:
 *	10/1/10, Adam
 *		Remove callse to initOutfit and initBody as they are alreday called from the base class 
 *			causes client crash when mobile-incomming pakets are sent probebly because of the double hair and whatnot.
 *		Remove unneeded backpack
 *	1/19/08, Adam
 *		make IsOwner an override of the new Mobile.cs virtual function
 *  06/27/06, Kit
 *		Changed IsInvunerable to constructor as is no longer a virtual function.
 *	11/30/04, Pix
 *		Fixed the refresh count... now it won't reset on world load.
 *  11/12/04, Jade
 *      Changed spelling to make Housesitter one word in title.
 *	11/9/04 - Pix
 *		Allowed owners and coowners to see the status off and dismiss all housesitters in their house.
 *	11/7/04 - Pix
 *		Upped cost per secure from 250 to 500.
 *		Added max uses of 90 refreshes.
 *		HouseSitter's owner has to be a friend to refresh.
 *	11/6/04 - Pix
 *		Initial Version
 */

using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class HouseSitter : BaseVendor
    {
        private Mobile m_Owner;
        private const int CHARGEPERSECURE = 500;
        private const double REFRESHUNTILDAYS = 15.0; // the number of days left on the house's decay before we start charging.
        private const int MAXNUMBEROFREFRESHES = 90; //the maximum number of times this housesitter with refresh the house.

        private int m_NumberOfRefreshes;


        //Misc vendor stuff.
        protected ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }
        public override bool IsActiveVendor { get { return false; } }
        public override bool CanBeDamaged() { return false; }
        public override bool ShowFameTitle { get { return false; } }
        public override bool DisallowAllMoves { get { return true; } }
        public override bool ClickTitle { get { return true; } }
        public override bool CanTeach { get { return false; } }
        public override void InitSBInfo() { }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Owner
        {
            get { return (PlayerMobile)m_Owner; }
            set { m_Owner = value; }
        }

        public int NumberOfRefreshes
        {
            get { return m_NumberOfRefreshes; }
            set { m_NumberOfRefreshes = value; }
        }

        public int MaxNumberOfRefreshes
        {
            get { return MAXNUMBEROFREFRESHES; }
        }

        public double RefreshUntilDays
        {
            get { return REFRESHUNTILDAYS; }
        }

        public int ChargPerSecure
        {
            get { return CHARGEPERSECURE; }
        }

        public HouseSitter(Mobile owner)
            : base("the housesitter")//Jade: Change spelling to housesitter.
        {
            m_Owner = owner;
            IsInvulnerable = true;
            CantWalkLand = true;
            InitStats(75, 75, 75);
            m_NumberOfRefreshes = 0;
        }

        public HouseSitter(Serial serial)
            : base(serial)
        {
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);
            bool bIsHouseOwner = false;
            if (house != null)
            {
                bIsHouseOwner = house.IsCoOwner(from);
            }

            if (IsOwner(from) || bIsHouseOwner || from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new StatusContextMenu(from, this));
                list.Add(new DismissContextMenu(from, this));

            }
            base.AddCustomContextEntries(from, list);
        }

        private class StatusContextMenu : ContextMenuEntry
        {
            HouseSitter m_Sitter;
            Mobile m_Mobile;

            public StatusContextMenu(Mobile from, HouseSitter sitter)
                : base(2134)
            {
                m_Sitter = sitter;
                m_Mobile = from;
            }

            public override void OnClick()
            {
                BaseHouse house = BaseHouse.FindHouseAt(m_Sitter);
                bool bIsHouseOwner = false;
                if (house != null)
                {
                    bIsHouseOwner = house.IsCoOwner(m_Mobile);
                }

                if (m_Sitter.IsOwner(m_Mobile) || bIsHouseOwner)
                {
                    m_Sitter.SendStatusTo(m_Mobile);
                }
            }
        }

        private class DismissContextMenu : ContextMenuEntry
        {
            HouseSitter m_Sitter;
            Mobile m_Mobile;

            public DismissContextMenu(Mobile from, HouseSitter sitter)
                : base(6129)
            {
                m_Sitter = sitter;
                m_Mobile = from;
            }

            public override void OnClick()
            {
                BaseHouse house = BaseHouse.FindHouseAt(m_Sitter);
                bool bIsHouseOwner = false;
                if (house != null)
                {
                    bIsHouseOwner = house.IsCoOwner(m_Mobile);
                }

                if (m_Sitter.IsOwner(m_Mobile) || bIsHouseOwner)
                {
                    m_Sitter.Dismiss(m_Mobile);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);//version
                                 //version 2:
            writer.Write(m_NumberOfRefreshes);

            //version 1:
            writer.Write(m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_NumberOfRefreshes = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            // new stuff that needs to be initialized if we're loading from an
            // old version.
            if (version < 1)
            {
                m_NumberOfRefreshes = 0;
            }

            NameHue = CalcInvulNameHue();
        }

        public override bool IsOwner(Mobile m)
        {
            return (m == m_Owner || m.AccessLevel >= AccessLevel.GameMaster);
        }

        /*public void RefreshHouseIfNeeded()
		{
			BaseHouse house = BaseHouse.FindHouseAt(this);
			if (house != null)
			{
				if (house.IsFriend(this.m_Owner))
				{
					if (m_NumberOfRefreshes < MAXNUMBEROFREFRESHES)
					{
						if (!house.m_NeverDecay && ((house.StructureDecayTime - DateTime.UtcNow) < TimeSpan.FromDays(REFRESHUNTILDAYS)))
						{
							int costperday = house.MaxSecures * CHARGEPERSECURE;
							Container cont = m_Owner.BankBox;
							if (cont != null)
							{
								int gold = cont.GetAmount(typeof(Gold), true);

								if (cont.ConsumeTotal(typeof(Gold), costperday, true))
								{
									System.Console.WriteLine("Refresh!!");
									house.RefreshHouseOneDay();
									m_NumberOfRefreshes++;
								}
								else //not enough money in bank
								{
								}
							}
						}
						else //doesn't need refreshing
						{
						}
					}
					else //reached max refreshes
					{
					}
				}
				else //not a friend
				{
				}
			}
		}*/

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();
            SpeechHue = 0x3B2;

            NameHue = CalcInvulNameHue();

            if (this.Female = Utility.RandomBool())
            {
                this.Body = 0x191;
                this.Name = NameList.RandomName("female");
            }
            else
            {
                this.Body = 0x190;
                this.Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Item item = new FancyShirt(Utility.RandomNeutralHue());
            item.Layer = Layer.InnerTorso;
            AddItem(item);
            AddItem(new LongPants(Utility.RandomNeutralHue()));
            AddItem(new BodySash(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new Cloak(Utility.RandomNeutralHue()));

            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);
        }


        public override bool HandlesOnSpeech(Mobile from)
        {
            return (from.GetDistanceToSqrt(this) <= 4);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (e.Handled)
                return;

            BaseHouse house = BaseHouse.FindHouseAt(this);
            bool bIsHouseOwner = false;
            if (house != null)
            {
                bIsHouseOwner = house.IsCoOwner(from);
            }

            if (e.HasKeyword(0x3F) || (e.HasKeyword(0x174))) // status
            {
                if (IsOwner(from) || bIsHouseOwner)
                {
                    SendStatusTo(from);
                    e.Handled = true;
                }
                else
                {
                    SayTo(from, "This is not your business.");
                }
            }
            else if (e.HasKeyword(0x40) || (e.HasKeyword(0x175))) // dismiss
            {
                if (IsOwner(from) || bIsHouseOwner)
                    Dismiss(from);
            }
            else if (e.HasKeyword(0x41) || (e.HasKeyword(0x176))) // cycle
            {
                if (IsOwner(from) || bIsHouseOwner)
                    this.Direction = this.GetDirectionTo(from);
            }
        }


        public void SendStatusTo(Mobile from)
        {
            Container cont = m_Owner.BankBox;
            if (cont != null)
            {
                int gold = cont.GetAmount(typeof(Gold), true);

                BaseHouse house = BaseHouse.FindHouseAt(this);
                if (house != null)
                {
                    int costperday = house.MaxSecures * CHARGEPERSECURE;
                    int availabledays = gold / costperday;

                    if (availabledays > MAXNUMBEROFREFRESHES - m_NumberOfRefreshes)
                    {
                        availabledays = MAXNUMBEROFREFRESHES - m_NumberOfRefreshes;
                    }

                    string message = string.Format("Based on the gold in your bank and how long I'll work, I can watch your house for {0} days.", availabledays);
                    SayTo(from, message);
                }
                else
                {
                    Say("I've lost the house!!  Goodbye!");
                    this.Delete();
                }

            }
            else
            {
                SayTo(from, "You have no more gold in your bank, so I can no longer watch your house.");
                this.Delete();
            }
        }

        private void Dismiss(Mobile from)
        {
            from.SendGump(new ConfirmGump(from, this));
        }

        private class ConfirmGump : Gump
        {
            private HouseSitter m_Sitter;

            public ConfirmGump(Mobile from, HouseSitter hs)
                : base(50, 50)
            {
                from.CloseGump(typeof(ConfirmGump));

                AddPage(0);

                AddBackground(10, 10, 190, 140, 0x242C);

                AddHtml(30, 30, 150, 75, String.Format("<div align=CENTER>{0}</div>", "Dismiss this house sitter?"), false, false);

                AddButton(40, 105, 0x81A, 0x81B, 0x1, GumpButtonType.Reply, 0); // Okay
                AddButton(110, 105, 0x819, 0x818, 0x2, GumpButtonType.Reply, 0); // Cancel
                m_Sitter = hs;
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                Mobile from = state.Mobile;

                if (info.ButtonID == 1)
                {
                    m_Sitter.Delete();
                }
            }
        }


    }
}