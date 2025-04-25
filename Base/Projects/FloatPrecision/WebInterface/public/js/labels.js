import * as THREE from 'three'; // Needed for THREE.Vector3, THREE.Vector2

// --- Constants for Label Behavior ---
const REFERENCE_DISTANCE = 40;    // Distance at which inverse scale is 1

// --- Module-level variables for Label Scaling & UI ---
let minLabelScale = 0.5;
let maxLabelScale = 2.0;
let labelSizeSlider, labelSizeValueSpan;
let minScaleSlider, minScaleValueSpan;
let maxScaleSlider, maxScaleValueSpan;
let rootElement; // Reference to <html> element for CSS variables

/**
 * Initializes the label control system.
 * Finds UI elements and sets up event listeners.
 * @param {HTMLElement} controlsContainerEl - The container div for the sliders.
 * @param {HTMLElement} rootEl - The root element (usually document.documentElement) for CSS vars.
 */
function initLabelControls(controlsContainerEl, rootEl) {
    // Find UI elements within the provided container
    labelSizeSlider = controlsContainerEl.querySelector('#labelSizeSlider');
    labelSizeValueSpan = controlsContainerEl.querySelector('#labelSizeValue');
    minScaleSlider = controlsContainerEl.querySelector('#minScaleSlider');
    minScaleValueSpan = controlsContainerEl.querySelector('#minScaleValue');
    maxScaleSlider = controlsContainerEl.querySelector('#maxScaleSlider');
    maxScaleValueSpan = controlsContainerEl.querySelector('#maxScaleValue');
    rootElement = rootEl;

    // Check if all elements were found
    if (!labelSizeSlider || !minScaleSlider || !maxScaleSlider || !rootElement) {
        console.error("Label controls initialization failed: Could not find all required UI elements.");
        return;
    }

    // Add event listeners
    labelSizeSlider.addEventListener('input', updateLabelBaseSize);
    minScaleSlider.addEventListener('input', updateMinScale);
    maxScaleSlider.addEventListener('input', updateMaxScale);

    // Initialize slider values and text displays
    labelSizeSlider.value = parseInt(getComputedStyle(rootElement).getPropertyValue('--base-label-font-size') || '12');
    minScaleSlider.value = minLabelScale;
    maxScaleSlider.value = maxLabelScale;
    updateLabelBaseSize();
    updateMinScale();
    updateMaxScale();

    console.log("Label controls initialized.");
}

/**
 * Creates an HTML label element for a given Three.js object and adds it to the objectsWithLabels array.
 * @param {THREE.Object3D} object3D - The Three.js object to label.
 * @param {HTMLElement} labelContainerEl - The HTML element to append the label to.
 * @param {Array<THREE.Object3D>} objectsWithLabels - Array to store objects with labels.
 * @returns {HTMLElement} The created label element.
 */
function createLabelForObject(object3D, labelContainerEl, objectsWithLabels) {
    const labelDiv = document.createElement('div');
    labelDiv.className = 'label';
    labelDiv.textContent = object3D.name || `Object ${object3D.id}`; // Use name or ID
    labelContainerEl.appendChild(labelDiv);

    // Initialize userData properties needed for label updates on the 3D object
    if (!object3D.userData) object3D.userData = {}; // Ensure userData exists
    object3D.userData.labelElement = labelDiv;
    object3D.userData.screenPos = new THREE.Vector2();
    object3D.userData.distance = 0;
    object3D.userData.isInFront = true;
    object3D.userData.rect = null;

    // Add the object to the objectsWithLabels array
    objectsWithLabels.push(object3D);

    return labelDiv;
}

/**
 * Updates the base font size CSS variable based on slider input.
 */
function updateLabelBaseSize() {
    if (!labelSizeSlider || !rootElement || !labelSizeValueSpan) return;
    const newSize = labelSizeSlider.value;
    rootElement.style.setProperty('--base-label-font-size', newSize + 'px');
    labelSizeValueSpan.textContent = newSize + 'px';
}

/**
 * Updates the minimum scale factor based on slider input.
 */
function updateMinScale() {
    if (!minScaleSlider || !minScaleValueSpan) return;
    minLabelScale = parseFloat(minScaleSlider.value);
    // Ensure min scale doesn't exceed max scale
    if (minLabelScale > maxLabelScale && maxScaleSlider) {
        maxScaleSlider.value = minLabelScale;
        updateMaxScale(); // Update max scale variable and display
    }
    minScaleValueSpan.textContent = minLabelScale.toFixed(1) + 'x';
}

/**
 * Updates the maximum scale factor based on slider input.
 */
function updateMaxScale() {
    if (!maxScaleSlider || !maxScaleValueSpan) return;
    maxLabelScale = parseFloat(maxScaleSlider.value);
    // Ensure max scale doesn't go below min scale
    if (maxLabelScale < minLabelScale && minScaleSlider) {
        minScaleSlider.value = maxLabelScale;
        updateMinScale(); // Update min scale variable and display
    }
    maxScaleValueSpan.textContent = maxLabelScale.toFixed(1) + 'x';
}

/**
 * Core function to update all label positions, visibility, scale, and handle overlaps.
 * @param {Array<THREE.Object3D>} objectsToLabel - Array of Three.js objects that have labels.
 * @param {THREE.Camera} camera - The main scene camera.
 * @param {number} screenWidth - Current screen width.
 * @param {number} screenHeight - Current screen height.
 */
function updateObjectLabels(objectsToLabel, camera, screenWidth, screenHeight) {
    const visibleLabelRects = []; // Stores BBoxes of visible labels this frame
    const tempVector = new THREE.Vector3(); // Reuse vector for performance

    // 1. Calculate screen positions and distances
    objectsToLabel.forEach(obj => {
        if (!obj.userData || !obj.userData.labelElement) return; // Skip if no label data

        obj.getWorldPosition(tempVector); // Get world position
        obj.userData.distance = tempVector.distanceTo(camera.position); // Calc distance

        tempVector.project(camera); // Project to NDC

        // Convert NDC to screen pixels
        obj.userData.screenPos.x = Math.round((tempVector.x + 1) / 2 * screenWidth);
        obj.userData.screenPos.y = Math.round((-tempVector.y + 1) / 2 * screenHeight);

        // Check if in front and beyond near plane
        obj.userData.isInFront = tempVector.z < 1 && obj.userData.distance > camera.near;
    });

    // 2. Sort objects by distance (closer first for overlap priority)
    // Sort based on the pre-calculated distance stored in userData
    objectsToLabel.sort((a, b) => {
        const distA = a.userData && typeof a.userData.distance === 'number' ? a.userData.distance : Infinity;
        const distB = b.userData && typeof b.userData.distance === 'number' ? b.userData.distance : Infinity;
        return distA - distB;
    });


    // 3. Determine visibility, scale, position, and check for overlaps
    objectsToLabel.forEach(obj => {
        const userData = obj.userData;
        if (!userData || !userData.labelElement) return; // Skip if no label data

        const label = userData.labelElement;
        let targetOpacity = 0;
        let targetPointerEvents = 'none';

        if (userData.isInFront) {
            // Calculate INVERSE scale
            let scale = userData.distance / REFERENCE_DISTANCE;
            scale = Math.max(minLabelScale, Math.min(maxLabelScale, scale)); // Clamp

            // Apply transform for measurement
            label.style.transform = `translate(-50%, -110%) scale(${scale})`;
            label.style.left = `${userData.screenPos.x}px`;
            label.style.top = `${userData.screenPos.y}px`;

            const currentRect = label.getBoundingClientRect();
            userData.rect = currentRect;

            let overlaps = false;
            // Check against BBoxes of already visible labels
            for (const visibleRect of visibleLabelRects) {
                if (
                    currentRect.left < visibleRect.right &&
                    currentRect.right > visibleRect.left &&
                    currentRect.top < visibleRect.bottom &&
                    currentRect.bottom > visibleRect.top
                ) {
                    overlaps = true;
                    break;
                }
            }

            // Show if it doesn't overlap
            if (!overlaps) {
                targetOpacity = 1;
                targetPointerEvents = 'auto';
                visibleLabelRects.push(currentRect); // Add rect for overlap checks
            }
        }

        // Apply final opacity
        label.style.opacity = targetOpacity;
        label.style.pointerEvents = targetPointerEvents;
    });
}

// Export the necessary functions
export { initLabelControls, createLabelForObject, updateObjectLabels };
