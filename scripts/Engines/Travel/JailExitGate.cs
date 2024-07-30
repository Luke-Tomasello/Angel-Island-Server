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

/* scripts\Engines\Travel\JailExitGate.cs
 * CHANGELOG
 *  11/13/05 Taran Kain
 *		Changed sentence back to using DateTime.UtcNow, allowing players to burn off time in or out of game.
 *  11/06/05 Taran Kain
 *		Changed inmate sentence storage method to use GameTime - now players will only burn off their jailtime if they're in-game.
 *  09/01/05 Taran Kain
 *		First version. Keeps track of [jail'ed players and allows them to leave when their sentence is up.
 *		Added JEGFactory object to only allow Administrators to create JailExitGates
 */

using Server.Mobiles;
using System;
using System.Collections;
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

    // Obsolete - Use JailExitSungate
    [Obsolete]
    public class JailExitGate : Moongate
    {
        public static JailExitGate Instance = null;

        private Hashtable m_Inmates;

        public JailExitGate()
            : base(new Point3D(829, 1081, 5), Map.Felucca) // gate points to Oc'Nivelle Bank
        {
            Hue = 0x2D1;
            Name = "to Oc'Neville";
            m_Inmates = new Hashtable();
            VerifyHighlander();
        }

        public JailExitGate(Serial serial)
            : base(serial)
        {
            VerifyHighlander();
        }

        // there can only be one!!
        private void VerifyHighlander()
        {
            if (JailExitGate.Instance != null)
            {
                m_Inmates = new Hashtable(Instance.m_Inmates);
                Instance.Delete();
            }

            JailExitGate.Instance = this;
        }

        public override bool ValidateUse(Mobile from, bool message)
        {
            if (!base.ValidateUse(from, message))
                return false;

            if (m_Inmates == null || Instance == null)
            {
                from.SendMessage("Tell a GM that the JailExitGate is broken. Hopefully they'll pity you.");
                return false;
            }

            if (!m_Inmates.ContainsKey(from) || !(from is PlayerMobile))
                return true;

            if (!(m_Inmates[from] is DateTime) || ((DateTime)m_Inmates[from]) <= DateTime.UtcNow)
            {
                m_Inmates.Remove(from);
                return true;
            }

            TimeSpan ts = (DateTime)m_Inmates[from] - DateTime.UtcNow;
            StringBuilder sb = new StringBuilder();
            if (ts.TotalHours >= 1)
            {
                sb.AppendFormat("{0} hours", (int)ts.TotalHours);
                ts -= TimeSpan.FromHours((int)ts.TotalHours);
                if (ts.Minutes > 0)
                    sb.Append(" and ");
            }
            if (ts.Minutes > 0)
                sb.AppendFormat("{0} minutes", ts.Minutes);

            from.SendMessage("There are still {0} left in your sentence.", sb.ToString());
            return false;
        }

        public static void AddInmate(Mobile inmate, int hours)
        {
            if (Instance == null || Instance.m_Inmates == null || !(inmate is PlayerMobile))
                return;

            Instance.m_Inmates[inmate] = DateTime.UtcNow + TimeSpan.FromHours(hours);
        }

        private void ValidateInmates()
        {
            int count = m_Inmates.Count;
            ArrayList keys = new ArrayList(m_Inmates.Keys),
                values = new ArrayList(m_Inmates.Values);

            for (int i = 0; i < count; i++)
            {
                PlayerMobile pm = keys[i] as PlayerMobile;
                if (pm == null || !(values[i] is DateTime) || ((DateTime)values[i]) <= DateTime.UtcNow)
                    m_Inmates.Remove(keys[i]);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3);

            ValidateInmates();
            int count = m_Inmates.Count;
            ArrayList keys = new ArrayList(m_Inmates.Keys),
                      values = new ArrayList(m_Inmates.Values);

            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                writer.Write((Mobile)keys[i]);
                writer.Write((DateTime)values[i]);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 3:
                    {
                        m_Inmates = new Hashtable();
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            DateTime dt = reader.ReadDateTime();
                            m_Inmates.Add(m, dt);
                        }
                        break;
                    }
                case 2:
                    {
                        m_Inmates = new Hashtable();
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            TimeSpan ts = reader.ReadTimeSpan();
                            DateTime dt = DateTime.UtcNow + ts;
                            m_Inmates.Add(m, dt);
                        }
                        break;
                    }
                case 1:
                    {
                        m_Inmates = new Hashtable();
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            DateTime dt = reader.ReadDateTime();
                            m_Inmates.Add(m, dt);
                        }
                        break;
                    }
                case 0:
                    {
                        m_Inmates = new Hashtable();
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            DateTime dt = reader.ReadDateTime() + TimeSpan.FromHours(24);
                            m_Inmates.Add(m, dt);
                        }
                        break;
                    }
            }
        }
    }
    public class JailExitSungate : Sungate
    {
        public static JailExitSungate Instance = null;

        private Hashtable m_Inmates;

        public JailExitSungate()
            : base(new Point3D(829, 1081, 5), Map.Felucca, dispellable: false) // gate points to Oc'Nivelle Bank
        {
            Hue = 0x2D1;
            Name = "to Oc'Neville";
            m_Inmates = new Hashtable();
            VerifyHighlander();
        }

        public JailExitSungate(Serial serial)
            : base(serial)
        {
            VerifyHighlander();
        }
        public override void DoTeleport(Mobile m)
        {
            base.DoTeleport(m);
            // release the lock on global chat
            Server.Engines.Chat.ChatHelper.SetChatBan(((PlayerMobile)m), false);
        }
        // there can only be one!!
        private void VerifyHighlander()
        {
            if (JailExitSungate.Instance != null)
            {
                m_Inmates = new Hashtable(Instance.m_Inmates);
                Instance.Delete();
            }

            JailExitSungate.Instance = this;
        }

        public override bool ValidateUse(Mobile from, bool message)
        {
            if (!base.ValidateUse(from, message))
                return false;

            if (m_Inmates == null || Instance == null)
            {
                from.SendMessage("Tell a GM that the JailExitGate is broken. Hopefully they'll pity you.");
                return false;
            }

            if (!m_Inmates.ContainsKey(from) || !(from is PlayerMobile))
                return true;

            if (!(m_Inmates[from] is DateTime) || ((DateTime)m_Inmates[from]) <= DateTime.UtcNow)
            {
                m_Inmates.Remove(from);
                return true;
            }

            TimeSpan ts = (DateTime)m_Inmates[from] - DateTime.UtcNow;
            StringBuilder sb = new StringBuilder();
            if (ts.TotalHours >= 1)
            {
                sb.AppendFormat("{0} hours", (int)ts.TotalHours);
                ts -= TimeSpan.FromHours((int)ts.TotalHours);
                if (ts.Minutes > 0)
                    sb.Append(" and ");
            }
            if (ts.Minutes > 0)
                sb.AppendFormat("{0} minutes", ts.Minutes);

            from.SendMessage("There are still {0} left in your sentence.", sb.ToString());
            return false;
        }

        public static void AddInmate(Mobile inmate, int hours)
        {
            if (Instance == null || Instance.m_Inmates == null || !(inmate is PlayerMobile))
                return;

            Instance.m_Inmates[inmate] = DateTime.UtcNow + TimeSpan.FromHours(hours);
        }

        private void ValidateInmates()
        {
            int count = m_Inmates.Count;
            ArrayList keys = new ArrayList(m_Inmates.Keys),
                values = new ArrayList(m_Inmates.Values);

            for (int i = 0; i < count; i++)
            {
                PlayerMobile pm = keys[i] as PlayerMobile;
                if (pm == null || !(values[i] is DateTime) || ((DateTime)values[i]) <= DateTime.UtcNow)
                    m_Inmates.Remove(keys[i]);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3);

            ValidateInmates();
            int count = m_Inmates.Count;
            ArrayList keys = new ArrayList(m_Inmates.Keys),
                      values = new ArrayList(m_Inmates.Values);

            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                writer.Write((Mobile)keys[i]);
                writer.Write((DateTime)values[i]);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 3:
                    {
                        m_Inmates = new Hashtable();
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            DateTime dt = reader.ReadDateTime();
                            if (m != null)
                                m_Inmates.Add(m, dt);
                        }
                        break;
                    }
                case 2:
                    {
                        m_Inmates = new Hashtable();
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            TimeSpan ts = reader.ReadTimeSpan();
                            DateTime dt = DateTime.UtcNow + ts;
                            m_Inmates.Add(m, dt);
                        }
                        break;
                    }
                case 1:
                    {
                        m_Inmates = new Hashtable();
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            DateTime dt = reader.ReadDateTime();
                            m_Inmates.Add(m, dt);
                        }
                        break;
                    }
                case 0:
                    {
                        m_Inmates = new Hashtable();
                        int count = reader.ReadInt();
                        for (int i = 0; i < count; i++)
                        {
                            Mobile m = reader.ReadMobile();
                            DateTime dt = reader.ReadDateTime() + TimeSpan.FromHours(24);
                            m_Inmates.Add(m, dt);
                        }
                        break;
                    }
            }
        }
    }
}