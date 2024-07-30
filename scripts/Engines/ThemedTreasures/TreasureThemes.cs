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

/* Scripts/Engines/ThemedTreasures/TreasureThemes.cs
 * CHANGELOG:
 * 9/8/2023, Adam (GuardIgnore)
 *      We have chests that can be dug up in town. Guards will ignore this spawn
 * 7/12/21, adam (line:213)
        Use the spawner logic for 'far' spawns. I don't much like the chest spawning 4 high-level creatures all right on top of you.
 *  6/02/06, Kit
 *		added Home and HomeRange to spawn function of spawning creatures. 
 *		no wandering away from chest!
 *	1/20/06, Adam
 *		add new functions: RandomOverlandTheme() and IsOverlandTheme().
 *	04/18/05, Kitaras
 *		Fixed bug to only set maps of level 3 or greater to themed.
 *	04/05/05, Kitaras
 *		Added controls to allow only 1 level 5 mob spawn for guardian spawns
 *	03/30/05, Kitaras
 *		Initial Creation
 */
using Server.Mobiles;
using System;
using static Server.Utility;

namespace Server.Misc
{
    //Theme types new ones to be added here or removed.
    public enum ChestThemeType
    {
        None = 0,
        Solen,
        Brigand,
        Savage,
        Undead,
        Pirate,
        Dragon,
        /* begin special themes - these themes are ONLY available via the Overland System */
        Lizardmen,
        Ettin,
        Ogre,
        Ophidian,
        Skeleton,
        Ratmen,
        Orc,
        Terathan,
        FrostTroll
        /* end special themes */
    };

    //overall control class for treasure chest spawn mechanics
    public class TreasureTheme
    {
        public static ChestThemeType RandomOverlandTheme()
        {
            return (ChestThemeType)Utility.RandomList(
                (int)ChestThemeType.Lizardmen,
                (int)ChestThemeType.Ettin,
                (int)ChestThemeType.Ogre,
                (int)ChestThemeType.Ophidian,
                (int)ChestThemeType.Skeleton,
                (int)ChestThemeType.Ratmen,
                (int)ChestThemeType.Orc,
                (int)ChestThemeType.Terathan,
                (int)ChestThemeType.FrostTroll);
        }

        public static bool IsOverlandTheme(ChestThemeType theme)
        {
            return ((int)theme >= (int)ChestThemeType.Lizardmen && (int)theme <= (int)ChestThemeType.FrostTroll);
        }

        //spawns strongest mob in given theme based on spot 0 in  creature list or 0/1 for undead theme
        public static BaseCreature SpawnHighestMob(ChestThemeType theme)
        {
            switch (theme)
            {
                default: break;
                case ChestThemeType.Undead:
                    {
                        int random = Utility.Random(1);
                        return (BaseCreature)Activator.CreateInstance(ThemeTypes[(int)theme][random]);
                    }

                case ChestThemeType.Pirate:
                    {
                        return (BaseCreature)Activator.CreateInstance(ThemeTypes[(int)theme][0]);
                    }

            }
            return null;
        }

        public static bool GetIsThemed(int level)
        {
            //15% chance of it being set to true
            if (level >= 3)
            {
                if (Utility.RandomDouble() < .15) return true;
            }
            return false;
        }

        public static int GetGuardianSpawn(bool IsThemed, ChestThemeType theme)
        {
            int NormalGuardians = 4; // spawns 4 guardians as if a normal chest
            int ThemeGuardians;

            if (theme == ChestThemeType.Pirate || theme == ChestThemeType.Undead)
            {
                ThemeGuardians = 5; //spawns 5 guardians if is a themed chest of above type because special case of spawning 1 highlevel mob
            }
            else
                ThemeGuardians = 6;

            if (IsThemed == true) return ThemeGuardians;
            return NormalGuardians;
        }

        //returns a random theme type based on level of map
        public static int GetThemeType(int level)
        {
            int theme = 0;
            if (level == 3) theme = Utility.RandomMinMax(1, 3);
            if (level == 4) theme = Utility.RandomMinMax(4, 5);
            if (level == 5) theme = 6;
            return theme;
        }

        //returns string to send to player when chest is dug
        public static string GetThemeMessage(ChestThemeType type)
        {
            if ((int)type >= 0 && (int)type < ThemeMessages.Length)
            {
                try
                {
                    return ThemeMessages[(int)type];
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }
            return null;
        }


        //Begin Spawn mechanics based on level and if themed and theme type
        private static BaseCreature Spawn(int level, bool IsThemed, ChestThemeType theme, bool guardian)
        {
            //handle standered spawn levels based on chest level
            if (IsThemed == false && level >= 0 && level < StanderedTypes.Length)
            {
                try
                {
                    return (BaseCreature)Activator.CreateInstance(StanderedTypes[level][Utility.Random(StanderedTypes[level].Length)]);
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }

            //handle ThemeSpawns depending on themeType
            if (IsThemed == true && theme >= 0 && (int)theme < ThemeTypes.Length)
            {
                try
                {
                    //if not a special case chest spawn random creatures based on list
                    if (guardian == false)
                    {
                        return (BaseCreature)Activator.CreateInstance(ThemeTypes[(int)theme][Utility.Random(ThemeTypes[(int)theme].Length)]);
                    }
                    if (guardian == true)
                    {
                        //begin special case check for iob themed chests to not spawn level 5 mobs dureing initial guardian spawn.
                        if (theme == ChestThemeType.Undead) return (BaseCreature)Activator.CreateInstance(ThemeTypes[(int)theme][Utility.RandomMinMax(2, 4)]);
                        if (theme == ChestThemeType.Pirate) return (BaseCreature)Activator.CreateInstance(ThemeTypes[(int)theme][Utility.RandomMinMax(1, 3)]);
                    }
                    else
                    {
                        //if guardian is true but not of above type return random based monster
                        return (BaseCreature)Activator.CreateInstance(ThemeTypes[(int)theme][Utility.Random(ThemeTypes[(int)theme].Length)]);
                    }
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            }
            return null;
        }

        //Main spawn generation function called from any item that needs to generate treasure type spawn.
        public static void Spawn(int level, Point3D p, Map map, Mobile target, bool IsThemed, ChestThemeType theme, bool guardian, bool guardian2)
        {
            //bool guardian is designator for if theme type has special spawn mechanics and thus does not spawn its highest mobs at random
            //guardian2 is second flag indicating to spawn highest mob.
            if (map == null)
                return;
            BaseCreature c = null;
            if (guardian == false) c = Spawn(level, IsThemed, theme, guardian);
            if (guardian == true && guardian2 == false) c = Spawn(level, IsThemed, theme, true);
            if (guardian == true && guardian2 == true) c = SpawnHighestMob(theme);
            if (c != null)
            {
                c.Home = p;
                c.RangeHome = 8;

                // 9/8/2023, Adam: We have chests that can be dug up in town. Guards will ignore this spawn
                c.GuardIgnore = true;

                // 7/12/21, adam
                //  Use the spawner logic for 'far' spawns. I don't much like the chest spawning 4 high-level creatures all right on top of you.
                bool spawned = true;
                Point3D dest;
                if ((dest = Spawner.GetSpawnPosition(map, p, 7, SpawnFlags.SpawnFar | SpawnFlags.AvoidPlayers, c)) == p)
                    spawned = false;
                else
                    c.MoveToWorld(dest, map);

                if (!spawned)
                    c.Delete();
                else if (target != null)
                    c.Combatant = target;
            }
        }


        //standered treasuremap spawns levels 0-5, add additional ones here
        private static Type[][] StanderedTypes = new Type[][]
        {
            new Type[]{ typeof( Mongbat ), typeof( Skeleton ) },
            new Type[]{ typeof( Mongbat ), typeof( Ratman ), typeof( HeadlessOne ), typeof( Skeleton ), typeof( Zombie ) },
            new Type[]{ typeof( OrcishMage ), typeof( Gargoyle ), typeof( Gazer ), typeof( HellHound ), typeof( EarthElemental ) },
            new Type[]{ typeof( Lich ), typeof( OgreLord ), typeof( DreadSpider ), typeof( AirElemental ), typeof( FireElemental ) },
            new Type[]{ typeof( DreadSpider ), typeof( LichLord ), typeof( Daemon ), typeof( ElderGazer ), typeof( OgreLord ) },
            new Type[]{ typeof( LichLord ), typeof( Daemon ), typeof( ElderGazer ), typeof( PoisonElemental ), typeof( BloodElemental ) }
        };

        //ThemedChest spawn levels add additional ones here
        private static Type[][] ThemeTypes = new Type[][]
        {
            new Type[]{ }, // level 0 blank chest no guardians or spawn
			new Type[]{ typeof(RedSolenWorker), typeof(RedSolenWarrior), typeof(RedSolenInfiltratorQueen), typeof(RedSolenInfiltratorWarrior) },
            new Type[]{ typeof( Brigand ), typeof( BrigandArcher ) },
            new Type[]{ typeof( Savage ), typeof( SavageRider ), typeof( SavageShaman ) },
            new Type[]{ typeof( WraithRiderMage ), typeof(WraithRiderWarrior), typeof( BoneMagiLord  ), typeof( SkeletalKnight ), typeof( BoneKnightLord ) },
            new Type[]{ typeof( PirateCaptain ), typeof( Pirate ), typeof(PirateDeckHand), typeof( PirateWench ) },
            new Type[]{ typeof( Dragon ), typeof( Wyvern ), typeof( Drake ) },
			/* begin special themes */
			new Type[]{ typeof( Lizardman ), typeof( Lizardman ), typeof( Lizardman ) }, // Lizardmen
			new Type[]{ typeof( Ettin ), typeof( Ettin ), typeof( Ettin ) }, // Ettin 
			new Type[]{ typeof( Ogre ), typeof( Ogre ), typeof( OgreLord ) }, // Ogre
			new Type[]{ typeof( OphidianWarrior ), typeof( OphidianKnight ), typeof( OphidianArchmage ) }, // Ophidian
			new Type[]{ typeof( SkeletalKnight ), typeof( BoneKnight ), typeof( BoneKnightLord ) }, // Skeleton
			new Type[]{ typeof( Ratman ), typeof( RatmanMage ), typeof( RatmanArcher ) }, // Ratmen
			new Type[]{ typeof( Orc ), typeof( OrcishLord ), typeof( OrcishMage ) }, // Orc
			new Type[]{ typeof( TerathanDrone ), typeof( TerathanWarrior ), typeof( TerathanAvenger ) }, // Terathan	
			new Type[]{ typeof( Troll ), typeof( FrostTroll ), typeof( FrostTroll ) } // FrostTroll
		};


        //Themed warning strings add new ones here
        private static string[] ThemeMessages = new string[]
        {
            "", //blank chest no theme set to match ChestThemeType.None
			"You have dug into a solen hole!",
            "You have sprung a trap and brigands come to protect their treasure!",
            "You have disturbed the sacred burial grounds of the savages!",
            "You have disturbed the resting place of lost souls!",
            "Arrr! The pirate spirits come to life to protect their booty!",
            "The ground shakes beneath you as you have just disturbed the slumber of the dragon kin!",	
			/* begin special themes */
			"The reptilian scourge you have found!", // Lizardmen,
			"*You feel the ground shake as you spot the first one of them*", // Ettin,
			"*a stench of rotting meat fills the air*", // Ogre,
			"The reptilian knights have been sent to protect their treasure!", // Ophidian,
			"*you hear the clatter and clack of bones closing in around you!*", // Skeleton,
			"The vermin horde descends upon you!", // Ratmen,
			"*a great stink blows the wilderness*", // Orc,
			"*a chill runs up your spine as you feel 1000 beady little eyes upon you!*", // Terathan,
			"*you hear heavy footsteps approaching, but are afraid to look*", // FrostTroll
		};
    }
}