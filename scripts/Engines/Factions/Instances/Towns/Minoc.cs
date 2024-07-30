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
    public class Minoc : Town
    {
        public Minoc()
        {
            Definition =
                new TownDefinition(
                    2,
                    0x186B,
                    "Minoc",
                    "Minoc",
                    new TextDefinition(1011437, "MINOC"),
                    new TextDefinition(1011564, "TOWN STONE FOR MINOC"),
                    new TextDefinition(1041036, "The Faction Sigil Monolith of Minoc"),
                    new TextDefinition(1041406, "The Faction Town Sigil Monolith Minoc"),
                    new TextDefinition(1041415, "Faction Town Stone of Minoc"),
                    new TextDefinition(1041397, "Faction Town Sigil of Minoc"),
                    new TextDefinition(1041388, "Corrupted Faction Town Sigil of Minoc"),
                    new Point3D(2471, 439, 15),
                    new Point3D(2469, 445, 15));
        }
    }
}