const API_BASE_URL = window.location.origin + '/api';

// Tab switching
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        const tab = btn.dataset.tab;
        
        // Update tab buttons
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        
        // Update forms
        document.querySelectorAll('.auth-form').forEach(f => f.classList.remove('active'));
        document.getElementById(`${tab}Form`).classList.add('active');
        
        // Clear errors
        clearErrors();
    });
});

function clearErrors() {
    document.getElementById('loginError').classList.remove('show');
    document.getElementById('registerError').classList.remove('show');
    document.getElementById('successMessage').classList.remove('show');
}

// Login form
document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors();
    
    const username = document.getElementById('loginUsername').value.trim();
    const password = document.getElementById('loginPassword').value;
    const errorDiv = document.getElementById('loginError');
    const submitBtn = e.target.querySelector('button[type="submit"]');
    
    if (!username || !password) {
        errorDiv.textContent = 'Vui lòng điền đầy đủ thông tin';
        errorDiv.classList.add('show');
        return;
    }
    
    submitBtn.disabled = true;
    submitBtn.textContent = 'Đang đăng nhập...';
    
    try {
        const response = await fetch(`${API_BASE_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username: username,
                passwordHash: password
            })
        });
        
        const data = await response.json();
        
        if (!response.ok) {
            throw new Error(data.message || 'Đăng nhập thất bại');
        }
        
        // Check if token exists in response
        if (!data.token) {
            throw new Error('Token không được trả về từ server');
        }
        
        // Save token to localStorage
        localStorage.setItem('token', data.token);
        localStorage.setItem('username', username);
        
        // Verify token was saved
        const savedToken = localStorage.getItem('token');
        const savedUsername = localStorage.getItem('username');
        
        console.log('[Login] Token saved successfully');
        console.log('[Login] Token exists in localStorage:', !!savedToken);
        console.log('[Login] Username saved:', savedUsername);
        console.log('[Login] Token preview:', savedToken ? savedToken.substring(0, 30) + '...' : 'null');
        
        if (!savedToken) {
            throw new Error('Không thể lưu token vào localStorage');
        }
        
        // Small delay to ensure localStorage is written (though it's synchronous)
        setTimeout(() => {
            console.log('[Login] Redirecting to chat page...');
            window.location.href = '/chat.html';
        }, 100);
        
    } catch (error) {
        errorDiv.textContent = error.message || 'Đăng nhập thất bại. Vui lòng thử lại.';
        errorDiv.classList.add('show');
        submitBtn.disabled = false;
        submitBtn.textContent = 'Đăng nhập';
    }
});

// Register form
document.getElementById('registerForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    clearErrors();
    
    const username = document.getElementById('registerUsername').value.trim();
    const password = document.getElementById('registerPassword').value;
    const passwordConfirm = document.getElementById('registerPasswordConfirm').value;
    const errorDiv = document.getElementById('registerError');
    const successDiv = document.getElementById('successMessage');
    const submitBtn = e.target.querySelector('button[type="submit"]');
    
    if (!username || !password || !passwordConfirm) {
        errorDiv.textContent = 'Vui lòng điền đầy đủ thông tin';
        errorDiv.classList.add('show');
        return;
    }
    
    if (password.length < 6) {
        errorDiv.textContent = 'Mật khẩu phải có ít nhất 6 ký tự';
        errorDiv.classList.add('show');
        return;
    }
    
    if (password !== passwordConfirm) {
        errorDiv.textContent = 'Mật khẩu xác nhận không khớp';
        errorDiv.classList.add('show');
        return;
    }
    
    submitBtn.disabled = true;
    submitBtn.textContent = 'Đang đăng ký...';
    
    try {
        const response = await fetch(`${API_BASE_URL}/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username: username,
                passwordHash: password
            })
        });
        
        const data = await response.json();
        
        if (!response.ok) {
            throw new Error(data.message || 'Đăng ký thất bại');
        }
        
        // Show success message
        successDiv.textContent = 'Đăng ký thành công! Vui lòng đăng nhập.';
        successDiv.classList.add('show');
        
        // Clear form
        document.getElementById('registerForm').reset();
        
        // Switch to login tab after 2 seconds
        setTimeout(() => {
            document.querySelector('[data-tab="login"]').click();
            document.getElementById('loginUsername').value = username;
            successDiv.classList.remove('show');
        }, 2000);
        
    } catch (error) {
        errorDiv.textContent = error.message || 'Đăng ký thất bại. Vui lòng thử lại.';
        errorDiv.classList.add('show');
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = 'Đăng ký';
    }
});

// Check if user is already logged in
window.addEventListener('DOMContentLoaded', () => {
    const token = localStorage.getItem('token');
    if (token) {
        window.location.href = '/chat.html';
    }
});

