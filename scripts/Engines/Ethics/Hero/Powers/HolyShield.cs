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

using Server.Items;
using Server.Spells;
using System;

namespace Server.Ethics.Hero
{
    public sealed class HolyShield : Power
    {
        public HolyShield()
        {
            m_Definition = new PowerDefinition(
                    Core.NewEthics ? 40 : 20,
                    7,
                    "Holy Shield",
                    "Erstok K'blac",
                    "Repels monsters for 1 hour."
                );
        }

        public override void BeginInvoke(Player from)
        {
            if (Core.NewEthics)
            {
                if (from.IsShielded)
                {
                    from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You are already under the protection of a holy shield.");
                    return;
                }

                from.BeginShield();

                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You are now under the protection of a holy shield.");
            }
            else
            {
                Point3D point = from.Mobile.Location;
                Map map = from.Mobile.Map;
                int itemID = 0x1bc3;
                TimeSpan duration = TimeSpan.FromMinutes(2);

                // are we at the maps edge?
                if (point.X < 2 || point.X > map.Width - 2 || point.Y < 2 || point.Y > map.Height - 2)
                {   // fizzle
                    from.Mobile.FixedEffect(0x3735, 6, 30);
                    from.Mobile.PlaySound(0x5C);
                    return;
                }

                bool blockPlaced = false;

                // the bounding box
                for (int x = point.X - 2; x <= point.X + 2; x++)
                {
                    for (int y = point.Y - 2; y <= point.Y + 2; y++)
                    {
                        Point3D loc = new Point3D(x, y, map.GetAverageZ(x, y));
                        Item item = null;
                        if (SpellHelper.AdjustField(ref loc, map, 12, false))
                        {   // can fit?
                            if (x == point.X - 2 || x == point.X + 2 || y == point.Y - 2 || y == point.Y + 2)
                            {
                                blockPlaced = true;

                                item = new BlockItem(loc, from.Mobile.Map, duration, itemID, from.Mobile);

                                // show the border
                                loc = item.GetWorldLocation();
                                Effects.SendLocationParticles(EffectItem.Create(new Point3D(loc.X + 1, loc.Y, loc.Z), item.Map, EffectItem.DefaultDuration), 0x376A, 9, 10, 9502);
                                Effects.SendLocationParticles(EffectItem.Create(new Point3D(loc.X, loc.Y - 1, loc.Z), item.Map, EffectItem.DefaultDuration), 0x376A, 9, 10, 9502);
                                Effects.SendLocationParticles(EffectItem.Create(new Point3D(loc.X - 1, loc.Y, loc.Z), item.Map, EffectItem.DefaultDuration), 0x376A, 9, 10, 9502);
                                Effects.SendLocationParticles(EffectItem.Create(new Point3D(loc.X, loc.Y + 1, loc.Z), item.Map, EffectItem.DefaultDuration), 0x376A, 9, 10, 9502);
                                Effects.SendLocationParticles(EffectItem.Create(new Point3D(loc.X, loc.Y, loc.Z), item.Map, EffectItem.DefaultDuration), 0, 0, 0, 5014);
                            }
                            else
                                item = new PassItem(loc, from.Mobile.Map, duration, itemID, from.Mobile);
                        }
                    }
                }

                if (blockPlaced)
                {
                    // give each mobile within the shiled a free pass flag
                    IPooledEnumerable eable = from.Mobile.GetMobilesInRange(2);
                    foreach (Mobile m in eable)
                    {   // we allow the shielding of enemies here since otherwise it could be exploited to box
                        // your enemy
                        m.ExpirationFlags.Add(new Mobile.ExpirationFlag(m, Mobile.ExpirationFlagID.ShieldIgnore, duration));
                        m.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, 502193); // An invisible shield forms around you.
                    }
                    eable.Free();

                    Effects.PlaySound(point, map, 0x1F6);
                }
                else
                {
                    from.Mobile.FixedEffect(0x3735, 6, 30);
                    from.Mobile.PlaySound(0x5C);
                }
            }

            FinishInvoke(from);
        }

        private class BlockItem : Item
        {
            private Timer m_Timer;
            protected Timer Timer { get { return m_Timer; } }
            private static Memory MessageGiven = new Memory();

            public override bool BlocksFit { get { return true; } }

            public BlockItem(Point3D loc, Map map, TimeSpan duration, int itemID, Mobile caster)
                : base(itemID)
            {
                Visible = false;
                Movable = false;
                MoveToWorld(loc, map);
                m_Timer = new InternalTimer(this, duration);
                m_Timer.Start();
            }

            public BlockItem(Serial serial)
                : base(serial)
            {   // delete now on restart
                m_Timer = new InternalTimer(this, TimeSpan.FromSeconds(5.0));
                m_Timer.Start();
            }

            public override bool OnMoveOver(Mobile m)
            {
                // if they have the ShieldIgnore flag, they may pass
                if (m.CheckState(Mobile.ExpirationFlagID.ShieldIgnore))
                    return true;

                // tell them about the invisible shield but don't spam
                if (MessageGiven.Recall(m) == false)
                {
                    MessageGiven.Remember(m, 5);
                    m.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, 502194); // An invisible shield seems to block your passage!
                }

                return false;
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }

            public override void OnAfterDelete()
            {
                base.OnAfterDelete();

                if (m_Timer != null)
                    m_Timer.Stop();
            }

            private class InternalTimer : Timer
            {
                private BlockItem m_Item;

                public InternalTimer(BlockItem item, TimeSpan duration)
                    : base(duration)
                {
                    Priority = TimerPriority.OneSecond;
                    m_Item = item;
                }

                protected override void OnTick()
                {
                    m_Item.Delete();
                }
            }
        }

        private class PassItem : BlockItem
        {

            public PassItem(Point3D loc, Map map, TimeSpan duration, int itemID, Mobile caster)
                : base(loc, map, duration, itemID, caster)
            {
            }

            public PassItem(Serial serial)
                : base(serial)
            {
            }

            public override bool OnMoveOver(Mobile m)
            {
                // exploit prevention:
                // Placing one of these shield areas at a house ban location, or at the exit of a dungeon, or basically at any
                //	teleport destination would trap the player. We have these PASS tiles to fit them with a special flag that allows them to 
                //	pass through the shield. You get one of these flags by <somehow> getting inside the shield, for instance teleport.
                TimeSpan remaining = TimeSpan.FromSeconds(1);
                if (DateTime.UtcNow < Timer.NextTick)
                    remaining = Timer.NextTick - DateTime.UtcNow;

                m.ExpirationFlags.Add(new Mobile.ExpirationFlag(m, Mobile.ExpirationFlagID.ShieldIgnore, remaining));
                return true;
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }
    }
}