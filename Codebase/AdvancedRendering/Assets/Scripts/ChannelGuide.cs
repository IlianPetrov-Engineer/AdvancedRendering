using System.Collections.Generic;
using UnityEngine;

public enum SphereCollisionMode
{
    None,
    Global,
    UniformGrid
}

public class ChannelGuide : MonoBehaviour
{
    #region Variables
    [Header("Channel")]
    public float width = 2.5f;
    public float wallHeight = 1f;
    public float verticalOffset = 0.4f;
    public float innerInset = 0.5f;

    [Header("Physics")]
    [SerializeField] private float gravityStrength = 9.81f;
    [SerializeField] private float particleRadius = 0.1f;

    [Header("Sphere Collision")]
    [SerializeField] private SphereCollisionMode collisionMode;

    [SerializeField] private float collisionBounce = 0.9f;
    [SerializeField] private float velocityTransfer = 0.5f;

    [Header("Uniform Grid")]
    [SerializeField] private float cellSize = 0.5f;
    private Dictionary<Vector3Int, List<int>> spatialGrid = new Dictionary<Vector3Int, List<int>>();

    #region ParticleSettings
    [SerializeField] private GameObject node;
    public List<GameObject> nodes = new List<GameObject>();
    private List<ChannelSegment> segments = new List<ChannelSegment>();

    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float particlesPerSecond = 50f;
    [SerializeField] private float particleLifetime = 10f;
    [SerializeField] private float forceStrength = 10f;
    [SerializeField] private float damping = 0.98f;
    [SerializeField] private int maxParticles = 100;
    [SerializeField] private float maxSpeed = 10f;
    #endregion

    private float spawnTimer;
    private List<ParticleData> particles = new List<ParticleData>();
    private List<GameObject> particleObjects = new List<GameObject>();

    private bool outdatedSegment = false;
    private List<Vector3> previousNodePositions = new List<Vector3>();

    private float simulationTimer = 0f;
    private const float fixedStep = 1f / 120f;
    #endregion

    private void LateUpdate()
    {
        CheckNodeChanges();

        if (outdatedSegment)
        {
            BuildSegments();
        }

        simulationTimer += Time.deltaTime;

        int substeps = 0;
        const int maxSubsteps = 8;

        while (simulationTimer >= fixedStep &&
               substeps < maxSubsteps)
        {
            Simulate(fixedStep);

            simulationTimer -= fixedStep;
            substeps++;
        }

        RenderParticles();
    }

    #region Channel
    public GameObject Note()
    {
        int lastNote = nodes.Count - 1;
        Vector3 pos;
        if (nodes.Count == 0)
            pos = transform.position;
        else
            pos = nodes[lastNote].transform.position;

        GameObject newNode = Instantiate(node, pos, Quaternion.identity, transform);
        nodes.Add(newNode);

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null)
                nodes.RemoveAt(i);
        }

        return newNode;
    }

    public void BuildSegments()
    {
        segments.Clear();

        if (nodes.Count < 2)
            return;

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            if (nodes[i] == null || nodes[i + 1] == null)
                continue;

            Vector3 start = nodes[i].transform.position;
            Vector3 end = nodes[i + 1].transform.position;

            Vector3 direction = (end - start).normalized;
            float length = Vector3.Distance(start, end);

            ChannelSegment segment = new ChannelSegment();
            segment.start = start;
            segment.end = end;
            segment.direction = direction;
            segment.length = length;

            segments.Add(segment);
        }

        outdatedSegment = false;
        MeshGeneration meshGen = GetComponent<MeshGeneration>();

        if (meshGen != null)
            meshGen.SetMesh();
    }
    #endregion

    #region ParticleMovement
    void SpawnParticle()
    {
        if (nodes.Count < 2) return;

        Vector3 spawnPos = nodes[0].transform.position + new Vector3(0, 1, 0);

        ParticleData particleData = new ParticleData
        {
            position = spawnPos,
            velocity = Vector3.zero,
            currentSegment = 0,
            lifetime = particleLifetime
        };

        particles.Add(particleData);

        GameObject obj = Instantiate(particlePrefab, spawnPos, Quaternion.identity, nodes[0].transform);
        particleObjects.Add(obj);
    }

    void UpdateParticle(ref ParticleData particleData, float deltaTime)
    {
        if (particleData.currentSegment >= segments.Count)
            return;

        var segment = segments[particleData.currentSegment];

        Vector3 direction = segment.direction;

        particleData.velocity += direction * forceStrength * deltaTime;
        particleData.velocity += Vector3.down * gravityStrength * deltaTime;
        particleData.velocity *= Mathf.Pow(damping, deltaTime * 60f);
        particleData.velocity = Vector3.ClampMagnitude(particleData.velocity, maxSpeed);

        int movementSubsteps = 2;

        float subDt = deltaTime / movementSubsteps;

        for (int i = 0; i < movementSubsteps; i++)
        {
            particleData.position += particleData.velocity * subDt;

            for (int j = 0; j < 2; j++)
            {
                ChannelCollision(ref particleData, segment);
            }
        }

        float projection = Vector3.Dot(particleData.position - segment.start, segment.direction);

        if (projection >= segment.length)
            particleData.currentSegment++;
    }

    void Simulate(float dt)
    {
        spawnTimer += dt;

        float interval = 1f / particlesPerSecond;

        while (spawnTimer >= interval && particles.Count < maxParticles)
        {
            spawnTimer -= interval;
            SpawnParticle();
        }

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            ParticleData particleData = particles[i];

            UpdateParticle(ref particleData, dt);

            particleData.lifetime -= dt;

            if (particleData.lifetime <= 0 || particleData.currentSegment >= segments.Count)
            {
                Destroy(particleObjects[i]);

                particleObjects.RemoveAt(i);
                particles.RemoveAt(i);

                continue;
            }

            particles[i] = particleData;
        }

        if (collisionMode == SphereCollisionMode.UniformGrid)
        {
            BuildSpatialGrid();
        }

        ChoseCollisionType();
    }
    #endregion

    #region Collision
    void ChoseCollisionType()
    {
        switch (collisionMode)
        {
            case SphereCollisionMode.None:
                return;

            case SphereCollisionMode.Global:
                GlobalCheckCollision();
                break;

            case SphereCollisionMode.UniformGrid:
                UniformGridCollision();
                break;
        }
    }

    void GlobalCheckCollision()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            for (int j = i + 1; j < particles.Count; j++)
            {
                SphereCollision(i, j);
            }
        }
    }

    #region UniformGrid
    Vector3Int GetCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    void BuildSpatialGrid()
    {
        spatialGrid.Clear();

        for (int i = 0; i < particles.Count; i++)
        {
            Vector3Int cell =
                GetCell(particles[i].position);

            if (!spatialGrid.ContainsKey(cell))
            {
                spatialGrid[cell] = new List<int>();
            }

            spatialGrid[cell].Add(i);
        }
    }

    void UniformGridCollision()
    {
        foreach (var cellPair in spatialGrid)
        {
            Vector3Int cell = cellPair.Key;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int neighbor =
                            cell + new Vector3Int(x, y, z);

                        if (!spatialGrid.ContainsKey(neighbor))
                            continue;

                        List<int> currentParticles =
                            spatialGrid[cell];

                        List<int> neighborParticles =
                            spatialGrid[neighbor];

                        for (int i = 0; i < currentParticles.Count; i++)
                        {
                            for (int j = 0; j < neighborParticles.Count; j++)
                            {
                                int a = currentParticles[i];
                                int b = neighborParticles[j];

                                if (a >= b)
                                    continue;

                                SphereCollision(a, b);
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

    void SphereCollision(int indexA, int indexB)
    {
        ParticleData a = particles[indexA];
        ParticleData b = particles[indexB];

        Vector3 delta = b.position - a.position;

        float distance = delta.magnitude;

        float combinedRadius = particleRadius * 2f;

        if (distance <= 0.0001f || distance >= combinedRadius)
            return;

        Vector3 normal = delta / distance;

        float penetration = combinedRadius - distance;

        a.position -= normal * (penetration * 0.5f);
        b.position += normal * (penetration * 0.5f);

        Vector3 relativeVelocity = b.velocity - a.velocity;

        float velocityAlongNormal =
            Vector3.Dot(relativeVelocity, normal);

        if (velocityAlongNormal > 0)
        {
            particles[indexA] = a;
            particles[indexB] = b;
            return;
        }

        float impulse = -(1f + collisionBounce) * velocityAlongNormal;

        impulse *= 0.5f;

        Vector3 impulseVector = normal * impulse;

        a.velocity -= impulseVector * velocityTransfer;
        b.velocity += impulseVector * velocityTransfer;

        particles[indexA] = a;
        particles[indexB] = b;
    }

    void ChannelCollision(ref ParticleData particleData, ChannelSegment segment)
    {
        Vector3 forward = segment.direction;
        Vector3 referenceUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
        Vector3 right = Vector3.Cross(referenceUp, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right).normalized;

        Vector3 toParticle = particleData.position - segment.start;

        float projection = Vector3.Dot(toParticle, forward);

        projection = Mathf.Clamp(projection, 0f, segment.length);

        Vector3 closestPoint = segment.start + forward * projection;

        Vector3 offset = particleData.position - closestPoint;

        float horizontal = Vector3.Dot(offset, right);

        float vertical = Vector3.Dot(offset, up);

        float halfWidth = (width * 0.5f) - particleRadius;

        float floorHeight = particleRadius;

        float ceiling = wallHeight + verticalOffset - particleRadius;

        bool corrected = false;

        if (horizontal > halfWidth)
        {
            horizontal = halfWidth;
            corrected = true;
        }
        else if (horizontal < -halfWidth)
        {
            horizontal = -halfWidth;
            corrected = true;
        }

        if (vertical < floorHeight)
        {
            vertical = floorHeight;
            corrected = true;
        }

        if (vertical > ceiling)
        {
            vertical = ceiling;
            corrected = true;
        }

        if (corrected)
        {
            particleData.position = closestPoint + right * horizontal + up * vertical;
            Vector3 wallNormal = Vector3.zero;

            if (Mathf.Abs(horizontal) >= halfWidth - 0.001f)
            {
                wallNormal = right * Mathf.Sign(horizontal);
            }

            if (vertical <= floorHeight + 0.001f)
                wallNormal += up;

            wallNormal.Normalize();

            float intoWall = Vector3.Dot(particleData.velocity, wallNormal);

            if (intoWall < 0)
            {
                particleData.velocity -= wallNormal * intoWall;
            }
        }

    }

    #endregion

    void RenderParticles()
    {
        for (int i = 0; i < particles.Count; i++)
        {
            particleObjects[i].transform.position = particles[i].position;
        }
    }

    void CacheNodePositions()
    {
        previousNodePositions.Clear();

        for (int i = 0; i < nodes.Count; i++)
        {
            previousNodePositions.Add(nodes[i].transform.position);
        }
    }

    void CheckNodeChanges()
    {
        bool changedPos = false;

        if (nodes.Count != previousNodePositions.Count)
        {
            changedPos = true;
        }
        else
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                {
                    changedPos = true;
                    break;
                }

                if (nodes[i].transform.position != previousNodePositions[i])
                {
                    changedPos = true;
                    break;
                }
            }
        }

        if (changedPos)
        {
            outdatedSegment = true;
            CacheNodePositions();
        }
    }

    public void CleanUp()
    {
        nodes.RemoveAll(item => item == null);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        float radius = 1f;

        foreach (var segment in segments)
        {
            Gizmos.DrawLine(segment.start, segment.end);
            Gizmos.DrawWireSphere(segment.start, radius);
            Gizmos.DrawWireSphere(segment.end, radius);
        }
    }

    [System.Serializable]
    public class ChannelSegment
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 direction;
        public float length;
    }

    struct ParticleData
    {
        public Vector3 position;
        public Vector3 velocity;
        public int currentSegment;
        public float lifetime;
    }
}

