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

/* Scripts/Mobiles/Guards/PatrolGuardData.cs
 * Changelog:
 * 1/7/23, Adam (Interesting Guard Wandering)
 *      Reminiscent of OSI guards of old, guards now will walk around, seemingly going somewhere or doing something (and do something).
 */

using System.Collections.ObjectModel;

namespace Server.Mobiles
{
    public partial class PatrolGuard : WarriorGuard
    {
        public static readonly ReadOnlyCollection<string> SunTzu = new ReadOnlyCollection<string>(
          new string[] {
            "He will win who knows when to fight and when not to fight.",

            "In the midst of chaos, there is also opportunity.",

            "Victorious warriors win first and then go to war, while defeated warriors go to war first and then seek to win.",

            "If you know the enemy and know yourself, you need not fear the result of a hundred battles. If you know yourself but not the enemy, for every victory gained you will also suffer a defeat. If you know neither the enemy nor yourself, you will succumb in every battle.",

            "The greatest victory is that which requires no battle.",

            "Quickness is the essence of the war.",

            "Even the finest sword plunged into salt water will eventually rust.",

            "The art of war is of vital importance to the State. It is a matter of life and death, a road either to safety or to ruin. Hence it is a subject of inquiry which can on no account be neglected.",

            "There is no instance of a nation benefiting from prolonged warfare.",

            "There are not more than five musical notes, yet the combinations of these five give rise to more melodies than can ever be heard. There are not more than five primary colours, yet in combination they produce more hues than can ever been seen. There are not more than five cardinal tastes, yet combinations of them yield more flavours than can ever be tasted.",

            "Who wishes to fight must first count the cost.",

            "You have to believe in yourself.",

            "Build your opponent a golden bridge to retreat across.",

            "One may know how to conquer without being able to do it.",

            "What the ancients called a clever fighter is one who not only wins, but excels in winning with ease.",

            "The wise warrior avoids the battle.",

            "The whole secret lies in confusing the enemy, so that he cannot fathom our real intent.",

            "One mark of a great soldier is that he fight on his own terms or fights not at all.",

            "If the mind is willing, the flesh could go on and on without many things.",

            "He who is prudent and lies in wait for an enemy who is not, will be victorious.",

            "Anger may in time change to gladness; vexation may be succeeded by content. But a kingdom that has once been destroyed can never come again into being; nor can the dead ever be brought back to life.",

            "There are roads which must not be followed, armies which must not be attacked, towns which must not be besieged, positions which must not be contested, commands of the sovereign which must not be obeyed.",

            "Attack is the secret of defense; defense is the planning of an attack.",

            "Great results can be achieved with small forces.",

            "Opportunities multiply as they are seized.",

            "If quick, I survive. If not quick, I am lost. This is death.",

            "To secure ourselves against defeat lies in our own hands, but the opportunity of defeating the enemy is provided by the enemy himself.",

            "Bravery without forethought, causes a man to fight blindly and desperately like a mad bull. Such an opponent, must not be encountered with brute force, but may be lured into an ambush and slain.",

            "Wheels of justice grind slow but grind fine.",

            "Never venture, never win!",

            "The skillful tactician may be likened to the shuai-jan. Now the shuai-jan is a snake that is found in the Ch'ang mountains. Strike at its head, and you will be attacked by its tail; strike at its tail, and you will be attacked by its head; strike at its middle, and you will be attacked by head and tail both.",

            "It is easy to love your friend, but sometimes the hardest lesson to learn is to love your enemy.",

            "Be where your enemy is not.",

            "Who does not know the evils of war cannot appreciate its benefits.",

            "In battle, there are not more than two methods of attack--the direct and the indirect; yet these two in combination give rise to an endless series of maneuvers.",

            "Plan for what it is difficult while it is easy, do what is great while it is small.",

            "The opportunity of defeating the enemy is provided by the enemy himself.",

            "Foreknowledge cannot be gotten from ghosts and spirits, cannot be had by analogy, cannot be found out by calculation. It must be obtained from people, people who know the conditions of the enemy.",

            "If you fight with all your might, there is a chance of life; where as death is certain if you cling to your corner.",

            "Do not swallow bait offered by the enemy. Do not interfere with an army that is returning home.",

            "We cannot enter into alliances until we are acquainted with the designs of our neighbors.",

            "When the outlook is bright, bring it before their eyes; but tell them nothing when the situation is gloomy.",

            "The worst calamities that befall an army arise from hesitation.",

            "If there is disturbance in the camp, the general's authority is weak.",

            "Hence that general is skillful in attack whose opponent does not know what to defend; and he is skillful in defense whose opponent does not know what to attack.",

            "Those skilled at making the enemy move do so by creating a situation to which he must conform; they entice him with something he is certain to take, and with lures of ostensible profit they await him in strength.",

            "Energy may be likened to the bending of a crossbow; decision, to the releasing of a trigger.",

            "When your army has crossed the border, you should burn your boats and bridges, in order to make it clear to everybody that you have no hankering after home.",

            "There are five dangerous faults which may affect a general: (1) Recklessness, which leads to destruction; (2) cowardice, which leads to capture; (3) a hasty temper, which can be provoked by insults; (4) a delicacy of honor which is sensitive to shame; (5) over-solicitude for his men, which exposes him to worry and trouble.",

            "Ponder and deliberate before you make a move.",

            "Rewards for good service should not be deferred a single day.",

            "Begin by seizing something which your opponent holds dear; then he will be amenable to your will.",

            "If words of command are not clear and distinct, if orders are not thoroughly understood, then the general is to blame. But, if orders are clear and the soldiers nevertheless disobey, then it is the fault of their officers.",

            "If his forces are united, separate them.",

            "Move not unless you see an advantage; use not your troops unless there is something to be gained; fight not unless the position is critical.",

            "The general who advances without coveting fame and retreats without fearing disgrace, whose only thought is to protect his country and do good service for his sovereign, is the jewel of the kingdom.",

            "It is only the enlightened ruler and the wise general who will use the highest intelligence of the army for the purposes of spying, and thereby they achieve great results.",

            "If soldiers are punished before they have grown attached to you, they will not prove submissive;, and, unless submissive, then will be practically useless. If, when the soldiers have become attached, to you, punishments are not enforced, they will still be unless.",

            "Convince your enemy that he will gain very little by attacking you; this will diminish his enthusiasm.",

            "To fight and conquer in all our battles is not supreme excellence; supreme excellence consists in breaking the enemy's resistance without fighting.",

            "Let your plans be dark and impenetrable as night, and when you move, fall like a thunderbolt.",

            "All warfare is based on deception. Hence, when we are able to attack, we must seem unable; when using our forces, we must appear inactive; when we are near, we must make the enemy believe we are far away; when far away, we must make him believe we are near.",

            "If your opponent is temperamental, seek to irritate him. Pretend to be weak, that he may grow arrogant. If he is taking his ease, give him no rest. If his forces are united, separate them. If sovereign and subject are in accord, put division between them. Attack him where he is unprepared, appear where you are not expected.",

            "To know your enemy, you must become your enemy.",

            "Thus we may know that there are five essentials for victory: (1) He will win who knows when to fight and when not to fight; (2) he will win who knows how to handle both superior and inferior forces; (3) he will win whose army is animated by the same spirit throughout all its ranks; (4) he will win who, prepared himself, waits to take the enemy unprepared; (5) he will win who has military capacity and is not interfered with by the sovereign.",

            "Treat your men as you would your own beloved sons. And they will follow you into the deepest valley.",

            "When the enemy is relaxed, make them toil. When full, starve them. When settled, make them move.",

            "So in war, the way is to avoid what is strong, and strike at what is weak.",

            "To win one hundred victories in one hundred battles is not the acme of skill. To subdue the enemy without fighting is the acme of skill.",

            "Be extremely subtle even to the point of formlessness. Be extremely mysterious even to the point of soundlessness. Thereby you can be the director of the opponent's fate.",

            "Thus the expert in battle moves the enemy, and is not moved by him.",

            "Water shapes its course according to the nature of the ground over which it flows; the soldier works out his victory in relation to the foe whom he is facing.",

            "The supreme art of war is to subdue the enemy without fighting.",

            "Appear weak when you are strong, and strong when you are weak.",

            "When one treats people with benevolence, justice, and righteousness, and reposes confidence in them, the army will be united in mind and all will be happy to serve their leaders.",


          });
    }
}

/* Do these Shakespearean translations work?
"He wilt  winneth  who  knoweth  at which hour  to  square  and  at which hour  not  to  square. ", 

"In  the  midst  of  chaos,  thither  is  eke  opportunity. ", 

"Victorious  warriors  winneth  first  and  then  wend  to  war,  while  defeated  warriors  wend  to  war  first  and  then  seek  to  winneth. ", 

"If  thee  knoweth  the  enemy  and  knoweth  yourself,  thee  needeth  not  fear  the  result  of  a  hundred  battles.  if 't be true  thee  knoweth  yourself  but  not  the  enemy,  for  every  victory  gained  thee  wilt  eke  suffer  a  defeat.  if 't be true  thee  knoweth  neither  the  enemy  nor  yourself,  thee  wilt  succumb  in  every  battle. "
*/