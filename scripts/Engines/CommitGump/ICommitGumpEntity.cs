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

/* Scripts/Engines/CommitGump/ICommitGumpEntity.cs
 * CHANGELOG:
 *	01/10/09 - Plasma,
 *		Initial creation
 */
using Server.Gumps;

namespace Server.Engines.CommitGump
{

    public interface ICommitGumpEntity
    {
        /// <summary>
        /// Unqiue ID use as a key in the sesssion
        /// </summary>
        string ID { get; }
        //bool IsDirty { get; }

        /// <summary>
        /// Commit any outstanding changes
        /// </summary>
        void CommitChanges();

        /// <summary>
        /// Creation of the entity's graphics
        /// </summary>
        void Create();

        /// <summary>
        /// Restore data from the session
        /// </summary>
        void LoadStateInfo();

        /// <summary>
        /// Handle user response 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        CommitGumpBase.GumpReturnType OnResponse(Server.Network.NetState sender, RelayInfo info);

        /// <summary>
        /// Update the state / session with any changes in memory
        /// </summary>
        void SaveStateInfo();

        /// <summary>
        /// Validate outstanding changes
        /// </summary>
        /// <returns></returns>
        bool Validate();
    }

}