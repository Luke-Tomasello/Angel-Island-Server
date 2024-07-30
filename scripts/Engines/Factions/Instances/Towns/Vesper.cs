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

namespace Server.Factions
{
    public class Vesper : Town
    {
        public Vesper()
        {
            Definition =
                new TownDefinition(
                    5,
                    0x186E,
                    "Vesper",
                    "Vesper",
                    new TextDefinition(1016413, "VESPER"),
                    new TextDefinition(1011566, "TOWN STONE FOR VESPER"),
                    new TextDefinition(1041039, "The Faction Sigil Monolith of Vesper"),
                    new TextDefinition(1041409, "The Faction Town Sigil Monolith of Vesper"),
                    new TextDefinition(1041418, "Faction Town Stone of Vesper"),
                    new TextDefinition(1041400, "Faction Town Sigil of Vesper"),
                    new TextDefinition(1041391, "Corrupted Faction Town Sigil of Vesper"),
                    new Point3D(2982, 818, 0),
                    new Point3D(2985, 821, 0));
        }
    }
}