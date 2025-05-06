using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magazine : MonoBehaviour
{
    public BulletData bulletData;
    public int currentAmmo = 0;
    public int maxAmmo = 30;

    public void Fire()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            Debug.Log("Fired! Current ammo: " + currentAmmo);
        }
        else
        {
            Debug.Log("Out of ammo!");
        }
    }
}
