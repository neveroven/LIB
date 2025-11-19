<?php
session_start();
require_once 'db.php';

$error = '';

// Если пользователь уже авторизован, перенаправляем его
if (isset($_SESSION['user_id'])) {
    if ($_SESSION['is_admin']) {
        header('Location: index_admin.php');
    } else {
        header('Location: user_dashboard.php');
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
        $stmt = mysqli_prepare($connect, "SELECT UID, User_login, Is_admin FROM users WHERE User_login = ? AND User_password = ?");
        mysqli_stmt_bind_param($stmt, 'ss', $login, $password);
        mysqli_stmt_execute($stmt);
        $result = mysqli_stmt_get_result($stmt);
        
        if ($user = mysqli_fetch_assoc($result)) {
            // Сохраняем данные в сессию
            $_SESSION['user_id'] = $user['UID'];
            $_SESSION['username'] = $user['User_login'];
            $_SESSION['is_admin'] = (bool)$user['Is_admin'];
            
            // Перенаправляем в зависимости от роли
            if ($_SESSION['is_admin']) {
                header('Location: index_admin.php');
            } else {
                header('Location: user_dashboard.php');
            }
            exit();
        } else {
            $error = "Неверный логин или пароль";
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
            // Создаем нового пользователя
            $stmt = mysqli_prepare($connect, "INSERT INTO users (User_login, User_password, Is_admin) VALUES (?, ?, 0)");
            mysqli_stmt_bind_param($stmt, 'ss', $login, $password);
            
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
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
    <style>
        body {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
        }
        .login-container {
            background: white;
            border-radius: 15px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.3);
            overflow: hidden;
        }
        .login-header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 2rem;
            text-align: center;
        }
        .login-body {
            padding: 2rem;
        }
        .nav-tabs .nav-link {
            border: none;
            color: #6c757d;
            font-weight: 500;
        }
        .nav-tabs .nav-link.active {
            color: #667eea;
            border-bottom: 3px solid #667eea;
            background: transparent;
        }
        .form-control:focus {
            border-color: #667eea;
            box-shadow: 0 0 0 0.2rem rgba(102, 126, 234, 0.25);
        }
        .btn-primary {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border: none;
        }
        .btn-primary:hover {
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="row justify-content-center">
            <div class="col-md-6 col-lg-5">
                <div class="login-container">
                    <div class="login-header">
                        <h1><i class="bi bi-book-half"></i></h1>
                        <h2>Paradise Library</h2>
                        <p class="mb-0">Добро пожаловать в цифровую библиотеку</p>
                    </div>
                    
                    <div class="login-body">
                        <!-- Сообщения об ошибках и успехе -->
                        <?php if (!empty($error)): ?>
                            <div class="alert alert-danger alert-dismissible fade show" role="alert">
                                <?= htmlspecialchars($error) ?>
                                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                            </div>
                        <?php endif; ?>
                        
                        <?php if (isset($success)): ?>
                            <div class="alert alert-success alert-dismissible fade show" role="alert">
                                <?= htmlspecialchars($success) ?>
                                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
                            </div>
                        <?php endif; ?>

                        <!-- Навигация между вкладками -->
                        <ul class="nav nav-tabs nav-justified mb-4" id="authTabs" role="tablist">
                            <li class="nav-item" role="presentation">
                                <button class="nav-link active" id="login-tab" data-bs-toggle="tab" data-bs-target="#login" type="button" role="tab">
                                    <i class="bi bi-box-arrow-in-right"></i> Вход
                                </button>
                            </li>
                            <li class="nav-item" role="presentation">
                                <button class="nav-link" id="register-tab" data-bs-toggle="tab" data-bs-target="#register" type="button" role="tab">
                                    <i class="bi bi-person-plus"></i> Регистрация
                                </button>
                            </li>
                        </ul>

                        <!-- Содержимое вкладок -->
                        <div class="tab-content" id="authTabsContent">
                            <!-- Вкладка входа -->
                            <div class="tab-pane fade show active" id="login" role="tabpanel">
                                <form method="POST" id="loginForm">
                                    <div class="mb-3">
                                        <label class="form-label">Логин</label>
                                        <div class="input-group">
                                            <span class="input-group-text"><i class="bi bi-person"></i></span>
                                            <input type="text" class="form-control" name="login_username" value="<?= htmlspecialchars($saved_login) ?>" required>
                                        </div>
                                    </div>
                                    <div class="mb-4">
                                        <label class="form-label">Пароль</label>
                                        <div class="input-group">
                                            <span class="input-group-text"><i class="bi bi-lock"></i></span>
                                            <input type="password" class="form-control" name="login_password" required>
                                        </div>
                                    </div>
                                    <button type="submit" name="login_submit" class="btn btn-primary w-100 py-2">
                                        <i class="bi bi-box-arrow-in-right"></i> Войти в систему
                                    </button>
                                </form>
                                
                                
                            </div>

                            <!-- Вкладка регистрации -->
                            <div class="tab-pane fade" id="register" role="tabpanel">
                                <form method="POST" id="registerForm">
                                    <div class="mb-3">
                                        <label class="form-label">Логин</label>
                                        <div class="input-group">
                                            <span class="input-group-text"><i class="bi bi-person"></i></span>
                                            <input type="text" class="form-control" name="reg_login" value="<?= htmlspecialchars($saved_reg_login) ?>" required>
                                        </div>
                                        <div class="form-text">Минимум 3 символа</div>
                                    </div>
                                    <div class="mb-3">
                                        <label class="form-label">Пароль</label>
                                        <div class="input-group">
                                            <span class="input-group-text"><i class="bi bi-lock"></i></span>
                                            <input type="password" class="form-control" name="reg_password" required>
                                        </div>
                                        <div class="form-text">Минимум 4 символа</div>
                                    </div>
                                    <div class="mb-4">
                                        <label class="form-label">Подтверждение пароля</label>
                                        <div class="input-group">
                                            <span class="input-group-text"><i class="bi bi-lock-fill"></i></span>
                                            <input type="password" class="form-control" name="confirm_password" required>
                                        </div>
                                    </div>
                                    <button type="submit" name="register_submit" class="btn btn-primary w-100 py-2">
                                        <i class="bi bi-person-plus"></i> Зарегистрироваться
                                    </button>
                                </form>
                            </div>
                        </div>

                        <!-- Гостевой вход -->
                        <div class="text-center mt-4">
                            <hr>
                            <p class="text-muted mb-2">Или продолжите как гость</p>
                            <a href="guest_dashboard.php" class="btn btn-outline-secondary">
                                <i class="bi bi-eye"></i> Просмотр каталога
                            </a>
                        </div>
                    </div>
                </div>
                
                <!-- Информация о системе -->
                <div class="text-center mt-4">
                    <small class="text-white">
                        &copy; 2024 Paradise Library. Цифровая библиотечная система
                    </small>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        // Очистка сообщений при переключении вкладок
        document.addEventListener('DOMContentLoaded', function() {
            const authTabs = document.querySelectorAll('#authTabs button[data-bs-toggle="tab"]');
            authTabs.forEach(tab => {
                tab.addEventListener('show.bs.tab', function() {
                    const alerts = document.querySelectorAll('.alert');
                    alerts.forEach(alert => {
                        const bsAlert = new bootstrap.Alert(alert);
                        bsAlert.close();
                    });
                });
            });

            // Предотвращение отправки обеих форм одновременно
            document.getElementById('loginForm').addEventListener('submit', function(e) {
                // Удаляем возможные данные из другой формы
                const registerForm = document.getElementById('registerForm');
                const registerInputs = registerForm.querySelectorAll('input[name]');
                registerInputs.forEach(input => {
                    input.disabled = true;
                });
            });

            document.getElementById('registerForm').addEventListener('submit', function(e) {
                // Удаляем возможные данные из другой формы
                const loginForm = document.getElementById('loginForm');
                const loginInputs = loginForm.querySelectorAll('input[name]');
                loginInputs.forEach(input => {
                    input.disabled = true;
                });
            });
        });
    </script>
</body>
</html>