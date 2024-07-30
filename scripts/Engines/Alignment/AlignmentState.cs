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

/* Scripts\Engines\Alignment\AlignmentState.cs
 * Changelog:
 *  4/14/23, Yoar
 *      Initial version.
 */

namespace Server.Engines.Alignment
{
    [PropertyObject]
    public class AlignmentState
    {
        public static AlignmentState GetState(AlignmentType type)
        {
            Alignment alignment = Alignment.Get(type);

            if (alignment != null)
                return alignment.State;

            return null;
        }

        private Alignment m_Alignment;

        public Alignment Alignment { get { return m_Alignment; } }

        public AlignmentState(Alignment alignment)
        {
            m_Alignment = alignment;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override string ToString()
        {
            return "...";
        }
    }
}