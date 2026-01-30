<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Запрос для получения информации о пользователях
// Нужно добавить поле даты регистрации, если его нет - используем минимальную дату из user_books
$query = "
    SELECT 
        u.UID as user_id,
        u.User_login as login,
        u.Is_admin as is_admin,
        COUNT(DISTINCT ub.book_id) as books_count,
        COUNT(DISTINCT rp.id) as reading_sessions,
        MIN(ub.added_at) as first_book_date,
        MAX(rp.last_read_at) as last_activity
    FROM users u
    LEFT JOIN user_books ub ON u.UID = ub.user_id
    LEFT JOIN reading_progress rp ON u.UID = rp.user_id
    GROUP BY u.UID, u.User_login, u.Is_admin
    ORDER BY u.UID ASC
";

$result = mysqli_query($connect, $query);
$users = [];

if ($result) {
    while ($row = mysqli_fetch_assoc($result)) {
        $users[] = $row;
    }
} else {
    $error = "Ошибка выполнения запроса: " . mysqli_error($connect);
}

// === EXPORT ===
if (isset($_GET['export']) && $_GET['export'] === 'excel') {
    $filename = "users_report_" . date('Y-m-d') . ".csv";

    // Используем тип Excel-файла, чтобы Windows/браузеры открывали файл сразу в Excel
    header('Content-Type: application/vnd.ms-excel; charset=UTF-8');
    header('Content-Disposition: attachment; filename="' . $filename . '"');
    header('Pragma: no-cache');
    header('Expires: 0');

    $out = fopen('php://output', 'w');
    // UTF-8 BOM для корректного отображения кириллицы в Excel
    fwrite($out, "\xEF\xBB\xBF");

    // Заголовочная часть отчёта
    fputcsv($out, ['Отчёт: Пользователи - Paradise Library'], ';');
    fputcsv($out, ['Дата формирования: ' . date('d.m.Y H:i')], ';');
    fputcsv($out, [''], ';'); // пустая строка-разделитель

    // Основная таблица
    fputcsv($out, ['ID', 'Логин', 'Статус', 'Количество книг', 'Сессий чтения', 'Дата первой книги', 'Последняя активность'], ';');
    foreach ($users as $u) {
        fputcsv($out, [
            $u['user_id'] ?? '',
            $u['login'] ?? '',
            (($u['is_admin'] ?? 0) == 1) ? 'Администратор' : 'Пользователь',
            $u['books_count'] ?? 0,
            $u['reading_sessions'] ?? 0,
            !empty($u['first_book_date']) ? date('d.m.Y', strtotime($u['first_book_date'])) : '',
            !empty($u['last_activity']) ? date('d.m.Y H:i', strtotime($u['last_activity'])) : '',
        ], ';');
    }

    fclose($out);
    exit();
}

// Подсчет статистики
$total_users = count($users);
$admin_count = count(array_filter($users, function($u) { return $u['is_admin'] == 1; }));
$regular_users = $total_users - $admin_count;
$active_users = count(array_filter($users, function($u) { return !empty($u['last_activity']); }));
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Отчёт: Пользователи - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
    <div class="container-fluid py-3">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2><i class="bi bi-people-fill"></i> Отчёт: Пользователи</h2>
            <a class="btn btn-success" href="?export=excel">
                <i class="bi bi-file-earmark-spreadsheet"></i> Скачать Excel
            </a>
        </div>

        <!-- Статистика -->
        <div class="row mb-4">
            <div class="col-md-3">
                <div class="card text-white bg-primary">
                    <div class="card-body">
                        <h5 class="card-title"><?= $total_users ?></h5>
                        <p class="card-text">Всего пользователей</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-success">
                    <div class="card-body">
                        <h5 class="card-title"><?= $regular_users ?></h5>
                        <p class="card-text">Обычных пользователей</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-warning">
                    <div class="card-body">
                        <h5 class="card-title"><?= $admin_count ?></h5>
                        <p class="card-text">Администраторов</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-white bg-info">
                    <div class="card-body">
                        <h5 class="card-title"><?= $active_users ?></h5>
                        <p class="card-text">Активных пользователей</p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Таблица отчёта -->
        <div class="card">
            <div class="card-header">
                <h5 class="mb-0">Список пользователей</h5>
            </div>
            <div class="card-body">
                <?php if (!empty($error)): ?>
                    <div class="alert alert-danger"><?= htmlspecialchars($error) ?></div>
                <?php endif; ?>
                
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead class="table-dark">
                            <tr>
                                <th>ID</th>
                                <th>Логин</th>
                                <th>Статус</th>
                                <th>Количество книг</th>
                                <th>Сессий чтения</th>
                                <th>Дата первой книги</th>
                                <th>Последняя активность</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php if (!empty($users)): ?>
                                <?php foreach ($users as $user): ?>
                                    <tr>
                                        <td><?= $user['user_id'] ?></td>
                                        <td><strong><?= htmlspecialchars($user['login']) ?></strong></td>
                                        <td>
                                            <?php if ($user['is_admin'] == 1): ?>
                                                <span class="badge bg-danger">Администратор</span>
                                            <?php else: ?>
                                                <span class="badge bg-success">Пользователь</span>
                                            <?php endif; ?>
                                        </td>
                                        <td>
                                            <span class="badge bg-primary"><?= $user['books_count'] ?: 0 ?></span>
                                        </td>
                                        <td>
                                            <span class="badge bg-info"><?= $user['reading_sessions'] ?: 0 ?></span>
                                        </td>
                                        <td>
                                            <?php if (!empty($user['first_book_date'])): ?>
                                                <?= date('d.m.Y', strtotime($user['first_book_date'])) ?>
                                            <?php else: ?>
                                                <span class="text-muted">-</span>
                                            <?php endif; ?>
                                        </td>
                                        <td>
                                            <?php if (!empty($user['last_activity'])): ?>
                                                <?= date('d.m.Y H:i', strtotime($user['last_activity'])) ?>
                                            <?php else: ?>
                                                <span class="text-muted">Нет активности</span>
                                            <?php endif; ?>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                            <?php else: ?>
                                <tr>
                                    <td colspan="7" class="text-center text-muted">
                                        <i class="bi bi-inbox"></i> Нет данных
                                    </td>
                                </tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>

