import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { TWEEN } from 'three/addons/libs/tween.module.min.js';

import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';

import { updateAxisLabels, axesHelper, axesRenderer, axesScene, axesCamera } from './axisguide.js';
// Import label functions from labels.js
import { initLabelControls, createLabelForObject, updateObjectLabels } from './labels.js';

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

// Array to store objects that will have labels
// Note: The label element itself is now managed via userData set in labels.js
let objectsWithLabels = [];

// --- Focus Mode Variables ---
let focusedObject = null;
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
        }
    } else {
        hoveredObject = null;
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
            console.log("Exiting focus mode, centering on sun");
        } else {
            // Enter focus mode on clicked object
            focusedObject = hoveredObject;
            smoothCameraTransition(focusedObject);
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
    
    // --- Update Labels ---
    // Call the imported update function, passing necessary info
    updateObjectLabels(objectsWithLabels, camera, window.innerWidth, window.innerHeight);

    // If in focus mode, always show the label for focused object
    // If not in focus mode but hovering over an object, show label for hovered object
    const targetObj = focusedObject || hoveredObject;
    if (targetObj) {
        const vector = targetObj.position.clone().project(camera);

        const x = (vector.x * 0.5 + 0.5) * window.innerWidth;
        const y = (-vector.y * 0.5 + 0.5) * window.innerHeight;
    }
}

// Start the animation loop
animate();

export { updateSidebarFromObject };
export { focusedObject };
export { objectsWithLabels };

// --- Add Reset Camera Button ---
const resetButton = document.createElement('button');
resetButton.textContent = 'Reset Camera';
resetButton.style.position = 'fixed';
resetButton.style.bottom = '20px';
resetButton.style.right = '20px';
resetButton.style.padding = '10px 20px';
resetButton.style.backgroundColor = '#007BFF';
resetButton.style.color = '#FFFFFF';
resetButton.style.border = 'none';
resetButton.style.borderRadius = '5px';
resetButton.style.cursor = 'pointer';
resetButton.style.zIndex = '1000';
document.body.appendChild(resetButton);

resetButton.addEventListener('click', () => {
    if (focusedObject) {
        smoothCameraTransition(focusedObject);
        console.log(`Resetting camera view to: ${focusedObject.name}`);
    } else {
        console.log('No object is currently selected to reset the camera view.');
    }
});