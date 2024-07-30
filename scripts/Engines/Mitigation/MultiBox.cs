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

/* Scripts\Engines\Mitigation\MultiBox.cs
 * ChangeLog:
 * 1/17/22, Adam
 *  removed console print statement when group members are attacking each other - too spammy
 * 1/14/22, Adam
 * Initial creation
 */

/* Description:
 * In this implementation we only handle MultiBoxPVP.
 * In essence, we find ‘groups’ of 2 or more players all on the same IP.
 * If they aggress someone outside their group, we heuristically determine the leader of the group,
 *  then break the LOS between all the followers and the leader.
 * This effectively breaks the ‘following’ initiated client-side.
 * It’s not perfect, but should work well enough to make MultiBoxPVP near worthless.
 */

namespace Server.Engines
{
#if false
    public static class MultiBox
    {
        private static Dictionary<IPAddress, List<NetState>> m_database = new Dictionary<IPAddress, List<NetState>>();
        private static List<string> m_log = new List<string>();
        public static void Initialize()
        {
            EventSink.GameLogin += new GameLoginEventHandler(EventSink_GameLogin);
            EventSink.AggressiveAction += new AggressiveActionEventHandler(EventSink_AggressiveAction);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }

        public static void EventSink_GameLogin(GameLoginEventArgs e)
        {
            if (m_database.ContainsKey(e.State.Address))
            {
                if (m_database[e.State.Address] == null)
                    m_database[e.State.Address] = new List<NetState>() { e.State };
                else
                    m_database[e.State.Address].Add(e.State);
            }
            else
            {
                m_database.Add(e.State.Address, new List<NetState> { e.State });
            }

            // housekeeping
            RemoveStaleNetStates(m_database[e.State.Address]);
        }

        public static void EventSink_AggressiveAction(AggressiveActionEventArgs e)
        {
            if (e.Aggressor.Player && e.Aggressed.Player && e.Aggressor.NetState != null && e.Aggressed.NetState != null)
            {   // currently we're only handling PvP
                if (m_database.ContainsKey(e.Aggressor.NetState.Address))
                {   // as expected we know about this guy
                    if (m_database[e.Aggressor.NetState.Address].Count > 1 && ClientsConnected(m_database[e.Aggressor.NetState.Address]) > 1)
                    {   // at least there are multi-clienting                                          /* this access level bit is to to allow dev testing */
                        if (m_database[e.Aggressor.NetState.Address].Contains(e.Aggressed.NetState) && (e.Aggressor.NetState.Account as Account).AccessLevel == AccessLevel.Player)
                        {   // It's cool, he's attacking one of his group (same IP)
                            //  removed print statement - too spammy
                            //Utility.ConsoleOut("It's cool, he's attacking one of his group (same IP)", ConsoleColor.Green);
                        }
                        else
                        {   // not attacking one of their group

                            // here we do rule processing and determine offense (if any)
                            DetermineOffense(e.Aggressor);
                        }
                    }

                }
            }

        }

        public static void DetermineOffense(Mobile aggressor)
        {
#if false
            // the first order of business is to build a list of this guy's group
            List<Mobile> group = new List<Mobile>();
            foreach (NetState ns in m_database[aggressor.NetState.Address])
            {   // make sure we have a valid netstate
                if (ns == null || ns.Running == false || ns.Mobile == null)
                    continue;
                if (DateTime.UtcNow > ns.LastMultiBoxCensure + TimeSpan.FromMilliseconds(CoreAI.MultiBoxCensure))
                {
                    // ProximityCheck: throw out anyone not near the aggressor
                    if (aggressor.GetDistanceToSqrt(ns.Mobile) <= 5)
                        group.Add(ns.Mobile);
                }
                else
                {
                    Utility.ConsoleOut("Centure ignored", ConsoleColor.Red);
                }
            }

            // there is either no "group", or the aggressor isn't part of it.
            if (group.Count <= 1 || !group.Contains(aggressor))
                return;

            // okay, now we have the group
            //  Determine the leader: This is complex!
            //      Usually, who walks first and stops first is the leader (of the follow)
            //      however: If the leader moves (say one tile) and the followers don't move, then the list is reversed.
            //          that is, he who walked last is the leader.
            // heuristically derive our leader, there are 4 known cases:
            //  1. All parties have just logged in and noboby has 'walked' 
            //      - unhandled: Since m_lastMoveTime isn't serialized, it's a coin flip.
            //  2. the leader walked first and has stopped. Followers followed and stopped. 
            //      - handled: sort such that the first to move is top of the list.. this should be the leader of the follow 
            //  3. the leader is moving, but close enough to followers such that they do not move
            //      - handled: sort such that the last to move is top of the list.. this should be the leader of the follow 
            //  4. funky. the leader walked and stopped. follower_b also stops. follower_c takes a few more steps to catch up.
            //      in the case, we'll probably pick handler #3 and the determination fails, i.e., we'll pick the wrong leader.
            //      - unhandled: how do do this.
            //  Note: the leader need not be the aggressor
            //  Note: 'moving' and 'stopped' are relative terms - they are not absolute

            List<KeyValuePair<Mobile, double>> groupProfile = GetGroupProfile(group);
            if (AnyoneHasStopped(groupProfile))
            {
                if (LongestStanding(groupProfile) > CoreAI.MultiBoxPlayerStopped + CoreAI.MultiBoxPlayerStopped / 2)
                {   /*(3)*/
                    // someone in that group has been standing for a long time, and the leader is moving
                    // we will sort placing the most recent move at the top of the list (leader)
                    MostRecentMove(group, groupProfile);
                    Utility.ConsoleOut("Followers Stopped leader is moving {0}", ConsoleColor.Red, group[0]);
                }
                else
                {   /*(2)*/
                    // if someone stopped, pick the one that has stopped for the longest time, that will be our leader.
                    LeastRecentMove(group, groupProfile);
                    Utility.ConsoleOut("Followers Stopped leader is stopped {0}", ConsoleColor.Green, group[0]);
                }
            }
            else
            {   /*(2)*/
                // pick the one that has stopped for the longest time, that will be our leader.
                LeastRecentMove(group, groupProfile);
                Utility.ConsoleOut("Our leader is {0}", ConsoleColor.Yellow, group[0]);
            }

            // Now we have the group, with the leader as the first element, all within 5 tiles of the aggressor

            // There are two cases to consider here:
            //  B follws A, C follows A, and D follows A
            // -and-
            // B follws A, C follows B, and D follows C

            // we probably don't need to handle the second case, although we can add that.
            //  I think by breaking the first case, we sufficiently mess up the multiBox to discourage its use

            // ix=1 to skip the leader as we only 'break follow' everyone following him
            // Note: the RunUO architecture generates two EventSink_AggressiveAction messages,
            //  so we track LastMultiBoxCensure as a DateTime to prevent hammering the client with too many censures.
            //  Not ideal, but beyond the scope of this mitigation strategy.
            for (int ix = 1; ix < group.Count; ix++)
                new CbreakFollow(group[0], group[ix]);
#endif
            return;
        }

        public static double LongestStanding(List<KeyValuePair<Mobile, double>> groupProfile)
        {
            // find the group member that has been standing the logest
            double longest = 0;
            foreach (KeyValuePair<Mobile, double> kvp in groupProfile)
                if (kvp.Value > longest)
                    longest = kvp.Value;

            return longest;
        }

        public static List<KeyValuePair<Mobile, double>> GetGroupProfile(List<Mobile> group)
        {
            List<KeyValuePair<Mobile, double>> keyValuePairs = new List<KeyValuePair<Mobile, double>>();
            DateTime current = DateTime.UtcNow;
            foreach (Mobile x in group)
            {
                TimeSpan ts = current - x.LastMoveTime;
                // likely the leader is moving and close enough that the followers are not
                keyValuePairs.Add(new KeyValuePair<Mobile, double>(x, ts.TotalMilliseconds));
            }

            return keyValuePairs;
        }

        public static bool AnyoneHasStopped(List<KeyValuePair<Mobile, double>> groupProfile)
        {
            // check to see if someone has stopped.
            bool stopped = false;
            foreach (KeyValuePair<Mobile, double> kvp in groupProfile)
                if (kvp.Value >= CoreAI.MultiBoxPlayerStopped)
                    stopped = true; // somebody has stopped.

            return stopped;
        }

        public static void MostRecentMove(List<Mobile> group, List<KeyValuePair<Mobile, double>> groupProfile)
        {
            // Sort by MostRecentMove
            groupProfile.Sort((e1, e2) =>
            {
                return e1.Value.CompareTo(e2.Value);
            });

            // copy new order to list
            for (int ix = 0; ix < group.Count; ix++)
                group[ix] = groupProfile[ix].Key;
        }

        public static void LeastRecentMove(List<Mobile> group, List<KeyValuePair<Mobile, double>> groupProfile)
        {
            // Sort by LeastRecentMove
            groupProfile.Sort((e1, e2) =>
            {
                return e2.Value.CompareTo(e1.Value);
            });

            // copy new order to list
            for (int ix = 0; ix < group.Count; ix++)
                group[ix] = groupProfile[ix].Key;
        }
        public class CbreakFollow
        {
            public CbreakFollow(Mobile followed, Mobile follower)
            {
                NetState state = follower.NetState;

                if (state.Mobile.CanSee(followed))
                {
                    state.Send(followed.RemovePacket);          // you can't see me
                    state.Send(new ChangeUpdateRange(0));       // your update range is now zero
                    state.LastMultiBoxCensure = DateTime.UtcNow;   // record last censure (you cannot be censured too often)

                    // now unhide
                    Timer.DelayCall(TimeSpan.FromSeconds(CoreAI.MultiBoxDelay), new TimerStateCallback(Tick), new object[] { state, followed });
                    state.Mobile.NonlocalStaffOverheadMessage(MessageType.Regular, 41, false, "*follow broken*");

                    // log it
                    m_log.Add(string.Format("followed {0}, follower {1} at {2}", followed, follower, follower.Location));
                }
            }

            // un(pseudo)hide followed
            private static void Tick(object state)
            {
                object[] aState = (object[])state;
                Network.NetState nstate = aState[0] as Network.NetState;
                Mobile followed = aState[1] as Mobile;
                Mobile follower = nstate.Mobile;
                nstate.Send(ChangeUpdateRange.Instantiate(18)); // your update range is restored
                nstate.Mobile.SendEverything();                 // you can now see everything
            }
        }
        public static int ClientsConnected(List<NetState> list)
        {
            int count = 0;
            foreach (NetState state in list)
                if (state != null && state.Running)
                    count++;

            return count;
        }

        public static void RemoveStaleNetStates(List<NetState> list)
        {
            List<NetState> toRemove = new List<NetState>();
            foreach (NetState state in list)
                if (state != null && state.Running == false)
                    toRemove.Add(state);

            foreach (NetState state in toRemove)
                list.Remove(state);
        }

        public static void Save(WorldSaveEventArgs e)
        {
            try
            {
                if (m_log.Count == 0)
                    return;

                LogHelper logger = new LogHelper("MultiBoxPvP.log", false, true);
                foreach (string s in m_log)
                    logger.Log(s);
                logger.Finish();
                m_log.Clear();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
    }
#endif
}