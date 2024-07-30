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

/* Scripts\Multis\Camps\Wrong\PrisonerCamps.cs
 * ChangeLog
 *	6/4/2023, Adam 
 *	    First time checkin
 *	    All different flavors of prisoners.
 *	    These 'camps' typically spawn a prisoner, a guard, and a dungeon chest
 *	    The key to the 'gate' of the cell is stashed on the guard.
 *	    If there is no gate, there is no key.
 */

using Server.Mobiles;

namespace Server.Multis
{
    public class WrongPrisonerNorth : BasePrisonerCamp
    {
        [Constructable]
        public WrongPrisonerNorth()
            : base()
        {
        }

        public override void AddComponents()
        {
            base.AddChest();
            AddGuard();
            base.AddMobiles();
        }
        public override void AddGuard()
        {
            // we want these guys outside the jail cell
            int x = 0;
            int y = 9;
            Mobile m = null;
            if (Utility.Chance(0.10))
                m = new BrigandLeader();
            else
                m = new Brigand();

            AddMobile(m, WanderRange, x, y, 0);

            ManageLock(m);
        }
        public WrongPrisonerNorth(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class WrongPrisonerLizardman : BasePrisonerCamp
    {
        [Constructable]
        public WrongPrisonerLizardman()
            : base()
        {
        }

        public override void AddComponents()
        {
            base.AddChest();
            AddGuard();
            AddMobiles();
        }
        public override void AddGuard()
        {
            // no guard for these guys, they're all dead!
        }
        public override void AddMobiles()
        {
            Prisoner = new Lizardman();
            AddMobile(Prisoner, 2, -2, 0, 0);
        }
        public override void OnEnter(Mobile m)
        {
        }
        public WrongPrisonerLizardman(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class WrongPrisonerWest : BasePrisonerCamp
    {
        [Constructable]
        public WrongPrisonerWest()
            : base()
        {
        }

        public override void AddComponents()
        {
            base.AddChest();
            AddGuard();
            AddMobiles();
        }
        public override void AddGuard()
        {
            // we want these guys outside the jail cell
            int x = 9;
            int y = 0;
            Mobile m = null;
            if (Utility.Chance(0.10))
                m = new EvilMageLord();
            else
                m = new EvilMage();

            AddMobile(m, WanderRange, x, y, 0);

            ManageLock(m);
        }
        public override void AddMobiles()
        {
            switch (Utility.Random(2))
            {
                case 0: Prisoner = new GolemController(); break;
                case 1: Prisoner = new GolemController(); break;
            }

            Prisoner.YellHue = Utility.RandomList(0x57, 0x67, 0x77, 0x87, 0x117);

            AddMobile(Prisoner, 2, -2, 0, 0);
        }
        public override void OnEnter(Mobile m)
        {
            if (m.Player && Prisoner != null)
            {
                string text = string.Empty;

                switch (Utility.Random(4))
                {
                    default:
                    case 0: text = "You think you can keep me in here?!"; break;
                    case 1: text = "I'll be out soon, you just wait and see."; break;
                    case 2: text = "I have friends you know. Big friends."; break;
                    case 3: text = "Someone get me a clockwork assembly"; break;
                }

                Prisoner.Yell(text);
            }
        }
        public WrongPrisonerWest(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class WrongPrisonerLastMeal : BasePrisonerCamp
    {
        [Constructable]
        public WrongPrisonerLastMeal()
            : base()
        {
        }

        public override void AddComponents()
        {
            base.AddChest();
            AddGuard();
            base.AddMobiles();
        }
        public override void AddGuard()
        {
            // we want these guys nearby
            int x = 0;
            int y = 2;

            Mobile m = null;
            if (Utility.Chance(0.10))
                m = new BrigandLeader();
            else
                m = new Brigand();

            AddMobile(m, WanderRange, x, y, 0);

            ManageLock(m);
        }
        public override void OnEnter(Mobile m)
        {
            if (m.Player && Prisoner != null)
            {
                string text = string.Empty;

                switch (Utility.Random(4))
                {
                    default:
                    case 0: text = "I have no regrets."; break;
                    case 1: text = "I shan't enjoy my last meal."; break;
                    case 2: text = "I have lost my appetite."; break;
                    case 3: text = "Bread? Have you brought me some bread?"; break;
                }

                Prisoner.Yell(text);
            }
        }

        public WrongPrisonerLastMeal(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class WrongExecuteNoble : BasePrisonerCamp
    {
        [Constructable]
        public WrongExecuteNoble()
            : base()
        {
        }

        public override void AddComponents()
        {
            base.AddChest();
            AddGuard();
            base.AddMobiles();
        }
        public override void AddGuard()
        {
            // we want these guys nearby
            int x = 0;
            int y = 2;

            Mobile m = new Executioner();
            AddMobile(m, WanderRange, x, y, 0);

            ManageLock(m);
        }
        public override void OnEnter(Mobile m)
        {
            if (m.Player && Prisoner != null)
            {
                string text = string.Empty;

                switch (Utility.Random(6))
                {
                    default:
                    case 0: text = "I regret nothing!"; break;
                    case 1: text = "You have the wrong person!"; break;
                    case 2: text = "It wasn't me!"; break;
                    case 3: text = "Spare me, for I have a family."; break;
                    case 4: text = "I didn't do it!"; break;
                    case 5: text = string.Format("Please {0}, explain to them the circumstances.", m.Name); break;
                }

                Prisoner.Yell(text);
            }
        }

        public WrongExecuteNoble(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class WrongExecuteController : BasePrisonerCamp
    {
        [Constructable]
        public WrongExecuteController()
            : base()
        {
        }

        public override void AddComponents()
        {
            base.AddChest();
            AddGuard();
            AddMobiles();
        }
        public override void AddGuard()
        {
            // we want these guys nearby
            int x = 0;
            int y = 2;

            Mobile m = new Executioner();
            AddMobile(m, WanderRange, x, y, 0);

            ManageLock(m);
        }
        public override void AddMobiles()
        {
            switch (Utility.Random(2))
            {
                case 0: Prisoner = new GolemController(); break;
                case 1: Prisoner = new GolemController(); break;
            }

            Prisoner.YellHue = Utility.RandomList(0x57, 0x67, 0x77, 0x87, 0x117);

            AddMobile(Prisoner, 2, -2, 0, 0);
        }
        public override void OnEnter(Mobile m)
        {
            if (m.Player && Prisoner != null)
            {
                string text = string.Empty;

                switch (Utility.Random(6))
                {
                    default:
                    case 0: text = "I regret nothing!"; break;
                    case 1: text = "Separating me from this world will not be an easy task."; break;
                    case 2: text = "I will introduce you to my friend, a golem!"; break;
                    case 3: text = "An axe? You'll need more than that!"; break;
                    case 4: text = "I did nothing wrong, I am but a lowly tinker."; break;
                    case 5: text = string.Format("Oh, {0}, you're next on my list!", m.Name); break;
                }

                Prisoner.Yell(text);
            }
        }

        public WrongExecuteController(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}