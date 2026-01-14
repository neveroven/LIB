<?php
session_start();
require_once 'db.php';

// Только авторизованный НЕ-админ
if (empty($_SESSION['user_id']) || !empty($_SESSION['is_admin'])) {
    header('Location: ../index.php');
    exit();
}

$user_id = (int)$_SESSION['user_id'];

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    header('Location: user_dashboard.php');
    exit();
}

$book_id = (int)($_POST['book_id'] ?? 0);
$status = $_POST['status'] ?? '';

$allowed_statuses = ['planned', 'reading', 'finished', 'paused', 'dropped'];
if ($book_id <= 0 || !in_array($status, $allowed_statuses, true)) {
    header('Location: user_dashboard.php');
    exit();
}

// Обновляем статус только своей записи
$stmt = mysqli_prepare(
    $connect,
    "UPDATE user_books SET status = ? WHERE user_id = ? AND book_id = ? LIMIT 1"
);
mysqli_stmt_bind_param($stmt, 'sii', $status, $user_id, $book_id);
mysqli_stmt_execute($stmt);
mysqli_stmt_close($stmt);

header('Location: user_dashboard.php');
exit();


