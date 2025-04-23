import * as THREE from 'three';
import { scene, updateSidebarFromObject } from './main.js';

const textureLoader = new THREE.TextureLoader();

var sun;
var earth;
var moon;
var player;

// --- Helper to create celestial objects ---
function createObject(name, geometry, material, position, userData) {
    const mesh = new THREE.Mesh(geometry, material);
    mesh.name = name;
    mesh.position.set(...position);
    mesh.userData = userData;  // Store data for the object
    scene.add(mesh);
    return mesh;
}

// --- Object Creation ---
textureLoader.load('/textures/2k_sun.jpg', (sunTexture) => {
    const sunMaterial = new THREE.MeshStandardMaterial({
        map: sunTexture,
        emissive: 0xffff00,
        emissiveIntensity: .5
    });

    sun = createObject('Sun', new THREE.SphereGeometry(2, 32, 32), sunMaterial, [0, 0, 0], {
        name: 'Sun',
        type: 'Yellow Dwarf Star',
        mass: "1.989 × 10^30 kg",
        radius: "696,340 km",
        distanceFromEarth: "149.6 million km",
    });
    updateSidebarFromObject(sun);// Default start object
});
textureLoader.load('/textures/2k_earth_daymap.jpg', (earthTexture) => {
    const earthMaterial = new THREE.MeshStandardMaterial({
        map: earthTexture,
        emissive: 0x0000ff,
        emissiveIntensity: 0
    });

    earth = createObject('Earth', new THREE.SphereGeometry(0.5, 32, 32), earthMaterial, [149.5978707, 0, 0], {
        name: 'Earth',
        type: 'Terrestrial',
        mass: "5.972 × 10^24 kg",
        radius: "6,371 km",
        distanceFromSun: "149.6 million km"
    });
});
moon = createObject('Moon', new THREE.SphereGeometry(0.2, 32, 32), new THREE.MeshStandardMaterial({
    color: 0x888888,
    emissive: 0x888888,
    emissiveIntensity: 10
}), [149.9822707, 0, 0]);
player = createObject('Player', new THREE.SphereGeometry(0.3, 32, 32), new THREE.MeshStandardMaterial({
    color: 0xff0000,
    emissive: 0xff0000,
    emissiveIntensity: 10
}), [8, 2, 0]);

export { player, moon, sun, earth };

