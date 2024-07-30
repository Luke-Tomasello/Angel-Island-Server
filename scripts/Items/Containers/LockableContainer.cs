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

/* Scripts/Items/Containers/LockableContainer.cs
 * CHANGELOG:
 *	3/28/10, adam
 *		Added an auto-reset mechanism for resetting the trap and lock after a timeout period.
 *		Note: because of the way trapped containers are untrapped via RemoveTrap (power and traptype are cleared)
 *			the autoreset doesn't kick in until the Locked value is set to false.
 *  04/05/06 Taran Kain
 *		Made DisplaysContent consult its parent class as well
 *	9/3/04: Pix
 *		Changed it so you can't drop things onto a locked container
 */

using Server.Engines.Craft;
using Server.Network;
using Server.Spells.Third;
using System;

namespace Server.Items
{
    public abstract class LockableContainer : TrapableContainer, ILockable, ILockpickable, ICraftable, IMagicUnlockable/*, todo IShipwreckedItem*/
    {
        private bool m_Locked;
        private int m_LockLevel, m_MaxLockLevel, m_RequiredSkill;
        private uint m_KeyValue;
        private Mobile m_Picker;
        private bool m_AutoReset_unused_pending_delete;
        private TimeSpan m_AutoResetTime_unused_pending_delete;
        AutoResetTimer m_AutoResetTimer_unused_pending_delete;

#if false
        [CommandProperty(AccessLevel.GameMaster)]
        public bool AutoReset
        {
            get { return m_AutoReset; }
            set
            {
                if (m_AutoReset != value)
                {
                    if (value == true && Locked == false)
                    {
                        if (m_AutoResetTimer == null || m_AutoResetTimer.Running == false)
                        {
                            m_AutoResetTimer = new AutoResetTimer(this, m_AutoResetTime);
                            m_AutoResetTimer.Start();
                        }
                    }
                    else
                    {
                        if (m_AutoResetTimer != null && m_AutoResetTimer.Running == true)
                        {
                            m_AutoResetTimer.Flush();
                            m_AutoResetTimer.Stop();
                        }
                    }
                }

                // ask the traped chest to remember it's power settings
                //	these are the values we will restore
                if (value == true)
                    this.RememberTrap();

                m_AutoReset = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan AutoResetTime { get { return m_AutoResetTime_unused_pending_delete; } set { m_AutoResetTime_unused_pending_delete = value; } }
#endif
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Picker
        {
            get
            {
                return m_Picker;
            }
            set
            {
                m_Picker = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLockLevel
        {
            get
            {
                return m_MaxLockLevel;
            }
            set
            {
                m_MaxLockLevel = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LockLevel
        {
            get
            {
                return m_LockLevel;
            }
            set
            {
                m_LockLevel = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredSkill
        {
            get
            {
                return m_RequiredSkill;
            }
            set
            {
                m_RequiredSkill = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Locked
        {
            get
            {
                return m_Locked;
            }
            set
            {
                m_Locked = value;

                if (m_Locked)
                    m_Picker = null;

                if (m_Locked == false && m_AutoReset_unused_pending_delete == true)
                {
                    if (m_AutoResetTimer_unused_pending_delete == null || m_AutoResetTimer_unused_pending_delete.Running == false)
                    {
                        m_AutoResetTimer_unused_pending_delete = new AutoResetTimer(this, m_AutoResetTime_unused_pending_delete);
                        m_AutoResetTimer_unused_pending_delete.Start();
                    }
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public uint KeyValue
        {
            get
            {
                return m_KeyValue;
            }
            set
            {
                m_KeyValue = value;
            }
        }

        public override bool TrapOnOpen
        {
            get
            {   // old style tinker traps always trap on open
                return Core.OldStyleTinkerTrap;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TrapOnLockpick
        {
            get
            {   // new style tinker traps always trap on pick
                return Core.NewStyleTinkerTrap;
            }
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            if (this.Locked)
            {
                return false;
            }
            else
            {
                return base.TryDropItem(from, dropped, sendFullMessage);
            }
        }

        /* IF we want to disallow dropping into open locked containers, uncomment this
				public override bool OnDragDropInto( Mobile from, Item item, Point3D p )
				{
					if( this.Locked )
					{
						return false;
					}
					else
					{
						return base.OnDragDropInto(from, item, p);
					}
				}
		*/
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)7); // version

            // version 7
            // remove m_TrapOnLockpick

            // version 6
            writer.Write(m_IsShipwreckedItem);
            //writer.Write((bool)m_TrapOnLockpick);	// removed in version 7

            // version 5
            writer.Write((bool)m_AutoReset_unused_pending_delete);
            writer.Write(m_AutoResetTime_unused_pending_delete);

            // version 4
            writer.Write((int)m_RequiredSkill);
            writer.Write((int)m_MaxLockLevel);
            writer.Write(m_KeyValue);
            writer.Write((int)m_LockLevel);
            writer.Write((bool)m_Locked);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 7:
                    {
                        // remove m_TrapOnLockpick
                        // see case 6
                        goto case 6;
                    }
                case 6:
                    {
                        m_IsShipwreckedItem = reader.ReadBool();
                        if (version < 7)
                        {
                            reader.ReadBool(); // m_TrapOnLockpick
                        }
                        goto case 5;
                    }
                case 5:
                    {
                        m_AutoReset_unused_pending_delete = reader.ReadBool();
                        m_AutoResetTime_unused_pending_delete = reader.ReadTimeSpan();
                        if (m_AutoReset_unused_pending_delete == true)
                        {
                            m_AutoResetTimer_unused_pending_delete = new AutoResetTimer(this, m_AutoResetTime_unused_pending_delete);
                            m_AutoResetTimer_unused_pending_delete.Start();
                        }
                        goto case 4;
                    }
                case 4:
                    {
                        m_RequiredSkill = reader.ReadInt();

                        goto case 3;
                    }
                case 3:
                    {
                        m_MaxLockLevel = reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        m_KeyValue = reader.ReadUInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_LockLevel = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                            m_MaxLockLevel = 100;

                        if (version < 4)
                        {
                            if ((m_MaxLockLevel - m_LockLevel) == 40)
                            {
                                m_RequiredSkill = m_LockLevel + 6;
                                m_LockLevel = m_RequiredSkill - 10;
                                m_LockLevel = m_RequiredSkill + 39;
                            }
                            else
                            {
                                m_RequiredSkill = m_LockLevel;
                            }
                        }

                        m_Locked = reader.ReadBool();

                        break;
                    }
            }

        }

        public class AutoResetTimer : Timer
        {
            private LockableContainer m_cont;

            public AutoResetTimer(LockableContainer lc, TimeSpan delay)
                : base(delay)
            {
                m_cont = lc;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                m_cont.ResetTrap();     // only changes power and type, does not change enabled
                m_cont.Locked = true;
            }
        }

        public LockableContainer(int itemID)
            : base(itemID)
        {
            m_MaxLockLevel = 100;
        }

        public LockableContainer(Serial serial)
            : base(serial)
        {
        }

        public override bool CheckContentDisplay(Mobile from)
        {
            return !m_Locked && base.CheckContentDisplay(from);
        }

        public override bool DisplaysContent
        {
            get
            {
                return !m_Locked && base.DisplaysContent;
            }
        }

        public virtual bool CheckLocked(Mobile from)
        {
            bool inaccessible = false;

            if (m_Locked)
            {
                int number;

                if (from.AccessLevel >= AccessLevel.GameMaster)
                {
                    number = 502502; // That is locked, but you open it with your godly powers.
                }
                else if (Core.NewStyleTinkerTrap && base.TrapEnabled && from == this.Owner)
                {   // the last to lock a tinker trap map auto open it.
                    // See comments in Scripts\Engines\Craft\DefTinkering.cs
                    // TODO: what message goes here?
                    return false;
                }
                else
                {
                    number = 501747; // It appears to be locked.
                    inaccessible = true;
                }

                from.Send(new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", ""));
            }

            return inaccessible;
        }

        public override void OnTelekinesis(Mobile from)
        {
            if (CheckLocked(from))
            {
                Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x376A, 9, 32, 5022);
                Effects.PlaySound(Location, Map, 0x1F5);
                return;
            }

            base.OnTelekinesis(from);
        }

#if old
		public override void OnDoubleClick(Mobile from)
		{
			if (CheckLocked(from))
				return;

			base.OnDoubleClick(from);
		}
#endif

        public override void Open(Mobile from)
        {
            if (CheckLocked(from))
                return;

            base.Open(from);
        }

        public override void OnSnoop(Mobile from)
        {
            if (CheckLocked(from))
                return;

            base.OnSnoop(from);
        }

        public virtual void LockPick(Mobile from)
        {
            // old style traps can be picked
            if (Core.OldStyleTinkerTrap)
            {
                Locked = false; // unlock
                Picker = from;  // give credit
            }
            // new style tinker traps can't be picked.
            else
            {
                if (TrapEnabled)
                {
                    bool bAutoReset = true;         // new style tinker traps auto reset
                    ExecuteTrap(from, bAutoReset);  // execute trap
                }
                else
                {   // if the trap has been disabled, the lock can now be picked
                    Locked = false; // unlock
                    Picker = from;  // give credit
                }
            }

#if old
			if (this.TrapOnLockpick && ExecuteTrap(from))
			{
				// RunUO has this, but I think it is wrong for new style tinker traps as they are unpickable, i.e.,
				//	the blowup on pick, and auto-reset (meaning they never unlock)
				this.TrapOnLockpick = false;
			}
#endif

        }

        #region ICraftable Members

        public override int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            base.OnCraft(quality, makersMark, from, craftSystem, typeRes, tool, craftItem, resHue);

            if (from.CheckSkill(SkillName.Tinkering, -5.0, 15.0, contextObj: new object[2]))
            {
                from.SendLocalizedMessage(500636); // Your tinker skill was sufficient to make the item lockable.

                Key key = new Key(KeyType.Copper, Key.RandomValue());

                KeyValue = key.KeyValue;
                DropItem(key);

                double tinkering = from.Skills[SkillName.Tinkering].Value;
                int level = (int)(tinkering * 0.8);

                RequiredSkill = level - 4;
                LockLevel = level - 14;
                MaxLockLevel = level + 35;

                if (LockLevel == 0)
                    LockLevel = -1;
                else if (LockLevel > 95)
                    LockLevel = 95;

                if (RequiredSkill > 95)
                    RequiredSkill = 95;

                if (MaxLockLevel > 95)
                    MaxLockLevel = 95;
            }
            else
            {
                from.SendLocalizedMessage(500637); // Your tinker skill was insufficient to make the item lockable.
            }

            return 1;
        }

#if old
		else if (item is LockableContainer)
					{
						if (from.CheckSkill(SkillName.Tinkering, -5.0, 15.0))
						{
							LockableContainer cont = (LockableContainer)item;

							from.SendLocalizedMessage(500636); // Your tinker skill was sufficient to make the item lockable.

							Key key = new Key(KeyType.Copper, Key.RandomValue());

							cont.KeyValue = key.KeyValue;
							cont.DropItem(key);
							/*
														double tinkering = from.Skills[SkillName.Tinkering].Value;
														int level = (int)(tinkering * 0.8);

														cont.RequiredSkill = level - 4;
														cont.LockLevel = level - 14;
														cont.MaxLockLevel = level + 35;

														if ( cont.LockLevel == 0 )
															cont.LockLevel = -1;
														else if ( cont.LockLevel > 95 )
															cont.LockLevel = 95;

														if ( cont.RequiredSkill > 95 )
															cont.RequiredSkill = 95;

														if ( cont.MaxLockLevel > 95 )
															cont.MaxLockLevel = 95;
							Commented out by darva to try new tinker lock strength code.*/

							double tinkering = from.Skills[SkillName.Tinkering].Value;
							int level = (int)(tinkering);
							cont.RequiredSkill = 36;
							if (level >= 65)
								cont.RequiredSkill = 76;
							if (level >= 80)
								cont.RequiredSkill = 84;
							if (level >= 90)
								cont.RequiredSkill = 92;
							if (level >= 100)
								cont.RequiredSkill = 100;
							cont.LockLevel = cont.RequiredSkill - 10;
							cont.MaxLockLevel = cont.RequiredSkill + 40;


						}
						else
						{
							from.SendLocalizedMessage(500637); // Your tinker skill was insufficient to make the item lockable.
						}
					}
#endif

        #endregion

        #region IShipwreckedItem Members

        private bool m_IsShipwreckedItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsShipwreckedItem
        {
            get { return m_IsShipwreckedItem; }
            set { m_IsShipwreckedItem = value; }
        }
        #endregion

        #region IMagicUnlockable Members

        public virtual void OnMagicUnlock(Mobile from)
        {
        }

        #endregion
    }
}