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

/* Items/Wands/Wand.cs
 * CHANGE LOG
 *  3/27/23, Adam
 *      Add a Wand class so that it works like all other enchanted items (clothing, jewelry, weapons, etc.)
 *      You create the Wand, then SetRandomMagicEffect() on it.
 */

/* ChatGPT: what monster dropped fireball wand ultima online
 * In Ultima Online, the Fireball Wand can be dropped by several different monsters, including:
 * Liches: These undead creatures can be found in various locations throughout the game and are known to drop powerful magical items.
 * Dread Spiders: These arachnids can be found in the Twisted Weald dungeon and are known for their ability to poison their enemies.
 * Daemon: This demon can be found in the Fire Dungeon and is known for its powerful fire-based attacks.
 * Phoenix: This legendary bird can be found in the Prism of Light dungeon and is known for its ability to create and control fire.
 * Fire Elementals: These elemental creatures can be found in various locations throughout the game and are known for their powerful fire-based attacks.
 * 
 * It's worth noting that the Fireball Wand is a rare and powerful item, so it may take some time and effort to obtain it from one of these monsters.
 */

namespace Server.Items
{
    public class Wand : BaseWand
    {
        [Constructable]
        public Wand()
            : base(MagicItemEffect.None, 0, 0)
        {
            MagicEffect = MagicItemEffect.None;
            MagicCharges = 0;
        }

        public Wand(Serial serial)
            : base(serial)
        {
        }
        public new void SetRandomMagicEffect(int MinLevel, int MaxLevel)
        {
            if (MinLevel < 1 || MaxLevel > 3)
                return;

            MagicEffect = Loot.WandEnchantments[Utility.Random(Loot.WandEnchantments.Length)];

            int NewLevel = Utility.RandomMinMax(MinLevel, MaxLevel);
            switch (NewLevel)
            {
                case 1:
                    MagicCharges = Utility.Random(1, 5);
                    break;
                case 2:
                    MagicCharges = Utility.Random(4, 11);
                    break;
                case 3:
                    MagicCharges = Utility.Random(9, 20);
                    break;
                default:
                    // should never happen
                    MagicCharges = 0;
                    break;
            }
            Identified = false;
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