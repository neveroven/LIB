// Admin panel specific JavaScript

function loadFrame(url) {
    document.getElementById('adminFrame').src = url;
    
    // Update active button
    document.querySelectorAll('.nav-button').forEach(btn => {
        btn.classList.remove('active');
    });
    event.target.classList.add('active');
}

document.addEventListener('DOMContentLoaded', function() {
    // Set active button on load
    const frame = document.getElementById('adminFrame');
    if (frame) {
        const currentSrc = frame.src.split('/').pop();
        
        document.querySelectorAll('.nav-button').forEach(btn => {
            btn.classList.remove('active');
            const onclick = btn.getAttribute('onclick');
            if (onclick && onclick.includes(currentSrc)) {
                btn.classList.add('active');
            }
        });
    }
    
    // Initialize navigation if available
    if (window.ParadiseLibrary && window.ParadiseLibrary.Navigation) {
        window.ParadiseLibrary.Navigation.init();
    }
});

