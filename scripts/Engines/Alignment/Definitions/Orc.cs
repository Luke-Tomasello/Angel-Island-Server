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

namespace Server.Engines.Alignment
{
    public sealed class Orc : Alignment
    {
        public override AlignmentType Type { get { return AlignmentType.Orc; } }

        public Orc()
            : base(new AlignmentDefinition(
                "Orc",
                "the orc",
                new string[] { "Gruntee", "Captain" },
                1554, // reddish brown
                0,
                false,
                true))
        {
        }
    }
}