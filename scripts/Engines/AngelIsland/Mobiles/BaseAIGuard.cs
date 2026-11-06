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

/* Scripts/Engines/AngelIsland/BaseAIGuard.cs
 
 *	ChangeLog
 *	10/16/26, Adam
 *	    Replace the static AfterHoursClub door with FindClubDoor(): the GuardPostDoor near the guard's own
 *	    champion engine (SpawnerEngine). Felucca and Trammel (and any other engine) each manage their own door.
 *	8/6/2021, Adam
 *	    Gards were too slow. Speed them up a bit. 
 *	5/17/04, mith
 *		Cleaned up the way we delete armor in OnBeforeDeath().
 *	4/10/04 changes by mith
 *		Added chance to get a bow along with the other weapons. 
 *		Changed Mace to WarMace.
 *	4/7/04, changes by mith
 *		Changed FightMode to Aggressor instead of None.
 *		Removed tunic/doublet/body sash creation so these items don't drop to corpse.
 *		Replaced halberd with Broadsword so they don't deal quite so much damage as quick.
 *	Created 4/3/04 by mith
 */
using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
using Server.Items;

namespace Server.Mobiles
{
    public class BaseAIGuard : BaseCreature
    {
        #region After Hours Club
        // The club door is the GuardPostDoor next to the guard champion engine (3 tiles away),
        //  so each engine, on each facet, manages its own door.
        public const int ClubDoorRange = 4;

        /// <summary>
        /// The After Hours Club door belonging to the champion engine that spawned this guard.
        /// Null for guards not spawned by a champion engine, when no door is found, or when the
        /// nearby doors are ambiguous (more than one door that are not linked halves of each other).
        /// </summary>
        public static BaseDoor? FindClubDoor(Mobile guard)
        {
            ChampEngine? engine = guard.SpawnerEngine;
            if (engine == null || engine.Deleted || engine.Map == null || engine.Map == Map.Internal)
                return null;

            BaseDoor? found = null;
            bool ambiguous = false;
            IPooledEnumerable eable = engine.Map.GetItemsInRange(engine.Location, ClubDoorRange);
            foreach (Item item in eable)
                if (item is GuardPostDoor door && !door.Deleted)
                {
                    if (found == null || (door.GetChain().Contains(found) && door.GetDistanceToSqrt(engine) < found.GetDistanceToSqrt(engine)))
                        found = door;
                    else if (!door.GetChain().Contains(found))
                        ambiguous = true;
                }
            eable.Free();

            if (ambiguous)
            {
                LogHelper logger = new LogHelper("AfterHoursClub.log", false);
                logger.Log(LogType.Item, engine, string.Format("More than one club door near this champion engine; no club door used (guard {0})", guard));
                logger.Finish();
                return null;
            }

            return found;
        }
        #endregion After Hours Club

        [Constructable]
        public BaseAIGuard()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Title = "the guard";

            InitStats(400, 100, 100);
            SpeechHue = Utility.RandomSpeechHue();

            SetSkill(SkillName.Anatomy, 70.1, 80.0);
            SetSkill(SkillName.Tactics, 70.1, 80.0);
            SetSkill(SkillName.Swords, 70.1, 80.0);
            SetSkill(SkillName.MagicResist, 70.1, 80.0);

            InitBody();
            InitOutfit();
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();

            Female = Utility.RandomBool();

            if (Female)
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            // Hair
            Item hair = new Item(Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2047, 0x2049, 0x204A));
            hair.Hue = Utility.RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            // Beard (male only)
            if (!Female && Utility.RandomBool())
            {
                Item beard = new Item(Utility.RandomList(0x203E, 0x203F, 0x2040, 0x2041, 0x204B, 0x204C, 0x204D));
                beard.Hue = hair.Hue;
                beard.Layer = Layer.FacialHair;
                beard.Movable = false;
                AddItem(beard);
            }

            if (Female)
            {
                switch (Utility.Random(2))
                {
                    case 0: AddItem(new LeatherSkirt()); break;
                    case 1: AddItem(new LeatherShorts()); break;
                }

                switch (Utility.Random(5))
                {
                    case 0: AddItem(new FemaleLeatherChest()); break;
                    case 1: AddItem(new FemaleStuddedChest()); break;
                    case 2: AddItem(new LeatherBustierArms()); break;
                    case 3: AddItem(new StuddedBustierArms()); break;
                    case 4: AddItem(new FemalePlateChest()); break;
                }
            }
            else
            {
                AddItem(new PlateChest());
                AddItem(new PlateArms());
                AddItem(new PlateGorget());
                AddItem(new PlateLegs());
            }

            // Weapon
            Broadsword weapon = new Broadsword
            {
                Movable = false,
                Crafter = this,
                Quality = WeaponQuality.Exceptional
            };

            AddItem(weapon);
        }

        public BaseAIGuard(Serial serial) : base(serial) { }

        public override bool OnBeforeDeath()
        {
            // 4/28/2026, Adam:
            // Legacy Angel Island behavior deleted all armor and clothing on death,
            // likely to limit inmate item access. This has been disabled.
            // The restriction adds little mechanical value and results in undesirable
            // visuals (e.g., naked corpses). Items are now preserved.

            //ArrayList items = new ArrayList(this.Items);
            //foreach (Item i in items)
            //{
            //    if (i is BaseArmor || i is BaseClothing)
            //        i.Delete();
            //}

            return base.OnBeforeDeath();
        }

        public bool DropWeapon(int minLevel, int maxLevel)
        {
            if (1.0 <= Utility.RandomDouble())
                return false;

            if (maxLevel > 2)
                maxLevel = 2;

            Cap(ref minLevel, 0, 2);
            Cap(ref maxLevel, 0, 2);

            BaseWeapon weapon = new Broadsword();

            double random = Utility.RandomDouble();
            if (random >= .75)
                weapon = new WarFork();
            else if (random >= .50)
                weapon = new WarMace();
            else if (random >= .25)
            {
                weapon = new Bow();
                PackItem(new Arrow { Amount = 25 });
            }

            Item item = Loot.ImbueWeaponOrArmor(weapon, minLevel, maxLevel);
            PackItem(item);

            return true;
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