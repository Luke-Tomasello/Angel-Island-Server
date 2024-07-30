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
    public class CraftSkillCol : System.Collections.CollectionBase
    {
        public CraftSkillCol()
        {
        }

        public void Add(CraftSkill craftSkill)
        {
            List.Add(craftSkill);
        }

        public void Remove(int index)
        {
            if (index > Count - 1 || index < 0)
            {
            }
            else
            {
                List.RemoveAt(index);
            }
        }

        public CraftSkill GetAt(int index)
        {
            return (CraftSkill)List[index];
        }
    }
}