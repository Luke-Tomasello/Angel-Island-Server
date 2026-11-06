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

/* scripts\Engines\Travel\JailExitGate.cs
 * CHANGELOG
 *  10/18/26, Adam (confinement Stage 5)
 *      ValidateUse asks the release vetoes (ConfinementVetoes) before the jail check-out: a refused release keeps the
 *      jail record (reported only when ValidateUse reports).
 *  10/17/26, Adam
 *      Confinement Stage 2: the jail record, account flag and chat ban go through JailOperations /
 *      ConfinementBookkeeping (ConfinementOperations.cs). Same changes, same order.
 *      Confinement Stage 3: facet-local release. The gate releases to its own facet: OnMapChange sets MapDest to the
 *      gate's map (the constructor still says Felucca, which only matters until the gate is placed). Gates mirrored to
 *      Trammel by MirrorFelucca already had MapDest = Trammel; Felucca gates are unchanged.
 *  11/13/05 Taran Kain
 *		Changed sentence back to using DateTime.UtcNow, allowing players to burn off time in or out of game.
 *  11/06/05 Taran Kain
 *		Changed inmate sentence storage method to use GameTime - now players will only burn off their jailtime if they're in-game.
 *  09/01/05 Taran Kain
 *		First version. Keeps track of [jail'ed players and allows them to leave when their sentence is up.
 *		Added JEGFactory object to only allow Administrators to create JailExitGates
 */

using Server.Diagnostics;
using Server.Engines.ConfinementSystem;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Items
{
    public class JEGFactory : Item
    {
        [Constructable]
        public JEGFactory()
            : base(0xF8B)
        {
            Name = "Double click to create a JailExitGate here.";
        }

        public JEGFactory(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Administrator)
            {
                new JailExitSungate().MoveToWorld(Location, Map);
                this.Delete();
            }
            else
                from.SendMessage("You must have Administrator access to use this object.");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }

    public class JailExitSungate : Sungate
    {
        public override Item Dupe(int amount)
        {
            JailExitSungate new_item = new JailExitSungate();
            Utility.CopyProperties(this, new_item);
            return base.Dupe(new_item, amount);
        }
        public JailExitSungate()
            : base(new Point3D(829, 1081, 5), Map.Felucca, dispellable: false)
        {
            Hue = 0x2D1;
            Name = "to Oc'Neville";
        }

        public JailExitSungate(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FeluccaInmates => JailSystem.CountInmates(Map.Felucca);

        [CommandProperty(AccessLevel.GameMaster)]
        public int TrammelInmates => JailSystem.CountInmates(Map.Trammel);

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalInmates => JailSystem.TotalInmates;

        // the gate's own facet is the release facet (a Trammel jail releases to Trammel): the constructor's Felucca
        //  destination is replaced whenever the gate is placed or moved (e.g. created on Trammel by a JEGFactory)
        public override void OnMapChange()
        {
            base.OnMapChange();
            if (Map != null && Map != Map.Internal && MapDest != Map)
                MapDest = Map;
        }

        public override void DoTeleport(Mobile m)
        {
            base.DoTeleport(m);

            // Release global chat ban when they exit jail
            if (m is PlayerMobile pm)
                ConfinementBookkeeping.LiftChatBan(pm);
        }

        public override bool ValidateUse(Mobile from, bool message)
        {
            if (!base.ValidateUse(from, message))
                return false;

            if (from is not PlayerMobile)
                return true;

            // release vetoes (ConfinementVetoes), before the check-out below
            if (ConfinementVetoes.RefuseAutomated(from, MapDest ?? Map, "the jail exit gate", report: message))
                return false;

            if (!JailSystem.TryGetReleaseTime(from, out DateTime releaseTime))
            {
                // Not in jail
                JailOperations.ClearAccountJailed(from);

                return true;
            }

            if (releaseTime <= DateTime.UtcNow)
            {
                // Sentence expired (jail check-out: record and account flag)
                JailOperations.RemoveRecord(from);
                JailOperations.ClearAccountJailed(from);

                return true;
            }

            // Still serving time
            TimeSpan ts = releaseTime - DateTime.UtcNow;
            from.SendMessage("There are still {0} left in your sentence.", FormatRemainingTime(ts));
            return false;
        }

        private static string FormatRemainingTime(TimeSpan ts)
        {
            var sb = new StringBuilder();

            if (ts.TotalHours >= 1)
            {
                int hours = (int)ts.TotalHours;
                sb.AppendFormat("{0} hour{1}", hours, hours > 1 ? "s" : "");
                ts -= TimeSpan.FromHours(hours);
                if (ts.Minutes > 0)
                    sb.Append(" and ");
            }

            if (ts.Minutes > 0)
                sb.AppendFormat("{0} minute{1}", ts.Minutes, ts.Minutes > 1 ? "s" : "");

            return sb.Length > 0 ? sb.ToString() : "less than a minute";
        }

        // ===================================================================
        // Serialization - Version 4+ (no more inmate data stored here)
        // ===================================================================
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)4);        // Current version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            // Discard old inmate data from versions 0-3
            if (version <= 3)
            {
                int count = reader.ReadInt();

                for (int i = 0; i < count; i++)
                {
                    reader.ReadMobile();

                    if (version == 2)
                        reader.ReadTimeSpan();
                    else
                        reader.ReadDateTime();
                }
            }
            // Version 4+: Nothing else to read
        }
    }

    public static class JailSystem
    {
        private static readonly Dictionary<Mobile, DateTime> m_Inmates = new Dictionary<Mobile, DateTime>();

        private const string FilePath = "Saves/Inmates.bin";

        public static void Configure()
        {
            EventSink.WorldSave += OnWorldSave;
            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            Save();
        }

        private static void OnWorldLoad()
        {
            Load();
        }

        #region Public API

        public static void AddInmate(Mobile inmate, int hours)
        {
            if (inmate == null || inmate is not PlayerMobile)
                return;

            m_Inmates[inmate] = DateTime.UtcNow + TimeSpan.FromHours(hours);
        }

        public static void RemoveInmate(Mobile inmate)
        {
            if (inmate != null)
                m_Inmates.Remove(inmate);
        }

        public static bool TryGetReleaseTime(Mobile inmate, out DateTime releaseTime)
        {
            return m_Inmates.TryGetValue(inmate, out releaseTime);
        }

        public static int TotalInmates
        {
            get
            {
                CleanupExpired();
                return m_Inmates.Count;
            }
        }

        public static int CountInmates(Map map)
        {
            if (map == null) return 0;
            CleanupExpired();

            int count = 0;
            foreach (var kvp in m_Inmates)
            {
                if (kvp.Key?.Map == map)
                    count++;
            }
            return count;
        }

        #endregion

        private static void CleanupExpired()
        {
            var now = DateTime.UtcNow;
            var toRemove = new List<Mobile>();

            foreach (var kvp in m_Inmates)
            {
                if (kvp.Value <= now)
                    toRemove.Add(kvp.Key);
            }

            foreach (var m in toRemove)
                m_Inmates.Remove(m);
        }
        #region Persistence (Binary - Inmates.bin)

        public static void Load()
        {
            if (!File.Exists(FilePath))
                return;

            Console.WriteLine("Jail System Loading...");

            BinaryFileReader? reader = null;
            try
            {
                reader = new BinaryFileReader(
                    new BinaryReader(new FileStream(FilePath, FileMode.Open, FileAccess.Read)));

                int version = reader.ReadInt();

                m_Inmates.Clear();

                int count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Mobile? m = reader.ReadMobile();
                    DateTime dt = reader.ReadDateTime();

                    if (m != null && dt > DateTime.UtcNow)
                        m_Inmates[m] = dt;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading Inmates.bin");
                LogHelper.LogException(ex);
            }
            finally
            {
                reader?.Close();
            }
        }

        public static void Save()
        {
            Console.WriteLine("Jail System Saving...");

            BinaryFileWriter? writer = null;
            try
            {
                writer = new BinaryFileWriter(FilePath, true);

                writer.Write((int)1);           // JailSystem version
                CleanupExpired();

                writer.Write(m_Inmates.Count);

                foreach (var kvp in m_Inmates)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Inmates.bin");
                LogHelper.LogException(ex);
            }
            finally
            {
                writer?.Close();
            }
        }

        #endregion
    }

}