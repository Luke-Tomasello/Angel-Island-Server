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

/* Items/Traps/FlameSpurtTrap.cs
 * CHANGELOG:
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  01/05/05, Darva
 *		Removed checks to see if the target is a player.
 *		Changed Sound played when damaging target.
 */

using Server.Mobiles;
using System;
namespace Server.Items
{
    public class FlameSpurtTrap : BaseTrap
    {
        private Item m_Spurt;
        private Timer m_Timer;

        [Constructable]
        public FlameSpurtTrap()
            : base(0x1B71)
        {
            Visible = false;
        }

        public virtual void StartTimer()
        {
            if (m_Timer == null)
                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), new TimerCallback(Refresh));
        }

        public virtual void StopTimer()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;
        }

        public virtual void CheckTimer()
        {
            Map map = this.Map;

            if (map != null && map.GetSector(GetWorldLocation()).Active)
                StartTimer();
            else
                StopTimer();
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            CheckTimer();
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            CheckTimer();
        }

        public override void OnSectorActivate()
        {
            base.OnSectorActivate();

            StartTimer();
        }

        public override void OnSectorDeactivate()
        {
            base.OnSectorDeactivate();

            StopTimer();
        }

        public override void OnDelete()
        {
            base.OnDelete();

            if (m_Spurt != null)
                m_Spurt.Delete();
        }

        public virtual void Refresh()
        {
            if (Deleted)
                return;

            bool foundPlayer = false;

            IPooledEnumerable eable = GetMobilesInRange(3);
            foreach (Mobile mob in eable)
            {
                if (!mob.Alive)
                    continue;
                if (mob is BaseCreature && !((BaseCreature)mob).Controlled)
                    continue;
                if (((this.Z + 8) >= mob.Z && (mob.Z + 16) > this.Z))
                {
                    foundPlayer = true;
                    break;
                }
            }
            eable.Free();

            if (!foundPlayer)
            {
                if (m_Spurt != null)
                    m_Spurt.Delete();

                m_Spurt = null;
            }
            else if (m_Spurt == null || m_Spurt.Deleted)
            {
                m_Spurt = new Static(0x3709);
                m_Spurt.MoveToWorld(this.Location, this.Map);

                Effects.PlaySound(GetWorldLocation(), Map, 0x309);
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.Alive)
            {
                CheckTimer();

                Spells.SpellHelper.Damage(TimeSpan.FromTicks(1), m, m, Utility.RandomMinMax(1, 30));
                m.PlaySound(m.GetHurtSound());
            }

            return false;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (m.Location == oldLocation || !m.Alive)
                return;

            if (CheckRange(m.Location, oldLocation, 1))
            {
                CheckTimer();

                Spells.SpellHelper.Damage(TimeSpan.FromTicks(1), m, m, Utility.RandomMinMax(1, 10));
                m.PlaySound(m.GetHurtSound());

                if (m.Body.IsHuman)
                    m.Animate(20, 1, 1, true, false, 0);
            }
        }

        public FlameSpurtTrap(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((Item)m_Spurt);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Item item = reader.ReadItem();

                        if (item != null)
                            item.Delete();

                        CheckTimer();

                        break;
                    }
            }
        }
    }
}