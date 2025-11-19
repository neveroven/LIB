<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

$error = '';
$success = '';

// Добавление файла книги
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['add_file'])) {
    $book_id = (int)($_POST['book_id'] ?? 0);
    $format = trim($_POST['format'] ?? '');
    $source_type = trim($_POST['source_type'] ?? 'local');
    $file_name = trim($_POST['file_name'] ?? '');
    $local_path = trim($_POST['local_path'] ?? '');
    $server_uri = trim($_POST['server_uri'] ?? '');
    $file_size_bytes = (int)($_POST['file_size_bytes'] ?? 0);
    $page_count = (int)($_POST['page_count'] ?? 0);
    $cover_image_uri = trim($_POST['cover_image_uri'] ?? '');

    if ($book_id <= 0 || empty($format) || empty($file_name)) {
        $error = 'Заполните обязательные поля: книга, формат и название файла';
    } else {
        $stmt = mysqli_prepare($connect, 
            "INSERT INTO book_files (book_id, format, source_type, local_path, server_uri, file_name, file_size_bytes, page_count, cover_image_uri) 
             VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)");
        
        mysqli_stmt_bind_param($stmt, 'isssssiis', $book_id, $format, $source_type, $local_path, $server_uri, $file_name, $file_size_bytes, $page_count, $cover_image_uri);
        
        if (mysqli_stmt_execute($stmt)) {
            $success = 'Файл книги успешно добавлен';
        } else {
            $error = 'Ошибка при добавлении файла: ' . mysqli_error($connect);
        }
        mysqli_stmt_close($stmt);
    }
}

// Удаление файла книги
if (isset($_GET['delete_file'])) {
    $file_id = (int)$_GET['delete_file'];
    if ($file_id > 0) {
        $stmt = mysqli_prepare($connect, "DELETE FROM book_files WHERE id = ?");
        mysqli_stmt_bind_param($stmt, 'i', $file_id);
        
        if (mysqli_stmt_execute($stmt)) {
            $success = 'Файл книги успешно удален';
        } else {
            $error = 'Ошибка при удалении файла: ' . mysqli_error($connect);
        }
        mysqli_stmt_close($stmt);
    }
}

// Получение списка файлов книг с информацией о книгах
$book_files = [];
$query = "SELECT bf.*, b.title as book_title, b.author 
          FROM book_files bf 
          JOIN books b ON bf.book_id = b.id 
          ORDER BY bf.id DESC";
$result = mysqli_query($connect, $query);
if ($result) {
    while ($row = mysqli_fetch_assoc($result)) {
        $book_files[] = $row;
    }
}

// Получение списка книг для выпадающего списка
$books = [];
$books_result = mysqli_query($connect, "SELECT id, title, author FROM books ORDER BY title");
while ($book = mysqli_fetch_assoc($books_result)) {
    $books[] = $book;
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Файлы книг - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
    <div class="container-fluid py-3">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2><i class="bi bi-file-earmark"></i> Управление файлами книг</h2>
            <button type="button" class="btn btn-success" data-bs-toggle="modal" data-bs-target="#addFileModal">
                <i class="bi bi-plus-circle"></i> Добавить файл
            </button>
        </div>

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

        <div class="card">
            <div class="card-header">
                <h5 class="card-title mb-0">Список файлов книг (<?= count($book_files) ?>)</h5>
            </div>
            <div class="card-body">
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Книга</th>
                                <th>Формат</th>
                                <th>Тип</th>
                                <th>Название файла</th>
                                <th>Размер</th>
                                <th>Страниц</th>
                                <th>Действия</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($book_files as $file): ?>
                                <tr>
                                    <td><?= $file['id'] ?></td>
                                    <td>
                                        <strong><?= htmlspecialchars($file['book_title']) ?></strong>
                                        <?php if (!empty($file['author'])): ?>
                                            <br><small class="text-muted"><?= htmlspecialchars($file['author']) ?></small>
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <span class="badge bg-secondary"><?= strtoupper($file['format']) ?></span>
                                    </td>
                                    <td>
                                        <span class="badge bg-<?= $file['source_type'] === 'server' ? 'info' : 'warning' ?>">
                                            <?= $file['source_type'] === 'server' ? 'Сервер' : 'Локальный' ?>
                                        </span>
                                    </td>
                                    <td><?= htmlspecialchars($file['file_name']) ?></td>
                                    <td>
                                        <?php if ($file['file_size_bytes'] > 0): ?>
                                            <?= number_format($file['file_size_bytes'] / 1024, 1) ?> KB
                                        <?php else: ?>
                                            <span class="text-muted">—</span>
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <?= $file['page_count'] ?: '<span class="text-muted">—</span>' ?>
                                    </td>
                                    <td>
                                        <a href="?delete_file=<?= $file['id'] ?>" 
                                           class="btn btn-sm btn-danger"
                                           onclick="return confirm('Удалить файл книги?')">
                                            <i class="bi bi-trash"></i>
                                        </a>
                                    </td>
                                </tr>
                            <?php endforeach; ?>
                            <?php if (empty($book_files)): ?>
                                <tr>
                                    <td colspan="8" class="text-center text-muted py-4">
                                        <i class="bi bi-file-earmark display-4 d-block mb-2"></i>
                                        Файлы книг не найдены
                                    </td>
                                </tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <!-- Модальное окно добавления файла -->
    <div class="modal fade" id="addFileModal" tabindex="-1">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <form method="POST">
                    <div class="modal-header">
                        <h5 class="modal-title">Добавить файл книги</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row g-3">
                            <div class="col-md-6">
                                <label class="form-label">Кника *</label>
                                <select class="form-select" name="book_id" required>
                                    <option value="">Выберите книгу</option>
                                    <?php foreach ($books as $book): ?>
                                        <option value="<?= $book['id'] ?>">
                                            <?= htmlspecialchars($book['title']) ?>
                                            <?php if (!empty($book['author'])): ?>
                                                (<?= htmlspecialchars($book['author']) ?>)
                                            <?php endif; ?>
                                        </option>
                                    <?php endforeach; ?>
                                </select>
                            </div>
                            <div class="col-md-3">
                                <label class="form-label">Формат *</label>
                                <select class="form-select" name="format" required>
                                    <option value="">Выберите формат</option>
                                    <option value="pdf">PDF</option>
                                    <option value="fb2">FB2</option>
                                    <option value="epub">EPUB</option>
                                    <option value="txt">TXT</option>
                                    <option value="doc">DOC</option>
                                    <option value="docx">DOCX</option>
                                    <option value="rtf">RTF</option>
                                    <option value="md">MD</option>
                                    <option value="unknown">Неизвестно</option>
                                </select>
                            </div>
                            <div class="col-md-3">
                                <label class="form-label">Тип источника *</label>
                                <select class="form-select" name="source_type" required>
                                    <option value="local">Локальный</option>
                                    <option value="server">Сервер</option>
                                </select>
                            </div>
                            <div class="col-12">
                                <label class="form-label">Название файла *</label>
                                <input type="text" class="form-control" name="file_name" required>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Локальный путь</label>
                                <input type="text" class="form-control" name="local_path" placeholder="/path/to/file.pdf">
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Серверный URI</label>
                                <input type="text" class="form-control" name="server_uri" placeholder="books/fiction/file.pdf">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Размер файла (байты)</label>
                                <input type="number" class="form-control" name="file_size_bytes" min="0">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Количество страниц</label>
                                <input type="number" class="form-control" name="page_count" min="0">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Обложка (URI)</label>
                                <input type="text" class="form-control" name="cover_image_uri" placeholder="/images/cover.jpg">
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button type="submit" name="add_file" class="btn btn-success">Добавить файл</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>