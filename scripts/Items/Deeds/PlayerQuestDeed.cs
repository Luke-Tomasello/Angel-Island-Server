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

/* Items/Deeds/PlayerQuestDeed.cs
 * ChangeLog:
 *	9/21/06, Adam
 *		Make sure the deed is not locked down in OnDoubleClick()
 *  9/08/06, Adam
 *		Created.
 */

using Server.Diagnostics;			// log helper
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Items
{
    public class PlayerQuestDeed : Item
    {
        BaseContainer m_container;                  // the Prize
        int m_PrizeID;                              // saved serial number of above container (should container get deleted)
        DateTime m_expires;                         // when the quest expired (and the prize deleted)
        private bool m_claimed = false;             // was the prize claimed. No need to serialize because it's only used in-session.

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseContainer Container
        {
            get { return m_container; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial PrizeID
        {
            get { return m_PrizeID; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Expires
        {
            get { return m_expires - DateTime.UtcNow; }
            set { m_expires = DateTime.UtcNow + value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Expired
        {
            get { return DateTime.UtcNow > m_expires; }
        }

        public PlayerQuestDeed(BaseContainer c)
            : base(0x14F0)
        {
            base.Weight = 1.0;
            base.Name = "a quest ticket";
            m_container = c;                                        // the prize
            m_expires = DateTime.UtcNow + TimeSpan.FromHours(24.0);    // Heartbeat has it's own hadrcoded notion of 24 hours not tied to this value
            m_PrizeID = (int)m_container.Serial;                    // identifies the prize
            PlayerQuestManager.Deeds.Add(this);                     // add to our managers list
            PlayerQuestManager.Announce();                          // force an announcement now
            LogHelper Logger = new LogHelper("PlayerQuest.log", false);
            string temp = String.Format("A Player Quest Deed({0}) has been created.", this.Serial);
            Logger.Log(LogType.Item, m_container, temp);
            Logger.Finish();
        }

        public PlayerQuestDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_container);
            writer.Write(m_expires);
            writer.Write(m_PrizeID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_container = reader.ReadItem() as BaseContainer;
            m_expires = reader.ReadDateTime();
            m_PrizeID = reader.ReadInt();

            // okay, add deeds read from disk to our quest manager
            if (m_container != null && Expired == false)
                // when we deserialize we don't know the ordr in which the deeds will be read,
                //	so insure they are stored sorted.
                PlayerQuestManager.AddSorted(this);
            else
            {
                LogHelper Logger = new LogHelper("PlayerQuest.log", false);
                string temp;
                if (m_container == null)
                    temp = String.Format("Orphaned Quest Deed({0}) loaded for nonexistent Chest(0x{1:X}).", this.Serial, m_PrizeID);
                else // Expired == true
                    temp = String.Format("Expired Quest Deed({0}) loaded for (non)existent Chest(0x{1:X}).", this.Serial, m_PrizeID);
                Logger.Log(LogType.Text, temp);
                Logger.Finish();
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            string text = null;
            if (Expired == true)
                text = "[Expired]";
            else
            {
                int hours = Expires.Hours;
                int minutes = Expires.Minutes;
                text = String.Format("[Expires: {0} {1}, and {2} {3}]",
                    hours, hours == 1 ? "hour" : "hours",
                    minutes, minutes == 1 ? "minute" : "minutes");
            }
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

            LogHelper Logger = new LogHelper("PlayerQuest.log", false);

            // Heartbeat cleanup will cleanup the prize automatically
            if (m_container != null && m_container.Deleted == false && Expired == false)
            {
                //m_container.MoveToWorld(from.Location, from.Map);					// move the the map
                m_container.RetrieveItemFromIntStorage(from.Location, from.Map);    // get from safe storage
                m_claimed = true;
                from.SendMessage("You have completed the quest!");
                string temp = String.Format("Mobile({0}) using Deed({1}) has completed the quest.", from.Serial, this.Serial);
                Logger.Log(LogType.Item, m_container, temp);
            }
            else
            {
                from.SendMessage("That quest item has expired.");
                if (m_container != null && m_container.Deleted == false)
                    m_container.Delete();                                           // the quest is over, delete the container, ticket deleted below
                string temp = String.Format("Quest expired for Mobile({0}) using Deed({1}) on quest Chest(0x{2:X}).", from.Serial, this.Serial, m_PrizeID);
                Logger.Log(LogType.Text, temp);
            }

            // cleanup
            Logger.Finish();
            this.Delete();
        }

        public override void OnDelete()
        {   // cleanup the chest if this ticket is deleted, unless if the prize was claimed.
            if (m_container != null && m_container.Deleted == false && m_claimed == false)
                m_container.Delete();                                           // the quest is over, delete the container, ticket deleted below

            base.OnDelete();
        }
    }

    public class PlayerQuestManager
    {
        private const int m_MaxMessages = 5;
        private static ArrayList m_Table = new ArrayList();
        private static TownCrierEntry[] m_TownCrierMessages = new TownCrierEntry[m_MaxMessages];

        public static ArrayList Deeds
        {
            get
            {
                return m_Table;
            }
        }

        public static void ClearAllMessages()
        {
            for (int ix = 0; ix < m_MaxMessages; ix++)
                ClearMessage(ix);
        }

        public static void ClearMessage(int index)
        {
            if (index < 0 || index >= m_MaxMessages)    // range check
                return;

            if (m_TownCrierMessages[index] != null)
            {
                GlobalTownCrierEntryList.Instance.RemoveEntry(m_TownCrierMessages[index]);
                m_TownCrierMessages[index] = null;
            }
        }

        // when we deserialize we don't know the order in which the deeds will be read,
        //	so insure they are stored sorted.
        public static void AddSorted(PlayerQuestDeed pqd)
        {
            // find the correct insert point
            if (m_Table.Count > 0)
                foreach (object temp in m_Table)
                {
                    if (temp == null || temp is PlayerQuestDeed == false)
                        continue;

                    PlayerQuestDeed px = temp as PlayerQuestDeed;
                    if (pqd.Expires < px.Expires)
                    {   // insert before this one
                        m_Table.Insert(m_Table.IndexOf(temp), pqd);
                        return;
                    }
                }

            // just add it, it's either the first element, or the oldest
            m_Table.Add(pqd);
        }

        public static void SetMessage(string[] lines, int index)
        {
            int duration = 5;                           // 5 minutes
            if (index < 0 || index >= m_MaxMessages)    // range check
                return;

            if (lines[0].Length > 0)
            {
                ClearMessage(index);
                m_TownCrierMessages[index] = new TownCrierEntry(lines, TimeSpan.FromMinutes(duration), Serial.MinusOne);
                GlobalTownCrierEntryList.Instance.AddEntry(m_TownCrierMessages[index]);
            }
        }

        public static int Announce(out int PlayerQuestsAnnounced)
        {
            PlayerQuestsAnnounced = 0;
            //LogHelper Logger = new LogHelper("PlayerQuest.log", false);
            int count = 0;

            try
            {
                int msgndx = 0;
                ArrayList ToDelete = new ArrayList();
                ArrayList ParentMobile = new ArrayList();

                // clear any messages currently on the TC
                PlayerQuestManager.ClearAllMessages();

                // find expired
                foreach (object o in PlayerQuestManager.Deeds)
                {
                    if (o is PlayerQuestDeed == false) continue;
                    PlayerQuestDeed pqd = o as PlayerQuestDeed;

                    if (pqd.Deleted == true)
                        ToDelete.Add(pqd);
                    else
                    {
                        object root = pqd.RootParent;
                        //bool exclude = false;
                        count++;

                        // don't announce an expired quest
                        if (pqd.Expired == true)
                            continue;

                        // don't announce if in a locked down container in a house
                        if (root is BaseContainer && Server.Multis.BaseHouse.FindHouseAt(pqd) != null)
                        {
                            BaseContainer bc = root as BaseContainer;
                            if (bc.IsLockedDown == true || bc.IsSecure == true)
                                continue;
                        }

                        // don't announce if locked down or secure
                        if (pqd.IsLockedDown == true || pqd.IsSecure == true)
                            continue;

                        // don't announce if on the internal map
                        if (pqd.Map == Map.Internal || root is Mobile && (root as Mobile).Map == Map.Internal)
                            continue;

                        // don't announce if in bankbox
                        if (root is Mobile && pqd.IsChildOf((root as Mobile).BankBox))
                            continue;

                        // only announce 1 ticket per mobile or container
                        // (15 tickets on a mob, or in a chest should generate 1 announcement)
                        if (root != null)
                            if (ParentMobile.Contains(root))
                                continue;

                        // only public houses
                        Server.Multis.BaseHouse house = null;
                        if (root is Item)
                            house = Server.Multis.BaseHouse.FindHouseAt(root as Item);
                        if (root is Mobile)
                            house = Server.Multis.BaseHouse.FindHouseAt(root as Mobile);
                        if (house != null && house.Public == false)
                            continue;

                        ///////////////////////////////////////////////////////
                        // okay announce it !
                        // record the parent
                        if (root != null)
                            ParentMobile.Add(root);

                        // format the message
                        string[] lines = new string[2];
                        if (root is Mobile)
                        {
                            Mobile mob = root as Mobile;
                            lines[0] = String.Format(
                                "{0} was last seen near {1}. {2} is not to be trusted.",
                                mob.Name,
                                BaseOverland.DescribeLocation(mob),
                                mob.Female == true ? "She" : "He");

                            lines[1] = String.Format(
                                "Do what you will with {0}, but get that quest ticket {1} carries.",
                                mob.Female == true ? "her" : "him",
                                mob.Female == true ? "she" : "he");
                        }
                        else
                        {
                            lines[0] = String.Format(
                                "A quest ticket was last seen near {0}",
                                BaseOverland.DescribeLocation(root == null ? pqd : root as Item));

                            lines[1] = String.Format(
                                "It may be of significant value, but be careful!");
                        }

                        // queue it
                        PlayerQuestManager.SetMessage(lines, msgndx++);

                        // count it
                        PlayerQuestsAnnounced++;

                    }

                    // record the expiring quest chest
                    //Logger.Log(LogType.Item, bc, "Player Quest prize being deleted because the quest has expired.");
                }

                // cleanup
                for (int i = 0; i < ToDelete.Count; i++)
                {
                    PlayerQuestDeed pqd = ToDelete[i] as PlayerQuestDeed;
                    if (pqd != null)
                        PlayerQuestManager.Deeds.Remove(pqd);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Console.WriteLine("Exception while running PlayerQuestManager.Announce() job");
                Console.WriteLine(e);
            }
            finally
            {
                //Logger.Finish();
            }

            return count;
        }

        public static void Announce()
        {
            int PlayerQuestsAnnounced;
            Announce(out PlayerQuestsAnnounced);
        }
    }

}