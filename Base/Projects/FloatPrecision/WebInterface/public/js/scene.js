import * as THREE from 'three';
import { scene } from './main.js';

const textureLoader = new THREE.TextureLoader();

var sun;
var earth;

// --- Helper to create celestial objects ---
function createObject(name, geometry, material, position) {
    const mesh = new THREE.Mesh(geometry, material);
    mesh.name = name;
    mesh.position.set(...position);
    scene.add(mesh);
    return mesh;
}

// --- Object Creation ---
textureLoader.load('/public/textures/2k_sun.jpg', (sunTexture) => {
    const sunMaterial = new THREE.MeshStandardMaterial({
        map: sunTexture,
        emissive: 0xffff00,
        emissiveIntensity: .5
    });

    sun = createObject('Sun', new THREE.SphereGeometry(2, 32, 32), sunMaterial, [0, 0, 0]);
});
textureLoader.load('/public/textures/2k_earth_daymap.jpg', (earthTexture) => {
    const earthMaterial = new THREE.MeshStandardMaterial({
        map: earthTexture,
        emissive: 0x0000ff,
        emissiveIntensity: 0
    });

    earth = createObject(
        'Earth',
        new THREE.SphereGeometry(0.5, 32, 32),
        earthMaterial,
        [149.5978707, 0, 0]
    );
});
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

export { player, moon, sun, earth };
