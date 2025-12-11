<?php
session_start();
include("db.php");

// Проверка авторизации администратора
if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Сводные показатели
$totals = [
    'books' => 0,
    'users' => 0,
    'reading' => 0,
    'files' => 0,
];

$queries = [
    'books' => "SELECT COUNT(*) as count FROM books",
    'users' => "SELECT COUNT(*) as count FROM users",
    'reading' => "SELECT COUNT(*) as count FROM reading_progress",
    'files' => "SELECT COUNT(*) as count FROM book_files",
];

foreach ($queries as $key => $sql) {
    $res = mysqli_query($connect, $sql);
    if ($res) {
        $row = mysqli_fetch_assoc($res);
        $totals[$key] = (int)($row['count'] ?? 0);
    }
}

// Активность чтения
$activity = [
    'active_readers_24h' => 0,
    'avg_progress' => 0,
    'last_activity' => null,
];

$activeRes = mysqli_query($connect, "
    SELECT COUNT(DISTINCT user_id) as total
    FROM reading_progress
    WHERE last_read_at >= DATE_SUB(NOW(), INTERVAL 1 DAY)
");
if ($activeRes) {
    $activity['active_readers_24h'] = (int)(mysqli_fetch_assoc($activeRes)['total'] ?? 0);
}

$progressRes = mysqli_query($connect, "
    SELECT AVG(progress_percent) as avg_progress, MAX(last_read_at) as last_activity
    FROM reading_progress
");
if ($progressRes) {
    $row = mysqli_fetch_assoc($progressRes);
    $activity['avg_progress'] = round((float)($row['avg_progress'] ?? 0), 1);
    $activity['last_activity'] = $row['last_activity'] ?? null;
}

// Последние добавленные книги
$recentBooks = [];
$recentBooksRes = mysqli_query($connect, "
    SELECT id, title, author, language, published_year
    FROM books
    ORDER BY id DESC
    LIMIT 5
");
if ($recentBooksRes) {
    while ($row = mysqli_fetch_assoc($recentBooksRes)) {
        $recentBooks[] = $row;
    }
}

// Последние сессии чтения
$recentReadings = [];
$recentReadingsRes = mysqli_query($connect, "
    SELECT u.User_login, b.title, b.author, rp.progress_percent, rp.last_read_at
    FROM reading_progress rp
    JOIN users u ON rp.user_id = u.UID
    JOIN book_files bf ON rp.book_file_id = bf.id
    JOIN books b ON bf.book_id = b.id
    ORDER BY rp.last_read_at DESC
    LIMIT 5
");
if ($recentReadingsRes) {
    while ($row = mysqli_fetch_assoc($recentReadingsRes)) {
        $recentReadings[] = $row;
    }
}

// Форматы файлов книг
$formatStats = [];
$formatRes = mysqli_query($connect, "
    SELECT UPPER(IFNULL(format, 'Не указано')) as format_label, COUNT(*) as total
    FROM book_files
    GROUP BY format_label
    ORDER BY total DESC
");
if ($formatRes) {
    while ($row = mysqli_fetch_assoc($formatRes)) {
        $formatStats[] = $row;
    }
}
?>
<!doctype html>
<html lang="ru">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Панель администратора - Paradise Library</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-iYQeCzEYFbKjA/T2uDLTpkwGzCiq6soy8tYaI1GyVh/UjpbCx/TYkiZhlZB6+fzT" crossorigin="anonymous">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>

<body>
    <div class="container-fluid py-4">
        <div class="d-flex flex-wrap justify-content-between align-items-center mb-4">
            <div>
                <h2 class="mb-1"><i class="bi bi-speedometer2"></i> Панель администратора</h2>
                <p class="text-muted mb-0">Краткий обзор базы, чтений и последних событий.</p>
            </div>
            <div class="text-end">
                <span class="badge bg-secondary">Обновлено: <?= date('d.m.Y H:i') ?></span>
            </div>
        </div>

        <!-- Сводка -->
        <div class="row g-3 mb-3">
            <div class="col-md-3">
                <div class="card text-white bg-primary h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <h5 class="card-title mb-1"><?= $totals['books'] ?></h5>
                                <p class="card-text mb-0">Всего книг</p>
                            </div>
                            <i class="bi bi-book fs-3 opacity-75"></i>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-success h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <h5 class="card-title mb-1"><?= $totals['users'] ?></h5>
                                <p class="card-text mb-0">Пользователей</p>
                            </div>
                            <i class="bi bi-people fs-3 opacity-75"></i>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-warning h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <h5 class="card-title mb-1"><?= $totals['reading'] ?></h5>
                                <p class="card-text mb-0">Активных чтений</p>
                            </div>
                            <i class="bi bi-clock-history fs-3 opacity-75"></i>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-info h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start">
                            <div>
                                <h5 class="card-title mb-1"><?= $totals['files'] ?></h5>
                                <p class="card-text mb-0">Файлов книг</p>
                            </div>
                            <i class="bi bi-folder2-open fs-3 opacity-75"></i>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Активность -->
        <div class="row g-3 mb-4">
            <div class="col-md-4">
                <div class="card border-0 shadow-sm h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <p class="text-muted mb-1">Активных читателей за 24ч</p>
                                <h4 class="mb-0"><?= $activity['active_readers_24h'] ?></h4>
                            </div>
                            <span class="badge bg-light text-success fs-6">
                                <i class="bi bi-activity"></i>
                            </span>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="card border-0 shadow-sm h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <p class="text-muted mb-1">Средний прогресс по чтениям</p>
                                <h4 class="mb-0"><?= $activity['avg_progress'] ?>%</h4>
                            </div>
                            <span class="badge bg-light text-warning fs-6">
                                <i class="bi bi-bar-chart-line"></i>
                            </span>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="card border-0 shadow-sm h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <p class="text-muted mb-1">Последняя активность</p>
                                <h6 class="mb-0">
                                    <?= $activity['last_activity'] ? date('d.m.Y H:i', strtotime($activity['last_activity'])) : 'Нет данных' ?>
                                </h6>
                            </div>
                            <span class="badge bg-light text-info fs-6">
                                <i class="bi bi-clock"></i>
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Последние события -->
        <div class="row g-3">
            <div class="col-lg-6">
                <div class="card h-100">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h5 class="mb-0"><i class="bi bi-journal-text"></i> Последние книги</h5>
                        <a class="btn btn-sm btn-outline-primary" href="books.php">Открыть каталог</a>
                    </div>
                    <div class="card-body">
                        <div class="table-responsive">
                            <table class="table align-middle mb-0">
                                <thead>
                                    <tr>
                                        <th>Название</th>
                                        <th>Автор</th>
                                        <th class="text-end">Год</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <?php foreach ($recentBooks as $book): ?>
                                        <tr>
                                            <td>
                                                <strong><?= htmlspecialchars($book['title']) ?></strong>
                                                <div class="text-muted small">
                                                    <?= htmlspecialchars($book['language'] ?: '—') ?>
                                                </div>
                                            </td>
                                            <td><?= htmlspecialchars($book['author'] ?: 'Не указан') ?></td>
                                            <td class="text-end"><?= $book['published_year'] ?: '—' ?></td>
                                        </tr>
                                    <?php endforeach; ?>
                                    <?php if (empty($recentBooks)): ?>
                                        <tr>
                                            <td colspan="3" class="text-center text-muted py-3">Книги не найдены</td>
                                        </tr>
                                    <?php endif; ?>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-lg-6">
                <div class="card h-100">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h5 class="mb-0"><i class="bi bi-clock-history"></i> Недавние чтения</h5>
                        <a class="btn btn-sm btn-outline-primary" href="reading_progress.php">Детали</a>
                    </div>
                    <div class="card-body">
                        <div class="list-group list-group-flush">
                            <?php foreach ($recentReadings as $progress): ?>
                                <div class="list-group-item">
                                    <div class="d-flex justify-content-between">
                                        <div>
                                            <strong><?= htmlspecialchars($progress['User_login']) ?></strong>
                                            <div class="text-muted small">
                                                <?= htmlspecialchars($progress['title']) ?>
                                                <?php if (!empty($progress['author'])): ?>
                                                    · <?= htmlspecialchars($progress['author']) ?>
                                                <?php endif; ?>
                                            </div>
                                        </div>
                                        <span class="badge bg-info text-dark">
                                            <?= round($progress['progress_percent'], 1) ?>%
                                        </span>
                                    </div>
                                    <div class="text-muted small mt-1">
                                        <?= date('d.m.Y H:i', strtotime($progress['last_read_at'])) ?>
                                    </div>
                                </div>
                            <?php endforeach; ?>
                            <?php if (empty($recentReadings)): ?>
                                <div class="list-group-item text-center text-muted py-3">
                                    Нет данных о чтениях
                                </div>
                            <?php endif; ?>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Форматы и быстрые действия -->
        <div class="row g-3 mt-3">
            <div class="col-lg-4">
                <div class="card h-100">
                    <div class="card-header">
                        <h5 class="mb-0"><i class="bi bi-file-earmark-richtext"></i> Форматы файлов</h5>
                    </div>
                    <div class="card-body">
                        <?php foreach ($formatStats as $format): ?>
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <span><?= htmlspecialchars($format['format_label']) ?></span>
                                <span class="badge bg-secondary"><?= $format['total'] ?></span>
                            </div>
                        <?php endforeach; ?>
                        <?php if (empty($formatStats)): ?>
                            <div class="text-muted text-center">Нет данных о файлах</div>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
            <div class="col-lg-8">
                <div class="card h-100">
                    <div class="card-header">
                        <h5 class="mb-0"><i class="bi bi-lightning"></i> Быстрые действия</h5>
                    </div>
                    <div class="card-body">
                        <div class="row g-2">
                            <div class="col-md-6 d-grid">
                                <a class="btn btn-outline-primary" href="books.php">
                                    <i class="bi bi-plus-circle"></i> Добавить или найти книгу
                                </a>
                            </div>
                            <div class="col-md-6 d-grid">
                                <a class="btn btn-outline-success" href="users.php">
                                    <i class="bi bi-person-plus"></i> Управление пользователями
                                </a>
                            </div>
                            <div class="col-md-6 d-grid">
                                <a class="btn btn-outline-info" href="reading_statistics.php">
                                    <i class="bi bi-graph-up"></i> Посмотреть аналитику
                                </a>
                            </div>
                            <div class="col-md-6 d-grid">
                                <a class="btn btn-outline-secondary" href="settings.php">
                                    <i class="bi bi-gear"></i> Настройки системы
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>