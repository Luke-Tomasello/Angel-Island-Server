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

/* Items/Triggers/Core/TriggerSystem.cs
 * CHANGELOG:
 *  3/13/2024, Adam (spawner.OnTick(external: true))
 *      When triggering an event spawner, we need to inform the spawner this request came 'externally' so that the spawner won't clean up the spawn
 *          on restart if the event has ended.
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    public interface ITriggerable
    {
        bool CanTrigger(Mobile from);

        void OnTrigger(Mobile from);
    }

    public interface ITrigger
    {
        Item Link { get; set; }
        string Event { get; set; }
    }

    public static class TriggerSystem
    {
        public static bool CheckTrigger(Mobile from, Item item)
        {
            if (CanTrigger(from, item))
            {
                OnTrigger(from, item);
                return true;
            }

            return false;
        }

        private static readonly HashSet<RecurseEntry> m_RecurseProtected = new HashSet<RecurseEntry>();

        public static bool CanTrigger(Mobile from, Item item)
        {
            if (item == null || item.Deleted)
                return false;

            RecurseEntry re = new RecurseEntry(0, item);

            if (m_RecurseProtected.Contains(re))
                return true;

            m_RecurseProtected.Add(re);

            bool result;

            if (item is RaisableItem)
            {
                RaisableItem raisableItem = (RaisableItem)item;

                result = raisableItem.IsRaisable;
            }
            else if (item is Spawner)
            {
                Spawner spawner = (Spawner)item;

                spawner.Defrag();

                result = !spawner.IsFull;
            }
            else if (item is ChestItemSpawner cis)
            {
                cis.Defrag();

                result = !cis.IsFull;
            }
            else if (item is Teleporter)
            {
                Teleporter teleporter = (Teleporter)item;

                result = (from != null && teleporter.CanTeleport(from));
            }
            else if (item is ITriggerable)
            {
                ITriggerable triggerable = (ITriggerable)item;

                result = triggerable.CanTrigger(from);
            }
            else
            {
                result = true;
            }

            m_RecurseProtected.Remove(re);

            item.DebugLabelTo(string.Format("({0})", result ? "True" : "False"), hue: result ? 0x44/*green*/ : 0x1C/*red*/);

            return result;
        }
        public static bool CheckEvent(Item item)
        {
            bool result = CheckEventInternal(item);
            if (!result && (item is ITrigger) && (Core.Debug || Core.RuleSets.TestCenterRules()))
                item.SendSystemMessage(string.Format("(Trigger was blocked because of EventSchedule for event {0})", (item as ITrigger).Event));

            return result && !FDFile.FDBackupInProgress;
        }
        private static bool CheckEventInternal(Item item)
        {
            if (item is ITrigger trigger)
                if (EventScheduler.ScheduleExists(trigger.Event))
                    if (EventScheduler.EventSchedulerDatabase[trigger.Event].Active)
                        return true;    // event scheduled and within window
                    else
                        return false;   // event scheduled and outside window

            // if there is no event scheduled, okay to continue;
            return true;
        }
        public static void OnTrigger(Mobile from, Item item)
        {
            if (item == null || item.Deleted)
                return;

            RecurseEntry re = new RecurseEntry(1, item);

            if (m_RecurseProtected.Contains(re))
                return;

            m_RecurseProtected.Add(re);

            if (item is BaseDoor)
            {
                BaseDoor door = (BaseDoor)item;

                door.Open = !door.Open;

                if (door.Link != null)
                    door.Link.Open = door.Open;
            }
            else if (item is BaseLight)
            {
                BaseLight light = (BaseLight)item;

                if (light.Burning)
                    light.Douse();
                else
                    light.Ignite();
            }
            else if (item is EffectController)
            {
                EffectController effectController = (EffectController)item;

                if (from != null)
                    effectController.DoEffect(from);
            }
            else if (item is RaisableItem)
            {
                RaisableItem raisableItem = (RaisableItem)item;

                if (raisableItem.IsRaisable)
                    raisableItem.Raise();
            }
            else if (item is Spawner)
            {
                Spawner spawner = (Spawner)item;

                if (!spawner.IsFull)
                    spawner.OnTick(external: true);
            }
            else if (item is ChestItemSpawner cis)
            {
                if (!cis.IsFull)
                    cis.OnTick();
            }
            //else if (item is Teleporter)
            //{
            //    Teleporter teleporter = (Teleporter)item;

            //    if (from != null && teleporter.CanTeleport(from))
            //        teleporter.StartTeleport(from);
            //}
            else if (item is ITriggerable)
            {
                ITriggerable triggerable = (ITriggerable)item;

                if (triggerable.CanTrigger(from))
                    triggerable.OnTrigger(from);
            }

            m_RecurseProtected.Remove(re);
        }

        public class ActivateCME : ContextMenuEntry
        {
            public ActivateCME(bool enabled)
                : base(3006170) // Activate
            {
                Enabled = enabled;
            }

            public override void OnClick()
            {
                CheckTrigger(Owner.From, (Item)Owner.Target);
            }
        }

        public class LinkCME : ContextMenuEntry
        {
            public LinkCME()
                : base(3006173) // Bind
            {
            }

            public override void OnClick()
            {
                if (Owner.Target is ITrigger)
                    BeginTarget(Owner.From, (ITrigger)Owner.Target);
            }

            private static void BeginTarget(Mobile from, ITrigger trigger)
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(OnTarget), trigger);
            }

            private static void OnTarget(Mobile from, object targeted, object state)
            {
                ITrigger trigger = (ITrigger)state;

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

                trigger.Link = item;
            }
        }

        private struct RecurseEntry : IEquatable<RecurseEntry>
        {
            private int m_Type;
            private Item m_Item;

            public RecurseEntry(int type, Item item)
            {
                m_Type = type;
                m_Item = item;
            }

            public override bool Equals(object obj)
            {
                return (obj is TextEntry && this.Equals((RecurseEntry)obj));
            }

            public bool Equals(RecurseEntry other)
            {
                return (m_Type == other.m_Type && m_Item == other.m_Item);
            }

            public override int GetHashCode()
            {
                return (m_Type.GetHashCode() & m_Item.GetHashCode());
            }
        }
    }
}