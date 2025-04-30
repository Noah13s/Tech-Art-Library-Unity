import { player, moon, sun, earth } from './scene.js';
import { focusedObject, isTransitioning, controls } from './main.js';

const socket = new WebSocket('ws://localhost:3000');
const scaleFactor = 1 / 1e9;


// Listen for messages
socket.onmessage = function(event) {
    function handleMessage(data) {
        try {
            const msg = JSON.parse(data);
            if (msg.type === "playerPosition") {
                player.position.set(msg.x * scaleFactor, msg.y * scaleFactor, msg.z * scaleFactor);
                console.log(`Player position: X=${msg.x}, Y=${msg.y}, Z=${msg.z}`);
                
                if (focusedObject && focusedObject.name === 'Player') {
                    if (!isTransitioning) {
                        controls.target.copy(player.position);
                    }
                }
            }
        } catch (err) {
            console.error("Failed to parse WebSocket message:", err);
        }
    }

    if (event.data instanceof Blob) {
        const reader = new FileReader();
        reader.onloadend = () => handleMessage(reader.result);
        reader.readAsText(event.data);
    } else {
        handleMessage(event.data); // Handle string messages directly
    }
};