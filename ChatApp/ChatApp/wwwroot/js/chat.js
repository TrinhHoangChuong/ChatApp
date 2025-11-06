const API_BASE_URL = window.location.origin + '/api';

let currentUserId = null;
let currentRoomId = null;
let currentReceiverId = null;
let currentType = null; // 'room' or 'dm'

// Check authentication
window.addEventListener('DOMContentLoaded', () => {
    console.log('[Chat] DOMContentLoaded event fired');
    
    const token = localStorage.getItem('token');
    const username = localStorage.getItem('username');
    
    console.log('[Chat] Page loaded. Token exists:', !!token, 'Username:', username);
    console.log('[Chat] Full localStorage check:');
    console.log('[Chat] - token:', token ? token.substring(0, 30) + '...' : 'null');
    console.log('[Chat] - username:', username);
    
    // Check all localStorage items for debugging
    console.log('[Chat] All localStorage items:');
    for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        const value = localStorage.getItem(key);
        console.log(`[Chat] - ${key}: ${value ? (value.length > 50 ? value.substring(0, 50) + '...' : value) : 'null'}`);
    }
    
    if (!token || !username) {
        console.log('[Chat] No token or username found, redirecting to login');
        console.log('[Chat] Token missing:', !token, 'Username missing:', !username);
        
        // Clear any partial data
        localStorage.clear();
        
        // Redirect to login
        setTimeout(() => {
            window.location.href = '/index.html';
        }, 500); // Small delay to allow logs to be seen
        return;
    }
    
    console.log('[Chat] Token found, initializing chat...');
    console.log('[Chat] Token preview:', token.substring(0, 30) + '...');
    
    // Get current user ID
    loadCurrentUser().then(() => {
        // Load initial data after user is loaded
        loadRoomsAndDMs();
        loadAllUsers();
        
        // Set up event handlers
        setupEventHandlers();
        
        // Auto-refresh every 2 seconds
        setInterval(() => {
            const currentToken = localStorage.getItem('token');
            if (!currentToken) {
                console.log('[Chat] Token lost during session, redirecting...');
                window.location.href = '/index.html';
                return;
            }
            if (currentRoomId) loadMessages();
            loadRoomsAndDMs();
        }, 2000);
    }).catch(error => {
        console.error('[Chat] Error initializing chat:', error);
    });
});

async function loadCurrentUser() {
    try {
        const token = localStorage.getItem('token');
        const username = localStorage.getItem('username');
        
        if (!token || !username) {
            console.error('[loadCurrentUser] Missing token or username');
            return;
        }
        
        console.log('[loadCurrentUser] Fetching user:', username);
        
        // Note: This endpoint doesn't require Authorization, but we send it anyway
        const response = await fetch(`${API_BASE_URL}/user/username/${encodeURIComponent(username)}`, {
            headers: { 
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        console.log('[loadCurrentUser] Response status:', response.status);
        
        if (response.status === 401) {
            console.error('[loadCurrentUser] Unauthorized - redirecting to login');
            localStorage.clear();
            window.location.href = '/index.html';
            return;
        }
        
        if (!response.ok) {
            console.error('[loadCurrentUser] Failed to load user, status:', response.status);
            // Don't redirect if user not found, just log
            if (response.status === 404) {
                console.warn('[loadCurrentUser] User not found in database');
            }
            return;
        }
        
        const user = await response.json();
        currentUserId = user.id || user.Id;
        console.log('[loadCurrentUser] Current user ID:', currentUserId);
    } catch (error) {
        console.error('[loadCurrentUser] Error loading current user:', error);
        // Don't redirect on network errors, just log
    }
}

async function loadRoomsAndDMs() {
    try {
        const token = localStorage.getItem('token');
        const username = localStorage.getItem('username');
        
        if (!token || !username) {
            console.error('[loadRoomsAndDMs] No token or username found, redirecting to login');
            window.location.href = '/index.html';
            return;
        }
        
        console.log('[loadRoomsAndDMs] Fetching rooms from:', `${API_BASE_URL}/room`);
        
        const response = await fetch(`${API_BASE_URL}/room`, {
            headers: { 
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        console.log('[loadRoomsAndDMs] Response status:', response.status, response.statusText);
        
        if (response.status === 401) {
            console.error('[loadRoomsAndDMs] Unauthorized - token invalid or expired');
            const errorData = await response.json().catch(() => ({}));
            console.error('[loadRoomsAndDMs] Error details:', errorData);
            localStorage.removeItem('token');
            localStorage.removeItem('username');
            window.location.href = '/index.html';
            return;
        }
        
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            console.error('[loadRoomsAndDMs] Error response:', errorData);
            // Don't redirect on other errors, just log and continue
            throw new Error(errorData.message || errorData.error || `Failed to load rooms (${response.status})`);
        }
        
        const data = await response.json();
        
        // Load public rooms - Backend trả về "PublicRooms" (P viết hoa)
        const roomsList = document.getElementById('roomsList');
        roomsList.innerHTML = '';
        
        const publicRooms = data.publicRooms || data.PublicRooms || [];
        if (publicRooms.length > 0) {
            publicRooms.forEach(room => {
                const roomItem = document.createElement('div');
                roomItem.className = 'room-item';
                roomItem.dataset.roomId = room.id;
                roomItem.textContent = room.name;
                roomItem.addEventListener('click', () => selectRoom(room.id, room.name));
                roomsList.appendChild(roomItem);
            });
        } else {
            roomsList.innerHTML = '<div style="color: #8e9297; font-size: 12px; padding: 8px;">Chưa có room nào</div>';
        }
        
        // Load DMs - Backend trả về "DirectMessages" (D viết hoa)
        const dmsList = document.getElementById('dmsList');
        dmsList.innerHTML = '';
        
        const directMessages = data.directMessages || data.DirectMessages || [];
        if (directMessages.length > 0) {
            directMessages.forEach(dm => {
                const dmItem = document.createElement('div');
                dmItem.className = 'dm-item';
                dmItem.dataset.roomId = dm.id;
                dmItem.textContent = dm.name;
                dmItem.addEventListener('click', () => selectDM(dm.id, dm.name));
                dmsList.appendChild(dmItem);
            });
        }
        
    } catch (error) {
        console.error('[loadRoomsAndDMs] Error loading rooms:', error);
        console.error('[loadRoomsAndDMs] Error message:', error.message);
        console.error('[loadRoomsAndDMs] Error stack:', error.stack);
        // Don't redirect on error, just log and continue - let user stay on page
    }
}

async function loadAllUsers() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            console.error('No token found in loadAllUsers');
            return;
        }
        
        const response = await fetch(`${API_BASE_URL}/user`, {
            headers: { 
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        if (response.status === 401) {
            console.error('Unauthorized in loadAllUsers');
            localStorage.clear();
            window.location.href = '/index.html';
            return;
        }
        
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || errorData.error || 'Failed to load users');
        }
        
        const users = await response.json();
        const allUsersList = document.getElementById('allUsersList');
        const currentUsername = localStorage.getItem('username');
        
        allUsersList.innerHTML = '';
        
        users.forEach(user => {
            if (user.username === currentUsername) return; // Skip current user
            
            const userItem = document.createElement('div');
            userItem.className = 'user-item-dm';
            userItem.innerHTML = `
                <div class="user-avatar-dm">${getAvatarEmoji(user.username)}</div>
                <div class="user-name-dm">${user.username}</div>
            `;
            userItem.addEventListener('click', () => selectUserForDM(user.id, user.username));
            allUsersList.appendChild(userItem);
        });
        
    } catch (error) {
        console.error('Error loading users:', error);
    }
}

async function selectRoom(roomId, roomName) {
    currentRoomId = roomId;
    currentReceiverId = null;
    currentType = 'room';
    
    // Update UI
    document.querySelectorAll('.room-item, .dm-item').forEach(item => item.classList.remove('active'));
    document.querySelector(`.room-item[data-room-id="${roomId}"]`)?.classList.add('active');
    
    document.getElementById('currentRoomName').textContent = roomName;
    document.getElementById('chatTitle').textContent = `# ${roomName}`;
    document.getElementById('chatSubtitle').textContent = 'Room chat';
    
    // Show/hide elements
    document.getElementById('roomMembersSection').style.display = 'block';
    document.getElementById('allUsersSection').style.display = 'none';
    document.getElementById('messageInputContainer').style.display = 'block';
    document.getElementById('joinRoomBtn').style.display = 'none';
    
    // Load room members
    await loadRoomMembers(roomId);
    
    // Load messages
    await loadMessages();
    
    // Check if user is member
    await checkRoomMembership(roomId);
}

async function selectUserForDM(userId, username) {
    try {
        const token = localStorage.getItem('token');
        
        // Create or get DM room
        const response = await fetch(`${API_BASE_URL}/room/dm/${userId}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: 'Failed to create/get DM' }));
            throw new Error(errorData.message || 'Failed to create/get DM');
        }
        
        const data = await response.json();
        
        // Backend trả về "RoomId" (PascalCase), cần check cả camelCase
        const roomId = data.roomId || data.RoomId;
        if (!roomId) {
            throw new Error('Invalid response from server');
        }
        
        currentRoomId = roomId;
        currentReceiverId = userId;
        currentType = 'dm';
        
        // Update UI
        document.querySelectorAll('.room-item, .dm-item').forEach(item => item.classList.remove('active'));
        document.querySelector(`.dm-item[data-room-id="${roomId}"]`)?.classList.add('active');
        
        document.getElementById('currentRoomName').textContent = username;
        document.getElementById('chatTitle').textContent = username;
        document.getElementById('chatSubtitle').textContent = 'Direct Message';
        
        // Show/hide elements
        document.getElementById('roomMembersSection').style.display = 'none';
        document.getElementById('allUsersSection').style.display = 'block';
        document.getElementById('messageInputContainer').style.display = 'block';
        document.getElementById('joinRoomBtn').style.display = 'none';
        
        // Load messages
        await loadMessages();
        
        // Reload DMs list
        loadRoomsAndDMs();
        
    } catch (error) {
        console.error('Error selecting user for DM:', error);
        alert('Không thể tạo DM. Vui lòng thử lại.');
    }
}

async function selectDM(roomId, dmName) {
    currentRoomId = roomId;
    currentReceiverId = null;
    currentType = 'dm';
    
    // Update UI
    document.querySelectorAll('.room-item, .dm-item').forEach(item => item.classList.remove('active'));
    document.querySelector(`.dm-item[data-room-id="${roomId}"]`)?.classList.add('active');
    
    document.getElementById('currentRoomName').textContent = dmName;
    document.getElementById('chatTitle').textContent = dmName;
    document.getElementById('chatSubtitle').textContent = 'Direct Message';
    
    // Show/hide elements
    document.getElementById('roomMembersSection').style.display = 'none';
    document.getElementById('allUsersSection').style.display = 'block';
    document.getElementById('messageInputContainer').style.display = 'block';
    document.getElementById('joinRoomBtn').style.display = 'none';
    
    // Load messages
    await loadMessages();
}

async function loadRoomMembers(roomId) {
    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`${API_BASE_URL}/room/${roomId}/members`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (!response.ok) throw new Error('Failed to load members');
        
        const members = await response.json();
        const membersList = document.getElementById('roomMembersList');
        const memberCount = document.getElementById('memberCount');
        
        memberCount.textContent = members.length;
        membersList.innerHTML = '';
        
        members.forEach(member => {
            const memberItem = document.createElement('div');
            memberItem.className = 'member-item';
            memberItem.innerHTML = `
                <div class="member-avatar">${getAvatarEmoji(member.username)}</div>
                <div class="member-name">${member.username}</div>
            `;
            membersList.appendChild(memberItem);
        });
        
    } catch (error) {
        console.error('Error loading room members:', error);
    }
}

async function checkRoomMembership(roomId) {
    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`${API_BASE_URL}/room/${roomId}/members`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.status === 403 || response.status === 401) {
            document.getElementById('joinRoomBtn').style.display = 'block';
            document.getElementById('messageInputContainer').style.display = 'none';
        } else {
            document.getElementById('joinRoomBtn').style.display = 'none';
        }
    } catch (error) {
        console.error('Error checking membership:', error);
    }
}

async function loadMessages() {
    if (!currentRoomId && !currentReceiverId) return;
    
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            console.error('No token found in loadMessages');
            return;
        }
        
        let response;
        
        if (currentType === 'room') {
            // Load messages from room
            response = await fetch(`${API_BASE_URL}/message/room/${currentRoomId}`, {
                headers: { 
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });
        } else {
            // For DM: if we have roomId, use it; otherwise use receiverId
            if (currentRoomId) {
                // Use room endpoint if we have the DM room ID
                response = await fetch(`${API_BASE_URL}/message/room/${currentRoomId}`, {
                    headers: { 
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    }
                });
            } else if (currentReceiverId) {
                // Fallback: use DM endpoint with receiver ID
                response = await fetch(`${API_BASE_URL}/message/dm/${currentReceiverId}`, {
                    headers: { 
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    }
                });
            } else {
                return; // No roomId or receiverId for DM
            }
        }
        
        if (response.status === 401) {
            console.error('Unauthorized in loadMessages');
            localStorage.clear();
            window.location.href = '/index.html';
            return;
        }
        
        if (!response.ok) {
            if (response.status === 403) {
                // User not a member, show join button
                return;
            }
            const errorData = await response.json().catch(() => ({}));
            throw new Error(errorData.message || errorData.error || 'Failed to load messages');
        }
        
        const messages = await response.json();
        const messagesList = document.getElementById('messagesList');
        const welcomeMessage = messagesList.querySelector('.welcome-message');
        
        // Store existing message IDs
        const existingIds = new Set();
        messagesList.querySelectorAll('.message').forEach(msg => {
            const id = msg.getAttribute('data-message-id');
            if (id) existingIds.add(id);
        });
        
        if (welcomeMessage && messages.length > 0) {
            welcomeMessage.remove();
        }
        
        // Display new messages only
        messages.forEach(msg => {
            const msgId = msg.id?.toString() || `${msg.senderId}-${msg.content}-${msg.timestamp}`;
            if (!existingIds.has(msgId)) {
                addMessageToUI(msg.sender || 'Unknown', msg.content, msg.timestamp, msg.id);
            }
        });
        
    } catch (error) {
        console.error('Error loading messages:', error);
    }
}

function addMessageToUI(sender, content, timestamp = null, messageId = null) {
    const messagesList = document.getElementById('messagesList');
    const welcomeMessage = messagesList.querySelector('.welcome-message');
    
    const uniqueId = messageId || `${sender}-${content}-${timestamp || Date.now()}`;
    const existingMessage = messagesList.querySelector(`[data-message-id="${uniqueId}"]`);
    
    if (existingMessage) return;
    
    if (welcomeMessage) welcomeMessage.remove();
    
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message';
    messageDiv.setAttribute('data-message-id', uniqueId);
    
    const currentUsername = localStorage.getItem('username');
    const displayName = sender === currentUsername ? 'Bạn' : sender;
    
    let timeString;
    if (timestamp) {
        const date = new Date(timestamp);
        timeString = date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    } else {
        timeString = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    }
    
    messageDiv.innerHTML = `
        <div class="message-avatar">${getAvatarEmoji(sender)}</div>
        <div class="message-content">
            <div class="message-header">
                <span class="message-sender">${displayName}</span>
                <span class="message-time">${timeString}</span>
            </div>
            <div class="message-text">${escapeHtml(content)}</div>
        </div>
    `;
    
    messagesList.appendChild(messageDiv);
    
    // Scroll to bottom
    const messagesContainer = document.getElementById('messagesContainer');
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

function getAvatarEmoji(username) {
    const emojis = ['👤', '👨', '👩', '🧑', '👧', '👦', '👨‍💻', '👩‍💻'];
    const index = username.charCodeAt(0) % emojis.length;
    return emojis[index];
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function setupEventHandlers() {
    // Logout
    document.getElementById('logoutBtn').addEventListener('click', () => {
        if (confirm('Bạn có chắc chắn muốn đăng xuất?')) {
            localStorage.removeItem('token');
            localStorage.removeItem('username');
            window.location.href = '/index.html';
        }
    });
    
    // Create room button
    document.getElementById('createRoomBtn').addEventListener('click', () => {
        document.getElementById('createRoomModal').style.display = 'flex';
    });
    
    // Close modal
    document.getElementById('closeModalBtn').addEventListener('click', () => {
        document.getElementById('createRoomModal').style.display = 'none';
    });
    
    // Create room form
    document.getElementById('createRoomForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const name = document.getElementById('roomName').value.trim();
        const description = document.getElementById('roomDescription').value.trim();
        
        if (!name) {
            alert('Vui lòng nhập tên room');
            return;
        }
        
        try {
            const token = localStorage.getItem('token');
            if (!token) {
                alert('Bạn chưa đăng nhập. Vui lòng đăng nhập lại.');
                window.location.href = '/index.html';
                return;
            }

            const response = await fetch(`${API_BASE_URL}/room`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ name, description })
            });
            
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: 'Failed to create room' }));
                throw new Error(errorData.message || 'Failed to create room');
            }
            
            const room = await response.json();
            
            // Backend có thể trả về id hoặc Id
            const roomId = room.id || room.Id;
            const roomName = room.name || room.Name;
            
            if (!roomId || !roomName) {
                throw new Error('Invalid response from server');
            }
            
            // Close modal
            document.getElementById('createRoomModal').style.display = 'none';
            document.getElementById('createRoomForm').reset();
            
            // Reload rooms
            loadRoomsAndDMs();
            
            // Select the new room
            setTimeout(() => selectRoom(roomId, roomName), 500);
            
        } catch (error) {
            console.error('Error creating room:', error);
            alert(error.message || 'Không thể tạo room. Vui lòng thử lại.');
        }
    });
    
    // Join room button
    document.getElementById('joinRoomBtn').addEventListener('click', async () => {
        if (!currentRoomId) return;
        
        try {
            const token = localStorage.getItem('token');
            const response = await fetch(`${API_BASE_URL}/room/${currentRoomId}/join`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });
            
            if (!response.ok) throw new Error('Failed to join room');
            
            // Reload room members and messages
            await loadRoomMembers(currentRoomId);
            await loadMessages();
            
            document.getElementById('joinRoomBtn').style.display = 'none';
            document.getElementById('messageInputContainer').style.display = 'block';
            
        } catch (error) {
            console.error('Error joining room:', error);
            alert('Không thể tham gia room. Vui lòng thử lại.');
        }
    });
    
    // Send message
    document.getElementById('messageForm').addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const message = document.getElementById('messageInput').value.trim();
        if (!message || !currentRoomId) return;
        
        const sendBtn = document.getElementById('sendBtn');
        sendBtn.disabled = true;
        sendBtn.innerHTML = '<span>...</span>';
        
        try {
            const token = localStorage.getItem('token');
            const response = await fetch(`${API_BASE_URL}/message`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    content: message,
                    roomId: currentType === 'room' ? currentRoomId : null,
                    receiverId: currentType === 'dm' ? currentReceiverId : null
                })
            });
            
            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: 'Failed to send message' }));
                throw new Error(errorData.message || 'Failed to send message');
            }
            
            const messageResponse = await response.json();
            
            // If DM and roomId was set by server, update currentRoomId
            if (currentType === 'dm' && messageResponse.roomId && !currentRoomId) {
                currentRoomId = messageResponse.roomId;
            }
            
            document.getElementById('messageInput').value = '';
            loadMessages();
            
        } catch (error) {
            console.error('Error sending message:', error);
            alert(error.message || 'Không thể gửi tin nhắn. Vui lòng thử lại.');
        } finally {
            sendBtn.disabled = false;
            sendBtn.innerHTML = '<span>➤</span>';
        }
    });
    
    // Send on Enter
    document.getElementById('messageInput').addEventListener('keypress', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            document.getElementById('messageForm').dispatchEvent(new Event('submit'));
        }
    });
}
