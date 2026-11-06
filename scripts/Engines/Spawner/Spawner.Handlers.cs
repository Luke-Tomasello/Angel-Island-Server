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

/* Scripts\Engines\Spawner\Spawner.Handlers.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Network;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        public virtual bool HandlesOnSingleClick { get; } = false;

        public virtual void OnSingleClickSpawned(Mobile from, BaseCreature creature) {; }

        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
                return;

            if (from.AccessLevel == AccessLevel.Player)
            {
                LabelTo(from, string.Format("[{0}]", "Super Secret"));
                return;
            }

            // display the name
            NetState ns = from.NetState;
            ns.Send(new UnicodeMessage(this.Serial, this.ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", (!string.IsNullOrEmpty(m_Source) ? m_Source + " " : "") + this.Name));
            int labels = 0;

            // This is a standard OSI spawn
            //  https://uo.stratics.com/hunters/spawn/spawnmap.jpg
            if (m_CoreSpawn)
            {
                LabelTo(from, (this is EventSpawner) ? "[Event]" : "" + "[Core]" + (Concentric ? " (Concentric)" : ""));
                labels++;
            }

            if (m_Shard.HasFlag(ShardConfig.AllShards))
            {
                if (labels == 0)
                    LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(ShardConfig.AllShards.ToString())));
                else
                    LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(ShardConfig.AllShards.ToString()) + (Running ? " [on]" : " (Off)")));
                labels++;
            }
            else
            {
                // Angel Island special spawn
                if (m_Shard.HasFlag(ShardConfig.AngelIsland))
                {
                    if (labels == 0)
                        LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(ShardConfig.AngelIsland.ToString())));
                    else
                        LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(ShardConfig.AngelIsland.ToString()) + (Running ? " [on]" : " (Off)")));
                    labels++;
                }
                // other shards
                if (m_Shard.HasFlag(ShardConfig.Siege) || m_Shard.HasFlag(ShardConfig.Mortalis) || m_Shard.HasFlag(ShardConfig.Seasons))
                {
                    if (labels == 0)
                        LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(m_Shard.ToString())).Replace(" ", string.Empty));
                    else
                        LabelTo(from, string.Format("[{0}]", Utility.SplitOnCase(m_Shard.ToString())).Replace(" ", string.Empty) + (Running ? " (on)" : " (Off)"));
                }
            }

            if (labels < 2)
                if (Running)
                    LabelTo(from, "[Running]");
                else
                    LabelTo(from, "[Off]");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.GameMaster)
                return;

            SpawnerGump g = new SpawnerGump(this);
            from.SendGump(g);
        }
    }
}
