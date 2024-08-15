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

/* Scripts\Engines\Township\AddFundsTarget.cs
 * CHANGELOG:
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Gumps;
using Server.Items;
using Server.Targeting;
using System;

namespace Server.Township
{
    public class AddFundsTarget : Target
    {
        public static void BeginAddFunds(TownshipStone stone, Mobile from)
        {
            from.SendMessage("Target a gold pile or a bank check to add to the township's funds.");
            from.Target = new AddFundsTarget(stone);
        }

        private TownshipStone m_Stone;

        private AddFundsTarget(TownshipStone stone)
            : base(2, false, TargetFlags.None)
        {
            CheckLOS = true;
            m_Stone = stone;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Ally))
                return;

            CheckDeposit(from, targeted);

            from.Target = new AddFundsTarget(m_Stone);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Ally))
                return;

            from.CloseGump(typeof(TownshipGump));
            from.SendGump(new TownshipGump(m_Stone, from));
        }

        public enum DepositResult
        {
            Success,
            Invalid,
            Full,
            StolenCheck,
            Unemployed,
            CheckTooValuable,
        }

        private void CheckDeposit(Mobile from, object targeted)
        {
            int deposited = 0;

            DepositResult result = DepositInternal(from, targeted, ref deposited);

            if (result != DepositResult.Success)
            {
                from.SendMessage(GetMessage(result));
                return;
            }

            Engines.DataRecorder.DataRecorder.GoldSink(from, m_Stone, deposited);

            from.SendMessage("You have deposited {0} gp into your township's fund.", deposited.ToString("N0"));
        }

        private DepositResult DepositInternal(Mobile from, object targeted, ref int deposited)
        {
            if (targeted is Gold)
            {
                return DepositGold(from, (Gold)targeted, ref deposited);
            }
            else if (targeted is UnemploymentCheck)
            {
                UnemploymentCheck check = (UnemploymentCheck)targeted;

                if (check.OwnerSerial != from.Serial)
                    return DepositResult.StolenCheck;
                else
                    return DepositResult.Unemployed;
            }
            else if (targeted is BankCheck)
            {
                return DepositCheck(from, (BankCheck)targeted, ref deposited);
            }
            else
            {
                return DepositResult.Invalid;
            }
        }

        private DepositResult DepositGold(Mobile from, Item gold, ref int deposited)
        {
            if (!gold.Movable || gold.Amount <= 0)
                return DepositResult.Invalid;

            int toAdd = Math.Min(gold.Amount, TownshipStone.MAX_GOLD_HELD - m_Stone.GoldHeld);

            if (toAdd <= 0)
                return DepositResult.Full;

            gold.Consume(toAdd);
            m_Stone.GoldHeld += toAdd;

            deposited += toAdd;

            m_Stone.RecordDeposit(deposited, string.Format("{0} deposited a gold pile", from.Name));

            return DepositResult.Success;
        }

        private DepositResult DepositCheck(Mobile from, BankCheck check, ref int deposited)
        {
            if (!check.Movable || check.Worth <= 0)
                return DepositResult.Invalid;

            int toAdd = check.Worth;

            if (m_Stone.GoldHeld + toAdd > TownshipStone.MAX_GOLD_HELD)
            {
                if (m_Stone.GoldHeld >= TownshipStone.MAX_GOLD_HELD)
                    return DepositResult.Full;
                else
                    return DepositResult.CheckTooValuable;
            }

            check.Delete();
            m_Stone.GoldHeld += toAdd;

            deposited += toAdd;

            m_Stone.RecordDeposit(deposited, string.Format("{0} deposited a bank check", from.Name));

            return DepositResult.Success;
        }

        private static string GetMessage(DepositResult result)
        {
            switch (result)
            {
                case DepositResult.Invalid:
                    return "You can't add that to the township's funds!";
                case DepositResult.Full:
                    return "You can't add any more gold to your township's funds.";
                case DepositResult.StolenCheck:
                    return "That check isn't yours!";
                case DepositResult.Unemployed:
                    return "If you are unemployed, you really shouldn't be investing that into the township...";
                case DepositResult.CheckTooValuable:
                    return "That check contains more than the township's fund can hold.";
            }

            return null;
        }
    }
}