<?php
header('Content-Type: application/json; charset=utf-8');
session_start();
require_once 'db.php';

if (empty($_SESSION['user_id'])) {
    http_response_code(401);
    echo json_encode(['success' => false, 'message' => 'Требуется авторизация']);
    exit();
}

$user_id = (int)$_SESSION['user_id'];

// Обработчик только для POST (обновление прогресса)
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['success' => false, 'message' => 'Метод не поддерживается']);
    exit();
}

$payload = json_decode(file_get_contents('php://input'), true);
$book_file_id = (int)($payload['book_file_id'] ?? 0);
$percent = isset($payload['progress_percent']) ? floatval($payload['progress_percent']) : null;
$current_page = (int)($payload['current_page'] ?? 0);
$total_pages = (int)($payload['total_pages'] ?? 0);

if ($book_file_id <= 0 || $percent === null) {
    http_response_code(400);
    echo json_encode(['success' => false, 'message' => 'Неверные данные']);
    exit();
}

// Проверяем, что файл принадлежит книге пользователя
$check_sql = "
    SELECT 1 
    FROM book_files bf 
    JOIN books b ON bf.book_id = b.id
    JOIN user_books ub ON ub.book_id = b.id
    WHERE bf.id = ? AND ub.user_id = ?
    LIMIT 1
";
$stmt = mysqli_prepare($connect, $check_sql);
mysqli_stmt_bind_param($stmt, 'ii', $book_file_id, $user_id);
mysqli_stmt_execute($stmt);
$res = mysqli_stmt_get_result($stmt);
$allowed = mysqli_fetch_assoc($res);
mysqli_stmt_close($stmt);

if (!$allowed) {
    http_response_code(403);
    echo json_encode(['success' => false, 'message' => 'Нет доступа к этой книге']);
    exit();
}

// Нормализуем процент
$percent = max(0, min(100, $percent));

// Сохраняем прогресс
$save_sql = "
    INSERT INTO reading_progress (book_file_id, user_id, current_page, total_pages, progress_percent, last_read_at)
    VALUES (?, ?, ?, ?, ?, NOW())
    ON DUPLICATE KEY UPDATE 
        current_page = VALUES(current_page),
        total_pages = VALUES(total_pages),
        progress_percent = VALUES(progress_percent),
        last_read_at = VALUES(last_read_at)
";
$stmt = mysqli_prepare($connect, $save_sql);
mysqli_stmt_bind_param($stmt, 'iiiid', $book_file_id, $user_id, $current_page, $total_pages, $percent);
$ok = mysqli_stmt_execute($stmt);
$err = mysqli_error($connect);
mysqli_stmt_close($stmt);

if (!$ok) {
    http_response_code(500);
    echo json_encode(['success' => false, 'message' => 'Ошибка сохранения: ' . $err]);
    exit();
}

echo json_encode(['success' => true, 'progress_percent' => $percent]);

