<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Функция для безопасного выполнения запросов
function executeQuery($connect, $sql) {
    $result = mysqli_query($connect, $sql);
    if (!$result) {
        error_log("SQL Error in reading_statistics.php: " . mysqli_error($connect));
        return false;
    }
    return $result;
}

// Получение общей статистики
$stats = [
    'active_users' => 0,
    'active_books' => 0, 
    'avg_progress' => 0,
    'last_activity' => null
];

$stats_query = "
    SELECT 
        COUNT(DISTINCT user_id) as active_users,
        COUNT(DISTINCT book_file_id) as active_books,
        AVG(progress_percent) as avg_progress,
        MAX(last_read_at) as last_activity
    FROM reading_progress
";

$stats_result = executeQuery($connect, $stats_query);
if ($stats_result) {
    $stats = mysqli_fetch_assoc($stats_result);
}

// Получение самых читаемых книг
$popular_books = [];
$popular_query = "
    SELECT b.title, b.author, COUNT(rp.id) as read_count, AVG(rp.progress_percent) as avg_progress
    FROM reading_progress rp
    JOIN book_files bf ON rp.book_file_id = bf.id
    JOIN books b ON bf.book_id = b.id
    GROUP BY b.id, b.title, b.author
    ORDER BY read_count DESC, avg_progress DESC
    LIMIT 10
";

$popular_result = executeQuery($connect, $popular_query);
if ($popular_result) {
    while ($row = mysqli_fetch_assoc($popular_result)) {
        $popular_books[] = $row;
    }
}

// Получение самых активных пользователей
$active_users = [];
$active_query = "
    SELECT u.User_login, COUNT(rp.id) as sessions, MAX(rp.last_read_at) as last_read,
           AVG(rp.progress_percent) as avg_progress
    FROM reading_progress rp
    JOIN users u ON rp.user_id = u.UID
    GROUP BY u.UID, u.User_login
    ORDER BY sessions DESC, last_read DESC
    LIMIT 10
";

$active_result = executeQuery($connect, $active_query);
if ($active_result) {
    while ($row = mysqli_fetch_assoc($active_result)) {
        $active_users[] = $row;
    }
}

// Статистика по дням (последние 7 дней)
$daily_stats = [];
$daily_query = "
    SELECT 
        DATE(last_read_at) as read_date,
        COUNT(*) as sessions,
        COUNT(DISTINCT user_id) as unique_users
    FROM reading_progress 
    WHERE last_read_at >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    GROUP BY DATE(last_read_at)
    ORDER BY read_date DESC
";

$daily_result = executeQuery($connect, $daily_query);
if ($daily_result) {
    while ($row = mysqli_fetch_assoc($daily_result)) {
        $daily_stats[] = $row;
    }
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Статистика чтения - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</head>
<body>
    <div class="container-fluid py-3">
        <h2><i class="bi bi-graph-up"></i> Статистика чтения</h2>

        <!-- Основная статистика -->
        <div class="row mt-4">
            <div class="col-md-3">
                <div class="card text-white bg-primary">
                    <div class="card-body">
                        <h5 class="card-title"><?= $stats['active_users'] ?: 0 ?></h5>
                        <p class="card-text">Активных читателей</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-success">
                    <div class="card-body">
                        <h5 class="card-title"><?= $stats['active_books'] ?: 0 ?></h5>
                        <p class="card-text">Активных книг</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-warning">
                    <div class="card-body">
                        <h5 class="card-title"><?= round($stats['avg_progress'] ?: 0, 1) ?>%</h5>
                        <p class="card-text">Средний прогресс</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-info">
                    <div class="card-body">
                        <h5 class="card-title">
                            <?= $stats['last_activity'] ? date('d.m.Y H:i', strtotime($stats['last_activity'])) : 'Нет данных' ?>
                        </h5>
                        <p class="card-text">Последняя активность</p>
                    </div>
                </div>
            </div>
        </div>

        <div class="row mt-4">
            <!-- Самые читаемые книги -->
            <div class="col-md-6">
                <div class="card">
                    <div class="card-header">
                        <h5 class="card-title mb-0">Самые читаемые книги</h5>
                    </div>
                    <div class="card-body">
                        <div class="table-responsive">
                            <table class="table table-sm">
                                <thead>
                                    <tr>
                                        <th>Книга</th>
                                        <th>Чтений</th>
                                        <th>Прогресс</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <?php foreach ($popular_books as $book): ?>
                                        <tr>
                                            <td>
                                                <strong><?= htmlspecialchars($book['title']) ?></strong>
                                                <?php if (!empty($book['author'])): ?>
                                                    <br><small class="text-muted"><?= htmlspecialchars($book['author']) ?></small>
                                                <?php endif; ?>
                                            </td>
                                            <td>
                                                <span class="badge bg-primary"><?= $book['read_count'] ?></span>
                                            </td>
                                            <td>
                                                <div class="progress" style="height: 20px;">
                                                    <div class="progress-bar" style="width: <?= $book['avg_progress'] ?>%">
                                                        <?= round($book['avg_progress'], 1) ?>%
                                                    </div>
                                                </div>
                                            </td>
                                        </tr>
                                    <?php endforeach; ?>
                                    <?php if (empty($popular_books)): ?>
                                        <tr>
                                            <td colspan="3" class="text-center text-muted">Нет данных о чтениях</td>
                                        </tr>
                                    <?php endif; ?>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Самые активные пользователи -->
            <div class="col-md-6">
                <div class="card">
                    <div class="card-header">
                        <h5 class="card-title mb-0">Самые активные читатели</h5>
                    </div>
                    <div class="card-body">
                        <div class="table-responsive">
                            <table class="table table-sm">
                                <thead>
                                    <tr>
                                        <th>Пользователь</th>
                                        <th>Сессий</th>
                                        <th>Последнее чтение</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <?php foreach ($active_users as $user): ?>
                                        <tr>
                                            <td>
                                                <strong><?= htmlspecialchars($user['User_login']) ?></strong>
                                            </td>
                                            <td>
                                                <span class="badge bg-success"><?= $user['sessions'] ?></span>
                                            </td>
                                            <td>
                                                <small class="text-muted">
                                                    <?= date('d.m.Y H:i', strtotime($user['last_read'])) ?>
                                                </small>
                                            </td>
                                        </tr>
                                    <?php endforeach; ?>
                                    <?php if (empty($active_users)): ?>
                                        <tr>
                                            <td colspan="3" class="text-center text-muted">Нет данных о пользователях</td>
                                        </tr>
                                    <?php endif; ?>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Статистика по дням -->
        <div class="row mt-4">
            <div class="col-12">
                <div class="card">
                    <div class="card-header">
                        <h5 class="card-title mb-0">Активность по дням (последние 7 дней)</h5>
                    </div>
                    <div class="card-body">
                        <div class="table-responsive">
                            <table class="table table-striped">
                                <thead>
                                    <tr>
                                        <th>Дата</th>
                                        <th>Сессий чтения</th>
                                        <th>Уникальных пользователей</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <?php foreach ($daily_stats as $day): ?>
                                        <tr>
                                            <td><?= date('d.m.Y', strtotime($day['read_date'])) ?></td>
                                            <td>
                                                <span class="badge bg-primary"><?= $day['sessions'] ?></span>
                                            </td>
                                            <td>
                                                <span class="badge bg-success"><?= $day['unique_users'] ?></span>
                                            </td>
                                        </tr>
                                    <?php endforeach; ?>
                                    <?php if (empty($daily_stats)): ?>
                                        <tr>
                                            <td colspan="3" class="text-center text-muted">Нет данных за последние 7 дней</td>
                                        </tr>
                                    <?php endif; ?>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>