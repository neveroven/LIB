<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Функция для безопасного выполнения запросов
function executeQuery($connect, $sql) {
    $result = mysqli_query($connect, $sql);
    if (!$result) {
        error_log("SQL Error in users.php: " . mysqli_error($connect));
        return false;
    }
    return $result;
}

// Обработка добавления пользователя
if (isset($_POST['action']) && $_POST['action'] == 'add_user') {
    $login = mysqli_real_escape_string($connect, $_POST['login']);
    $password = password_hash($_POST['password'], PASSWORD_DEFAULT);
    $is_admin = isset($_POST['is_admin']) ? 1 : 0;
    
    $sql = "INSERT INTO users (User_login, User_password, Is_admin) VALUES ('$login', '$password', $is_admin)";
    if (executeQuery($connect, $sql)) {
        $_SESSION['message'] = "Пользователь успешно добавлен";
        header('Location: users.php');
        exit();
    } else {
        $error = "Ошибка при добавлении пользователя";
    }
}

// Обработка редактирования пользователя
if (isset($_POST['action']) && $_POST['action'] == 'edit_user') {
    $user_id = intval($_POST['user_id']);
    $login = mysqli_real_escape_string($connect, $_POST['login']);
    $is_admin = isset($_POST['is_admin']) ? 1 : 0;
    
    // Проверяем, не пытаемся ли редактировать другого админа
    $current_user = mysqli_fetch_assoc(executeQuery($connect, "SELECT Is_admin FROM users WHERE UID = $user_id"));
    if ($current_user && $current_user['Is_admin'] && $user_id != $_SESSION['user_id']) {
        $error = "Нельзя редактировать других администраторов";
    } else {
        $sql = "UPDATE users SET User_login = '$login', Is_admin = $is_admin WHERE UID = $user_id";
        if (executeQuery($connect, $sql)) {
            $_SESSION['message'] = "Пользователь успешно обновлен";
            header('Location: users.php');
            exit();
        } else {
            $error = "Ошибка при обновлении пользователя";
        }
    }
}

// Обработка смены пароля
if (isset($_POST['action']) && $_POST['action'] == 'change_password') {
    $user_id = intval($_POST['user_id']);
    $password = password_hash($_POST['password'], PASSWORD_DEFAULT);
    
    // Проверяем, не пытаемся ли изменить пароль другого админа
    $current_user = mysqli_fetch_assoc(executeQuery($connect, "SELECT Is_admin FROM users WHERE UID = $user_id"));
    if ($current_user && $current_user['Is_admin'] && $user_id != $_SESSION['user_id']) {
        $error = "Нельзя изменять пароль других администраторов";
    } else {
        $sql = "UPDATE users SET User_password = '$password' WHERE UID = $user_id";
        if (executeQuery($connect, $sql)) {
            $_SESSION['message'] = "Пароль успешно изменен";
            header('Location: users.php');
            exit();
        } else {
            $error = "Ошибка при изменении пароля";
        }
    }
}

// Обработка удаления пользователя
if (isset($_GET['action']) && $_GET['action'] == 'delete' && isset($_GET['id'])) {
    $user_id = intval($_GET['id']);
    
    // Проверяем, не пытаемся ли удалить другого админа или себя
    $current_user = mysqli_fetch_assoc(executeQuery($connect, "SELECT Is_admin FROM users WHERE UID = $user_id"));
    if ($current_user) {
        if ($current_user['Is_admin']) {
            $error = "Нельзя удалять администраторов";
        } elseif ($user_id == $_SESSION['user_id']) {
            $error = "Нельзя удалить самого себя";
        } else {
            $sql = "DELETE FROM users WHERE UID = $user_id";
            if (executeQuery($connect, $sql)) {
                $_SESSION['message'] = "Пользователь успешно удален";
                header('Location: users.php');
                exit();
            } else {
                $error = "Ошибка при удалении пользователя";
            }
        }
    }
}

// Получение данных пользователя для редактирования
$edit_user = null;
if (isset($_GET['action']) && $_GET['action'] == 'edit' && isset($_GET['id'])) {
    $user_id = intval($_GET['id']);
    $result = executeQuery($connect, "SELECT * FROM users WHERE UID = $user_id");
    $edit_user = mysqli_fetch_assoc($result);
}

// Получение списка пользователей
$users = [];
$result = executeQuery($connect, "SELECT UID, User_login, Is_admin FROM users ORDER BY UID");
if ($result) {
    while ($row = mysqli_fetch_assoc($result)) {
        $users[] = $row;
    }
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <title>Пользователи - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
    <link rel="stylesheet" href="../css/users.css">
</head>
<body>
    <div class="container-fluid py-3">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2><i class="bi bi-people"></i> Управление пользователями</h2>
            <button class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#addUserModal">
                <i class="bi bi-plus-circle"></i> Добавить пользователя
            </button>
        </div>

        <!-- Сообщения -->
        <?php if (isset($_SESSION['message'])): ?>
            <div class="alert alert-success alert-dismissible fade show" role="alert">
                <?= $_SESSION['message'] ?>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
            <?php unset($_SESSION['message']); ?>
        <?php endif; ?>

        <?php if (isset($error)): ?>
            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                <?= $error ?>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        <?php endif; ?>

        <div class="card">
            <div class="card-body">
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Логин</th>
                                <th>Роль</th>
                                <th>Статус</th>
                                <th>Действия</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($users as $user): ?>
                            <tr>
                                <td><?= $user['UID'] ?></td>
                                <td>
                                    <strong><?= htmlspecialchars($user['User_login']) ?></strong>
                                    <?php if ($user['UID'] == $_SESSION['user_id']): ?>
                                        <span class="badge bg-info">Вы</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <?php if ($user['Is_admin']): ?>
                                        <span class="badge badge-admin">Администратор</span>
                                    <?php else: ?>
                                        <span class="badge badge-user">Пользователь</span>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <?php if ($user['Is_admin']): ?>
                                        <i class="bi bi-shield-check text-success"></i> Активен
                                    <?php else: ?>
                                        <i class="bi bi-person-check text-primary"></i> Активен
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <div class="btn-group btn-group-sm">
                                        <button class="btn btn-outline-warning" 
                                                data-bs-toggle="modal" 
                                                data-bs-target="#editUserModal"
                                                onclick="setEditUser(<?= $user['UID'] ?>, '<?= htmlspecialchars($user['User_login']) ?>', <?= $user['Is_admin'] ?>)">
                                            <i class="bi bi-pencil"></i> Редактировать
                                        </button>
                                        <button class="btn btn-outline-info" 
                                                data-bs-toggle="modal"
                                                data-bs-target="#changePasswordModal"
                                                onclick="setChangePasswordUser(<?= $user['UID'] ?>, '<?= htmlspecialchars($user['User_login']) ?>')">
                                            <i class="bi bi-key"></i> Пароль
                                        </button>
                                        <?php if (!$user['Is_admin'] && $user['UID'] != $_SESSION['user_id']): ?>
                                            <a href="users.php?action=delete&id=<?= $user['UID'] ?>" 
                                               class="btn btn-outline-danger"
                                               onclick="return confirm('Вы уверены, что хотите удалить пользователя <?= htmlspecialchars($user['User_login']) ?>?')">
                                                <i class="bi bi-trash"></i> Удалить
                                            </a>
                                        <?php else: ?>
                                            <button class="btn btn-outline-secondary" disabled>
                                                <i class="bi bi-trash"></i> Удалить
                                            </button>
                                        <?php endif; ?>
                                    </div>
                                </td>
                            </tr>
                            <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>

    <!-- Модальное окно добавления пользователя -->
    <div class="modal fade" id="addUserModal" tabindex="-1">
        <div class="modal-dialog">
            <div class="modal-content">
                <form method="POST">
                    <div class="modal-header">
                        <h5 class="modal-title">Добавить пользователя</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <input type="hidden" name="action" value="add_user">
                        <div class="mb-3">
                            <label class="form-label">Логин</label>
                            <input type="text" class="form-control" name="login" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Пароль</label>
                            <input type="password" class="form-control" name="password" required minlength="6">
                        </div>
                        <div class="mb-3">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" name="is_admin" id="addIsAdmin">
                                <label class="form-check-label" for="addIsAdmin">
                                    Администратор
                                </label>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button type="submit" class="btn btn-primary">Добавить</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <!-- Модальное окно редактирования пользователя -->
    <div class="modal fade" id="editUserModal" tabindex="-1">
        <div class="modal-dialog">
            <div class="modal-content">
                <form method="POST">
                    <div class="modal-header">
                        <h5 class="modal-title">Редактировать пользователя</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <input type="hidden" name="action" value="edit_user">
                        <input type="hidden" name="user_id" id="editUserId">
                        <div class="mb-3">
                            <label class="form-label">Логин</label>
                            <input type="text" class="form-control" name="login" id="editUserLogin" required>
                        </div>
                        <div class="mb-3">
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" name="is_admin" id="editIsAdmin">
                                <label class="form-check-label" for="editIsAdmin">
                                    Администратор
                                </label>
                            </div>
                        </div>
                        <div class="alert alert-info">
                            <small>
                                <i class="bi bi-info-circle"></i> 
                                Для изменения пароля используйте кнопку "Пароль"
                            </small>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button type="submit" class="btn btn-primary">Сохранить</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <!-- Модальное окно смены пароля -->
    <div class="modal fade" id="changePasswordModal" tabindex="-1">
        <div class="modal-dialog">
            <div class="modal-content">
                <form method="POST">
                    <div class="modal-header">
                        <h5 class="modal-title">Смена пароля</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <input type="hidden" name="action" value="change_password">
                        <input type="hidden" name="user_id" id="changePasswordUserId">
                        <div class="mb-3">
                            <label class="form-label">Пользователь</label>
                            <input type="text" class="form-control" id="changePasswordUserLogin" readonly>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Новый пароль</label>
                            <input type="password" class="form-control" name="password" required minlength="6">
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Отмена</button>
                        <button type="submit" class="btn btn-primary">Изменить пароль</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
    <script src="../js/users.js"></script>
</body>
</html>