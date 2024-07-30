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

/* Scripts\Engines\AngelIsland\Items\RareTreasureChest.cs
 * ChangeLog:
 *  6/26/2023, Adam
 *      Created
 */

using static Server.Loot;

namespace Server.Items
{
    [FlipableAttribute(0xE41, 0xE40)]
    public class RareTreasureChest : DungeonTreasureChest
    {
        private bool m_rare_dropped = false;
        [Constructable]
        public RareTreasureChest()
            : base(null, 0)
        {
            Locked = false;
            TrapLevel = 5;
            TrapType = Utility.RandomBool() ? TrapType.ExplosionTrap : TrapType.PoisonTrap;
            Fill(this, 0);
        }
        public new static void Fill(LockableContainer cont, int level)
        {
            // now give level 4 (Siege) loot
            // reagents, scrolls(level 1 to 7), blank scrolls, gems, magic wands, magic armor, weapons, clothing and jewelry, crystal balls.
            DungeonTreasureChest.Fill(cont, 4);

            // keep the gold, can be used to bribe the parole officer.

            // make rare so they can exit prison with them
            // make sure nothing is converted to a scroll
            foreach (Item item in cont.Items)
                if (item is not Gold)
                {
                    item.LootType = LootType.Smuggled;
                    item.SetItemBool(ItemBoolTable.NoScroll, true);
                }
        }
        public override bool ExecuteTrap(Mobile from, bool bAutoReset)
        {
            // this will be LootType.Rare;
            if (m_rare_dropped == false)
            {
                m_rare_dropped = true;                  // one rare per chest
                UpdateRareChance(from);                 // chance per player per hour
                double chance = GetRareChance(from);
                DropItem(Loot.RareFactoryItem(chance, RareType.UnusualChestDrop));
            }

            //Uncomment if you want a autoreset
            //Timer.DelayCall(TimeSpan.FromSeconds(2.5), new TimerStateCallback(AutoReset), new object[] { null });
            bool bReturn = base.ExecuteTrap(from, bAutoReset: false);
            return bReturn;
        }
        private void AutoReset(object state)
        {
            TrapEnabled = true;
            TrapType = Utility.RandomBool() ? TrapType.ExplosionTrap : TrapType.PoisonTrap;
        }
        private static Memory OpenMemory = new Memory();
        private void UpdateRareChance(Mobile from)
        {
            Memory.ObjectMemory om = OpenMemory.Recall((object)from);
            if (om != null)
            {   // we remember this guy
                int temp = (int)om.Context;
                om.Context = temp + 1;      // number of times this guy has opened a chest this hour (different chests)
            }
            else
            {
                OpenMemory.Remember(from, 3600);    // one hour
                om = OpenMemory.Recall((object)from);
                om.Context = 1;
            }
        }
        private double GetRareChance(Mobile from)
        {
            Memory.ObjectMemory om = OpenMemory.Recall((object)from);
            if (om != null && om.Context != null)
            {   // we remember this guy
                double chest_opens = (double)((int)om.Context);
                if (chest_opens > 10)
                    return 0.0;
                // every open decreases your rare chance by 10%
                double base_chance = 1.0;
                double chance = base_chance - (chest_opens * .1);
                ;
                return chance;

            }
            return 1.0;
        }
        public RareTreasureChest(Serial serial)
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

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
}