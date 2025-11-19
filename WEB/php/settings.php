<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

$error = '';
$success = '';

// Сохранение настроек
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['save_settings'])) {
    $settings = [
        'site_name' => trim($_POST['site_name'] ?? ''),
        'admin_email' => trim($_POST['admin_email'] ?? ''),
        'books_per_page' => (int)($_POST['books_per_page'] ?? 20),
        'allow_registration' => isset($_POST['allow_registration']) ? 1 : 0,
        'guest_access' => isset($_POST['guest_access']) ? 1 : 0
    ];
    
    foreach ($settings as $key => $value) {
        $stmt = mysqli_prepare($connect, 
            "INSERT INTO settings (setting_key, setting_value) VALUES (?, ?) 
             ON DUPLICATE KEY UPDATE setting_value = ?");
        mysqli_stmt_bind_param($stmt, 'sss', $key, $value, $value);
        mysqli_stmt_execute($stmt);
        mysqli_stmt_close($stmt);
    }
    
    $success = 'Настройки успешно сохранены';
}

// Получение текущих настроек
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
    'guest_access' => 1
], $current_settings);
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Настройки системы - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
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
</body>
</html>