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

/* Scripts/Gumps/Properties/SetObjectTarget.cs
 * Changelog:
 *  4/4/2024, Adam
 *      an ArcheryButte for instance has an Addon field of null.
 *      We therefore treat these objects as 'items' and not Addons
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Targeting;
using System;
using System.Collections;
using System.Reflection;

namespace Server.Gumps
{
    public class SetObjectTarget : Target
    {
        private PropertyInfo m_Property;
        private Mobile m_Mobile;
        private object m_Object;
        private Stack m_Stack;
        private Type m_Type;
        private int m_Page;
        private ArrayList m_List;

        public SetObjectTarget(PropertyInfo prop, Mobile mobile, object o, Stack stack, Type type, int page, ArrayList list)
            : base(-1, false, TargetFlags.None)
        {
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;
            m_Stack = stack;
            m_Type = type;
            m_Page = page;
            m_List = list;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            try
            {
                if (m_Type == typeof(Type))
                    targeted = targeted.GetType();
                else if ((m_Type == typeof(BaseAddon) || m_Type.IsAssignableFrom(typeof(BaseAddon))) && targeted is AddonComponent)
                {   // 4/4/2024, Adam: an ArcheryButte for instance has an Addon field of null
                    if (((AddonComponent)targeted).Addon != null)
                        targeted = ((AddonComponent)targeted).Addon;
                }
                #region Set Multi 1/22/24, Yoar: Set multi by StaticTarget
                else if (targeted is StaticTarget)
                {
                    StaticTarget targ = (StaticTarget)targeted;

                    BaseMulti multi = BaseMulti.FindAt(new Point3D(targ), from.Map);

                    if (multi != null)
                    {
                        MultiComponentList mcl = multi.Components;

                        int vx = targ.X - multi.X - mcl.Min.m_X;
                        int vy = targ.Y - multi.Y - mcl.Min.m_Y;

                        if (vx >= 0 && vx < mcl.Width && vy >= 0 && vy < mcl.Height)
                        {
                            StaticTile[] tiles = mcl.Tiles[vx][vy];

                            for (int i = 0; i < tiles.Length; i++)
                            {
                                int itemID = tiles[i].ID & 0x3FFF;

                                ItemData id = TileData.ItemTable[itemID & TileData.MaxItemValue];

                                if (itemID == targ.ItemID && targ.Z == multi.Z + tiles[i].Z + id.Height)
                                {
                                    targeted = multi;
                                    break;
                                }
                            }
                        }
                    }
                }
                #endregion Set Multi 1/22/24, Yoar: Set multi by StaticTarget

                if (m_Type.IsAssignableFrom(targeted.GetType()))
                {
                    Server.Commands.CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, targeted.ToString());
                    m_Property.SetValue(m_Object, targeted, null);
                    PropertiesGump.OnValueChanged(m_Object, m_Property, m_Stack);

                    // we don't try to reference count links - too complex.
                    //  Instead, on server up we clean this up.
                    if (targeted is Mobile m)
                        m.SetMobileBool(Mobile.MobileBoolTable.IsLinked, true);
                    else if (targeted is Item item)
                        item.SetItemBool(Item.ItemBoolTable.IsLinked, true);
                }
                else
                {
                    m_Mobile.SendMessage("That cannot be assigned to a property of type : {0}", m_Type.Name);
                }
            }
            catch
            {
                m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
            }
        }

        protected override void OnTargetFinish(Mobile from)
        {
            if (m_Type == typeof(Type))
                from.SendGump(new PropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
            else
                from.SendGump(new SetObjectGump(m_Property, m_Mobile, m_Object, m_Stack, m_Type, m_Page, m_List));
        }
    }
}