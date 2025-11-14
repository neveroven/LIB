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
    <style>
        .sidebar {
            background-color: #f8f9fa;
            height: calc(100vh - 56px);
            position: fixed;
        }
        .main-content {
            margin-left: 280px;
        }
        iframe {
            border: 1px solid #dee2e6;
            border-radius: 0.375rem;
        }
    </style>
</head>

<body>
    <header class="navbar navbar-dark sticky-top bg-dark flex-md-nowrap p-0 shadow">
        <a class="navbar-brand col-md-3 col-lg-2 me-0 px-3 fs-6" href="#">
            <span>📚 Paradise Library Admin</span>
        </a>
        <div class="navbar-nav">
            <div class="nav-item text-nowrap">
                <a class="nav-link px-3" href="logout.php">Выход</a>
            </div>
        </div>
    </header>

    <div class="container-fluid">
        <div class="row">
            <nav class="col-md-3 col-lg-2 d-md-block sidebar collapse">
                <div class="position-sticky pt-3">
                    <ul class="nav flex-column">
                        <li class="nav-item">
                            <a class="nav-link active" href="index_admin.php">
                                <span data-feather="home"></span>
                                Главная панель
                            </a>
                        </li>

                        <h6 class="sidebar-heading px-3 mt-4 mb-1 text-muted text-uppercase">
                            Управление контентом
                        </h6>
                        <li class="nav-item">
                            <a class="nav-link" href="books.php" target="adminFrame">
                                <span data-feather="book"></span>
                                Книги
                            </a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="book_files.php" target="adminFrame">
                                <span data-feather="file"></span>
                                Файлы книг
                            </a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="users.php" target="adminFrame">
                                <span data-feather="users"></span>
                                Пользователи
                            </a>
                        </li>

                        <h6 class="sidebar-heading px-3 mt-4 mb-1 text-muted text-uppercase">
                            Статистика и отчеты
                        </h6>
                        <li class="nav-item">
                            <a class="nav-link" href="reading_statistics.php" target="adminFrame">
                                <span data-feather="activity"></span>
                                Статистика чтения
                            </a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="user_books.php" target="adminFrame">
                                <span data-feather="list"></span>
                                Книги пользователей
                            </a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="reading_progress.php" target="adminFrame">
                                <span data-feather="bar-chart"></span>
                                Прогресс чтения
                            </a>
                        </li>
                    </ul>

                    <h6 class="sidebar-heading px-3 mt-4 mb-1 text-muted text-uppercase">
                        Система
                    </h6>
                    <ul class="nav flex-column mb-2">
                        <li class="nav-item">
                            <a class="nav-link" href="settings.php" target="adminFrame">
                                <span data-feather="settings"></span>
                                Настройки
                            </a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="backup.php" target="adminFrame">
                                <span data-feather="database"></span>
                                Резервное копирование
                            </a>
                        </li>
                    </ul>
                </div>
            </nav>

            <main class="col-md-9 ms-sm-auto col-lg-10 px-md-4 main-content">
                <div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
                    <h1 class="h2">Панель управления библиотекой</h1>
                    <div class="btn-toolbar mb-2 mb-md-0">
                        <div class="btn-group me-2">
                            <span class="btn btn-sm btn-outline-secondary">
                                <?php echo $_SESSION['username'] ?? 'Администратор'; ?>
                            </span>
                        </div>
                    </div>
                </div>

                <!-- Приветственная информация -->
                <div class="row">
                    <div class="col-12">
                        <div class="alert alert-info">
                            <h5>Добро пожаловать в панель управления Paradise Library!</h5>
                            <p class="mb-0">Используйте меню слева для навигации по разделам административной панели.</p>
                        </div>
                    </div>
                </div>

                <!-- Быстрая статистика -->
                <div class="row mb-4">
                    <div class="col-md-3">
                        <div class="card text-white bg-primary">
                            <div class="card-body">
                                <h5 class="card-title">
                                    <?php
                                    $result = mysqli_query($connect, "SELECT COUNT(*) as count FROM books");
                                    $row = mysqli_fetch_assoc($result);
                                    echo $row['count'];
                                    ?>
                                </h5>
                                <p class="card-text">Всего книг</p>
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

                <!-- Основной контент -->
                <div class="row">
                    <div class="col-12">
                        <iframe name="adminFrame" src="books.php" frameborder="0" width="100%" height="600px" style="background: white;"></iframe>
                    </div>
                </div>
            </main>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js" crossorigin="anonymous"></script>
    <script src="https://cdn.jsdelivr.net/npm/feather-icons@4.28.0/dist/feather.min.js"></script>
    <script>
        // Активация feather icons
        feather.replace();
    </script>
</body>
</html>