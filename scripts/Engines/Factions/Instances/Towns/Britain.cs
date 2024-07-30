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
    public class Britain : Town
    {
        public Britain()
        {
            Definition =
                new TownDefinition(
                    0,
                    0x1869,
                    "Britain",
                    "Britain",
                    new TextDefinition(1011433, "BRITAIN"),
                    new TextDefinition(1011561, "TOWN STONE FOR BRITAIN"),
                    new TextDefinition(1041034, "The Faction Sigil Monolith of Britain"),
                    new TextDefinition(1041404, "The Faction Town Sigil Monolith of Britain"),
                    new TextDefinition(1041413, "Faction Town Stone of Britain"),
                    new TextDefinition(1041395, "Faction Town Sigil of Britain"),
                    new TextDefinition(1041386, "Corrupted Faction Town Sigil of Britain"),
                    new Point3D(1592, 1680, 10),
                    new Point3D(1588, 1676, 10));
        }
    }
}