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

/* Items/Triggers/ItemDispenser.cs
 * CHANGELOG:
 *  3/29/2024, Adam
 *      Add location dispensing
 *  3/27/2024, Adam
 *      DispenserType.MobilesFeet: Include both dead and alive mobiles.
 *      In the case of the dead mobile, leave at the corpse. Otherwise at the mobiles feet.
 *  3/26/2024, Adam
 *      1. Percolate construction errors up to the controller for display to staff.
 *          These will often be AccessLevel errors (setting props you do not have sufficient rights to set.)
 *      2. Fail to trigger of the SpawnStr is null or empty
 *  3/20/2024, Adam (DispenserType)
 *      Allow the following locations for the dispensed item: MobilesBackpack, MobilesCorpse, MobilesFeet, ControlMastersBackpack, ControlMastersFeet
 *  6/2/23, Yoar
 *      Bumped up SpawnStr write access level from GM to Seer
 *      This is because SpawnEngine can perform Seer-level operations
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using Server.Diagnostics;
using Server.Mobiles;
using System;
using System.Collections;

namespace Server.Items.Triggers
{
    public class ItemDispenser : Item, ITriggerable
    {
        public enum DispenserType : byte
        {
            MobilesBackpack,
            MobilesCorpse,
            MobilesFeet,
            ControlMastersBackpack,
            ControlMastersFeet,
            Location
        }

        private Point3D m_DropLocation = Point3D.Zero;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public Point3D DropLocation { get { return m_DropLocation; } set { m_DropLocation = value; } }

        private DispenserType m_SpawnWhere = DispenserType.MobilesBackpack;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public DispenserType SpawnWhere { get { return m_SpawnWhere; } set { m_SpawnWhere = value; } }
        public override string DefaultName { get { return "Item Dispenser"; } }

        private string m_Message;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Message
        {
            get { return m_Message; }
            set
            {
                m_Message = value;
                SendSystemMessage("Supported macros:");
                SendSystemMessage("{name} {title} {guild_short} {guild_long} {item.Name}");
            }
        }

        private string m_SpawnStr;
        private bool m_PackFullMessage;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public string SpawnStr
        {
            get { return m_SpawnStr; }
            set { m_SpawnStr = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool PackFullMessage
        {
            get { return m_PackFullMessage; }
            set { m_PackFullMessage = value; }
        }

        [Constructable]
        public ItemDispenser()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_PackFullMessage = true;
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public void OnTrigger(Mobile from)
        {
            if (from == null)
                return;

            string reason = string.Empty;
            Item toGive = CreateItem(ref reason);

            //if (reason != string.Empty)
            //    this.SendSystemMessage(string.Format("{0}: {1}", m_SpawnStr, reason));

            if (toGive == null)
                return;

            LogHelper logger = new LogHelper(String.Format("ItemDispenser.log"), true, true, true);
            logger.Log(String.Format("Successfully created item {0} for {1}", toGive, from));
            logger.Finish();

            switch (m_SpawnWhere)
            {
                case DispenserType.MobilesBackpack:
                    {
                        if (!from.PlaceInBackpack(toGive))
                        {
                            if (m_PackFullMessage)
                                from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.

                            toGive.Delete();
                        }
                        else if (!string.IsNullOrEmpty(m_Message))
                            from.SendMessage(Utility.ExpandMacros(from, toGive, m_Message));
                        break;
                    }
                case DispenserType.MobilesCorpse:
                    {
                        if (from.Corpse == null)
                            toGive.Delete();
                        else
                            from.Corpse.AddItem(toGive);
                        break;
                    }
                case DispenserType.ControlMastersBackpack:
                    {
                        if (from is BaseCreature bc && bc.LastControlMaster != null)
                        {
                            bc.LastControlMaster.AddToBackpack(toGive);
                            if (!string.IsNullOrEmpty(m_Message))
                                bc.LastControlMaster.SendMessage(Utility.ExpandMacros(bc.LastControlMaster, toGive, m_Message));
                        }
                        else
                            toGive.Delete();
                        break;
                    }
                case DispenserType.ControlMastersFeet:
                    {
                        if (from is BaseCreature bc && bc.LastControlMaster != null)
                        {
                            toGive.MoveToWorld(bc.LastControlMaster.Location, bc.LastControlMaster.Map);
                            if (!string.IsNullOrEmpty(m_Message))
                                from.SendMessage(Utility.ExpandMacros(from, toGive, m_Message));
                        }
                        else
                            toGive.Delete();
                        break;
                    }
                case DispenserType.MobilesFeet:
                    {
                        if (from.Dead)
                        {
                            if (from.Corpse == null)
                                toGive.Delete();
                            else
                            {
                                toGive.MoveToWorld(from.Corpse.Location, from.Corpse.Map);
                                if (!string.IsNullOrEmpty(m_Message) && from.Corpse is Corpse c && c.Owner is PlayerMobile pm)
                                    pm.SendMessage(Utility.ExpandMacros(pm, toGive, m_Message));
                            }
                        }
                        else
                        {
                            toGive.MoveToWorld(from.Location, from.Map);
                            if (!string.IsNullOrEmpty(m_Message))
                                from.SendMessage(Utility.ExpandMacros(from, toGive, m_Message));
                        }
                        break;
                    }
                case DispenserType.Location:
                    {
                        if (m_DropLocation == Point3D.Zero)
                            toGive.Delete();
                        else
                            toGive.MoveToWorld(m_DropLocation, this.Map);
                        break;
                    }
            }
        }

        private Item CreateItem(ref string reason)
        {
            return SpawnEngine.Build<Item>(m_SpawnStr, ref reason);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                list.Add(new TriggerSystem.ActivateCME(((ITriggerable)this).CanTrigger(from)));
        }

        #region ITriggerable

        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return !string.IsNullOrEmpty(m_SpawnStr);
        }

        void ITriggerable.OnTrigger(Mobile from)
        {
            OnTrigger(from);
        }

        #endregion

        public ItemDispenser(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            writer.Write(m_Message);

            // version 2
            writer.Write(m_DropLocation);

            // version 1
            writer.Write((byte)m_SpawnWhere);

            // version 0
            writer.Write((string)m_SpawnStr);
            writer.Write((bool)m_PackFullMessage);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_Message = reader.ReadString();
                        goto case 2;
                    }
                case 2:
                    {
                        m_DropLocation = reader.ReadPoint3D();
                        goto case 1;
                    }
                case 1:
                    {
                        m_SpawnWhere = (DispenserType)reader.ReadByte();
                        goto case 0;
                    }
                case 0:
                    {
                        m_SpawnStr = reader.ReadString();
                        m_PackFullMessage = reader.ReadBool();

                        break;
                    }
            }
        }
    }
}