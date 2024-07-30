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

/* Scripts\Engines\Township\DeleteTownshipPrompt.cs
 * CHANGELOG:
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Prompts;
using System;

namespace Server.Township
{
    public class DeleteTownshipPrompt : Prompt
    {
        public static void BeginDeleteTownship(TownshipStone stone, Mobile from)
        {
            from.SendMessage("WARNING! You are about to delete your township. Type \"Delete my township\" to confirm. Press Escape to cancel.");
            from.Prompt = new DeleteTownshipPrompt(stone);
        }

        private TownshipStone m_Stone;

        private DeleteTownshipPrompt(TownshipStone stone)
            : base()
        {
            m_Stone = stone;
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Leader))
                return;

            if (!Insensitive.Equals(text.Trim(), "delete my township"))
            {
                Cancel(from);
                return;
            }

            LogHelper Logger = new LogHelper("township.log", false, true);
            Logger.Log(LogType.Item, m_Stone, String.Format("TownshipStone {0} of [{1}] has been deleted by {2}. Daily fees: {3} Funds: {4}.",
                m_Stone, m_Stone.GuildAbbreviation, from, m_Stone.TotalFeePerDay, m_Stone.GoldHeld));
            Logger.Finish();

            m_Stone.Delete();

            from.SendMessage("Your township has been deleted.");
        }

        public override void OnCancel(Mobile from)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Leader))
                return;

            Cancel(from);
        }

        private void Cancel(Mobile from)
        {
            from.SendMessage("Canceled township deletion.");

            from.CloseGump(typeof(TownshipGump));
            from.SendGump(new TownshipGump(m_Stone, from, TownshipGump.Page.DeleteTownship));
        }
    }
    public class PackUpTownshipPrompt : Prompt
    {
        public static void BeginPackUpTownship(TownshipStone stone, Mobile from)
        {
            from.SendMessage("WARNING! You are about to pack up your township. Type \"pack up my township\" to confirm. Press Escape to cancel.");
            from.Prompt = new PackUpTownshipPrompt(stone);
        }

        private TownshipStone m_Stone;

        private PackUpTownshipPrompt(TownshipStone stone)
            : base()
        {
            m_Stone = stone;
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Leader))
                return;

            if (!Insensitive.Equals(text.Trim(), "pack up my township"))
            {
                Cancel(from);
                return;
            }

            LogHelper Logger = new LogHelper("township.log", false, true);
            Logger.Log(LogType.Item, m_Stone, String.Format("TownshipStone {0} of [{1}] has been packed up by {2}. Daily fees: {3} Funds: {4}.",
                m_Stone, m_Stone.GuildAbbreviation, from, m_Stone.TotalFeePerDay, m_Stone.GoldHeld));
            Logger.Finish();

            if (Core.RuleSets.PackUpStructureRules())
            {
                LogHelper logger = new LogHelper("Pack up township.log", overwrite: false, sline: true);
                try
                {
                    if (m_Stone.IsPackedUp())
                    {
                        from.SendMessage("You will need to finish unpacking before you can pack it up again.");
                        return;
                    }
                    else if (!m_Stone.PackUpTownship(from, logger))
                        return;
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
                finally
                {
                    logger.Finish();
                }
                from.SendMessage("Your township has been packed up.");
            }
            else
                from.SendMessage("Currently unavailable on production shards.");
        }

        public override void OnCancel(Mobile from)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Leader))
                return;

            Cancel(from);
        }

        private void Cancel(Mobile from)
        {
            from.SendMessage("Canceled packing up of township.");

            from.CloseGump(typeof(TownshipGump));
            from.SendGump(new TownshipGump(m_Stone, from, TownshipGump.Page.PackUpTownship));
        }
    }
}