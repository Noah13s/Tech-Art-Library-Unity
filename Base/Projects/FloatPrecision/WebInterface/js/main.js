import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { FontLoader } from 'three/addons/loaders/FontLoader.js';

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
                        centerCameraOnObject(player);
                    }
                }
            } catch (err) {
                console.error("Failed to parse WebSocket message:", err);
            }
        };
        reader.readAsText(event.data); // Convert the Blob to text
    }
};

// --- Scene Setup ---
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 10000);

const renderer = new THREE.WebGLRenderer();
renderer.setSize(window.innerWidth, window.innerHeight);
document.body.appendChild(renderer.domElement);

const controls = new OrbitControls(camera, renderer.domElement);
camera.position.set(0, 50, 100);
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

// --- Focus Mode Variables ---
let focusedObject = null;
const defaultFocusObject = sun;

// --- Function to center camera on object ---
function centerCameraOnObject(object) {
    controls.target.copy(object.position);
    controls.update();
}

// --- Raycasting Setup ---
const raycaster = new THREE.Raycaster();
const mouse = new THREE.Vector2();
let hoveredObject = null;

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

// --- Create Orientation Axes ---
// Create a separate scene for the orientation axes
const axesScene = new THREE.Scene();
const axesCamera = new THREE.PerspectiveCamera(50, 1, 0.1, 10);
axesCamera.position.set(0, 0, 2);

// Create orientation axes with label meshes
const axesHelper = new THREE.AxesHelper(1);
axesScene.add(axesHelper);

// Create text meshes for axes labels
const axesContainer = document.createElement('div');
axesContainer.style.position = 'absolute';
axesContainer.style.top = '10px';
axesContainer.style.right = '10px';
axesContainer.style.width = '100px';
axesContainer.style.height = '100px';
axesContainer.style.pointerEvents = 'none';
document.body.appendChild(axesContainer);

// Create a renderer for the axes
const axesRenderer = new THREE.WebGLRenderer({ alpha: true });
axesRenderer.setSize(100, 100);
axesRenderer.setClearColor(0x000000, 0);
axesContainer.appendChild(axesRenderer.domElement);

// Create 3D text meshes for the axes labels
function createAxisLabelMesh(text, color, position) {
    const fontLoader = new FontLoader();
    // Use a simple div element as a placeholder instead of 3D text
    const labelElem = document.createElement('div');
    labelElem.textContent = text;
    labelElem.style.position = 'absolute';
    labelElem.style.color = color;
    labelElem.style.fontWeight = 'bold';
    labelElem.style.fontSize = '12px';
    labelElem.style.pointerEvents = 'none';
    axesContainer.appendChild(labelElem);
    return { element: labelElem, position: position };
}

// Create axis label objects
const axisLabels = [
    createAxisLabelMesh('X', '#ff0000', new THREE.Vector3(1.2, 0, 0)),
    createAxisLabelMesh('Y', '#00ff00', new THREE.Vector3(0, 1.2, 0)),
    createAxisLabelMesh('Z', '#0000ff', new THREE.Vector3(0, 0, 1.2))
];

// --- Mouse Move (hover logic) ---
window.addEventListener('mousemove', (event) => {
    mouse.x = (event.clientX / window.innerWidth) * 2 - 1;
    mouse.y = -(event.clientY / window.innerHeight) * 2 + 1;

    raycaster.setFromCamera(mouse, camera);
    const intersects = raycaster.intersectObjects(scene.children);

    if (intersects.length > 0) {
        const obj = intersects[0].object;
        // If we're not in focus mode or hovering over different object than focused
        if (!focusedObject || focusedObject !== obj) {
            hoveredObject = obj;
            // Only show label for hovered object if we're not in focus mode
            if (!focusedObject) {
                label.style.display = 'block';
                updateLabelContent(obj);
            }
        }
    } else {
        hoveredObject = null;
        // Only hide label if we're not in focus mode
        if (!focusedObject) {
            label.style.display = 'none';
        }
    }
});

// --- Click to toggle focus mode ---
window.addEventListener('click', () => {
    if (hoveredObject) {
        if (focusedObject && focusedObject === hoveredObject) {
            // Exit focus mode and return to default (sun)
            focusedObject = null;
            centerCameraOnObject(defaultFocusObject);
            label.style.display = 'none';
            console.log("Exiting focus mode, centering on sun");
        } else {
            // Enter focus mode on clicked object
            focusedObject = hoveredObject;
            centerCameraOnObject(focusedObject);
            updateLabelContent(focusedObject);
            label.style.display = 'block';
            console.log(`Entering focus mode on: ${focusedObject.name}`);
        }
    }
});

// --- Handle window resizing ---
window.addEventListener('resize', () => {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
});

// --- Function to update axis labels positions ---
function updateAxisLabels() {
    // Center point of the axes widget
    const centerX = 50;
    const centerY = 50;
    const scale = 35; // Scale factor for axis visibility
    
    // For each axis label
    axisLabels.forEach(label => {
        // Create a copy of the position vector
        const pos = label.position.clone();
        
        // Apply the camera's rotation
        pos.applyQuaternion(axesHelper.quaternion);
        
        // Project to 2D space and position the label accordingly
        label.element.style.left = `${centerX + pos.x * scale}px`;
        label.element.style.top = `${centerY - pos.y * scale}px`;
        
        // Handle visibility - hide labels that are pointing away (z < 0)
        if (pos.z < 0) {
            label.element.style.opacity = '0.3'; // Dim labels pointing away
        } else {
            label.element.style.opacity = '1';
        }
    });
}

// --- Animate loop ---
function animate() {
    renderer.render(scene, camera);
    controls.update();

    // Update orientation axes to match camera rotation
    axesHelper.quaternion.copy(camera.quaternion).invert();
    axesRenderer.render(axesScene, axesCamera);
    updateAxisLabels();
    console.log("Camera Zoom:"+controls.getDistance());

    // Update label position
    // If in focus mode, always show the label for focused object
    // If not in focus mode but hovering over an object, show label for hovered object
    const targetObj = focusedObject || hoveredObject;

    if (targetObj) {
        const vector = targetObj.position.clone().project(camera);

        const x = (vector.x * 0.5 + 0.5) * window.innerWidth;
        const y = (-vector.y * 0.5 + 0.5) * window.innerHeight;

        label.style.left = `${x + 10}px`;
        label.style.top = `${y + 10}px`;
    }
}

renderer.setAnimationLoop(animate);