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

/* Items/Misc/Key.cs
 * CHANGELOG:
 *  1/16/11, Pix
 *      Made Boat key location only show on AngelIsland
 *	10/5/07, Adam
 *		You cannot copy magic keys
 *		You cannot rename magic keys
 *  06/08/07, plasma,
 *      Fix OnSingleClick so it actually displays the co-ords of the boat not the player (lol)
 *  5/19/07, Adam
 *      Add support for lockable private houses
 *  03/28/07, plasma,
 *      Allowed OnSingleClick to display the co-ords of a boat to the owner.
 *	11/05/04, Darva
 *			Fixed a typo that caused the unlock message to appear at the wrong
 *			time.
 *    10/23/04, Darva
 *			Removed previous changes.
 *			Added code to prevent locking public houses, but allow currently locked public
 *			houses to be unlocked.
 *    10/20/04, Darva
 *			Made it so containers will not enable their traps while on a 
 *			player vendor, even when locked.
 *	8/26/04, Pix
 *		Made it so keys and keyrings must be in your pack to use.
 *	7/15/04, Pix
 *		Added check to key copying to make sure that key is in the backpack
 *		of the person trying to copy it.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  5/18/2004, pixie
 *		Fixed re-enabling of tinker-made traps
 *	5/18/2004, pixie
 *		Added handling of (un)locking/(dis)abling of tinker made traps
 */

using Server.Multis;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
namespace Server.Items
{
    public enum KeyType
    {
        Copper = 0x100E,
        Gold = 0x100F,
        Iron = 0x1010,
        Rusty = 0x1013,
        Magic = 0x1012
    }

    public interface ILockable
    {
        bool Locked { get; set; }
        uint KeyValue { get; set; }
    }

    public class Key : Item
    {
        private string m_Description;
        private uint m_KeyVal;
        private Item m_Link;
        private int m_MaxRange;

        public static uint RandomValue()
        {
            return (uint)(0xFFFFFFFE * Utility.RandomDouble()) + 1;
        }

        public static void RemoveKeys(Mobile m, uint keyValue)
        {
            if (keyValue == 0)
                return;

            Container pack = m.Backpack;

            if (pack != null)
            {
                Item[] keys = pack.FindItemsByType(typeof(Key), true);

                foreach (Key key in keys)
                    if (key.KeyValue == keyValue)
                        key.Delete();
            }

            BankBox box = m.BankBox;

            if (box != null)
            {
                Item[] keys = box.FindItemsByType(typeof(Key), true);

                foreach (Key key in keys)
                    if (key.KeyValue == keyValue)
                        key.Delete();
            }
        }

        public KeyType Type
        {
            get { return (KeyType)ItemID; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Description
        {
            get
            {
                return m_Description;
            }
            set
            {
                m_Description = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxRange
        {
            get
            {
                return m_MaxRange;
            }

            set
            {
                m_MaxRange = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public uint KeyValue
        {
            get
            {
                return m_KeyVal;
            }

            set
            {
                m_KeyVal = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get
            {
                return m_Link;
            }

            set
            {
                m_Link = value;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((int)m_MaxRange);

            writer.Write((Item)m_Link);

            writer.Write((string)m_Description);
            writer.Write((uint)m_KeyVal);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_MaxRange = reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Link = reader.ReadItem();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 2 || m_MaxRange == 0)
                            m_MaxRange = 3;

                        m_Description = reader.ReadString();

                        m_KeyVal = reader.ReadUInt();

                        break;
                    }
            }
        }

        [Constructable]
        public Key()
            : this(KeyType.Copper, 0)
        {
        }

        [Constructable]
        public Key(KeyType type)
            : this(type, 0)
        {
        }

        [Constructable]
        public Key(uint val)
            : this(KeyType.Copper, val)
        {
        }

        [Constructable]
        public Key(KeyType type, uint LockVal)
            : this(type, LockVal, null)
        {
            m_KeyVal = LockVal;
        }

        public Key(KeyType type, uint LockVal, Item link)
            : base((int)type)
        {
            Weight = 1.0;

            m_MaxRange = 3;
            m_KeyVal = LockVal;
            m_Link = link;
        }

        public Key(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            Target t;
            int number;

            if (m_KeyVal != 0)
            {
                number = 501662; // What shall I use this key on?
                t = new UnlockTarget(this);
            }
            else
            {
                number = 501663; // This key is a key blank. Which key would you like to make a copy of?
                t = new CopyTarget(this);
            }

            from.SendLocalizedMessage(number);
            from.Target = t;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            string desc;

            if (m_KeyVal == 0)
                desc = "(blank)";
            else if ((desc = m_Description) == null || (desc = desc.Trim()).Length <= 0)
                desc = null;

            if (desc != null)
                list.Add(desc);
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            string desc = string.Empty;

            if (m_KeyVal == 0)
                desc = "(blank)";
            else if (Link is BaseBoat && ((BaseBoat)Link).Owner == from && (Core.RuleSets.AngelIslandRules()))
            {
                //plasma: display boat's co-ords to owner (stole/modified from Sextant.cs)
                int xLong = 0, yLat = 0;
                int xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;

                if (Sextant.Format(((BaseBoat)Link).Location, from.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
                {
                    desc = "Your boat is situated at " + Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                    from.LocalOverheadMessage(MessageType.Regular, from.SpeechHue, false, desc);
                    return;
                }
            }
            else if ((desc = m_Description) == null || (desc = desc.Trim()).Length <= 0)
                desc = "";


            if (desc.Length > 0)
                from.Send(new UnicodeMessage(Serial, ItemID, MessageType.Regular, 0x3B2, 3, "ENU", "", desc));
        }

        private class RenamePrompt : Prompt
        {
            private Key m_Key;

            public RenamePrompt(Key key)
            {
                m_Key = key;
            }

            public override void OnResponse(Mobile from, string text)
            {
                m_Key.Description = Utility.FixHtml(text);
            }
        }

        private class UnlockTarget : Target
        {
            private Key m_Key;

            public UnlockTarget(Key key)
                : base(key.MaxRange, false, TargetFlags.None)
            {
                m_Key = key;
                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                int number;

                // rename the key
                if (targeted == m_Key)
                {   // cannot rename magic keys
                    if (m_Key.Type != KeyType.Magic)
                    {
                        number = 501665; // Enter a description for this key.
                        from.Prompt = new RenamePrompt(m_Key);
                    }
                    else
                        return;
                }
                else if (targeted is ILockable)
                {
                    number = -1;

                    ILockable o = (ILockable)targeted;

                    if (o.KeyValue == m_Key.KeyValue)
                    {
                        if (o is BaseDoor && !((BaseDoor)o).UseLocks())
                        {
                            number = 501668; // This key doesn't seem to unlock that.
                        }
                        else if (o is BaseDoor && !(m_Key.IsChildOf(from.Backpack)))
                        {
                            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                        }
                        else
                        {
                            if (o is BaseHouseDoor)
                            {
                                BaseHouse home;
                                home = ((BaseHouseDoor)o).FindHouse();
                                /*if (home.Public == true)
								{
									if (o.Locked != true)
									from.SendMessage("You cannot lock a public house.");
									o.Locked = false;
									return;
								}*/
                            }

                            // lock/unlock the door
                            o.Locked = !o.Locked;

                            // 5/28/2021, Adam
                            //	If the door lock was rusty, destroy the key (single use)
                            //	Used at the Angel Island guard tower
                            if ((o is BaseDoor) && (o as BaseDoor).RustyLock == true)
                            {
                                from.SendMessage("The lock was rusty and the key broke off in your fingers.");
                                from.PlaySound(0x3A4);
                                Key.RemoveKeys(from, m_Key.KeyValue);
                            }

                            if (o is LockableContainer)
                            {
                                LockableContainer cont = (LockableContainer)o;
                                if (UnusualContainerSpawner.IsUnusualContainer(cont))
                                {
                                    from.SendMessage("The lock was rusty and the key broke off in your fingers.");
                                    from.PlaySound(0x3A4);
                                    m_Key.Delete();
                                }
                                #region trap stuff
                                if (cont.TrapType != TrapType.None)
                                {
                                    if (cont.Locked)
                                    {
                                        if (Core.NewStyleTinkerTrap)    // last person to lock trap is 'owner'
                                            cont.Owner = from;

                                        cont.TrapEnabled = true;
                                        (o as LockableContainer).SendLocalizedMessageTo(from, 501673); // You re-enable the trap.
                                    }
                                    else
                                    {
                                        cont.TrapEnabled = false;
                                        (o as LockableContainer).SendLocalizedMessageTo(from, 501672); // You disable the trap temporarily.  Lock it again to re-enable it.
                                    }
                                }
                                #endregion trap stuff
                                /// gosh, I wish someone had left a comment - adam :/
                                if (cont.LockLevel == -255)
                                    cont.LockLevel = cont.RequiredSkill - 10;
                            }

                            if (targeted is Item)
                            {
                                Item item = (Item)targeted;

                                if (o.Locked)
                                    item.SendLocalizedMessageTo(from, 1048000); // "You lock it."

                                else
                                    item.SendLocalizedMessageTo(from, 1048001); // "You unlock it."
                            }
                        }
                    }
                    else
                    {
                        number = 501668; // This key doesn't seem to unlock that.
                    }
                }
                else
                {
                    number = 501666; // You can't unlock that!
                }

                if (number != -1)
                {
                    if ((targeted is GuardPostDoor))
                        (targeted as GuardPostDoor).FailUnlockMessage(from, number);
                    else
                        from.SendLocalizedMessage(number);
                }
            }
        }

        private class CopyTarget : Target
        {
            private Key m_Key;

            public CopyTarget(Key key)
                : base(3, false, TargetFlags.None)
            {
                m_Key = key;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                int number;

                if (targeted is Key)
                {
                    Key k = (Key)targeted;

                    if (!k.IsChildOf(from.Backpack))
                    {
                        from.SendLocalizedMessage(501661); // That key is unreachable.
                        return;
                    }

                    if (k.Type == KeyType.Magic)
                    {
                        from.SendLocalizedMessage(501677); // You fail to make a copy of the key.
                        return;
                    }

                    if (k.m_KeyVal == 0)
                    {
                        number = 501675; // This key is also blank.
                    }
                    else if (from.CheckTargetSkill(SkillName.Tinkering, k, 0, 75.0, new object[2] /*contextObj*/))
                    {
                        number = 501676; // You make a copy of the key.

                        m_Key.Description = k.Description;
                        m_Key.KeyValue = k.KeyValue;
                        m_Key.Link = k.Link;
                        m_Key.MaxRange = k.MaxRange;
                    }
                    else if (Utility.RandomDouble() <= 0.1) // 10% chance to destroy the key
                    {
                        from.SendLocalizedMessage(501677); // You fail to make a copy of the key.

                        number = 501678; // The key was destroyed in the attempt.

                        m_Key.Delete();
                    }
                    else
                    {
                        number = 501677; // You fail to make a copy of the key.
                    }
                }
                else
                {
                    number = 501688; // Not a key.
                }

                from.SendLocalizedMessage(number);
            }
        }
    }
}