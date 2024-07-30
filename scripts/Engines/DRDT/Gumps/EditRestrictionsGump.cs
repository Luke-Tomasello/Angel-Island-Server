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

/* Scripts/Engines/DRDT/Gumps/RestrictGump.cs
 *  9/11/22, Yoar (Custom Region Overhaul)
 *      Completely overhauled custom region system
 *	6/30/08, Adam
 *		- Use math.min to create a loop counter of the smaller of the BitArray or Table for which the BitArray was created 
 *			This came about because the RegionController saves two a bitarrays; one for the size of the Spell Table and the other for the size of the
 *			Skill Table. The problem is that when we merge in code that includes new Spells or Skills, this logic breaks. 
 *			(This happened when we recently merged in new networking code and also merged in SpellWeaving, the 55th skill.
 *		- Add exception logging via assert
 *	6/30/08, weaver
 *		Added try/catch to cater for cases where the registered spell count differs from the restricted spell count
 *		(in order to prevent shard crash when editing restricted spells while this is the case).
 *		Added changelog + copyright header.
 */

using Server.Network;
using Server.Spells;
using System;
using System.Collections.Generic;

namespace Server.Gumps
{
    public abstract class EditRestrictionsGump : Gump
    {
        protected abstract string Label { get; }
        protected abstract int RegistrySize { get; }

        private HashSet<int> m_Restricted;

        public EditRestrictionsGump(HashSet<int> restricted)
            : base(50, 50)
        {
            m_Restricted = restricted;

            AddPage(0);

            AddBackground(10, 10, 225, 425, 9380);
            AddLabel(73, 15, 1152, Label);

            AddButton(91, 411, 247, 248, 1, GumpButtonType.Reply, 0);

            AddPage(1);

            int itemsThisPage = 0;
            int page = 1;

            for (int i = 0; i < RegistrySize; i++)
            {
                object o = GetAt(i);

                if (o != null)
                {
                    if (itemsThisPage != 0 && (itemsThisPage % 8) == 0)
                    {
                        itemsThisPage = 0;

                        AddButton(190, 412, 4005, 4007, 2, GumpButtonType.Page, page + 1);
                        AddPage(++page);
                        AddButton(29, 412, 4014, 4016, 3, GumpButtonType.Page, page - 1);
                    }

                    AddCheck(40, 55 + (45 * itemsThisPage), 210, 211, m_Restricted.Contains(i), i);
                    AddLabel(70, 55 + (45 * itemsThisPage), 0, Format(o));

                    itemsThisPage++;
                }
            }
        }

        protected abstract object GetAt(int index);

        protected virtual string Format(object o)
        {
            return o.ToString();
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1) // Okay
            {
                m_Restricted.Clear();

                for (int i = 0; i < info.Switches.Length; i++)
                    m_Restricted.Add(info.Switches[i]);
            }
        }
    }

    public class EditRestrictedSpellsGump : EditRestrictionsGump
    {
        protected override string Label { get { return "Restrict Spells"; } }
        protected override int RegistrySize { get { return SpellRegistry.Types.Length; } }

        public EditRestrictedSpellsGump(HashSet<int> restricted)
            : base(restricted)
        {
        }

        protected override object GetAt(int index)
        {
            return SpellRegistry.Types[index];
        }

        protected override string Format(object o)
        {
            return ((o is Type) ? ((Type)o).Name : base.Format(o));
        }
    }

    public class EditRestrictedSkillsGump : EditRestrictionsGump
    {
        protected override string Label { get { return "Restrict Skills"; } }
        protected override int RegistrySize { get { return SkillInfo.Table.Length; } }

        public EditRestrictedSkillsGump(HashSet<int> restricted)
            : base(restricted)
        {
        }

        protected override object GetAt(int index)
        {
            return SkillInfo.Table[index];
        }

        protected override string Format(object o)
        {
            return ((o is SkillInfo) ? ((SkillInfo)o).Name : base.Format(o));
        }
    }
}