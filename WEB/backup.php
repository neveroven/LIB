<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

$error = '';
$success = '';

// Создание резервной копии
if (isset($_POST['create_backup'])) {
    $backup_file = 'backup/backup_' . date('Y-m-d_H-i-s') . '.sql';
    
    // Создаем папку backup если её нет
    if (!is_dir('backup')) {
        mkdir('backup', 0755, true);
    }
    
    // Получаем все таблицы
    $tables = [];
    $result = mysqli_query($connect, "SHOW TABLES");
    while ($row = mysqli_fetch_array($result)) {
        $tables[] = $row[0];
    }
    
    $output = "";
    
    foreach ($tables as $table) {
        // Получаем структуру таблицы
        $result = mysqli_query($connect, "SHOW CREATE TABLE $table");
        $row = mysqli_fetch_array($result);
        $output .= "\n\n" . $row[1] . ";\n\n";
        
        // Получаем данные таблицы
        $result = mysqli_query($connect, "SELECT * FROM $table");
        while ($row = mysqli_fetch_assoc($result)) {
            $output .= "INSERT INTO $table VALUES(";
            $first = true;
            foreach ($row as $value) {
                if (!$first) $output .= ", ";
                $output .= "'" . mysqli_real_escape_string($connect, $value) . "'";
                $first = false;
            }
            $output .= ");\n";
        }
    }
    
    if (file_put_contents($backup_file, $output)) {
        $success = "Резервная копия создана: " . basename($backup_file);
    } else {
        $error = "Ошибка при создании резервной копии";
    }
}

// Получение списка резервных копий
$backups = [];
if (is_dir('backup')) {
    $files = scandir('backup');
    foreach ($files as $file) {
        if (pathinfo($file, PATHINFO_EXTENSION) === 'sql') {
            $backups[] = [
                'name' => $file,
                'size' => filesize('backup/' . $file),
                'date' => date('d.m.Y H:i:s', filemtime('backup/' . $file))
            ];
        }
    }
    // Сортируем по дате (новые сверху)
    usort($backups, function($a, $b) {
        return filemtime('backup/' . $b['name']) - filemtime('backup/' . $b['name']);
    });
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Резервное копирование - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
    <div class="container-fluid py-3">
        <h2><i class="bi bi-database"></i> Резервное копирование</h2>

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

        <div class="row mt-4">
            <div class="col-md-6">
                <div class="card">
                    <div class="card-header">
                        <h5 class="card-title mb-0"><i class="bi bi-plus-circle"></i> Создание резервной копии</h5>
                    </div>
                    <div class="card-body">
                        <p>Создайте полную резервную копию базы данных библиотеки.</p>
                        <form method="POST">
                            <button type="submit" name="create_backup" class="btn btn-success">
                                <i class="bi bi-database-add"></i> Создать резервную копию
                            </button>
                        </form>
                    </div>
                </div>

                <div class="card mt-4">
                    <div class="card-header">
                        <h5 class="card-title mb-0"><i class="bi bi-info-circle"></i> Информация о базе данных</h5>
                    </div>
                    <div class="card-body">
                        <?php
                        $db_info = [
                            'Имя базы данных' => 'paradise',
                            'Размер базы данных' => '~' . round(array_sum(array_column($backups, 'size')) / 1024 / 1024, 2) . ' MB',
                            'Количество таблиц' => mysqli_num_rows(mysqli_query($connect, "SHOW TABLES")),
                            'Всего резервных копий' => count($backups)
                        ];
                        ?>
                        <div class="list-group list-group-flush">
                            <?php foreach ($db_info as $label => $value): ?>
                                <div class="list-group-item d-flex justify-content-between align-items-center">
                                    <?= $label ?>
                                    <span class="badge bg-primary rounded-pill"><?= $value ?></span>
                                </div>
                            <?php endforeach; ?>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-md-6">
                <div class="card">
                    <div class="card-header">
                        <h5 class="card-title mb-0"><i class="bi bi-archive"></i> Существующие резервные копии</h5>
                    </div>
                    <div class="card-body">
                        <?php if (!empty($backups)): ?>
                            <div class="table-responsive">
                                <table class="table table-striped">
                                    <thead>
                                        <tr>
                                            <th>Имя файла</th>
                                            <th>Размер</th>
                                            <th>Дата создания</th>
                                            <th>Действия</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <?php foreach ($backups as $backup): ?>
                                            <tr>
                                                <td>
                                                    <i class="bi bi-file-earmark-text"></i>
                                                    <?= htmlspecialchars($backup['name']) ?>
                                                </td>
                                                <td><?= round($backup['size'] / 1024, 2) ?> KB</td>
                                                <td><?= $backup['date'] ?></td>
                                                <td>
                                                    <a href="backup/<?= urlencode($backup['name']) ?>" 
                                                       class="btn btn-sm btn-primary" download>
                                                        <i class="bi bi-download"></i>
                                                    </a>
                                                    <a href="?delete_backup=<?= urlencode($backup['name']) ?>" 
                                                       class="btn btn-sm btn-danger"
                                                       onclick="return confirm('Удалить резервную копию?')">
                                                        <i class="bi bi-trash"></i>
                                                    </a>
                                                </td>
                                            </tr>
                                        <?php endforeach; ?>
                                    </tbody>
                                </table>
                            </div>
                        <?php else: ?>
                            <p class="text-muted text-center py-4">
                                <i class="bi bi-inbox display-4 d-block mb-2"></i>
                                Резервные копии отсутствуют
                            </p>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
        </div>

        <!-- Предупреждение -->
        <div class="alert alert-warning mt-4">
            <h5><i class="bi bi-exclamation-triangle"></i> Важно!</h5>
            <ul class="mb-0">
                <li>Регулярно создавайте резервные копии базы данных</li>
                <li>Храните копии в безопасном месте</li>
                <li>Перед обновлением системы обязательно создавайте резервную копию</li>
            </ul>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>