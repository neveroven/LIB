<?php
session_start();
require_once 'db.php';

header('Content-Type: application/json');

// Проверка авторизации
if (empty($_SESSION['user_id'])) {
    echo json_encode([
        'success' => false,
        'message' => 'Не авторизован'
    ]);
    exit();
}

$user_id = $_SESSION['user_id'];

try {
    // Получаем общее количество книг пользователя
    $query_total = "SELECT COUNT(*) as total 
                    FROM user_books 
                    WHERE user_id = ?";
    $stmt_total = mysqli_prepare($connect, $query_total);
    mysqli_stmt_bind_param($stmt_total, 'i', $user_id);
    mysqli_stmt_execute($stmt_total);
    $result_total = mysqli_stmt_get_result($stmt_total);
    $total_data = mysqli_fetch_assoc($result_total);
    $total = $total_data['total'] ?? 0;
    
    // Получаем дату последней добавленной книги
    $query_last = "SELECT ub.added_at 
                   FROM user_books ub 
                   WHERE ub.user_id = ? 
                   ORDER BY ub.added_at DESC 
                   LIMIT 1";
    $stmt_last = mysqli_prepare($connect, $query_last);
    mysqli_stmt_bind_param($stmt_last, 'i', $user_id);
    mysqli_stmt_execute($stmt_last);
    $result_last = mysqli_stmt_get_result($stmt_last);
    $last_data = mysqli_fetch_assoc($result_last);
    
    $last_added = '-';
    if ($last_data && $last_data['added_at']) {
        $date = new DateTime($last_data['added_at']);
        $last_added = $date->format('d.m.Y');
    }
    
    // Получаем статистику по статусам
    $query_status = "SELECT status, COUNT(*) as count 
                     FROM user_books 
                     WHERE user_id = ? 
                     GROUP BY status";
    $stmt_status = mysqli_prepare($connect, $query_status);
    mysqli_stmt_bind_param($stmt_status, 'i', $user_id);
    mysqli_stmt_execute($stmt_status);
    $result_status = mysqli_stmt_get_result($stmt_status);
    
    $status_stats = [
        'reading' => 0,
        'finished' => 0,
        'planned' => 0
    ];
    
    while ($row = mysqli_fetch_assoc($result_status)) {
        $status_stats[$row['status']] = (int)$row['count'];
    }
    
    echo json_encode([
        'success' => true,
        'stats' => [
            'total' => (int)$total,
            'last_added' => $last_added,
            'reading' => $status_stats['reading'],
            'finished' => $status_stats['finished'],
            'planned' => $status_stats['planned']
        ]
    ]);
    
} catch (Exception $e) {
    echo json_encode([
        'success' => false,
        'message' => 'Ошибка при получении статистики: ' . $e->getMessage()
    ]);
}

