(() => {
    const API_BASE = window.location.origin;
    const SESSION_KEY = "chatapp.session.v2";
    const DM_THREADS_KEY = "chatapp.dm.threads";

    const state = {
        token: null,
        username: null,
        connection: null,
        typingTimeout: null,
        isTyping: false,
        servers: [],
        activeServerId: null,
        activeChannelId: null,
        activeView: "channel",
        activeDmTarget: null,
        directory: [],
        friends: [],
        incomingRequests: [],
        outgoingRequests: [],
        onlineUsers: [],
        channelMessages: new Map(),
        dmMessages: new Map(),
        uploadedFiles: [],
        unreadChannels: new Map(), // Map<channelId, count>
        unreadDms: new Map(), // Map<username, count>
        lastReadChannel: new Map(),
        lastReadDm: new Map(),
        typingInChannel: null, // { channelId, username }
        typingInDm: null, // { username }
        guildMembers: new Map(),
        invitingUsers: new Set(),
        joinedGuildIds: new Set(),
        guildInvitations: [],
    };

    const dmDedupKeys = new Map();

    const $ = (selector) => document.querySelector(selector);

    const els = {
        authOverlay: $("#auth-overlay"),
        appShell: $("#app-shell"),
        loginForm: $("#login-form"),
        registerForm: $("#register-form"),
        tabLogin: $("#tab-login"),
        tabRegister: $("#tab-register"),
        loginUsername: $("#login-username"),
        loginPassword: $("#login-password"),
        registerUsername: $("#register-username"),
        registerPassword: $("#register-password"),
        serverList: $("#server-list"),
        channelList: $("#channel-list"),
        dmList: $("#dm-list"),
        dmSearchInput: $("#dm-search-input"),
        dmFilter: $("#dm-filter"),
        dmCandidateList: $("#dm-candidate-list"),
        openDmBtn: $("#open-dm-btn"),
        openFriendCenter: $("#open-friend-center"),
        friendList: $("#friend-list"),
        incomingRequests: $("#incoming-requests"),
        outgoingRequests: $("#outgoing-requests"),
        addFriendForm: $("#add-friend-form"),
        addFriendUsername: $("#add-friend-username"),
        searchUserInput: $("#search-user-input"),
        searchUserList: $("#search-user-list"),
        createChannelBtn: $("#create-channel-btn"),
        openCreateServer: $("#open-create-server"),
        createServerModal: $("#create-server-modal"),
        createChannelModal: $("#create-channel-modal"),
        createDmModal: $("#create-dm-modal"),
        createServerForm: $("#create-server-form"),
        createChannelForm: $("#create-channel-form"),
        messageList: $("#message-list"),
        messageInput: $("#message-input"),
        composer: $("#composer"),
        typingIndicator: $("#typing-indicator"),
        fileInput: $("#file-input"),
        toast: $("#toast"),
        sessionUsername: $("#session-username"),
        sessionAvatar: $("#session-avatar"),
        logoutBtn: $("#logout-btn"),
        activeServerName: $("#active-server-name"),
        activeServerDesc: $("#active-server-desc"),
        activeChannelName: $("#active-channel-name"),
        activeChannelTopic: $("#active-channel-topic"),
        onlineUsers: $("#online-users"),
        onlineCount: $("#online-count"),
        directoryUsers: $("#directory-users"),
        userSearch: $("#user-search"),
        eventLog: $("#event-log"),
        uploadedFiles: $("#uploaded-files"),
        memberPane: $("#member-pane"),
        toggleMemberListBtn: $("#toggle-memberlist"),
        globalSearch: $("#global-search"),
        emojiBtn: $("#emoji-btn"),
        inviteFriendBtn: $("#invite-friend-btn"),
        inviteFriendModal: $("#invite-friend-modal"),
        inviteFriendList: $("#invite-friend-list"),
        imageViewerModal: $("#image-viewer-modal"),
        imageViewerImg: $("#image-viewer-img"),
    };

    const modalCloseSelector = "[data-close-modal]";

    let notificationPermission = null;

    async function requestNotificationPermission() {
        if (!("Notification" in window)) return false;
        if (notificationPermission !== null) return notificationPermission === "granted";
        if (Notification.permission === "granted") {
            notificationPermission = "granted";
            return true;
        }
        if (Notification.permission !== "denied") {
            const permission = await Notification.requestPermission();
            notificationPermission = permission;
            return permission === "granted";
        }
        return false;
    }

    function showNotification(title, body, icon = null) {
        if (!("Notification" in window)) return;
        if (Notification.permission === "granted") {
            new Notification(title, { body, icon: icon || undefined, tag: title });
        }
    }

    document.addEventListener("visibilitychange", () => {
        if (!document.hidden && Notification.permission === "default") {
            requestNotificationPermission();
        }
    });

    function showToast(message, type = "info", timeout = 4200) {
        if (!els.toast) return;
        const schemes = {
            success: { border: "rgba(61, 214, 140, 0.6)", shadow: "0 18px 32px rgba(11, 38, 22, 0.45)" },
            warning: { border: "rgba(250, 204, 21, 0.6)", shadow: "0 18px 32px rgba(36, 29, 4, 0.45)" },
            error: { border: "rgba(248, 113, 113, 0.6)", shadow: "0 18px 32px rgba(46, 7, 11, 0.45)" },
            info: { border: "rgba(99, 102, 241, 0.25)", shadow: "0 18px 32px rgba(4, 6, 18, 0.45)" },
        };
        const palette = schemes[type] ?? schemes.info;
        els.toast.textContent = message;
        els.toast.style.borderColor = palette.border;
        els.toast.style.boxShadow = palette.shadow;
        els.toast.classList.add("show");
        clearTimeout(els.toast._timer);
        els.toast._timer = setTimeout(() => els.toast.classList.remove("show"), timeout);
    }

    function saveSession(session) {
        if (!session) {
            localStorage.removeItem(SESSION_KEY);
            return;
        }
        localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    }

    function restoreSession() {
        try {
            const raw = localStorage.getItem(SESSION_KEY);
            if (!raw) return false;
            const session = JSON.parse(raw);
            if (!session?.token || !session?.username) return false;
            state.token = session.token;
            state.username = session.username;
            return true;
        } catch {
            return false;
        }
    }

    function loadSavedDmThreads() {
        try {
            const raw = localStorage.getItem(DM_THREADS_KEY);
            if (!raw) return;
            const threads = JSON.parse(raw);
            if (Array.isArray(threads)) {
                threads.forEach((user) => {
                    if (user && user !== state.username) {
                        ensureDmThread(user);
                    }
                });
            }
        } catch {
            /* ignore */
        }
    }

    function persistDmThreads() {
        localStorage.setItem(DM_THREADS_KEY, JSON.stringify(Array.from(state.dmMessages.keys())));
    }

    function initials(username) {
        if (!username) return "??";
        return username
            .split(/\s+/)
            .map((part) => part[0])
            .join("")
            .slice(0, 2)
            .toUpperCase();
    }

    function toggleShell(showAuth) {
        els.authOverlay.classList.toggle("hidden", !showAuth);
        els.appShell.classList.toggle("hidden", showAuth);
    }

    function showModal(modal) {
        if (modal) modal.classList.remove("hidden");
    }

    function hideModal(modal) {
        if (modal) modal.classList.add("hidden");
    }

    function closeAllModals() {
        hideModal(els.createServerModal);
        hideModal(els.createChannelModal);
        hideModal(els.createDmModal);
        hideModal(els.inviteFriendModal);
        hideModal(els.imageViewerModal);
    }

    function showImageModal(imageUrl, altText) {
        if (els.imageViewerImg && els.imageViewerModal) {
            els.imageViewerImg.src = imageUrl;
            els.imageViewerImg.alt = altText || "Ảnh";
            showModal(els.imageViewerModal);
        }
    }

    document.addEventListener("click", (event) => {
        if (event.target.matches(modalCloseSelector)) {
            closeAllModals();
        }
    });

    function formatTimestamp(value) {
        if (!value) return "";
        const date = value instanceof Date ? value : new Date(value);
        return date.toLocaleString(undefined, {
            hour: "2-digit",
            minute: "2-digit",
            day: "2-digit",
            month: "2-digit",
        });
    }

    function ensureChannelMessages(serverId, channelId) {
        const key = `${serverId}#${channelId}`;
        if (!state.channelMessages.has(key)) {
            state.channelMessages.set(key, []);
        }
        return state.channelMessages.get(key);
    }

    function ensureDmThread(username) {
        if (!state.dmMessages.has(username)) {
            state.dmMessages.set(username, []);
            persistDmThreads();
        }
        return state.dmMessages.get(username);
    }

    function findGuildByChannel(channelId) {
        return state.servers.find((guild) => guild.channels.some((c) => c.id === channelId));
    }

    function renderServers() {
        els.serverList.innerHTML = "";
        state.servers.forEach((server) => {
            const li = document.createElement("li");
            const btn = document.createElement("button");
            btn.className = "server-pill";
            btn.textContent = (server.name || "").slice(0, 2).toUpperCase();
            btn.title = server.name;
            if (server.id === state.activeServerId) btn.classList.add("active");
            btn.addEventListener("click", () => setActiveServer(server.id));
            li.append(btn);
            els.serverList.append(li);
        });
    }

    function showChannelMenu(event, channel, guildId) {
        const menu = document.createElement("div");
        menu.className = "channel-menu";
        menu.style.position = "fixed";
        menu.style.left = `${event.clientX}px`;
        menu.style.top = `${event.clientY}px`;
        menu.style.zIndex = "10000";
        
        const editBtn = document.createElement("button");
        editBtn.className = "menu-item";
        editBtn.textContent = "✏️ Sửa kênh";
        editBtn.addEventListener("click", () => {
            const newName = prompt("Tên kênh mới:", channel.name);
            if (newName && newName.trim() && newName.trim() !== channel.name) {
                updateChannel(guildId, channel.id, newName.trim(), channel.topic);
            }
            menu.remove();
        });
        
        const deleteBtn = document.createElement("button");
        deleteBtn.className = "menu-item danger";
        deleteBtn.textContent = "🗑️ Xóa kênh";
        deleteBtn.addEventListener("click", () => {
            if (confirm(`Bạn có chắc muốn xóa kênh #${channel.name}? Tất cả tin nhắn trong kênh sẽ bị xóa.`)) {
                deleteChannel(guildId, channel.id);
            }
            menu.remove();
        });
        
        menu.append(editBtn, deleteBtn);
        document.body.append(menu);
        
        const closeMenu = (e) => {
            if (!menu.contains(e.target) && e.target !== event.target) {
                menu.remove();
                document.removeEventListener("click", closeMenu);
            }
        };
        setTimeout(() => document.addEventListener("click", closeMenu), 100);
    }

    async function updateChannel(guildId, channelId, name, topic) {
        try {
            const result = await fetchJson(`${API_BASE}/api/Guilds/${guildId}/channels/${channelId}`, {
                method: "PUT",
                body: JSON.stringify({ name, topic }),
            });
            showToast("Đã cập nhật kênh!", "success");
            await fetchGuilds();
        } catch (error) {
            showToast(error.message || "Không thể cập nhật kênh.", "error");
        }
    }

    async function deleteChannel(guildId, channelId) {
        try {
            await fetchJson(`${API_BASE}/api/Guilds/${guildId}/channels/${channelId}`, {
                method: "DELETE",
            });
            showToast("Đã xóa kênh!", "success");
            await fetchGuilds();
            // Nếu đang ở kênh bị xóa, chuyển sang kênh đầu tiên
            if (state.activeChannelId === channelId) {
                const server = state.servers.find(s => s.id === guildId);
                if (server && server.channels.length > 0) {
                    setActiveChannel(guildId, server.channels[0].id);
                }
            }
        } catch (error) {
            showToast(error.message || "Không thể xóa kênh.", "error");
        }
    }

    function renderChannels() {
        const server = state.servers.find((s) => s.id === state.activeServerId);
        if (!server) return;

        els.activeServerName.textContent = server.name;
        els.activeServerDesc.textContent = server.description || "Cộng đồng thân thiện";
        els.channelList.innerHTML = "";

        server.channels.forEach((channel) => {
            const li = document.createElement("li");
            li.className = "channel-item";
            if (state.activeView === "channel" && channel.id === state.activeChannelId) {
                li.classList.add("active");
            }
            const hash = document.createElement("span");
            hash.className = "hash";
            hash.textContent = "#";
            const label = document.createElement("span");
            label.textContent = channel.name;
            li.append(hash, label);
            const unreadCount = state.unreadChannels.get(channel.id) || 0;
            if (unreadCount > 0) {
                const badge = document.createElement("span");
                badge.className = "unread-badge";
                badge.textContent = unreadCount > 99 ? "99+" : unreadCount.toString();
                li.append(badge);
            }
            
            // Thêm menu xóa/sửa cho owner/admin
            const serverRole = server.role;
            if (serverRole === "owner" || serverRole === "admin") {
                const menuBtn = document.createElement("button");
                menuBtn.className = "channel-menu-btn";
                menuBtn.textContent = "⋯";
                menuBtn.title = "Tùy chọn kênh";
                menuBtn.addEventListener("click", (e) => {
                    e.stopPropagation();
                    showChannelMenu(e, channel, server.id);
                });
                li.append(menuBtn);
            }
            
            li.addEventListener("click", () => setActiveChannel(server.id, channel.id));
            els.channelList.append(li);
        });
    }

    function renderDmList() {
        els.dmList.innerHTML = "";
        const friendSet = new Set(state.friends.map((f) => f.username));
        const dmSet = new Set(Array.from(state.dmMessages.keys()));
        const usernames = Array.from(new Set([...friendSet, ...dmSet])).filter(Boolean).sort((a, b) => a.localeCompare(b));

        usernames.forEach((username) => {
            const li = document.createElement("li");
            li.className = "dm-item";
            if (state.activeView === "dm" && state.activeDmTarget === username) {
                li.classList.add("active");
            }
            const avatar = document.createElement("span");
            avatar.className = "avatar small";
            avatar.textContent = initials(username);
            const label = document.createElement("span");
            label.textContent = username;
            li.append(avatar, label);
            const unreadCount = state.unreadDms.get(username) || 0;
            if (unreadCount > 0) {
                const badge = document.createElement("span");
                badge.className = "unread-badge";
                badge.textContent = unreadCount > 99 ? "99+" : unreadCount.toString();
                li.append(badge);
            }
            li.addEventListener("click", () => openDirectConversation(username));
            els.dmList.append(li);
        });
    }

    function updateOnlineUsers() {
        els.onlineUsers.innerHTML = "";
        const unique = [...new Set(state.onlineUsers)];
        els.onlineCount.textContent = unique.length;
        unique.forEach((user) => {
            const li = document.createElement("li");
            li.className = "member-item online";
            const avatar = document.createElement("span");
            avatar.className = "avatar small";
            avatar.textContent = initials(user);
            const label = document.createElement("span");
            label.textContent = user;
            li.append(avatar, label);
            els.onlineUsers.append(li);
        });
    }

    function renderDirectory() {
        const query = (els.userSearch.value || "").toLowerCase();
        els.directoryUsers.innerHTML = "";
        state.directory
            .filter((user) => user.username.toLowerCase().includes(query))
            .forEach((user) => {
                const li = document.createElement("li");
                li.className = "member-item";
                const avatar = document.createElement("span");
                avatar.className = "avatar small";
                avatar.textContent = initials(user.username);
                const label = document.createElement("span");
                label.textContent = user.username;
                li.append(avatar, label);
                // Click vào user để gửi lời mời hoặc mở DM nếu đã là bạn
                li.addEventListener("click", () => {
                    const isFriend = state.friends.some(f => f.username === user.username);
                    if (isFriend) {
                        openDirectConversation(user.username);
                    } else {
                        sendFriendRequest(user.username);
                    }
                });
                els.directoryUsers.append(li);
            });
    }

    function renderSearchUsers() {
        if (!els.searchUserList) return;
        const query = (els.searchUserInput?.value || "").toLowerCase();
        els.searchUserList.innerHTML = "";
        
        if (!state.directory || state.directory.length === 0) {
            const empty = document.createElement("li");
            empty.className = "modal-empty";
            empty.textContent = "Đang tải danh sách người dùng...";
            els.searchUserList.append(empty);
            return;
        }

        const friendUsernames = new Set(state.friends.map(f => f.username.toLowerCase()));
        const outgoingUsernames = new Set(state.outgoingRequests.map(r => ((r.username ?? r.Username) || "").toLowerCase()));
        
        const filtered = state.directory
            .filter((user) => {
                const username = user.username.toLowerCase();
                // Loại bỏ chính mình
                if (username === state.username?.toLowerCase()) return false;
                // Lọc theo query
                if (query && !username.includes(query)) return false;
                return true;
            })
            .slice(0, 10); // Giới hạn 10 kết quả

        if (filtered.length === 0) {
            const empty = document.createElement("li");
            empty.className = "modal-empty";
            empty.textContent = query ? "Không tìm thấy người dùng." : "Nhập username để tìm kiếm...";
            els.searchUserList.append(empty);
            return;
        }

        filtered.forEach((user) => {
            const username = user.username.toLowerCase();
            const isFriend = friendUsernames.has(username);
            const hasPendingRequest = outgoingUsernames.has(username);
            
            const li = document.createElement("li");
            li.className = "modal-item";
            
            const name = document.createElement("span");
            name.textContent = user.username;
            
            const actions = document.createElement("div");
            actions.className = "modal-actions";
            
            if (isFriend) {
                const chatBtn = document.createElement("button");
                chatBtn.type = "button";
                chatBtn.textContent = "Nhắn tin";
                chatBtn.addEventListener("click", () => {
                    hideModal(els.createDmModal);
                    openDirectConversation(user.username);
                });
                actions.append(chatBtn);
            } else if (hasPendingRequest) {
                const note = document.createElement("span");
                note.className = "modal-note";
                note.textContent = "Đã gửi lời mời";
                actions.append(note);
            } else {
                const addBtn = document.createElement("button");
                addBtn.type = "button";
                addBtn.textContent = "Gửi lời mời";
                addBtn.classList.add("primary-btn", "small");
                addBtn.addEventListener("click", () => sendFriendRequest(user.username));
                actions.append(addBtn);
            }
            
            li.append(name, actions);
            els.searchUserList.append(li);
        });
    }

    function addEventLog(message) {
        const li = document.createElement("li");
        li.textContent = `${formatTimestamp(new Date())} • ${message}`;
        els.eventLog.prepend(li);
        if (els.eventLog.children.length > 40) {
            els.eventLog.removeChild(els.eventLog.lastChild);
        }
    }

    function addUploadedFile({ fileUrl, fileName }) {
        const li = document.createElement("li");
        const link = document.createElement("a");
        link.href = fileUrl;
        link.target = "_blank";
        link.rel = "noopener noreferrer";
        link.textContent = fileName;
        li.append(link);
        els.uploadedFiles.prepend(li);
        state.uploadedFiles.unshift({ fileUrl, fileName });
        if (els.uploadedFiles.children.length > 25) {
            els.uploadedFiles.removeChild(els.uploadedFiles.lastChild);
        }
    }

    function clearMessages() {
        els.messageList.innerHTML = "";
    }

    function scrollMessagesToBottom() {
        if (!els.messageList) return;
        requestAnimationFrame(() => {
            els.messageList.scrollTop = els.messageList.scrollHeight;
        });
    }

    function makeDmDedupKey(sender, recipient, content, mediaUrl, timestamp) {
        return `${sender}|${recipient}|${content ?? ""}|${mediaUrl ?? ""}|${timestamp ?? ""}`;
    }

    function shouldSkipDuplicateDm(peer, key) {
        const lastKey = dmDedupKeys.get(peer);
        if (lastKey === key) {
            return true;
        }
        dmDedupKeys.set(peer, key);
        return false;
    }

    function resetDmDedup(peer) {
        dmDedupKeys.delete(peer);
    }

    function isImageUrl(url) {
        if (!url) return false;
        try {
            const clean = url.split("?")[0];
            return /\.(png|jpe?g|gif|webp|bmp|svg)$/i.test(clean);
        } catch {
            return false;
        }
    }

    function appendMessage(message, scroll = true) {
        const row = document.createElement("div");
        row.className = "message-row";
        if (message.id) {
            row.setAttribute("data-message-id", message.id);
        }
        const isSelf =
            typeof message.sender === "string" &&
            typeof state.username === "string" &&
            message.sender.trim().toLowerCase() === state.username.trim().toLowerCase();
        if (isSelf) {
            row.classList.add("self");
        }

        const avatar = document.createElement("div");
        avatar.className = "avatar";
        avatar.textContent = initials(message.sender);

        const bubble = document.createElement("div");
        bubble.className = "message-bubble";
        if (isSelf) bubble.classList.add("self");

        const meta = document.createElement("div");
        meta.className = "message-meta";
        const sender = document.createElement("span");
        sender.className = "message-sender";
        sender.textContent = message.sender;
        const time = document.createElement("span");
        time.className = "message-time";
        time.textContent = formatTimestamp(message.timestamp);
        meta.append(sender, time);
        
        if (isSelf && message.id && typeof message.id === 'number' && message.id > 0) {
            const deleteBtn = document.createElement("button");
            deleteBtn.className = "message-delete";
            deleteBtn.textContent = "🗑️";
            deleteBtn.title = "Xóa tin nhắn";
            deleteBtn.addEventListener("click", (e) => {
                e.stopPropagation();
                if (confirm("Bạn có chắc muốn xóa tin nhắn này?")) {
                    deleteMessage(message.id);
                }
            });
            meta.append(deleteBtn);
        }

        const content = document.createElement("div");
        content.className = "message-content";
        content.textContent = message.content ?? "";

        bubble.append(meta, content);

        if (message.mediaUrl) {
            const attachment = document.createElement("div");
            attachment.className = "attachment";
            if (isImageUrl(message.mediaUrl)) {
                const img = document.createElement("img");
                img.src = message.mediaUrl;
                img.alt = message.content || "attachment";
                img.style.cursor = "pointer";
                img.style.maxWidth = "400px";
                img.style.maxHeight = "400px";
                img.style.borderRadius = "8px";
                img.addEventListener("click", () => {
                    showImageModal(message.mediaUrl, message.content || "Ảnh");
                });
                attachment.append(img);
            } else {
                const link = document.createElement("a");
                link.href = message.mediaUrl;
                link.download = message.content || "file";
                link.target = "_blank";
                link.rel = "noopener noreferrer";
                link.textContent = `📎 ${message.content || "Tải tệp"}`;
                link.style.cursor = "pointer";
                link.style.textDecoration = "underline";
                link.style.color = "var(--accent)";
                attachment.append(link);
            }
            bubble.append(attachment);
        }

        row.append(avatar, bubble);
        els.messageList.append(row);
        if (scroll) {
            scrollMessagesToBottom();
        }
    }

    function renderMessages(messages) {
        clearMessages();
        messages.forEach((msg) => appendMessage(msg, false));
        scrollMessagesToBottom();
    }

    async function fetchJson(url, options = {}) {
        const headers = options.headers ? { ...options.headers } : {};
        if (state.token) headers.Authorization = `Bearer ${state.token}`;
        const response = await fetch(url, {
            ...options,
            headers: {
                "Content-Type": "application/json",
                ...headers,
            },
        });
        if (!response.ok) {
            let errorText = await response.text();
            try {
                const errorJson = JSON.parse(errorText);
                if (errorJson.message) errorText = errorJson.message;
            } catch {}
            const error = new Error(errorText || `Request failed: ${response.status}`);
            error.status = response.status;
            throw error;
        }
        if (response.status === 204) return null;
        return response.json();
    }

    async function loadChannelHistory(serverId, channelId) {
        const key = `${serverId}#${channelId}`;
        try {
            const data = await fetchJson(`${API_BASE}/api/Message?channelId=${channelId}&limit=200`);
            const list = Array.isArray(data) ? data : [];
            state.channelMessages.set(key, list);
            if (state.activeView === "channel" && state.activeChannelId === channelId) {
                renderMessages(list);
            }
        } catch (error) {
            console.error(error);
            showToast("Không thể tải lịch sử kênh.", "error");
        }
    }

    async function loadDirectory() {
        try {
            const users = await fetchJson(`${API_BASE}/api/User`);
            state.directory = Array.isArray(users) ? users : [];
            renderDirectory();
        } catch (error) {
            console.error(error);
        }
    }

    async function loadDmHistory(username) {
        ensureDmThread(username);
        try {
            const params = new URLSearchParams({ userA: state.username, userB: username });
            const data = await fetchJson(`${API_BASE}/api/Message/conversation?${params}`);
            const list = Array.isArray(data) ? data : [];
            state.dmMessages.set(username, list);
            resetDmDedup(username);
            if (state.activeView === "dm" && state.activeDmTarget === username) {
                renderMessages(list);
            }
        } catch (error) {
            console.error(error);
            showToast("Không thể tải lịch sử tin nhắn riêng.", "error");
        }
    }

    function buildDmCandidates() {
        if (!els.dmCandidateList) return;
        const filter = (els.dmFilter?.value || "").toLowerCase();
        els.dmCandidateList.innerHTML = "";
        state.friends
            .filter((friend) => friend.username.toLowerCase().includes(filter))
            .forEach((friend) => {
                const li = document.createElement("li");
                li.className = "modal-item";
                const name = document.createElement("span");
                name.textContent = friend.username;
                const action = document.createElement("button");
                action.type = "button";
                action.textContent = "Mở chat";
                action.addEventListener("click", () => {
                    ensureDmThread(friend.username);
                    renderDmList();
                    hideModal(els.createDmModal);
                    openDirectConversation(friend.username);
                });
                li.append(name, action);
                els.dmCandidateList.append(li);
            });
    }

    async function fetchGuilds() {
        try {
            let guilds = await fetchJson(`${API_BASE}/api/Guilds`);
            if (!Array.isArray(guilds)) {
                guilds = [];
            }
            
            // Backend đã trả về tất cả guilds mà user là member (owner hoặc member)
            // Nếu user chưa có guild nào, tạo server riêng
            if (guilds.length === 0) {
                const created = await fetchJson(`${API_BASE}/api/Guilds`, {
                    method: "POST",
                    body: JSON.stringify({
                        name: `${state.username}'s server`,
                        description: "Máy chủ cá nhân",
                    }),
                });
                guilds = [created];
            }

            guilds.forEach((guild) => {
                if (!Array.isArray(guild.channels) || guild.channels.length === 0) {
                    guild.channels = [
                        {
                            id: guild.Id != null ? guild.Id * 1000 : Date.now(),
                            name: "general",
                            topic: "Nơi trò chuyện chung",
                        },
                    ];
                }
            });

            const newServers = guilds.map((guild) => ({
                id: guild.Id ?? guild.id,
                name: guild.Name ?? guild.name,
                description: guild.Description ?? guild.description,
                ownerId: guild.OwnerId ?? guild.ownerId,
                role: guild.Role ?? guild.role,
                channels: (guild.channels ?? []).map((channel) => ({
                    id: channel.Id ?? channel.id,
                    name: channel.Name ?? channel.name,
                    topic: channel.Topic ?? channel.topic,
                })),
            }));

            // Reset state.servers và joinedGuildIds để đảm bảo chỉ có guilds hiện tại
            state.servers = newServers;
            if (!state.joinedGuildIds) {
                state.joinedGuildIds = new Set();
            }
            // Clear và chỉ giữ lại guilds hiện tại
            state.joinedGuildIds.clear();

            state.activeServerId = state.servers[0]?.id ?? null;
            state.activeChannelId = state.servers[0]?.channels[0]?.id ?? null;
        } catch (error) {
            console.error("Failed to load guilds", error);
            showToast("Không thể tải danh sách máy chủ.", "error");
            state.servers = [];
            state.activeServerId = null;
            state.activeChannelId = null;
            if (state.joinedGuildIds) {
                state.joinedGuildIds.clear();
            }
        }

        renderServers();
        renderChannels();
        renderDmList();
        updateChannelHeader();
        updateComposerPlaceholder();
        joinGuildGroups().catch(() => null);
    }

    async function fetchFriends() {
        try {
            const friends = await fetchJson(`${API_BASE}/api/Friends`);
            state.friends = Array.isArray(friends)
                ? friends.map((f) => ({ id: f.Id ?? f.id, username: f.Username ?? f.username }))
                : [];
        } catch (error) {
            console.error("Failed to load friends", error);
            state.friends = [];
        }
        renderFriends();
        renderDmList();
        buildDmCandidates();
    }

    async function fetchFriendRequests() {
        try {
            const data = await fetchJson(`${API_BASE}/api/Friends/requests`);
            state.incomingRequests = Array.isArray(data?.incoming) ? data.incoming : [];
            state.outgoingRequests = Array.isArray(data?.outgoing) ? data.outgoing : [];
        } catch (error) {
            console.error("Failed to load friend requests", error);
            state.incomingRequests = [];
            state.outgoingRequests = [];
        }
        renderFriendRequests();
    }

    async function sendFriendRequest(username) {
        if (!username || username.trim() === "") {
            showToast("Nhập username để gửi lời mời.", "warning");
            return;
        }
        try {
            await fetchJson(`${API_BASE}/api/Friends/request`, {
                method: "POST",
                body: JSON.stringify({ username: username.trim() }),
            });
            showToast("Đã gửi lời mời kết bạn!", "success");
            await fetchFriendRequests();
            // Clear input và refresh search
            els.addFriendUsername.value = "";
            renderSearchUsers();
        } catch (error) {
            showToast(error.message || "Không thể gửi lời mời.", "error");
        }
    }

    async function respondFriendRequest(requestId, accept) {
        try {
            const endpoint = accept ? "accept" : "reject";
            await fetchJson(`${API_BASE}/api/Friends/requests/${requestId}/${endpoint}`, { method: "POST" });
            await fetchFriends();
            await fetchFriendRequests();
            if (accept) {
                showToast("Đã chấp nhận lời mời kết bạn!", "success");
                // Tự động refresh danh sách bạn bè và DM
                renderFriends();
                renderDmList();
            } else {
                showToast("Đã từ chối lời mời.", "info");
            }
        } catch (error) {
            showToast(error.message || "Không thể xử lý yêu cầu.", "error");
        }
    }

    async function removeFriend(friendId) {
        try {
            await fetchJson(`${API_BASE}/api/Friends/${friendId}`, { method: "DELETE" });
            await fetchFriends();
        } catch (error) {
            showToast(error.message || "Không thể xoá bạn bè.", "error");
        }
    }

    function renderFriends() {
        if (!els.friendList) return;
        els.friendList.innerHTML = "";

        if (state.friends.length === 0) {
            const empty = document.createElement("li");
            empty.className = "modal-empty";
            empty.textContent = "Chưa có bạn bè. Hãy gửi lời mời!";
            els.friendList.append(empty);
            return;
        }

        state.friends
            .slice()
            .sort((a, b) => a.username.localeCompare(b.username))
            .forEach((friend) => {
                const li = document.createElement("li");
                li.className = "modal-item";
                // Click vào item để mở chat
                li.addEventListener("click", (e) => {
                    // Chỉ mở chat nếu không click vào button
                    if (e.target.tagName !== "BUTTON") {
                        ensureDmThread(friend.username);
                        hideModal(els.createDmModal);
                        openDirectConversation(friend.username);
                    }
                });

                const name = document.createElement("span");
                name.textContent = friend.username;
                name.style.cursor = "pointer";

                const actions = document.createElement("div");
                actions.className = "modal-actions";

                const chatBtn = document.createElement("button");
                chatBtn.type = "button";
                chatBtn.textContent = "Nhắn tin";
                chatBtn.addEventListener("click", (e) => {
                    e.stopPropagation();
                    ensureDmThread(friend.username);
                    hideModal(els.createDmModal);
                    openDirectConversation(friend.username);
                });

                const removeBtn = document.createElement("button");
                removeBtn.type = "button";
                removeBtn.textContent = "Gỡ";
                removeBtn.classList.add("danger");
                removeBtn.addEventListener("click", (e) => {
                    e.stopPropagation();
                    if (confirm(`Bạn có chắc muốn gỡ ${friend.username} khỏi danh sách bạn bè?`)) {
                        removeFriend(friend.id);
                    }
                });

                actions.append(chatBtn, removeBtn);
                li.append(name, actions);
                els.friendList.append(li);
            });
    }

    async function fetchGuildInvitations() {
        try {
            const invitations = await fetchJson(`${API_BASE}/api/Guilds/invitations`);
            state.guildInvitations = Array.isArray(invitations) ? invitations : [];
            renderGuildInvitations();
        } catch (error) {
            console.error("Failed to fetch guild invitations", error);
            state.guildInvitations = [];
        }
    }

    function renderGuildInvitations() {
        // Hiển thị toast khi có invitation mới
        // User sẽ thấy notification và có thể chấp nhận/từ chối
    }

    async function acceptGuildInvitation(invitationId) {
        try {
            await fetchJson(`${API_BASE}/api/Guilds/invitations/${invitationId}/accept`, {
                method: "POST",
            });
            showToast("Đã tham gia máy chủ!", "success");
            await fetchGuildInvitations();
            await fetchGuilds();
        } catch (error) {
            showToast(error.message || "Không thể chấp nhận lời mời.", "error");
        }
    }

    async function rejectGuildInvitation(invitationId) {
        try {
            await fetchJson(`${API_BASE}/api/Guilds/invitations/${invitationId}/reject`, {
                method: "POST",
            });
            showToast("Đã từ chối lời mời.", "info");
            await fetchGuildInvitations();
        } catch (error) {
            showToast(error.message || "Không thể từ chối lời mời.", "error");
        }
    }

    function renderFriendRequests() {
        if (els.incomingRequests) {
            els.incomingRequests.innerHTML = "";
            if (state.incomingRequests.length === 0) {
                const empty = document.createElement("li");
                empty.className = "modal-empty";
                empty.textContent = "Không có lời mời đang chờ.";
                els.incomingRequests.append(empty);
            } else {
                state.incomingRequests.forEach((req) => {
                    const li = document.createElement("li");
                    li.className = "modal-item";
                    const name = document.createElement("span");
                    const username = req.username ?? req.Username;
                    name.textContent = username;
                    name.style.cursor = "pointer";
                    // Click vào tên để xem thông tin hoặc mở DM nếu đã là bạn
                    name.addEventListener("click", () => {
                        const isFriend = state.friends.some(f => f.username === username);
                        if (isFriend) {
                            hideModal(els.createDmModal);
                            openDirectConversation(username);
                        }
                    });
                    const actions = document.createElement("div");
                    actions.className = "modal-actions";
                    const acceptBtn = document.createElement("button");
                    acceptBtn.type = "button";
                    acceptBtn.textContent = "Chấp nhận";
                    acceptBtn.classList.add("primary-btn", "small");
                    acceptBtn.addEventListener("click", async () => {
                        await respondFriendRequest(req.requestId ?? req.RequestId, true);
                        // Sau khi chấp nhận, có thể mở DM ngay
                        const acceptedUsername = req.username ?? req.Username;
                        setTimeout(() => {
                            const isNowFriend = state.friends.some(f => f.username === acceptedUsername);
                            if (isNowFriend) {
                                showToast(`Đã kết bạn với ${acceptedUsername}! Click vào tên để nhắn tin.`, "success");
                            }
                        }, 500);
                    });
                    const rejectBtn = document.createElement("button");
                    rejectBtn.type = "button";
                    rejectBtn.textContent = "Từ chối";
                    rejectBtn.classList.add("danger");
                    rejectBtn.addEventListener("click", () => respondFriendRequest(req.requestId ?? req.RequestId, false));
                    actions.append(acceptBtn, rejectBtn);
                    li.append(name, actions);
                    els.incomingRequests.append(li);
                });
            }
        }

        if (els.outgoingRequests) {
            els.outgoingRequests.innerHTML = "";
            if (state.outgoingRequests.length === 0) {
                const empty = document.createElement("li");
                empty.className = "modal-empty";
                empty.textContent = "Không có lời mời đã gửi.";
                els.outgoingRequests.append(empty);
            } else {
                state.outgoingRequests.forEach((req) => {
                    const li = document.createElement("li");
                    li.className = "modal-item";
                    const name = document.createElement("span");
                    name.textContent = req.username ?? req.Username;
                    const note = document.createElement("span");
                    note.className = "modal-note";
                    note.textContent = "Đang chờ...";
                    li.append(name, note);
                    els.outgoingRequests.append(li);
                });
            }
        }
    }

    function updateChannelHeader() {
        if (state.activeView === "channel") {
            const server = state.servers.find((s) => s.id === state.activeServerId);
            if (!server) {
                els.activeChannelName.textContent = "Không có kênh";
                els.activeChannelTopic.textContent = "";
                return;
            }
            const channel = server.channels.find((c) => c.id === state.activeChannelId);
            if (channel) {
                els.activeChannelName.textContent = channel.name;
                els.activeChannelTopic.textContent = channel.topic || "Thảo luận vui vẻ!";
            }
        } else if (state.activeView === "dm") {
            els.activeChannelName.textContent = state.activeDmTarget;
            els.activeChannelTopic.textContent = "Cuộc trò chuyện riêng tư giữa hai bạn.";
        }
    }

    function updateComposerPlaceholder() {
        if (state.activeView === "dm" && state.activeDmTarget) {
            els.messageInput.placeholder = `Gửi tin nhắn đến ${state.activeDmTarget}`;
        } else {
            els.messageInput.placeholder = `Nhập tin nhắn. Sử dụng Ctrl + Enter để gửi nhanh.`;
        }
    }

    async function setActiveServer(serverId) {
        state.activeServerId = serverId;
        state.activeView = "channel";
        const server = state.servers.find((s) => s.id === serverId);
        state.activeChannelId = server?.channels[0]?.id ?? null;
        state.activeDmTarget = null;
        renderServers();
        renderChannels();
        renderDmList();
        updateChannelHeader();
        updateComposerPlaceholder();
        if (state.activeChannelId != null) {
            await loadChannelHistory(serverId, state.activeChannelId);
            if (state.connection) {
                await state.connection.invoke("JoinChannel", state.activeChannelId).catch(() => null);
            }
        } else {
            clearMessages();
        }
    }

    async function setActiveChannel(serverId, channelId) {
        const previousChannel = state.activeChannelId;
        state.activeServerId = serverId;
        state.activeChannelId = channelId;
        state.activeView = "channel";
        state.activeDmTarget = null;

        // Ẩn typing indicator khi chuyển channel
        state.typingInChannel = null;
        state.typingInDm = null;
        els.typingIndicator.classList.add("hidden");

        renderServers();
        renderChannels();
        renderDmList();
        updateChannelHeader();
        updateComposerPlaceholder();

        if (state.connection && previousChannel && previousChannel !== channelId) {
            state.connection.invoke("LeaveChannel", previousChannel).catch(() => null);
        }

        const key = `${serverId}#${channelId}`;
        const cached = state.channelMessages.get(key);
        if (!cached || cached.length === 0) {
            await loadChannelHistory(serverId, channelId);
        } else {
            renderMessages(cached);
        }

        if (state.connection) {
            state.connection.invoke("JoinChannel", channelId).catch(() => null);
        }
        markChannelAsRead(channelId);
    }

    function markChannelAsRead(channelId) {
        state.unreadChannels.delete(channelId);
        state.lastReadChannel.set(channelId, Date.now());
        renderChannels();
    }

    function markDmAsRead(username) {
        state.unreadDms.delete(username);
        state.lastReadDm.set(username, Date.now());
        renderDmList();
    }

    async function deleteMessage(messageId) {
        try {
            await fetchJson(`${API_BASE}/api/Message/${messageId}`, { method: "DELETE" });
            const messageRow = els.messageList.querySelector(`[data-message-id="${messageId}"]`);
            if (messageRow) {
                messageRow.remove();
            }
            if (state.activeView === "channel" && state.activeChannelId) {
                const key = `${state.activeServerId}#${state.activeChannelId}`;
                const messages = state.channelMessages.get(key) || [];
                const index = messages.findIndex(m => m.id === messageId);
                if (index >= 0) {
                    messages.splice(index, 1);
                    state.channelMessages.set(key, messages);
                }
            } else if (state.activeView === "dm" && state.activeDmTarget) {
                const messages = state.dmMessages.get(state.activeDmTarget) || [];
                const index = messages.findIndex(m => m.id === messageId);
                if (index >= 0) {
                    messages.splice(index, 1);
                    state.dmMessages.set(state.activeDmTarget, messages);
                }
            }
            showToast("Đã xóa tin nhắn", "success");
        } catch (error) {
            showToast(error.message || "Không thể xóa tin nhắn", "error");
        }
    }

    async function openDirectConversation(username) {
        if (!username) return;
        const previousChannel = state.activeChannelId;
        state.activeView = "dm";
        state.activeDmTarget = username;
        state.activeChannelId = null;

        // Ẩn typing indicator khi chuyển DM
        state.typingInChannel = null;
        state.typingInDm = null;
        els.typingIndicator.classList.add("hidden");

        renderServers();
        renderChannels();
        renderDmList();
        updateChannelHeader();
        updateComposerPlaceholder();

        ensureDmThread(username);
        const cached = state.dmMessages.get(username);
        if (!cached || cached.length === 0) {
            await loadDmHistory(username);
        } else {
            renderMessages(cached);
        }

        if (state.connection && previousChannel) {
            state.connection.invoke("LeaveChannel", previousChannel).catch(() => null);
        }

        try {
            await state.connection?.invoke("OpenDirectChannel", state.username, username);
        } catch (error) {
            console.warn("OpenDirectChannel failed", error);
        }
        markDmAsRead(username);
    }

    async function sendChannelMessage(text) {
        if (state.connection && state.activeChannelId) {
            await state.connection.invoke("SendChannelMessage", state.activeChannelId, text);
        }
    }

    async function sendDirectMessage(text) {
        if (state.connection && state.activeDmTarget) {
            await state.connection.invoke("SendDirectMessage", state.username, state.activeDmTarget, text);
        }
    }

    async function startConnection() {
        if (state.connection) {
            await state.connection.stop().catch(() => null);
        }

        state.connection = new signalR.HubConnectionBuilder()
            .withUrl("/chatHub", { accessTokenFactory: () => state.token ?? "" })
            .withAutomaticReconnect()
            .build();

        state.connection.on("ReceiveChannelMessage", (channelId, sender, content, timestamp, messageId) => {
            const guild = findGuildByChannel(channelId);
            if (!guild) return;
            const payload = { sender, content, timestamp, type: "text", channelId, id: messageId || Date.now() };
            const list = ensureChannelMessages(guild.id, channelId);
            list.push(payload);
            const isActive = state.activeView === "channel" && state.activeChannelId === channelId;
            if (isActive) {
                appendMessage(payload);
                markChannelAsRead(channelId);
            } else {
                const channel = guild.channels.find(c => c.id === channelId);
                if (channel && sender !== state.username) {
                    const currentCount = state.unreadChannels.get(channelId) || 0;
                    state.unreadChannels.set(channelId, currentCount + 1);
                    renderChannels();
                    showNotification(`${sender} trong #${channel.name}`, content);
                    showToast(`Tin nhắn mới từ ${sender} trong #${channel.name}`, "info", 3000);
                }
            }
        });

        state.connection.on("ReceiveChannelSticker", (channelId, sender, stickerUrl, timestamp) => {
            const guild = findGuildByChannel(channelId);
            if (!guild) return;
            const payload = { sender, mediaUrl: stickerUrl, timestamp, type: "sticker", channelId };
            const list = ensureChannelMessages(guild.id, channelId);
            list.push(payload);
            if (state.activeView === "channel" && state.activeChannelId === channelId) {
                appendMessage(payload);
            }
        });

        state.connection.on("ReceiveDirectMessage", (sender, recipient, content, timestamp, messageId) => {
            const peer = sender === state.username ? recipient : sender;
            const dedupKey = makeDmDedupKey(sender, recipient, content, null, timestamp);
            if (shouldSkipDuplicateDm(peer, dedupKey)) {
                return;
            }
            const payload = { sender, recipient, content, timestamp, type: "dm", id: messageId || Date.now() };
            const list = ensureDmThread(peer);
            list.push(payload);
            const isActive = state.activeView === "dm" && state.activeDmTarget === peer;
            if (isActive) {
                appendMessage(payload);
                markDmAsRead(peer);
            } else if (sender !== state.username) {
                const currentCount = state.unreadDms.get(peer) || 0;
                state.unreadDms.set(peer, currentCount + 1);
                renderDmList();
                showNotification(`${sender}`, content);
                showToast(`Tin nhắn mới từ ${sender}`, "info", 3000);
            }
            renderDmList();
            persistDmThreads();
        });

        state.connection.on("ReceiveDirectAttachment", (sender, recipient, mediaUrl, fileName, timestamp) => {
            const peer = sender === state.username ? recipient : sender;
            const dedupKey = makeDmDedupKey(sender, recipient, fileName, mediaUrl, timestamp);
            if (shouldSkipDuplicateDm(peer, dedupKey)) {
                return;
            }
            const payload = { sender, recipient, mediaUrl, content: fileName, timestamp, type: "attachment" };
            const list = ensureDmThread(peer);
            list.push(payload);
            if (state.activeView === "dm" && state.activeDmTarget === peer) {
                appendMessage(payload);
            }
            renderDmList();
            persistDmThreads();
        });

        state.connection.on("GuildChannelUpdated", async (guildId, channel) => {
            const server = state.servers.find((s) => s.id === guildId);
            if (!server) return;

            const channelId = channel.Id ?? channel.id;
            const existingChannel = server.channels.find(c => c.id === channelId);
            if (existingChannel) {
                existingChannel.name = channel.Name ?? channel.name;
                existingChannel.topic = channel.Topic ?? channel.topic;
                server.channels.sort((a, b) => a.name.localeCompare(b.name));
                renderChannels();
                if (state.activeChannelId === channelId) {
                    updateChannelHeader();
                }
                showToast(`Kênh #${existingChannel.name} đã được cập nhật`, "info");
            }
        });

        state.connection.on("GuildChannelDeleted", async (guildId, channelId) => {
            const server = state.servers.find((s) => s.id === guildId);
            if (!server) return;

            const index = server.channels.findIndex(c => c.id === channelId);
            if (index >= 0) {
                server.channels.splice(index, 1);
                renderChannels();
                // Nếu đang ở kênh bị xóa, chuyển sang kênh đầu tiên
                if (state.activeChannelId === channelId) {
                    if (server.channels.length > 0) {
                        setActiveChannel(guildId, server.channels[0].id);
                    } else {
                        state.activeChannelId = null;
                        clearMessages();
                    }
                }
                showToast("Kênh đã bị xóa", "info");
            }
        });

        state.connection.on("GuildChannelCreated", async (guildId, channel) => {
            // Kiểm tra xem user có phải là member của guild này không
            const server = state.servers.find((s) => s.id === guildId);
            if (!server) {
                // Nếu không có trong danh sách, bỏ qua event này (user không phải member)
                console.log(`Ignoring GuildChannelCreated for guild ${guildId} - not a member`);
                return;
            }

            // Kiểm tra lại: chỉ xử lý nếu guild có trong state.servers (tức là user là member)
            if (!state.joinedGuildIds || !state.joinedGuildIds.has(guildId)) {
                console.log(`Ignoring GuildChannelCreated for guild ${guildId} - not in joined list`);
                return;
            }

            // Refresh guilds để lấy danh sách channels mới nhất (chỉ channels mà user có quyền truy cập)
            // Backend sẽ chỉ trả về channels mà user có membership hoặc là owner
            try {
                await fetchGuilds();
                // fetchGuilds sẽ tự động render channels, không cần làm gì thêm
            } catch (error) {
                console.error("Error refreshing guilds after channel creation:", error);
            }
        });

        state.connection.on("GuildMemberJoined", (guildId, username) => {
            const server = state.servers.find((s) => s.id === guildId);
            if (server && state.activeServerId === guildId) {
                showToast(`${username} đã tham gia máy chủ`, "success");
            }
        });

        state.connection.on("GuildInvitationReceived", async (invitedUsername, guildId, guildName, inviterUsername, invitationId) => {
            // Chỉ xử lý nếu đây là thông báo cho chính mình
            if (invitedUsername && state.username && 
                invitedUsername.toLowerCase() === state.username.toLowerCase()) {
                showToast(`${inviterUsername} đã mời bạn vào máy chủ "${guildName}"`, "info");
                showNotification("Lời mời vào máy chủ", `${inviterUsername} đã mời bạn vào "${guildName}"`);
                await fetchGuildInvitations();
                
                // Hiển thị prompt để chấp nhận/từ chối
                setTimeout(async () => {
                    if (confirm(`${inviterUsername} đã mời bạn vào máy chủ "${guildName}"\n\nBạn có muốn tham gia không?`)) {
                        await acceptGuildInvitation(invitationId);
                    } else {
                        await rejectGuildInvitation(invitationId);
                    }
                }, 500);
            }
        });

        state.connection.on("GuildInvitationAccepted", async (username, guildId, guildName) => {
            if (username && state.username && username.toLowerCase() === state.username.toLowerCase()) {
                showToast(`Đã tham gia máy chủ "${guildName}"`, "success");
                await fetchGuilds();
            }
        });

        state.connection.on("MessageDeleted", (messageId, channelId) => {
            const messageRow = els.messageList.querySelector(`[data-message-id="${messageId}"]`);
            if (messageRow) {
                messageRow.remove();
            }
            if (channelId) {
                const guild = findGuildByChannel(channelId);
                if (guild) {
                    const key = `${guild.id}#${channelId}`;
                    const messages = state.channelMessages.get(key) || [];
                    const index = messages.findIndex(m => m.id === messageId);
                    if (index >= 0) {
                        messages.splice(index, 1);
                        state.channelMessages.set(key, messages);
                    }
                }
            } else if (state.activeDmTarget) {
                const messages = state.dmMessages.get(state.activeDmTarget) || [];
                const index = messages.findIndex(m => m.id === messageId);
                if (index >= 0) {
                    messages.splice(index, 1);
                    state.dmMessages.set(state.activeDmTarget, messages);
                }
            }
        });

        state.connection.on("DirectHistory", (peer, messages) => {
            ensureDmThread(peer);
            const list = Array.isArray(messages) ? messages : [];
            state.dmMessages.set(peer, list);
            resetDmDedup(peer);
            if (state.activeView === "dm" && state.activeDmTarget === peer) {
                renderMessages(list);
            }
        });

        state.connection.on("Error", (message) => {
            showToast(message || "Đã xảy ra lỗi.", "error");
        });

        state.connection.on("UserList", (users) => {
            state.onlineUsers = users || [];
            updateOnlineUsers();
        });

        state.connection.on("UserConnected", (username) => {
            addEventLog(`${username} đã online.`);
            showToast(`${username} vừa tham gia phòng chat.`);
        });

        state.connection.on("UserDisconnected", (username) => {
            addEventLog(`${username} đã offline.`);
            showToast(`${username} vừa rời đi.`, "warning");
        });

        state.connection.on("UserTyping", (username, context) => {
            if (username === state.username) return;
            
            // context có thể là { type: "channel", channelId } hoặc { type: "dm", recipient }
            if (context && context.type === "channel") {
                // Chỉ hiển thị nếu đang ở đúng channel
                if (state.activeView === "channel" && state.activeChannelId === context.channelId) {
                    state.typingInChannel = { channelId: context.channelId, username };
                    els.typingIndicator.textContent = `${username} đang nhập...`;
                    els.typingIndicator.classList.remove("hidden");
                }
            } else if (context && context.type === "dm") {
                // Chỉ hiển thị nếu đang ở đúng DM với người đang typing
                // context.recipient là người nhận typing (người mà username đang gửi typing cho)
                // Nếu context.recipient là chính mình (state.username), nghĩa là username đang gửi typing cho mình
                // Ta cần kiểm tra xem có đang ở DM với username không
                const isRecipientMe = context.recipient && 
                    context.recipient.toLowerCase() === state.username?.toLowerCase();
                if (state.activeView === "dm" && state.activeDmTarget && 
                    state.activeDmTarget.toLowerCase() === username.toLowerCase() && isRecipientMe) {
                    state.typingInDm = { username };
                    els.typingIndicator.textContent = `${username} đang nhập...`;
                    els.typingIndicator.classList.remove("hidden");
                }
            } else {
                // Fallback cho backward compatibility - không có context, kiểm tra xem có đang ở DM với username không
                if (state.activeView === "dm" && state.activeDmTarget && 
                    state.activeDmTarget.toLowerCase() === username.toLowerCase()) {
                    state.typingInDm = { username };
                    els.typingIndicator.textContent = `${username} đang nhập...`;
                    els.typingIndicator.classList.remove("hidden");
                }
            }
        });

        state.connection.on("UserStopTyping", (context) => {
            if (context && context.type === "channel") {
                if (state.typingInChannel && state.typingInChannel.channelId === context.channelId) {
                    state.typingInChannel = null;
                    els.typingIndicator.classList.add("hidden");
                }
            } else if (context && context.type === "dm") {
                // context.recipient là người nhận typing (người mà username đang gửi typing cho)
                // Nếu context.recipient là chính mình, nghĩa là username đang dừng typing cho mình
                const isRecipientMe = context.recipient && 
                    context.recipient.toLowerCase() === state.username?.toLowerCase();
                if (state.typingInDm && isRecipientMe && state.activeDmTarget) {
                    state.typingInDm = null;
                    els.typingIndicator.classList.add("hidden");
                }
            } else {
                // Fallback - ẩn typing indicator nếu không có context hoặc context không khớp
                if (state.typingInDm || state.typingInChannel) {
                    state.typingInChannel = null;
                    state.typingInDm = null;
                    els.typingIndicator.classList.add("hidden");
                }
            }
        });

        state.connection.onreconnected(async () => {
            addEventLog("Đã kết nối lại SignalR.");
            state.connection.invoke("RegisterUser", state.username);
            await joinGuildGroups().catch(() => null);
            if (state.activeView === "dm" && state.activeDmTarget) {
                state.connection.invoke("OpenDirectChannel", state.username, state.activeDmTarget);
            }
            if (state.activeView === "channel" && state.activeChannelId != null) {
                state.connection.invoke("JoinChannel", state.activeChannelId);
            }
        });

        state.connection.onclose(() => {
            addEventLog("Mất kết nối realtime, đang chờ thử lại...");
        });

        try {
            await state.connection.start();
            await state.connection.invoke("RegisterUser", state.username);
            if (state.activeView === "channel" && state.activeChannelId != null) {
                await state.connection.invoke("JoinChannel", state.activeChannelId);
            }
            addEventLog("Kết nối realtime thành công.");
        } catch (error) {
            console.error("SignalR connection failed", error);
            showToast("Không thể kết nối realtime, thử tải lại trang.", "error");
        }
    }

    async function handleAuth(action, username, password) {
        const endpoint = action === "login" ? "login" : "register";
        const payload = { username, passwordHash: password };
        const data = await fetchJson(`${API_BASE}/api/Auth/${endpoint}`, {
            method: "POST",
            body: JSON.stringify(payload),
        });
        if (action === "login") {
            if (!data?.token) throw new Error("Token không hợp lệ.");
            state.token = data.token;
            state.username = username;
            saveSession({ token: state.token, username: state.username });
            
            // Xóa thông tin nhạy cảm khỏi URL để bảo mật
            if (window.history && window.history.replaceState) {
                const url = new URL(window.location.href);
                url.searchParams.delete("username");
                url.searchParams.delete("password");
                window.history.replaceState({}, document.title, url.pathname + url.search);
            }
            
            showToast("Đăng nhập thành công!", "success");
        } else {
            showToast("Tạo tài khoản thành công, hãy đăng nhập.", "success");
        }
    }

    function bindAuthForms() {
        els.tabLogin.addEventListener("click", () => {
            els.tabLogin.classList.add("active");
            els.tabRegister.classList.remove("active");
            els.loginForm.classList.remove("hidden");
            els.registerForm.classList.add("hidden");
        });

        els.tabRegister.addEventListener("click", () => {
            els.tabRegister.classList.add("active");
            els.tabLogin.classList.remove("active");
            els.registerForm.classList.remove("hidden");
            els.loginForm.classList.add("hidden");
        });

        els.loginForm.addEventListener("submit", async (event) => {
            event.preventDefault();
            const username = els.loginUsername.value.trim();
            const password = els.loginPassword.value;
            if (!username || !password) return;
            try {
                await handleAuth("login", username, password);
                await enterApp();
            } catch (error) {
                console.error("login failed", error);
                showToast(error.message || "Đăng nhập thất bại.", "error");
            }
        });

        els.registerForm.addEventListener("submit", async (event) => {
            event.preventDefault();
            const username = els.registerUsername.value.trim();
            const password = els.registerPassword.value;
            if (!username || password.length < 6) {
                showToast("Mật khẩu cần tối thiểu 6 ký tự.", "warning");
                return;
            }
            try {
                await handleAuth("register", username, password);
                els.tabLogin.click();
                els.loginUsername.value = username;
                els.loginPassword.value = "";
            } catch (error) {
                console.error("register failed", error);
                showToast(error.message || "Đăng ký thất bại.", "error");
            }
        });
    }

    function bindComposer() {
        const handleSend = async () => {
            const text = els.messageInput.value.trim();
            if (!text) return;
            try {
                if (state.activeView === "dm") {
                    await sendDirectMessage(text);
                } else if (state.activeChannelId != null) {
                    await sendChannelMessage(text);
                } else {
                    await sendChannelMessage(text);
                }
                els.messageInput.value = "";
                els.messageInput.focus();
                state.connection?.invoke("StopTyping", state.username).catch(() => null);
                state.isTyping = false;
            } catch (error) {
                console.error("send message error", error);
                showToast("Không gửi được tin nhắn.", "error");
            }
        };

        els.composer.addEventListener("submit", (event) => {
            event.preventDefault();
            handleSend();
        });

        els.messageInput.addEventListener("keydown", async (event) => {
            const shortcut = event.key === "Enter" && (event.ctrlKey || event.metaKey);
            if (shortcut) {
                event.preventDefault();
                await handleSend();
                return;
            }
            if (!state.connection) return;
            
            // Xác định context: channel hay DM
            if (state.activeView === "channel" && state.activeChannelId != null) {
                if (!state.isTyping) {
                    state.isTyping = true;
                    state.connection.invoke("Typing", state.username, "channel", state.activeChannelId, null).catch(() => null);
                }
                clearTimeout(state.typingTimeout);
                state.typingTimeout = setTimeout(() => {
                    state.isTyping = false;
                    state.connection?.invoke("StopTyping", state.username, "channel", state.activeChannelId, null).catch(() => null);
                }, 1400);
            } else if (state.activeView === "dm" && state.activeDmTarget) {
                if (!state.isTyping) {
                    state.isTyping = true;
                    state.connection.invoke("Typing", state.username, "dm", null, state.activeDmTarget).catch(() => null);
                }
                clearTimeout(state.typingTimeout);
                state.typingTimeout = setTimeout(() => {
                    state.isTyping = false;
                    state.connection?.invoke("StopTyping", state.username, "dm", null, state.activeDmTarget).catch(() => null);
                }, 1400);
            } else {
                if (!state.isTyping) {
                    state.isTyping = true;
                    state.connection.invoke("Typing", state.username).catch(() => null);
                }
                clearTimeout(state.typingTimeout);
                state.typingTimeout = setTimeout(() => {
                    state.isTyping = false;
                    state.connection?.invoke("StopTyping", state.username).catch(() => null);
                }, 1400);
            }
        });

        els.messageInput.addEventListener("blur", () => {
            if (state.isTyping) {
                state.isTyping = false;
                if (state.activeView === "channel" && state.activeChannelId != null) {
                    state.connection?.invoke("StopTyping", state.username, "channel", state.activeChannelId, null).catch(() => null);
                } else if (state.activeView === "dm" && state.activeDmTarget) {
                    state.connection?.invoke("StopTyping", state.username, "dm", null, state.activeDmTarget).catch(() => null);
                } else {
                    state.connection?.invoke("StopTyping", state.username).catch(() => null);
                }
            }
        });

        els.fileInput.addEventListener("change", async (event) => {
            const [file] = event.target.files || [];
            if (!file) return;
            if (!state.token) {
                showToast("Cần đăng nhập để tải tệp.", "error");
                return;
            }

            const formData = new FormData();
            formData.append("file", file, file.name);

            try {
                const response = await fetch(`${API_BASE}/api/File/upload`, {
                    method: "POST",
                    headers: state.token ? { Authorization: `Bearer ${state.token}` } : {},
                    body: formData,
                });

                if (!response.ok) {
                    const message = await response.text();
                    throw new Error(message || "Upload thất bại.");
                }

                const result = await response.json();
                const fileUrl = result.file_url || result.url;

                if (fileUrl) {
                    if (state.activeView === "dm" && state.activeDmTarget) {
                        await state.connection?.invoke("SendDirectAttachment", state.username, state.activeDmTarget, fileUrl, file.name);
                    } else if (state.activeChannelId != null) {
                        await state.connection?.invoke("SendChannelSticker", state.activeChannelId, fileUrl);
                    } else {
                        await state.connection?.invoke("SendSticker", state.username, fileUrl);
                    }
                    addUploadedFile({ fileUrl, fileName: file.name });
                    showToast("Tải tệp thành công!", "success");
                }
            } catch (error) {
                console.error("Upload error", error);
                showToast(error.message || "Không thể tải tệp.", "error");
            } finally {
                els.fileInput.value = "";
            }
        });
    }

    function bindNavigation() {
        els.logoutBtn.addEventListener("click", async () => {
            saveSession(null);
            localStorage.removeItem(DM_THREADS_KEY);
            try {
                await state.connection?.stop();
            } catch {
                /* ignore */
            }
            state.connection = null;
            state.token = null;
            state.username = null;
            toggleShell(true);
            showToast("Đã đăng xuất.", "info");
        });

        const openFriendModal = async () => {
            // Load directory nếu chưa có
            if (state.directory.length === 0) {
                await loadDirectory();
            }
            renderFriends();
            renderFriendRequests();
            buildDmCandidates();
            renderSearchUsers();
            showModal(els.createDmModal);
        };

        els.openFriendCenter?.addEventListener("click", openFriendModal);
        els.openDmBtn?.addEventListener("click", openFriendModal);

        els.addFriendForm?.addEventListener("submit", async (event) => {
            event.preventDefault();
            const username = els.addFriendUsername.value.trim();
            if (!username) {
                showToast("Nhập username để gửi lời mời.", "warning");
                return;
            }
            await sendFriendRequest(username);
            els.addFriendUsername.value = "";
        });

        els.createChannelBtn?.addEventListener("click", () => {
            if (!state.activeServerId) {
                showToast("Bạn cần chọn máy chủ trước.", "warning");
                return;
            }
            showModal(els.createChannelModal);
        });

        els.createServerForm?.addEventListener("submit", async (event) => {
            event.preventDefault();
            const name = (document.getElementById("new-server-name")?.value || "").trim();
            const desc = (document.getElementById("new-server-desc")?.value || "").trim();
            if (!name) {
                showToast("Tên máy chủ không được trống.", "warning");
                return;
            }
            try {
                await fetchJson(`${API_BASE}/api/Guilds`, {
                    method: "POST",
                    body: JSON.stringify({ name, description: desc }),
                });
                showToast("Tạo máy chủ thành công!", "success");
                hideModal(els.createServerModal);
                await fetchGuilds();
            } catch (error) {
                showToast(error.message || "Không thể tạo máy chủ.", "error");
            }
        });

        els.createChannelForm?.addEventListener("submit", async (event) => {
            event.preventDefault();
            const name = (document.getElementById("new-channel-name")?.value || "").trim();
            const topic = (document.getElementById("new-channel-topic")?.value || "").trim();
            if (!name) {
                showToast("Tên kênh không được trống.", "warning");
                return;
            }
            try {
                const channel = await fetchJson(`${API_BASE}/api/Guilds/${state.activeServerId}/channels`, {
                    method: "POST",
                    body: JSON.stringify({ name, topic }),
                });
                showToast("Tạo kênh thành công!", "success");
                hideModal(els.createChannelModal);
                await fetchGuilds();
                setActiveServer(state.activeServerId);
                setActiveChannel(state.activeServerId, channel.id ?? channel.Id);
            } catch (error) {
                showToast(error.message || "Không thể tạo kênh.", "error");
            }
        });

        els.toggleMemberListBtn?.addEventListener("click", () => {
            els.memberPane.classList.toggle("hidden");
        });

        els.dmSearchInput?.addEventListener("input", () => {
            const keyword = els.dmSearchInput.value.toLowerCase();
            Array.from(els.dmList.children).forEach((item) => {
                item.style.display = item.textContent.toLowerCase().includes(keyword) ? "" : "none";
            });
        });

        els.dmFilter?.addEventListener("input", buildDmCandidates);
        els.searchUserInput?.addEventListener("input", renderSearchUsers);
        els.userSearch?.addEventListener("input", renderDirectory);

        els.inviteFriendBtn?.addEventListener("click", () => {
            if (!state.activeServerId) {
                showToast("Bạn cần chọn máy chủ trước.", "warning");
                return;
            }
            if (!state.activeChannelId) {
                showToast("Bạn cần chọn kênh trước.", "warning");
                return;
            }
            renderInviteFriendList();
            showModal(els.inviteFriendModal);
        });
    }

    async function renderInviteFriendList() {
        if (!els.inviteFriendList) return;
        els.inviteFriendList.innerHTML = "";

        if (state.friends.length === 0) {
            const empty = document.createElement("li");
            empty.className = "modal-empty";
            empty.textContent = "Chưa có bạn bè. Hãy kết bạn trước!";
            els.inviteFriendList.append(empty);
            return;
        }

        if (!state.activeServerId) return;

        try {
            const members = await fetchJson(`${API_BASE}/api/Guilds/${state.activeServerId}/members`);
            const memberUsernames = new Set((Array.isArray(members) ? members : []).map(m => {
                const username = m.Username ?? m.username ?? "";
                return username.toLowerCase();
            }));
            state.guildMembers.set(state.activeServerId, memberUsernames);
        } catch (error) {
            console.error("Failed to load guild members", error);
        }

        // Lấy danh sách members đã có trong channel
        let channelMemberUsernames = new Set();
        if (state.activeChannelId) {
            try {
                const channelMembers = await fetchJson(`${API_BASE}/api/Guilds/${state.activeServerId}/channels/${state.activeChannelId}/members`).catch(() => []);
                channelMemberUsernames = new Set((Array.isArray(channelMembers) ? channelMembers : []).map(m => {
                    const username = m.Username ?? m.username ?? "";
                    return username.toLowerCase();
                }));
            } catch (error) {
                console.error("Failed to load channel members", error);
            }
        }

        const memberUsernames = state.guildMembers.get(state.activeServerId) || new Set();
        const availableFriends = state.friends.filter(f => {
            const usernameLower = (f.username || "").toLowerCase();
            // Phải là member của guild và chưa có trong channel
            return memberUsernames.has(usernameLower) && 
                   !channelMemberUsernames.has(usernameLower) && 
                   usernameLower !== (state.username || "").toLowerCase();
        });

        if (availableFriends.length === 0) {
            const empty = document.createElement("li");
            empty.className = "modal-empty";
            empty.textContent = state.activeChannelId 
                ? "Tất cả bạn bè đã có trong kênh này hoặc chưa là thành viên máy chủ."
                : "Tất cả bạn bè đã là thành viên hoặc không có bạn bè nào.";
            els.inviteFriendList.append(empty);
            return;
        }

        availableFriends.forEach((friend) => {
            const li = document.createElement("li");
            li.className = "modal-item";
            const name = document.createElement("span");
            name.textContent = friend.username;
            const inviteBtn = document.createElement("button");
            inviteBtn.type = "button";
            inviteBtn.textContent = "Mời";
            inviteBtn.classList.add("primary-btn", "small");
            const isInviting = state.invitingUsers.has(friend.username);
            if (isInviting) {
                inviteBtn.disabled = true;
                inviteBtn.textContent = "Đang mời...";
                inviteBtn.style.opacity = "0.6";
            }
            inviteBtn.addEventListener("click", () => {
                if (!isInviting) {
                    inviteFriendToGuild(friend.username);
                }
            });
            li.append(name, inviteBtn);
            els.inviteFriendList.append(li);
        });
    }

    async function inviteFriendToGuild(username) {
        if (!state.activeServerId) {
            showToast("Bạn cần chọn máy chủ trước.", "warning");
            return;
        }

        if (state.invitingUsers.has(username)) {
            return;
        }

        state.invitingUsers.add(username);
        renderInviteFriendList();

        try {
            // Nếu có activeChannelId, mời vào channel, nếu không thì mời vào server
            if (state.activeChannelId) {
                const result = await fetchJson(`${API_BASE}/api/Guilds/${state.activeServerId}/channels/${state.activeChannelId}/invite`, {
                    method: "POST",
                    body: JSON.stringify({ username: username.trim() }),
                });
                showToast(result.message || `Đã mời ${username} vào kênh!`, "success");
                await fetchGuilds();
            } else {
                const result = await fetchJson(`${API_BASE}/api/Guilds/${state.activeServerId}/invite`, {
                    method: "POST",
                    body: JSON.stringify({ username: username.trim() }),
                });
                showToast(result.message || `Đã mời ${username} vào máy chủ!`, "success");
                await fetchGuilds();
                const members = await fetchJson(`${API_BASE}/api/Guilds/${state.activeServerId}/members`).catch(() => []);
                const memberUsernames = new Set((Array.isArray(members) ? members : []).map(m => {
                    const username = m.Username ?? m.username ?? "";
                    return username.toLowerCase();
                }));
                state.guildMembers.set(state.activeServerId, memberUsernames);
            }
        } catch (error) {
            let errorMsg = error.message || "Không thể mời bạn bè.";
            if (error.status === 409) {
                errorMsg = state.activeChannelId 
                    ? "Người này đã có trong kênh này."
                    : "Người này đã là thành viên của máy chủ.";
                const memberUsernames = state.guildMembers.get(state.activeServerId) || new Set();
                memberUsernames.add((username || "").toLowerCase());
                state.guildMembers.set(state.activeServerId, memberUsernames);
            } else if (error.status === 404) {
                errorMsg = "Không tìm thấy người dùng.";
            } else if (error.status === 400) {
                errorMsg = error.message || "Yêu cầu không hợp lệ.";
            }
            showToast(errorMsg, error.status === 409 ? "info" : "warning");
        } finally {
            state.invitingUsers.delete(username);
            renderInviteFriendList();
        }
    }

    async function renderGuildMembers() {
        if (!els.guildMembers || !state.activeServerId) return;
        els.guildMembers.innerHTML = "";

        try {
            const members = await fetchJson(`${API_BASE}/api/Guilds/${state.activeServerId}/members`);
            if (!Array.isArray(members)) return;

            if (els.guildMemberCount) {
                els.guildMemberCount.textContent = members.length;
            }

            const server = state.servers.find(s => s.id === state.activeServerId);
            // Kiểm tra owner bằng cách so sánh role hoặc lấy từ API
            const isOwner = server && server.role === "owner";
            const isAdmin = server && (server.role === "owner" || server.role === "admin");

            members.forEach((member) => {
                const li = document.createElement("li");
                li.className = "member-item";
                li.setAttribute("data-member-id", member.Id ?? member.id);
                
                const avatar = document.createElement("span");
                avatar.className = "avatar small";
                avatar.textContent = initials(member.Username ?? member.username);
                
                const info = document.createElement("div");
                info.className = "member-info";
                const name = document.createElement("span");
                name.className = "member-name";
                name.textContent = member.Username ?? member.username;
                const role = document.createElement("span");
                role.className = "member-role";
                const roleText = member.Role ?? member.role ?? "member";
                role.textContent = roleText === "owner" ? "👑 Chủ sở hữu" : roleText === "admin" ? "⭐ Admin" : "Thành viên";
                info.append(name, role);
                
                li.append(avatar, info);

                // Thêm menu quản lý cho owner/admin
                if (isAdmin && (member.Username ?? member.username) !== state.username) {
                    const menuBtn = document.createElement("button");
                    menuBtn.className = "member-menu-btn";
                    menuBtn.textContent = "⋯";
                    menuBtn.title = "Tùy chọn";
                    menuBtn.addEventListener("click", (e) => {
                        e.stopPropagation();
                        showMemberMenu(e, member, state.activeServerId);
                    });
                    li.append(menuBtn);
                }

                els.guildMembers.append(li);
            });
        } catch (error) {
            console.error("Failed to load guild members", error);
        }
    }

    function showMemberMenu(event, member, guildId) {
        const menu = document.createElement("div");
        menu.className = "channel-menu";
        menu.style.position = "fixed";
        menu.style.left = `${event.clientX}px`;
        menu.style.top = `${event.clientY}px`;
        menu.style.zIndex = "10000";
        
        const server = state.servers.find(s => s.id === guildId);
        const isOwner = server && (server.role === "owner");
        const memberRole = member.Role ?? member.role ?? "member";
        const memberId = member.Id ?? member.id;

        if (isOwner && memberRole === "member") {
            const promoteBtn = document.createElement("button");
            promoteBtn.className = "menu-item";
            promoteBtn.textContent = "⭐ Bổ nhiệm Admin";
            promoteBtn.addEventListener("click", () => {
                updateMemberRole(guildId, memberId, "admin");
                menu.remove();
            });
            menu.append(promoteBtn);
        }

        if (isOwner && memberRole === "admin") {
            const demoteBtn = document.createElement("button");
            demoteBtn.className = "menu-item";
            demoteBtn.textContent = "⬇️ Hạ quyền";
            demoteBtn.addEventListener("click", () => {
                updateMemberRole(guildId, memberId, "member");
                menu.remove();
            });
            menu.append(demoteBtn);
        }

        if (isOwner || (server && server.role === "admin")) {
            const removeBtn = document.createElement("button");
            removeBtn.className = "menu-item danger";
            removeBtn.textContent = "🗑️ Xóa khỏi máy chủ";
            removeBtn.addEventListener("click", () => {
                if (confirm(`Bạn có chắc muốn xóa ${member.Username ?? member.username} khỏi máy chủ?`)) {
                    removeGuildMember(guildId, memberId);
                }
                menu.remove();
            });
            menu.append(removeBtn);
        }

        if (menu.children.length === 0) {
            menu.remove();
            return;
        }

        document.body.append(menu);
        
        const closeMenu = (e) => {
            if (!menu.contains(e.target) && e.target !== event.target) {
                menu.remove();
                document.removeEventListener("click", closeMenu);
            }
        };
        setTimeout(() => document.addEventListener("click", closeMenu), 100);
    }

    async function updateMemberRole(guildId, memberId, role) {
        try {
            await fetchJson(`${API_BASE}/api/Guilds/${guildId}/members/${memberId}/role`, {
                method: "PUT",
                body: JSON.stringify({ role }),
            });
            showToast(`Đã ${role === "admin" ? "bổ nhiệm" : "hạ quyền"} thành viên!`, "success");
            await renderGuildMembers();
        } catch (error) {
            showToast(error.message || "Không thể cập nhật quyền.", "error");
        }
    }

    async function removeGuildMember(guildId, memberId) {
        try {
            await fetchJson(`${API_BASE}/api/Guilds/${guildId}/members/${memberId}`, {
                method: "DELETE",
            });
            showToast("Đã xóa thành viên khỏi máy chủ!", "success");
            await renderGuildMembers();
            await fetchGuilds();
        } catch (error) {
            showToast(error.message || "Không thể xóa thành viên.", "error");
        }
    }

    async function enterApp() {
        toggleShell(false);
        // Đảm bảo username được set đúng
        if (!state.username) {
            console.error("Username is not set!");
            return;
        }
        els.sessionUsername.textContent = state.username;
        els.sessionAvatar.textContent = initials(state.username);
        console.log("Entered app as:", state.username);
        await requestNotificationPermission();
        await fetchGuilds();
        await fetchFriends();
        await fetchFriendRequests();
        await fetchGuildInvitations();
        loadSavedDmThreads();
        renderServers();
        renderChannels();
        renderDmList();
        updateChannelHeader();
        updateComposerPlaceholder();
        await loadDirectory();
        if (state.activeServerId && state.activeChannelId != null) {
            await loadChannelHistory(state.activeServerId, state.activeChannelId);
        }
        await startConnection();
        await joinGuildGroups();
        await renderGuildMembers();
    }

    async function bootstrap() {
        // Xóa thông tin nhạy cảm khỏi URL ngay khi trang load để bảo mật
        if (window.history && window.history.replaceState) {
            const url = new URL(window.location.href);
            const hadPassword = url.searchParams.has("password");
            url.searchParams.delete("username");
            url.searchParams.delete("password");
            if (hadPassword || url.searchParams.has("username")) {
                window.history.replaceState({}, document.title, url.pathname + url.search);
            }
        }

        bindAuthForms();
        bindComposer();
        bindNavigation();

        const hasSession = restoreSession();
        if (hasSession) {
            showToast("Khôi phục phiên đăng nhập...", "info", 2000);
            await enterApp();
        } else {
            toggleShell(true);
        }
    }

    document.addEventListener("DOMContentLoaded", bootstrap);

    async function joinGuildGroups() {
        if (!state.connection) return;
        
        // Lấy danh sách guild IDs hiện tại
        const currentGuildIds = new Set(state.servers.map(s => s.id));
        
        // Join groups cho tất cả guilds hiện tại
        await Promise.all(
            state.servers.map((server) =>
                state.connection.invoke("JoinGuildGroup", server.id).catch(() => null)
            )
        );
        
        // Lưu danh sách guild IDs đã join để có thể leave sau này nếu cần
        if (!state.joinedGuildIds) {
            state.joinedGuildIds = new Set();
        }
        
        // Cập nhật danh sách đã join
        currentGuildIds.forEach(id => state.joinedGuildIds.add(id));
    }
})();

