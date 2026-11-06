using Server;
using Server.Diagnostics;
using Server.Engines;
using Server.Items;
using Server.Mobiles;
using System;

public static class BountyUtility
{
    /// <summary>
    /// Creates and registers a bounty funded by the target's bank (if any),
    /// capped by CoreAI.PJUMBountyCap. Optionally links a Town Crier entry id
    /// so the announcement can be updated when the bounty is collected.
    ///
    /// Returns the created Bounty or null if target is invalid.
    /// </summary>
    public static Bounty? CreateAndRegisterBounty(Mobile? reporter, Mobile target, double? townCrierEntryId)
    {
        if (target is not PlayerMobile pm)
            return null;

        // reporter may be null (allowed)
        PlayerMobile? pmReporter = reporter as PlayerMobile;

        string issuerName = GenerateOfficerName();

        int bountyAmount = WithdrawRandomBountyFromBank(pm, CoreAI.PJUMBountyCap);

        Bounty bounty = new Bounty(pmReporter, pm, bountyAmount, issuerName);

        // Bounty ctor should not return null, but keep the original pattern.
        if (bounty != null && townCrierEntryId.HasValue)
            bounty.TownCrierEntryID = townCrierEntryId.Value;

        if (bounty != null)
            BountyKeeper.Add(bounty);

        // Audit log: automated bounty creation + proof of bank gold consumption
        LogHelper logger = new LogHelper("BountySystem.log", false, true);

        string targetText = logger.Format(LogType.Mobile, pm);

        string reporterText = pmReporter != null
            ? logger.Format(LogType.Mobile, pmReporter)
            : "SYSTEM";

        logger.Log(
            LogType.Text,
            string.Format(
                "{0} placed an automated bounty of {1} gold on ({2}) via officer \"{3}\". Consumed {1} gold from bank container (0x{4:X8}).",
                reporterText,
                bountyAmount,
                targetText,
                issuerName,
                (int)pm.BankBox.Serial
            )
        );
        logger.Finish();

        return bounty;
    }

    /// <summary>
    /// Builds a display name like "Officer John" using NameList.
    /// </summary>
    private static string GenerateOfficerName()
    {
        string baseName = Utility.RandomBool()
            ? NameList.RandomName("male")
            : NameList.RandomName("female");

        return string.Format("Officer {0}", baseName);
    }

    /// <summary>
    /// Withdraws a random amount between half and all of the target's bank gold,
    /// capped. Returns the amount actually withdrawn (0 if none).
    /// </summary>
    private static int WithdrawRandomBountyFromBank(PlayerMobile target, int cap)
    {
        if (target == null)
            return 0;

        Container bank = target.BankBox;
        if (bank == null)
            return 0;

        int wealth = CountGold(bank); // gold + bank checks
        if (wealth <= 0)
            return 0;

        // Tunables
        const int MinFine = 1000;      // floor so it always "stings"
        const int WealthForMax = 250000; // wealth at which cap is reached

        int amount = ComputeScaledFine(
            wealth,
            minFine: MinFine,
            maxFine: cap,
            w0: WealthForMax
        );

        if (amount <= 0)
            return 0;

        return ConsumeTotalGoldFirst(bank, amount) ? amount : 0;
    }

    private static int ComputeScaledFine(int wealth, int minFine, int maxFine, int w0)
    {
        if (wealth <= 0)
            return 0;

        if (minFine < 0) minFine = 0;
        if (maxFine < minFine) maxFine = minFine;
        if (w0 <= 0) w0 = 1;

        double t = (double)wealth / (double)w0;
        if (t < 0) t = 0;
        if (t > 1) t = 1;

        double scaled = minFine + (maxFine - minFine) * Math.Sqrt(t);

        // Small jitter +/- 10% so it isnt perfectly predictable
        int jitterPct = Utility.RandomMinMax(90, 110);
        scaled = scaled * jitterPct / 100.0;

        int fine = (int)Math.Round(scaled);

        if (fine < minFine) fine = minFine;
        if (fine > maxFine) fine = maxFine;
        if (fine > wealth) fine = wealth; // never attempt more than liquid wealth

        return fine;
    }

    // Replace:
    // return bank.ConsumeTotal(typeof(Gold), amount) ? amount : 0;
    //
    // With:
    // return ConsumeTotalGoldFirst(bank, amount) ? amount : 0;
    private static bool ConsumeTotalGoldFirst(Container bank, int amount)
    {
        if (bank == null || amount <= 0)
            return false;

        // Transactional guard: if we cannot cover the amount with gold+checks, do nothing.
        int available = CountGold(bank); // your updated CountGold: gold + bankcheck.Worth
        if (available < amount)
            return false;

        int remaining = amount;

        // 1) Consume loose gold first (deep)
        Item[] golds = bank.FindItemsByType(typeof(Gold), true);
        for (int i = 0; i < golds.Length && remaining > 0; i++)
        {
            Gold? g = golds[i] as Gold;
            if (g == null || g.Deleted)
                continue;

            int take = g.Amount;
            if (take <= 0)
                continue;

            if (take > remaining)
                take = remaining;

            int newAmt = g.Amount - take;
            if (newAmt <= 0)
                g.Delete();
            else
                g.Amount = newAmt;

            remaining -= take;
        }

        // 2) Then consume bank checks by Worth (deep)
        if (remaining > 0)
        {
            Item[] checks = bank.FindItemsByType(typeof(BankCheck), true);
            for (int i = 0; i < checks.Length && remaining > 0; i++)
            {
                BankCheck? bc = checks[i] as BankCheck;
                if (bc == null || bc.Deleted)
                    continue;

                int worth = bc.Worth;
                if (worth <= 0)
                    continue;

                int take = worth;
                if (take > remaining)
                    take = remaining;

                int newWorth = worth - take;
                if (newWorth <= 0)
                    bc.Delete();
                else
                    bc.Worth = newWorth;

                remaining -= take;
            }
        }

        // With the available check above, remaining should always hit 0.
        return (remaining <= 0);
    }

    private static int CountGold(Container cont)
    {
        if (cont == null)
            return 0;

        int total = 0;

        // Count loose gold
        Item[] golds = cont.FindItemsByType(typeof(Gold), true);
        for (int i = 0; i < golds.Length; i++)
        {
            if (golds[i] is Gold g)
                total += g.Amount;
        }

        // Count bank checks (by Worth)
        Item[] checks = cont.FindItemsByType(typeof(BankCheck), true);
        for (int i = 0; i < checks.Length; i++)
        {
            if (checks[i] is BankCheck bc)
                total += bc.Worth;
        }

        return total;
    }

}
