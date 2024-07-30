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
    public class Yew : Town
    {
        public Yew()
        {
            Definition =
                new TownDefinition(
                    4,
                    0x186D,
                    "Yew",
                    "Yew",
                    new TextDefinition(1011438, "YEW"),
                    new TextDefinition(1011565, "TOWN STONE FOR YEW"),
                    new TextDefinition(1041038, "The Faction Sigil Monolith of Yew"),
                    new TextDefinition(1041408, "The Faction Town Sigil Monolith of Yew"),
                    new TextDefinition(1041417, "Faction Town Stone of Yew"),
                    new TextDefinition(1041399, "Faction Town Sigil of Yew"),
                    new TextDefinition(1041390, "Corrupted Faction Town Sigil of Yew"),
                    new Point3D(548, 979, 0),
                    new Point3D(542, 980, 0));
        }
    }
}