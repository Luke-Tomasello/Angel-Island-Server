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

/* Scripts\Engines\AI\AI\BaseAI\MuxTimer.cs
 * CHANGELOG
 *  10/19/2023, Adam
 *      Add some top-level try/catch statements to catch any AI crash that would otherwise be missed.
 *  10/10/2023, Adam
 *      First time check in.
 */

/* Overview:
 *  AI/SP has about 16,000 mobiles. 
 *  In the RunUO model, each had its own virtual timer. Timers were constantly being created and destroyed and recreated with a change in frequency, 
 *      usually due to a mobile speed change, Passive vs Active.
 *  In this new model, we have a single timer that services only the active mobiles. The timer is never destroyed and/or modified.
 *      This 'service table' is updated (serviceable elements added/removed) as needed.
 *  Result: Mobile movement is smoother, and we unburden the standard RunUO timer system of the constant creation, destruction and servicing of so many timers.
 * ---
 * demultiplex: We take a single high-frequency AI Timer and multiplex it to N serviceable AI instances
 *          | AI1 needing servicing
 *  Tick => | AI2 needing servicing
 *          | AI3 needing servicing
 *          | ... etc
 */
using Server.Diagnostics;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public abstract partial class BaseAI : SerializableObject
    {
        private DateTime m_NextThought;
        private DateTime NextThought { get { return m_NextThought; } set { m_NextThought = value; } }

        private static Dictionary<BaseAI, bool> AIServiceTable = new();
        public static void InitializeMux()
        {
            Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromMilliseconds(10), new TimerCallback(MuxTick));
        }
        public enum ChangeRequest
        {
            Start,
            Stop,
            Add,
            Remove
        }
        public class Request
        {
            public BaseAI AI;
            public ChangeRequest ChangeRequest;
            public Request(BaseAI ai, ChangeRequest cr)
            {
                AI = ai;
                ChangeRequest = cr;
            }
        }
        private static Queue<Request> UpdateQueue = new Queue<Request>();
        private static void MuxTick()
        {
            while (UpdateQueue.Count > 0)
            {
                Request change = UpdateQueue.Dequeue();
                switch (change.ChangeRequest)
                {
                    case ChangeRequest.Start:
                        if (AIServiceTable.ContainsKey(change.AI))
                            AIServiceTable[change.AI] = true;
                        else
                            AIServiceTable.Add(change.AI, true);
                        break;
                    case ChangeRequest.Stop:
                        if (AIServiceTable.ContainsKey(change.AI))
                            AIServiceTable[change.AI] = false;
                        break;
                    case ChangeRequest.Add:
                        if (!AIServiceTable.ContainsKey(change.AI))
                            AIServiceTable.Add(change.AI, false);
                        break;
                    case ChangeRequest.Remove:
                        if (AIServiceTable.ContainsKey(change.AI))
                            AIServiceTable.Remove(change.AI);
                        break;
                }
            }

            UpdateQueue.Clear();

            // demultiplex: We take a single high-frequency AI Timer and multiplex it to N serviceable AI instances
            //         | AI1 needing servicing
            // Tick => | AI2 needing servicing
            //         | AI3 needing servicing
            //         | ... etc
            try { DMux(); }
            catch (Exception ex)
            {   // better safe than sorry.
                LogHelper.LogException(ex);
            }
        }

        private static void DMux()
        {
            DateTime now = DateTime.UtcNow;
            foreach (var kvp in AIServiceTable)
            {
                if (kvp.Value == false)
                {
                    UpdateQueue.Enqueue(new Request(kvp.Key, ChangeRequest.Remove));
                    continue;                           // AI not running
                }

                if (kvp.Key.m_Mobile == null || kvp.Key.m_Mobile.Deleted || kvp.Key.m_Mobile.Map == null || kvp.Key.m_Mobile.Map == Map.Internal)
                {
                    UpdateQueue.Enqueue(new Request(kvp.Key, ChangeRequest.Remove));
                    continue;                           // bad state
                }

                if (now >= kvp.Key.NextThought)
                {
                    kvp.Key.NextThought = now + TimeSpan.FromSeconds(kvp.Key.m_Mobile.CurrentSpeed);
                    try { kvp.Key.m_Timer.OnTick(); }
                    catch (Exception ex) { LogHelper.LogException(ex); }
                }
            }
        }

        public static void MuxRegister(BaseAI ai, TimeSpan delay, TimeSpan interval)
        {
            DateTime now = DateTime.UtcNow;
            ai.NextThought = now + delay;
            ai.NextThought += interval;
            UpdateQueue.Enqueue(new Request(ai, ChangeRequest.Add));
        }
        public static void MuxStart(BaseAI ai)
        {
            UpdateQueue.Enqueue(new Request(ai, ChangeRequest.Start));
        }
        public static void MuxStop(BaseAI ai)
        {
            UpdateQueue.Enqueue(new Request(ai, ChangeRequest.Stop));
        }
        public static bool MuxRunning(BaseAI ai)
        {
            if (AIServiceTable.ContainsKey(ai))
                return AIServiceTable[ai];

            return false;
        }
    }
}