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

/* Scripts/Engines/TestCenter/TestCenterRegionControl.cs
 * CHANGELOG:
 *  4/16/2026, Adam
 *      added.
 */

using Server.Regions;
using System;

namespace Server.Items
{
    public class TestCenterRegionControl : CustomRegionControl//, IOracleDestination (maybe)
    {
        public static void Initialize()
        {

        }

        public override string DefaultName { get { return "a test center controller"; } }

        [Constructable]
        public TestCenterRegionControl()
            : base()
        {
            Visible = true;

            InitRegionProps();
        }
        public override CustomRegion CreateRegion(CustomRegionControl rc)
        {
            return new TestCenterRegion(rc);
        }
        public TestCenterRegionControl(Serial serial)
            : base(serial)
        {
            InitRegionProps();
        }

        private void InitRegionProps()
        {
            CustomRegion.Name = "Test Center";
            CustomRegion.TestCenter = true;
            CustomRegion.ShowEnterMessage = true;
            CustomRegion.ShowExitMessage = true;
            CustomRegion.IsGuarded = false;
            CustomRegion.CanResurrect = true;
            CustomRegion.CanUsePotions = true;
            CustomRegion.EnableStuckMenu = false;
            CustomRegion.EnableHousing = true;
            CustomRegion.LightLevel = -1;
            CustomRegion.AllowTravelSpellsInRegion = true;
            CustomRegion.NoGateInto = true;
            CustomRegion.NoRecallInto = true;
        }

        public void HandleSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!e.Handled)
            {
                // handle speech
            }
        }

        public override void Delete()
        {
            try
            {
                // special handling?
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            base.Delete();
        }

        // keep
        #region OracleDestination
        #endregion

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            int version = 1;
            writer.Write(version);                      // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();


            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
}
