// User Dashboard specific JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Initialize navigation
    if (window.ParadiseLibrary && window.ParadiseLibrary.Navigation) {
        window.ParadiseLibrary.Navigation.init();
    }
    
    // Initialize statistics
    if (window.ParadiseLibrary && window.ParadiseLibrary.Statistics) {
        window.ParadiseLibrary.Statistics.init();
    }
    
    // Handle book item clicks
    const bookItems = document.querySelectorAll('.book-item');
    bookItems.forEach(item => {
        item.addEventListener('dblclick', function() {
            const bookId = item.getAttribute('data-book-id');
            if (bookId) {
                // Navigate to book reading page
                window.location.href = `reading.php?id=${bookId}`;
            }
        });
    });
});

