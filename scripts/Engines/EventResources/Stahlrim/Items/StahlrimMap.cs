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

/* Server/Engines/EventResources/Stahlrim/Items/StahlrimMap.cs
 * CHANGELOG:
 *  10/18/23, Yoar
 *      Initial version.
 */

using Server.Engines.Harvest;

namespace Server.Items
{
    public class StahlrimMap : ResourceMap
    {
        public override HarvestDefinition HarvestDefinition { get { return Mining.System.OreAndStone; } }

        [Constructable]
        public StahlrimMap()
            : this(50)
        {
        }

        [Constructable]
        public StahlrimMap(int uses)
            : base(CraftResource.Stahlrim, uses, Map.Felucca)
        {
        }

        protected override Point2D GetRandomLocation()
        {
            if (m_Locations.Length == 0)
                return Point2D.Zero;

            Point2D loc = m_Locations[Utility.Random(m_Locations.Length)];

            return new Point2D(loc.X / 8, loc.Y / 8);
        }

        public StahlrimMap(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        private static readonly Point2D[] m_Locations = new Point2D[]
            {
                #region Dagger Island
                new Point2D(3936, 256),
                new Point2D(3944, 256),
                new Point2D(3952, 256),
                new Point2D(3960, 256),
                new Point2D(3968, 256),
                new Point2D(3976, 256),
                new Point2D(3936, 264),
                new Point2D(3976, 264),
                new Point2D(3984, 264),
                new Point2D(3992, 264),
                new Point2D(4000, 264),
                new Point2D(4008, 264),
                new Point2D(4016, 264),
                new Point2D(3936, 272),
                new Point2D(4008, 272),
                new Point2D(4016, 272),
                new Point2D(3936, 280),
                new Point2D(4016, 280),
                new Point2D(4024, 280),
                new Point2D(3936, 288),
                new Point2D(4024, 288),
                new Point2D(4032, 288),
                new Point2D(3936, 296),
                new Point2D(3944, 296),
                new Point2D(4008, 296),
                new Point2D(4016, 296),
                new Point2D(4032, 296),
                new Point2D(3944, 304),
                new Point2D(4000, 304),
                new Point2D(4008, 304),
                new Point2D(4016, 304),
                new Point2D(4024, 304),
                new Point2D(4032, 304),
                new Point2D(4040, 304),
                new Point2D(3944, 312),
                new Point2D(3952, 312),
                new Point2D(4008, 312),
                new Point2D(4016, 312),
                new Point2D(4024, 312),
                new Point2D(4032, 312),
                new Point2D(4040, 312),
                new Point2D(4048, 312),
                new Point2D(3952, 320),
                new Point2D(4008, 320),
                new Point2D(4016, 320),
                new Point2D(4024, 320),
                new Point2D(4032, 320),
                new Point2D(4048, 320),
                new Point2D(3952, 328),
                new Point2D(4008, 328),
                new Point2D(4016, 328),
                new Point2D(4024, 328),
                new Point2D(4032, 328),
                new Point2D(4040, 328),
                new Point2D(4048, 328),
                new Point2D(4056, 328),
                new Point2D(3952, 336),
                new Point2D(4008, 336),
                new Point2D(4016, 336),
                new Point2D(4024, 336),
                new Point2D(4032, 336),
                new Point2D(4056, 336),
                new Point2D(4064, 336),
                new Point2D(3952, 344),
                new Point2D(3960, 344),
                new Point2D(4008, 344),
                new Point2D(4016, 344),
                new Point2D(4024, 344),
                new Point2D(4064, 344),
                new Point2D(4072, 344),
                new Point2D(3960, 352),
                new Point2D(4072, 352),
                new Point2D(3960, 360),
                new Point2D(3968, 360),
                new Point2D(4072, 360),
                new Point2D(4080, 360),
                new Point2D(3968, 368),
                new Point2D(4080, 368),
                new Point2D(4088, 368),
                new Point2D(3968, 376),
                new Point2D(3976, 376),
                new Point2D(4088, 376),
                new Point2D(3976, 384),
                new Point2D(4088, 384),
                new Point2D(4096, 384),
                new Point2D(3976, 392),
                new Point2D(4088, 392),
                new Point2D(4096, 392),
                new Point2D(4104, 392),
                new Point2D(4112, 392),
                new Point2D(4120, 392),
                new Point2D(3976, 400),
                new Point2D(3984, 400),
                new Point2D(3992, 400),
                new Point2D(4120, 400),
                new Point2D(4128, 400),
                new Point2D(3968, 408),
                new Point2D(3976, 408),
                new Point2D(3984, 408),
                new Point2D(3992, 408),
                new Point2D(4128, 408),
                new Point2D(4136, 408),
                new Point2D(3952, 416),
                new Point2D(3960, 416),
                new Point2D(3968, 416),
                new Point2D(4136, 416),
                new Point2D(4144, 416),
                new Point2D(3936, 424),
                new Point2D(3944, 424),
                new Point2D(3952, 424),
                new Point2D(4016, 424),
                new Point2D(4024, 424),
                new Point2D(4032, 424),
                new Point2D(4040, 424),
                new Point2D(4048, 424),
                new Point2D(4080, 424),
                new Point2D(4088, 424),
                new Point2D(4096, 424),
                new Point2D(4104, 424),
                new Point2D(4112, 424),
                new Point2D(4144, 424),
                new Point2D(4152, 424),
                new Point2D(3928, 432),
                new Point2D(3936, 432),
                new Point2D(4016, 432),
                new Point2D(4024, 432),
                new Point2D(4032, 432),
                new Point2D(4040, 432),
                new Point2D(4048, 432),
                new Point2D(4056, 432),
                new Point2D(4064, 432),
                new Point2D(4072, 432),
                new Point2D(4080, 432),
                new Point2D(4112, 432),
                new Point2D(4120, 432),
                new Point2D(4152, 432),
                new Point2D(4160, 432),
                new Point2D(3920, 440),
                new Point2D(3928, 440),
                new Point2D(4024, 440),
                new Point2D(4032, 440),
                new Point2D(4040, 440),
                new Point2D(4048, 440),
                new Point2D(4056, 440),
                new Point2D(4064, 440),
                new Point2D(4072, 440),
                new Point2D(4080, 440),
                new Point2D(4088, 440),
                new Point2D(4096, 440),
                new Point2D(4112, 440),
                new Point2D(4120, 440),
                new Point2D(4160, 440),
                new Point2D(4168, 440),
                new Point2D(3920, 448),
                new Point2D(4032, 448),
                new Point2D(4040, 448),
                new Point2D(4048, 448),
                new Point2D(4064, 448),
                new Point2D(4080, 448),
                new Point2D(4096, 448),
                new Point2D(4104, 448),
                new Point2D(4112, 448),
                new Point2D(4120, 448),
                new Point2D(4176, 448),
                new Point2D(3920, 456),
                new Point2D(4024, 456),
                new Point2D(4032, 456),
                new Point2D(4040, 456),
                new Point2D(4048, 456),
                new Point2D(4064, 456),
                new Point2D(4080, 456),
                new Point2D(4184, 456),
                new Point2D(3920, 464),
                new Point2D(3928, 464),
                new Point2D(4024, 464),
                new Point2D(4032, 464),
                new Point2D(4040, 464),
                new Point2D(4056, 464),
                new Point2D(4064, 464),
                new Point2D(4080, 464),
                new Point2D(4184, 464),
                new Point2D(4192, 464),
                new Point2D(3928, 472),
                new Point2D(3936, 472),
                new Point2D(4056, 472),
                new Point2D(4072, 472),
                new Point2D(4080, 472),
                new Point2D(4192, 472),
                new Point2D(4200, 472),
                new Point2D(3936, 480),
                new Point2D(3944, 480),
                new Point2D(3952, 480),
                new Point2D(4048, 480),
                new Point2D(4064, 480),
                new Point2D(4072, 480),
                new Point2D(4200, 480),
                new Point2D(3952, 488),
                new Point2D(3960, 488),
                new Point2D(3968, 488),
                new Point2D(3976, 488),
                new Point2D(3984, 488),
                new Point2D(3992, 488),
                new Point2D(4000, 488),
                new Point2D(4008, 488),
                new Point2D(4016, 488),
                new Point2D(4024, 488),
                new Point2D(4032, 488),
                new Point2D(4040, 488),
                new Point2D(4048, 488),
                new Point2D(4064, 488),
                new Point2D(4072, 488),
                new Point2D(4080, 488),
                new Point2D(4096, 488),
                new Point2D(4200, 488),
                new Point2D(4080, 496),
                new Point2D(4088, 496),
                new Point2D(4096, 496),
                new Point2D(4104, 496),
                new Point2D(4112, 496),
                new Point2D(4120, 496),
                new Point2D(4200, 496),
                new Point2D(4208, 496),
                new Point2D(4120, 504),
                new Point2D(4128, 504),
                new Point2D(4208, 504),
                new Point2D(4216, 504),
                new Point2D(4128, 512),
                new Point2D(4136, 512),
                new Point2D(4216, 512),
                new Point2D(4224, 512),
                new Point2D(4136, 520),
                new Point2D(4144, 520),
                new Point2D(4216, 520),
                new Point2D(4224, 520),
                new Point2D(4144, 528),
                new Point2D(4208, 528),
                new Point2D(4216, 528),
                new Point2D(4224, 528),
                new Point2D(4232, 528),
                new Point2D(4144, 536),
                new Point2D(4152, 536),
                new Point2D(4208, 536),
                new Point2D(4216, 536),
                new Point2D(4232, 536),
                new Point2D(4240, 536),
                new Point2D(4152, 544),
                new Point2D(4160, 544),
                new Point2D(4208, 544),
                new Point2D(4216, 544),
                new Point2D(4240, 544),
                new Point2D(4160, 552),
                new Point2D(4168, 552),
                new Point2D(4200, 552),
                new Point2D(4208, 552),
                new Point2D(4216, 552),
                new Point2D(4224, 552),
                new Point2D(4240, 552),
                new Point2D(4168, 560),
                new Point2D(4176, 560),
                new Point2D(4200, 560),
                new Point2D(4224, 560),
                new Point2D(4232, 560),
                new Point2D(4240, 560),
                new Point2D(4248, 560),
                new Point2D(4176, 568),
                new Point2D(4200, 568),
                new Point2D(4224, 568),
                new Point2D(4232, 568),
                new Point2D(4248, 568),
                new Point2D(4256, 568),
                new Point2D(4176, 576),
                new Point2D(4200, 576),
                new Point2D(4208, 576),
                new Point2D(4216, 576),
                new Point2D(4224, 576),
                new Point2D(4256, 576),
                new Point2D(4176, 584),
                new Point2D(4184, 584),
                new Point2D(4200, 584),
                new Point2D(4208, 584),
                new Point2D(4216, 584),
                new Point2D(4224, 584),
                new Point2D(4248, 584),
                new Point2D(4256, 584),
                new Point2D(4184, 592),
                new Point2D(4200, 592),
                new Point2D(4208, 592),
                new Point2D(4248, 592),
                new Point2D(4256, 592),
                new Point2D(4184, 600),
                new Point2D(4192, 600),
                new Point2D(4200, 600),
                new Point2D(4208, 600),
                new Point2D(4232, 600),
                new Point2D(4240, 600),
                new Point2D(4248, 600),
                new Point2D(4192, 608),
                new Point2D(4200, 608),
                new Point2D(4208, 608),
                new Point2D(4216, 608),
                new Point2D(4224, 608),
                new Point2D(4232, 608),
                new Point2D(4240, 608),
                #endregion
            };
    }
}