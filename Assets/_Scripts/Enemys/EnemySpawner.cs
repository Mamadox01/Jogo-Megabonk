using UnityEngine;
using UnityEngine.Pool;

public class EnemySpawner : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public EnemyData defaultEnemyData;
    public LayerMask groundLayer;

    [Header("Ritmo de Oleadas")]
    public float spawnInterval = 1.2f;
    public float minSpawnInterval = 0.2f;
    public int maxActiveEnemies = 250;

    [Header("Radio de Aparición")]
    public float minSpawnRadius = 16f;
    public float maxSpawnRadius = 26f;
    public float raycastHeight = 25f;

    private IObjectPool<EnemyController> _enemyPool;
    private float _spawnTimer;
    private float _elapsedTime;
    private int _activeCount;

    private void Awake()
    {
        _enemyPool = new ObjectPool<EnemyController>(
            createFunc: CreateEnemy,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: false,
            defaultCapacity: 100,
            maxSize: 400
        );
    }

    private void Update()
    {
        if (player == null || defaultEnemyData == null || defaultEnemyData.prefab == null) return;

        _elapsedTime += Time.deltaTime;
        _spawnTimer -= Time.deltaTime;

        // Acelera progresivamente la aparición con el paso de los minutos
        float currentInterval = Mathf.Max(minSpawnInterval, spawnInterval - (_elapsedTime * 0.005f));

        if (_spawnTimer <= 0f && _activeCount < maxActiveEnemies)
        {
            SpawnEnemy();
            _spawnTimer = currentInterval;
        }
    }

    private void SpawnEnemy()
    {
        if (TryGetValidSpawnPosition(out Vector3 spawnPos))
        {
            EnemyController enemy = _enemyPool.Get();
            enemy.transform.position = spawnPos;

            // Escala la vida un +15% por cada minuto sobrevivido
            float hpScale = 1f + (_elapsedTime / 60f) * 0.15f;
            enemy.Initialize(player, _enemyPool, hpScale);
        }
    }

    private bool TryGetValidSpawnPosition(out Vector3 validPosition)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float randomDist = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 candidate = player.position + new Vector3(randomDir.x * randomDist, raycastHeight, randomDir.y * randomDist);

            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, raycastHeight * 2.5f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                validPosition = hit.point + Vector3.up * 1f;
                return true;
            }
        }

        validPosition = Vector3.zero;
        return false;
    }

    private EnemyController CreateEnemy()
    {
        GameObject obj = Instantiate(defaultEnemyData.prefab);
        EnemyController controller = obj.GetComponent<EnemyController>();
        controller.data = defaultEnemyData;
        return controller;
    }

    private void OnTakeFromPool(EnemyController enemy)
    {
        enemy.gameObject.SetActive(true);
        _activeCount++;
    }

    private void OnReturnedToPool(EnemyController enemy)
    {
        enemy.gameObject.SetActive(false);
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }

    private void OnDestroyPoolObject(EnemyController enemy)
    {
        if (enemy != null) Destroy(enemy.gameObject);
    }
}
