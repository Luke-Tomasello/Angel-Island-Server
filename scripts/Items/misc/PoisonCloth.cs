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

//  /Scripts/Items/Misc/PoisonCloth.cs
//	CHANGE LOG
//  04/17/2004 - Pulse
//		This item is totally new, created for the Angel Island shard.
//		This item is created when a piece of cloth is poisoned.
//		If this item is in a players root backpack, when they fire a ranged 
//		weapon, the ranged weapon will be poisoned for 1 round and a poison
//		charge is subtracted from this item.  When the item's PoisonCharges count
//		reaches 0 during use the item is deleted.

using System.Collections;

namespace Server.Items
{
    public class PoisonCloth : Item
    {
        private Poison m_Poison;
        private int m_PoisonCharges;
        private double m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison
        {
            get { return m_Poison; }
            set { m_Poison = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PoisonCharges
        {
            get { return m_PoisonCharges; }
            set { m_PoisonCharges = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; }
        }

        [Constructable]
        public PoisonCloth()
            : base(0x175D)
        {
            Weight = 1.0;
            Hue = 63;
            Stackable = false;
            Amount = 1;
            Poison = null;
            PoisonCharges = 0;
            Delay = 0.0;
            Name = "a poison soaked rag";
        }

        public override void OnSingleClick(Mobile from)
        {
            ArrayList attrs = new ArrayList();

            if (Poison != null && PoisonCharges > 0)
                this.LabelTo(from, Name + "\n[Poisoned: " + PoisonCharges.ToString() + "]");
            else
                this.LabelTo(from, Name);
        }


        public PoisonCloth(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            Poison.Serialize(m_Poison, writer);
            writer.Write((int)m_PoisonCharges);
            writer.Write((double)m_Delay);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_Poison = Poison.Deserialize(reader);
            m_PoisonCharges = reader.ReadInt();
            m_Delay = reader.ReadDouble();
        }
    }
}