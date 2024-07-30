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

/* Scripts\Engines\Mitigation\GoodwillAsshat.cs
 * ChangeLog
 *  7/28/21, Adam
 *		First time checkin
 *		A Certain griefer is emptying the goodwill boxes at WBB. It has really infuriated the playerbase,
 *		    and for good reason. We will:
 *		    1. set DenyAccessPublicContainer = true on this player. This will prevent him from accessing public containers.
 *		    2. Each 24 hours, we will Poison him with Deadly poison.
 *		    3. If he dies, we will create a doppelganger (in his likeness) corpse to be on display for 24 hours. 
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using static Server.Utility;

namespace Server.Misc
{
    public partial class GoodwillAsshat
    {
        public static void Initialize()
        {
            EventSink.PlayerDeath += new PlayerDeathEventHandler(EventSink_PlayerDeath);
            EventSink.BeforePlayerDeath += new BeforePlayerDeathEventHandler(EventSink_BeforePlayerDeath);
        }

        private static Memory m_AsshatMemory = new Memory();            // short-term memory used to remember if we a saw this asshat before
        private static Memory m_DoppelgangerMemory = new Memory();      // long-term memory used to remember if already Doppelganger'ed him yet
        private static TimeSpan ShortTermMemory = TimeSpan.FromMinutes(5);
        private static TimeSpan LongTermMemory = TimeSpan.FromHours(24);
        public static bool ApplyPunishment(Mobile from, Container cont, Poison poison)
        {
            if (m_AsshatMemory.Recall(from) == false)
            {   // we haven't seen this player yet
                m_AsshatMemory.Remember(from, ShortTermMemory.TotalSeconds);
                if (cont == null)
                {
                    IPooledEnumerable eable = from.GetItemsInRange(3);
                    foreach (Item item in eable)
                        if (item is TrapableContainer bc)
                        {
                            cont = bc;
                            break;
                        }
                    eable.Free();
                }

                DoPoison(from, cont, poison);
            }
            /* else, we still remember him. No action. */

            return false;
        }

        private static void DoPoison(Mobile from, Container cont, Poison poison)
        {
            if (cont is TrapableContainer trapableContainer)
                trapableContainer.SendMessageTo(from, 502999, 0x3B2); // You set off a trap!

            from.Say("I have been a bad boy!");

            from.ApplyPoison(from, poison);

            // You are enveloped in a noxious green cloud!
            from.LocalOverheadMessage(Network.MessageType.Regular, 0x44, 503004);

            Effects.SendLocationEffect(from.Location, from.Map, 0x113A, 10, 20);
            Effects.PlaySound(from.Location, from.Map, 0x231);

            string message = string.Format("poisoned with {0} poison", poison);
            LogHelper logger = new LogHelper("Goodwill Asshat.log", false);
            logger.Log(LogType.Mobile, from, message);
            logger.Finish();

            // tell staff
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Goodwill Asshat: {0} {1}", from, message));
        }
        public static void EventSink_BeforePlayerDeath(BeforePlayerDeathEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            if (m_AsshatMemory.Recall(e.Mobile) == false)
                // we're not tracking this guy
                return;

            if (!e.Mobile.Poisoned)
            {   // no longer poisoned, you can go on about your business
                m_AsshatMemory.Forget(e.Mobile);
                return;
            }

            if (m_DoppelgangerMemory.Recall(e.Mobile) == true)
                // we already Doppelganger'ed this guy (once in 24 hours)
                return;
            else
                m_DoppelgangerMemory.Remember(e.Mobile, LongTermMemory.TotalSeconds);

            // Doppelganger time!
            Mobile killed = pm;
            // this timeout logic prevents someone from generating hundreds of Doppelgangers
            // create the dead miner    
            MurderedMiner exhibit = new MurderedMiner();

            // copy the clothes, jewelry, everything
            Utility.CopyLayers(exhibit, killed, CopyLayerFlags.Default);

            // now everything else
            exhibit.Name = killed.Name;
            exhibit.Hue = killed.Hue;
            exhibit.Body = (killed.Female) ? 401 : 400;   // get the correct body
            exhibit.Female = killed.Female;               // get the correct death sound
            exhibit.Direction = killed.Direction;         // face them the correct way
            exhibit.LastKiller = World.GetAdminAcct();    // let the world know who did them in
            exhibit.MoveToWorld(killed.Location, killed.Map);
        }
        public static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;
            if (pm.Corpse == null) return;
            if (m_AsshatMemory.Recall(e.Mobile) == false)
                // we're not tracking this guy
                return;

            string message = string.Format("died at location: {0}", pm.Location);
            LogHelper logger = new LogHelper("Goodwill Asshat.log", false);
            logger.Log(LogType.Mobile, pm, message);
            logger.Finish();

            // tell staff
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Goodwill Asshat: {0} {1}", pm, message));

            // forget about him. I think he learned his lesson
            m_AsshatMemory.Forget(e.Mobile);

            // finally, delete his corpse - too harsh
            //pm.Corpse.Delete();
        }

    }
}