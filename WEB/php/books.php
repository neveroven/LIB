<?php
session_start();
require_once 'db.php';

// Проверка авторизации администратора
if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

$error = '';
$success = '';

// Обработка добавления книги
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['add_book'])) {
    $title = trim($_POST['title'] ?? '');
    $author = trim($_POST['author'] ?? '');
    $published_year = (int)($_POST['published_year'] ?? 0);
    $description = trim($_POST['description'] ?? '');
    $language = trim($_POST['language'] ?? '');
    $series = trim($_POST['series'] ?? '');
    $series_index = (int)($_POST['series_index'] ?? 0);

    if (empty($title)) {
        $error = 'Название книги обязательно для заполнения';
    } else {
        $stmt = mysqli_prepare($connect, 
            "INSERT INTO books (title, author, published_year, description, language, series, series_index) 
             VALUES (?, ?, ?, ?, ?, ?, ?)");
        
        mysqli_stmt_bind_param($stmt, 'ssisssi', $title, $author, $published_year, $description, $language, $series, $series_index);
        
        if (mysqli_stmt_execute($stmt)) {
            $success = 'Книга успешно добавлена';
        } else {
            $error = 'Ошибка при добавлении книги: ' . mysqli_error($connect);
        }
        mysqli_stmt_close($stmt);
    }
}

// Обработка обновления книги
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['update_book'])) {
    $id = (int)($_POST['id'] ?? 0);
    $title = trim($_POST['title'] ?? '');
    $author = trim($_POST['author'] ?? '');
    $published_year = (int)($_POST['published_year'] ?? 0);
    $description = trim($_POST['description'] ?? '');
    $language = trim($_POST['language'] ?? '');
    $series = trim($_POST['series'] ?? '');
    $series_index = (int)($_POST['series_index'] ?? 0);

    if ($id <= 0 || empty($title)) {
        $error = 'Неверные данные для обновления';
    } else {
        $stmt = mysqli_prepare($connect, 
            "UPDATE books SET title = ?, author = ?, published_year = ?, description = ?, language = ?, series = ?, series_index = ? 
             WHERE id = ?");
        
        mysqli_stmt_bind_param($stmt, 'ssisssii', $title, $author, $published_year, $description, $language, $series, $series_index, $id);
        
        if (mysqli_stmt_execute($stmt)) {
            $success = 'Книга успешно обновлена';
        } else {
            $error = 'Ошибка при обновлении книги: ' . mysqli_error($connect);
        }
        mysqli_stmt_close($stmt);
    }
}

// Обработка удаления книги
if (isset($_GET['delete'])) {
    $id = (int)$_GET['delete'];
    if ($id > 0) {
        mysqli_begin_transaction($connect);
        try {
            // Удаляем связанные записи
            mysqli_query($connect, "DELETE FROM reading_progress WHERE book_file_id IN (SELECT id FROM book_files WHERE book_id = $id)");
            mysqli_query($connect, "DELETE FROM book_files WHERE book_id = $id");
            mysqli_query($connect, "DELETE FROM user_books WHERE book_id = $id");
            mysqli_query($connect, "DELETE FROM books WHERE id = $id");
            mysqli_commit($connect);
            $success = 'Книга и все связанные данные успешно удалены';
        } catch (Exception $e) {
            mysqli_rollback($connect);
            $error = 'Ошибка при удалении книги: ' . $e->getMessage();
        }
    }
}

// Получение списка книг
$books = [];
$query = "SELECT id, title, author, published_year, description, language, series, series_index FROM books ORDER BY id DESC";
$result = mysqli_query($connect, $query);
if ($result) {
    while ($row = mysqli_fetch_assoc($result)) {
        $books[] = $row;
    }
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Управление книгами - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
                    
    <div class="container-fluid py-3">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2><i class="bi bi-book"></i> Управление книгами</h2>
            <button type="button" class="btn btn-success" data-bs-toggle="modal" data-bs-target="#addBookModal">
                <i class="bi bi-plus-circle"></i> Добавить книгу
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
                <h5 class="card-title mb-0">Список книг (<?= count($books) ?>)</h5>
            </div>
            <div class="card-body">
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Название</th>
                                <th>Автор</th>
                                <th>Год</th>
                                <th>Язык</th>
                                <th>Серия</th>
                                <th>Действия</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($books as $book): ?>
                                <tr>
                                    <td><?= $book['id'] ?></td>
                                    <td>
                                        <strong><?= htmlspecialchars($book['title']) ?></strong>
                                        <?php if (!empty($book['description'])): ?>
                                            <br><small class="text-muted"><?= htmlspecialchars(substr($book['description'], 0, 100)) ?>...</small>
                                        <?php endif; ?>
                                    </td>
                                    <td><?= htmlspecialchars($book['author'] ?? 'Не указан') ?></td>
                                    <td><?= $book['published_year'] ?: '-' ?></td>
                                    <td><?= htmlspecialchars($book['language'] ?? '-') ?></td>
                                    <td>
                                        <?php if (!empty($book['series'])): ?>
                                            <?= htmlspecialchars($book['series']) ?>
                                            <?php if ($book['series_index'] > 0): ?>
                                                <small class="text-muted">(#<?= $book['series_index'] ?>)</small>
                                            <?php endif; ?>
                                        <?php else: ?>
                                            -
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <button type="button" class="btn btn-sm btn-warning" 
                                                data-bs-toggle="modal" 
                                                data-bs-target="#editBookModal"
                                                data-id="<?= $book['id'] ?>"
                                                data-title="<?= htmlspecialchars($book['title']) ?>"
                                                data-author="<?= htmlspecialchars($book['author'] ?? '') ?>"
                                                data-published_year="<?= $book['published_year'] ?>"
                                                data-description="<?= htmlspecialchars($book['description'] ?? '') ?>"
                                                data-language="<?= htmlspecialchars($book['language'] ?? '') ?>"
                                                data-series="<?= htmlspecialchars($book['series'] ?? '') ?>"
                                                data-series_index="<?= $book['series_index'] ?>">
                                            <i class="bi bi-pencil"></i>
                                        </button>
                                        <a href="?delete=<?= $book['id'] ?>" 
                                           class="btn btn-sm btn-danger"
                                           onclick="return confirm('Удалить книгу и все связанные данные?')">
                                            <i class="bi bi-trash"></i>
                                        </a>
                                    </td>
                                </tr>
                            <?php endforeach; ?>
                            <?php if (empty($books)): ?>
                                <tr>
                                    <td colspan="7" class="text-center text-muted py-4">
                                        <i class="bi bi-book display-4 d-block mb-2"></i>
                                        Книги не найдены
                                    </td>
                                </tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <!-- Модальное окно добавления книги -->
    <div class="modal fade" id="addBookModal" tabindex="-1">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <form method="POST">
                    <div class="modal-header">
                        <h5 class="modal-title">Добавить новую книгу</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row g-3">
                            <div class="col-md-6">
                                <label class="form-label">Название *</label>
                                <input type="text" class="form-control" name="title" required>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Автор</label>
                                <input type="text" class="form-control" name="author">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Год издания</label>
                                <input type="number" class="form-control" name="published_year" min="1000" max="<?= date('Y') ?>">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Язык</label>
                                <input type="text" class="form-control" name="language" placeholder="ru, en и т.д.">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Серия</label>
                                <input type="text" class="form-control" name="series">
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Номер в серии</label>
                                <input type="number" class="form-control" name="series_index" min="0">
                            </div>
                            <div class="col-12">
                                <label class="form-label">Описание</label>
                                <textarea class="form-control" name="description" rows="4"></textarea>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button type="submit" name="add_book" class="btn btn-success">Добавить книгу</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <!-- Модальное окно редактирования книги -->
    <div class="modal fade" id="editBookModal" tabindex="-1">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <form method="POST">
                    <input type="hidden" name="id" id="edit_id">
                    <div class="modal-header">
                        <h5 class="modal-title">Редактировать книгу</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="row g-3">
                            <div class="col-md-6">
                                <label class="form-label">Название *</label>
                                <input type="text" class="form-control" name="title" id="edit_title" required>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Автор</label>
                                <input type="text" class="form-control" name="author" id="edit_author">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Год издания</label>
                                <input type="number" class="form-control" name="published_year" id="edit_published_year">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Язык</label>
                                <input type="text" class="form-control" name="language" id="edit_language">
                            </div>
                            <div class="col-md-4">
                                <label class="form-label">Серия</label>
                                <input type="text" class="form-control" name="series" id="edit_series">
                            </div>
                            <div class="col-md-6">
                                <label class="form-label">Номер в серии</label>
                                <input type="number" class="form-control" name="series_index" id="edit_series_index">
                            </div>
                            <div class="col-12">
                                <label class="form-label">Описание</label>
                                <textarea class="form-control" name="description" id="edit_description" rows="4"></textarea>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button type="submit" name="update_book" class="btn btn-warning">Обновить книгу</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
    <script src="../js/books.js"></script>
</body>
</html>