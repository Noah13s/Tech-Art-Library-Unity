import { player, moon, sun, earth } from './scene.js';

const socket = new WebSocket('ws://localhost:3000');
const scaleFactor = 1 / 1e9;


// Listen for messages
socket.onmessage = function(event) {
    // Check if the message is a Blob (which it is if it's binary data)
    if (event.data instanceof Blob) {
        const reader = new FileReader();
        reader.onloadend = function() {
            try {
                const msg = JSON.parse(reader.result);
                if (msg.type === "playerPosition") {
                    // Process the player's position here
                    const playerPosition = msg;
                    player.position.set(msg.x * scaleFactor, msg.y * scaleFactor, msg.z * scaleFactor);
                    console.log(`Player position: X=${playerPosition.x}, Y=${playerPosition.y}, Z=${playerPosition.z}`);
                    
                    // If the player is in focus, update camera target
                    if (focusedObject && focusedObject.name === 'Player') {
                        if (!isTransitioning) {
                            // Only update directly if not in transition
                            controls.target.copy(player.position);
                        }
                    }
                }
            } catch (err) {
                console.error("Failed to parse WebSocket message:", err);
            }
        };
        reader.readAsText(event.data); // Convert the Blob to text
    }
};