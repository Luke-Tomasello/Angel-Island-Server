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

/* Items/Traps/BaseTrap.cs
 * CHANGELOG:
 *  9/5/2023, Adam
 *      Add LOS for things like mushrooms that would otherwise pop on the other side of a wall
 *      Add PopOnce property so that base traps can be spawned on a spawner. I.e., pop once, then get respawned.
 *	2/21/06, Adam
 *		Make a check to see that something is a BaseCreature before
 *		casting it to a BaseCreature. This is *rarely* needed, but there is 
 *		at least one 'mobile' that runs around ans is not a BaseCreature.
 *		See: Wanderer.cs
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  01/05/05, Darva
 *  		Caused traps to only attack players, tamed mobs, and controlled mobs.
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    public abstract class BaseTrap : Item
    {
        private bool m_PopOnce = false;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool PopOnce { get { return m_PopOnce; } set { m_PopOnce = value; } }
        public virtual bool PassivelyTriggered { get { return false; } }
        public virtual TimeSpan PassiveTriggerDelay { get { return TimeSpan.Zero; } }
        public virtual int PassiveTriggerRange { get { return -1; } }
        public virtual TimeSpan ResetDelay { get { return TimeSpan.Zero; } }

        private DateTime m_NextPassiveTrigger, m_NextActiveTrigger;

        public virtual void OnTrigger(Mobile from)
        {
            if (m_PopOnce)
                Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(1.75, 2.5)), new TimerCallback(Delete));
        }

        public override bool HandlesOnMovement { get { return true; } } // Tell the core that we implement OnMovement

        public virtual int GetEffectHue()
        {
            int hue = this.Hue & 0x3FFF;

            if (hue < 2)
                return 0;

            return hue - 1;
        }

        public bool CheckRange(Point3D loc, Point3D oldLoc, int range)
        {
            return CheckRange(loc, range) && !CheckRange(oldLoc, range);
        }

        public bool CheckRange(Point3D loc, int range)
        {
            return ((this.Z + 8) >= loc.Z && (loc.Z + 16) > this.Z)
                && Utility.InRange(GetWorldLocation(), loc, range);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (m.Location == oldLocation)
                return;

            // leave hidden staff alone.
            //  Also, don't process wild creatures.
            if ((m.AccessLevel > AccessLevel.Player && m.Hidden) || !(m is BaseCreature bc && bc.Controlled))
                return;

            // 9/5/2023, Adam: Add LOS for things like mushrooms that would otherwise pop on the other side of a wall
            if (m.InLOS(this))
            {
                if (CheckRange(m.Location, oldLocation, 0) && DateTime.UtcNow >= m_NextActiveTrigger)
                {
                    m_NextActiveTrigger = m_NextPassiveTrigger = DateTime.UtcNow + ResetDelay;
                    if (m is PlayerMobile || (m is BaseCreature && ((BaseCreature)m).Controlled == true))
                        OnTrigger(m);
                }
                else if (PassivelyTriggered && CheckRange(m.Location, oldLocation, PassiveTriggerRange) && DateTime.UtcNow >= m_NextPassiveTrigger)
                {
                    m_NextPassiveTrigger = DateTime.UtcNow + PassiveTriggerDelay;
                    if (m is PlayerMobile || (m is BaseCreature && ((BaseCreature)m).Controlled == true))
                        OnTrigger(m);
                }
            }
        }

        public BaseTrap(int itemID)
            : base(itemID)
        {
            Movable = false;
        }

        public BaseTrap(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_PopOnce);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_PopOnce = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}