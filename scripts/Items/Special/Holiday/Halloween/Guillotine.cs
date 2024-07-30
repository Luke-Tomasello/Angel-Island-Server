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

/* Scripts/Items/Special/Holiday/Halloween/Guillotine.cs
 * CHANGELOG
 *  11/9/2023, Adam
 *      Fix Z-axis exploit. 
 *      This item could for example be on the balcony of a house, and double clicking it will teleport you to that balcony.
 *  10/26/23, Yoar
 *      Merge from RunUO.
 */

using Server.Network;
using System;

namespace Server.Items
{
    [Flipable(0x125E, 0x1230)]
    public class GuillotineComponent : AddonComponent
    {
#if RunUO
        public override int LabelNumber { get { return 1024656; } } // Guillotine
#endif

        public GuillotineComponent(int itemID) : base(itemID)
        {
        }

        public GuillotineComponent(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }

    public class GuillotineAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new GuillotineDeed(); } }
        public override bool Redeedable { get { return true; } }

        [Constructable]
        public GuillotineAddon() : this(1)
        {
        }

        [Constructable]
        public GuillotineAddon(int type) : base()
        {
            switch (type)
            {
                case 0: AddComponent(new GuillotineComponent(0x125E), 0, 0, 0); break; // east
                case 1: AddComponent(new GuillotineComponent(0x1230), 0, 0, 0); break; // south
            }
        }

        public GuillotineAddon(Serial serial) : base(serial)
        {
        }

        public override void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
            /* 1. InRange. Only checks X/Y
             * 2. Verify Z is also OK
             * 3. If the player is 2 tiles away, make sure that there is no blocking object (barrier) between the player and the target
             */
            bool spawanable = Utility.CanSpawnLandMobile(from.Map, Utility.OffsetPoint(from.Location, from.GetDirectionTo(Location)));
            if (from.InRange(Location, 2) && Math.Abs(from.Z - Location.Z) < 2 && ((int)from.GetDistanceToSqrt(Location) < 2 || spawanable))
            {
                if (Utility.RandomBool())
                {
                    from.Location = Location;

                    Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(Activate), new object[] { c, from });
                }
                else
                    from.LocalOverheadMessage(MessageType.Regular, 0, 501777); // Hmm... you suspect that if you used this again, it might hurt.
            }
            else
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }

        private void Activate(object obj)
        {
            object[] param = (object[])obj;

            if (param[0] is AddonComponent && param[1] is Mobile)
                Activate((AddonComponent)param[0], (Mobile)param[1]);
        }

        public virtual void Activate(AddonComponent c, Mobile from)
        {
            Map map = c.Map;

            if (map == null || map == Map.Internal)
                return;

            if (c.ItemID == 0x125E || c.ItemID == 0x1269 || c.ItemID == 0x1260)
                c.ItemID = 0x1269;
            else
                c.ItemID = 0x1247;

            // blood
            int amount = Utility.RandomMinMax(3, 7);

            for (int i = 0; i < amount; i++)
            {
                int x = c.X + Utility.RandomMinMax(-1, 1);
                int y = c.Y + Utility.RandomMinMax(-1, 1);
                int z = c.Z;

                if (!map.CanFit(x, y, z, 1, false, false, true))
                {
                    z = map.GetAverageZ(x, y);

                    if (!map.CanFit(x, y, z, 1, false, false, true))
                        continue;
                }

                Blood blood = new Blood(Utility.RandomMinMax(0x122C, 0x122F));
                blood.MoveToWorld(new Point3D(x, y, z), map);
            }

            if (from.Female)
                from.PlaySound(Utility.RandomMinMax(0x150, 0x153));
            else
                from.PlaySound(Utility.RandomMinMax(0x15A, 0x15D));

            from.LocalOverheadMessage(MessageType.Regular, 0, 501777); // Hmm... you suspect that if you used this again, it might hurt.
            from.Damage(Utility.Dice(2, 10, 5), from, null);

            Timer.DelayCall(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), 2, new TimerStateCallback(Deactivate), c);
        }

        private void Deactivate(object obj)
        {
            if (obj is AddonComponent)
            {
                AddonComponent c = (AddonComponent)obj;

                if (c.ItemID == 0x1269)
                    c.ItemID = 0x1260;
                else if (c.ItemID == 0x1260)
                    c.ItemID = 0x125E;
                else if (c.ItemID == 0x1247)
                    c.ItemID = 0x1246;
                else if (c.ItemID == 0x1246)
                    c.ItemID = 0x1230;
            }
        }
    }

    public class GuillotineDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new GuillotineAddon(m_Type); } }
#if RunUO
        public override int LabelNumber { get { return 1024656; } } // Guillotine
#else
        public override string DefaultName { get { return "a guillotine deed"; } }
#endif

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Guillotine (East)",
                "Guillotine (South)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public GuillotineDeed() : base()
        {
#if RunUO
            LootType = LootType.Blessed;
#endif
        }

        public GuillotineDeed(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}