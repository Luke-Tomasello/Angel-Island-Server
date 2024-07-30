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

/* Items/Misc/UnholyBone.cs
 * CHANGELOG:
 *	12/17/05, Adam
 *		Swap out the lich and lich lord (Council) for BoneMagi and BoneMagiLord (Undead)
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    public class UnholyBone : Item, ICarvable
    {
        private SpawnTimer m_Timer;

        [Constructable]
        public UnholyBone()
            : base(0xF7E)
        {
            Movable = false;
            Hue = 0x497;
            Name = "unholy bone";

            m_Timer = new SpawnTimer(this);
            m_Timer.Start();
        }

        public void Carve(Mobile from, Item item)
        {
            Effects.PlaySound(GetWorldLocation(), Map, 0x48F);
            Effects.SendLocationEffect(GetWorldLocation(), Map, 0x3728, 10, 10, 0, 0);

            if (0.3 > Utility.RandomDouble())
            {
                if (ItemID == 0xF7E)
                    from.SendMessage("You destroy the bone.");
                else
                    from.SendMessage("You destroy the bone pile.");

                Gold gold = new Gold(25, 100);

                gold.MoveToWorld(GetWorldLocation(), Map);

                Delete();

                m_Timer.Stop();
            }
            else
            {
                if (ItemID == 0xF7E)
                    from.SendMessage("You damage the bone.");
                else
                    from.SendMessage("You damage the bone pile.");
            }
        }

        public UnholyBone(Serial serial)
            : base(serial)
        {
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

            m_Timer = new SpawnTimer(this);
            m_Timer.Start();
        }

        private class SpawnTimer : Timer
        {
            private Item m_Item;

            public SpawnTimer(Item item)
                : base(TimeSpan.FromSeconds(Utility.RandomMinMax(5, 10)))
            {
                Priority = TimerPriority.FiftyMS;

                m_Item = item;
            }

            protected override void OnTick()
            {
                if (m_Item.Deleted)
                    return;

                Mobile spawn;

                switch (Utility.Random(12))
                {
                    default:
                    case 0: spawn = new Skeleton(); break;
                    case 1: spawn = new Zombie(); break;
                    case 2: spawn = new Wraith(); break;
                    case 3: spawn = new Spectre(); break;
                    case 4: spawn = new Ghoul(); break;
                    case 5: spawn = new Mummy(); break;
                    case 6: spawn = new Bogle(); break;
                    case 7: spawn = new RottingCorpse(); break;
                    case 8: spawn = new BoneKnight(); break;
                    case 9: spawn = new SkeletalKnight(); break;
                    case 10: spawn = new BoneMagi(); break;
                    case 11: spawn = new BoneMagiLord(); break;
                }

                spawn.MoveToWorld(m_Item.Location, m_Item.Map);

                m_Item.Delete();
            }
        }
    }
}