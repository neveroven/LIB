<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['user_id'])) {
    header('Location: ../index.php');
    exit();
}

$user_id = $_SESSION['user_id'];

// Получаем все книги, которые еще не добавлены пользователю
$available_books = [];
$query = "
    SELECT 
        b.*,
        (
            SELECT cover_image_uri 
            FROM book_files bf 
            WHERE bf.book_id = b.id 
              AND bf.cover_image_uri IS NOT NULL 
              AND bf.cover_image_uri <> ''
            ORDER BY bf.id ASC 
            LIMIT 1
        ) AS cover_image_uri
    FROM books b 
    WHERE b.id NOT IN (SELECT book_id FROM user_books WHERE user_id = ?)
    ORDER BY b.title
";
$stmt = mysqli_prepare($connect, $query);
mysqli_stmt_bind_param($stmt, 'i', $user_id);
mysqli_stmt_execute($stmt);
$result = mysqli_stmt_get_result($stmt);

while ($row = mysqli_fetch_assoc($result)) {
    $available_books[] = $row;
}

// Получаем путь к папке DB из настроек
$db_folder_path = '';
$settings_res = mysqli_query($connect, "SELECT setting_value FROM settings WHERE setting_key = 'db_folder_path' LIMIT 1");
if ($settings_res && $srow = mysqli_fetch_assoc($settings_res)) {
    $db_folder_path = trim($srow['setting_value'] ?? '');
}

function build_cover_url($cover_uri, $db_folder_path) {
    if (empty($cover_uri)) return '';
    $coverUriNormalized = ltrim(str_replace('\\', '/', $cover_uri), '/');
    $dbBase = rtrim(str_replace('\\', '/', $db_folder_path), '/');

    // Абсолютный путь к файлу
    $abs = $cover_uri;
    if ($dbBase !== '') {
        $abs = $dbBase . '/' . $coverUriNormalized;
    }
    // если указали абсолютный путь в БД
    if (file_exists($cover_uri)) {
        $abs = $cover_uri;
    }

    // Проверяем существование
    if (!file_exists($abs)) {
        return '';
    }

    // Кодируем путь для прокси показа
    $token = urlencode(base64_encode($abs));
    return "cover_image.php?p={$token}";
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
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Каталог книг - Paradise Library</title>
    <link rel="stylesheet" href="../css/main.css">
    <link rel="stylesheet" href="../css/catalog.css">
</head>
<body>
    <div class="app-container">
        <!-- Left Sidebar -->
        <div class="left-panel">
            <div class="library-title" onclick="window.location.href='user_dashboard.php'">📚 Paradise</div>
            
            <div class="nav-buttons">
                <button class="nav-button" data-href="user_dashboard.php">📖 Книги</button>
                <button class="nav-button active" data-href="catalog.php">📚 Каталог книг</button>
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
                <button class="back-button" onclick="window.location.href='user_dashboard.php'">← Вернуться к библиотеке</button>
                <button class="exit-button" onclick="window.location.href='logout.php'">Выход</button>
            </div>
            
            <!-- Main Content -->
            <div class="main-content">
                <!-- Header -->
                <div class="panel">
                    <h1 class="panel-title">📚 Каталог книг</h1>
                    <p class="panel-subtitle">Доступные книги для добавления в вашу библиотеку</p>
                </div>
                
                <!-- Search -->
                <div class="panel">
                    <div style="display: flex; align-items: center; gap: 10px;">
                        <label style="font-size: 14px; font-weight: 600;">🔍 Поиск:</label>
                        <input type="text" id="catalogSearch" class="form-control" style="flex: 1;" placeholder="Введите название или автора...">
                    </div>
                </div>
                
                <!-- Books Grid -->
                <?php if (!empty($available_books)): ?>
                    <div class="books-grid">
                        <?php foreach ($available_books as $book): ?>
                        <?php $coverUrl = build_cover_url($book['cover_image_uri'] ?? '', $db_folder_path); ?>
                        <div class="book-card">
                            <div class="book-cover" style="display: flex; align-items: center; justify-content: center; color: rgba(0,0,0,0.3); font-size: 48px; overflow:hidden;">
                                <?php if ($coverUrl): ?>
                                    <img src="<?= htmlspecialchars($coverUrl) ?>" alt="Обложка" style="width:100%; height:100%; object-fit:cover;">
                                <?php else: ?>
                                    📖
                                <?php endif; ?>
                            </div>
                            <div class="book-info">
                                <div class="book-card-title"><?= htmlspecialchars($book['title']) ?></div>
                                <div class="book-card-author"><?= htmlspecialchars($book['author'] ?? 'Не указан') ?></div>
                                <?php if ($book['published_year']): ?>
                                    <div style="font-size: 10px; opacity: 0.6; text-align: center; margin-top: 5px;">
                                        Год: <?= $book['published_year'] ?>
                                    </div>
                                <?php endif; ?>
                            </div>
                            <form method="POST" style="margin-top: auto;">
                                <input type="hidden" name="book_id" value="<?= $book['id'] ?>">
                                <button type="submit" name="add_to_library" class="btn btn-primary" style="width: 100%; height: 35px; font-size: 11px;">
                                    ➕ Добавить в библиотеку
                                </button>
                            </form>
                        </div>
                        <?php endforeach; ?>
                    </div>
                <?php else: ?>
                    <div class="panel">
                        <div style="text-align: center; padding: 40px;">
                            <h3 style="margin-bottom: 15px;">Все книги уже добавлены в вашу библиотеку!</h3>
                            <p style="opacity: 0.7; margin-bottom: 20px;">Перейдите в личный кабинет для просмотра ваших книг.</p>
                            <a href="user_dashboard.php" class="btn btn-primary">Перейти в библиотеку</a>
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
    <script src="../js/catalog.js"></script>
</body>
</html>