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

/* Scripts\Engines\Spawner\Spawner.Lifecycle.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Diagnostics;
using Server.Engines;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        public static List<Spawner> Instances = new List<Spawner>();

        public override Item Dupe(int amount)
        {
            Spawner new_spawner = new Spawner();
            // The template reference is copied normally because it is not marked DoNotCopy.
            //
            // However, templates are marked DoNotRelink, so the duped spawner keeps the
            // original shared template reference instead of being remapped to a cloned item.
            //
            // Because the reference is intentionally shared, manually increment the
            // SpawnerTempRefCount for each copied template.
            Utility.CopyProperties(this, new_spawner);
            if (TemplateItem != null)
            {
                new_spawner.TemplateItem.SpawnerTempRefCount += 1;
            }
            if (TemplateMobile != null)
            {
                new_spawner.TemplateMobile.SpawnerTempRefCount += 1;
            }
            if (LootPack != null)
            {
                new_spawner.LootPack.SpawnerTempRefCount += 1;
            }
            if (CarvePack != null)
            {
                new_spawner.CarvePack.SpawnerTempRefCount += 1;
            }
            if (ArtifactPack != null)
            {
                new_spawner.ArtifactPack.SpawnerTempRefCount += 1;
            }

            return base.Dupe(new_spawner, amount);
        }

        [Constructable]
        public Spawner(int amount, int minDelay, int maxDelay, int team, int homeRange, string objectName)
            : base(0x1f13)
        {
            ArrayList objectNames = new ArrayList();
            objectNames.Add(objectName.ToLower());
            InitSpawn(amount, TimeSpan.FromMinutes(minDelay), TimeSpan.FromMinutes(maxDelay), team, homeRange, objectNames);
        }
        [Constructable]
        public Spawner(string objectName)
            : base(0x1f13)
        {
            ArrayList objectNames = new ArrayList();
            objectNames.Add(objectName.ToLower());
            InitSpawn(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, 4, objectNames);
        }
        [Constructable]
        public Spawner()
            : base(0x1f13)
        {
            ArrayList objectNames = new ArrayList();
            InitSpawn(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, 4, objectNames);
        }
        public Spawner(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, ArrayList objectNames)
            : base(0x1f13)
        {
            InitSpawn(amount, minDelay, maxDelay, team, homeRange, objectNames);
        }
        public void InitSpawn(int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, ArrayList objectNames)
        {

            Visible = false;
            Movable = false;
            Running = true;
            m_Group = false;
            m_MobDirection = Direction.North;
            Name = "Spawner";
            m_MinDelay = minDelay;
            m_MaxDelay = maxDelay;
            SetSlotCount(index: 0, amount);
            m_Team = team;
            m_HomeRange = homeRange;
            m_ObjectNames = objectNames;
            m_Objects = new ArrayList();
            DoTimer(TimeSpan.FromSeconds(1));

            if (Core.RuleSets.StandardShardRules())
            {
                m_Shard = ShardConfig.Mortalis | ShardConfig.Siege | ShardConfig.Seasons | ShardConfig.Upheaval;
                m_CoreSpawn = false;
                Distro = SpawnerModeAttribs.ModeAI;
                _defaultSpawnMap();
            }
            else if (Core.RuleSets.AngelIslandRules())
            {
                m_Shard = ShardConfig.AngelIsland;
                m_CoreSpawn = false;
                Distro = SpawnerModeAttribs.ModeAI;
                _defaultSpawnMap();
            }
            else
            {   // error
                m_Shard = ShardConfig.None;
                m_CoreSpawn = false;
                Distro = SpawnerModeAttribs.None;
                _defaultSpawnMap();
            }

            // inform our spawner cache this spawner has been created
            try { CacheFactory.UpdateQuickTables(this, CacheFactory.QuickTableUpdate.Changed); }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            Instances.Add(this);
        }

        public Spawner(Serial serial)
            : base(serial)
        {
            Instances.Add(this);
        }
    }
}
