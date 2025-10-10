using UnityEditor;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Magazine magazine;
    public Transform bulletSpawnPoint; // Where bullets spawn from
    public BulletData[] compatibleAmmo; // Array of compatible ammo types


    public void Fire()
    {
        // Check if the magazine has ammo
        if (magazine == null || magazine.currentAmmo <= 0)
        {
            Debug.LogWarning("Cannot fire: Magazine is empty or not assigned.");
            return;
        }

        // Validate if the current bullet type in the magazine is compatible
        bool isCompatible = false;
        foreach (BulletData ammo in compatibleAmmo)
        {
            if (ammo == magazine.bulletData)
            {
                isCompatible = true;
                break;
            }
        }

        if (!isCompatible)
        {
            Debug.LogWarning("Cannot fire: Current bullet type in the magazine is not compatible with this weapon.");
            return;
        }

        // Fire the magazine (reduce ammo count)
        magazine.Fire();

        // Instantiate the bullet prefab at the spawn point
        if (magazine.bulletData.bulletPrefab == null)
        {
            Debug.LogError("Cannot fire: Bullet prefab is not assigned in the BulletData.");
            return;
        }

        GameObject bulletGO = Instantiate(magazine.bulletData.bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        // Assign bullet data to the instantiated bullet object
        BulletObject bullet = bulletGO.GetComponent<BulletObject>();
        if (bullet != null)
        {
            bullet.bulletData = magazine.bulletData;
        }
        else
        {
            Debug.LogWarning("Bullet prefab is missing BulletObject component!");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Weapon))]
public class WeaponEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        if (!Application.isPlaying) { return; }
        // Add a button to the inspector
        if (GUILayout.Button("Fire"))
        {
            // Code to execute when button is clicked
            Weapon weapon = (Weapon)target;

            // Example: Call a method from your Weapon script
            weapon.Fire();
        }
    }
}
#endif