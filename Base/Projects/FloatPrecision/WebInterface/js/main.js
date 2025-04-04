import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

// --- Scene Setup ---
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);

const renderer = new THREE.WebGLRenderer();
renderer.setSize(window.innerWidth, window.innerHeight);
document.body.appendChild(renderer.domElement);

const controls = new OrbitControls(camera, renderer.domElement);
camera.position.set(0, 5, 15);
controls.update();

// --- Lighting ---
const light = new THREE.PointLight(0xffffff, 1.5);
light.position.set(0, 0, 0);
scene.add(light);

// --- Helper to create celestial objects ---
function createObject(name, geometry, material, position) {
    const mesh = new THREE.Mesh(geometry, material);
    mesh.name = name;
    mesh.position.set(...position);
    scene.add(mesh);
    return mesh;
}

// --- Objects ---
const sun = createObject('Sun', new THREE.SphereGeometry(2, 32, 32), new THREE.MeshBasicMaterial({ color: 0xffff00 }), [0, 0, 0]);
const earth = createObject('Earth', new THREE.SphereGeometry(0.5, 32, 32), new THREE.MeshStandardMaterial({ color: 0x0000ff }), [8, 0, 0]);
const moon = createObject('Moon', new THREE.SphereGeometry(0.2, 32, 32), new THREE.MeshStandardMaterial({ color: 0x888888 }), [9.5, 0, 0]);
const player = createObject('Player', new THREE.SphereGeometry(0.3, 32, 32), new THREE.MeshStandardMaterial({ color: 0xff0000 }), [8, 2, 0]);

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
window.addEventListener('click', () => {
    if (hoveredObject) {
        if (stickyObject && stickyObject === hoveredObject) {
            stickyObject = null;
            label.style.display = 'none';
        } else {
            stickyObject = hoveredObject;
            updateLabelContent(stickyObject);
            label.style.display = 'block';
        }
    } else {
        stickyObject = null;
        label.style.display = 'none';
    }
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
