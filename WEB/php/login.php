<?php
session_start();
require_once 'db.php';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $login = trim($_POST['login'] ?? '');
    $password = trim($_POST['password'] ?? '');
    
    // Получаем хеш пароля из БД
    $stmt = mysqli_prepare($connect, "SELECT UID, User_login, User_password, Is_admin FROM users WHERE User_login = ? AND Is_admin = 1");
    mysqli_stmt_bind_param($stmt, 's', $login);
    mysqli_stmt_execute($stmt);
    $result = mysqli_stmt_get_result($stmt);
    
    if ($user = mysqli_fetch_assoc($result)) {
        // Проверяем пароль с помощью password_verify
        if (password_verify($password, $user['User_password'])) {
            $_SESSION['user_id'] = $user['UID'];
            $_SESSION['username'] = $user['User_login'];
            $_SESSION['is_admin'] = true;
            header('Location: index_admin.php');
            exit();
        } else {
            $error = "Неверные учетные данные или недостаточно прав";
        }
    } else {
        $error = "Неверные учетные данные или недостаточно прав";
    }
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <title>Вход в админ-панель</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
</head>
<body class="bg-light">
    <div class="container">
        <div class="row justify-content-center align-items-center min-vh-100">
            <div class="col-md-6">
                <div class="card shadow">
                    <div class="card-body p-5">
                        <h2 class="text-center mb-4">📚 Paradise Library</h2>
                        <h4 class="text-center mb-4">Административная панель</h4>
                        
                        <?php if (isset($error)): ?>
                            <div class="alert alert-danger"><?= $error ?></div>
                        <?php endif; ?>
                        
                        <form method="POST">
                            <div class="mb-3">
                                <label class="form-label">Логин</label>
                                <input type="text" class="form-control" name="login" required>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Пароль</label>
                                <input type="password" class="form-control" name="password" required>
                            </div>
                            <button type="submit" class="btn btn-primary w-100">Войти</button>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>