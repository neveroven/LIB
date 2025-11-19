// Index page specific JavaScript - Authorization/Registration

document.addEventListener('DOMContentLoaded', function() {
    const loginTab = document.getElementById('login-tab');
    const registerTab = document.getElementById('register-tab');
    const loginForm = document.getElementById('loginForm');
    const registerForm = document.getElementById('registerForm');
    
    if (loginTab && registerTab && loginForm && registerForm) {
        loginTab.addEventListener('click', function(e) {
            e.preventDefault();
            loginTab.classList.add('active');
            registerTab.classList.remove('active');
            loginForm.style.display = 'block';
            registerForm.style.display = 'none';
        });
        
        registerTab.addEventListener('click', function(e) {
            e.preventDefault();
            registerTab.classList.add('active');
            loginTab.classList.remove('active');
            loginForm.style.display = 'none';
            registerForm.style.display = 'block';
        });
    }
    
    // Ensure forms submit correctly - disable other form inputs to prevent conflicts
    if (loginForm) {
        loginForm.addEventListener('submit', function(e) {
            // Don't prevent default - let form submit normally
            const registerForm = document.getElementById('registerForm');
            if (registerForm) {
                const registerInputs = registerForm.querySelectorAll('input[name]');
                registerInputs.forEach(input => {
                    input.disabled = true;
                });
            }
            // Ensure submit button is enabled
            const submitBtn = loginForm.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = false;
            }
        });
    }
    
    if (registerForm) {
        registerForm.addEventListener('submit', function(e) {
            // Don't prevent default - let form submit normally
            const loginForm = document.getElementById('loginForm');
            if (loginForm) {
                const loginInputs = loginForm.querySelectorAll('input[name]');
                loginInputs.forEach(input => {
                    input.disabled = true;
                });
            }
            // Ensure submit button is enabled
            const submitBtn = registerForm.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.disabled = false;
            }
        });
    }
});

