<?php
session_start();
// Доступ: любой авторизованный пользователь (user/admin). Можно ослабить по требованию.
if (empty($_SESSION['user_id']) && empty($_SESSION['is_admin'])) {
    http_response_code(401);
    exit('auth required');
}

require_once 'db.php';

// Получаем базовый путь к DB
$db_folder_path = '';
$settings_res = mysqli_query($connect, "SELECT setting_value FROM settings WHERE setting_key = 'db_folder_path' LIMIT 1");
if ($settings_res && $row = mysqli_fetch_assoc($settings_res)) {
    $db_folder_path = trim($row['setting_value'] ?? '');
}

// Декодируем путь
$token = $_GET['p'] ?? '';
if ($token === '') {
    http_response_code(400);
    exit('missing path');
}

$path = base64_decode($token, true);
if ($path === false) {
    http_response_code(400);
    exit('bad path');
}

// Безопасность: разрешаем только внутри db_folder_path (если указан)
if ($db_folder_path !== '') {
    $realBase = realpath($db_folder_path);
    $realPath = realpath($path);
    if ($realBase === false || $realPath === false || strpos($realPath, $realBase) !== 0) {
        http_response_code(403);
        exit('forbidden');
    }
} else {
    // если базовый путь не задан, просто проверяем существование
    if (!file_exists($path)) {
        http_response_code(404);
        exit('not found');
    }
}

if (!is_readable($path)) {
    http_response_code(404);
    exit('not readable');
}

$mime = mime_content_type($path) ?: 'application/octet-stream';
header('Content-Type: ' . $mime);
header('Content-Length: ' . filesize($path));
readfile($path);
exit;

