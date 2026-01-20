<?php
session_start();
include("db.php");

// Проверка авторизации администратора
if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}
?>
<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Панель администратора - Paradise Library</title>
    <link rel="stylesheet" href="../css/main.css">
    <link rel="stylesheet" href="../css/admin.css">
</head>
<body>
    <div class="app-container">
        <!-- Left Sidebar -->
        <div class="left-panel">
            <div class="library-title" onclick="window.location.href='index_admin.php'">📚 Paradise</div>
            
            <div class="nav-buttons">
                <button class="nav-button active" onclick="loadFrame('admin_main_page_support.php')">🏠 Главная панель</button>
                
                <div style="margin-top: 10px; padding-top: 10px; border-top: 1px solid var(--button-border);">
                    <div style="font-size: 11px; opacity: 0.7; padding: 5px 0; text-transform: uppercase;">Управление контентом</div>
                    <button class="nav-button" onclick="loadFrame('books.php')">📚 Книги</button>
                    <button class="nav-button" onclick="loadFrame('book_files.php')">📄 Файлы книг</button>
                    <button class="nav-button" onclick="loadFrame('users.php')">👥 Пользователи</button>
                </div>
                
                <div style="margin-top: 10px; padding-top: 10px; border-top: 1px solid var(--button-border);">
                    <div style="font-size: 11px; opacity: 0.7; padding: 5px 0; text-transform: uppercase;">Статистика и отчеты</div>
                    <button class="nav-button" onclick="loadFrame('reading_statistics.php')">📊 Статистика чтения</button>
                    <button class="nav-button" onclick="loadFrame('user_books.php')">📖 Книги пользователей</button>
                    <button class="nav-button" onclick="loadFrame('reading_progress.php')">📈 Прогресс чтения</button>
                    <button class="nav-button" onclick="loadFrame('reports_popular_books.php')">📚 Отчёт: Популярные книги</button>
                    <button class="nav-button" onclick="loadFrame('reports_admin_books.php')">📖 Отчёт: Книги администратора</button>
                    <button class="nav-button" onclick="loadFrame('reports_users.php')">👥 Отчёт: Пользователи</button>
                </div>
                
                <div style="margin-top: 10px; padding-top: 10px; border-top: 1px solid var(--button-border);">
                    <div style="font-size: 11px; opacity: 0.7; padding: 5px 0; text-transform: uppercase;">Система</div>
                    <button class="nav-button" onclick="loadFrame('settings.php')">⚙️ Настройки</button>
                    <button class="nav-button" onclick="loadFrame('backup.php')">💾 Резервное копирование</button>
                </div>
            </div>
            
            <div class="settings-buttons">
                <button class="nav-button" onclick="window.location.href='logout.php'">🚪 Выход</button>
            </div>
        </div>
        
        <!-- Right Panel -->
        <div class="right-panel">
            <!-- Top Bar -->
            <div class="top-bar">
                <div>
                    <h1 style="font-size: 20px; font-weight: bold; margin: 0;">Панель управления библиотекой</h1>
                </div>
                <div style="display: flex; align-items: center; gap: 15px;">
                    <span><?php echo $_SESSION['username'] ?? 'Администратор'; ?></span>
                    
                </div>
            </div>
            
            <!-- Main Content -->
            <div class="main-content">
                <iframe name="adminFrame" id="adminFrame" class="admin-iframe" src="admin_main_page_support.php"></iframe>
            </div>
        </div>
    </div>
    
    <script src="../js/main.js"></script>
    <script src="../js/admin.js"></script>
</body>
</html>