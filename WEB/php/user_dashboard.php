<?php
session_start();
require_once 'db.php';

// Проверка авторизации
if (empty($_SESSION['user_id']) || $_SESSION['is_admin']) {
    header('Location: ../index.php');
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
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Личный кабинет - Paradise Library</title>
    <link rel="stylesheet" href="../css/main.css">
    <link rel="stylesheet" href="../css/user_dashboard.css">
</head>
<body>
    <div class="app-container">
        <!-- Left Sidebar -->
        <div class="left-panel">
            <div class="library-title" onclick="window.location.href='user_dashboard.php'">📚 Paradise</div>
            
            <div class="nav-buttons">
                <button class="nav-button active" data-href="user_dashboard.php">📖 Книги</button>
                <button class="nav-button" data-href="catalog.php">📚 Каталог книг</button>
            </div>
            
            <div class="settings-buttons">
                <button class="nav-button" data-href="settings.php">⚙️ Настройки</button>
                <button class="nav-button" onclick="window.location.href='logout.php'">🚪 Выход</button>
            </div>
        </div>
        
        <!-- Right Panel -->
        <div class="right-panel">
            <!-- Top Bar -->
            <div class="top-bar">
                <div></div>
                <div style="display: flex; align-items: center; gap: 15px;">
                    <span>Привет, <?= htmlspecialchars($username) ?>!</span>
                    <button class="exit-button" onclick="window.location.href='logout.php'">Выход</button>
                </div>
            </div>
            
            <!-- Main Content -->
            <div class="main-content">
                <!-- Welcome Panel -->
                <div class="panel">
                    <h1 class="panel-title">Добро пожаловать в Paradise!</h1>
                </div>
                
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
                    <!-- Books List -->
                    <div class="panel">
                        <h2 class="panel-title" style="font-size: 18px; margin-bottom: 15px;">📚 Список книг</h2>
                        
                        <?php if (!empty($user_books)): ?>
                            <ul class="books-list">
                                <?php foreach ($user_books as $book): ?>
                                <li class="book-item" data-book-id="<?= $book['id'] ?>">
                                    <div class="book-title"><?= htmlspecialchars($book['title']) ?></div>
                                    <div class="book-author"><?= htmlspecialchars($book['author'] ?? 'Не указан') ?></div>
                                    <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 5px;">
                                        <span class="badge badge-<?= 
                                            $book['status'] === 'reading' ? 'primary' : 
                                            ($book['status'] === 'finished' ? 'success' : 'secondary')
                                        ?>">
                                            <?= $book['status'] === 'reading' ? 'Читаю' : 
                                                ($book['status'] === 'finished' ? 'Прочитано' : 'Запланировано') ?>
                                        </span>
                                        <span style="font-size: 10px; opacity: 0.6;">
                                            <?= date('d.m.Y H:i', strtotime($book['added_at'])) ?>
                                        </span>
                                    </div>
                                </li>
                                <?php endforeach; ?>
                            </ul>
                        <?php else: ?>
                            <p style="text-align: center; opacity: 0.6; margin: 20px 0;">Книги ещё не добавлены</p>
                            <a href="catalog.php" class="btn btn-primary" style="width: 100%;">Перейти в каталог</a>
                        <?php endif; ?>
                    </div>
                    
                    <!-- Statistics -->
                    <div class="panel">
                        <h2 class="panel-title" style="font-size: 18px; margin-bottom: 15px;">📊 Статистика библиотеки</h2>
                        <div class="statistics-panel">
                            <div class="stat-item">Всего книг: <?= count($user_books) ?></div>
                            <div class="stat-item">
                                Читаю: <?= count(array_filter($user_books, fn($b) => $b['status'] === 'reading')) ?>
                            </div>
                            <div class="stat-item">
                                Прочитано: <?= count(array_filter($user_books, fn($b) => $b['status'] === 'finished')) ?>
                            </div>
                            <div class="stat-item">
                                Запланировано: <?= count(array_filter($user_books, fn($b) => $b['status'] === 'planned')) ?>
                            </div>
                            <?php if (!empty($user_books)): ?>
                                <div class="stat-item" style="margin-top: 15px; opacity: 0.7;">
                                    Последняя добавлена: <?= date('d.m.Y', strtotime($user_books[0]['added_at'])) ?>
                                </div>
                            <?php endif; ?>
                        </div>
                    </div>
                </div>
                
                <!-- Quick Actions -->
                <div class="panel">
                    <h2 class="panel-title" style="font-size: 18px; margin-bottom: 15px;">⚡ Быстрые действия</h2>
                    <div class="quick-actions">
                        <a href="catalog.php" class="btn quick-action-btn">➕ Добавить книгу</a>
                        <button class="btn quick-action-btn" onclick="alert('Функция в разработке')">📋 Отчёт</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <script src="../js/main.js"></script>
    <script src="../js/user_dashboard.js"></script>
</body>
</html>