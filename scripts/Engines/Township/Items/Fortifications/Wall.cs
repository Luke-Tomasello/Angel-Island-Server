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

/* Engines/Township/Items/Fortifications/Wall.cs
 * CHANGELOG:
 * 3/17/22, Adam
 *  Update OnBuild to use new parm list
 * 11/23/21, Yoar
 *      Now derives from TownshipStatic.
 *      Moved most functionality to TownshipStatic.
 * 11/20/21, Yoar
 *	    Added AFK checks.
 *	    Added TC notice.
 * 11/20/21, Yoar
 *	    Refactored township walls/tools.
 * 2010.05.24 - Pix
 *      Code cleanup (renaming functions/classes to make sense, reorganizing for easier reading)
 *      Moves Stone and Spear walls to separate files.
 * 4/23/10, adam
 *		1. Add CanDamageWall & DamageWeapon to insure the player has the right tool for the job
 *		2. adam hack until pixie reviews
 * 			The problem is twofold. Firstly a GM carpenter can do 20+25=42 HP damage per 120 seconds to a wall with only 127 HP
 * 			this means a GM carpenter can take down a rather expensive wall in ~3 minutes
 * 			Secondly, since we do a skill check, we're awarding carpentry skill points for free (while we grief the township owner). This 
 * 			seems unbalanced
 * 			HACK: reduce damage to average 1 HP per 120 seconds AND require a special tool (not bare hands, and not a newbied ax)
 * 11/30/08, Pix
 *		Added Alive checks.
 * 11/16/08, Pix
 *		Refactored, rebalanced, and fixed stuff
 * 10/19/08, Pix
 *		Spelling fix.
 * 10/17/08, Pix
 *		Reduced the skill requirement to repair the wall.
 * 10/17/08, Pix
 *		Fixed the timer for repair/damage to stop if they moved.
 * 10/15/08, Pix
 *		Changed that you need to be within 2 tiles of the wall to damage/repair it.
 * 10/15/08, Pix
 *		Added graphics.
 *		Added delays to repair/damage.
 * 10/10/08, Pix
 *		Initial.
*/

using System;

namespace Server.Township
{
    public class FortificationWall : TownshipStatic
    {
        [Constructable]
        public FortificationWall(int itemID)
            : base(itemID)
        {
            Weight = 150;
        }

        public override void OnBuild(Mobile from)
        {
            base.OnBuild(from);

            int hits = GetInitialHits(from);

            this.HitsMax = hits;
            this.Hits = hits;
        }

        protected virtual int GetInitialHits(Mobile m)
        {
            int carp = (int)m.Skills[SkillName.Carpentry].Value;
            int tink = (int)m.Skills[SkillName.Tinkering].Value;
            int mine = (int)m.Skills[SkillName.Mining].Value;
            int jack = (int)m.Skills[SkillName.Lumberjacking].Value;

            int smit = (int)m.Skills[SkillName.Blacksmith].Value;
            int alch = (int)m.Skills[SkillName.Alchemy].Value;
            int item = (int)m.Skills[SkillName.ItemID].Value;
            int mace = (int)m.Skills[SkillName.Macing].Value;
            int scrb = (int)m.Skills[SkillName.Inscribe].Value;
            int dtct = (int)m.Skills[SkillName.DetectHidden].Value;
            int cart = (int)m.Skills[SkillName.Cartography].Value;

            int baseHits = 100;

            //"main" skills add the most
            baseHits += carp / 4; //+25 @ GM
            baseHits += tink / 4; //+25 @ GM
            baseHits += mine / 4; //+25 @ GM
            baseHits += jack / 4; //+25 @ GM

            //"support" skills add some more
            baseHits += smit / 10;//+10 @ GM
            baseHits += alch / 10;//+10 @ GM
            baseHits += item / 10;//+10 @ GM
            baseHits += mace / 10;//+10 @ GM
            baseHits += scrb / 10;//+10 @ GM
            baseHits += dtct / 10;//+10 @ GM
            baseHits += cart / 10;//+10 @ GM

            return baseHits;
        }

        public FortificationWall(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version >= 3)
            {
                // class insertion, do nothing
            }
            else
            {
                #region Legacy

                switch (version)
                {
                    case 2:
                    case 1:
                        {
                            this.LastDamage = reader.ReadDateTime();

                            goto case 0;
                        }
                    case 0:
                        {
                            if (version < 2)
                                reader.ReadInt(); // repair skill

                            Mobile owner = reader.ReadMobile();
                            DateTime placed = reader.ReadDateTime();

                            ValidationQueue<FortificationWall>.Enqueue(this, new object[] { owner, placed });

                            reader.ReadInt(); // hits max init
                            this.HitsMax = reader.ReadInt();
                            this.LastRepair = reader.ReadDateTime();
                            this.Hits = reader.ReadInt();

                            break;
                        }
                }

                #endregion
            }
        }

        private void Validate(object state)
        {
            object[] array = (object[])state;

            Mobile owner = (Mobile)array[0];
            DateTime placed = (DateTime)array[1];

            TownshipItemHelper.SetOwnership(this, owner, placed);
        }
    }
}