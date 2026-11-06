/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\TestCenter\TestCenter.Facet.cs
 * CHANGELOG
 *	5/2/2026, Adam
 *	    first check in
 */

using System.Collections.Generic;


namespace Server.Engines
{
    public class TestCenterFacet
    {
        private static readonly HashSet<Map> m_TestCenterMaps = new HashSet<Map>();

        [CallPriority(20)]// must load after map definitions
        public static void Configure()
        {
            // ====================== CONFIGURE TEST CENTER FACETS HERE ======================
            m_TestCenterMaps.Add(Map.TestCenter);
        }

        public static void Initialize()
        {
            EventSink.Speech += new SpeechEventHandler(OnSpeech_Facet);
        }

        private static void OnSpeech_Facet(SpeechEventArgs args)
        {
            TestCenterCommands.TCHelpGump.EventSink_Speech(args, IsTestCenter);
            TestCenterCommands.TCSetSkills.EventSink_Speech(args, IsTestCenter);
        }

        /// <summary>
        /// Returns true if the map is a Test Center facet.
        /// </summary>
        public static bool IsTestCenter(Map map)
        {
            return map != null && m_TestCenterMaps.Contains(map);
        }

        /// <summary>
        /// Returns true if the mobile is currently on a Test Center facet.
        /// </summary>
        public static bool IsTestCenter(Mobile m)
        {
            return m != null && IsTestCenter(m.Map);
        }
    }
}
