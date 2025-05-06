using UnityEditor;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Magazine magazine;
    public Transform bulletSpawnPoint; // Where bullets spawn from


    public void Fire()
    {
        magazine.Fire();

        // Instantiate the bullet prefab at the spawn point
        GameObject bulletGO = Instantiate(magazine.bulletData.bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        // Get the BulletObject component and assign bullet data
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
            Debug.Log("Custom action triggered for " + weapon.name);

            // Example: Call a method from your Weapon script
            weapon.Fire();
        }
    }
}
#endif