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

/* Scripts\Engines\ChampionSpawn\Champs\Special\PumpkinKing\Items\PumpkinPatchVine.cs
 * CHANGELOG
 *  10/28/2026, Adam
 *      Initial version: The Pumpkin King's pumpkin patch (Halloween 2026, grid spawned). A pumpkin vine that grows a real,
 *      movable pumpkin on its tile. The pumpkin carries the spawner and OnSpawner (so it does not decay while it sits
 *      there; lifting clears OnSpawner as usual). Taking the pumpkin withers the vine: it is removed after 5 minutes and
 *      the spawner grows a new one. The vine itself cannot be picked.
 */

using System;

namespace Server.Items
{
    public class PumpkinPatchVine : FarmablePumpkin
    {
        private static readonly TimeSpan WitherDelay = TimeSpan.FromMinutes(5.0);

        private PumpkinPatchPumpkin? m_Pumpkin;

        [Constructable]
        public PumpkinPatchVine()
            : base()
        {
            // once placed (and given our spawner), grow the pumpkin
            Timer.DelayCall(TimeSpan.Zero, GrowPumpkin);
        }

        private void GrowPumpkin()
        {
            if (Deleted || Map == null || Map == Map.Internal || m_Pumpkin != null)
                return;

            PumpkinPatchPumpkin pumpkin = new PumpkinPatchPumpkin(this);
            pumpkin.ItemID = Utility.Random(0xC6A, 3);
            pumpkin.Spawner = Spawner;
            pumpkin.SetItemBool(ItemBoolTable.OnSpawner, true);
            pumpkin.MoveToWorld(Location, Map);
            m_Pumpkin = pumpkin;
        }

        // players pick up the pumpkin; the vine itself is not picked
        public override void OnDoubleClick(Mobile from)
        {
        }

        // our pumpkin was lifted (or deleted): wither, and let the spawner grow a new vine
        public void OnPumpkinTaken()
        {
            if (Deleted || m_Pumpkin == null)
                return;

            m_Pumpkin = null;
            ItemID = GetPickedID();
            Timer.DelayCall(WitherDelay, Delete);
        }

        public override void OnDelete()
        {
            // a vine removed with its pumpkin still on it (spawner cleanup, staff) takes the pumpkin along
            PumpkinPatchPumpkin? pumpkin = m_Pumpkin;
            m_Pumpkin = null;
            if (pumpkin != null && !pumpkin.Deleted && pumpkin.Parent == null && pumpkin.Location == Location)
                pumpkin.Delete();

            base.OnDelete();
        }

        public PumpkinPatchVine(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Pumpkin);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            m_Pumpkin = reader.ReadItem() as PumpkinPatchPumpkin;

            // a vine whose pumpkin is gone (or withering when we went down) is removed; the spawner grows a new one
            Timer.DelayCall(TimeSpan.Zero, () =>
            {
                if (!Deleted && (m_Pumpkin == null || m_Pumpkin.Deleted || m_Pumpkin.Parent != null || m_Pumpkin.Location != Location))
                {
                    m_Pumpkin = null;
                    Delete();
                }
            });
        }
    }

    // a real pumpkin grown by a PumpkinPatchVine; lifting it withers the vine
    public class PumpkinPatchPumpkin : Pumpkin
    {
        private PumpkinPatchVine? m_Vine;

        public PumpkinPatchPumpkin(PumpkinPatchVine vine)
            : base()
        {
            m_Vine = vine;
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            base.OnItemLifted(from, item);     // clears OnSpawner: from here on an ordinary pumpkin
            Spawner = null;
            ReleaseVine();
        }

        public override void OnDelete()
        {
            ReleaseVine();
            base.OnDelete();
        }

        private void ReleaseVine()
        {
            PumpkinPatchVine? vine = m_Vine;
            m_Vine = null;
            if (vine != null && !vine.Deleted)
                vine.OnPumpkinTaken();
        }

        public PumpkinPatchPumpkin(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Vine);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            m_Vine = reader.ReadItem() as PumpkinPatchVine;
        }
    }
}
