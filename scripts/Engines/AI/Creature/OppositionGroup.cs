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

/* Scripts\Engines\AI\Creature\OppositionGroup.cs
 * ChangeLog
 *	1/28/11, Adam
 *		Add back OppositionGroup for !Core.AngelIsland
 */

using Server.Mobiles;
using System;

namespace Server
{
    public class OppositionGroup
    {
        private Type[][] m_Types;

        public OppositionGroup(Type[][] types)
        {
            m_Types = types;
        }

        public bool IsEnemy(object from, object target)
        {
            int fromGroup = IndexOf(from);
            int targGroup = IndexOf(target);

            return (fromGroup != -1 && targGroup != -1 && fromGroup != targGroup);
        }

        public int IndexOf(object obj)
        {
            if (obj == null)
                return -1;

            Type type = obj.GetType();

            for (int i = 0; i < m_Types.Length; ++i)
            {
                Type[] group = m_Types[i];

                bool contains = false;

                for (int j = 0; !contains && j < group.Length; ++j)
                    contains = (type == group[j]);

                if (contains)
                    return i;
            }

            return -1;
        }

        private static OppositionGroup m_TerathansAndOphidians = new OppositionGroup(new Type[][]
            {
                new Type[]
                {
                    typeof( TerathanAvenger ),
                    typeof( TerathanDrone ),
                    typeof( TerathanMatriarch ),
                    typeof( TerathanWarrior )
                },
                new Type[]
                {
                    typeof( OphidianArchmage ),
                    typeof( OphidianKnight ),
                    typeof( OphidianMage ),
                    typeof( OphidianMatriarch ),
                    typeof( OphidianWarrior )
                }
            });

        public static OppositionGroup TerathansAndOphidians
        {
            get { return m_TerathansAndOphidians; }
        }

        private static OppositionGroup m_SavagesAndOrcs = new OppositionGroup(new Type[][]
            {
                new Type[]
                {
                    typeof( Orc ),
                    typeof( OrcBomber ),
                    typeof( OrcBrute ),
                    typeof( OrcCaptain ),
                    typeof( OrcishLord ),
                    typeof( OrcishMage ),
                    typeof( SpawnedOrcishLord )
                },
                new Type[]
                {
                    typeof( Savage ),
                    typeof( SavageRider ),
                    typeof( SavageRidgeback ),
                    typeof( SavageShaman )
                }
            });

        public static OppositionGroup SavagesAndOrcs
        {
            get { return m_SavagesAndOrcs; }
        }

        private static OppositionGroup m_FeyAndUndead = new OppositionGroup(new Type[][]
            {
                new Type[]
                {
                    typeof( Centaur ),
                    typeof( EtherealWarrior ),
                    typeof( Kirin ),
                    typeof( LordOaks ),
                    typeof( Pixie ),
                    typeof( Silvani ),
                    typeof( Unicorn ),
                    typeof( Wisp ),
                    typeof( Treefellow )
                },
                new Type[]
                {
                    typeof( AncientLich ),
                    typeof( Bogle ),
                    typeof( LichLord ),
                    typeof( Shade ),
                    typeof( Spectre ),
                    typeof( Wraith ),
                    typeof( BoneKnight ),
                    typeof( Ghoul ),
                    typeof( Mummy ),
                    typeof( SkeletalKnight ),
                    typeof( Skeleton ),
                    typeof( Zombie ),
                    typeof( ShadowKnight ),
                    typeof( DarknightCreeper ),
					//typeof( RevenantLion ),
					//typeof( LadyOfTheSnow ),
					typeof( RottingCorpse ),
                    typeof( SkeletalDragon ),
                    typeof( Lich )
                }
            });

        public static OppositionGroup FeyAndUndead
        {
            get { return m_FeyAndUndead; }
        }

        private static readonly OppositionGroup m_ElvesAndEvilElves = new OppositionGroup(new Type[][]
            {
                new Type[]
                {
                    typeof( ElfPeasant ),
                    typeof( ElfMace ),
                    typeof( ElfMage ),
                },
                new Type[]
                {
                    typeof( EvilElfPeasant ),
                    typeof( EvilElfMace ),
                    typeof( EvilElfMage ),
                }
            });

        public static OppositionGroup ElvesAndEvilElves
        {
            get { return m_ElvesAndEvilElves; }
        }
    }
}