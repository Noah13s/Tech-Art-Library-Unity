using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master_Counter : MonoBehaviour
{
    [Serializable]
    public struct TransformSpawnPoints
    {
        [SerializeField]
        private Transform transformSpawnPoint;
        [SerializeField]
        public Vector2 initialVelocity;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
