import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { FontLoader } from 'three/addons/loaders/FontLoader.js';
import { TWEEN } from 'three/addons/libs/tween.module.min.js';

import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';


// --- Scene Setup ---
const scene = new THREE.Scene();
export { scene };
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 10000);

const ambientLight = new THREE.AmbientLight(0xffffff, 1); // soft white light
scene.add(ambientLight);

const renderer = new THREE.WebGLRenderer();
renderer.setSize(window.innerWidth, window.innerHeight);
document.body.appendChild(renderer.domElement);

const controls = new OrbitControls(camera, renderer.domElement);
camera.position.set(0, 50, 100);
controls.update();

const composer = new EffectComposer(renderer);
composer.addPass(new RenderPass(scene, camera));

// Add bloom effect (glow)
const bloomPass = new UnrealBloomPass(
    new THREE.Vector2(window.innerWidth, window.innerHeight),
    0.5, // strength
    0.4, // radius
    0.85 // threshold
);
composer.addPass(bloomPass);


// --- Focus Mode Variables ---
let focusedObject = null;
export { focusedObject };
let isTransitioning = false;
const transitionDuration = 1500; // Transition time in milliseconds

// --- Function to smoothly transition camera to focus on object ---
function smoothCameraTransition(targetObject) {
    if (isTransitioning) return; // Prevent overlapping transitions
    
    isTransitioning = true;
    controls.enabled = false; // Temporarily disable orbit controls during transition
    
    // Store current camera position and target
    const startPosition = camera.position.clone();
    const startTarget = controls.target.clone();
    
    // Calculate target position (keep same relative distance)
    const targetPosition = targetObject.position.clone();
    const direction = new THREE.Vector3().subVectors(camera.position, controls.target).normalize();
    
    // Get current distance from camera to target
    const distance = camera.position.distanceTo(controls.target);
    
    // Calculate new camera position based on same distance but centered on new target
    const endPosition = targetPosition.clone().add(direction.multiplyScalar(distance));
    
    // Create tweens for smooth transition
    const positionTween = new TWEEN.Tween(startPosition)
        .to(endPosition, transitionDuration)
        .easing(TWEEN.Easing.Cubic.InOut)
        .onUpdate(() => {
            camera.position.copy(startPosition);
        });
    
    const targetTween = new TWEEN.Tween(startTarget)
        .to(targetPosition, transitionDuration)
        .easing(TWEEN.Easing.Cubic.InOut)
        .onUpdate(() => {
            controls.target.copy(startTarget);
            controls.update();
        })
        .onComplete(() => {
            isTransitioning = false;
            controls.enabled = true; // Re-enable controls after transition
            controls.update();
            console.log(`Camera transition to ${targetObject.name} complete`);
        });
    
    // Start both tweens simultaneously
    positionTween.start();
    targetTween.start();
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

// Function to update the sidebar with the selected object's user data
function updateSidebarFromObject(selectedObject) {
    const bodyData = selectedObject.userData;
    const infoPanel = document.getElementById("sideBar");
    infoPanel.getElementsByClassName("title")[0].textContent = bodyData.name;
    infoPanel.getElementsByClassName("subTitle")[0].textContent = bodyData.type;
    document.getElementById("panelContent").innerHTML = `
      <p>Mass: ${bodyData.mass}</p>
      <p>Radius: ${bodyData.radius}</p>
      <p>Distance from Sun: ${bodyData.distanceFromSun}</p>
      <img src="${bodyData.imageUrl}" alt="${bodyData.name}" />
    `;
}
export { updateSidebarFromObject };

// Create axis label objects
const axisLabels = [
    createAxisLabelMesh('X', '#ff0000', new THREE.Vector3(1.2, 0, 0)),
    createAxisLabelMesh('Y', '#00ff00', new THREE.Vector3(0, 1.2, 0)),
    createAxisLabelMesh('Z', '#0000ff', new THREE.Vector3(0, 0, 1.2))
];

// --- Mouse Move (hover logic) ---
window.addEventListener('mousemove', (event) => {
    // Skip hover detection during camera transitions
    if (isTransitioning) return;
    
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
    // Prevent clicking during transitions
    if (isTransitioning) return;
    
    if (hoveredObject) {
        if (focusedObject && focusedObject === hoveredObject) {
            // Exit focus mode and return to default (sun)
            focusedObject = null;
            smoothCameraTransition(defaultFocusObject);
            label.style.display = 'none';
            console.log("Exiting focus mode, centering on sun");
        } else {
            // Enter focus mode on clicked object
            focusedObject = hoveredObject;
            smoothCameraTransition(focusedObject);
            updateLabelContent(focusedObject);
            label.style.display = 'block';
            console.log(`Entering focus mode on: ${focusedObject.name}`);
            updateSidebarFromObject(focusedObject);
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
    requestAnimationFrame(animate);
    
    // Update TWEEN animations
    TWEEN.update();
    
    controls.update();
    composer.render();

    // Update orientation axes to match camera rotation
    axesHelper.quaternion.copy(camera.quaternion).invert();
    axesRenderer.render(axesScene, axesCamera);
    updateAxisLabels();

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


// Start the animation loop
animate();
