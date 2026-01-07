const connection = new signalR.HubConnectionBuilder()
    .withUrl("/midihub")
    .withAutomaticReconnect()
    .build();

const pianoContainer = document.getElementById('piano-container');
const eventLog = document.getElementById('event-log');
const activeNotesDisplay = document.getElementById('active-notes');
const statusMessage = document.getElementById('status-message');
const emojiContainer = document.getElementById('emoji-container');

const activeNotes = new Set();

// 浪漫 emoji 库
const romanticEmojis = [
    '🎵', '🎶', '♪', '♫', '⭐', '✨', '💫', '⚡',
    '💖', '💕', '💗', '💓', '💝', '💞', '💜', '💙',
    '🐱', '🐈', '😺', '😸', '😻', '🌸', '🌺', '🌷',
    '🦋', '🌙', '☁️', '🎀', '💐', '🌹'
];

// Generate piano keys (88 keys)
function createPiano() {
    const pattern = ['white', 'black', 'white', 'black', 'white', 'white', 'black', 'white', 'black', 'white', 'black', 'white'];

    for (let i = 21; i <= 108; i++) {
        const keyType = pattern[i % 12];
        const key = document.createElement('div');
        key.className = `piano-key ${keyType}`;
        key.dataset.note = i;
        pianoContainer.appendChild(key);
    }
}

createPiano();

// 创建浮动 emoji
function createFloatingEmoji(x, y) {
    const emoji = document.createElement('div');
    emoji.className = 'emoji-particle';
    emoji.textContent = romanticEmojis[Math.floor(Math.random() * romanticEmojis.length)];
    emoji.style.left = x + 'px';
    emoji.style.top = y + 'px';
    emojiContainer.appendChild(emoji);

    setTimeout(() => {
        emoji.remove();
    }, 2000);
}

// 创建烟花爆炸效果
function createFireworkExplosion(x, y) {
    const particleCount = 12;
    const radius = 150;

    for (let i = 0; i < particleCount; i++) {
        const angle = (i / particleCount) * Math.PI * 2;
        const tx = Math.cos(angle) * radius;
        const ty = Math.sin(angle) * radius;

        const particle = document.createElement('div');
        particle.className = 'emoji-firework';
        particle.textContent = romanticEmojis[Math.floor(Math.random() * romanticEmojis.length)];
        particle.style.left = x + 'px';
        particle.style.top = y + 'px';
        particle.style.setProperty('--tx', tx + 'px');
        particle.style.setProperty('--ty', ty + 'px');

        emojiContainer.appendChild(particle);

        setTimeout(() => {
            particle.remove();
        }, 1500);
    }
}

// SignalR event handlers

// 接收音符触发 - 激活钢琴键
connection.on("ReceiveNoteOn", (noteNumber, velocity, channel, noteName, timestamp) => {
    activeNotes.add(noteNumber);
    activeNotesDisplay.textContent = activeNotes.size;

    // Activate piano key with blue/pink glow
    const key = pianoContainer.querySelector(`[data-note="${noteNumber}"]`);
    if (key) {
        // 蓝粉色系发光
        const colors = [
            'rgba(125, 211, 252, 0.8)',  // 蓝色
            'rgba(199, 125, 255, 0.8)',  // 紫色
            'rgba(255, 110, 196, 0.8)',  // 粉色
            'rgba(224, 179, 255, 0.8)'   // 淡紫色
        ];
        const glowColor = colors[Math.floor(Math.random() * colors.length)];
        
        key.classList.add('active');
        key.style.boxShadow = `0 0 30px ${glowColor}, 0 0 60px ${glowColor}`;

        // 获取按键位置
        const rect = key.getBoundingClientRect();
        const x = rect.left + rect.width / 2;
        const y = rect.top + rect.height / 2;

        // 随机选择效果：浮动 emoji 或烟花爆炸
        if (Math.random() > 0.3) {
            // 70% 概率显示浮动 emoji
            createFloatingEmoji(x, y);
        } else {
            // 30% 概率显示烟花爆炸
            createFireworkExplosion(x, y);
        }
    }

    addLogEntry('note-on', timestamp, 'NOTE ON', `${noteName} CH${channel + 1} VEL:${velocity}`);
});

connection.on("ReceiveNoteOff", (noteNumber, channel, noteName, timestamp) => {
    activeNotes.delete(noteNumber);
    activeNotesDisplay.textContent = activeNotes.size;

    // Deactivate piano key
    const key = pianoContainer.querySelector(`[data-note="${noteNumber}"]`);
    if (key) {
        key.classList.remove('active');
        key.style.boxShadow = '';
    }

    addLogEntry('note-off', timestamp, 'NOTE OFF', `${noteName} CH${channel + 1}`);
});

connection.on("ReceiveControlChange", (controllerName, value, channel, timestamp) => {
    addLogEntry('control', timestamp, 'CTRL CHG', `${controllerName} CH${channel + 1} VAL:${value}`);
});

connection.on("ReceiveMessage", (type, message, color) => {
    statusMessage.textContent = message;
    addLogEntry('info', new Date().toLocaleTimeString(), type, message);
});

function addLogEntry(className, timestamp, type, message) {
    const entry = document.createElement('div');
    entry.className = `log-entry ${className}`;
    entry.innerHTML = `
        <span class="timestamp">${timestamp}</span>
        <span class="type">${type}</span>
        <span>${message}</span>
    `;

    eventLog.insertBefore(entry, eventLog.firstChild);

    // Keep only last 50 entries
    while (eventLog.children.length > 50) {
        eventLog.removeChild(eventLog.lastChild);
    }
}

// Start connection
connection.start()
    .then(() => {
        console.log('Connected to MIDI Player Hub');
        statusMessage.textContent = 'Connected - Ready to play!';
    })
    .catch(err => {
        console.error('Connection error:', err);
        statusMessage.textContent = 'Connection failed!';
    });