<?php
    $connect = mysqli_connect('localhost', 'root', 'root', 'Paradise');
    if (!$connect) {
        http_response_code(500);
        die('Ошибка подключения к базе данных');
    }
    // Устанавливаем корректную кодировку соединения
    if (!mysqli_set_charset($connect, 'utf8mb4')) {
        // Попытка задать через запрос, если функция не сработала
        mysqli_query($connect, "SET NAMES 'utf8mb4' COLLATE 'utf8mb4_general_ci'");
    }
