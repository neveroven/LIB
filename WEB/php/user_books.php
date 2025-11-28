<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Получение списка книг пользователей
$user_books = [];
$query = "
    SELECT ub.*, u.User_login, b.title, b.author, bf.file_name, bf.format
    FROM user_books ub
    JOIN users u ON ub.user_id = u.UID
    JOIN books b ON ub.book_id = b.id
    LEFT JOIN book_files bf ON b.id = bf.book_id
    ORDER BY ub.added_at DESC
";
$result = mysqli_query($connect, $query);
while ($row = mysqli_fetch_assoc($result)) {
    $user_books[] = $row;
}

// Статусы книг с цветами
$status_colors = [
    'reading' => 'primary',
    'planned' => 'secondary', 
    'finished' => 'success',
    'paused' => 'warning',
    'dropped' => 'danger'
];

$status_labels = [
    'reading' => 'Читается',
    'planned' => 'Запланирована',
    'finished' => 'Прочитана',
    'paused' => 'На паузе',
    'dropped' => 'Брошена'
];
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Книги пользователей - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
    <div class="container-fluid py-3">
        <h2><i class="bi bi-journal-bookmark"></i> Книги пользователей</h2>

        <!-- Фильтры -->
        <div class="card mt-4">
            <div class="card-body">
                <div class="row g-3">
                    <div class="col-md-4">
                        <label class="form-label">Статус</label>
                        <select class="form-select" id="statusFilter">
                            <option value="">Все статусы</option>
                            <?php foreach ($status_labels as $value => $label): ?>
                                <option value="<?= $value ?>"><?= $label ?></option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <label class="form-label">Пользователь</label>
                        <select class="form-select" id="userFilter">
                            <option value="">Все пользователи</option>
                            <?php
                            $users = array_unique(array_column($user_books, 'User_login'));
                            foreach ($users as $user): ?>
                                <option value="<?= htmlspecialchars($user) ?>"><?= htmlspecialchars($user) ?></option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <label class="form-label">&nbsp;</label>
                        <button type="button" class="btn btn-secondary w-100" onclick="resetFilters()">
                            <i class="bi bi-arrow-clockwise"></i> Сбросить фильтры
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <div class="card mt-4">
            <div class="card-header">
                <h5 class="card-title mb-0">Список книг пользователей (<?= count($user_books) ?>)</h5>
            </div>
            <div class="card-body">
                <div class="table-responsive">
                    <table class="table table-striped table-hover" id="userBooksTable">
                        <thead>
                            <tr>
                                <th>Пользователь</th>
                                <th>Книга</th>
                                <th>Статус</th>
                                <th>Формат</th>
                                <th>Дата добавления</th>
                                <th>Последнее открытие</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($user_books as $user_book): ?>
                                <tr data-status="<?= $user_book['status'] ?>" data-user="<?= htmlspecialchars($user_book['User_login']) ?>">
                                    <td>
                                        <strong><?= htmlspecialchars($user_book['User_login']) ?></strong>
                                    </td>
                                    <td>
                                        <strong><?= htmlspecialchars($user_book['title']) ?></strong>
                                        <?php if (!empty($user_book['author'])): ?>
                                            <br><small class="text-muted"><?= htmlspecialchars($user_book['author']) ?></small>
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <span class="badge bg-<?= $status_colors[$user_book['status']] ?>">
                                            <?= $status_labels[$user_book['status']] ?>
                                        </span>
                                    </td>
                                    <td>
                                        <?php if (!empty($user_book['format'])): ?>
                                            <span class="badge bg-secondary"><?= strtoupper($user_book['format']) ?></span>
                                        <?php else: ?>
                                            <span class="text-muted">—</span>
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <small class="text-muted">
                                            <?= date('d.m.Y H:i', strtotime($user_book['added_at'])) ?>
                                        </small>
                                    </td>
                                    <td>
                                        <?php if (!empty($user_book['last_opened_at'])): ?>
                                            <small class="text-muted">
                                                <?= date('d.m.Y H:i', strtotime($user_book['last_opened_at'])) ?>
                                            </small>
                                        <?php else: ?>
                                            <span class="text-muted">—</span>
                                        <?php endif; ?>
                                    </td>
                                </tr>
                            <?php endforeach; ?>
                            <?php if (empty($user_books)): ?>
                                <tr>
                                    <td colspan="6" class="text-center text-muted py-4">
                                        <i class="bi bi-journal display-4 d-block mb-2"></i>
                                        Пользователи еще не добавили книги в свои библиотеки
                                    </td>
                                </tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <!-- Статистика по статусам -->
        <div class="row mt-4">
            <?php
            $status_counts = [];
            foreach ($user_books as $book) {
                $status = $book['status'];
                $status_counts[$status] = ($status_counts[$status] ?? 0) + 1;
            }
            ?>
            <?php foreach ($status_counts as $status => $count): ?>
                <div class="col-md-2 col-6">
                    <div class="card text-white bg-<?= $status_colors[$status] ?>">
                        <div class="card-body text-center">
                            <h5 class="card-title"><?= $count ?></h5>
                            <p class="card-text"><?= $status_labels[$status] ?></p>
                        </div>
                    </div>
                </div>
            <?php endforeach; ?>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
    <script src="../js/user_books.js">
        
    </script>
</body>
</html>