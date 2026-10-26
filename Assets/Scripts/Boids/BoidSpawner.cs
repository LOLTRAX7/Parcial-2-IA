using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    [SerializeField] private BoidAgent boidPrefab;
    [SerializeField] private int count = 8;

    private void Start()
    {
        if (boidPrefab == null)
        {
            Debug.LogWarning("[BoidSpawner] No BoidAgent prefab assigned.", this);
            return;
        }

        if (Bounds.Instance == null)
        {
            Debug.LogWarning("[BoidSpawner] No Bounds object found in the scene.", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = Bounds.Instance.GetRandomPointInside();
            spawnPosition.y = boidPrefab.transform.position.y;
            BoidAgent boid = Instantiate(boidPrefab, spawnPosition, Quaternion.identity);
            boid.name = "Boid_" + (i + 1);
        }
    }
}
