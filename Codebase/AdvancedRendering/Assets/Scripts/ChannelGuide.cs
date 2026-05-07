using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class ChannelGuide : MonoBehaviour
{
    [SerializeField] private GameObject node;
    public List<GameObject> nodes = new List<GameObject>();
    public List<ChannelSegment> segments = new List<ChannelSegment>();
    [SerializeField] private float radius = 1f;

    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private float particlesPerSecond = 50f;
    [SerializeField] private float particleLifetime = 10f;
    [SerializeField] private float forceStrength = 10f;
    [SerializeField] private float damping = 0.98f;
    [SerializeField] private int maxParticles = 100;
    [SerializeField] private float maxSpeed = 10f;
    private float spawnTimer;
    private List<ParticleData> particles = new List<ParticleData>();
    private List<GameObject> particleObjects = new List<GameObject>();

    public GameObject Test()
    {
        int lastNote = nodes.Count - 1;
        Vector3 pos;
        if (nodes.Count == 0)
            pos = transform.position;
        else
            pos = nodes[lastNote].transform.position;

        GameObject newNode = Instantiate(node, pos, Quaternion.identity, transform);
        nodes.Add(newNode);

        for(int i = 0; i < nodes.Count; i++)
        {
            if (node == null)
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
            float lenght = Vector3.Distance(start, end);

            ChannelSegment segment = new ChannelSegment();
            segment.start = start;
            segment.end = end;
            segment.direction = direction;
            segment.length = lenght;

            segments.Add(segment);
        }
    }

    void SpawnParticle()
    {
        if (nodes.Count < 2) return;

        Vector3 spawnPos = nodes[0].transform.position/* + new Vector3(0, 1, 0)*/;

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

        Vector3 target = segment.end;

        Vector3 direction = (target - particleData.position);
        float distance = direction.magnitude;

        if (distance > 0.001f)
            direction /= distance;

        particleData.velocity = Vector3.ClampMagnitude(particleData.velocity, maxSpeed);
        particleData.velocity += direction * forceStrength * deltaTime;

        particleData.velocity *= damping;

        particleData.position += particleData.velocity * deltaTime;

        if (distance < 0.5f)
            particleData.currentSegment++;
    }

    private void Update()
    {
        BuildSegments();

        float deltaTime = Time.deltaTime;

        spawnTimer += deltaTime;
        float interval = 1f / particlesPerSecond;

        while (spawnTimer >= interval && particles.Count < maxParticles)
        {
            spawnTimer -= interval;
            SpawnParticle();
        }

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            ParticleData particleData = particles[i];

            UpdateParticle(ref particleData, deltaTime);

            particleData.lifetime -= deltaTime;

            if (particleData.lifetime <= 0 || particleData.currentSegment >= segments.Count)
            {
                Destroy(particleObjects[i]);
                particleObjects.RemoveAt(i);
                particles.RemoveAt(i);
                continue;
            }

            particles[i] = particleData;

            particleObjects[i].transform.position = particleData.position;
        }
    }

    private void OnDrawGizmos()
    {
        BuildSegments();

        Gizmos.color = Color.cyan;

        foreach (var segment in segments)
        {
            Gizmos.DrawLine(segment.start, segment.end);
            Gizmos.DrawWireSphere(segment.start, radius);
            Gizmos.DrawWireSphere(segment.end, radius);
        }
    }

    public void CleanUp()
    {
        nodes.RemoveAll(item => item == null);
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

