<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Получение прогресса чтения
$reading_progress = [];
$query = "
    SELECT rp.*, u.User_login, b.title as book_title, b.author, bf.file_name, bf.format
    FROM reading_progress rp
    JOIN users u ON rp.user_id = u.UID
    JOIN book_files bf ON rp.book_file_id = bf.id
    JOIN books b ON bf.book_id = b.id
    ORDER BY rp.last_read_at DESC
";
$result = mysqli_query($connect, $query);
while ($row = mysqli_fetch_assoc($result)) {
    $reading_progress[] = $row;
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Прогресс чтения - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
    <div class="container-fluid py-3">
        <h2><i class="bi bi-clock-history"></i> Прогресс чтения</h2>

        <div class="card mt-4">
            <div class="card-header">
                <h5 class="card-title mb-0">Активные чтения (<?= count($reading_progress) ?>)</h5>
            </div>
            <div class="card-body">
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead>
                            <tr>
                                <th>Пользователь</th>
                                <th>Книга</th>
                                <th>Прогресс</th>
                                <th>Страницы</th>
                                <th>Формат</th>
                                <th>Последнее чтение</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($reading_progress as $progress): ?>
                                <tr>
                                    <td>
                                        <strong><?= htmlspecialchars($progress['User_login']) ?></strong>
                                    </td>
                                    <td>
                                        <strong><?= htmlspecialchars($progress['book_title']) ?></strong>
                                        <?php if (!empty($progress['author'])): ?>
                                            <br><small class="text-muted"><?= htmlspecialchars($progress['author']) ?></small>
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <div class="d-flex align-items-center">
                                            <div class="progress flex-grow-1" style="height: 20px;">
                                                <div class="progress-bar 
                                                    <?= $progress['progress_percent'] >= 90 ? 'bg-success' : 
                                                       ($progress['progress_percent'] >= 50 ? 'bg-warning' : 'bg-info') ?>"
                                                    style="width: <?= $progress['progress_percent'] ?>%">
                                                    <?= round($progress['progress_percent'], 1) ?>%
                                                </div>
                                            </div>
                                        </div>
                                    </td>
                                    <td>
                                        <?= $progress['current_page'] ?> 
                                        <?php if ($progress['total_pages']): ?>
                                            / <?= $progress['total_pages'] ?>
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <span class="badge bg-secondary"><?= strtoupper($progress['format']) ?></span>
                                    </td>
                                    <td>
                                        <small class="text-muted">
                                            <?= date('d.m.Y H:i', strtotime($progress['last_read_at'])) ?>
                                        </small>
                                    </td>
                                </tr>
                            <?php endforeach; ?>
                            <?php if (empty($reading_progress)): ?>
                                <tr>
                                    <td colspan="6" class="text-center text-muted py-4">
                                        <i class="bi bi-clock display-4 d-block mb-2"></i>
                                        Нет данных о прогрессе чтения
                                    </td>
                                </tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <!-- Статистика прогресса -->
        <div class="row mt-4">
            <div class="col-md-3">
                <?php
                $avg_progress = array_sum(array_column($reading_progress, 'progress_percent')) / max(count($reading_progress), 1);
                ?>
                <div class="card text-white bg-info">
                    <div class="card-body text-center">
                        <h5 class="card-title"><?= round($avg_progress, 1) ?>%</h5>
                        <p class="card-text">Средний прогресс</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <?php
                $almost_finished = count(array_filter($reading_progress, function($p) { 
                    return $p['progress_percent'] >= 90; 
                }));
                ?>
                <div class="card text-white bg-success">
                    <div class="card-body text-center">
                        <h5 class="card-title"><?= $almost_finished ?></h5>
                        <p class="card-text">Почти закончили (>90%)</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <?php
                $just_started = count(array_filter($reading_progress, function($p) { 
                    return $p['progress_percent'] <= 10; 
                }));
                ?>
                <div class="card text-white bg-warning">
                    <div class="card-body text-center">
                        <h5 class="card-title"><?= $just_started ?></h5>
                        <p class="card-text">Только начали (<10%)</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <?php
                $recent_activity = count(array_filter($reading_progress, function($p) { 
                    return strtotime($p['last_read_at']) >= strtotime('-24 hours'); 
                }));
                ?>
                <div class="card text-white bg-primary">
                    <div class="card-body text-center">
                        <h5 class="card-title"><?= $recent_activity ?></h5>
                        <p class="card-text">Активны за 24ч</p>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>