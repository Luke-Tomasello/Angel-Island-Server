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

/* Scripts\Engines\Spawner\ChestLootPackSpawner.cs
 * CHANGELOG
 *  7/15/2024, Adam
 *      Complete rewrite of ChestItemspawner:
 *      (ChestItemspawner)
 *      1. Obsolete ChestLootPackSpawner. ChestItemspawner now supports loot packs
 *      2. Allow the specification of up to 6 target containers
 *      3. Select: Allow a random 'select'ion of n of those 6 target containers
 *      4. RandomItem:
 *          a. When RandomItem is true, Count represents the total number of items to spawn into each container (selected randomly.)
 *          b. When RandomItem is false, Count represents the number of items of a particular type to spawn into each container
 *      5. Loot Pack support: Loot packs are not special, they are simply added to the list of available items to spawn. Equal weighted with the 'list items'
 *          a. The function of loot packs is to allow GMs to construct custom items for distribution from the spawner
 *      6. Rare items: You spawn rare items with a combination of Min/Max delay + ChanceToSpawn
 *	3/22/10, adam
 *		1) Restore the ability to spawn one random item from list. Keep the defalut that all items are spawned (needed for AI prison chests)
 *		2) Add the ability for spawn from a LootPack object
 *		3) add new ChestLootPackSpawner for spawning raw items usually constructed by staff (Differs from normal spawners that spawn TYPE items.)
 */

using System;
using System.Collections;

namespace Server.Items
{
    [Obsolete("Chest LootPack Spawner is Obsolete. Please use Chest Item Spawner instead.")]
    public class ChestLootPackSpawner : ChestItemSpawner
    {
        public override Item Dupe(int amount)
        {
            ChestLootPackSpawner copy = new ChestLootPackSpawner();

            Utility.CopyProperties(this, copy);

            copy.MyItemNames = MyItemNames != null
                ? new ArrayList(MyItemNames)
                : new ArrayList();

            copy.MyItems = new ArrayList();

            return base.Dupe(copy, amount);
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public override bool RandomItem
        {
            get
            {
                return base.RandomItem;
            }
        }

        public override string DefaultName { get { return "Chest LootPack Spawner"; } }
        [Constructable]
        public ChestLootPackSpawner()
            : base()
        {
            // lootpack spawners don't understand how to spawn the spawn-list understood by normal spawners
            base.RandomItem = true;

            Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(ObsoleteTick), new object[] { });
        }
        private void ObsoleteTick(object state)
        {
            SendMessageNearby("Chest LootPack Spawner is Obsolete. Please use Chest Item Spawner instead.");
        }
        public ChestLootPackSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version 

            //writer.Write(m_LootPack); - eliminated in version 2
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {   // removed local lootpack, uses base class now
                        break;
                    }
                case 1:
                    {
                        LootPack = reader.ReadItem() as Container;
                        goto case 0;
                    }

                case 0:
                    {

                    }
                    break;
            }

            if (version < 2)
            {
                if (Name == "ChestItemSpawner")
                    Name = null;
            }
        }
    }
}