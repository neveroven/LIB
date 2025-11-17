<?php
session_start();
require_once 'db.php';

// Получаем список книг для гостевого просмотра
$books = [];
$query = "SELECT id, title, author, published_year, description FROM books ORDER BY title";
$result = mysqli_query($connect, $query);
while ($row = mysqli_fetch_assoc($result)) {
    $books[] = $row;
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <title>Каталог книг - Paradise Library</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
</head>
<body>
    <nav class="navbar navbar-expand-lg navbar-light bg-light">
        <div class="container">
            <a class="navbar-brand" href="#">📚 Paradise Library</a>
            <div class="navbar-nav ms-auto">
                <a class="nav-link" href="index.php">Войти</a>
            </div>
        </div>
    </nav>

    <div class="container mt-4">
        <h2>Каталог книг</h2>
        <p class="text-muted">Гостевой доступ. Для добавления книг в библиотеку войдите в систему.</p>
        
        <div class="row mt-4">
            <?php foreach ($books as $book): ?>
            <div class="col-md-4 mb-4">
                <div class="card h-100">
                    <div class="card-body">
                        <h5 class="card-title"><?= htmlspecialchars($book['title']) ?></h5>
                        <h6 class="card-subtitle mb-2 text-muted"><?= htmlspecialchars($book['author'] ?? 'Не указан') ?></h6>
                        <?php if ($book['published_year']): ?>
                            <p class="card-text"><small class="text-muted">Год: <?= $book['published_year'] ?></small></p>
                        <?php endif; ?>
                        <?php if (!empty($book['description'])): ?>
                            <p class="card-text"><?= htmlspecialchars(substr($book['description'], 0, 100)) ?>...</p>
                        <?php endif; ?>
                    </div>
                    <div class="card-footer">
                        <small class="text-muted">Для чтения требуется авторизация</small>
                    </div>
                </div>
            </div>
            <?php endforeach; ?>
        </div>
    </div>
</body>
</html>