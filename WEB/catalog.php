<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['user_id'])) {
    header('Location: index.php');
    exit();
}

$user_id = $_SESSION['user_id'];

// Получаем все книги, которые еще не добавлены пользователю
$available_books = [];
$query = "SELECT b.* FROM books b 
          WHERE b.id NOT IN (SELECT book_id FROM user_books WHERE user_id = ?)
          ORDER BY b.title";
$stmt = mysqli_prepare($connect, $query);
mysqli_stmt_bind_param($stmt, 'i', $user_id);
mysqli_stmt_execute($stmt);
$result = mysqli_stmt_get_result($stmt);

while ($row = mysqli_fetch_assoc($result)) {
    $available_books[] = $row;
}

// Обработка добавления книги в библиотеку
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['add_to_library'])) {
    $book_id = (int)$_POST['book_id'];
    
    $stmt = mysqli_prepare($connect, "INSERT INTO user_books (user_id, book_id, status) VALUES (?, ?, 'planned')");
    mysqli_stmt_bind_param($stmt, 'ii', $user_id, $book_id);
    
    if (mysqli_stmt_execute($stmt)) {
        header('Location: user_dashboard.php');
        exit();
    }
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
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
        <div class="container">
            <a class="navbar-brand" href="#">📚 Paradise Library</a>
            <div class="navbar-nav ms-auto">
                <a class="nav-link" href="user_dashboard.php">Моя библиотека</a>
                <a class="nav-link" href="logout.php">Выйти</a>
            </div>
        </div>
    </nav>

    <div class="container mt-4">
        <h2>Каталог книг</h2>
        
        <div class="row mt-4">
            <?php foreach ($available_books as $book): ?>
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
                        <form method="POST">
                            <input type="hidden" name="book_id" value="<?= $book['id'] ?>">
                            <button type="submit" name="add_to_library" class="btn btn-success btn-sm">Добавить в библиотеку</button>
                        </form>
                    </div>
                </div>
            </div>
            <?php endforeach; ?>
            
            <?php if (empty($available_books)): ?>
            <div class="col-12">
                <div class="alert alert-info">
                    <h5>Все книги уже добавлены в вашу библиотеку!</h5>
                    <p class="mb-0">Перейдите в <a href="user_dashboard.php">личный кабинет</a> для просмотра ваших книг.</p>
                </div>
            </div>
            <?php endif; ?>
        </div>
    </div>
</body>
</html>