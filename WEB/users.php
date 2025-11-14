<?php
session_start();
require_once 'db.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Получение списка пользователей
$users = [];
$result = mysqli_query($connect, "SELECT UID, User_login, Is_admin FROM users ORDER BY UID");
while ($row = mysqli_fetch_assoc($result)) {
    $users[] = $row;
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <title>Пользователи - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
</head>
<body>
    <div class="container-fluid py-3">
        <h2><i class="bi bi-people"></i> Управление пользователями</h2>
        
        <div class="card mt-3">
            <div class="card-body">
                <table class="table table-striped">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Логин</th>
                            <th>Администратор</th>
                            <th>Действия</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($users as $user): ?>
                        <tr>
                            <td><?= $user['UID'] ?></td>
                            <td><?= htmlspecialchars($user['User_login']) ?></td>
                            <td><?= $user['Is_admin'] ? 'Да' : 'Нет' ?></td>
                            <td>
                                <button class="btn btn-sm btn-warning">Редактировать</button>
                            </td>
                        </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</body>
</html>