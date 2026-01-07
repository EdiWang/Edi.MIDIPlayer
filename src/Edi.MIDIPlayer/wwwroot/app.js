const connection = new signalR.HubConnectionBuilder()
    .withUrl("/midihub")
    .withAutomaticReconnect()
    .build();

const pianoContainer = document.getElementById('piano-container');
const eventLog = document.getElementById('event-log');
const activeNotesDisplay = document.getElementById('active-notes');
const statusMessage = document.getElementById('status-message');

const activeNotes = new Set();

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

// SignalR event handlers

// ½ÓÊÕÒô·û´¥·¢ - ¼¤»î¸ÖÇÙ¼ü
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