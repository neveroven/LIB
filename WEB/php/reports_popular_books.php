<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Получение параметров фильтрации по дате
$date_from = isset($_GET['date_from']) ? $_GET['date_from'] : date('Y-m-d', strtotime('-30 days'));
$date_to = isset($_GET['date_to']) ? $_GET['date_to'] : date('Y-m-d');

// Валидация дат
if (!strtotime($date_from) || !strtotime($date_to)) {
    $date_from = date('Y-m-d', strtotime('-30 days'));
    $date_to = date('Y-m-d');
}

// Запрос для получения популярных книг за период
$query = "
    SELECT 
        b.id,
        b.title,
        b.author,
        b.series as category,
        COUNT(ub.book_id) as add_count,
        COUNT(DISTINCT ub.user_id) as unique_users
    FROM user_books ub
    JOIN books b ON ub.book_id = b.id
    WHERE DATE(ub.added_at) BETWEEN ? AND ?
    GROUP BY b.id, b.title, b.author, b.series
    ORDER BY add_count DESC, unique_users DESC
    LIMIT 100
";

$stmt = mysqli_prepare($connect, $query);
if ($stmt) {
    mysqli_stmt_bind_param($stmt, 'ss', $date_from, $date_to);
    mysqli_stmt_execute($stmt);
    $result = mysqli_stmt_get_result($stmt);
    
    $popular_books = [];
    while ($row = mysqli_fetch_assoc($result)) {
        $popular_books[] = $row;
    }
    mysqli_stmt_close($stmt);
} else {
    $popular_books = [];
    $error = "Ошибка выполнения запроса: " . mysqli_error($connect);
}

// === EXPORT ===
if (isset($_GET['export']) && $_GET['export'] === 'excel') {
    $filename = "popular_books_{$date_from}_{$date_to}.csv";
    header('Content-Type: text/csv; charset=UTF-8');
    header('Content-Disposition: attachment; filename="' . $filename . '"');
    header('Pragma: no-cache');
    header('Expires: 0');

    $out = fopen('php://output', 'w');
    // UTF-8 BOM for Excel
    fwrite($out, "\xEF\xBB\xBF");

    fputcsv($out, ['Название книги', 'Автор', 'Категория', 'Количество добавлений', 'Уникальных пользователей'], ';');
    foreach ($popular_books as $row) {
        fputcsv($out, [
            $row['title'] ?? '',
            ($row['author'] ?? '') !== '' ? $row['author'] : 'Не указан',
            ($row['category'] ?? '') !== '' ? $row['category'] : 'Без категории',
            $row['add_count'] ?? 0,
            $row['unique_users'] ?? 0,
        ], ';');
    }
    fclose($out);
    exit();
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Отчёт: Популярные книги - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
    <div class="container-fluid py-3">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2><i class="bi bi-bar-chart-fill"></i> Отчёт: Популярные книги</h2>
            <a class="btn btn-success"
               href="?date_from=<?= urlencode($date_from) ?>&date_to=<?= urlencode($date_to) ?>&export=excel">
                <i class="bi bi-file-earmark-spreadsheet"></i> Скачать Excel
            </a>
        </div>

        <!-- Фильтр по дате -->
        <div class="card mb-4">
            <div class="card-header">
                <h5 class="mb-0">Фильтр по периоду</h5>
            </div>
            <div class="card-body">
                <form method="GET" class="row g-3">
                    <div class="col-md-4">
                        <label for="date_from" class="form-label">Дата начала:</label>
                        <input type="date" class="form-control" id="date_from" name="date_from" 
                               value="<?= htmlspecialchars($date_from) ?>" required>
                    </div>
                    <div class="col-md-4">
                        <label for="date_to" class="form-label">Дата окончания:</label>
                        <input type="date" class="form-control" id="date_to" name="date_to" 
                               value="<?= htmlspecialchars($date_to) ?>" required>
                    </div>
                    <div class="col-md-4 d-flex align-items-end">
                        <button type="submit" class="btn btn-primary w-100">
                            <i class="bi bi-search"></i> Применить фильтр
                        </button>
                    </div>
                </form>
            </div>
        </div>

        <!-- Таблица отчёта -->
        <div class="card">
            <div class="card-header">
                <h5 class="mb-0">
                    Популярные книги за период: 
                    <?= date('d.m.Y', strtotime($date_from)) ?> - <?= date('d.m.Y', strtotime($date_to)) ?>
                </h5>
            </div>
            <div class="card-body">
                <?php if (!empty($error)): ?>
                    <div class="alert alert-danger"><?= htmlspecialchars($error) ?></div>
                <?php endif; ?>
                
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead class="table-dark">
                            <tr>
                                <th>№</th>
                                <th>Название книги</th>
                                <th>Автор</th>
                                <th>Категория (Серия)</th>
                                <th>Количество добавлений</th>
                                <th>Уникальных пользователей</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php if (!empty($popular_books)): ?>
                                <?php $index = 1; ?>
                                <?php foreach ($popular_books as $book): ?>
                                    <tr>
                                        <td><?= $index++ ?></td>
                                        <td><strong><?= htmlspecialchars($book['title']) ?></strong></td>
                                        <td><?= htmlspecialchars($book['author'] ?: 'Не указан') ?></td>
                                        <td>
                                            <?php if (!empty($book['category'])): ?>
                                                <span class="badge bg-info"><?= htmlspecialchars($book['category']) ?></span>
                                            <?php else: ?>
                                                <span class="text-muted">-</span>
                                            <?php endif; ?>
                                        </td>
                                        <td>
                                            <span class="badge bg-primary"><?= $book['add_count'] ?></span>
                                        </td>
                                        <td>
                                            <span class="badge bg-success"><?= $book['unique_users'] ?></span>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                            <?php else: ?>
                                <tr>
                                    <td colspan="6" class="text-center text-muted">
                                        <i class="bi bi-inbox"></i> Нет данных за выбранный период
                                    </td>
                                </tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>

                <!-- Итоговая статистика -->
                <?php if (!empty($popular_books)): ?>
                    <div class="mt-4 p-3 bg-light rounded">
                        <h6>Итоговая статистика:</h6>
                        <div class="row">
                            <div class="col-md-4">
                                <strong>Всего книг в отчёте:</strong> <?= count($popular_books) ?>
                            </div>
                            <div class="col-md-4">
                                <strong>Всего добавлений:</strong> <?= array_sum(array_column($popular_books, 'add_count')) ?>
                            </div>
                            <div class="col-md-4">
                                <strong>Уникальных пользователей:</strong> 
                                <?= count(array_unique(array_column($popular_books, 'unique_users'))) ?>
                            </div>
                        </div>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>

