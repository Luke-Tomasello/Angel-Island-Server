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

/* Scripts/Engines/AngelIsland/Items/TriggerSpawner.cs
 * ChangeLog
 *  10/22/26, Adam
 *      Documented the trigger key model (no code change).
 *
 * Overview
 *  A TriggerSpawner is a normal Spawner that never respawns on its own (Running false, zero delays); it spawns
 *  only when Trigger(key, map) is called with its own key on its own map. The key (m_key, set when the spawner is
 *  created: [add TriggerSpawner <key>) is a named signal, not an object reference:
 *  - A sender broadcasts the key to every registered TriggerSpawner (TriggerSpawner.Instances); each one on the
 *    sender's map whose key matches respawns. Spawners on other facets ignore it, so Felucca and Trammel may share
 *    a key and still act independently.
 *  - Any number of spawners may listen for the same key: one event can have several effects on a facet (by design).
 *  Known senders:
 *  - AIGuardCaptain.OnBeforeDeath: key 0xCE1EB734, the prison Captain of the Guard's death (spawns the chest on the
 *    captain's facet). Any captain dying on that facet sends it, not only the champion engine's.
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class TriggerSpawner : Spawner
    {
        public override Item Dupe(int amount)
        {
            TriggerSpawner new_spawner = new TriggerSpawner(m_key);
            Utility.CopyProperties(this, new_spawner);

            return base.Dupe(new_spawner, amount);
        }
        private static readonly List<TriggerSpawner> m_Instances = new List<TriggerSpawner>();
        public new static List<TriggerSpawner> Instances { get { return m_Instances; } }

        uint m_key;
        [Constructable]
        public TriggerSpawner(uint key)
            : base()
        {
            base.Name = "Trigger Spawner";
            m_key = key;
            base.MinDelay = System.TimeSpan.Zero;
            base.MaxDelay = System.TimeSpan.Zero;
            base.Running = false;
            m_Instances.Add(this);
        }
        public override void OnAfterDelete()
        {
            m_Instances.Remove(this);
        }
        #region Hidden Properties
        public override bool ScheduleRespawn
        {
            get => base.ScheduleRespawn;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Running
        {
            get => base.Running;
        }
        public override TimeSpan MinDelay
        {
            get => base.MinDelay;
        }
        public override TimeSpan MaxDelay
        {
            get => base.MaxDelay;
        }
        #endregion Hidden Properties
        public TriggerSpawner(Serial serial)
            : base(serial)
        {
            m_Instances.Add(this);
        }

        // Respawn when the broadcast key is ours and the sender is on our map (see the header: one signal, per facet,
        //  any number of listeners).
        public void Trigger(uint key, Map map)
        {
            if (this.Map != map)
                return;

            if (m_key == key)
                base.Respawn();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_key); // 
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_key = reader.ReadUInt();
        }
    }
}