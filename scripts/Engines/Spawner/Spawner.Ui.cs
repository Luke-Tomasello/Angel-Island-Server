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

/* Scripts\Engines\Spawner\Spawner.Ui.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using System.Collections.Generic;

namespace Server.Mobiles
{
    public partial class Spawner
    {

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Running)
            {
                list.Add(1060742); // active

                list.Add(1060656, m_SlotCount.ToString()); // amount to make: ~1_val~
                list.Add(1061169, m_HomeRange.ToString()); // range ~1_val~

                list.Add(1060658, "group\t{0}", m_Group); // ~1_val~: ~2_val~
                list.Add(1060659, "team\t{0}", m_Team); // ~1_val~: ~2_val~
                list.Add(1060660, "speed\t{0} to {1}", m_MinDelay, m_MaxDelay); // ~1_val~: ~2_val~

                List<string> names = ObjectNamesParsed;
                for (int i = 0; i < 3 && i < names.Count; ++i)
                    list.Add(1060661 + i, "{0}\t{1}", names[i], CountObjects((string)names[i]));
            }
            else
            {
                list.Add(1060743); // inactive
            }
        }

        public void UpdateDisplay()
        {
            if (Core.RuleSets.AngelIslandRules())
                return;

            if (Distro == SpawnerModeAttribs.ModeNeruns)
            {
                if (Running)
                    Hue = (int)SpawnerGraphics.NerunRunningHue;
                else
                    Hue = (int)SpawnerGraphics.NerunStoppedHue;
            }
            else if (Shard == ShardConfig.AllShards)
            {
                if (Running)
                    Hue = (int)SpawnerGraphics.AllShardsRunningHue;
                else
                    Hue = (int)SpawnerGraphics.AllShardsStoppedHue;
            }
            else
            {
                if (Running)
                    Hue = (int)SpawnerGraphics.CustomRunningHue;
                else
                    Hue = (int)SpawnerGraphics.CustomStoppedHue;
            }
        }
    }
}
