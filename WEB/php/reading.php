<?php
session_start();
require_once 'db.php';

// Доступ только авторизованным пользователям (не гостям). Админу тоже позволяем читать.
if (empty($_SESSION['user_id'])) {
    header('Location: ../index.php');
    exit();
}

$user_id = (int)$_SESSION['user_id'];
$book_id = (int)($_GET['id'] ?? 0);

if ($book_id <= 0) {
    header('Location: user_dashboard.php');
    exit();
}

// Получаем книгу и связанный файл, который принадлежит пользователю
$book_sql = "
    SELECT 
        b.id            AS book_id,
        b.title,
        b.author,
        b.description,
        bf.id           AS file_id,
        bf.format,
        bf.source_type,
        bf.local_path,
        bf.server_uri,
        bf.file_name
    FROM books b
    JOIN user_books ub   ON ub.book_id = b.id AND ub.user_id = ?
    LEFT JOIN book_files bf ON bf.book_id = b.id
    WHERE b.id = ?
    ORDER BY bf.id ASC
    LIMIT 1
";

$stmt = mysqli_prepare($connect, $book_sql);
mysqli_stmt_bind_param($stmt, 'ii', $user_id, $book_id);
mysqli_stmt_execute($stmt);
$result = mysqli_stmt_get_result($stmt);
$book = mysqli_fetch_assoc($result);
mysqli_stmt_close($stmt);

if (!$book) {
    http_response_code(404);
    echo "Книга не найдена или не добавлена в вашу библиотеку.";
    exit();
}

// Получаем путь к папке DB из настроек (settings.setting_key = 'db_folder_path')
$db_folder_path = '';
$settings_sql = "SELECT setting_value FROM settings WHERE setting_key = 'db_folder_path' LIMIT 1";
$settings_res = mysqli_query($connect, $settings_sql);
if ($settings_res && $row = mysqli_fetch_assoc($settings_res)) {
    $db_folder_path = trim($row['setting_value'] ?? '');
}

// Путь к файлу: используем db_folder_path + server_uri, иначе local_path
$file_path = '';
if (!empty($book['server_uri']) && $db_folder_path !== '') {
    // нормализуем слеши
    $serverUri = ltrim(str_replace(['\\'], '/', $book['server_uri']), '/');
    $dbBase = rtrim(str_replace(['\\'], '/', $db_folder_path), '/');
    $candidate = $dbBase . '/' . $serverUri;
    if (file_exists($candidate)) {
        $file_path = $candidate;
    }
}

// если не нашли по db_folder_path, пробуем local_path как есть
if ($file_path === '' && !empty($book['local_path']) && file_exists($book['local_path'])) {
    $file_path = $book['local_path'];
}

// если по-прежнему пусто, попробуем относительный server_uri к проекту
if ($file_path === '' && !empty($book['server_uri'])) {
    $candidate = realpath(__DIR__ . '/../' . ltrim(str_replace(['\\'], '/', $book['server_uri']), '/'));
    if ($candidate && file_exists($candidate)) {
        $file_path = $candidate;
    } else {
        $file_path = str_replace(['\\'], '/', $book['server_uri']); // оставляем как ссылку для скачивания
    }
}

// Функции чтения
function read_text_content(string $path): ?string {
    if (!is_readable($path)) {
        return null;
    }
    $content = @file_get_contents($path);
    return $content === false ? null : $content;
}

function read_fb2_xml_content(string $path): ?string {
    $raw = read_text_content($path);
    if ($raw === null) return null;
    // Простая очистка тегов
    $clean = strip_tags($raw);
    return $clean;
}

function read_rtf_content(string $path): ?string {
    $raw = read_text_content($path);
    if ($raw === null) return null;
    // Удаляем управляющие последовательности RTF
    $clean = preg_replace('/\\\\[a-z]+[0-9]?/', '', $raw);
    $clean = preg_replace('/{\\\*?[^}]+}/', '', $clean);
    $clean = strip_tags($clean);
    return $clean;
}

function get_book_content(array $book, string $file_path): array {
    if (empty($file_path) || !file_exists($file_path)) {
        return ['content' => null, 'error' => 'Файл книги не найден на сервере'];
    }

    $format = strtolower($book['format'] ?? pathinfo($file_path, PATHINFO_EXTENSION));
    $content = null;
    $error = null;

    switch ($format) {
        case 'txt':
        case 'md':
            $content = read_text_content($file_path);
            break;
        case 'rtf':
            $content = read_rtf_content($file_path);
            break;
        case 'fb2':
        case 'xml':
            $content = read_fb2_xml_content($file_path);
            break;
        // Для сложных форматов пока не поддерживаем онлайн-просмотр
        case 'pdf':
        case 'epub':
        case 'doc':
        case 'docx':
            $error = 'Онлайн-чтение для этого формата пока не поддерживается. Скачайте файл и откройте его локально.';
            break;
        default:
            $content = read_text_content($file_path);
            break;
    }

    if ($content === null && $error === null) {
        $error = 'Не удалось прочитать содержимое файла.';
    }

    return ['content' => $content, 'error' => $error];
}

$reading = get_book_content($book, $file_path);

// Загружаем сохранённый прогресс
$progress = [
    'percent' => 0,
    'current_page' => 0,
    'total_pages' => 0,
];

if (!empty($book['file_id'])) {
    $progress_sql = "SELECT progress_percent, current_page, total_pages FROM reading_progress WHERE user_id = ? AND book_file_id = ?";
    $stmt = mysqli_prepare($connect, $progress_sql);
    mysqli_stmt_bind_param($stmt, 'ii', $user_id, $book['file_id']);
    mysqli_stmt_execute($stmt);
    $res = mysqli_stmt_get_result($stmt);
    if ($row = mysqli_fetch_assoc($res)) {
        $progress['percent'] = (float)$row['progress_percent'];
        $progress['current_page'] = (int)$row['current_page'];
        $progress['total_pages'] = (int)$row['total_pages'];
    }
    mysqli_stmt_close($stmt);
}
?>
<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= htmlspecialchars($book['title']) ?> – чтение</title>
    <link rel="stylesheet" href="../css/main.css">
    <link rel="stylesheet" href="../css/reading.css">
</head>
<body>
    <div class="app-container">
        <div class="left-panel">
            <div class="library-title" onclick="window.location.href='user_dashboard.php'">📚 Paradise</div>
            <div class="nav-buttons">
                <button class="nav-button" data-href="user_dashboard.php">📖 Книги</button>
                <button class="nav-button" data-href="catalog.php">📚 Каталог книг</button>
            </div>
            <div class="settings-buttons">
                <button class="nav-button" data-href="settings.php">⚙️ Настройки</button>
                <button class="nav-button" onclick="window.location.href='logout.php'">🚪 Выход</button>
            </div>
        </div>

        <div class="right-panel">
            <div class="top-bar">
                <button class="back-button" onclick="window.location.href='user_dashboard.php'">← Вернуться к библиотеке</button>
                <div style="display: flex; gap: 10px; align-items: center;">
                    <?php if ($file_path): ?>
                        <a class="btn" style="height: 35px; padding: 8px 12px;" href="<?= htmlspecialchars('../' . ltrim(str_replace('\\', '/', $file_path), '/')) ?>" download>
                            ⬇️ Скачать файл
                        </a>
                    <?php endif; ?>
                    <button class="exit-button" onclick="window.location.href='logout.php'">Выход</button>
                </div>
            </div>

            <div class="main-content">
                <div class="panel">
                    <h1 class="panel-title" style="font-size: 22px; display: flex; align-items: center; gap: 10px;">
                        📖 <?= htmlspecialchars($book['title']) ?>
                    </h1>
                    <p class="panel-subtitle" style="margin-bottom: 8px;">
                        <?= htmlspecialchars($book['author'] ?: 'Автор не указан') ?>
                    </p>
                    <?php if (!empty($book['description'])): ?>
                        <p style="opacity: 0.8; margin-bottom: 0;"><?= htmlspecialchars($book['description']) ?></p>
                    <?php endif; ?>
                </div>

                <div class="panel reader-panel"
                     data-book-file-id="<?= (int)$book['file_id'] ?>"
                     data-initial-progress="<?= $progress['percent'] ?>">
                    <div class="reader-progress">
                        <div class="reader-progress__bar" id="readerProgressBar"></div>
                        <div class="reader-progress__meta">
                            <span id="readerProgressText"><?= $progress['percent'] > 0 ? round($progress['percent']) . '%' : '0%' ?></span>
                            <?php if (!empty($book['format'])): ?>
                                <span class="badge"><?= strtoupper($book['format']) ?></span>
                            <?php endif; ?>
                        </div>
                    </div>

                    <?php if ($reading['error']): ?>
                        <div class="reader-error">
                            <?= htmlspecialchars($reading['error']) ?>
                            <?php if ($file_path): ?>
                                <div style="margin-top: 10px;">
                                    <a class="btn btn-primary" href="<?= htmlspecialchars('../' . ltrim(str_replace('\\', '/', $file_path), '/')) ?>" download>Скачать файл</a>
                                </div>
                            <?php endif; ?>
                        </div>
                    <?php else: ?>
                        <div class="book-content" id="bookContent">
                            <?= nl2br(htmlspecialchars($reading['content'])) ?>
                        </div>
                    <?php endif; ?>
                </div>
            </div>
        </div>
    </div>

    <script>
        window.READING_PROGRESS = {
            bookFileId: <?= (int)$book['file_id'] ?>,
            initialPercent: <?= (float)$progress['percent'] ?>,
            apiUrl: 'reading_progress_api.php'
        };
    </script>
    <script src="../js/main.js"></script>
    <script src="../js/reading.js"></script>
    <script>
        // Синхронизация пользовательских настроек (тема/шрифт) из PHP-сессии
        (function() {
            const phpTheme = '<?= !empty($_SESSION["user_theme"]) && $_SESSION["user_theme"] === "dark" ? "dark" : "light" ?>';
            const phpFont = '<?= !empty($_SESSION["user_font_size"]) ? $_SESSION["user_font_size"] : "medium" ?>';

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
</body>
</html>

