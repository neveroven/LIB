-- MySQL dump 10.13  Distrib 8.0.36, for Win64 (x86_64)
--
-- Host: localhost    Database: paradise
-- ------------------------------------------------------
-- Server version	8.3.0

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `reading_progress`
--

DROP TABLE IF EXISTS `reading_progress`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `reading_progress` (
  `id` int NOT NULL AUTO_INCREMENT,
  `book_file_id` int NOT NULL,
  `user_id` int NOT NULL,
  `device_id` text,
  `current_page` int NOT NULL DEFAULT '0',
  `total_pages` int DEFAULT NULL,
  `progress_percent` double DEFAULT NULL,
  `last_read_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `book_file_id` (`book_file_id`),
  KEY `user_id` (`user_id`),
  CONSTRAINT `reading_progress_ibfk_1` FOREIGN KEY (`book_file_id`) REFERENCES `book_files` (`id`) ON DELETE CASCADE,
  CONSTRAINT `reading_progress_ibfk_2` FOREIGN KEY (`user_id`) REFERENCES `users` (`UID`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `reading_progress`
--

LOCK TABLES `reading_progress` WRITE;
/*!40000 ALTER TABLE `reading_progress` DISABLE KEYS */;
INSERT INTO `reading_progress` VALUES (1,1,1,NULL,0,13664,0.007318501170960188,'2025-10-11 16:02:12');
/*!40000 ALTER TABLE `reading_progress` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-10-19 22:51:32
