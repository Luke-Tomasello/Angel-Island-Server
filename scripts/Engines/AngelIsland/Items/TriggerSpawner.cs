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

using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class TriggerSpawner : Spawner
    {
        private static readonly List<TriggerSpawner> m_Instances = new List<TriggerSpawner>();
        public static List<TriggerSpawner> Instances { get { return m_Instances; } }

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

        public void Trigger(uint key)
        {
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