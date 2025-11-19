// Catalog page specific JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Initialize catalog search
    if (window.ParadiseLibrary && window.ParadiseLibrary.CatalogSearch) {
        window.ParadiseLibrary.CatalogSearch.init();
    }
    
    // Initialize navigation
    if (window.ParadiseLibrary && window.ParadiseLibrary.Navigation) {
        window.ParadiseLibrary.Navigation.init();
    }
});

