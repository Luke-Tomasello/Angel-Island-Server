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

/* Scripts/Gumps/Properties/PropsConfig.cs
 * Changelog:
 *  11/17,06, Adam
 *      Add: #pragma warning disable 429
 *      The three Unreachable code complaints in this file are acceptable
 *      C:\Program Files\RunUO\Scripts\Gumps\Properties\PropsConfig.cs(36,46): warning CS0429: Unreachable expression code detected
 */

#pragma warning disable 429


namespace Server.Gumps
{

    public class PropsConfig
    {
        public const bool OldStyle = false;

        public const int GumpOffsetX = 30;
        public const int GumpOffsetY = 30;

        public const int TextHue = 0;
        public const int TextOffsetX = 2;

        public const int OffsetGumpID = 0x0A40; // Pure black
        public const int HeaderGumpID = OldStyle ? 0x0BBC : 0x0E14; // Light offwhite, textured : Dark navy blue, textured
        public const int EntryGumpID = 0x0BBC; // Light offwhite, textured
        public const int BackGumpID = 0x13BE; // Gray slate/stoney
        public const int SetGumpID = OldStyle ? 0x0000 : 0x0E14; // Empty : Dark navy blue, textured

        public const int SetWidth = 20;
        public const int SetOffsetX = OldStyle ? 4 : 2, SetOffsetY = 2;
        public const int SetButtonID1 = 0x15E1; // Arrow pointing right
        public const int SetButtonID2 = 0x15E5; // " pressed

        public const int PrevWidth = 20;
        public const int PrevOffsetX = 2, PrevOffsetY = 2;
        public const int PrevButtonID1 = 0x15E3; // Arrow pointing left
        public const int PrevButtonID2 = 0x15E7; // " pressed

        public const int NextWidth = 20;
        public const int NextOffsetX = 2, NextOffsetY = 2;
        public const int NextButtonID1 = 0x15E1; // Arrow pointing right
        public const int NextButtonID2 = 0x15E5; // " pressed

        public const int OffsetSize = 1;

        public const int EntryHeight = 20;
        public const int BorderSize = 10;
    }
}