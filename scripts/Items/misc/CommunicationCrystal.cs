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

/* Scripts/Comm Crystals/CommunicationCrystal.cs
 * Changelog
 *  12/06/07, Adam
 *      Add an explicit replacement for the Item.SendMessage() function
 *	01/03/07, Pix
 *		Fixed freeze-drying of linked-crystal lists.
 *  09/09/06, Rhiannon
 *		Fixed: If receiver is on the ground, but not locked down, you can't access it.
 *  09/08/06, Rhiannon
 *		Changed on/off message to be private.
 *		Added test for deleted connected crystal in SendMessage().
 *		Changed OnDelete() and arcane gem use to just clear the Connected list (i.e.,
 *			the list of connected receivers).
 *  09/07/06, Rhiannon
 *		Restored mysteriously deleted code
 *		Receivers now lose a charge whenever they receive a message.
 *  08/27/06, Rhiannon
 *		Got rid of name, and text commands.
 *		To turn the crystal on and off, just double-click and target the crystal.
 *		Removed Ping()
 *		Made crystals ISecurable with level set to CoOwners, so it can be locked down and
 *			accessed by owners and co-owners.
 *  08/25/06, Rhiannon
 *		Turning the crystal on and off now changes the ItemID, allowing it to animate.
 *		Locked down receivers can now receive messages from senders not in backpacks.
 *		Fixed a few problems related to Multiplier.
 *		Changed method of calculating gem consumption.
 *  08/24/06, Rhiannon
 *		Removed speaker indicator
 *		Made crystals one-way
 *		Added constant MaxCharges to control the maximum number of charges a crystal can hold
 *		Different gems now add different numbers of charges
 *		If more than one gem is targetted, the gems needed to recharge are consumed.
 *		If multiple gems are used, the appropriate number of charges is added
 *		Hue is green when active, red when inactive.
 *		Crystals on Mobiles that aren't PlayerMobiles (e.g., packhorses) now display text 
 *			above the Mobile.
 *		Changed ping to only do something if there are people to list.
 *		Changed range of crystal from 3 to 12.
 *		Charges now decrement every time a message is sent to a receiver.
 *		Changed Multiplier to double so it can be used to decrease as well as increase the 
 *			number of charges supplied by a gem.
 *		Moved file to Items/Misc
 *  08/21/06, Rhiannon
 *		Imported from Knives' Comm Crystals V1.0, found in the RunUO Custom Script Release Archive
 *		Changed to indicate who is speaking.
 */
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public class CommunicationCrystal : Item, ISecurable
    {
        private const double DefaultMultiplier = 1.0;

        //private ArrayList m_Connected;
        private List<Serial> m_Connected;
        private int m_Charges;
        private double m_Multiplier;
        private bool m_Active;
        private int m_TextHue;
        private SecureLevel m_Level;
        private static int MaxCharges = 500;
        private static int ActiveHue = 65;
        private static int InactiveHue = 33;

        public List<Serial> Connected { get { return m_Connected; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get { return m_Charges; }
            set
            {
                if (value > MaxCharges)
                    value = MaxCharges;

                m_Charges = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Multiplier { get { return m_Multiplier; } set { m_Multiplier = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TextHue
        {
            get { return m_TextHue; }
            set { m_TextHue = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_Level; }
            set { m_Level = value; }
        }

        private int m_OnID = 0x1ECD;
        [CommandProperty(AccessLevel.GameMaster)]
        public int OnID
        {
            get { return m_OnID; }
            set { m_OnID = value; }
        }
        private int m_OffID = 0x1ED0;
        [CommandProperty(AccessLevel.GameMaster)]
        public int OffID
        {
            get { return m_OffID; }
            set { m_OffID = value; }
        }
        [Constructable]
        public CommunicationCrystal()
            : this(DefaultMultiplier)
        {
        }

        [Constructable]
        public CommunicationCrystal(double mult)
            : base(0x1ECD)
        {
            //m_Connected = new ArrayList();
            m_Connected = new List<Serial>();
            m_Charges = 500;
            m_Multiplier = mult;
            m_Active = true;
            m_TextHue = 1150; // White
            m_Level = SecureLevel.CoOwners;

            Name = "comm crystal";
            Light = LightType.Circle150;
        }

        public override void OnDoubleClick(Mobile m)
        {
            // If the crystal is in the player's backpack or it is locked down and 
            // the player has access, allow the player to use the crystal.
            if (IsChildOf(m.Backpack) || CheckAccess(m))
                m.Target = new InternalTarget(this);
            else
                m.SendMessage("You do not have access to that crystal.");
        }

        public bool CheckAccess(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster) return true;
            if (!IsLockedDown) return false;

            BaseHouse house = BaseHouse.FindHouseAt(this);

            // Allow access if the player is at least a co-owner of the house.
            return (house != null && house.HasSecureAccess(m, m_Level));
        }

        private bool Recharge(Mobile m, object obj)
        {
            // Gems must be in the player's backpack
            if (!(obj is Item) || !((Item)obj).IsChildOf(m.Backpack))
                return false;

            if (Multiplier == 0.0) // Don't use gems if charges are unlimited.
            {
                m.SendMessage("This crystal does not need to be recharged.");
                return true;
            }

            Item gem = (Item)obj;
            int GemCharges = 0;

            // Each gem supplies a different number of charges depending on its value.
            if (gem is Citrine) GemCharges = 5; // Price per gem: 60
            else if (gem is Amber) GemCharges = 10; // Price per gem: 90
            else if (gem is Ruby) GemCharges = 10; // Price per gem: 90
            else if (gem is Tourmaline) GemCharges = 10; // Price per gem: 90
            else if (gem is Amethyst) GemCharges = 15; // Price per gem: 120
            else if (gem is Emerald) GemCharges = 15; // Price per gem: 120
            else if (gem is Sapphire) GemCharges = 15; // Price per gem: 120
            else if (gem is StarSapphire) GemCharges = 20; // Price per gem: 150
            else if (gem is Diamond) GemCharges = 25; // Price per gem: 240
            else if (gem is ArcaneGem)
            {
                m_Connected.Clear();
                m.SendMessage("The arcane gem removed all connected receivers.");
                return true;
            }
            else
                return false;

            if (m_Charges < MaxCharges) // Only do this if the crystal needs charges.
            {
                int ChargesNeeded = MaxCharges - m_Charges;
                int NetCharges = (int)Multiplier * GemCharges;
                int TotalCharges = NetCharges * gem.Amount;

                if (m_Charges + TotalCharges <= MaxCharges) // If no extra gems, apply charges and delete.
                {
                    m_Charges += TotalCharges;
                    gem.Delete();
                }
                else // Figure out how many gems to consume.
                {
                    m_Charges = MaxCharges;
                    int consume = (ChargesNeeded / NetCharges) + ((ChargesNeeded % NetCharges == 0) ? 0 : 1);
                    gem.Consume(consume);
                }

                m.SendMessage(ReportCharges());
            }
            else
                m.SendMessage("The crystal is already at maximum charges.");

            return true;
        }

        public override bool HandlesOnSpeech { get { return true; } }

        // This is only used in item.OnAosSingleClick();
        public override void AddNameProperty(ObjectPropertyList list)
        {
            base.AddNameProperty(list);

            if (m_Multiplier != 0.0)
                list.Add(1060658, "Charges\t{0}", m_Charges.ToString());
            list.Add(m_Active ? 1060742 : 1060743);
        }

        #region OnDragDrop() (doesn't work)
        // OnDragDrop() doesn't work because of something in the client.
        //		public override bool OnDragDrop( Mobile from, Item dropped )
        //		{
        //			Console.WriteLine("Trying to drag-drop");
        //
        //			int amount = dropped.Amount;
        //			int gemcharges, addcharges;
        //
        //			if ( dropped is Citrine ) gemcharges = 5; // Price per gem: 60
        //			else if ( dropped is Amber ) gemcharges = 10; // Price per gem: 90
        //			else if ( dropped is Ruby ) gemcharges = 10; // Price per gem: 90
        //			else if ( dropped is Tourmaline ) gemcharges = 10; // Price per gem: 90
        //			else if ( dropped is Amethyst ) gemcharges = 15; // Price per gem: 120
        //			else if ( dropped is Emerald ) gemcharges = 15; // Price per gem: 120
        //			else if ( dropped is Sapphire ) gemcharges = 15; // Price per gem: 120
        //			else if ( dropped is StarSapphire ) gemcharges = 20; // Price per gem: 150
        //			else if ( dropped is Diamond ) gemcharges = 25; // Price per gem: 240
        //			else if ( dropped is ArcaneGem )
        //			{
        //				foreach( CommunicationCrystal comm in m_Connected )
        //					comm.Connected.Remove( this );
        //
        //				m_Connected.Clear();
        //				from.SendMessage( "The arcane gem cleared all your links!" );
        //				return true;
        //			}
        //			else
        //			{
        //				Console.WriteLine("Not a gem");
        //				from.AddToBackpack( dropped );
        //				from.PlaySound( dropped.GetDropSound() );
        //				return true;
        //			}
        //
        //			if ( m_Charges < MaxCharges )
        //			{
        //				addcharges = Multiplier * amount * gemcharges;
        //
        //				if ( addcharges > (MaxCharges - m_Charges) )
        //				{
        //					dropped.Consume( MaxCharges - m_Charges );
        //					m_Charges = MaxCharges;
        //				}
        //				else
        //				{
        //					m_Charges += addcharges;
        //					dropped.Delete();
        //				}
        //
        //				String text;
        //				if ( Multiplier == 0 )
        //					text = "You apply the gem to the crystal.";
        //				else
        //					text = ReportCharges();
        //
        //				from.SendMessage( text );
        //
        //			}
        //			else
        //				from.SendMessage( "The crystal is already at maximum charges." );
        //
        //			return true;
        //		}
        #endregion

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            String text = ReportCharges();

            this.LabelTo(from, text);
        }

        public String ReportCharges()
        {
            String text;

            if (m_Charges <= 0)
                text = "The crystal has no charges.";
            else if (m_Charges < MaxCharges / 20)
                text = "The crystal has very few charges left.";
            else if (m_Charges < MaxCharges / 5)
                text = "The crystal does not have very many charges.";
            else if (m_Charges < (MaxCharges / 4) + (MaxCharges / 100))
                text = "The crystal is at about one quarter charges.";
            else if (m_Charges < (MaxCharges / 3) + (MaxCharges / 100))
                text = "The crystal is at about one third charges.";
            else if (m_Charges < (MaxCharges / 2) - (MaxCharges / 100))
                text = "The crystal is at just under one half charges.";
            else if (m_Charges < (MaxCharges / 2) + (MaxCharges / 100))
                text = "The crystal is at about one half charges.";
            else if (m_Charges < (MaxCharges / 2) + (MaxCharges / 100))
                text = "The crystal is at just over one half charges.";
            else if (m_Charges < ((MaxCharges / 3) * 2) + (MaxCharges / 100))
                text = "The crystal is at about two thirds charges.";
            else if (m_Charges < MaxCharges - (MaxCharges / 100))
                text = "The crystal is close to being fully charged.";
            else if (m_Charges < MaxCharges)
                text = "The crystal is almost fully charged.";
            else
                text = "The crystal is fully charged.";

            return text;
        }

        public override void OnDelete()
        {
            //			foreach( CommunicationCrystal comm in m_Connected )
            //				comm.Connected.Remove( this );

            m_Connected.Clear();
        }

        protected void OnTarget(Mobile m, object obj)
        {
            if (Recharge(m, obj))
                return;

            if (!(obj is CommunicationCrystal))
                return;

            if (obj == this)
            {
                if (Active)
                {
                    Active = false;
                    ItemID = m_OffID;
                    m.SendMessage(InactiveHue, "You turn the crystal off.");
                }
                else
                {
                    if (m_Charges == 0)
                        m.SendMessage("The crystal has no charges.");
                    else
                    {
                        Active = true;
                        ItemID = m_OnID;
                        m.SendMessage(ActiveHue, "You turn the crystal on.");
                    }
                }
                return;
            }

            if (!((Item)obj).Movable)
            {
                m.SendMessage("You cannot connect to this crystal.");
                return;
            }

            CommunicationCrystal ccTarget = (CommunicationCrystal)obj;
            if (m_Connected.Contains(ccTarget.Serial))
            {
                m_Connected.Remove(ccTarget.Serial);
                m.SendMessage("You have disconnected the crystals.");
            }
            else
            {
                m_Connected.Add(ccTarget.Serial);
                m.SendMessage("You have connected the crystals.");
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!m_Active)
                return;

            if (IsChildOf(e.Mobile.Backpack)
                || e.Mobile.InRange(Location, 12))
                SendMessage(e.Speech);
        }

        private void SendMessage(string text)
        {
            ArrayList ToDelete = new ArrayList();
            foreach (Serial commSerial in m_Connected)
            {
                bool bIsFD = false;
                CommunicationCrystal comm = ItemBroker.GetItem<CommunicationCrystal>(commSerial, ref bIsFD);

                if (bIsFD)
                {
                    //skip, because that Crystal is freezedried
                }
                else if (comm == null || comm.Deleted)
                {
                    //Mark to delete it if we can't find it in the world OR it's been deleted
                    ToDelete.Add(commSerial);
                }
                else
                {
                    // If the crystals are not in the same place, or the receiver is locked down, send message.
                    if (comm.RootParent != RootParent || comm.IsLockedDown)
                    {
                        comm.ReceiveMessage(text);

                        // Decrement charges if Multiplier is not 0.0, and turn the crystal off when empty.
                        if (m_Multiplier != 0.0 && --Charges <= 0)
                        {
                            Active = false;
                        }
                    }
                }
            }
            // Remove deleted crystals
            foreach (Serial s in ToDelete)
            {
                if (m_Connected.Contains(s))
                {
                    m_Connected.Remove(s);
                }
            }
        }

        private void ReceiveMessage(string text)
        {
            if (!m_Active)
                return;

            if (RootParent is Mobile)
            {
                if (RootParent is PlayerMobile)
                    ((Mobile)RootParent).SendMessage(TextHue, string.Format("(crystal) {0}", text));
                else ((Mobile)RootParent).Say(text); // Pack animal
            }
            else if (Parent == null)
                PublicOverheadMessage(Network.MessageType.Regular, TextHue, false, text);

            if (m_Multiplier != 0.0 && --Charges <= 0)
                Active = false;
        }

        public CommunicationCrystal(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)4); // version

            // version 4
            writer.Write(m_OnID);
            writer.Write(m_OffID);

            // older versions
            writer.Write((int)m_Level);
            writer.Write(m_Active);
            writer.Write(m_Multiplier);
            writer.Write(m_Charges);
            //writer.WriteItemList( m_Connected, true );
            ItemBroker.WriteSerialList(writer, m_Connected);
            writer.Write(m_TextHue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        m_OnID = reader.ReadInt();
                        m_OffID = reader.ReadInt();
                        goto case 3;
                    }
                case 3:
                    {
                        goto case 2;
                    }
                case 2:
                case 1:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        m_Multiplier = reader.ReadDouble();
                        m_Charges = reader.ReadInt();
                        if (version < 3)
                        {
                            //m_Connected = reader.ReadItemList();
                            ArrayList list = reader.ReadItemList();
                            m_Connected = new List<Serial>(list.Count);
                            for (int i = 0; i < list.Count; i++)
                            {
                                CommunicationCrystal cc = list[i] as CommunicationCrystal;
                                if (cc != null)
                                {
                                    m_Connected.Add(cc.Serial);
                                }
                            }
                        }
                        else
                        {
                            m_Connected = ItemBroker.ReadSerialList(reader);
                        }
                        m_TextHue = reader.ReadInt();

                        break;
                    }
            }
        }


        private class InternalTarget : Target
        {
            private CommunicationCrystal m_Crystal;

            public InternalTarget(CommunicationCrystal crystal)
                : base(1, false, TargetFlags.None)
            {
                m_Crystal = crystal;
            }

            protected override void OnTarget(Mobile m, object obj)
            {
                m_Crystal.OnTarget(m, obj);
            }
        }

    }


}