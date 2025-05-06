using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Bullet Data")]
public class BulletData : ScriptableObject
{
    public enum BulletShape { Spitzer, BoatTail, FlatBase, RoundNose, FlatNose, HollowPoint, }

    [Tooltip("Speed in m/s")]
    public float speed = 350f;
    public float ballisticCoefficient = 0.1f;
    public float lifetime = 5f;
    public GameObject impactVFX;
    public GameObject bulletPrefab;
}