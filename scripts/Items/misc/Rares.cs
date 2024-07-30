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

/* Scripts/Items/Misc/Rares.cs
 * ChangeLog
 *  9/13/23, Yoar
 *      Added RawFish, RawFishHeadless, FishHead, FishHeads, CookedFish
 *  7/29/23, Yoar
 *      Added small fish, arrow shafts
 *  7/7/23, Yoar
 *      Added Leviathan rares
 * 5/27/2021, Adam
 *		Add the 'rock' rare.
 *			1. random rock id
 *			2. 1:1000 chance to get a metal-hued rock
 *	1/19/06, Adam
 *		Add the following new rares
 *			ophidian bardiche - 9563 (0x255B), ogre's club - 9561 (0x2559), lizardman's staff - 9560 (0x2558)
 *			ettin hammer - 9557 (0x2555), lizardman's mace - 9559 (0x2557), skeleton scimitar - 9568 (0x2560)
 *			skeleton axe - 9567 (0x255F), ratman sword - 9566 (0x255E), ratman axe - 9565 (0x255D)
 *			orc club - 9564 (0x255C), terathan staff - 9569 (0x2561), terathan spear - 9570 (0x2562)
 *			terathan mace - 9571 (0x2563), troll axe - 9572 (0x2564), troll maul - 9573 (0x2565),
 *			bone mage staff - 9577 (0x2569), orc mage staff - 9576 (0x2568), orc lord battleaxe - 9575 (0x2567)
 *			frost troll club - 9574 (0x2566)
 *	4/29/04, mith
 *		Added BloodVial, copied from DaemonBlood definition in /Scripts/Items/Resources/Reagents/DaemonBlood.cs
 *		Removed ability for BloodVials to be added to a CommodityDeed.
 */

namespace Server.Items
{
    [Flipable(0x14F8, 0x14FA)]
    public class Rope : Item
    {
        [Constructable]
        public Rope()
            : this(1)
        {
        }

        [Constructable]
        public Rope(int amount)
            : base(0x14F8)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Rope(amount), amount);
        }

        public Rope(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class IronWire : Item
    {
        [Constructable]
        public IronWire()
            : this(1)
        {
        }

        [Constructable]
        public IronWire(int amount)
            : base(0x1876)
        {
            Stackable = true;
            Weight = 2.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new IronWire(amount), amount);
        }

        public IronWire(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SilverWire : Item
    {
        [Constructable]
        public SilverWire()
            : this(1)
        {
        }

        [Constructable]
        public SilverWire(int amount)
            : base(0x1877)
        {
            Stackable = true;
            Weight = 2.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new SilverWire(amount), amount);
        }

        public SilverWire(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class GoldWire : Item
    {
        [Constructable]
        public GoldWire()
            : this(1)
        {
        }

        [Constructable]
        public GoldWire(int amount)
            : base(0x1878)
        {
            Stackable = true;
            Weight = 2.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new GoldWire(amount), amount);
        }

        public GoldWire(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class CopperWire : Item
    {
        [Constructable]
        public CopperWire()
            : this(1)
        {
        }

        [Constructable]
        public CopperWire(int amount)
            : base(0x1879)
        {
            Stackable = true;
            Weight = 2.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new CopperWire(amount), amount);
        }

        public CopperWire(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class Whip : Item
    {
        [Constructable]
        public Whip()
            : base(0x166E)
        {
            Weight = 1.0;
        }

        public Whip(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class PaintsAndBrush : Item
    {
        [Constructable]
        public PaintsAndBrush()
            : base(0xFC1)
        {
            Weight = 1.0;
        }

        public PaintsAndBrush(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class PenAndInk : Item
    {
        [Constructable]
        public PenAndInk()
            : base(0xFBF)
        {
            Weight = 1.0;
        }

        public PenAndInk(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class ChiselsNorth : Item
    {
        [Constructable]
        public ChiselsNorth()
            : base(0x1026)
        {
            Weight = 1.0;
        }

        public ChiselsNorth(Serial serial)
            : base(serial)
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

    public class ChiselsWest : Item
    {
        [Constructable]
        public ChiselsWest()
            : base(0x1027)
        {
            Weight = 1.0;
        }

        public ChiselsWest(Serial serial)
            : base(serial)
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

    public class BloodVial : Item
    {
        [Constructable]
        public BloodVial()
            : this(1)
        {
        }

        [Constructable]
        public BloodVial(int amount)
            : base(0xF7D)
        {
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
            Name = "Blood Vial";
        }

        public BloodVial(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new BloodVial(amount), amount);
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

    [Flipable(0xC3B, 0xC3C)]
    public class DriedFlowers1 : Item
    {
        [Constructable]
        public DriedFlowers1()
            : this(1)
        { }

        [Constructable]
        public DriedFlowers1(int amount)
            : base(0xC3B)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new DriedFlowers1(amount), amount);
        }

        public DriedFlowers1(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0xC3D, 0xC3E)]
    public class DriedFlowers2 : Item
    {
        [Constructable]
        public DriedFlowers2()
            : this(1)
        { }

        [Constructable]
        public DriedFlowers2(int amount)
            : base(0xC3D)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new DriedFlowers2(amount), amount);
        }

        public DriedFlowers2(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0xC3C, 0xC3B)]// 7/7/2024, adam: Made flippable, now matches DriedFlowers1 except reversed flip
    public class WhiteDriedFlowers : Item
    {
        [Constructable]
        public WhiteDriedFlowers()
            : this(1)
        {
        }

        [Constructable]
        public WhiteDriedFlowers(int amount)
            : base(0xC3C)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new WhiteDriedFlowers(amount), amount);
        }

        public WhiteDriedFlowers(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0xC3E, 0xC3D)] // 7/7/2024, adam: Made flippable, now matches DriedFlowers2 except reversed flip
    public class GreenDriedFlowers : Item
    {
        [Constructable]
        public GreenDriedFlowers()
            : this(1)
        {
        }

        [Constructable]
        public GreenDriedFlowers(int amount)
            : base(0xC3E)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new GreenDriedFlowers(amount), amount);
        }

        public GreenDriedFlowers(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0xC3F, 0xC40)]
    public class DriedOnions : Item
    {
        [Constructable]
        public DriedOnions()
            : this(1)
        { }

        [Constructable]
        public DriedOnions(int amount)
            : base(0xC3F)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new DriedOnions(amount), amount);
        }

        public DriedOnions(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0xC41, 0xC42)]
    public class DriedHerbs : Item
    {
        [Constructable]
        public DriedHerbs()
            : this(1)
        { }

        [Constructable]
        public DriedHerbs(int amount)
            : base(0xC41)
        {
            Stackable = true;
            Weight = 1.0;
            Amount = amount;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new DriedHerbs(amount), amount);
        }

        public DriedHerbs(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class RedCheckers : Item
    {
        [Constructable]
        public RedCheckers()
            : base(0xE1A)
        {
            Weight = 2.0;
        }

        public RedCheckers(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class WhiteCheckers : Item
    {
        [Constructable]
        public WhiteCheckers()
            : base(0xE1B)
        {
            Weight = 2.0;
        }

        public WhiteCheckers(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class BlackChessmen : Item
    {
        [Constructable]
        public BlackChessmen()
            : base(0xE13)
        {
            Weight = 2.0;
        }

        public BlackChessmen(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class WhiteChessmen : Item
    {
        [Constructable]
        public WhiteChessmen()
            : base(0xE14)
        {
            Weight = 2.0;
        }

        public WhiteChessmen(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class HorseShoes : Item
    {
        [Constructable]
        public HorseShoes()
            : base(0xFB6)
        {
            Weight = 4.0;
        }

        public HorseShoes(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class EmptyJar : Item
    {
        [Constructable]
        public EmptyJar()
            : base(0x1005)
        {
            Weight = 1.0;
            Movable = true;
        }

        public EmptyJar(Serial serial)
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
        }
    }

    [FlipableAttribute(0x0E44, 0x0E45)]
    public class EmptyJars2 : Item
    {
        [Constructable]
        public EmptyJars2()
            : base(0x0E44)
        {
            Weight = 2.0;
            Movable = true;
        }

        public EmptyJars2(Serial serial)
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
        }
    }
    public class EmptyJars3 : Item
    {
        [Constructable]
        public EmptyJars3()
            : base(0x0E46)
        {
            Weight = 3.0;
            Movable = true;
        }

        public EmptyJars3(Serial serial)
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
        }
    }
    public class EmptyJars4 : Item
    {
        [Constructable]
        public EmptyJars4()
            : base(0x0E47)
        {
            Weight = 4.0;
            Movable = true;
        }

        public EmptyJars4(Serial serial)
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
        }
    }
    public class HalfEmptyJar : Item
    {
        [Constructable]
        public HalfEmptyJar()
            : base(0x1007)
        {
            Weight = 1.0;
            Movable = true;
        }

        public HalfEmptyJar(Serial serial)
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
        }
    }
    [FlipableAttribute(0x0E4C, 0x0E4D)]
    public class HalfEmptyJars2 : Item
    {
        [Constructable]
        public HalfEmptyJars2()
            : base(0x0E4C)
        {
            Weight = 2.0;
            Movable = true;
        }

        public HalfEmptyJars2(Serial serial)
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
        }
    }
    public class HalfEmptyJars3 : Item
    {
        [Constructable]
        public HalfEmptyJars3()
            : base(0x0E4E)
        {
            Weight = 3.0;
            Movable = true;
        }

        public HalfEmptyJars3(Serial serial)
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
        }
    }
    public class HalfEmptyJars4 : Item
    {
        [Constructable]
        public HalfEmptyJars4()
            : base(0x0E4f)
        {
            Weight = 4.0;
            Movable = true;
        }

        public HalfEmptyJars4(Serial serial)
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
        }
    }

    public class FullJar : Item
    {
        [Constructable]
        public FullJar()
            : base(0x1006)
        {
            Weight = 1.0;
        }

        public FullJar(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0xE48, 0xE49)]
    public class TwoFullJars : Item
    {
        [Constructable]
        public TwoFullJars()
            : base(0xE48)
        {
            Weight = 2.0;
        }

        public TwoFullJars(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class ThreeFullJars : Item
    {
        [Constructable]
        public ThreeFullJars()
            : base(0xE4A)
        {
            Weight = 3.0;
        }

        public ThreeFullJars(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class FourFullJars : Item
    {
        [Constructable]
        public FourFullJars()
            : base(0xE4B)
        {
            Weight = 4.0;
        }

        public FourFullJars(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0xFB7, 0xFB8)]
    public class ForgedMetal : Item
    {
        [Constructable]
        public ForgedMetal()
            : base(0xFB7)
        {
            Weight = 3.0;
        }

        public ForgedMetal(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0x1EBA, 0x1EBB)]
    public class Toolkit : Item
    {
        [Constructable]
        public Toolkit()
            : base(0x1EBA)
        {
            Weight = 2.0;
        }

        public Toolkit(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class Rocks : Item
    {
        [Constructable]
        public Rocks()
            : base(0x1367)
        {
            Weight = 5.0;
        }

        public Rocks(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class Rock : Item
    {
        [Constructable]
        public Rock()
            : base(0x1367)
        {
            Weight = 2.0;
            // rock ids
            this.ItemID = Utility.RandomList(0x1363, 0x1364, 0x136B, 0x1772, 0x1773, 0x1775, 0x1777);

            // 1:1000 chance to get a hued rock
            if (Utility.RandomDouble() < 0.001)
                this.Hue = Utility.RandomMetalHue();
        }

        public Rock(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // ophidian bardiche - 9563 (0x255B)
    public class OphidianBardiche : Item
    {
        [Constructable]
        public OphidianBardiche()
            : base(0x255B)
        {
            Weight = 7.0;
        }

        public OphidianBardiche(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // ogre's club - 9561 (0x2559)
    public class OgresClub : Item
    {
        [Constructable]
        public OgresClub()
            : base(0x2559)
        {
            Weight = 22.0;
        }

        public OgresClub(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // lizardman's staff - 9560 (0x2558)
    public class LizardmansStaff : Item
    {
        [Constructable]
        public LizardmansStaff()
            : base(0x2558)
        {
            Weight = 6.0;
        }

        public LizardmansStaff(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // ettin hammer - 9557 (0x2555)
    public class EttinHammer : Item
    {
        [Constructable]
        public EttinHammer()
            : base(0x2555)
        {
            Weight = 20.0;
        }

        public EttinHammer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // lizardman's mace - 9559 (0x2557)
    public class LizardmansMace : Item
    {
        [Constructable]
        public LizardmansMace()
            : base(0x2557)
        {
            Weight = 10.0;
        }

        public LizardmansMace(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // skeleton scimitar - 9568 (0x2560)
    public class SkeletonScimitar : Item
    {
        [Constructable]
        public SkeletonScimitar()
            : base(0x2560)
        {
            Weight = 5.0;
        }

        public SkeletonScimitar(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // skeleton axe - 9567 (0x255F)
    public class SkeletonAxe : Item
    {
        [Constructable]
        public SkeletonAxe()
            : base(0x255F)
        {
            Weight = 4.0;
        }

        public SkeletonAxe(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // ratman sword - 9566 (0x255E)
    public class RatmanSword : Item
    {
        [Constructable]
        public RatmanSword()
            : base(0x255E)
        {
            Weight = 6.0;
        }

        public RatmanSword(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // ratman axe - 9565 (0x255D)
    public class RatmanAxe : Item
    {
        [Constructable]
        public RatmanAxe()
            : base(0x255D)
        {
            Weight = 5.0;
        }

        public RatmanAxe(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // orc club - 9564 (0x255C)
    public class OrcClub : Item
    {
        [Constructable]
        public OrcClub()
            : base(0x255C)
        {
            Weight = 9.0;
        }

        public OrcClub(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // terathan staff - 9569 (0x2561)
    public class TerathanStaff : Item
    {
        [Constructable]
        public TerathanStaff()
            : base(0x2561)
        {
            Weight = 7.0;
        }

        public TerathanStaff(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // terathan spear - 9570 (0x2562)
    public class TerathanSpear : Item
    {
        [Constructable]
        public TerathanSpear()
            : base(0x2562)
        {
            Weight = 6.0;
        }

        public TerathanSpear(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // terathan mace - 9571 (0x2563)
    public class TerathanMace : Item
    {
        [Constructable]
        public TerathanMace()
            : base(0x2563)
        {
            Weight = 17.0;
        }

        public TerathanMace(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // troll axe - 9572 (0x2564)
    public class TrollAxe : Item
    {
        [Constructable]
        public TrollAxe()
            : base(0x2564)
        {
            Weight = 8.0;
        }

        public TrollAxe(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // troll maul - 9573 (0x2565)
    public class TrollMaul : Item
    {
        [Constructable]
        public TrollMaul()
            : base(0x2565)
        {
            Weight = 21.0;
        }

        public TrollMaul(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // bone mage staff - 9577 (0x2569)
    public class BoneMageStaff : Item
    {
        [Constructable]
        public BoneMageStaff()
            : base(0x2569)
        {
            Weight = 4.0;
        }

        public BoneMageStaff(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // orc mage staff - 9576 (0x2568)
    public class OrcMageStaff : Item
    {
        [Constructable]
        public OrcMageStaff()
            : base(0x2568)
        {
            Weight = 6.0;
        }

        public OrcMageStaff(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    // orc lord battleaxe - 9575 (0x2567)
    public class OrcLordBattleaxe : Item
    {
        [Constructable]
        public OrcLordBattleaxe()
            : base(0x2567)
        {
            Weight = 12.0;
        }

        public OrcLordBattleaxe(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

    }

    // frost troll club - 9574 (0x2566) 
    public class FrostTrollClub : Item
    {
        [Constructable]
        public FrostTrollClub()
            : base(0x2566)
        {
            Weight = 19.0;
        }

        public FrostTrollClub(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

    }

    public class DirtyPan : Item
    {
        [Constructable]
        public DirtyPan()
            : base(0x9E8)
        {
            Weight = 1.0;
        }

        public DirtyPan(Serial serial)
            : base(serial)
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

    public class DirtySmallRoundPot : Item
    {
        [Constructable]
        public DirtySmallRoundPot()
            : base(0x9E7)
        {
            Weight = 1.0;
        }

        public DirtySmallRoundPot(Serial serial)
            : base(serial)
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

    public class DirtyPot : Item
    {
        [Constructable]
        public DirtyPot()
            : base(0x9E6)
        {
            Weight = 1.0;
        }

        public DirtyPot(Serial serial)
            : base(serial)
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

    public class DirtyRoundPot : Item
    {
        [Constructable]
        public DirtyRoundPot()
            : base(0x9DF)
        {
            Weight = 1.0;
        }

        public DirtyRoundPot(Serial serial)
            : base(serial)
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

    public class DirtyFrypan : Item
    {
        [Constructable]
        public DirtyFrypan()
            : base(0x9DE)
        {
            Weight = 1.0;
        }

        public DirtyFrypan(Serial serial)
            : base(serial)
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

    public class DirtySmallPot : Item
    {
        [Constructable]
        public DirtySmallPot()
            : base(0x9DD)
        {
            Weight = 1.0;
        }

        public DirtySmallPot(Serial serial)
            : base(serial)
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

    public class DirtyKettle : Item
    {
        [Constructable]
        public DirtyKettle()
            : base(0x9DC)
        {
            Weight = 1.0;
        }

        public DirtyKettle(Serial serial)
            : base(serial)
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

    public class CandelabraOfSouls : Item
    {
        public override int LabelNumber { get { return 1063478; } } // Candelabra of Souls

        [Constructable]
        public CandelabraOfSouls() : base(0xB26)
        {
            Light = LightType.Circle300;
        }

        public CandelabraOfSouls(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 1 && Light == LightType.ArchedWindowEast)
                Light = LightType.Circle300;
        }
    }

    public class GhostShipAnchor : Item
    {
        public override int LabelNumber { get { return 1070816; } } // Ghost Ship Anchor

        [Constructable]
        public GhostShipAnchor() : base(0x14F7)
        {
            Hue = 0x47E;
        }

        public GhostShipAnchor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (ItemID == 0x1F47)
                ItemID = 0x14F7;
        }
    }

    public class GoldBricks : Item
    {
        public override int LabelNumber { get { return 1063489; } } // Gold Bricks

        [Constructable]
        public GoldBricks() : base(0x1BEB)
        {
        }

        public GoldBricks(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SeahorseStatuette : MonsterStatuette
    {
        [Constructable]
        public SeahorseStatuette() : base(MonsterStatuetteType.SeaHorse)
        {
            LootType = LootType.Regular;
#if false
            Hue = Utility.RandomList(0, 0x482, 0x489, 0x495, 0x4F2);
#endif
        }

        public SeahorseStatuette(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class ShipModelOfTheHMSCape : Item
    {
        public override int LabelNumber { get { return 1063476; } } // Ship Model of the H.M.S. Cape

        [Constructable]
        public ShipModelOfTheHMSCape() : base(0x14F3)
        {
            Hue = 0x37B;
        }

        public ShipModelOfTheHMSCape(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class AdmiralsHeartyRum : BeverageBottle
    {
        public override int LabelNumber { get { return 1063477; } } // The Admiral's Hearty Rum

        [Constructable]
        public AdmiralsHeartyRum() : base(BeverageType.Ale)
        {
            Hue = 0x66C;
        }

        public AdmiralsHeartyRum(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SmallFish : Item
    {
        [Constructable]
        public SmallFish() : base(Utility.RandomBool() ? 0x0DD6 : 0x0DD7)
        {
        }

        public SmallFish(Serial serial) : base(serial)
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

    [Flipable(0x1024, 0x1025)]
    public class ArrowShafts : Item
    {
        [Constructable]
        public ArrowShafts() : base(0x1025)
        {
        }

        public ArrowShafts(Serial serial) : base(serial)
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

    [Flipable(0x1E15, 0x1E16)]
    public class RawFish : Item
    {
        [Constructable]
        public RawFish() : base(0x1E16)
        {
        }

        public RawFish(Serial serial) : base(serial)
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

    [Flipable(0x1E17, 0x1E18)]
    public class RawFishHeadless : Item
    {
        [Constructable]
        public RawFishHeadless() : base(0x1E18)
        {
        }

        public RawFishHeadless(Serial serial) : base(serial)
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

    [Flipable(0x1E19, 0x1E1A)]
    public class FishHead : Item
    {
        [Constructable]
        public FishHead() : base(0x1E1A)
        {
        }

        public FishHead(Serial serial) : base(serial)
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

    public class FishHeads : Item
    {
        [Constructable]
        public FishHeads() : base(0x1E1B)
        {
        }

        public FishHeads(Serial serial) : base(serial)
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

    [Flipable(0x1E1C, 0x1E1D)]
    public class CookedFish : Item
    {
        [Constructable]
        public CookedFish() : base(0x1E1D)
        {
        }

        public CookedFish(Serial serial) : base(serial)
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

    [Flipable(0x14F7, 0x14F9)]
    public class Anchor : Item
    {
        [Constructable]
        public Anchor()
            : base(0x14F7)
        {
        }

        public Anchor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0x1E9A, 0x1E9B)]
    public class Hook : Item
    {
        [Constructable]
        public Hook()
            : base(0x1E9A)
        {
        }

        public Hook(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0x1E9E, 0x1E9F)]
    public class Pulley : Item
    {
        [Constructable]
        public Pulley()
            : base(0x1E9E)
        {
        }

        public Pulley(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0x1E9C, 0x1E9D)]
    public class Pullies : Item
    {
        [Constructable]
        public Pullies()
            : base(0x1E9C)
        {
        }

        public Pullies(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class TallCandelabra : Item
    {
        [Constructable]
        public TallCandelabra()
            : base(0xB26)
        {
            Light = LightType.Circle300;
        }

        public TallCandelabra(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [Flipable(0xF34, 0xF35)]
    public class Hay : Item
    {
        [Constructable]
        public Hay()
            : base(Utility.RandomBool() ? 0xF34 : 0xF35)
        {
        }

        public Hay(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}