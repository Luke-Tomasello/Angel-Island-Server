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

/* Scripts\Items\Triggers\StateControllers.cs
 * CHANGELOG:
 *  6/14/2024, Adam (Defrag.Available())
 *      Removing map check. Players can leave the map as long as they return in time.
 *      Keeping Alive check as per Tabby.
 *  6/11/2024, Adam
 *      Added ResetStateControllers
 *  4/29/2024, Adam
 *      Add single click debugging:
 *      1. Where the controller points for true branch and false branch
 *      2. highlight controllers
 *      3. label controllers as (true) and (false)
 *      4. system message: serial numbers, and controller type
 *      5. system message: if the state controller 'has state(true)' for the mobile issuing the single click
 *  4/23/2024, Adam
 * 		Initial version.
 * 		Allows setting, querying, and clearing simple state variables
 */

using Server.ContextMenus;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    [NoSort]
    public class StateController : Item, ITriggerable
    {
        public override string DefaultName { get { return "State Controller"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }

        private TimeSpan m_Remember;
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Remember
        {
            get { return m_Remember; }
            set { m_Remember = value; }
        }

        public enum _Scope
        {
            Mobile,
            OneAtATime
        }

        private _Scope m_Scope = _Scope.Mobile;
        [CommandProperty(AccessLevel.GameMaster)]
        public _Scope Scope
        {
            get { return m_Scope; }
            set { m_Scope = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Clear
        {
            get { return false; }
            set
            {
                if (value)
                    Memory = new();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Tripped
        {
            get { Defrag(); return Memory.Count > 0; }
        }

        [Constructable]
        public StateController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return Ready(from);
        }

        public void OnTrigger(Mobile from)
        {
            SetBlock(from);
        }

        private Dictionary<Mobile, DateTime> Memory = new();
        public bool Ready(Mobile from)
        {
            return !CheckBlock(from);
        }

        public void SetBlock(Mobile m)
        {
            Defrag();

            if (Scope == _Scope.OneAtATime && Memory.Count > 0)
                return;

            Memory[m] = DateTime.UtcNow + Remember;
        }
        public bool CheckBlock(Mobile m)
        {
            Defrag();

            return Memory.ContainsKey(m);
        }
        private void Defrag()
        {
            List<Mobile> list = new List<Mobile>();
            foreach (var kvp in Memory)
                if (DateTime.UtcNow > kvp.Value || !Available(kvp.Key))
                    list.Add(kvp.Key);

            foreach (var m in list)
                Memory.Remove(m);
        }
        private bool Available(Mobile m)
        {   // 6/14/2024, Adam: Removing map check: Players can leave the map as long as they return in time.
            // Keeping Alive check as per Tabby.
            if (m.Alive /*&& m.Map == Map*/)
                // distance check?
                return true;

            return false;
        }
        #region External Access
        public bool HasState(Mobile m)
        {
            return CheckBlock(m);
        }
        public bool HasStateAny()
        {
            Defrag();
            return Memory.Count > 0;
        }
        public void ClearState(Mobile m)
        {
            if (Memory.ContainsKey(m))
                Memory.Remove(m);
        }
        #endregion External Access

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        public StateController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(Memory.Count);
            foreach (var kvp in Memory)
            {
                writer.Write(kvp.Key);
                writer.WriteDeltaTime(kvp.Value);
            }

            writer.Write(m_Event);
            writer.Write((TimeSpan)m_Remember);
            writer.Write((int)m_Scope);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        int count = reader.ReadInt();
                        Mobile m = null;
                        DateTime dt;
                        for (int ix = 0; ix < count; ix++)
                        {
                            m = reader.ReadMobile();
                            dt = reader.ReadDeltaTime();

                            if (m != null)
                                Memory.Add(m, dt);
                        }

                        m_Event = reader.ReadString();
                        m_Remember = reader.ReadTimeSpan();
                        m_Scope = (_Scope)reader.ReadInt();
                        break;
                    }
            }
        }
    }
    [NoSort]
    public class CheckStateController : Item, ITriggerable, ITrigger
    {
        public override string DefaultName { get { return "Check State Controller"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }

        private Item m_Link;
        [CommandProperty(AccessLevel.System)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        private Item m_Else;
        [CommandProperty(AccessLevel.System)]
        public Item Else
        {
            get { return m_Else; }
            set { m_Else = value; }
        }

        [Copyable(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Item TrueBranch
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [Copyable(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Item FalseBranch
        {
            get { return m_Else; }
            set { m_Else = value; }
        }

        private StateController m_Controller;
        [CommandProperty(AccessLevel.GameMaster)]
        public StateController StateController
        {
            get { return m_Controller; }
            set { m_Controller = value; }
        }

        private bool m_ResetOnSuccess = true;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool ResetOnSuccess
        {
            get { return m_ResetOnSuccess; }
            set { m_ResetOnSuccess = value; }
        }

        private CheckMode m_CheckMode = CheckMode.SinglePlayer;
        public enum CheckMode : int
        {
            SinglePlayer,
            AnyPlayer
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public CheckMode Mode
        {
            get { return m_CheckMode; }
            set { m_CheckMode = value; }
        }
        [Constructable]
        public CheckStateController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            SendSystemMessage(string.Format("True Branch {0}", TrueBranch == null ? "(null)" : TrueBranch.ToString()));
            SendSystemMessage(string.Format("False Branch {0}", FalseBranch == null ? "(null)" : FalseBranch.ToString()));
            if (StateController != null)
            {
                bool hasState = (m_CheckMode == CheckMode.SinglePlayer) ? StateController.HasState(from) : StateController.HasStateAny();
                SendSystemMessage(string.Format("{0} has state {1} for {2}",
                    StateController, hasState, (m_CheckMode == CheckMode.SinglePlayer) ? from : "Any"));
            }
            else
                SendSystemMessage("No StateController link.");
            if (TrueBranch != null)
            {
                TrueBranch.Blink();
                TrueBranch.LabelTo(from, "(True)");
            }
            if (FalseBranch != null)
            {
                FalseBranch.Blink();
                FalseBranch.LabelTo(from, "(False)");
            }
            if (StateController != null)
            {
                StateController.Blink();
                StateController.LabelTo(from, "(Source)");
            }
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            bool result;
            return CheckCondition(from, out result) && TriggerSystem.CanTrigger(from, result ? m_Link : m_Else);
        }
        private bool CheckCondition(Mobile from, out bool result)
        {
            result = HasState(from);
            return true;
        }
        public bool HasState(Mobile m)
        {
            if (m_Controller != null && (m_CheckMode == CheckMode.SinglePlayer) ? m_Controller.HasState(m) : m_Controller.HasStateAny())
                return true;

            return false;
        }
        public void OnTrigger(Mobile from)
        {
            bool result;

            if (CheckCondition(from, out result))
                TriggerSystem.OnTrigger(from, result ? m_Link : m_Else);

            if (result && ResetOnSuccess)
                if (m_Controller != null)
                    m_Controller.ClearState(from);
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        public CheckStateController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write((int)m_CheckMode);

            // version 1
            writer.Write(m_Event);
            writer.Write((Item)m_Link);
            writer.Write((Item)m_Else);
            writer.Write((Item)m_Controller);
            writer.Write(m_ResetOnSuccess);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_CheckMode = (CheckMode)reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Event = reader.ReadString();
                        m_Link = reader.ReadItem();
                        m_Else = reader.ReadItem();
                        m_Controller = (StateController)reader.ReadItem();
                        m_ResetOnSuccess = reader.ReadBool();
                        break;
                    }
            }
        }
    }
    [NoSort]
    public class ResetStateControllers : Item, ITriggerable
    {
        public const int Limit = 6;

        public override string DefaultName { get { return "Reset State Controllers"; } }

        private List<Item> m_Links; // note: may contain null values
        [CopyableAttribute(CopyType.DoNotCopy)]
        public List<Item> Links
        {
            get { return m_Links; }
            set { m_Links = value; }
        }

        #region Link Accessors

        public override Item Dupe(int amount)
        {
            ResetStateControllers new_relay = new ResetStateControllers();
            Utility.CopyProperties(new_relay, this);
            if (Links != null)
            {   // without this, 'duped' ResetStateControllerss will SHARE a Links list.
                //  Add an item to one, it adds it to the other. Change a value in one, it changes it in the other.
                new_relay.Links = new(Links);
            }

            return base.Dupe(new_relay, amount);
        }

        private Item GetLink(int index)
        {
            if (index >= 0 && index < m_Links.Count)
                return m_Links[index];

            return null;
        }

        private void SetLink(int index, Item value)
        {
            if (index < 0 || index >= Limit)
                return;

            while (index >= m_Links.Count)
                m_Links.Add(null);

            m_Links[index] = value;
        }

        private void AddLink(Item value)
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
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StateController Link1
        {
            get { return (StateController)GetLink(0); }
            set { SetLink(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StateController Link2
        {
            get { return (StateController)GetLink(1); }
            set { SetLink(1, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StateController Link3
        {
            get { return (StateController)GetLink(2); }
            set { SetLink(2, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StateController Link4
        {
            get { return (StateController)GetLink(3); }
            set { SetLink(3, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StateController Link5
        {
            get { return (StateController)GetLink(4); }
            set { SetLink(4, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public StateController Link6
        {
            get { return (StateController)GetLink(5); }
            set { SetLink(5, value); }
        }

        #endregion

        [Constructable]
        public ResetStateControllers()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Links = new List<Item>();
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public virtual bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            if (m_Links.Count == 0)
                return false;

            bool any = false;

            foreach (Item link in m_Links)
            {
                if (link is StateController sc && sc.HasState(from))
                {
                    any = true;
                    break;
                }
            }

            return any;
        }

        public virtual void OnTrigger(Mobile from)
        {
            foreach (Item link in m_Links)
                if (link is StateController sc && sc.HasState(from))
                    sc.ClearState(from);
        }

        #region Context Menu
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new LinkCME());
            }
        }

        private class LinkCME : ContextMenuEntry
        {
            public LinkCME()
                : base(3006173) // Bind
            {
            }

            public override void OnClick()
            {
                BeginTarget(Owner.From, (ResetStateControllers)Owner.Target);
            }

            private static void BeginTarget(Mobile from, ResetStateControllers link)
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(OnTarget), link);
            }

            private static void OnTarget(Mobile from, object targeted, object state)
            {
                ResetStateControllers link = (ResetStateControllers)state;

                if (link.Links.Count >= Limit)
                    return;

                Item item = targeted as Item;

                if (item == null)
                {
                    from.SendLocalizedMessage(1149667); // Invalid target.
                    return;
                }

                if (item is AddonComponent)
                {
                    AddonComponent ac = (AddonComponent)item;

                    if (ac.Addon != null)
                        item = ac.Addon;
                }

                if (!link.Links.Contains(item))
                {
                    link.AddLink(item);

                    from.SendMessage("Link added.");
                }

                if (link.Links.Count < Limit)
                    BeginTarget(from, link);
            }
        }
        #endregion Context Menu

        public ResetStateControllers(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.WriteItemList(m_Links);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Links = reader.ReadStrongItemList();
                        break;
                    }
            }
        }
    }
}