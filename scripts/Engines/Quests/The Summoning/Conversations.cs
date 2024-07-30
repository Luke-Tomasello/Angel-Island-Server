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

using Server.Mobiles;

namespace Server.Engines.Quests.Doom
{
    public class AcceptConversation : QuestConversation
    {
        public override object Message
        {
            get
            {
                /* You have accepted Victoria's help.  She requires 1000 Daemon
				 * bones to summon the devourer.<BR><BR>
				 * 
				 * You may hand Victoria the bones as you collect them and she
				 * will keep count of how many you have brought her.<BR><BR>
				 * 
				 * Daemon bones can be collected via various means throughout
				 * Dungeon Doom.<BR><BR>
				 * 
				 * Good luck.
				 */
                return 1050027;
            }
        }

        public AcceptConversation()
        {
        }

        public override void OnRead()
        {
            System.AddObjective(new CollectBonesObjective());
        }
    }

    public class VanquishDaemonConversation : QuestConversation
    {
        public override object Message
        {
            get
            {
                /* Well done brave soul.   I shall summon the beast to the circle
				 * of stones just South-East of here.  Take great care - the beast
				 * takes many forms.  Now hurry...
				 */
                return 1050021;
            }
        }

        public VanquishDaemonConversation()
        {
        }

        public override void OnRead()
        {
            Victoria victoria = ((TheSummoningQuest)System).Victoria;

            if (victoria == null)
            {
                System.From.SendMessage("Internal error: unable to find Victoria. Quest unable to continue.");
                System.Cancel();
            }
            else
            {
                SummoningAltar altar = victoria.Altar;

                if (altar == null)
                {
                    System.From.SendMessage("Internal error: unable to find summoning altar. Quest unable to continue.");
                    System.Cancel();
                }
                else if (altar.Daemon == null || !altar.Daemon.Alive)
                {
                    BoneDemon daemon = new BoneDemon();

                    daemon.MoveToWorld(altar.Location, altar.Map);
                    altar.Daemon = daemon;

                    System.AddObjective(new VanquishDaemonObjective(daemon));
                }
                else
                {
                    victoria.SayTo(System.From, "The devourer has already been summoned.");

                    ((TheSummoningQuest)System).WaitForSummon = true;
                }
            }
        }
    }
}