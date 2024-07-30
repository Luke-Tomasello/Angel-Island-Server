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

/* Items/Misc/KeyRing.cs
 * ChangeLog:
 *	5/27/2021, Adam (OnDelete())
 *		Add OnDelete to cleanup keys on the internal map.
 *		Note: A patch has already been run to cleanup these orphaned keys.
 *	3/16/11,
 *		move the keys from the 'container' (old implementation) to the key list.
 *		we do this because when Razor sees an item.Items.Count > 0, it assumes it's a container and tries to inventory it
 * 		this inventory action causes the double click action which invokes the target cursor (ugly)
 *  5/19/07, Adam
 *      Add support for lockable private houses
 *	11/05/04, Darva
 *			Fixed locking public houses error message.
 *	10/29/04 - Pix
 *		Made keys with keyvalue 0 (blank) not lock/unlock doors when they're on keyrings.
 *    10/23/04, Darva
 *			Added code to prevent locking public houses, but allow currently locked public
 *			houses to be unlocked.
 *	9/4/04, mith
 *		OnDragDrop(): Copied Else block from Spellbook, to prevent people dropping things on book to have it bounce back to original location.
 *	8/26/04, Pix
 *		Made it so keys and keyrings must be in your pack to use.
 *	6/24/04, Pix
 *		KeyRing change - contained keys don't decay (set to non-movable when put on keyring,
 *		and movable when taken off).
 *		Also - GM+ can view the contents of a keyring.
 *	5/18/2004
 *		Added handling of (un)locking/(dis)abling of tinker made traps
 *	5/02/2004, pixie
 *		Changed to be a container...
 *		Now you can doubleclick the keyring, target the keyring, and it'll dump all the keys
 *		into your pack akin to OSI.
 *   4/26/2004, pixie
 *     Initial Version
 */

#define current

using Server.Mobiles;
using Server.Targeting;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
#if current
    /// <summary>
    /// Summary description for KeyRing.
    /// </summary>
    public class KeyRing : BaseContainer
    {
        private const int ZEROKEY_ITEMID = 0x1011;
        private const int ONEKEY_ITEMID = 0x1769;
        private const int THREEKEY_ITEMID = 0x176A;
        private const int MANYKEY_ITEMID = 0x176B;
        private const int MAX_KEYS = 20;

        private List<Key> m_Keys = new List<Key>();
        public List<Key> Keys { get { return m_Keys; } }

        [Constructable]
        public KeyRing()
            : base(0x1011)
        {
            Weight = 1;
        }

        public KeyRing(Serial serial)
            : base(serial)
        {
        }

        public bool IsKeyOnRing(uint keyid)
        {
            bool bReturn = false;

            foreach (Key i in m_Keys)
            {
                if (i is Key)
                {
                    Key k = i;
                    if (keyid == k.KeyValue)
                    {
                        bReturn = true;
                        break;
                    }
                }
            }

            #region Fix Bugged key rings
            // remove deleted keys. Happened due to a bug
            ArrayList remove = new ArrayList(0);
            foreach (Key i in m_Keys)
                if (i is Key && i.Deleted)
                    remove.Add(i);

            foreach (object key in remove)
                m_Keys.Remove(key as Key);
            UpdateItemID();
            #endregion

            return bReturn;
        }

        public bool IsKeyOnRing(Serial keyid)
        {
            bool bReturn = false;

            foreach (Key i in m_Keys)
            {
                if (i is Key)
                {
                    Key k = i;
                    if (keyid == k.Serial)
                    {
                        bReturn = true;
                        break;
                    }
                }
            }

            return bReturn;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxRange
        {
            get { return 3; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Count
        {
            get { return m_Keys.Count; }
        }

        public void UpdateItemID()
        {
            if (Count == 0)
                ItemID = ZEROKEY_ITEMID;
            else if (Count == 1 || Count == 2)
                ItemID = ONEKEY_ITEMID;
            else if (Count == 3 || Count == 4)
                ItemID = THREEKEY_ITEMID;
            else if (Count > 4)
                ItemID = MANYKEY_ITEMID;
        }

        public override void OnDelete()
        {
            foreach (Key key in m_Keys)
            {
                if (key != null && !key.Deleted)
                {
                    key.IsIntMapStorage = false;
                    key.Delete();
                }
            }
            m_Keys.Clear();
            base.OnDelete();
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {

            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
                return false;
            }

            bool bReturn = false;
            Key key = item as Key;

            if (key == null || key.KeyValue == 0)
            {
                from.SendLocalizedMessage(501689); // Only non-blank keys can be put on a keyring.
                return false;
            }
            else if (Count < MAX_KEYS)
            {
#if false
				m_Keys.Add(key);
				key.MoveItemToIntStorage(false);    // put in protected storage
				UpdateItemID();
#else
                AddKey(key);
#endif
                from.SendLocalizedMessage(501691); // You put the key on the keyring.
                bReturn = true;
            }
            else
            {
                from.SendLocalizedMessage(1008138); // This keyring is full.
                bReturn = false;
            }

            return bReturn;
        }

        public override void OnDoubleClick(Mobile from)
        {

            if (!this.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            Target t;

            if (Count > 0)
            {
                t = new RingUnlockTarget(this);
                from.SendLocalizedMessage(501680); // What do you want to unlock?
                from.Target = t;
            }
            else
            {
                from.SendMessage("The keyring contains no keys");
            }
        }

        public override bool DisplaysContent { get { return false; } }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            string descr = "";
            if (Count == 1)
            {
                descr = string.Format("{0} key", Count);
            }
            else if (Count > 1)
            {
                descr = string.Format("{0} keys", Count);
            }
            else
            {
                descr = "Empty";
            }

            this.LabelTo(from, descr);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            string descr = "";
            if (Count == 1)
            {
                descr = string.Format("{0} key", Count);
            }
            else if (Count > 1)
            {
                descr = string.Format("{0} keys", Count);
            }
            else
            {
                descr = "Empty";
            }

            if (descr != null)
                list.Add(descr);
        }
#if false
		public override bool CanFreezeDry { get { return true; } }
#endif
        public void AddKey(Key key)
        {
            m_Keys.Add(key);
            key.MoveToIntStorage(false);    // put in protected storage
            UpdateItemID();
        }
        public void DeleteKey(uint keyvalue)
        {
            List<Key> list = new List<Key>(Keys);
            foreach (Key i in list)
            {
                if (i is Key)
                {
                    if (i.KeyValue == keyvalue)
                    {
                        if (Keys.Contains(i))
                        {
                            Keys.Remove(i);
                            if (i.IsIntMapStorage)
                                i.IsIntMapStorage = false;
                            i.Delete();
                        }
                        // only delete the first one of these keys
                        UpdateItemID();
                        return;
                    }
                }
            }
        }
        public void RemoveKey(uint keyvalue)
        {
            List<Key> list = new List<Key>(Keys);
            foreach (Key i in list)
            {
                if (i is Key)
                {
                    if (i.KeyValue == keyvalue)
                    {
                        if (Keys.Contains(i))
                        {
                            Keys.Remove(i);
                            if (i.IsIntMapStorage)
                                i.IsIntMapStorage = false;
                        }
                        // only remove the first one of these keys
                        UpdateItemID();
                        return;
                    }
                }
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version of new keyring

            // version 1
            writer.WriteItemList<Key>(m_Keys);

            // version 0 (obsolete in version 1)
            // writer.Write((int)m_MaxRange);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Keys = reader.ReadStrongItemList<Key>();
                        // no goto 0 here as the reading of m_MaxRange is now obsolete
                        break;
                    }

                case 0:
                    {
                        int m_MaxRange = reader.ReadInt();  // obsolete in version 1

                        // move the keys from the 'container' (old implementation) to the key list.
                        //	we do this because when Razor sees an item.Items.Count > 0, it assumes it's a container and tries to inventory it
                        //	this inventory action causes the double click action which invokes the target cursor (ugly)
                        if (this.Items != null)
                        {
                            Item[] keys = this.FindItemsByType(typeof(Key));
                            foreach (Key key in keys)
                            {
                                key.Movable = true;         // old implementation had these movable = false
                                this.RemoveItem(key);
                                this.m_Keys.Add(key);
                            }
                        }

                        break;
                    }
            }
        }


        private class RingUnlockTarget : Target
        {
            private KeyRing m_KeyRing;

            public RingUnlockTarget(KeyRing keyring)
                : base(keyring.MaxRange, false, TargetFlags.None)
            {
                m_KeyRing = keyring;
                CheckLOS = false;
            }
#if false
			private void DeleteKey(uint keyvalue)
			{
				List<Key> list = new List<Key>(m_KeyRing.Keys);
				foreach (Key i in list)
				{
					if (i is Key)
					{
						if (i.KeyValue == keyvalue)
						{
							if (m_KeyRing.Keys.Contains(i))
							{
								m_KeyRing.Keys.Remove(i);
								if (i.IsIntMapStorage)
									i.IsIntMapStorage = false;
								i.Delete();
							}
							// only delete the first one of these keys
							m_KeyRing.UpdateItemID();
							return;
						}
					}
				}
			}
#endif
            protected override void OnTarget(Mobile from, object targeted)
            {

                if (m_KeyRing.Deleted || !m_KeyRing.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    return;
                }

                int number;

                if (targeted == m_KeyRing)
                {
                    number = -1;
                    //remove keys from keyring
                    List<Key> list = new List<Key>(m_KeyRing.Keys);
                    foreach (Key i in list)
                    {
                        if (i is Key)
                        {
                            if (from is PlayerMobile)
                            {
                                Container b = ((PlayerMobile)from).Backpack;
                                if (b != null)
                                {
                                    if (m_KeyRing.Keys.Contains(i))
                                    {
                                        m_KeyRing.Keys.Remove(i);
                                        //b.DropItem(i);
                                        i.RetrieveItemFromIntStorage(from); // retrieve from safe storage
                                    }
                                }
                            }
                        }
                    }
                    m_KeyRing.UpdateItemID();
                    from.SendMessage("You remove all the keys.");
                }
                else if (targeted is ILockable)
                {
                    number = -1;
                    ILockable o = (ILockable)targeted;

                    if (m_KeyRing.IsKeyOnRing(o.KeyValue) && o.KeyValue != 0)
                    {
                        if (o is BaseDoor && !((BaseDoor)o).UseLocks())
                        {
                            //number = 501668;	// This key doesn't seem to unlock that.
                            number = 1008140;   // You do not have a key for that.
                        }
                        else
                        {

                            #region PUBLIC HOUSE (disabled)
                            /*if (o is BaseHouseDoor)
							{
								BaseHouse home;
								home = ((BaseHouseDoor)o).FindHouse();
								if (home.Public == true)
								{
									if (o.Locked != true)
									from.SendMessage("You cannot lock a public house.");
									o.Locked = false;
									return;
								}
							}*/
                            #endregion

                            o.Locked = !o.Locked;

                            if (o is LockableContainer)
                            {
                                LockableContainer cont = (LockableContainer)o;

                                if (UnusualContainerSpawner.IsUnusualContainer(cont))
                                {
                                    from.SendMessage("The lock was rusty and the key was destroyed.");
                                    from.PlaySound(0x3A4);
                                    m_KeyRing.DeleteKey(cont.KeyValue);
                                }
                                else if (Core.OldStyleTinkerTrap)
                                {   // old-school traps (< publish 4)
                                    if (cont.TrapEnabled)
                                    {
                                        from.SendMessage("You leave the trap enabled.");
                                    }
                                    else
                                    {   // only give a message if trapped (even if it's disabled.)
                                        if (cont.TrapType != TrapType.None)
                                            from.SendMessage("You leave the trap disabled.");
                                    }
                                }
                                else
                                {   // new-style traps (>= publish 4)
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
                                }

                                if (cont.LockLevel == -255)
                                    cont.LockLevel = cont.RequiredSkill - 10;
                            }

                            if (targeted is Item)
                            {
                                Item item = (Item)targeted;

                                if (o.Locked)
                                    item.SendLocalizedMessageTo(from, 1048000);
                                else
                                    item.SendLocalizedMessageTo(from, 1048001);
                            }
                        }
                    }
                    else
                    {
                        //number = 501668;	// This key doesn't seem to unlock that.
                        number = 1008140;   // You do not have a key for that.
                    }
                }
                else
                {
                    number = 501666; // You can't unlock that!
                }

                if (number != -1)
                {
                    from.SendLocalizedMessage(number);
                }
            }
        }//end RingUnlockTarget

    }
#elif old
	/// <summary>
	/// Summary description for KeyRing.
	/// </summary>
	public class KeyRing : BaseContainer
	{
		private const int ZEROKEY_ITEMID = 0x1011;
		private const int ONEKEY_ITEMID = 0x1769;
		private const int THREEKEY_ITEMID = 0x176A;
		private const int MANYKEY_ITEMID = 0x176B;
		private const int MAX_KEYS = 10;
		private int m_MaxRange;

		private ArrayList m_Keys;


		[Constructable]
		public KeyRing()
			: base(0x1011)
		{
			//
			// TODO: Add constructor logic here
			//
			m_Keys = new ArrayList(MAX_KEYS);
			m_MaxRange = 3;
		}

		public KeyRing(Serial serial)
			: base(serial)
		{
		}

		public bool IsKeyOnRing(uint keyid)
		{
			bool bReturn = false;

			Item[] keys = FindItemsByType(typeof(Key));
			foreach (Item i in keys)
			{
				if (i is Key)
				{
					Key k = (Key)i;
					if (keyid == k.KeyValue)
					{
						bReturn = true;
						break;
					}
				}
			}

			return bReturn;
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int MaxRange
		{
			get { return m_MaxRange; }
			set { m_MaxRange = value; }
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int Count
		{
			get { return GetAmount(typeof(Key)); }
		}

		public void UpdateItemID()
		{
			if (Count == 0)
				ItemID = ZEROKEY_ITEMID;
			else if (Count == 1 || Count == 2)
				ItemID = ONEKEY_ITEMID;
			else if (Count == 3 || Count == 4)
				ItemID = THREEKEY_ITEMID;
			else if (Count > 4)
				ItemID = MANYKEY_ITEMID;
		}

		public override bool OnDragDrop(Mobile from, Item item)
		{
			bool bReturn = false;
			if (item is Key)
			{
				if (Count < MAX_KEYS)
				{
					bReturn = base.OnDragDrop(from, item);
					if (bReturn)
					{
						item.Movable = false;
					}
					UpdateItemID();
				}
				else
				{
					from.SendMessage("That key can't fit on that key ring.");
					bReturn = false;
				}
			}
			else
			{
				// Adam: anything other than a key will get dropped into your backpack
				// (so your best sword doesn't get dropped on the ground.)
				from.AddToBackpack(item);
				//	For richness, we add the drop sound of the item dropped.
				from.PlaySound(item.GetDropSound());
				return true;
			}

			return bReturn;
		}

		public override void OnDoubleClick(Mobile from)
		{
			Target t;

			if (from.AccessLevel >= AccessLevel.GameMaster)
			{
				base.OnDoubleClick(from);
			}

			if (Count > 0)
			{
				t = new RingUnlockTarget(this);
				from.SendMessage("What do you wish to unlock?");
				from.Target = t;
			}
			else
			{
				from.SendMessage("The keyring contains no keys");
			}
		}

		public override void OnSingleClick(Mobile from)
		{
			base.OnSingleClick(from);

			string descr = "";
			if (Count > 0)
			{
				descr = string.Format("{0} keys", Count);
			}
			else
			{
				descr = "Empty";
			}

			this.LabelTo(from, descr);
		}

		public override void GetProperties(ObjectPropertyList list)
		{
			base.GetProperties(list);
			if (Count > 0)
			{
				string descr = string.Format("{0} Keys", Count);
				list.Add(descr);
			}
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int)0); // version
			writer.Write((int)m_MaxRange);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();

			m_MaxRange = reader.ReadInt();
			if (m_MaxRange == 0)
			{
				m_MaxRange = 3;
			}
		}


		private class RingUnlockTarget : Target
		{
			private KeyRing m_KeyRing;

			public RingUnlockTarget(KeyRing keyring)
				: base(keyring.MaxRange, false, TargetFlags.None)
			{
				m_KeyRing = keyring;
				CheckLOS = false;
			}

			protected override void OnTarget(Mobile from, object targeted)
			{
				int number;

				if (targeted == m_KeyRing)
				{
					number = -1;
					//remove keys from keyring
					Item[] keys = m_KeyRing.FindItemsByType(typeof(Key));
					foreach (Item i in keys)
					{
						if (i is Key) //doublecheck!
						{
							if (from is PlayerMobile)
							{
								Container b = ((PlayerMobile)from).Backpack;
								if (b != null)
								{
									b.DropItem(i);
									i.Movable = true;
								}
							}
						}
					}
					m_KeyRing.UpdateItemID();
					from.SendMessage("You remove all the keys.");
				}
				else if (targeted is ILockable)
				{
					number = -1;

					ILockable o = (ILockable)targeted;

					if (m_KeyRing.IsKeyOnRing(o.KeyValue) && o.KeyValue != 0)
					{
						if (o is BaseDoor && !((BaseDoor)o).UseLocks())
						{
							number = 501668; // This key doesn't seem to unlock that.
						}
						else if (o is BaseDoor && !(m_KeyRing.IsChildOf(from.Backpack)))
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

							o.Locked = !o.Locked;

							if (o is LockableContainer)
							{
								LockableContainer cont = (LockableContainer)o;

								if (false)
								{
									if (cont.TrapType != TrapType.None)
									{
										if (cont.TrapEnabled)
										{
											from.SendMessage("You leave the trap enabled.");
										}
										else
										{
											from.SendMessage("You leave the trap disabled.");
										}
									}
								}

								if (cont.LockLevel == -255)
									cont.LockLevel = cont.RequiredSkill - 10;
							}

							if (targeted is Item)
							{
								Item item = (Item)targeted;

								if (o.Locked)
									item.SendLocalizedMessageTo(from, 1048000);
								else
									item.SendLocalizedMessageTo(from, 1048001);
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
					from.SendLocalizedMessage(number);
				}
			}
		}//end RingUnlockTarget

	}
#else
	/// <summary>
	/// Summary description for KeyRing.
	/// </summary>
	public class KeyRing : Item
	{
		private const int ZEROKEY_ITEMID = 0x1011;
		private const int ONEKEY_ITEMID = 0x1769;
		private const int THREEKEY_ITEMID = 0x176A;
		private const int MANYKEY_ITEMID = 0x176B;
		private const int MAX_KEYS = 20;
		private int m_MaxRange;

		[Constructable]
		public KeyRing()
			: base(0x1011)
		{
			m_MaxRange = 3;
			Weight = 1;
		}

		public KeyRing(Serial serial)
			: base(serial)
		{
		}

		public bool IsKeyOnRing(uint keyid)
		{
			bool bReturn = false;

			foreach (Item i in this.Items)
			{
				if (i is Key)
				{
					Key k = (Key)i;
					if (keyid == k.KeyValue)
					{
						bReturn = true;
						break;
					}
				}
			}

			return bReturn;
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int MaxRange
		{
			get { return m_MaxRange; }
			set { m_MaxRange = value; }
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public int Count
		{
			get { return this.Items == null ? 0 : this.Items.Count; }
		}

		public void UpdateItemID()
		{
			if (Count == 0)
				ItemID = ZEROKEY_ITEMID;
			else if (Count == 1 || Count == 2)
				ItemID = ONEKEY_ITEMID;
			else if (Count == 3 || Count == 4)
				ItemID = THREEKEY_ITEMID;
			else if (Count > 4)
				ItemID = MANYKEY_ITEMID;
		}

		public override bool OnDragDrop(Mobile from, Item item)
		{

			if (!this.IsChildOf(from.Backpack))
			{
				from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
				return false;
			}

			bool bReturn = false;
			Key key = item as Key;

			if (key == null || key.KeyValue == 0)
			{
				from.SendLocalizedMessage(501689); // Only non-blank keys can be put on a keyring.
				return false;
			}
			else if (Count < MAX_KEYS)
			{
				this.AddItem(item);
				item.Movable = false;
				UpdateItemID();
				from.SendLocalizedMessage(501691); // You put the key on the keyring.
				bReturn = true;
			}
			else
			{
				from.SendLocalizedMessage(1008138); // This keyring is full.
				bReturn = false;
			}

			return bReturn;
		}

		public override void OnDoubleClick(Mobile from)
		{

			if (!this.IsChildOf(from.Backpack))
			{
				from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
				return;
			}

			Target t;

			if (Count > 0)
			{
				t = new RingUnlockTarget(this);
				from.SendLocalizedMessage(501680); // What do you want to unlock?
				from.Target = t;
			}
			else
			{
				from.SendMessage("The keyring contains no keys");
			}
		}

		public override void OnSingleClick(Mobile from)
		{
			base.OnSingleClick(from);

			string descr = "";
			if (Count > 0)
			{
				descr = string.Format("{0} keys", Count);
			}
			else
			{
				descr = "Empty";
			}

			this.LabelTo(from, descr);
		}

		public override void GetProperties(ObjectPropertyList list)
		{
			base.GetProperties(list);
			if (Count > 0)
			{
				string descr = string.Format("{0} Keys", Count);
				list.Add(descr);
			}
		}

		private static ObsoleteKeyring obsoleteKeyring = new ObsoleteKeyring(1);

		public override void Serialize(GenericWriter writer)
		{
    #region obsolete keyring
			// obsolete keyring
			obsoleteKeyring.Serialize(writer);
			writer.Write((int)1);	// ObsoleteKeyring version (obsolete as of version 1)
			writer.Write((int)0);	// m_MaxRange (don't care)
    #endregion obsolete keyring

			// new keyring
			base.Serialize(writer);
			writer.Write((int)0); // version of new keyring
			writer.Write((int)m_MaxRange);
		}

		public override void Deserialize(GenericReader reader)
		{
    #region obsolete keyring
			// obsolete keyring
			obsoleteKeyring.Deserialize(reader);
			int obsoleteVersion = reader.ReadInt();		// we will use this to tell us if we patched
			int dummy = reader.ReadInt();				// m_MaxRange (don't care)

			if (obsoleteVersion == 0)
			{
				Console.WriteLine("Updating keyring {0} to {1}", obsoleteKeyring, this);

				// now, if the obsolete keyring we just read has keys in it and this is the first time we've read it since the upgrade,
				//	we should move the keys from the old BaseContainer to the new keyring
				if (obsoleteKeyring.Items != null)
				{
					Item[] keys = obsoleteKeyring.FindItemsByType(typeof(Key));
					foreach (Key key in keys)
					{
						obsoleteKeyring.RemoveItem(key);
						this.AddItem(key);
					}
				}

				// patch attributes - location etc.
				this.Location = obsoleteKeyring.Location;
				this.Map = obsoleteKeyring.Map;
				this.IsLockedDown = obsoleteKeyring.IsLockedDown;
				this.IsSecure = obsoleteKeyring.IsSecure;
				this.Movable = obsoleteKeyring.Movable;
				this.Parent = obsoleteKeyring.Parent;
				this.ItemID = obsoleteKeyring.ItemID;

				// release the old one
				obsoleteKeyring.IsLockedDown = false;
				obsoleteKeyring.IsSecure = false;

				// delete the old one
				obsoleteKeyring.Delete();
				obsoleteKeyring = new ObsoleteKeyring(1);
			}
    #endregion
			else
			{	// upgrade to new keyring

				// new keyring, Item
				base.Deserialize(reader);
				int version = reader.ReadInt();

				switch (version)
				{
					case 0:
						{
							m_MaxRange = reader.ReadInt();
							if (m_MaxRange == 0)
								m_MaxRange = 3;
							break;
						}
				}
			}
		}


		private class RingUnlockTarget : Target
		{
			private KeyRing m_KeyRing;

			public RingUnlockTarget(KeyRing keyring)
				: base(keyring.MaxRange, false, TargetFlags.None)
			{
				m_KeyRing = keyring;
				CheckLOS = false;
			}

			protected override void OnTarget(Mobile from, object targeted)
			{

				if (m_KeyRing.Deleted || !m_KeyRing.IsChildOf(from.Backpack))
				{
					from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
					return;
				}

				int number;

				if (targeted == m_KeyRing)
				{
					number = -1;
					//remove keys from keyring
					ArrayList list = new ArrayList(m_KeyRing.Items);
					foreach (Item i in list)
					{
						if (i is Key) //doublecheck!
						{
							if (from is PlayerMobile)
							{
								Container b = ((PlayerMobile)from).Backpack;
								if (b != null)
								{
									m_KeyRing.RemoveItem(i);
									b.DropItem(i);
									i.Movable = true;
								}
							}
						}
					}
					m_KeyRing.UpdateItemID();
					from.SendMessage("You remove all the keys.");
				}
				else if (targeted is ILockable)
				{
					number = -1;

					ILockable o = (ILockable)targeted;

					if (m_KeyRing.IsKeyOnRing(o.KeyValue) && o.KeyValue != 0)
					{
						if (o is BaseDoor && !((BaseDoor)o).UseLocks())
						{
							number = 501668; // This key doesn't seem to unlock that.
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

							o.Locked = !o.Locked;

							if (o is LockableContainer)
							{
								LockableContainer cont = (LockableContainer)o;

								if (cont.TrapEnabled)
								{
									from.SendMessage("You leave the trap enabled.");
								}
								else
								{
									from.SendMessage("You leave the trap disabled.");
								}

								if (cont.LockLevel == -255)
									cont.LockLevel = cont.RequiredSkill - 10;
							}

							if (targeted is Item)
							{
								Item item = (Item)targeted;

								if (o.Locked)
									item.SendLocalizedMessageTo(from, 1048000);
								else
									item.SendLocalizedMessageTo(from, 1048001);
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
					from.SendLocalizedMessage(number);
				}
			}
		}//end RingUnlockTarget

	}

	public class ObsoleteKeyring : BaseContainer
	{
		public ObsoleteKeyring(int item) : base(item) { }
		public ObsoleteKeyring(Serial serial) : base(serial) { }
		public override bool CanFreezeDry { get { return true; } }
		public override void Serialize(GenericWriter writer) { base.Serialize(writer); }
		public override void Deserialize(GenericReader reader) { base.Deserialize(reader); }
	}

#endif
}