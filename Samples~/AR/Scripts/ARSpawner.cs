using UnityEngine;

public class ARSpawner : MonoBehaviour
{
    private ARPlayer player;
    private void Awake()
    {
        player = GetComponentInParent<ARPlayer>();
    }
    public void SpawnPrefab(GameObject prefab)
    {
        RaycastHit hit;
        if (Physics.Raycast(player.camera.transform.position, player.camera.transform.forward, out hit))
        {
            Instantiate(prefab, hit.point, new Quaternion());
        }
    }
}
