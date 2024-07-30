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

/* Scripts\Items\misc\Corpses\DeadGuys.cs
 * CHANGELOG
 *  8/4/21, Adam
 *      Add MurderedMiner
 *      MurderedMiner's differ from dead miners in that they assume the name of the slain
 *          Don't set a direction
 *  07/02/06, Kit
 *		Overrid InitOutFit/Body, new base mobile functions
 *  09/06/05 Taran Kain
 *		Set StaticCorpse property in OnDeath to prevent looting.
 */

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class DeadInmate : BaseCreature
    {
        [Constructable]
        public DeadInmate()
            : base(AIType.AI_Use_Default, FightMode.None, 0, 0, 0.0, 0.0)
        {
            InitBody();
            InitOutfit();

            Timer.DelayCall(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.0), new TimerCallback(Kill));
        }

        public DeadInmate(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
                Title = "Inmate";
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
                Title = "Inmate";
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            //AddItem(new Shirt(Utility.RandomNeutralHue()));
            //AddItem((Utility.RandomBool() ? (Item)(new LongPants(Utility.RandomNeutralHue())) : (Item)(new ShortPants(Utility.RandomNeutralHue()))));
            //AddItem(new Boots(Utility.RandomNeutralHue()));
            //AddItem(new HalfApron(Utility.RandomNeutralHue()));

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new PonyTail(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new LongHair(Utility.RandomHairHue())); break;
            }

            //int hue = Utility.RandomOrangeHue();
            //Console.WriteLine("robe hue {0}", hue);
            Robe robe = new Robe(0x2b); // don't really like this orange.. need a better one
            AddItem(robe);
            AddItem(Server.Loot.RandomWeapon());
        }

        public override void OnDeath(Server.Items.Container c)
        {
            base.OnDeath(c);

            TimeSpan decayTime = TimeSpan.FromHours(24.0);
            if (base.Spawner is Spawner spawner)
                decayTime = Utility.RandomMinMax(spawner.MinDelay, spawner.MinDelay);

            Corpse corpse = c as Corpse;
            corpse.BeginDecay(decayTime);
            corpse.StaticCorpse = true;
            for (int i = 0; i < 3; i++)
            {
                Point3D p = new Point3D(Location);
                p.X += Utility.RandomMinMax(-1, 1);
                p.Y += Utility.RandomMinMax(-1, 1);
                new Blood(Utility.Random(0x122A, 5), decayTime.TotalSeconds).MoveToWorld(p, c.Map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
    public class DeadExecutioner : BaseCreature
    {
        [Constructable]
        public DeadExecutioner()
            : base(AIType.AI_Use_Default, FightMode.None, 0, 0, 0.0, 0.0)
        {
            InitBody();
            InitOutfit();

            Timer.DelayCall(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.0), new TimerCallback(Kill));
        }

        public DeadExecutioner(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }

            Title = "the executioner";
        }
        public override bool AlwaysMurderer { get { return true; } }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            if (Female)
                AddItem(new Skirt(Utility.RandomNeutralHue()));
            else
                AddItem(new ShortPants(Utility.RandomNeutralHue()));

            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            AddItem(new ThighBoots(Utility.RandomRedHue()));
            AddItem(new Surcoat(Utility.RandomRedHue()));
            AddItem(new ExecutionersAxe());
        }

        public override void OnDeath(Server.Items.Container c)
        {
            base.OnDeath(c);

            TimeSpan decayTime = TimeSpan.FromHours(24.0);
            if (base.Spawner is Spawner spawner)
                decayTime = Utility.RandomMinMax(spawner.MinDelay, spawner.MinDelay);

            Corpse corpse = c as Corpse;
            corpse.BeginDecay(decayTime);
            corpse.StaticCorpse = true;
            for (int i = 0; i < 3; i++)
            {
                Point3D p = new Point3D(Location);
                p.X += Utility.RandomMinMax(-1, 1);
                p.Y += Utility.RandomMinMax(-1, 1);
                new Blood(Utility.Random(0x122A, 5), decayTime.TotalSeconds).MoveToWorld(p, c.Map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
    public class DeadBrigandGuard : BaseCreature
    {
        [Constructable]
        public DeadBrigandGuard()
            : base(AIType.AI_Use_Default, FightMode.None, 0, 0, 0.0, 0.0)
        {
            InitBody();
            InitOutfit();

            Timer.DelayCall(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.0), new TimerCallback(Kill));
        }

        public DeadBrigandGuard(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }

            Title = "the brigand guard";
        }
        public override bool AlwaysMurderer { get { return true; } }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            if (Female)
                AddItem(new Skirt(Utility.RandomNeutralHue()));
            else
                AddItem(new ShortPants(Utility.RandomNeutralHue()));

            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new FancyShirt());
            AddItem(new Bandana());

            switch (Utility.Random(7))
            {
                case 0: AddItem(new Longsword()); break;
                case 1: AddItem(new Cutlass()); break;
                case 2: AddItem(new Broadsword()); break;
                case 3: AddItem(new Axe()); break;
                case 4: AddItem(new Club()); break;
                case 5: AddItem(new Dagger()); break;
                case 6: AddItem(new Spear()); break;
            }
        }

        public override void OnDeath(Server.Items.Container c)
        {
            base.OnDeath(c);

            TimeSpan decayTime = TimeSpan.FromHours(24.0);
            if (base.Spawner is Spawner spawner)
                decayTime = Utility.RandomMinMax(spawner.MinDelay, spawner.MinDelay);

            Corpse corpse = c as Corpse;
            corpse.BeginDecay(decayTime);
            corpse.StaticCorpse = true;
            for (int i = 0; i < 3; i++)
            {
                Point3D p = new Point3D(Location);
                p.X += Utility.RandomMinMax(-1, 1);
                p.Y += Utility.RandomMinMax(-1, 1);
                new Blood(Utility.Random(0x122A, 5), decayTime.TotalSeconds).MoveToWorld(p, c.Map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
    public class MurderedMiner : BaseCreature
    {
        [Constructable]
        public MurderedMiner()
            : base(AIType.AI_Use_Default, FightMode.None, 0, 0, 0.0, 0.0)
        {
            InitBody();
            InitOutfit();

            Timer.DelayCall(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.0), new TimerCallback(Kill));
        }

        public MurderedMiner(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            AddItem(new Shirt(Utility.RandomNeutralHue()));
            AddItem((Utility.RandomBool() ? (Item)(new LongPants(Utility.RandomNeutralHue())) : (Item)(new ShortPants(Utility.RandomNeutralHue()))));
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new HalfApron(Utility.RandomNeutralHue()));

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new PonyTail(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new LongHair(Utility.RandomHairHue())); break;
            }

            AddItem(new Pickaxe());
        }

        public override void OnDeath(Server.Items.Container c)
        {
            base.OnDeath(c);

            TimeSpan decayTime = TimeSpan.FromHours(24.0);
            if (base.Spawner is Spawner spawner)
                decayTime = Utility.RandomMinMax(spawner.MinDelay, spawner.MinDelay);

            Corpse corpse = c as Corpse;
            corpse.BeginDecay(decayTime);
            corpse.StaticCorpse = true;
            for (int i = 0; i < 3; i++)
            {
                Point3D p = new Point3D(Location);
                p.X += Utility.RandomMinMax(-1, 1);
                p.Y += Utility.RandomMinMax(-1, 1);
                new Blood(Utility.Random(0x122A, 5), decayTime.TotalSeconds).MoveToWorld(p, c.Map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
    [CorpseName("a dead miner")]
    public class DeadMiner : BaseCreature
    {
        [Constructable]
        public DeadMiner()
            : base(AIType.AI_Use_Default, FightMode.None, 0, 0, 0.0, 0.0)
        {
            InitBody();
            InitOutfit();

            this.Direction = (Direction)Utility.Random(8);

            Timer.DelayCall(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.0), new TimerCallback(Kill));
        }

        public DeadMiner(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            AddItem(new Shirt(Utility.RandomNeutralHue()));
            AddItem((Utility.RandomBool() ? (Item)(new LongPants(Utility.RandomNeutralHue())) : (Item)(new ShortPants(Utility.RandomNeutralHue()))));
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new HalfApron(Utility.RandomNeutralHue()));

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new PonyTail(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new LongHair(Utility.RandomHairHue())); break;
            }

            AddItem(new Pickaxe());
        }

        public override void OnDeath(Server.Items.Container c)
        {
            base.OnDeath(c);

            TimeSpan decayTime = TimeSpan.FromHours(24.0);
            if (base.Spawner is Spawner spawner)
                decayTime = Utility.RandomMinMax(spawner.MinDelay, spawner.MinDelay);

            Corpse corpse = c as Corpse;
            corpse.BeginDecay(decayTime);
            corpse.StaticCorpse = true;
            for (int i = 0; i < 3; i++)
            {
                Point3D p = new Point3D(Location);
                p.X += Utility.RandomMinMax(-1, 1);
                p.Y += Utility.RandomMinMax(-1, 1);
                new Blood(Utility.Random(0x122A, 5), decayTime.TotalSeconds).MoveToWorld(p, c.Map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
    [CorpseName("a dead guard")]
    public class DeadGuard : BaseGuard
    {
        [Constructable]
        public DeadGuard()
            : base(null)
        {
            this.Direction = (Direction)Utility.Random(8);

            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is Halberd)
                    ((Item)Items[i]).Movable = true;
            }

            //AddItem(new Halberd());

            Timer.DelayCall(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.0), new TimerCallback(Kill));
        }

        public DeadGuard(Serial serial)
            : base(serial)
        {
        }

        public override Mobile Focus
        {
            get { return null; }
            set {; }
        }

        public override bool OnBeforeDeath()
        {
            return true;
        }

        public override void OnDeath(Server.Items.Container c)
        {
            base.OnDeath(c);

            Corpse corpse = c as Corpse;
            corpse.BeginDecay(TimeSpan.FromHours(24.0));
            corpse.StaticCorpse = true;
            for (int i = 0; i < 3; i++)
            {
                Point3D p = new Point3D(Location);
                p.X += Utility.RandomMinMax(-1, 1);
                p.Y += Utility.RandomMinMax(-1, 1);
                new Blood(Utility.Random(0x122A, 5), 86400.0).MoveToWorld(p, c.Map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
}