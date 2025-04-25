import * as THREE from 'three';
import { FontLoader } from 'three/addons/loaders/FontLoader.js';

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

export {updateAxisLabels, axesHelper, axesCamera, axesRenderer, axesScene };