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

/* Scripts/Gumps/RessurectGump.cs
 * ChangeLog
 *	07/27/08, weaver
 *		Correctly remove gumps from the NetState object on CloseGump() (integrated RunUO fix).
 *	07/26/08, weaver
 *		Updated OnResponse() code to correctly clear resurrection gumps for newer clients, preventing freezing problem.
 *	4/4/08, Adam
 *		Don't give statloss during Server Wars.
 *		Encapsulate the meaning of the variables: Inmate, ShortTermMurders, and ServerWars into the following functions:
 *		StatLossCandidate(owner), StatLossReprieve(owner)
 *  3/18/08, Adam
 *		Remove references to virtue code
 *  7/22/07, Adam
 *      Cleanup the look of the res gump so that all the text fits nicely
 *	4/4/06, Pix
 *		Protected against getting a 5th short term count while the resurrect gump is up.
 *	2/8/06, Pix
 *		Added logging of statloss taken.
 *	10/30/05, Pix
 *		Changed wording on statloss warning to specifically include the word "skill"
 *  08/28/05 Taran Kain
 *		Disallowed Mortal players from resurrecting
 *	1/21/05, Adam
 *		Add a ResurrectGump that doesn't invoke stat loss (called from ResGate)
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  1/5/05, Darva
 *		Added Statloss Warning.
 *	3/30/04, changes by mith
 *		OnResponse(): modified stat-loss check to include check of Inmate PlayerFlag.
 */


using Server.Mobiles;
using Server.Network;
using System;

namespace Server.Gumps
{
    public enum ResurrectMessage
    {
        ChaosShrine = 0,
        VirtueShrine = 1,
        Healer = 2,
        Generic = 3,
    }

    public class ResurrectGump : Gump
    {
        private Mobile m_Healer;
        private int m_Price;
        private bool m_Statloss = true;

        public ResurrectGump(Mobile owner)
            : this(owner, owner, ResurrectMessage.Generic)
        {
        }

        public ResurrectGump(Mobile owner, Mobile healer)
            : this(owner, healer, ResurrectMessage.Generic)
        {
        }

        public ResurrectGump(Mobile owner, ResurrectMessage msg)
            : this(owner, owner, msg)
        {
        }

        public ResurrectGump(Mobile owner, Mobile healer, ResurrectMessage msg)
            : base(100, 0)
        {
            m_Healer = healer;

            AddPage(0);

            AddBackground(0, 0, 400, 350, 2600);

            AddHtmlLocalized(0, 20, 400, 35, 1011022, false, false); // <center>Resurrection</center>

            #region UOAI
            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
            {
                /* It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
				 * CONTINUE - You chose to try to come back to life now.<br>
				 * CANCEL - You prefer to remain a ghost for now.
				 */
                string text = String.Format("{0}<br>",
                    "It is possible for you to be resurrected here by this healer. Do you wish to try?");

                // no statloss if an inmate or server wars are on
                if (StatLossCandidate(owner))
                    text += "<br>You will experience both skill and stat loss if you continue.";
                else if (StatLossReprieve(owner))
                    text += "<br>You will not experience stat loss if you continue.";
                else
                {
                    text += "CONTINUE - You chose to try to come back to life now.<br>";
                    text += "CANCEL - You prefer to remain a ghost for now.";
                }

                //AddHtmlLocalized( 50, 55, 300, 140, 1011023 + (int)msg, true, true ); 
                AddHtml(50, 55, 300, 140, text, true, true);

                // Warn of statloss if not an inmate and server wars are not running
                if (StatLossCandidate(owner))
                {
                    AddLabel(90, 270, 0x20, "Warning: YOU ARE IN STAT LOSS!");
                }
                else
                {
                    //Make sure that we mark no statloss - this protects vs getting a 5th count WHILE the ressurectgump is up
                    m_Statloss = false;
                }
            }
            #endregion
            else
            {
                AddHtmlLocalized(50, 55, 300, 140, 1011023 + (int)msg, true, true); /* It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
																				   * CONTINUE - You chose to try to come back to life now.<br>
																				   * CANCEL - You prefer to remain a ghost for now.
																				   */
            }

            AddButton(200, 227, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(235, 230, 110, 35, 1011012, false, false); // CANCEL

            AddButton(65, 227, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(100, 230, 110, 35, 1011011, false, false); // CONTINUE
        }

        // called from ResGate (no statloss)
        public ResurrectGump(Mobile owner, bool Statloss)
            : base(100, 0)
        {
            m_Healer = owner;
            m_Statloss = Statloss;

            AddPage(0);

            AddBackground(0, 0, 400, 350, 2600);

            AddHtmlLocalized(0, 20, 400, 35, 1011022, false, false); // <center>Resurrection</center>

            // It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
            // CONTINUE - You chose to try to come back to life now.<br>
            // CANCEL - You prefer to remain a ghost for now.
            //
            AddHtmlLocalized(50, 55, 300, 140, 1011023 + (int)ResurrectMessage.Generic, true, true);

            // Notice of no statloss
            if (StatLossCandidate(owner))
                AddLabel(32, 285, 52, "Note: You will not experience stat loss if you continue.");

            AddButton(200, 227, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(235, 230, 110, 35, 1011012, false, false); // CANCEL

            AddButton(65, 227, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(100, 230, 110, 35, 1011011, false, false); // CONTINUE
        }

        public ResurrectGump(Mobile owner, Mobile healer, int price)
            : base(150, 50)
        {
            m_Healer = healer;
            m_Price = price;

            Closable = false;

            AddPage(0);

            AddImage(0, 0, 3600);

            AddImageTiled(0, 14, 15, 200, 3603);
            AddImageTiled(380, 14, 14, 200, 3605);

            AddImage(0, 201, 3606);

            AddImageTiled(15, 201, 370, 16, 3607);
            AddImageTiled(15, 0, 370, 16, 3601);

            AddImage(380, 0, 3602);

            AddImage(380, 201, 3608);

            AddImageTiled(15, 15, 365, 190, 2624);

            AddRadio(30, 140, 9727, 9730, true, 1);
            AddHtmlLocalized(65, 145, 300, 25, 1060015, 0x7FFF, false, false); // Grudgingly pay the money

            AddRadio(30, 175, 9727, 9730, false, 0);
            AddHtmlLocalized(65, 178, 300, 25, 1060016, 0x7FFF, false, false); // I'd rather stay dead, you scoundrel!!!

            AddHtmlLocalized(30, 20, 360, 35, 1060017, 0x7FFF, false, false); // Wishing to rejoin the living, are you?  I can restore your body... for a price of course...

            AddHtmlLocalized(30, 105, 345, 40, 1060018, 0x5B2D, false, false); // Do you accept the fee, which will be withdrawn from your bank?

            AddImage(65, 72, 5605);

            AddImageTiled(80, 90, 200, 1, 9107);
            AddImageTiled(95, 92, 200, 1, 9157);

            AddLabel(90, 70, 1645, price.ToString());
            AddHtmlLocalized(140, 70, 100, 25, 1023823, 0x7FFF, false, false); // gold coins

            AddButton(290, 175, 247, 248, 2, GumpButtonType.Reply, 0);

            AddImageTiled(15, 14, 365, 1, 9107);
            AddImageTiled(380, 14, 1, 190, 9105);
            AddImageTiled(15, 205, 365, 1, 9107);
            AddImageTiled(15, 14, 1, 190, 9105);
            AddImageTiled(0, 0, 395, 1, 9157);
            AddImageTiled(394, 0, 1, 217, 9155);
            AddImageTiled(0, 216, 395, 1, 9157);
            AddImageTiled(0, 0, 1, 217, 9155);
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            // wea: Correctly clear gumps for newer clients.
            from.CloseGumps(typeof(ResurrectGump));

            if (info.ButtonID == 1 || info.ButtonID == 2)
            {
                if (from is PlayerMobile && ((PlayerMobile)from).Mortal && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Thy soul was too closely intertwined with thy flesh - thou'rt unable to incorporate a new body.");
                    return;
                }

                if (from.Map == null || !Utility.CanFit(from.Map, from.Location, 16, Utility.CanFitFlags.requireSurface))
                {
                    from.SendLocalizedMessage(502391); // Thou can not be resurrected there!
                    return;
                }

                if (m_Price > 0)
                {
                    if (info.IsSwitched(1))
                    {
                        if (Banker.CombinedWithdrawFromAllEnrolled(from, m_Price))
                        {
                            from.SendLocalizedMessage(1060398, m_Price.ToString()); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                            from.SendLocalizedMessage(1060022, Banker.GetAccessibleBalance(from).ToString()); // You have ~1_AMOUNT~ gold in cash remaining in your bank box.
                        }
                        else
                        {
                            from.SendLocalizedMessage(1060020); // Unfortunately, you do not have enough cash in your bank to cover the cost of the healing.
                            return;
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(1060019); // You decide against paying the healer, and thus remain dead.
                        return;
                    }
                }

                from.PlaySound(0x214);
                from.FixedEffect(0x376A, 10, 16);

                from.Resurrect();

                // adam: 3/18/08 - virtues are obsolete
                /*if ( m_Healer != null && from != m_Healer )
				{
					VirtueLevel level = VirtueHelper.GetLevel( m_Healer, VirtueName.Compassion );

					switch ( level )
					{
						case VirtueLevel.Seeker: from.Hits = AOS.Scale( from.HitsMax, 20 ); break;
						case VirtueLevel.Follower: from.Hits = AOS.Scale( from.HitsMax, 40 ); break;
						case VirtueLevel.Knight: from.Hits = AOS.Scale( from.HitsMax, 80 ); break;
					}
				}*/

                Mobile m = from;

                Misc.Titles.AwardFame(from, -100, true); // TODO: Proper fame loss

                PlayerMobile pm = from as PlayerMobile;
                // If the player is an inmate of AI, we don't want them taking stat-loss
                // Adam: we now have gumps that don't invoke stat loss (m_Statloss)
                if (!Core.RuleSets.AOSRules() && StatLossCandidate(from) && m_Statloss == true)
                {
                    double loss = (100.0 - (4.0 + (from.ShortTermMurders / 5.0))) / 100.0;//5 to 15% loss
                    if (loss < 0.85)
                        loss = 0.85;
                    else if (loss > 0.95)
                        loss = 0.95;

                    if (from.RawStr * loss > 10)
                        from.RawStr = (int)(from.RawStr * loss);
                    if (from.RawInt * loss > 10)
                        from.RawInt = (int)(from.RawInt * loss);
                    if (from.RawDex * loss > 10)
                        from.RawDex = (int)(from.RawDex * loss);

                    for (int s = 0; s < from.Skills.Length; s++)
                    {
                        if (from.Skills[s].Base * loss > 35)
                            from.Skills[s].Base *= loss;
                    }

                    //Pix: LOG it!!
                    try
                    {
                        Server.Diagnostics.LogHelper lh = new Server.Diagnostics.LogHelper("statloss.log");
                        lh.Log(Server.Diagnostics.LogType.Mobile, pm, "Statloss was " + loss.ToString());
                        lh.Finish();
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                }
            }
        }

        private bool StatLossCandidate(Mobile owner)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                return false;
            }

            if (owner == null) return false;
            return (owner.ShortTermMurders >= 5 && ((PlayerMobile)owner).PrisonInmate == false && Server.Misc.AutoRestart.ServerWars == false);
        }

        private bool StatLossReprieve(Mobile owner)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                return false;
            }

            if (owner == null) return false;
            return (owner.ShortTermMurders >= 5 && (((PlayerMobile)owner).PrisonInmate == true || Server.Misc.AutoRestart.ServerWars == true));
        }

    }
}