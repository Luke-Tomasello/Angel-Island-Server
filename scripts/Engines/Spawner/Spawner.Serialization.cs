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

/* Scripts\Engines\Spawner\Spawner.Serialization.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Items;
using System;
using System.Collections;
using System.IO;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        #region Save Flags
        [Flags]
        enum SaveFlags : UInt32
        {
            None = 0,

            Shard = 1u << 0,
            Source = 1u << 1,
            NeedsReview = 1u << 2,
            SpawnerAttribs = 1u << 3,
            WalkRange = 1u << 4,
            SlotCount = 1u << 5,
            PatchID = 1u << 6,

            Invulnerable = 1u << 8,
            WalkRangeCalc = 1u << 9,
            Exhibit = 1u << 10,
            SpawnFlags = 1u << 11,
            QuestCode = 1u << 12,
            GraphicID = 1u << 13,
            Followers = 1u << 14,
            SetProp = 1u << 15,
            SetSkill = 1u << 16,
            TemplateInternalize = 1u << 17,
            CarveOverride = 1u << 18,
            TriggerLinkPlayers = 1u << 19,
            Goodies = 1u << 20,
            Artifacts = 1u << 21,
            TemplateMobileDefinition = 1u << 22,
            Tamable = 1u << 23,
            NavDest = 1u << 24,
            LootString = 1u << 25,
            LootStringProps = 1u << 26,
            MutateEquipment = 1u << 27,
            TriggerLinkCreature = 1u << 28,
            DropsSterling = 1u << 29,
            MorphMode = 1u << 30,
        }

        private SaveFlags m_SaveFlags = SaveFlags.None;

        private void SetFlag(SaveFlags flag, bool value)
        {
            if (value)
                m_SaveFlags |= flag;
            else
                m_SaveFlags &= ~flag;
        }

        private bool GetFlag(SaveFlags flag)
        {
            return ((m_SaveFlags & flag) != 0);
        }

        private SaveFlags ReadSaveFlags(GenericReader reader, int version)
        {
            SaveFlags sf = SaveFlags.None;
            if (version > 16)
                sf = (SaveFlags)reader.ReadInt();
            return sf;
        }

        private SaveFlags WriteSaveFlags(GenericWriter writer)
        {
            m_SaveFlags = SaveFlags.None;
            SetFlag(SaveFlags.Shard, m_Shard != Server.Mobiles.Spawner.ShardConfig.AngelIsland ? true : false);    // don't save unless the shard config is not AI
            SetFlag(SaveFlags.Source, m_Source != string.Empty ? true : false);             // Sometimes these areas: https://uo.stratics.com/hunters/spawn/spawnmap.jpg 
            SetFlag(SaveFlags.NeedsReview, m_NeedsReview != false ? true : false);          // this spawner needs to be reviewed.
            SetFlag(SaveFlags.SpawnerAttribs, m_SpawnerAttribs != Server.Mobiles.Spawner.SpawnerAttribs.None ? true : false);
            SetFlag(SaveFlags.WalkRange, m_WalkRange != -1 ? true : false);
            SetFlag(SaveFlags.SlotCount, Count > 0 ? true : false);
            SetFlag(SaveFlags.PatchID, m_PatchID > 0 ? true : false);
            SetFlag(SaveFlags.Invulnerable, m_Invulnerable ? true : false);
            SetFlag(SaveFlags.WalkRangeCalc, m_WalkRangeCalc != false ? true : false);
            SetFlag(SaveFlags.Exhibit, m_Exhibit != false ? true : false);
            SetFlag(SaveFlags.SpawnFlags, m_SpawnerFlags != Utility.SpawnFlags.None ? true : false);
            SetFlag(SaveFlags.QuestCode, m_QuestCode != 0 ? true : false);
            SetFlag(SaveFlags.GraphicID, GraphicID != 0 ? true : false);
            SetFlag(SaveFlags.Followers, !string.IsNullOrEmpty(m_Followers));
            SetFlag(SaveFlags.SetProp, m_SetProp != null ? true : false);
            SetFlag(SaveFlags.SetSkill, m_SetSkill != null ? true : false);
            SetFlag(SaveFlags.TemplateInternalize, m_TemplateInternalize ? true : false);
            SetFlag(SaveFlags.CarveOverride, m_CarveOverride ? true : false);
            SetFlag(SaveFlags.TriggerLinkPlayers, m_TriggerLinkPlayers != null ? true : false);
            SetFlag(SaveFlags.TriggerLinkCreature, m_TriggerLinkCreature != null ? true : false);
            SetFlag(SaveFlags.Goodies, m_GoodiesRadius != 0 || m_GoodiesTotalMin != 0 || m_GoodiesTotalMax != 0);
            SetFlag(SaveFlags.Artifacts, m_ArtifactPack != null || m_ArtifactCount != 0);
            SetFlag(SaveFlags.TemplateMobileDefinition, m_TemplateMobileDefinition != 0);
            SetFlag(SaveFlags.Tamable, Tamable != Server.Mobiles.Spawner.BoolFlags.Default);
            SetFlag(SaveFlags.NavDest, !string.IsNullOrEmpty(m_NavDest));
            SetFlag(SaveFlags.LootString, !string.IsNullOrEmpty(m_LootString));
            SetFlag(SaveFlags.LootStringProps, !string.IsNullOrEmpty(m_LootStringProps));
            SetFlag(SaveFlags.MutateEquipment, m_EquipmentDisposition != BaseCreature.EquipmentDisposition.None);
            SetFlag(SaveFlags.DropsSterling, GetFlag(Server.Mobiles.Spawner.SpawnerAttribs.DropsSterling));
            SetFlag(SaveFlags.MorphMode, m_MorphMode != Utility.MorphMode.Default);

            writer.Write((int)m_SaveFlags);
            return m_SaveFlags;
        }
        #endregion Save Flags
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 48;
            writer.Write(version);                  // version
            m_SaveFlags = WriteSaveFlags(writer);   // always follows version

            // version 48
            if (GetFlag(SaveFlags.Followers))
                writer.Write(m_Followers);

            // version 47
            if (GetFlag(SaveFlags.MorphMode))
                writer.Write((byte)m_MorphMode);

            // version 46
            if (GetFlag(SaveFlags.DropsSterling))
            {
                writer.Write(m_SterlingMin);
                writer.Write(m_SterlingMax);
            }

            // version 45, eliminate m_LastChange. Now in base class

            // v44
            if (GetFlag(SaveFlags.TriggerLinkCreature))
                writer.Write((Item)m_TriggerLinkCreature);

            // v43
            if (GetFlag(SaveFlags.MutateEquipment))
                writer.Write((int)m_EquipmentDisposition);

            // v42
            if (GetFlag(SaveFlags.LootString))
                writer.Write(m_LootString);

            if (GetFlag(SaveFlags.LootStringProps))
                writer.Write(m_LootStringProps);

            // v41
            if (GetFlag(SaveFlags.NavDest))
                writer.Write(m_NavDest);

            // v40
            if (GetFlag(SaveFlags.Tamable))
                writer.Write((byte)m_Tamable);

            // v39
            if (GetFlag(SaveFlags.TemplateMobileDefinition))
                writer.Write(m_TemplateMobileDefinition);

            // v37
            if (GetFlag(SaveFlags.Artifacts))
            {
                writer.Write((Item)m_ArtifactPack);
                writer.Write((int)m_ArtifactCount);
            }

            // v36
            if (GetFlag(SaveFlags.Goodies))
            {
                writer.Write((int)m_GoodiesRadius);
                writer.Write((int)m_GoodiesTotalMin);
                writer.Write((int)m_GoodiesTotalMax);
            }

            // v35
            if (GetFlag(SaveFlags.TriggerLinkPlayers))
                writer.Write((Item)m_TriggerLinkPlayers);

            // v34
            // added CarveOverride save flag

            // v33
            // added InternalizeTemplates save flag

            // v32
            if (GetFlag(SaveFlags.SetSkill))
                writer.Write(m_SetSkill);

            // v31
            if (GetFlag(SaveFlags.SetProp))
                writer.Write(m_SetProp);

            // v30 - obsolete in version 45
            //if (GetFlag(SaveFlags.LastChange))
            //writer.Write(m_LastChange);

            // v29
            if (GetFlag(SaveFlags.SlotCount))
            {
                writer.Write(GetSlotTableSize());
                foreach (int ix in m_SlotCount)
                    writer.Write(ix);
            }

            // v28
            if (GetFlag(SaveFlags.GraphicID))
            {
                writer.Write(m_GraphicID);
            }

            // v27
            if (GetFlag(SaveFlags.QuestCode))
            {
                writer.Write(m_QuestCode);
                writer.Write(m_QuestCodeChance);
            }

            // v26
            // eliminate v13 m_isConcentric

            // v25
            if (GetFlag(SaveFlags.SpawnFlags))
                writer.Write((int)m_SpawnerFlags);

            // v24 
            if (GetFlag(SaveFlags.Exhibit))
                writer.Write(m_Exhibit);

            // v23
            if (GetFlag(SaveFlags.WalkRangeCalc))
                writer.Write(m_WalkRangeCalc);

            // v22
            if (GetFlag(SaveFlags.Invulnerable))
                writer.Write(m_Invulnerable);

            // v21
            if (GetFlag(SaveFlags.PatchID))
                writer.Write(m_PatchID);

            // v20
            #region OBSOLETE
            #endregion OBSOLETE
            // v19
            if (GetFlag(SaveFlags.WalkRange))
                writer.Write(m_WalkRange);

            // v18
            if (GetFlag(SaveFlags.SpawnerAttribs))
                writer.Write((UInt32)m_SpawnerAttribs);

            // v17
            if (GetFlag(SaveFlags.Shard))
                writer.Write((int)m_Shard);
            if (GetFlag(SaveFlags.Source))
                writer.Write(m_Source);
            if (GetFlag(SaveFlags.NeedsReview))
                writer.Write(m_NeedsReview);

            // v16
            m_GoldDice.Serialize(writer);

            // v15
            writer.Write((Item)m_CarvePack);
            writer.Write((string)m_CarveMessage);

            // ver 14
            writer.Write(m_CoreSpawn);

            //version 13 - eliminated in version 26
            // writer.Write(m_isConcentric);

            //v12 - obsolete in version 38
            //writer.Write((int)m_OurWorldSize);

            // v11
            writer.Write(m_LootPack);

            // v10
            writer.Write(m_DynamicCopy);
            writer.Write(m_TemplateMobile);
            writer.Write(m_TemplateItem);

            // not used.. just throw away - obsolete in version 38
            //writer.Write((bool)TemplateEnabled);

            if (version <= 21)
                writer.Write((int)0); // 8

            //writer.Write((int)m_NavDest);         // recast as string in v41
            writer.Write((int)m_MobDirection);
            // obsolete in version 38
            //writer.Write(false);                // obsolete: m_FreezeDecay
            writer.Write(m_WayPoint);
            writer.Write(m_Group);
            writer.Write(m_MinDelay);
            writer.Write(m_MaxDelay);
            //writer.Write(m_SlotCount);
            writer.Write(m_Team);
            writer.Write(m_HomeRange);

            // obsolete in version 38
            // writer.Write(Running);

            if (Running)
                writer.WriteDeltaTime(m_End);

            writer.Write(m_ObjectNames.Count);

            for (int i = 0; i < m_ObjectNames.Count; ++i)
                writer.Write((string)m_ObjectNames[i]);

            writer.Write(m_Objects.Count);

            for (int i = 0; i < m_Objects.Count; ++i)
            {
                object? o = m_Objects[i];

                if (o is Item)
                    writer.Write((Item)o);
                else if (o is Mobile)
                    writer.Write((Mobile)o);
                else
                    writer.Write(Serial.MinusOne);
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_SaveFlags = ReadSaveFlags(reader, version);       // must always follow version

            switch (version)
            {
                case 48:
                    {
                        if (GetFlag(SaveFlags.Followers))
                            m_Followers = reader.ReadString();
                        goto case 47;
                    }
                case 47:
                    {
                        if (GetFlag(SaveFlags.MorphMode))
                            m_MorphMode = (Utility.MorphMode)reader.ReadByte();
                        goto case 46;
                    }
                case 46:
                    {
                        if (GetFlag(SaveFlags.DropsSterling))
                        {
                            m_SterlingMin = reader.ReadUShort();
                            m_SterlingMax = reader.ReadUShort();
                        }
                        goto case 45;
                    }
                case 45:
                    // eliminate m_LastChange - moved to base class
                    goto case 44;
                case 44:
                    {
                        if (GetFlag(SaveFlags.TriggerLinkCreature))
                            m_TriggerLinkCreature = reader.ReadItem();
                        goto case 43;
                    }
                case 43:
                    {
                        if (GetFlag(SaveFlags.MutateEquipment))
                            m_EquipmentDisposition = (BaseCreature.EquipmentDisposition)reader.ReadInt();
                        goto case 42;
                    }
                case 42:
                    {
                        if (GetFlag(SaveFlags.LootString))
                            m_LootString = reader.ReadString();

                        if (GetFlag(SaveFlags.LootStringProps))
                            m_LootStringProps = reader.ReadString();

                        goto case 41;
                    }
                case 41:
                    {
                        if (GetFlag(SaveFlags.NavDest))
                            m_NavDest = reader.ReadString();

                        goto case 40;
                    }
                case 40:
                    {
                        if (GetFlag(SaveFlags.Tamable))
                            m_Tamable = (Server.Mobiles.Spawner.BoolFlags)reader.ReadByte();
                        goto case 39;
                    }
                case 39:
                    {
                        if (GetFlag(SaveFlags.TemplateMobileDefinition))
                            m_TemplateMobileDefinition = reader.ReadInt();
                        goto case 38;
                    }
                case 38:
                    {
                        goto case 37;
                    }
                case 37:
                    {
                        if (GetFlag(SaveFlags.Artifacts))
                        {
                            m_ArtifactPack = reader.ReadItem();
                            m_ArtifactCount = reader.ReadInt();
                        }
                        goto case 36;
                    }

                case 36:
                    {
                        if (GetFlag(SaveFlags.Goodies))
                        {
                            m_GoodiesRadius = reader.ReadInt();
                            m_GoodiesTotalMin = reader.ReadInt();
                            m_GoodiesTotalMax = reader.ReadInt();
                        }
                        goto case 35;
                    }

                case 35:
                    {
                        if (GetFlag(SaveFlags.TriggerLinkPlayers))
                            m_TriggerLinkPlayers = reader.ReadItem();
                        goto case 34;
                    }

                case 34:
                    {
                        m_CarveOverride = GetFlag(SaveFlags.CarveOverride);
                        goto case 33;
                    }

                case 33:
                    {
                        m_TemplateInternalize = GetFlag(SaveFlags.TemplateInternalize);
                        goto case 32;
                    }

                case 32:
                    {
                        if (GetFlag(SaveFlags.SetSkill))
                            m_SetSkill = reader.ReadString();
                        goto case 31;
                    }

                case 31:
                    {
                        if (GetFlag(SaveFlags.SetProp))
                            m_SetProp = reader.ReadString();
                        goto case 30;
                    }

                case 30:
                    {
                        //if (GetFlag(SaveFlags.LastChange))
                        //    m_LastChange = reader.ReadString();
                        if (version < 45)
                            // LastChange == 0x4000
                            if (GetFlag((SaveFlags)0x4000))
                                base.LastChange = reader.ReadString();
                        goto case 29;
                    }

                case 29:
                    {
                        if (GetFlag(SaveFlags.SlotCount))
                        {
                            int count = reader.ReadInt();
                            //m_SlotCount = new(count);
                            for (int ix = 0; ix < count; ix++)
                                SetSlotCount(index: ix, reader.ReadInt());
                        }
                        goto case 28;
                    }
                case 28:
                    {
                        if (GetFlag(SaveFlags.GraphicID))
                            m_GraphicID = reader.ReadInt();
                        goto case 27;
                    }
                case 27:
                    {
                        if (GetFlag(SaveFlags.QuestCode))
                        {
                            m_QuestCode = reader.ReadUInt();
                            m_QuestCodeChance = reader.ReadDouble();
                        }
                        goto case 26;
                    }
                case 26:
                    {
                        // elimination of v13 m_isConcentric
                        goto case 25;
                    }
                case 25:
                    {
                        if (GetFlag(SaveFlags.SpawnFlags))
                            m_SpawnerFlags = (Utility.SpawnFlags)reader.ReadInt();
                        goto case 24;
                    }
                case 24:
                    {
                        if (GetFlag(SaveFlags.Exhibit))
                            m_Exhibit = reader.ReadBool();
                        goto case 23;
                    }
                case 23:
                    {
                        if (GetFlag(SaveFlags.WalkRangeCalc))
                            m_WalkRangeCalc = reader.ReadBool();
                        goto case 22;
                    }
                case 22:
                    {
                        if (GetFlag(SaveFlags.Invulnerable))
                            m_Invulnerable = reader.ReadBool();
                        goto case 21;
                    }
                case 21:
                    {
                        if (GetFlag(SaveFlags.PatchID))
                            m_PatchID = reader.ReadUInt();
                        goto case 20;
                    }
                case 20:
                    {
                        if (GetFlag(SaveFlags.SlotCount) && version <= 28)
                        {
                            m_SlotCount = new(6);   // one extra since we will put m_Count at 0
                            for (int ix = 0; ix < 5 /*m_EntryCount.Length*/; ix++)
                                SetSlotCount(index: ix + 1, reader.ReadInt());
                        }
                        goto case 19;
                    }
                case 19:
                    {
                        if (GetFlag(SaveFlags.WalkRange))
                            m_WalkRange = reader.ReadInt();
                        goto case 18;
                    }
                case 18:
                    {
                        if (GetFlag(SaveFlags.SpawnerAttribs))
                            m_SpawnerAttribs = (Server.Mobiles.Spawner.SpawnerAttribs)reader.ReadUInt();
                        goto case 17;
                    }
                case 17:
                    {
                        if (GetFlag(SaveFlags.Shard))
                            m_Shard = (Server.Mobiles.Spawner.ShardConfig)reader.ReadInt();
                        if (GetFlag(SaveFlags.Source))
                            m_Source = reader.ReadString();
                        if (GetFlag(SaveFlags.NeedsReview))
                            m_NeedsReview = reader.ReadBool();
                        goto case 16;
                    }
                case 16:
                    {
                        m_GoldDice = new DiceEntry(reader);
                        goto case 15;
                    }
                case 15:
                    {
                        m_CarvePack = reader.ReadItem();
                        m_CarveMessage = reader.ReadString();
                        goto case 14;
                    }
                case 14:
                    {
                        m_CoreSpawn = reader.ReadBool();
                        goto case 13;
                    }
                case 13:
                    {
                        if (version < 26)
                        {
                            bool isConcentric = reader.ReadBool();
                            if (isConcentric)
                                m_SpawnerFlags |= Utility.SpawnFlags.Concentric;
                        }
                        goto case 12;
                    }
                case 12:
                    {
                        if (version < 38)
                            /*m_OurWorldSize = (CoreAI.WorldSize)*/
                            reader.ReadInt();
                        goto case 11;
                    }
                case 11:
                    {
                        m_LootPack = reader.ReadItem();
                        goto case 10;
                    }
                case 10:
                    {
                        m_DynamicCopy = reader.ReadBool();
                        goto case 9;
                    }
                case 9:
                    {
                        m_TemplateMobile = reader.ReadMobile();
                        m_TemplateItem = reader.ReadItem();
                        if (version < 38)
                            /*TemplateEnabled =*/
                            reader.ReadBool();

                        goto case 8;
                    }
                case 8:
                    {
                        if (version <= 21)
                            reader.ReadInt();
                        goto case 7;
                    }
                case 7:
                    {
                        if (version < 24)
                        {
                            int dummy = 0;
                            bool b = reader.ReadBool();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                            dummy = reader.ReadInt();
                        }
                        goto case 6;
                    }
                case 6:
                    {
                        if (version < 24)
                        {
                            int dummy = 0;
                            string? s = reader.ReadString();
                            dummy = reader.ReadInt();
                        }
                        goto case 5;
                    }
                case 5:
                    {   // obsolete in version 41
                        if (version < 41)
                            /*m_NavDest = (NavDestinations)*/
                            reader.ReadInt();
                        goto case 4;
                    }
                case 4:
                    {
                        m_MobDirection = (Direction)reader.ReadInt();
                        goto case 3;
                    }
                case 3:
                    {
                        if (version < 38)
                            // obsolete: m_FreezeDecay
                            reader.ReadBool();
                        goto case 2;
                    }

                case 2:
                    {
                        m_WayPoint = reader.ReadItem() as WayPoint;

                        goto case 1;
                    }

                case 1:
                    {
                        m_Group = reader.ReadBool();

                        goto case 0;
                    }

                case 0:
                    {
                        m_MinDelay = reader.ReadTimeSpan();
                        m_MaxDelay = reader.ReadTimeSpan();
                        if (version <= 28)
                        {
                            int old_count = reader.ReadInt();
                            SetSlotCount(index: 0, old_count);
                        }
                        m_Team = reader.ReadInt();
                        m_HomeRange = reader.ReadInt();

                        if (version < 38)
                            Running = reader.ReadBool();

                        TimeSpan ts = TimeSpan.Zero;

                        if (Running)
                            ts = reader.ReadDeltaTime() - DateTime.UtcNow;

                        int size = reader.ReadInt();

                        m_ObjectNames = new ArrayList(size);

                        for (int i = 0; i < size; ++i)
                        {
                            string? typeName = reader.ReadString();
                            m_ObjectNames.Add(typeName);

                            if (SpawnerType.GetType(typeName) == null)
                            {
                                // this is a bad spawner. It will be picked up elsewhere (OnLoad())
                                // when the GM tries to spawn something we don't have, like "Naturalist" this is the error.
                                //	We track it elsewhere, log it, and can go investigate/delete the spawner
                                ;//debug
                            }
                        }

                        int count = reader.ReadInt();

                        m_Objects = new ArrayList(count);

                        for (int i = 0; i < count; ++i)
                        {
                            IEntity? e = World.FindEntity(reader.ReadInt());

                            if (e != null)
                                m_Objects.Add(e);
                        }

                        if (Running)
                        {   // NextSpawn is in TS
                            if (ts < TimeSpan.Zero)
                                DoTimer(TimeSpan.Zero);
                            else if (ts > MaxDelay)
                                ResetTimer();
                            else
                                DoTimer(ts);
                        }

                        break;
                    }
            }
        }

        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/Spawner.bin"))
                return;

            Console.WriteLine("Spawner ID Table Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/Spawner.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                int id = reader.ReadInt();
                                if (!Registry.Contains(id))
                                    Registry.Add(id);
                            }

                            break;
                        }

                    default:
                        {
                            throw new Exception("Invalid Spawner.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Spawner.bin, using default values:", ConsoleColor.Red);
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Spawner ID Table Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/Spawner.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(Registry.Count);
                            foreach (var id in Registry)
                                writer.Write(id);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Spawner.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }
}
