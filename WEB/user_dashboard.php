<?php
session_start();
require_once 'db.php';

// Проверка авторизации
if (empty($_SESSION['user_id']) || $_SESSION['is_admin']) {
    header('Location: index.php');
    exit();
}

$user_id = $_SESSION['user_id'];
$username = $_SESSION['username'];

// Получаем книги пользователя
$user_books = [];
$query = "SELECT b.id, b.title, b.author, ub.status, ub.added_at 
          FROM user_books ub 
          JOIN books b ON ub.book_id = b.id 
          WHERE ub.user_id = ? 
          ORDER BY ub.added_at DESC";
$stmt = mysqli_prepare($connect, $query);
mysqli_stmt_bind_param($stmt, 'i', $user_id);
mysqli_stmt_execute($stmt);
$result = mysqli_stmt_get_result($stmt);

while ($row = mysqli_fetch_assoc($result)) {
    $user_books[] = $row;
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <title>Личный кабинет - Paradise Library</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
</head>
<body>
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
        <div class="container">
            <a class="navbar-brand" href="#">📚 Paradise Library</a>
            <div class="navbar-nav ms-auto">
                <span class="navbar-text me-3">Привет, <?= htmlspecialchars($username) ?>!</span>
                <a class="nav-link" href="logout.php">Выйти</a>
            </div>
        </div>
    </nav>

    <div class="container mt-4">
        <h2>Моя библиотека</h2>
        
        <div class="row mt-4">
            <div class="col-md-3">
                <div class="card">
                    <div class="card-body">
                        <h5 class="card-title">Мои книги</h5>
                        <p class="card-text"><?= count($user_books) ?> книг в библиотеке</p>
                        <a href="catalog.php" class="btn btn-primary">Добавить книги</a>
                    </div>
                </div>
            </div>
            
            <div class="col-md-9">
                <div class="card">
                    <div class="card-header">
                        <h5 class="card-title mb-0">Список моих книг</h5>
                    </div>
                    <div class="card-body">
                        <?php if (!empty($user_books)): ?>
                            <div class="table-responsive">
                                <table class="table table-striped">
                                    <thead>
                                        <tr>
                                            <th>Название</th>
                                            <th>Автор</th>
                                            <th>Статус</th>
                                            <th>Дата добавления</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <?php foreach ($user_books as $book): ?>
                                        <tr>
                                            <td><?= htmlspecialchars($book['title']) ?></td>
                                            <td><?= htmlspecialchars($book['author'] ?? 'Не указан') ?></td>
                                            <td>
                                                <span class="badge bg-<?= 
                                                    $book['status'] === 'reading' ? 'primary' : 
                                                    ($book['status'] === 'finished' ? 'success' : 'secondary')
                                                ?>">
                                                    <?= $book['status'] ?>
                                                </span>
                                            </td>
                                            <td><?= date('d.m.Y H:i', strtotime($book['added_at'])) ?></td>
                                        </tr>
                                        <?php endforeach; ?>
                                    </tbody>
                                </table>
                            </div>
                        <?php else: ?>
                            <p class="text-muted">У вас пока нет книг в библиотеке.</p>
                            <a href="catalog.php" class="btn btn-primary">Перейти в каталог</a>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>