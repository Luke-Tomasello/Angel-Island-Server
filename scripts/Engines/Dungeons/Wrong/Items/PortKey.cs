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

/* Scripts\Engines\Dungeons\Wrong\Items\PortKey.cs
 *	ChangeLog :
 *	6/17/2023, Adam
 *	    Fix a bug in Corrupted() if single clicked while still in the chest.
 *	6/9/2023, Adam
 *	    Special item required to active (currently) the teleporters to the Wrong mini-champ
 *		
 */

using System;

namespace Server.Items
{
    public class PortKey : Vase
    {
        public override string DefaultName => "a port key";
        [Constructable]
        public PortKey()
            : base()
        {
            Name = null;
        }

        public PortKey(Serial serial)
            : base(serial)
        {
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Expiry
        {
            get { return (m_Lifted + TimeSpan.FromMinutes(10)) - DateTime.UtcNow; }
        }
        public override void OnSingleClick(Mobile from)
        {
            if (Deleted || !from.CanSee(this))
                return;


            LabelTo(from, string.Format("{0}", DefaultName));
            if (Corrupted())
            {
                LabelTo(from, string.Format("({0})", "corrupted"));
                from.SendAsciiMessage("The ancient vase crumbles in your hands.");
                Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(DeleteTick), new object[] { from });
            }
            else
                LabelTo(from, string.Format("({0})", "active"));
            return;
        }
        public bool Corrupted()
        {   // 10 minutes to figure out how to use the port key before it poofs
            if (m_Lifted != DateTime.MinValue)
                return DateTime.UtcNow > m_Lifted + TimeSpan.FromMinutes(10);
            else
                return false;
        }
        private DateTime m_Lifted = DateTime.MinValue;
        public override void OnItemLifted(Mobile from, Item item)
        {
            base.OnItemLifted(from, item);
            if (m_Lifted == DateTime.MinValue)
                m_Lifted = DateTime.UtcNow;
        }
        private void DeleteTick(object state)
        {
            object[] aState = (object[])state;
            if (aState[0] != null && aState[0] is Mobile from && !this.Deleted)
                from.PlaySound(0x3A4);

            if (!this.Deleted)
            {
                this.Delete();
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);

            // version 1
            writer.Write(m_Lifted);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_Lifted = reader.ReadDateTime();
                        goto case 0;
                    }

                case 0:
                    {
                        break;
                    }
            }
        }
    }
}