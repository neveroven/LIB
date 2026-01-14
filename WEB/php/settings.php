<?php
session_start();
require_once 'db.php';

// Определяем режим: admin или user
$is_admin = !empty($_SESSION['is_admin']) && $_SESSION['is_admin'];

$error = '';
$success = '';

// --- ОБЩИЕ СИСТЕМНЫЕ НАСТРОЙКИ (для админа) ---
if ($is_admin && $_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['save_settings'])) {
    $settings = [
        'site_name' => trim($_POST['site_name'] ?? ''),
        'admin_email' => trim($_POST['admin_email'] ?? ''),
        'books_per_page' => (int)($_POST['books_per_page'] ?? 20),
        'allow_registration' => isset($_POST['allow_registration']) ? 1 : 0,
        'guest_access' => isset($_POST['guest_access']) ? 1 : 0,
        // новый параметр: путь к папке DB
        'db_folder_path' => trim($_POST['db_folder_path'] ?? '')
    ];
    
    foreach ($settings as $key => $value) {
        $value_str = (string)$value;
        $stmt = mysqli_prepare($connect, 
            "INSERT INTO settings (setting_key, setting_value) VALUES (?, ?) 
             ON DUPLICATE KEY UPDATE setting_value = ?");
        mysqli_stmt_bind_param($stmt, 'sss', $key, $value_str, $value_str);
        mysqli_stmt_execute($stmt);
        mysqli_stmt_close($stmt);
    }
    
    $success = 'Системные настройки успешно сохранены';
}

// Получение текущих системных настроек
$current_settings = [];
$result = mysqli_query($connect, "SELECT setting_key, setting_value FROM settings");
while ($row = mysqli_fetch_assoc($result)) {
    $current_settings[$row['setting_key']] = $row['setting_value'];
}

// Значения по умолчанию
$settings = array_merge([
    'site_name' => 'Paradise Library',
    'admin_email' => 'admin@example.com',
    'books_per_page' => 20,
    'allow_registration' => 1,
    'guest_access' => 1,
    'db_folder_path' => ''
], $current_settings);

// --- ПОЛЬЗОВАТЕЛЬСКИЕ НАСТРОЙКИ (для обычных пользователей) ---
$user_id = isset($_SESSION['user_id']) ? (int)$_SESSION['user_id'] : 0;
$user_prefs = [
    'theme' => 'light',
    'font_size' => 'medium'
];

if ($user_id > 0) {
    // читаем из session (просто и без отдельной таблицы) или используем дефолты
    if (!empty($_SESSION['user_theme'])) {
        $user_prefs['theme'] = $_SESSION['user_theme'];
    }
    if (!empty($_SESSION['user_font_size'])) {
        $user_prefs['font_size'] = $_SESSION['user_font_size'];
    }

    if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['save_user_settings']) && !$is_admin) {
        $theme = $_POST['theme'] === 'dark' ? 'dark' : 'light';
        $font_size = in_array($_POST['font_size'], ['small', 'medium', 'large']) ? $_POST['font_size'] : 'medium';

        $_SESSION['user_theme'] = $theme;
        $_SESSION['user_font_size'] = $font_size;

        $user_prefs['theme'] = $theme;
        $user_prefs['font_size'] = $font_size;

        $success = 'Ваши настройки сохранены (для этого браузера и сессии)';
    }
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= $is_admin ? 'Настройки системы - Paradise Library Admin' : 'Настройки - Paradise Library' ?></title>
    <?php if ($is_admin): ?>
        <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
        <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
    <?php else: ?>
        <link rel="stylesheet" href="../css/main.css">
        <link rel="stylesheet" href="../css/user_dashboard.css">
    <?php endif; ?>
</head>
<body>
<?php if ($is_admin): ?>
    <div class="container-fluid py-3">
        <h2><i class="bi bi-gear"></i> Настройки системы</h2>

        <?php if ($success): ?>
            <div class="alert alert-success alert-dismissible fade show" role="alert">
                <?= htmlspecialchars($success) ?>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        <?php endif; ?>

        <?php if ($error): ?>
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <?= htmlspecialchars($error) ?>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        <?php endif; ?>

        <form method="POST">
            <div class="row mt-4">
                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header">
                            <h5 class="card-title mb-0"><i class="bi bi-info-circle"></i> Основные настройки</h5>
                        </div>
                        <div class="card-body">
                            <div class="mb-3">
                                <label class="form-label">Название сайта</label>
                                <input type="text" class="form-control" name="site_name" 
                                       value="<?= htmlspecialchars($settings['site_name']) ?>" required>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Email администратора</label>
                                <input type="email" class="form-control" name="admin_email" 
                                       value="<?= htmlspecialchars($settings['admin_email']) ?>" required>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Книг на странице</label>
                                <input type="number" class="form-control" name="books_per_page" 
                                       value="<?= $settings['books_per_page'] ?>" min="5" max="100" required>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-6">
                    <div class="card">
                        <div class="card-header">
                            <h5 class="card-title mb-0"><i class="bi bi-shield-check"></i> Настройки доступа</h5>
                        </div>
                        <div class="card-body">
                            <div class="form-check form-switch mb-3">
                                <input class="form-check-input" type="checkbox" name="allow_registration" 
                                       id="allow_registration" <?= $settings['allow_registration'] ? 'checked' : '' ?>>
                                <label class="form-check-label" for="allow_registration">
                                    Разрешить регистрацию новых пользователей
                                </label>
                            </div>
                            <div class="form-check form-switch mb-3">
                                <input class="form-check-input" type="checkbox" name="guest_access" 
                                       id="guest_access" <?= $settings['guest_access'] ? 'checked' : '' ?>>
                                <label class="form-check-label" for="guest_access">
                                    Разрешить гостевой доступ к каталогу
                                </label>
                            </div>
                        </div>
                    </div>

                    <div class="card mt-4">
                        <div class="card-header">
                            <h5 class="card-title mb-0"><i class="bi bi-database"></i> Информация о системе</h5>
                        </div>
                        <div class="card-body">
                            <?php
                            $stats = [
                                'Версия PHP' => PHP_VERSION,
                                'Версия MySQL' => mysqli_get_server_info($connect),
                                'Книг в базе' => mysqli_fetch_assoc(mysqli_query($connect, "SELECT COUNT(*) as count FROM books"))['count'],
                                'Пользователей' => mysqli_fetch_assoc(mysqli_query($connect, "SELECT COUNT(*) as count FROM users"))['count'],
                                'Файлов книг' => mysqli_fetch_assoc(mysqli_query($connect, "SELECT COUNT(*) as count FROM book_files"))['count']
                            ];
                            ?>
                            <div class="list-group list-group-flush">
                                <?php foreach ($stats as $label => $value): ?>
                                    <div class="list-group-item d-flex justify-content-between align-items-center">
                                        <?= $label ?>
                                        <span class="badge bg-primary rounded-pill"><?= $value ?></span>
                                    </div>
                                <?php endforeach; ?>
                            </div>
                        </div>
                    </div>

                    <div class="card mt-4">
                        <div class="card-header">
                            <h5 class="card-title mb-0"><i class="bi bi-folder"></i> Путь к папке DB</h5>
                        </div>
                        <div class="card-body">
                            <div class="mb-2">
                                <label class="form-label">Базовый путь к папке DB на сервере</label>
                                <input type="text"
                                       class="form-control"
                                       name="db_folder_path"
                                       value="<?= htmlspecialchars($settings['db_folder_path']) ?>"
                                       placeholder="Например: C:\Users\ПК-1\Documents\GitHub\LIB\DB">
                            </div>
                            <small class="text-muted">
                                Этот путь используется для поиска файлов книг и обложек.
                                В базе указываются относительные пути внутри этой папки.
                            </small>
                        </div>
                    </div>
                </div>
            </div>

            <div class="row mt-4">
                <div class="col-12">
                    <div class="card">
                        <div class="card-body text-center">
                            <button type="submit" name="save_settings" class="btn btn-success btn-lg">
                                <i class="bi bi-check-circle"></i> Сохранить настройки
                            </button>
                            <a href="index_admin.php" class="btn btn-secondary btn-lg ms-2">
                                <i class="bi bi-arrow-left"></i> Назад в панель управления
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </form>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
<?php else: ?>
    <!-- Пользовательские настройки -->
    <div class="app-container">
        <div class="left-panel">
            <div class="library-title" onclick="window.location.href='user_dashboard.php'">📚 Paradise</div>
            <div class="nav-buttons">
                <button class="nav-button" data-href="user_dashboard.php">📖 Книги</button>
                <button class="nav-button" data-href="catalog.php">📚 Каталог книг</button>
            </div>
            <div class="settings-buttons">
                <button class="nav-button active" data-href="settings.php">⚙️ Настройки</button>
                <button class="nav-button" onclick="window.location.href='logout.php'">🚪 Выход</button>
            </div>
        </div>
        <div class="right-panel">
            <div class="top-bar">
                <button class="back-button" onclick="window.location.href='user_dashboard.php'">← Вернуться к библиотеке</button>
                <button class="exit-button" onclick="window.location.href='logout.php'">Выход</button>
            </div>
            <div class="main-content">
                <div class="panel">
                    <h1 class="panel-title" style="font-size: 22px;">⚙️ Личные настройки</h1>
                    <p class="panel-subtitle">Эти настройки применяются в этом браузере и сохраняются в рамках сессии.</p>
                    <?php if ($success): ?>
                        <div class="alert alert-success"><?= htmlspecialchars($success) ?></div>
                    <?php endif; ?>
                    <form method="POST">
                        <div class="form-group">
                            <label class="form-label">Тема оформления</label>
                            <select name="theme" class="form-control">
                                <option value="light" <?= $user_prefs['theme'] === 'light' ? 'selected' : '' ?>>Светлая</option>
                                <option value="dark" <?= $user_prefs['theme'] === 'dark' ? 'selected' : '' ?>>Тёмная</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label class="form-label">Размер интерфейса</label>
                            <select name="font_size" class="form-control">
                                <option value="small" <?= $user_prefs['font_size'] === 'small' ? 'selected' : '' ?>>Мелкий</option>
                                <option value="medium" <?= $user_prefs['font_size'] === 'medium' ? 'selected' : '' ?>>Обычный</option>
                                <option value="large" <?= $user_prefs['font_size'] === 'large' ? 'selected' : '' ?>>Крупный</option>
                            </select>
                        </div>
                        <button type="submit" name="save_user_settings" class="btn btn-primary" style="margin-top: 15px;">
                            Сохранить настройки
                        </button>
                    </form>
                </div>
            </div>
        </div>
    </div>
    <script src="../js/main.js"></script>
    <script>
        // Синхронизуем только что сохранённые настройки с фронтендом
        (function() {
            const phpTheme = '<?= $user_prefs['theme'] === 'dark' ? 'dark' : 'light' ?>';
            const phpFont = '<?= in_array($user_prefs['font_size'], ['small','medium','large']) ? $user_prefs['font_size'] : 'medium' ?>';

            localStorage.setItem('theme', phpTheme);
            if (phpTheme === 'dark') {
                document.body.classList.add("dark-theme");
            } else {
                document.body.classList.remove("dark-theme");
            }

            let fontSize = "16px";
            if (phpFont === "small") fontSize = "14px";
            if (phpFont === "large") fontSize = "18px";
            document.body.style.fontSize = fontSize;
        })();
    </script>
<?php endif; ?>
</body>
</html>