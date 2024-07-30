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

namespace Server.Engines.Craft
{
    public class CraftSkill
    {
        private SkillName m_SkillToMake;
        private double m_MinSkill;
        private double m_MaxSkill;

        public CraftSkill(SkillName skillToMake, double minSkill, double maxSkill)
        {
            m_SkillToMake = skillToMake;
            m_MinSkill = minSkill;
            m_MaxSkill = maxSkill;
        }

        public SkillName SkillToMake
        {
            get { return m_SkillToMake; }
        }

        public double MinSkill
        {
            get { return m_MinSkill; }
        }

        public double MaxSkill
        {
            get { return m_MaxSkill; }
        }
    }
}