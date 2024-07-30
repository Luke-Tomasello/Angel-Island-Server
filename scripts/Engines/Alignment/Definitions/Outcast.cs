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

/* Scripts\Engines\Alignment\Definitions\Outcast.cs
 * Changelog:
 *  4/30/23, Yoar
 *      Added non-selectable Outcast alignment
 */

namespace Server.Engines.Alignment
{
    public sealed class Outcast : Alignment
    {
        public override AlignmentType Type { get { return AlignmentType.Outcast; } }

        public Outcast()
            : base(new AlignmentDefinition(
                "Outcast",
                "the outcast",
                new string[] { "Inmate", "Escapee" },
                2101,
                0,
                true,
                false))
        {
        }
    }
}