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

/* Items/Deeds/TentReimbursementDeed.cs
 * ChangeLog:
 *	6/28/21, Adam
 *		Put the possessions into the players backpack
 *	2/27/10, Adam
 *		Created.
 */

using Server.Diagnostics;			// log helper
using System;

namespace Server.Items
{
    public class TentReimbursementDeed : Item
    {
        BaseContainer m_container;                  // the id of the container (for debug reasons)
        int m_ContainerID;                          // saved serial number of above container (should container get deleted)

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseContainer Container
        {
            get { return m_container; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial ContainerID
        {
            get { return m_ContainerID; }
        }

        public TentReimbursementDeed(BaseContainer c)
            : base(0x14F0)
        {
            base.Weight = 1.0;
            base.LootType = LootType.Newbied;                       // newbie shouldn't loose this
            base.Name = "tent reimbursement deed";
            m_container = c;                                        // the storage container
            m_ContainerID = (int)m_container.Serial;                // identifies the prize
            LogHelper Logger = new LogHelper("TentReimbursementDeed.log", false);
            string temp = String.Format("A Tent Reimbursement Deed ({0}) has been created.", this.Serial);
            Logger.Log(LogType.Item, m_container, temp);
            Logger.Finish();
        }

        public TentReimbursementDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_container);
            writer.Write(m_ContainerID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_container = reader.ReadItem() as BaseContainer;
            m_ContainerID = reader.ReadInt();

            if (m_container == null)
            {
                LogHelper Logger = new LogHelper("TentReimbursementDeed.log", false);
                string temp;
                temp = String.Format("Orphaned Tent Reimbursement Deed({0}) loaded for nonexistent Backpack(0x{1:X}).", this.Serial, m_ContainerID);
                Logger.Log(LogType.Text, temp);
                Logger.Finish();
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            string text = null;
            text = "Double click to recover your possessions.";
            this.LabelTo(from, text);
        }

        public override void OnDoubleClick(Mobile from)
        {
            // must not be locked down
            if (this.IsLockedDown == true || this.IsSecure == true)
            {
                from.SendMessage("That is locked down.");
                return;
            }

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            LogHelper Logger = new LogHelper("TentReimbursementDeed.log", false);

            if (m_container != null && m_container.Deleted == false)
            {
                //m_container.MoveToWorld(from.Location, from.Map);		// move the the map
                from.AddToBackpack(m_container);
                //from.SendMessage("Your possessions have been returned.");
                string temp = String.Format("Mobile({0}) using Deed({1}) has recovered their possessions.", from.Serial, this.Serial);
                Logger.Log(LogType.Item, m_container, temp);
            }
            else
            {
                from.SendMessage("There was a problem recovering your possessions.");
                string temp = String.Format("Tent doods missing for Mobile({0}) using Deed({1}) on Backpack(0x{2:X}).", from.Serial, this.Serial, m_ContainerID);
                Logger.Log(LogType.Text, temp);
            }

            // cleanup
            Logger.Finish();
            this.Delete();
        }
    }

}