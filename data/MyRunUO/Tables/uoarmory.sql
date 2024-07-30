-- phpMyAdmin SQL Dump
-- version 3.3.6
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Nov 29, 2010 at 08:39 PM
-- Server version: 5.1.50
-- PHP Version: 5.2.14

SET SQL_MODE="NO_AUTO_VALUE_ON_ZERO";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `uosp_myrunuo`
--

-- --------------------------------------------------------

--
-- Table structure for table `myrunuo_characters`
--

CREATE TABLE IF NOT EXISTS `myrunuo_characters` (
  `char_id` int(12) unsigned NOT NULL,
  `char_name` varchar(150) DEFAULT NULL,
  `char_str` int(3) unsigned DEFAULT NULL,
  `char_dex` int(3) unsigned DEFAULT NULL,
  `char_int` int(3) unsigned DEFAULT NULL,
  `char_female` int(2) unsigned DEFAULT NULL,
  `char_counts` int(3) unsigned DEFAULT NULL,
  `char_guild` varchar(4) DEFAULT NULL,
  `char_guildtitle` varchar(150) DEFAULT NULL,
  `char_nototitle` varchar(150) DEFAULT NULL,
  `char_bodyhue` int(3) unsigned DEFAULT NULL,
  `char_public` int(1) unsigned DEFAULT NULL,
  `char_maxhits` int(3) unsigned DEFAULT NULL,
  `char_weight` int(4) unsigned DEFAULT NULL,
  `char_gold` int(12) unsigned DEFAULT NULL,
  `char_armor_rating` int(3) DEFAULT NULL,
  PRIMARY KEY (`char_id`),
  KEY `char_guild` (`char_guild`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

--
-- Dumping data for table `myrunuo_characters`
--

INSERT INTO `myrunuo_characters` (`char_id`, `char_name`, `char_str`, `char_dex`, `char_int`, `char_female`, `char_counts`, `char_guild`, `char_guildtitle`, `char_nototitle`, `char_bodyhue`, `char_public`, `char_maxhits`, `char_weight`, `char_gold`, `char_armor_rating`) VALUES
(2422, 'myfirstguildguy', 60, 10, 10, 0, 0, '1', 'smerX', 'myfirstguildguy', 33770, 0, 80, 186, 1000, NULL),
(2421, 'playername', 60, 10, 10, 0, 0, NULL, 'NULL', 'playername', 33770, 0, 80, 64, 1000, NULL),
(1, 'RunUO', 25, 10, 45, 0, 0, NULL, 'NULL', 'RunUO', 33777, 0, 62, 82, 1000, NULL),
(2423, 'Myrunuo test', 95, 95, 35, 0, 0, NULL, 'NULL', 'The Glorious Lord Myrunuo test, Grandmaster Healer', 33770, 1, 97, 97, 1000, NULL),
(2420, 'Lotsofskills', 60, 10, 10, 0, 0, NULL, 'NULL', 'Lotsofskills', 33770, 0, 80, 65, 1000, NULL),
(2425, 'a one', 60, 10, 10, 0, 0, NULL, 'NULL', 'a one', 33770, 0, 80, 62, 1000, NULL),
(2426, 'q one', 60, 10, 10, 0, 0, NULL, 'NULL', 'q one', 33770, 0, 80, 58, 1000, NULL);

-- --------------------------------------------------------

--
-- Table structure for table `myrunuo_characters_layers`
--

CREATE TABLE IF NOT EXISTS `myrunuo_characters_layers` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `char_id` int(12) unsigned DEFAULT NULL,
  `layer_id` int(10) unsigned NOT NULL,
  `item_id` int(12) unsigned DEFAULT NULL,
  `item_hue` int(3) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `charid` (`char_id`)
) ENGINE=MyISAM  DEFAULT CHARSET=latin1 AUTO_INCREMENT=603 ;

--
-- Dumping data for table `myrunuo_characters_layers`
--

INSERT INTO `myrunuo_characters_layers` (`id`, `char_id`, `layer_id`, `item_id`, `item_hue`) VALUES
(585, 2423, 11, 5911, 0),
(584, 2423, 10, 7107, 0),
(583, 2423, 9, 3934, 0),
(582, 2423, 8, 5077, 1436),
(581, 2423, 7, 5435, 0),
(580, 2423, 6, 5078, 1436),
(579, 2423, 5, 8059, 0),
(578, 2423, 4, 5083, 1436),
(577, 2423, 3, 5084, 1436),
(576, 2423, 2, 5899, 0),
(575, 2423, 1, 5082, 1436),
(591, 2420, 5, 10127, 0),
(574, 2423, 0, 4234, 0),
(573, 2422, 4, 8251, 1102),
(572, 2422, 3, 8271, 0),
(571, 2422, 2, 5903, 1730),
(570, 2422, 1, 5422, 203),
(569, 2422, 0, 7933, 162),
(568, 2421, 5, 8251, 1102),
(567, 2421, 4, 5042, 0),
(566, 2421, 3, 7939, 1221),
(565, 2421, 2, 8059, 155),
(564, 2421, 1, 5903, 1712),
(563, 2421, 0, 5433, 156),
(562, 1, 6, 5912, 0),
(561, 1, 5, 3834, 0),
(560, 1, 4, 7939, 1326),
(559, 1, 3, 5062, 0),
(558, 1, 2, 5903, 1734),
(557, 1, 1, 5422, 202),
(556, 1, 0, 7933, 202),
(590, 2420, 4, 10146, 0),
(589, 2420, 3, 10130, 0),
(588, 2420, 2, 10132, 0),
(587, 2420, 1, 10135, 0),
(586, 2420, 0, 10139, 0),
(597, 2425, 5, 8251, 1102),
(596, 2425, 4, 3713, 0),
(595, 2425, 3, 7939, 1243),
(594, 2425, 2, 8059, 127),
(593, 2425, 1, 5903, 1727),
(592, 2425, 0, 5422, 238),
(602, 2426, 4, 8251, 1102),
(601, 2426, 3, 7939, 1228),
(600, 2426, 2, 5903, 1754),
(599, 2426, 1, 5433, 231),
(598, 2426, 0, 5399, 407);

-- --------------------------------------------------------

--
-- Table structure for table `myrunuo_characters_skills`
--

CREATE TABLE IF NOT EXISTS `myrunuo_characters_skills` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `char_id` int(12) unsigned DEFAULT NULL,
  `skill_id` int(10) unsigned DEFAULT NULL,
  `skill_value` int(3) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `charid` (`char_id`)
) ENGINE=MyISAM  DEFAULT CHARSET=latin1 AUTO_INCREMENT=530 ;

--
-- Dumping data for table `myrunuo_characters_skills`
--

INSERT INTO `myrunuo_characters_skills` (`id`, `char_id`, `skill_id`, `skill_value`) VALUES
(485, 2423, 46, 71),
(484, 2423, 40, 1000),
(483, 2423, 27, 1000),
(482, 2423, 17, 1000),
(481, 2423, 8, 500),
(480, 2423, 7, 1000),
(479, 2423, 5, 1000),
(478, 2423, 1, 1000),
(477, 2422, 50, 8),
(476, 2422, 46, 4),
(475, 2422, 6, 500),
(474, 2422, 0, 500),
(473, 2421, 50, 13),
(472, 2421, 46, 6),
(471, 2421, 31, 500),
(470, 2421, 0, 500),
(469, 1, 50, 11),
(468, 1, 46, 500),
(467, 1, 43, 300),
(466, 1, 25, 500),
(465, 1, 16, 300),
(523, 2420, 50, 130),
(522, 2420, 48, 240),
(521, 2420, 46, 4),
(520, 2420, 45, 180),
(519, 2420, 44, 170),
(518, 2420, 43, 235),
(517, 2420, 42, 220),
(516, 2420, 41, 231),
(515, 2420, 40, 233),
(514, 2420, 39, 200),
(513, 2420, 37, 957),
(512, 2420, 35, 236),
(511, 2420, 34, 9),
(510, 2420, 31, 210),
(509, 2420, 30, 241),
(508, 2420, 29, 999),
(507, 2420, 28, 190),
(506, 2420, 27, 234),
(505, 2420, 24, 160),
(504, 2420, 23, 1),
(503, 2420, 22, 909),
(502, 2420, 21, 239),
(501, 2420, 20, 150),
(500, 2420, 18, 120),
(499, 2420, 17, 140),
(498, 2420, 14, 238),
(497, 2420, 13, 100),
(496, 2420, 12, 99),
(495, 2420, 11, 999),
(494, 2420, 10, 10),
(493, 2420, 9, 99),
(492, 2420, 8, 1000),
(491, 2420, 7, 50),
(490, 2420, 6, 237),
(489, 2420, 5, 232),
(488, 2420, 4, 500),
(487, 2420, 1, 500),
(486, 2420, 0, 50),
(526, 2425, 46, 7),
(525, 2425, 2, 500),
(524, 2425, 0, 500),
(529, 2426, 46, 6),
(528, 2426, 1, 500),
(527, 2426, 0, 500);

-- --------------------------------------------------------

--
-- Table structure for table `myrunuo_guilds`
--

CREATE TABLE IF NOT EXISTS `myrunuo_guilds` (
  `guild_id` varchar(4) NOT NULL DEFAULT '',
  `guild_name` varchar(150) DEFAULT NULL,
  `guild_abbreviation` varchar(4) DEFAULT NULL,
  `guild_website` varchar(150) DEFAULT NULL,
  `guild_charter` varchar(250) DEFAULT NULL,
  `guild_type` varchar(8) DEFAULT NULL,
  `guild_wars` int(3) unsigned DEFAULT NULL,
  `guild_members` int(3) unsigned DEFAULT NULL,
  `guild_master` int(12) unsigned DEFAULT NULL,
  PRIMARY KEY (`guild_id`),
  KEY `guild_id` (`guild_id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

--
-- Dumping data for table `myrunuo_guilds`
--

INSERT INTO `myrunuo_guilds` (`guild_id`, `guild_name`, `guild_abbreviation`, `guild_website`, `guild_charter`, `guild_type`, `guild_wars`, `guild_members`, `guild_master`) VALUES
('1', 'publish 16', 'p16', NULL, 'To thwart OSI&#39;s attempt to drive the game toward i', 'Chaos', 0, 1, 2422);

-- --------------------------------------------------------

--
-- Table structure for table `myrunuo_guilds_wars`
--

CREATE TABLE IF NOT EXISTS `myrunuo_guilds_wars` (
  `guild_war_id` int(11) NOT NULL AUTO_INCREMENT,
  `guild_1` varchar(4) DEFAULT NULL,
  `guild_2` varchar(4) DEFAULT NULL,
  PRIMARY KEY (`guild_war_id`),
  KEY `guild1` (`guild_1`),
  KEY `guild2` (`guild_2`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1 AUTO_INCREMENT=1 ;

--
-- Dumping data for table `myrunuo_guilds_wars`
--


-- --------------------------------------------------------

--
-- Table structure for table `myrunuo_status`
--

CREATE TABLE IF NOT EXISTS `myrunuo_status` (
  `status_id` int(11) NOT NULL AUTO_INCREMENT,
  `char_id` int(12) unsigned DEFAULT NULL,
  `char_location` varchar(14) DEFAULT NULL,
  `char_map` varchar(8) DEFAULT NULL,
  `char_karma` int(6) DEFAULT NULL,
  `char_fame` int(6) DEFAULT NULL,
  PRIMARY KEY (`status_id`),
  KEY `charid` (`char_id`)
) ENGINE=MyISAM  DEFAULT CHARSET=latin1 AUTO_INCREMENT=2185 ;

--
-- Dumping data for table `myrunuo_status`
--


-- --------------------------------------------------------

--
-- Table structure for table `myrunuo_timestamps`
--

CREATE TABLE IF NOT EXISTS `myrunuo_timestamps` (
  `timestamp_id` int(11) NOT NULL AUTO_INCREMENT,
  `time_datetime` varchar(22) DEFAULT NULL,
  `time_type` varchar(6) DEFAULT NULL,
  PRIMARY KEY (`timestamp_id`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1 AUTO_INCREMENT=1 ;

--
-- Dumping data for table `myrunuo_timestamps`
--

