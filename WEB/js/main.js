// Paradise Library - Main JavaScript
// Based on MainWindow.xaml functionality

// Theme management
const ThemeManager = {
    init() {
        // Check saved theme preference
        const savedTheme = localStorage.getItem('theme');
        if (savedTheme === 'dark') {
            document.body.classList.add('dark-theme');
        }
        
        // Setup theme toggle if exists
        const themeToggle = document.getElementById('themeToggle');
        if (themeToggle) {
            themeToggle.addEventListener('change', (e) => {
                this.toggleTheme(e.target.checked);
            });
            
            // Set initial state
            if (savedTheme === 'dark') {
                themeToggle.checked = true;
            }
        }
    },
    
    toggleTheme(isDark) {
        if (isDark) {
            document.body.classList.add('dark-theme');
            localStorage.setItem('theme', 'dark');
        } else {
            document.body.classList.remove('dark-theme');
            localStorage.setItem('theme', 'light');
        }
    }
};

// Navigation management
const Navigation = {
    init() {
        // Handle navigation buttons
        const navButtons = document.querySelectorAll('.nav-button');
        navButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                const href = button.getAttribute('data-href');
                if (href) {
                    window.location.href = href;
                }
            });
        });
        
        // Set active navigation button based on current page
        this.setActiveNav();
    },
    
    setActiveNav() {
        const currentPath = window.location.pathname;
        const navButtons = document.querySelectorAll('.nav-button');
        
        navButtons.forEach(button => {
            const href = button.getAttribute('data-href');
            if (href && currentPath.includes(href)) {
                button.classList.add('active');
            } else {
                button.classList.remove('active');
            }
        });
    }
};

// Auto-dismiss alerts (common functionality)
function autoDismissAlerts() {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => {
                alert.remove();
            }, 300);
        }, 5000);
    });
}

// Books management
const BooksManager = {
    init() {
        // Handle book item clicks
        const bookItems = document.querySelectorAll('.book-item, .book-card');
        bookItems.forEach(item => {
            item.addEventListener('dblclick', (e) => {
                const bookId = item.getAttribute('data-book-id');
                if (bookId) {
                    this.openBook(bookId);
                }
            });
        });
        
        // Handle action buttons
        const actionButtons = document.querySelectorAll('.action-btn');
        actionButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.stopPropagation();
                const action = button.getAttribute('data-action');
                const bookId = button.getAttribute('data-book-id');
                
                if (action && bookId) {
                    this.handleAction(action, bookId);
                }
            });
        });
    },
    
    openBook(bookId) {
        // Navigate to reading page
        // relative to current php location (user_dashboard/catalog)
        window.location.href = `reading.php?id=${bookId}`;
    },
    
    handleAction(action, bookId) {
        switch(action) {
            case 'read':
                this.openBook(bookId);
                break;
            case 'edit':
                this.editBook(bookId);
                break;
            case 'delete':
                if (confirm('Вы уверены, что хотите удалить эту книгу?')) {
                    this.deleteBook(bookId);
                }
                break;
            case 'reset':
                if (confirm('Сбросить прогресс чтения?')) {
                    this.resetProgress(bookId);
                }
                break;
        }
    },
    
    editBook(bookId) {
        // Open edit modal or navigate to edit page
        window.location.href = `php/edit_book.php?id=${bookId}`;
    },
    
    deleteBook(bookId) {
        // Send delete request
        fetch(`php/delete_book.php`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ book_id: bookId })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                location.reload();
            } else {
                alert('Ошибка при удалении книги: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Ошибка при удалении книги');
        });
    },
    
    resetProgress(bookId) {
        // Send reset progress request
        fetch(`php/reset_progress.php`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ book_id: bookId })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                location.reload();
            } else {
                alert('Ошибка при сбросе прогресса: ' + data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Ошибка при сбросе прогресса');
        });
    }
};

// Catalog search
const CatalogSearch = {
    init() {
        const searchInput = document.getElementById('catalogSearch');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.filterBooks(e.target.value);
            });
        }
    },
    
    filterBooks(searchTerm) {
        const bookCards = document.querySelectorAll('.book-card');
        const term = searchTerm.toLowerCase();
        
        bookCards.forEach(card => {
            const title = card.querySelector('.book-card-title')?.textContent.toLowerCase() || '';
            const author = card.querySelector('.book-card-author')?.textContent.toLowerCase() || '';
            
            if (title.includes(term) || author.includes(term)) {
                card.style.display = 'flex';
            } else {
                card.style.display = 'none';
            }
        });
        
        // Show/hide "no results" message
        const noResults = document.getElementById('noCatalogBooks');
        const visibleCount = Array.from(bookCards).filter(card => 
            card.style.display !== 'none'
        ).length;
        
        if (noResults) {
            if (visibleCount === 0 && term !== '') {
                noResults.style.display = 'block';
            } else {
                noResults.style.display = 'none';
            }
        }
    }
};

// Statistics
const Statistics = {
    init() {
        this.updateStatistics();
    },
    
    updateStatistics() {
        // Fetch and update statistics
        fetch('php/get_statistics.php')
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    this.displayStatistics(data.stats);
                }
            })
            .catch(error => {
                console.error('Error fetching statistics:', error);
            });
    },
    
    displayStatistics(stats) {
        const totalBooks = document.getElementById('totalBooks');
        const lastAdded = document.getElementById('lastAdded');
        
        if (totalBooks) {
            totalBooks.textContent = `Всего книг: ${stats.total || 0}`;
        }
        
        if (lastAdded) {
            lastAdded.textContent = `Последняя добавлена: ${stats.last_added || '-'}`;
        }
    }
};

// Initialize common functionality when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    ThemeManager.init();
    Navigation.init();
    autoDismissAlerts();
});

// Utility functions
const Utils = {
    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('ru-RU', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    },
    
    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `alert alert-${type}`;
        notification.textContent = message;
        notification.style.position = 'fixed';
        notification.style.top = '20px';
        notification.style.right = '20px';
        notification.style.zIndex = '9999';
        notification.style.minWidth = '300px';
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.style.opacity = '0';
            setTimeout(() => {
                notification.remove();
            }, 300);
        }, 3000);
    }
};

// Export for use in other scripts
window.ParadiseLibrary = {
    ThemeManager,
    Navigation,
    BooksManager,
    CatalogSearch,
    Statistics,
    Utils
};

