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

using Server.Engines.Craft;

namespace Server.Items
{
    [FlipableAttribute(0x13E4, 0x13E3)]
    public class RunicHammer : BaseRunicTool
    {
        public override CraftSystem CraftSystem { get { return DefBlacksmithy.CraftSystem; } }

        public override int LabelNumber
        {
            get
            {
                int index = CraftResources.GetIndex(Resource);

                if (index >= 1 && index <= 8)
                    return 1049019 + index;

                return 1045128; // runic smithy hammer
            }
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            int index = CraftResources.GetIndex(Resource);

            if (index >= 1 && index <= 8)
                return;

            if (!CraftResources.IsStandard(Resource))
            {
                int num = CraftResources.GetLocalizationNumber(Resource);

                if (num > 0)
                    list.Add(num);
                else
                    list.Add(CraftResources.GetName(Resource));
            }
        }

        [Constructable]
        public RunicHammer(CraftResource resource)
            : base(resource, 0x13E4)
        {
            Weight = 8.0;
            Layer = Layer.OneHanded;
            Hue = CraftResources.GetHue(resource);
        }

        [Constructable]
        public RunicHammer(CraftResource resource, int uses)
            : base(resource, uses, 0x13E4)
        {
            Weight = 8.0;
            Layer = Layer.OneHanded;
            Hue = CraftResources.GetHue(resource);
        }

        public RunicHammer(Serial serial)
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
    }
}