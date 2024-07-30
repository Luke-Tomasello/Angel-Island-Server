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

/* Scripts\Engines\Dungeons\Wrong\Items\PortalManager.cs
 *	ChangeLog :
 *	6/9/2023, Adam
 *	    Enables Wrong mini-champ if the player has the port key (which has not yet corrupted.)
 *		
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Items
{
    public class PortalManager : Item
    {
        public override string DefaultName => "PortalManager";
        [Constructable]
        public PortalManager()
            : base(0x1B72 /*BronzeShield*/)
        {
            Name = null;
            Visible = false;
        }

        public PortalManager(Serial serial)
            : base(serial)
        {
        }

        public override bool HandlesOnMovement { get { return base.IsRunning; } }
        Timer m_Timer = null;
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {   // running into an already open portal, even if you have a port key, will not reset the timer.
            //  You must active an inactive portal in order to restart the timer.
            if (m != null && m.Player && m.GetDistanceToSqrt(this) < 3 && HasPortKey(m) && !PortalOpen(ControlledItems()))
            {
                Engines.PlaySoundEffect.Play(m, Engines.SoundEffect.Secret);
                EnableControlledItems(ControlledItems(), mode: true);
                if (m_Timer != null && m_Timer.Running)
                {
                    m_Timer.Stop();
                    m_Timer.Flush();
                }
                m_Timer = Timer.DelayCall(TimeSpan.FromMinutes(5), new TimerStateCallback(ClosePortalTick), new object[] { });
            }
        }
        private bool PortalOpen(List<Item> list)
        {
            if (list != null && list.Count > 0)
                foreach (Item item in list)
                    if (item != null && !item.Deleted && item.Map == Map.Felucca)
                        if (item is Teleporter tp && tp.Running)
                            return true;
                        else if (item.ItemID == 0x375A /*sparkle*/ && item.Visible)
                            return true;
            return false;
        }
        private void EnableControlledItems(List<Item> list, bool mode)
        {
            if (list != null && list.Count > 0)
                foreach (Item item in list)
                    if (item != null && !item.Deleted && item.Map == Map.Felucca)
                        if (item is Teleporter tp && tp.Running != mode)
                            tp.Running = mode;
                        else if (item.ItemID == 0x375A /*sparkle*/ && item.Visible != mode)
                            item.Visible = mode;
        }
        private List<Item> ControlledItems()
        {
            List<Item> list = new();
            IPooledEnumerable eable = this.GetItemsInRange(10);
            foreach (Item item in eable)
                if (item != null && !item.Deleted && item.Map == Map.Felucca)
                    if (item is Teleporter || item.ItemID == 0x375A /*sparkle*/)
                        list.Add(item);
            eable.Free();
            return list;
        }
        private bool HasPortKey(Mobile m)
        {
            if (m == null) return false;
            List<Item> list = new();
            if (m.Items != null && m.Items.Count > 0)
                list.AddRange(m.Items);
            if (m.Backpack != null && m.Backpack.Items != null && m.Backpack.Items.Count > 0)
                list.AddRange(m.Backpack.GetDeepItems().Cast<Item>().ToList());
            foreach (Item item in list)
                if (item is PortKey pk && !pk.Corrupted())
                    return true;
            return false;
        }
        private void ClosePortalTick(object state)
        {
            EnableControlledItems(ControlledItems(), mode: false);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running
        {
            get { return base.IsRunning; }
            set
            {
                if (base.IsRunning == value)
                    return;

                base.IsRunning = value;
                if (base.IsRunning == false)
                {
                    if (m_Timer != null && m_Timer.Running)
                    {
                        m_Timer.Stop();
                        m_Timer.Flush();
                    }

                    EnableControlledItems(ControlledItems(), mode: false);
                }
                InvalidateProperties();
            }
        }
        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
                return;


            LabelTo(from, String.Format("{0}", DefaultName));
            if (Running)
                LabelTo(from, String.Format("({0})", "Active"));
            else
                LabelTo(from, String.Format("({0})", "Inactive"));
            return;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            // version 0 - eliminated in version 1
            //writer.Write(Running);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        break;
                    }
                case 0:
                    {
                        Running = reader.ReadBool();
                        break;
                    }
            }
            // we don't try to save/restore states here, just close the portal if it's open
            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerStateCallback(ClosePortalTick), new object[] { });
        }
    }
}