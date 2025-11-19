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
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Каталог книг - Paradise Library</title>
    <link rel="stylesheet" href="../css/main.css">
    <link rel="stylesheet" href="../css/guest.css">
</head>
<body>
    <div class="app-container">
        <!-- Left Sidebar -->
        <div class="left-panel">
            <div class="library-title" onclick="window.location.href='guest_dashboard.php'">📚 Paradise</div>
            
            <div class="nav-buttons">
                <button class="nav-button active" data-href="guest_dashboard.php">📚 Каталог книг</button>
            </div>
            
            <div class="settings-buttons">
                <button class="nav-button" onclick="window.location.href='../index.php'">🔐 Войти</button>
            </div>
        </div>
        
        <!-- Right Panel -->
        <div class="right-panel">
            <!-- Top Bar -->
            <div class="top-bar">
                <div></div>
                <button class="exit-button" onclick="window.location.href='../index.php'">Войти</button>
            </div>
            
            <!-- Main Content -->
            <div class="main-content">
                <!-- Header -->
                <div class="panel">
                    <h1 class="panel-title">📚 Каталог книг</h1>
                    <p class="panel-subtitle">Гостевой доступ. Для добавления книг в библиотеку войдите в систему.</p>
                </div>
                
                <!-- Search -->
                <div class="panel">
                    <div style="display: flex; align-items: center; gap: 10px;">
                        <label style="font-size: 14px; font-weight: 600;">🔍 Поиск:</label>
                        <input type="text" id="catalogSearch" class="form-control" style="flex: 1;" placeholder="Введите название или автора...">
                    </div>
                </div>
                
                <!-- Books Grid -->
                <?php if (!empty($books)): ?>
                    <div class="books-grid">
                        <?php foreach ($books as $book): ?>
                        <div class="book-card">
                            <div class="book-cover" style="display: flex; align-items: center; justify-content: center; color: rgba(0,0,0,0.3); font-size: 48px;">
                                📖
                            </div>
                            <div class="book-info">
                                <div class="book-card-title"><?= htmlspecialchars($book['title']) ?></div>
                                <div class="book-card-author"><?= htmlspecialchars($book['author'] ?? 'Не указан') ?></div>
                                <?php if ($book['published_year']): ?>
                                    <div style="font-size: 10px; opacity: 0.6; text-align: center; margin-top: 5px;">
                                        Год: <?= $book['published_year'] ?>
                                    </div>
                                <?php endif; ?>
                                <?php if (!empty($book['description'])): ?>
                                    <div style="font-size: 10px; opacity: 0.7; text-align: center; margin-top: 5px; max-height: 40px; overflow: hidden;">
                                        <?= htmlspecialchars(substr($book['description'], 0, 100)) ?>...
                                    </div>
                                <?php endif; ?>
                            </div>
                            <div style="text-align: center; padding: 10px; font-size: 10px; opacity: 0.6;">
                                Для чтения требуется авторизация
                            </div>
                        </div>
                        <?php endforeach; ?>
                    </div>
                <?php else: ?>
                    <div class="panel">
                        <div style="text-align: center; padding: 40px; opacity: 0.6;">
                            Нет доступных книг в каталоге
                        </div>
                    </div>
                <?php endif; ?>
                
                <!-- No results message -->
                <div id="noCatalogBooks" style="display: none; text-align: center; padding: 40px; opacity: 0.6;">
                    Нет доступных книг в каталоге
                </div>
            </div>
        </div>
    </div>
    
    <script src="../js/main.js"></script>
    <script src="../js/guest.js"></script>
</body>
</html>