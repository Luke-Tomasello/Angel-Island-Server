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

using Server.Mobiles;

namespace Server.ContextMenus
{
    public class TeachEntry : ContextMenuEntry
    {
        private SkillName m_Skill;
        private BaseCreature m_Mobile;
        private Mobile m_From;

        public TeachEntry(SkillName skill, BaseCreature m, Mobile from, bool enabled)
            : base(6000 + (int)skill, 4)
        {
            m_Skill = skill;
            m_Mobile = m;
            m_From = from;

            if (!enabled)
                Flags |= Network.CMEFlags.Disabled;
        }

        public override void OnClick()
        {
            if (!m_From.CheckAlive())
                return;

            m_Mobile.Teach(m_Skill, m_From, 0, false);
        }
    }
}