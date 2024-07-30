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

/* Scripts\Items\Controllers\ActivationController.cs
 * CHANGELOG:
 * 	4/7/2024, Adam
 * 		Initial version, activates creatures when the sector deactivates so they can go home
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Items
{
    public class ActivationController : Item
    {
        public const int Limit = 6;

        public override string DefaultName { get { return "Activation Controller"; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public bool Running
        {
            get { return base.IsRunning; }
            set { base.IsRunning = value; }
        }

        private List<EventSpawner> m_Links = new(0); // note: may contain null values
        [CopyableAttribute(CopyType.DoNotCopy)]
        public List<EventSpawner> Links
        {
            get { return m_Links; }
            set { m_Links = value; }
        }

        #region Link Accessors

        public override Item Dupe(int amount)
        {
            ActivationController new_object = new ActivationController();
            Utility.CopyProperties(new_object, this);
            if (Links != null)
            {   // without this, 'duped' ActivationControllers will SHARE a Links list.
                //  Add an EventSpawner to one, it adds it to the other. Change a value in one, it changes it in the other.
                new_object.Links = new(Links);
            }

            return base.Dupe(new_object, amount);
        }

        private EventSpawner GetLink(int index)
        {
            if (index >= 0 && index < m_Links.Count)
                return m_Links[index];

            return null;
        }

        private void SetLink(int index, EventSpawner value)
        {
            if (index < 0 || index >= Limit)
                return;

            if (value != null && value is not EventSpawner)
            {
                this.SendSystemMessage("That is not an event spawner");
                return;
            }

            while (index >= m_Links.Count)
                m_Links.Add(null);

            m_Links[index] = value;
        }

        /*private void AddLink(EventSpawner value)
        {
            int index = 0;

            while (index < m_Links.Count && index < Limit)
            {
                if (m_Links[index] == null || m_Links[index].Deleted)
                {
                    m_Links[index] = value;
                    return;
                }

                index++;
            }

            if (index < Limit)
                SetLink(index, value);
        }*/

        [CommandProperty(AccessLevel.GameMaster)]
        public EventSpawner Link1
        {
            get { return GetLink(0); }
            set { SetLink(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public EventSpawner Link2
        {
            get { return GetLink(1); }
            set { SetLink(1, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public EventSpawner Link3
        {
            get { return GetLink(2); }
            set { SetLink(2, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public EventSpawner Link4
        {
            get { return GetLink(3); }
            set { SetLink(3, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public EventSpawner Link5
        {
            get { return GetLink(4); }
            set { SetLink(4, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public EventSpawner Link6
        {
            get { return GetLink(5); }
            set { SetLink(5, value); }
        }

        #endregion

        [Constructable]
        public ActivationController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
            Running = true;

            m_Links = new List<EventSpawner>();
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public override void OnSectorDeactivate()
        {
            base.OnSectorDeactivate();
            // we need the timer since Sector deactivates Items first, THEN mobiles.
            // when we get called here, mobiles haven't yet been deactivated
            if (Running)
                Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(1.0, 1.75)), new TimerStateCallback(ActivateTick), new object[] { null });
        }

        private void ActivateTick(object state)
        {
            foreach (var link in m_Links)
                if (link != null && link.Objects != null)
                    foreach (object obj in link.Objects)
                        if (obj is BaseCreature bc)
                            bc.SeekHome = true;
        }

        public ActivationController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            // version 0
            writer.WriteItemList(m_Links);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Links = reader.ReadStrongItemList().Cast<EventSpawner>().ToList();
                        break;
                    }
            }
        }
    }
}