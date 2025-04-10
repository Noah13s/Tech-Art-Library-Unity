import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

const socket = new WebSocket('ws://localhost:3000');
const scaleFactor = 1 / 1e9;

// Listen for messages
socket.onmessage = function(event) {
    if (event.data instanceof Blob) {
        const reader = new FileReader();
        reader.onloadend = function() {
            try {
                const msg = JSON.parse(reader.result);
                if (msg.type === "playerPosition") {
                    // Process the player's position here
                    player.position.set(msg.x * scaleFactor, msg.y * scaleFactor, msg.z * scaleFactor);
                    console.log(`Player position: X=${msg.x * scaleFactor}, Y=${msg.y * scaleFactor}, Z=${msg.z * scaleFactor}`);
                }
            } catch (err) {
                console.error("Failed to parse WebSocket message:", err);
            }
        };
        reader.readAsText(event.data);
    }
};

// --- Scene Setup ---
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 10000);

const renderer = new THREE.WebGLRenderer();
renderer.setSize(window.innerWidth, window.innerHeight);
document.body.appendChild(renderer.domElement);

const controls = new OrbitControls(camera, renderer.domElement);
camera.position.set(0, 5, 15);
controls.update();

// --- Helper to create celestial objects ---
function createObject(name, geometry, material, position) {
    const mesh = new THREE.Mesh(geometry, material);
    mesh.name = name;
    mesh.position.set(...position);
    scene.add(mesh);
    return mesh;
}

// --- Objects ---
const sun = createObject('Sun', new THREE.SphereGeometry(2, 32, 32), new THREE.MeshStandardMaterial({
    color: 0xffff00,
    emissive: 0xffff00,
    emissiveIntensity: 10
}), [0, 0, 0]);

const earth = createObject('Earth', new THREE.SphereGeometry(0.5, 32, 32), new THREE.MeshStandardMaterial({
    color: 0x0000ff,
    emissive: 0x0000ff,
    emissiveIntensity: 10
}), [149.5978707, 0, 0]);

const moon = createObject('Moon', new THREE.SphereGeometry(0.2, 32, 32), new THREE.MeshStandardMaterial({
    color: 0x888888,
    emissive: 0x888888,
    emissiveIntensity: 10
}), [149.9822707, 0, 0]);

const player = createObject('Player', new THREE.SphereGeometry(0.3, 32, 32), new THREE.MeshStandardMaterial({
    color: 0xff0000,
    emissive: 0xff0000,
    emissiveIntensity: 10
}), [8, 2, 0]);

// --- Raycasting Setup ---
const raycaster = new THREE.Raycaster();
const mouse = new THREE.Vector2();
let hoveredObject = null;
let stickyObject = null;

// --- Label UI ---
const label = document.createElement('div');
label.style.position = 'absolute';
label.style.backgroundColor = 'rgba(0,0,0,0.7)';
label.style.color = '#fff';
label.style.padding = '5px';
label.style.borderRadius = '4px';
label.style.display = 'none';
label.style.pointerEvents = 'none';
document.body.appendChild(label);

// --- Update label content ---
function updateLabelContent(obj) {
    label.innerHTML = `
        <strong>${obj.name}</strong><br>
        x: ${obj.position.x.toFixed(2)}<br>
        y: ${obj.position.y.toFixed(2)}<br>
        z: ${obj.position.z.toFixed(2)}
    `;
}

// --- Mouse Move (hover logic) ---
window.addEventListener('mousemove', (event) => {
    mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
    mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;

    raycaster.setFromCamera(mouse, camera);
    const intersects = raycaster.intersectObjects(scene.children);

    if (intersects.length > 0) {
        const obj = intersects[0].object;
        if (!stickyObject) {
            hoveredObject = obj;
            label.style.display = 'block';
            updateLabelContent(obj);
        }
    } else {
        if (!stickyObject) {
            hoveredObject = null;
            label.style.display = 'none';
        }
    }
});

// --- Click to toggle stickiness ---
// Modified: Only toggle label when an object is selected; if nothing is hovered, do nothing.
window.addEventListener('click', () => {
    if (hoveredObject) {
        if (stickyObject && stickyObject === hoveredObject) {
            // Toggle off sticky but do not hide the label immediately
            stickyObject = null;
            updateLabelContent(hoveredObject);
        } else {
            stickyObject = hoveredObject;
            updateLabelContent(stickyObject);
            label.style.display = 'block';
        }
    }
    // If nothing is hovered, do nothing (the label remains as is).
});

// --- Animate loop ---
function animate() {
    renderer.render(scene, camera);
    controls.update();

    const targetObj = stickyObject || hoveredObject;

    if (targetObj) {
        const vector = targetObj.position.clone().project(camera);

        const x = (vector.x * 0.5 + 0.5) * window.innerWidth;
        const y = (-vector.y * 0.5 + 0.5) * window.innerHeight;

        label.style.left = `${x + 10}px`;
        label.style.top = `${y + 10}px`;
    }
}

renderer.setAnimationLoop(animate);
