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


/* Server/Memory.cs
 * ChangeLog:
 *  10/6/2026
 *      Moved from Utility.cs as part of a structural cleanup; no behavior change.
 *      Item and mobile Memory (Memory, Memory.ObjectMemory).
 */

using System;
using System.Collections;
namespace Server
{
    #region Item and Mobile Memory
    /*	Item and Mobile Memory
	 *	The Memory system is passive, that is it doesn't use any timers for cleaning up old objects, it
	 *	simply cleans them up the next time it's called before processing the request. The one exception
	 *	to this rule is when you specify a 'Release' callback to be notified when an object expires.
	 *	In this case a timer is created to insure your callback is timely.
	 */
    public class Memory
    {
        public class ObjectMemory
        {
            private object? m_context;
            public object? Context                                   // return what we remember about this thing
            { get { return m_context; } set { m_context = value; } }
            private object m_object;
            public object Object { get { return m_object; } }       // return the thing we remember
            private double m_seconds;
            private DateTime m_Expiration;
            public DateTime RefreshTime { get { return DateTime.UtcNow + TimeSpan.FromSeconds(m_seconds); } }
            public DateTime Expiration { get { return m_Expiration; } set { m_Expiration = value; } }
            public TimerStateCallback? m_OnReleaseEventHandler;
            public TimerStateCallback? OnReleaseEventHandler { get { return m_OnReleaseEventHandler; } }

            public ObjectMemory(object ox, double seconds)
                : this(ox, null, seconds)
            {
            }

            public ObjectMemory(object ox, object? context, double seconds)
                : this(ox, null, null, seconds)
            {
            }

            public ObjectMemory(object ox, object? context, TimerStateCallback? ReleaseEventHandler, double seconds)
            {
                m_object = ox;                                  // the thing to remember
                m_seconds = seconds;                            // how long to remember
                m_context = context;                            // what we remember about this object
                m_OnReleaseEventHandler = ReleaseEventHandler;  // release callback
                m_Expiration = RefreshTime;                     // when to delete
            }
        };

        private Hashtable m_MemoryCache = new Hashtable();
        public Hashtable MemoryCache => m_MemoryCache;
        private bool m_Tidying;

        public void TidyMemory()
        {   // we can reenter when we are called back via a user callback.
            //	For instance if the usercallback calls Recal() or anyother function that calls Tidy, we will reenter
            if (m_Tidying == false)
            {
                m_Tidying = true;
                // first clreanup the LOS cache
                ArrayList cleanup = new ArrayList();
                foreach (DictionaryEntry de in m_MemoryCache)
                {   // list expired elements
                    if (de.Value == null) continue;
                    if (DateTime.UtcNow > (de.Value as ObjectMemory).Expiration)
                        cleanup.Add(de.Key as object);
                }

                foreach (object ox in cleanup)
                {   // remove expired elements
                    if (ox == null) continue;
                    if (m_MemoryCache.Contains(ox))
                        Remove(ox);
                }
                m_Tidying = false;
            }
        }

        public void WipeMemory()
        {
            m_MemoryCache.Clear();
        }

        // you should be calling Forget() to remove objects.
        //	This is called internally
        public void Remove(object ox)
        {   // DO NOT CALL TIDY HERE
            if (m_MemoryCache.Contains(ox))
            {   // call user defined cleanup
                ObjectMemory? om = m_MemoryCache[ox] as ObjectMemory;
                if (om.m_OnReleaseEventHandler != null)
                    om.m_OnReleaseEventHandler(om.Context);
                m_MemoryCache.Remove(ox);
            }
        }

        public int Count
        {
            get
            {
                TidyMemory();
                return m_MemoryCache.Count;
            }
        }

        public void Remember(object ox, double seconds)
        {   // preserve the previous context if there was one
            ObjectMemory? om = Recall(ox);
            object? temp = (om != null) ? om.Context : null;
            Remember(ox, temp, seconds);
        }

        public void Remember(object ox, object? context, double seconds)
        {
            Remember(ox, context, null, seconds);
        }

        public void Remember(object ox, object? context, TimerStateCallback? releaseHandler, double seconds)
        {
            TidyMemory();
            if (ox == null) return;
            if (Recall(ox) != null)
            {   // we already know about this guy - just update with a temporary expiration.
                //  This expiration is temporary since any subsequent refreshes will revert to the stored m_seconds refresh.
                (m_MemoryCache[ox] as ObjectMemory).Expiration = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);
                return;
            }
            m_MemoryCache[ox] = new ObjectMemory(ox, context, releaseHandler, seconds);

            if (releaseHandler != null)
                Timer.DelayCall(TimeSpan.FromSeconds(seconds), new TimerStateCallback(Remove), ox);
        }

        public void Forget(object ox)
        {
            TidyMemory();
            if (ox == null) return;
            if (m_MemoryCache.Contains(ox))
                Remove(ox);
        }

        public bool Recall(Mobile mx)
        {
            return Recall(mx as object) != null;
        }

        public ObjectMemory? Recall(object? ox)
        {
            TidyMemory();
            if (ox == null) return null;
            if (m_MemoryCache.Contains(ox))
                return m_MemoryCache[ox] as ObjectMemory;
            return null;
        }

        public void Refresh(object ox, object? context, double? seconds = null)
        {
            TidyMemory();
            if (ox == null) return;
            if (m_MemoryCache.Contains(ox))
            {
                if (seconds == null)
                    (m_MemoryCache[ox] as ObjectMemory).Expiration = (m_MemoryCache[ox] as ObjectMemory).RefreshTime;
                else
                    (m_MemoryCache[ox] as ObjectMemory).Expiration = DateTime.UtcNow + TimeSpan.FromSeconds(seconds.GetValueOrDefault());
                (m_MemoryCache[ox] as ObjectMemory).Context = context;
            }
        }

        public void Refresh(object ox)
        {
            // preserve the previous context if there was one
            ObjectMemory? om = Recall(ox);
            object? temp = (om != null) ? om.Context : null;
            Refresh(ox, temp);
        }
    }
    #endregion Item and Mobile Memory
}
