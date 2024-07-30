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

/* Items/Misc/FlipableAttribute.cs
 * ChangeLog:
 *  7/19/2023, Adam (Loot.FlipTable)
 *      Add the Loot.FlipTable for those flipable items that have no [flipable meta data, a 'wall torch' for example
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Targeting;
using System;
using System.Reflection;

namespace Server.Items
{
    public class FlipCommandHandlers
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Flip", AccessLevel.GameMaster, new CommandEventHandler(Flip_OnCommand));
        }

        [Usage("Flip")]
        [Description("Turns an item.")]
        public static void Flip_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new FlipTarget();
        }

        private class FlipTarget : Target
        {
            public FlipTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item)
                {
                    Item item = (Item)targeted;

                    if (item.Movable == false && from.AccessLevel == AccessLevel.Player)
                        return;

                    Type type = targeted.GetType();

                    FlipableAttribute[] AttributeArray = (FlipableAttribute[])type.GetCustomAttributes(typeof(FlipableAttribute), false);

                    if (AttributeArray.Length == 0 && !Loot.FlipTable.ContainsKey(item.ItemID))
                    {
                        return;
                    }

                    FlipableAttribute fa = null;
                    if (AttributeArray.Length != 0)
                        fa = AttributeArray[0];
                    else
                        fa = new FlipableAttribute(Loot.FlipTable[item.ItemID]);

                    fa.Flip((Item)targeted);
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DynamicFlipingAttribute : Attribute
    {
        public DynamicFlipingAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FlipableAttribute : Attribute
    {
        private int[] m_ItemIDs;

        public int[] ItemIDs
        {
            get { return m_ItemIDs; }
        }

        public FlipableAttribute()
            : this(null)
        {
        }

        public FlipableAttribute(params int[] itemIDs)
        {
            m_ItemIDs = itemIDs;
        }

        public virtual void Flip(Item item)
        {
            if (m_ItemIDs == null)
            {
                try
                {
                    MethodInfo flipMethod = item.GetType().GetMethod("Flip", Type.EmptyTypes);
                    if (flipMethod != null)
                        flipMethod.Invoke(item, new object[0]);
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

            }
            else
            {
                int index = 0;
                for (int i = 0; i < m_ItemIDs.Length; i++)
                {
                    if (item.ItemID == m_ItemIDs[i])
                    {
                        index = i + 1;
                        break;
                    }
                }

                if (index > m_ItemIDs.Length - 1)
                    index = 0;

                item.ItemID = m_ItemIDs[index];
            }
        }
    }
}