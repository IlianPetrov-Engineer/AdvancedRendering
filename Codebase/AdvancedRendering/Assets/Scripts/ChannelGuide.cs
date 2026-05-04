using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChannelGuide : MonoBehaviour
{
    [SerializeField] GameObject node;
    public List<GameObject> nodes = new List<GameObject>();
    public List<ChannelSegment> segments = new List<ChannelSegment>();
    [SerializeField] float radius = 1f;

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

    private void Update()
    {
        BuildSegments();
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
}

