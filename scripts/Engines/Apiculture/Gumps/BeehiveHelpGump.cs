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

/* scripts\Engines\Apiculture\Gumps\BeehiveHelpGump.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Gumps;

namespace Server.Engines.Apiculture
{
    public class BeehiveHelpGump : BaseApicultureGump
    {
        public BeehiveHelpGump(int index)
            : base(20, 20)
        {
            AddBackground(50, 50, 400, 350, 0xE10);

            AddItem(45, 50, 0xCEF);
            AddItem(47, 130, 0xCEF);
            AddItem(45, 215, 0xCEF);
            AddItem(43, 300, 0xCEF);
            AddItem(415, 50, 0xCEB);
            AddItem(417, 130, 0xCEB);
            AddItem(415, 215, 0xCEB);
            AddItem(413, 300, 0xCEB);

            AddLabelCentered(100, 65, 300, 92, "Apiculture Help");

            if (index >= 0 && index < m_Entries.Length)
                AddHtml(70, 85, 360, 262, m_Entries[index], true, true);

            AddButton(218, 355, 0xF7, 0xF8, 0, GumpButtonType.Reply, 0); // Okay
        }

        private static readonly string[] m_Entries = new string[]
            {
@"<b>Apiculture</b> is the science (and some say art) of raising honey bees, also know as <b>beekeeping</b>. Bees live together in groups called <b>colonies</b> and make their homes in <b>beehives</b>. Tending a hive is not as easy as it may sound, although it can be a very rewarding experience. To start on the path of the <b>apiculturist</b>, all one needs is a <b>beehive deed</b> and an area with plenty of <b>flowers</b> and <b>water</b>.

Managing and caring for the hive is done using the <b>Beehive gump</b>. Almost every aspect of the hive can be monitored from here.

In the top-center of the beehive gump, the <b>development stage</b> of the hive is displayed. There are three distinct stages in a beehive's development:
<b>Colonizing</b>: The hive sends out scouts to survey the area and find sources of flowers and water.
<b>Brooding</b>: Egg laying begins in full force as the hive gets ready to begin full scale production.
<b>Producing</b>: After a hive reaches maturity, it begins producing excess amounts of honey and wax.

In the bottom-center of the beehive gump, the <b>over all health</b> of the hive is displayed. The over all health offers an indication of the average bee's well being:
<b>Thriving</b>: The bees are extremely healthy. A thriving colony produces honey and wax at an increased rate.
<b>Healthy</b>: The bees are healthy and producing excess honey and wax.
<b>Sickly</b>: The bees are sickly and no longer producing excess resources.
<b>Dying</b>: If something isn't done quickly, bee population will begin to drop.

Down the left side of the beehive gump are the status icons:
<b>Production</b>: This button brings up the <b>production gump</b> where the beekeeper can harvest any beeswax or honey produced by the hive. To harvest prduce from the hive, you require a <b>hive tool</b>. Hive tools can be bought from beekeeper NPCs.
<b>Infestation</b>: This icon provides an indication of the infestation level of the hive. A red or yellow hyphen means the hive is infested by parasites or other insects. Use <b>poison potions</b> to kill the pests.
<b>Disease</b>: This icon provides an indication of the disease level of the hive. A red or yellow hyphen means the hive is currently diseased. Use <b>cure potions</b> to help the bees fight off the sickness.
<b>Water</b>: This icon provides an indication of the availability of water in the area. A red or yellow hyphen means the hive has insufficient water sources nearby. Be warned, water breeds disease carrying bacteria, so too much water can make a hive more susceptible to disease.
<b>Flowers</b>: This icon provides an indication of the availability of flowers in the area. Bees use flowers and their by-products for almost every function of the hive including building and food. A red or yellow hyphen means the bees can't find enough flowers nearby. Be warned, too many flowers in the area can bring the bees into contact with more parasites and insects.

Note: If there are other beehives close by, the hives must <b>compete</b> for the resources, leading to less available resources for all hives.

Down the right side of the beehive gump are the potion icons:
<b>Cure</b>: Cure potions can be used to combat diseases such as foulbrood and dysentery. These potions can also be used to neutralize excess poison. Only greater cure potions may be used.
<b>Poison</b>: Poison potions can be used to combat insects or parasites that infest the hive. Care must be used! Too many poison potions can harm the bees. Either greater poison or deadly poison potions may be used.
<b>Strength</b>: Strength potions can be used to build up a hive's immunity to infestation and disease. Only greater strength potions may be used.
<b>Heal potions</b>: Heal potions can be used to heal the bees. Only greater heal potions may be used.
<b>Agility</b>: Agility potions give the bees extra energy allowing them to work harder. This will boost honey and wax output as well as increase the range in which the bees can search for flowers and water. Only greater agility potions may be used.

Once your hive has reached the <b>producing</b> stage, the population of your hive may grow and will also be displayed in the beehive gump. <b>Bee population</b> is a rough estimate of the number of bees in a hive. A single bee hive can support up to 100 thousand bees. More bees does not always mean better - a large hive is more difficult to maintain. More water and flowers are needed in the area to support a large hive. However, an increase in population will also increase the range in which the bees can search for flowers and water. If the conditions get bad enough, a colony of bees will <b>abscond</b>, leaving an empty hive behind. A single bee hive can support up to 100 thousand bees.

A hive's <b>growth check</b> is performed once a day. The upper right hand corner of the <b>Apiculture gump</b> displays the results of the last growth check:
<basefont color=#FF0000><b>!</b><basefont color=#000000>: Not healthy;
<basefont color=#FFFF00><b>!</b><basefont color=#000000>: Low resources;
<basefont color=#FF0000><b>-</b><basefont color=#000000>: Population decrease;
<basefont color=#00FF00><b>+</b><basefont color=#000000>: Population growth;
<basefont color=#0000FF><b>+</b><basefont color=#000000>: Stage increase/Resource production.

A healthy hive can live indefinitely, however, an older hive is more susceptible to infestation and disease."
            };
    }
}