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

/* Scripts/Gumps/Properties/SetCustomEnumGump.cs
 * Changelog:
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Network;
using System;
using System.Collections;
using System.Reflection;

namespace Server.Gumps
{
    public class SetCustomEnumGump : SetListOptionGump
    {
        private string[] m_Names;

        public SetCustomEnumGump(PropertyInfo prop, Mobile mobile, object o, Stack stack, int propspage, ArrayList list, string[] names)
            : base(prop, mobile, o, stack, propspage, list, names, null)
        {
            m_Names = names;
        }

        public override void OnResponse(NetState sender, RelayInfo relayInfo)
        {
            int index = relayInfo.ButtonID - 1;

            if (index >= 0 && index < m_Names.Length)
            {
                try
                {
                    MethodInfo info = m_Property.PropertyType.GetMethod("Parse", new Type[] { typeof(string) });

                    Server.Commands.CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, m_Names[index]);

                    if (info != null)
                        m_Property.SetValue(m_Object, info.Invoke(null, new object[] { m_Names[index] }), null);
                    else if (m_Property.PropertyType == typeof(Enum) || m_Property.PropertyType.IsSubclassOf(typeof(Enum)))
                        m_Property.SetValue(m_Object, Enum.Parse(m_Property.PropertyType, m_Names[index], false), null);

                    PropertiesGump.OnValueChanged(m_Object, m_Property, m_Stack);
                }
                catch
                {
                    m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }
            }

            m_Mobile.SendGump(new PropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
        }
    }
}