<?php
session_start();
require_once 'php/db.php';

// Проверка подключения к БД
if (!isset($connect) || !$connect) {
    die('Ошибка подключения к базе данных');
}

$error = '';

// Если пользователь уже авторизован, перенаправляем его
if (isset($_SESSION['user_id'])) {
    if ($_SESSION['is_admin']) {
        header('Location: php/index_admin.php');
    } else {
        header('Location: php/user_dashboard.php');
    }
    exit();
}

// Обработка формы авторизации
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['login_submit'])) {
    $login = trim($_POST['login_username'] ?? '');
    $password = trim($_POST['login_password'] ?? '');
    
    error_log("Login attempt - Username: '$login', Password: '$password'"); // Для отладки
    
    if (empty($login) || empty($password)) {
        $error = "Пожалуйста, заполните все поля";
    } else {
        // Проверяем пользователя в базе данных
        if (!isset($connect) || !$connect) {
            $error = "Ошибка подключения к базе данных";
        } else {
            // Получаем хеш пароля из БД
            $stmt = mysqli_prepare($connect, "SELECT UID, User_login, User_password, Is_admin FROM users WHERE User_login = ?");
            if (!$stmt) {
                $error = "Ошибка подготовки запроса: " . mysqli_error($connect);
            } else {
                mysqli_stmt_bind_param($stmt, 's', $login);
                mysqli_stmt_execute($stmt);
                $result = mysqli_stmt_get_result($stmt);
                
                if ($user = mysqli_fetch_assoc($result)) {
                    // Проверяем пароль с помощью password_verify
                    if (password_verify($password, $user['User_password'])) {
                        // Сохраняем данные в сессию
                        $_SESSION['user_id'] = $user['UID'];
                        $_SESSION['username'] = $user['User_login'];
                        $_SESSION['is_admin'] = (bool)$user['Is_admin'];
                        
                        // Перенаправляем в зависимости от роли
                        if ($_SESSION['is_admin']) {
                            header('Location: php/index_admin.php');
                        } else {
                            header('Location: php/user_dashboard.php');
                        }
                        exit();
                    } else {
                        $error = "Неверный логин или пароль";
                    }
                } else {
                    $error = "Неверный логин или пароль";
                }
                mysqli_stmt_close($stmt);
            }
        }
    }
}

// Обработка формы регистрации
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['register_submit'])) {
    $login = trim($_POST['reg_login'] ?? '');
    $password = trim($_POST['reg_password'] ?? '');
    $confirm_password = trim($_POST['confirm_password'] ?? '');
    
    if (empty($login) || empty($password) || empty($confirm_password)) {
        $error = "Пожалуйста, заполните все поля";
    } elseif ($password !== $confirm_password) {
        $error = "Пароли не совпадают";
    } elseif (strlen($password) < 4) {
        $error = "Пароль должен содержать минимум 4 символа";
    } else {
        // Проверяем, не занят ли логин
        $stmt = mysqli_prepare($connect, "SELECT UID FROM users WHERE User_login = ?");
        mysqli_stmt_bind_param($stmt, 's', $login);
        mysqli_stmt_execute($stmt);
        $result = mysqli_stmt_get_result($stmt);
        
        if (mysqli_fetch_assoc($result)) {
            $error = "Пользователь с таким логином уже существует";
        } else {
            // Хешируем пароль перед сохранением (используем BCrypt для совместимости с C# приложением)
            $passwordHash = password_hash($password, PASSWORD_BCRYPT);
            
            // Создаем нового пользователя
            $stmt = mysqli_prepare($connect, "INSERT INTO users (User_login, User_password, Is_admin) VALUES (?, ?, 0)");
            mysqli_stmt_bind_param($stmt, 'ss', $login, $passwordHash);
            
            if (mysqli_stmt_execute($stmt)) {
                $success = "Регистрация успешна! Теперь вы можете войти в систему.";
            } else {
                $error = "Ошибка при регистрации: " . mysqli_error($connect);
            }
        }
    }
}

// Получаем сохраненные значения для автозаполнения
$saved_login = $_POST['login_username'] ?? '';
$saved_reg_login = $_POST['reg_login'] ?? '';
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Paradise Library - Вход в систему</title>
    <link rel="stylesheet" href="css/main.css">
    <link rel="stylesheet" href="css/index.css">
</head>
<body>
    <div class="auth-container">
        <div class="auth-panel">
            <div class="auth-header">
                <h1 class="auth-title">🔐 Добро пожаловать!</h1>
                <p class="auth-subtitle">Войдите в систему или создайте новый аккаунт</p>
            </div>
            
            <!-- Сообщения об ошибках и успехе -->
            <?php if (!empty($error)): ?>
                <div class="alert alert-danger">
                    <?= htmlspecialchars($error) ?>
                </div>
            <?php endif; ?>
            
            <?php if (isset($success)): ?>
                <div class="alert alert-success">
                    <?= htmlspecialchars($success) ?>
                </div>
            <?php endif; ?>

            <!-- Переключатель режимов -->
            <div class="toggle-container">
                <button type="button" class="toggle-button active" id="login-tab">Вход</button>
                <button type="button" class="toggle-button" id="register-tab">Регистрация</button>
            </div>

            <!-- Форма входа -->
            <form method="POST" id="loginForm" style="display: block;">
                <div class="form-group">
                    <label class="form-label">Логин:</label>
                    <input type="text" class="form-control" name="login_username" value="<?= htmlspecialchars($saved_login) ?>" required>
                </div>
                <div class="form-group">
                    <label class="form-label">Пароль:</label>
                    <input type="password" class="form-control" name="login_password" required>
                </div>
                <button type="submit" name="login_submit" class="btn btn-primary" style="width: 100%; height: 45px; font-size: 16px; font-weight: 600; margin-top: 10px;">
                    Войти
                </button>
            </form>

            <!-- Форма регистрации -->
            <form method="POST" id="registerForm" style="display: none;">
                <div class="form-group">
                    <label class="form-label">Логин:</label>
                    <input type="text" class="form-control" name="reg_login" value="<?= htmlspecialchars($saved_reg_login) ?>" required>
                </div>
                <div class="form-group">
                    <label class="form-label">Пароль:</label>
                    <input type="password" class="form-control" name="reg_password" required>
                </div>
                <div class="form-group">
                    <label class="form-label">Подтвердите пароль:</label>
                    <input type="password" class="form-control" name="confirm_password" required>
                </div>
                <button type="submit" name="register_submit" class="btn btn-primary" style="width: 100%; height: 45px; font-size: 16px; font-weight: 600; margin-top: 10px;">
                    Зарегистрироваться
                </button>
            </form>

            <!-- Гостевой вход -->
            <div class="guest-login-section">
                <p class="guest-text">Или войдите как гость</p>
                <a href="php/guest_dashboard.php" class="btn guest-button">
                    Продолжить как гость
                </a>
            </div>
        </div>
    </div>

    <script src="js/main.js"></script>
    <script src="js/index.js"></script>
</body>
</html>