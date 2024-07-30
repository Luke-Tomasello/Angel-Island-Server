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

/* Scripts/Engines/CommitGump/CommitGump.cs
 * CHANGELOG:
 *	02/10/08 - Plasma,
 *		Initial creation
 */
using Server.Gumps;
using System;
using System.Collections.Generic;

namespace Server.Engines.CommitGump
{
    public abstract class CommitGumpBase : Gump
    {

        #region session class

        /// <summary>
        /// Web style session !  Damn postbacks!
        /// </summary>
        public class GumpSession
        {
            public Dictionary<string, object> m_SessionData = new Dictionary<string, object>();

            /// <summary>
            /// Indexer will ensure the key is added if it doesn't exist
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public object this[string key]
            {
                get
                {
                    if (m_SessionData.ContainsKey(key)) return m_SessionData[key];
                    return null;
                }
                set
                {
                    if (!m_SessionData.ContainsKey(key)) m_SessionData.Add(key, null);
                    m_SessionData[key] = value;
                }
            }

            public int ObjectCount
            {
                get { return m_SessionData.Count; }
            }

        }

        #endregion

        #region enums

        public enum GumpReturnType
        {
            None,
            OK,
            Cancel
        }

        #endregion

        #region fields

        private int m_Page = 0;
        private GumpSession m_GumpSession = new GumpSession();
        private GumpReturnType m_GumpReturnType = GumpReturnType.Cancel;
        private ICommitGumpEntity m_MasterPage = null;
        private ICommitGumpEntity m_CurrentPage = null;
        private Mobile m_From = null;
        protected Dictionary<int, Type> m_EntityRegister = null;

        #endregion

        #region props

        public int Page
        {
            get { return m_Page; }
            set { m_Page = value; }
        }

        public ICommitGumpEntity MasterPage
        {
            get { return m_MasterPage; }
            set { m_MasterPage = value; }
        }

        public ICommitGumpEntity CurrentPage
        {
            get { return m_CurrentPage; }
            set { m_CurrentPage = value; }
        }

        public GumpSession Session
        {
            get { return m_GumpSession; }
            set { m_GumpSession = value; }
        }

        public Mobile From
        {
            get { return m_From; }
        }

        #endregion

        #region ctors

        public CommitGumpBase(Mobile from) : this(0, from) { }

        public CommitGumpBase(int page, Mobile from) : this(page, null, from) { }

        public CommitGumpBase(int page, GumpSession session, Mobile from)
            : base(25, 25)
        {
            this.Closable = true;
            this.Disposable = true;
            this.Dragable = true;
            this.Resizable = true;
            m_Page = page;
            m_From = from;
            if (session == null)
            {
                //First time creation
                //Create session object and populate entity register
                m_EntityRegister = new Dictionary<int, Type>();
                m_GumpSession = new GumpSession();
                RegisterEntities();

                m_GumpSession["COMMIT_ENTITY_REGISTER"] = m_EntityRegister;

                //Let the subclass create the pages the first time so its ctor has a chance to fire
            }
            else
            {
                //Restore session and entity register
                m_GumpSession = session;
                m_EntityRegister = m_GumpSession["COMMIT_ENTITY_REGISTER"] as Dictionary<int, Type>;
                //Set the type of current page to be created 
                SetCurrentPage();
                //Create master and current if they exist
                if (MasterPage != null)
                {
                    MasterPage.Create();
                    if (CurrentPage != null) this.AddPage(1);
                }
                if (CurrentPage != null) CurrentPage.Create();
            }


        }


        #endregion

        #region methods

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 0)
                return;

            GumpReturnType current = m_GumpReturnType = GumpReturnType.None;
            //First bubble response to the current page as it takes priority over the master
            if (CurrentPage != null)
            {
                current = CurrentPage.OnResponse(sender, info);
                if (current != GumpReturnType.None) m_GumpReturnType = current;
            }
            //Bubble to master page
            if (MasterPage != null)
            {
                current = MasterPage.OnResponse(sender, info);
                if (current != GumpReturnType.None) m_GumpReturnType = current;
            }

            //On a cancel simply return!
            if (m_GumpReturnType == GumpReturnType.Cancel)
                return;

            //On a OK, instantiate an instance of each entity in the register with the session data and call commit
            if (m_GumpReturnType == GumpReturnType.OK)
            {
                //Commit											
                foreach (KeyValuePair<int, Type> kvp in m_EntityRegister)
                {
                    try
                    {
                        ICommitGumpEntity entity = Activator.CreateInstance(kvp.Value, this) as ICommitGumpEntity;
                        if (entity != null)
                        {
                            if (entity.Validate())
                            {
                                entity.CommitChanges();
                            }
                        }
                    }
                    catch
                    {
                        //TODO: announce fail
                    }

                }
            }
            // None is a continuation - save the current page's state and continue
            else if (m_GumpReturnType == GumpReturnType.None)
            {
                if (CurrentPage != null)
                    CurrentPage.SaveStateInfo();

                //Continue by creating a new gump of whatever this type is with reflection
                CommitGumpBase gump = null;
                try
                {
                    gump = Activator.CreateInstance(this.GetType(), m_Page, m_GumpSession, m_From) as CommitGumpBase;
                }
                catch
                {
                }

                if (gump != null) sender.Mobile.SendGump(gump);

            }

            return;


        }

        #endregion

        #region abstract methods

        protected abstract void RegisterEntities();

        protected abstract void SetCurrentPage();

        #endregion
    }
}