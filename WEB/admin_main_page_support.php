<?php
session_start();
include("db.php");

// Проверка авторизации администратора
if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}
?>
<!doctype html>
<html lang="ru">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="description" content="">
    <meta name="author" content="Mark Otto, Jacob Thornton, and Bootstrap contributors">
    <meta name="generator" content="Hugo 0.101.0">
    <title>Панель администратора - Paradise Library</title>
    <link rel="canonical" href="https://getbootstrap.com/docs/5.2/examples/dashboard/">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-iYQeCzEYFbKjA/T2uDLTpkwGzCiq6soy8tYaI1GyVh/UjpbCx/TYkiZhlZB6+fzT" crossorigin="anonymous">
</head>

<body>
    <!-- Быстрая статистика -->
    <div class="row mb-4" >
                    <div class="col-md-3">
                        <div class="card text-white bg-primary">
                            <div class="card-body"  >
                                <h5 class="card-title" >
                                    <?php
                                    $result = mysqli_query($connect, "SELECT COUNT(*) as count FROM books");
                                    $row = mysqli_fetch_assoc($result);
                                    echo $row['count'];
                                    ?>
                                </h5>
                                <p class="card-text" >Всего книг</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card text-white bg-success">
                            <div class="card-body">
                                <h5 class="card-title">
                                    <?php
                                    $result = mysqli_query($connect, "SELECT COUNT(*) as count FROM users");
                                    $row = mysqli_fetch_assoc($result);
                                    echo $row['count'];
                                    ?>
                                </h5>
                                <p class="card-text">Пользователей</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card text-white bg-warning">
                            <div class="card-body">
                                <h5 class="card-title">
                                    <?php
                                    $result = mysqli_query($connect, "SELECT COUNT(*) as count FROM reading_progress");
                                    $row = mysqli_fetch_assoc($result);
                                    echo $row['count'];
                                    ?>
                                </h5>
                                <p class="card-text">Активных чтений</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card text-white bg-info">
                            <div class="card-body">
                                <h5 class="card-title">
                                    <?php
                                    $result = mysqli_query($connect, "SELECT COUNT(*) as count FROM book_files");
                                    $row = mysqli_fetch_assoc($result);
                                    echo $row['count'];
                                    ?>
                                </h5>
                                <p class="card-text">Файлов книг</p>
                            </div>
                        </div>
                    </div>
                </div>
</body>
</html>