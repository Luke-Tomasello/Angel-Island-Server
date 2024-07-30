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

/* Items/Misc/TrashChest.cs
 * ChangeLog:
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  10/24/07 Taran Kain
 *      Added a 3-minute delay for deleting items, a la TrashBarrel
 *	9/21/06, Adam
 *		Change the graphic to a barrel from a chest.
 *		Replace the talking chest with a chest that notifies a nearby NPC of your good deed.
 *  02/17/05, mith
 *		Changed base object from Container to BaseContainer to include fixes made there that override
 *			functionality in the core.
 */

using Server.Diagnostics;
using System;
using System.Collections;

namespace Server.Items
{
    public class TrashChest : BaseContainer
    {
        public override int MaxWeight { get { return 0; } } // A value of 0 signals unlimited weight
        public override int DefaultGumpID { get { return 0x3E; } }
        public override int DefaultDropSound { get { return 0x42; } }

        // not serialized
        private Mobile m_LastPlayer = null;
        private Timer m_EmptyTimer = null;

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        [Constructable]
        public TrashChest()
            : base(0xE77)
        {
            Movable = false;
            Weight = 1;
            Name = "trash";
        }

        public TrashChest(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:             // morph to a barrel graphic
                                    // nothing to do
                    goto case 0;

                case 0:
                    if (this.ItemID == 0xE41)
                        this.ItemID = 0xE77;

                    if (Name == null)
                        Name = "trash";
                    break;
            }

            if (Items.Count > 0)
                m_EmptyTimer = Timer.DelayCall(TimeSpan.FromMinutes(3.0), new TimerCallback(Empty));
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            DateTime LastAccess = this.LastAccessed;

            if (!base.OnDragDrop(from, dropped))
                return false;

            if (!Core.EraAccurate)
                DisplayMessage(from, dropped, LastAccess);

            if (m_EmptyTimer == null)
                m_EmptyTimer = Timer.DelayCall(TimeSpan.FromMinutes(3.0), new TimerCallback(Empty));

            return true;
        }

        public override bool OnDragDropInto(Mobile from, Item dropped, Point3D p)
        {
            DateTime LastAccess = this.LastAccessed;

            if (!base.OnDragDropInto(from, dropped, p))
                return false;

            if (!Core.EraAccurate)
                DisplayMessage(from, dropped, LastAccess);

            if (m_EmptyTimer == null)
                m_EmptyTimer = Timer.DelayCall(TimeSpan.FromMinutes(3.0), new TimerCallback(Empty));

            return true;
        }

        public override bool IsAccessibleTo(Mobile m)
        {
            return true;
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            DropItem(dropped);
            return true;
        }

        public void DisplayMessage(Mobile from, Item item, DateTime LastAccess)
        {
            string text = null;

            try
            {
                // do not print a message more than once per minute, or twice for the same player
                bool timeout = DateTime.UtcNow - LastAccess > TimeSpan.FromMinutes(1.0);
                if (timeout == true && from != m_LastPlayer)
                    m_LastPlayer = from;
                else
                    return;

                // select a 'thank you' message
                switch (Utility.Random(15))
                {
                    case 0:
                        text = String.Format("Good job {0}.", from.Name);
                        break;
                    case 1:
                        text = String.Format("Thank you for helping to keep our city clean.");
                        break;
                    case 2:
                        text = String.Format("Thank you {0} for helping to keep our city clean.", from.Name);
                        break;
                    case 3:
                        text = String.Format("Thank you!");
                        break;
                    case 4:
                        text = String.Format("Thank you {0}!", from.Name);
                        break;
                    case 5:
                        text = String.Format("Thank you, {0}, would you mind emptying that for me now?", from.Name);
                        break;
                    case 6:
                        text = String.Format("Thank you, would you mind emptying that for me now?");
                        break;
                    case 7:
                        text = String.Format("{0}, What was that you just threw away?", from.Name);
                        break;
                    case 8:
                        text = String.Format("What was that you just threw away?");
                        break;
                    case 9:
                        text = String.Format("That's a good place for that.");
                        break;
                    case 10:
                        text = String.Format("That's a good place for that, {0}.", from.Name);
                        break;
                    case 11:
                        text = String.Format("Aw, I wanted that.");
                        break;
                    case 12:
                        text = String.Format("Aw, I wanted that, {0}.", from.Name);
                        break;
                    case 13:
                        text = String.Format("Thank you for disposing of such a foul item. Many thanks.");
                        break;
                    case 14:
                        text = String.Format("Thank you for disposing of such a foul item. Many thanks, {0}.", from.Name);
                        break;
                }

                ArrayList selected = new ArrayList();

                // find an NPC to speak
                IPooledEnumerable eable = from.GetMobilesInRange(10);
                foreach (Mobile m in eable)
                {
                    // no weirdness
                    if (m == null || m.Deleted || m.Alive == false || m.IsDeadBondedPet)
                        continue;

                    // only humans NPCs say 'thank you'
                    if (m.Body.IsHuman == false || m.Player == true)
                        continue;

                    // make sure they can see one another
                    if (m.CanSee(from) == false || from.CanSee(m) == false)
                        continue;

                    selected.Add(m);
                }
                eable.Free();

                // send text to nearby mob
                if (selected.Count > 0)
                {
                    Mobile mx = selected[Utility.Random(selected.Count)] as Mobile;
                    mx.Say(text);
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in DisplayMessage(): " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }

        private new void Empty()
        {
            try
            {
                while (Items.Count > 0)
                {
                    Item i = Items[0] as Item;
                    i.Delete();
                }

                if (m_EmptyTimer != null)
                    m_EmptyTimer.Stop();

                m_EmptyTimer = null;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in Empty(): " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }
    }
}