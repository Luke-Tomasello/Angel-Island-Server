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

/* Server/Misc/ValidationQueue.cs
 * CHANGELOG:
 *  8/27/23, Yoar
 *	    Initial version.
 *	    
 *	    Use this generic, static/singleton class to enqueue post-serialization fixes to items/mobiles
 */

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server
{
    public delegate void ValidationEventHandler();

    public static class ValidationQueue
    {
        public static event ValidationEventHandler StartValidation;

        public static void Initialize()
        {
            if (StartValidation != null)
                StartValidation();

            StartValidation = null;
        }
    }

    public static class ValidationQueue<T>
    {
        private static Queue<InternalEntry> m_Queue;

        static ValidationQueue()
        {
            m_Queue = new Queue<InternalEntry>();

            ValidationQueue.StartValidation += new ValidationEventHandler(ValidateAll);
        }

        public static void Enqueue(T t)
        {
            Enqueue(t, null);
        }

        public static void Enqueue(T t, object state)
        {
            m_Queue.Enqueue(new InternalEntry(t, state));
        }

        private static void ValidateAll()
        {
            if (m_Queue.Count == 0)
                return;

            MethodInfo methodInfo = GetMethod();

            if (methodInfo == null)
                return;

            while (m_Queue.Count != 0)
            {
                InternalEntry e = m_Queue.Dequeue();

                InvokeValidate(methodInfo, e.Instance, e.State);
            }
        }

        private static MethodInfo GetMethod()
        {
            MethodInfo methodInfo;

            try
            {
                methodInfo = typeof(T).GetMethod("Validate", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(object) }, null);
            }
            catch
            {
                methodInfo = null;
            }

            return methodInfo;
        }

        private static void InvokeValidate(MethodInfo methodInfo, T instance, object state)
        {
            try
            {
                methodInfo.Invoke(instance, new object[] { state });
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private struct InternalEntry
        {
            private T m_Instance;
            private object m_State;

            public T Instance { get { return m_Instance; } }
            public object State { get { return m_State; } }

            public InternalEntry(T instance, object state)
            {
                m_Instance = instance;
                m_State = state;
            }
        }
    }
}