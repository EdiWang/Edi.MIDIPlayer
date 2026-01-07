const connection = new signalR.HubConnectionBuilder()
    .withUrl("/midihub")
    .withAutomaticReconnect()
    .build();

const canvas = document.getElementById('waterfallCanvas');
const ctx = canvas.getContext('2d');
const pianoContainer = document.getElementById('piano-container');
const eventLog = document.getElementById('event-log');
const activeNotesDisplay = document.getElementById('active-notes');
const statusMessage = document.getElementById('status-message');

// Canvas setup
function resizeCanvas() {
    canvas.width = canvas.offsetWidth;
    canvas.height = canvas.offsetHeight;
}
resizeCanvas();
window.addEventListener('resize', resizeCanvas);

// Waterfall notes (falling from top to piano)
const fallingNotes = [];
const activeNotes = new Set();

// Convert HSL to RGB
function hslToRgb(h, s, l) {
    s /= 100;
    l /= 100;
    const k = n => (n + h / 30) % 12;
    const a = s * Math.min(l, 1 - l);
    const f = n => l - a * Math.max(-1, Math.min(k(n) - 3, Math.min(9 - k(n), 1)));
    return [Math.round(255 * f(0)), Math.round(255 * f(8)), Math.round(255 * f(4))];
}

class FallingNote {
    constructor(noteNumber, velocity, channel, delayMs) {
        this.noteNumber = noteNumber;
        this.velocity = velocity;
        this.channel = channel;
        this.hue = (noteNumber * 3) % 360;
        
        // Convert HSL to RGB
        const [r, g, b] = hslToRgb(this.hue, 80, 60);
        this.rgb = `${r}, ${g}, ${b}`;
        this.color = `rgb(${this.rgb})`;
        
        // 根据音符号计算水平位置（21-108 映射到画布宽度）
        const noteRange = 108 - 21;
        const notePosition = (noteNumber - 21) / noteRange;
        this.targetX = notePosition * canvas.width;
        
        // 起始位置在顶部
        this.x = this.targetX;
        this.y = 0;
        
        // 钢琴键的位置（底部往上180px左右）
        this.targetY = canvas.height - 200;
        
        // 计算下落速度：需要在 delayMs 毫秒内到达目标位置
        this.totalDistance = this.targetY;
        this.speed = this.totalDistance / delayMs; // pixels per millisecond
        
        // 音符块的尺寸
        this.width = Math.max(8, canvas.width / noteRange * 0.8);
        this.height = 20 + (velocity / 127) * 30;
        
        this.alpha = 0.9;
        this.reached = false;
        this.shouldRemove = false;
        
        // 记录创建时间，用于精确计时
        this.createdAt = performance.now();
        this.delayMs = delayMs;
    }

    update(deltaTime) {
        if (this.reached) {
            // 已经到达，淡出效果
            this.alpha -= 0.05;
            if (this.alpha <= 0) {
                this.shouldRemove = true;
            }
            return;
        }

        // 使用精确的时间计算位置
        const elapsed = performance.now() - this.createdAt;
        const progress = Math.min(elapsed / this.delayMs, 1);
        
        this.y = progress * this.totalDistance;

        // 检查是否到达钢琴键位置
        if (progress >= 1) {
            this.reached = true;
            this.y = this.targetY;
        }
    }

    draw() {
        ctx.save();
        ctx.globalAlpha = this.alpha;

        // 绘制发光效果
        const gradient = ctx.createLinearGradient(
            this.x - this.width / 2, this.y,
            this.x + this.width / 2, this.y
        );
        gradient.addColorStop(0, `rgba(${this.rgb}, 0.3)`);
        gradient.addColorStop(0.5, this.color);
        gradient.addColorStop(1, `rgba(${this.rgb}, 0.3)`);

        // 外发光
        ctx.shadowColor = this.color;
        ctx.shadowBlur = 20;
        ctx.fillStyle = gradient;
        ctx.fillRect(
            this.x - this.width / 2,
            this.y,
            this.width,
            this.height
        );

        // 内部高亮
        ctx.shadowBlur = 0;
        const highlightGradient = ctx.createLinearGradient(
            this.x - this.width / 2, this.y,
            this.x - this.width / 2, this.y + this.height
        );
        highlightGradient.addColorStop(0, `rgba(255, 255, 255, 0.6)`);
        highlightGradient.addColorStop(1, `rgba(${this.rgb}, 0.2)`);
        
        ctx.fillStyle = highlightGradient;
        ctx.fillRect(
            this.x - this.width / 2,
            this.y,
            this.width,
            this.height
        );

        ctx.restore();
    }

    isTargetNote() {
        return this.reached;
    }
}

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

// Animation loop
let lastTime = performance.now();

function animate(currentTime) {
    const deltaTime = currentTime - lastTime;
    lastTime = currentTime;

    // 清除画布，使用透明度创建拖尾效果
    ctx.fillStyle = 'rgba(10, 10, 10, 0.1)';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // 更新和绘制所有下落的音符
    for (let i = fallingNotes.length - 1; i >= 0; i--) {
        const note = fallingNotes[i];
        note.update(deltaTime);
        note.draw();

        if (note.shouldRemove) {
            fallingNotes.splice(i, 1);
        }
    }

    requestAnimationFrame(animate);
}

animate(performance.now());

// SignalR event handlers

// 接收音符预告 - 创建下落的音符
connection.on("ReceiveNotePreview", (noteNumber, velocity, channel, noteName, delayMs) => {
    const fallingNote = new FallingNote(noteNumber, velocity, channel, delayMs);
    fallingNotes.push(fallingNote);
});

// 接收音符触发 - 激活钢琴键
connection.on("ReceiveNoteOn", (noteNumber, velocity, channel, noteName, timestamp) => {
    activeNotes.add(noteNumber);
    activeNotesDisplay.textContent = activeNotes.size;

    // Activate piano key
    const key = pianoContainer.querySelector(`[data-note="${noteNumber}"]`);
    if (key) {
        const hue = (noteNumber * 3) % 360;
        key.classList.add('active');
        key.style.boxShadow = `0 0 30px hsl(${hue}, 80%, 60%)`;
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