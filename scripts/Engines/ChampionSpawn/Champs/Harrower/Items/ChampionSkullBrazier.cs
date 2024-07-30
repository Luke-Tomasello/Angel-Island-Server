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

/* Scripts\Engines\ChampionSpawn\Champs\Harrower\Items\ChampionSkullBrazier.cs
 * CHANGELOG
 *  03/09/07, plasma    
 *      Change to ChampionSapwn namespace (again) 
 *  01/05/07, plasma!
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *	12/21/04, Pigpen
 *		Changed Item type to Fire pit. This is to fix players not being able to walk in the gate.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.ChampionSpawn
{
    public class ChampionSkullBrazier : AddonComponent
    {
        private ChampionSkullPlatform m_Platform;
        private ChampionSkullType m_Type;
        private Item m_Skull;

        [CommandProperty(AccessLevel.GameMaster)]
        public ChampionSkullPlatform Platform { get { return m_Platform; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public ChampionSkullType Type { get { return m_Type; } set { m_Type = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Skull { get { return m_Skull; } set { m_Skull = value; if (m_Platform != null) m_Platform.Validate(); } }

        public override int LabelNumber { get { return 1049489 + (int)m_Type; } }

        public ChampionSkullBrazier(ChampionSkullPlatform platform, ChampionSkullType type)
            : base(0xFAC) //changed from 0x19BB(brazier) to 0xFAC(fire pit), Pigpen
        {
            //Hue = 0x455; (removed hueing.) Pigpen
            Light = LightType.Circle300;

            m_Platform = platform;
            m_Type = type;
        }

        public ChampionSkullBrazier(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Platform != null)
                m_Platform.Validate();

            BeginSacrifice(from);
        }

        public void BeginSacrifice(Mobile from)
        {
            if (Deleted)
                return;

            if (m_Skull != null && m_Skull.Deleted)
                Skull = null;

            if (from.Map != this.Map || !from.InRange(GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (!Harrower.CanSpawn)
            {
                from.SendMessage("The harrower has already been spawned.");
            }
            else if (m_Skull == null)
            {
                from.SendLocalizedMessage(1049485); // What would you like to sacrifice?
                from.Target = new SacrificeTarget(this);
            }
            else
            {
                SendLocalizedMessageTo(from, 1049487, ""); // I already have my champions awakening skull!
            }
        }

        public void EndSacrifice(Mobile from, ChampionSkull skull)
        {
            if (Deleted)
                return;

            if (m_Skull != null && m_Skull.Deleted)
                Skull = null;

            if (from.Map != this.Map || !from.InRange(GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (!Harrower.CanSpawn)
            {
                from.SendMessage("The harrower has already been spawned.");
            }
            else if (skull == null)
            {
                SendLocalizedMessageTo(from, 1049488, ""); // That is not my champions awakening skull!
            }
            else if (m_Skull != null)
            {
                SendLocalizedMessageTo(from, 1049487, ""); // I already have my champions awakening skull!
            }
            else if (!skull.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1049486); // You can only sacrifice items that are in your backpack!
            }
            else
            {
                if (skull.Type == this.Type)
                {
                    skull.Movable = false;
                    skull.MoveToWorld(GetWorldTop(), this.Map);

                    this.Skull = skull;
                }
                else
                {
                    SendLocalizedMessageTo(from, 1049488, ""); // That is not my champions awakening skull!
                }
            }
        }

        private class SacrificeTarget : Target
        {
            private ChampionSkullBrazier m_Brazier;

            public SacrificeTarget(ChampionSkullBrazier brazier)
                : base(12, false, TargetFlags.None)
            {
                m_Brazier = brazier;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                m_Brazier.EndSacrifice(from, targeted as ChampionSkull);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Type);
            writer.Write(m_Platform);
            writer.Write(m_Skull);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Type = (ChampionSkullType)reader.ReadInt();
                        m_Platform = reader.ReadItem() as ChampionSkullPlatform;
                        m_Skull = reader.ReadItem();

                        if (m_Platform == null)
                            Delete();

                        break;
                    }
            }

            if (Hue == 0x497)
                Hue = 0x455;

            if (Light != LightType.Circle300)
                Light = LightType.Circle300;
        }
    }
}