using NUnit.Framework.Constraints;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshGeneration : MonoBehaviour
{
    private ChannelGuide guide;
    private MeshFilter meshFilter;
    private Mesh mesh;

    [SerializeField] float width = 2.5f;
    [SerializeField] float wallHeight = 1f;
    [SerializeField] float verticalOffset = 0.4f;
    [SerializeField] float innerInset = 0.5f; 

    void Awake()
    {
        guide = GetComponent<ChannelGuide>();
        meshFilter = GetComponent<MeshFilter>();

        mesh = new Mesh();
        mesh.name = "Channel Mesh";

        meshFilter.sharedMesh = mesh;
    }

    void Update()
    {
        BuildMesh();
    }

    public void BuildMesh()
    {
        if (guide.nodes.Count < 2)
        {
            mesh.Clear();
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < guide.nodes.Count; i++)
        {
            Vector3 pos = transform.InverseTransformPoint(guide.nodes[i].transform.position);
            Vector3 forward = GetForward(i);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            CreateSection(vertices, pos, right);
        }

        int crosssSectionVertices = 6;

        for (int i = 0; i < guide.nodes.Count - 1; i++)
        {
            int sectionA = i * crosssSectionVertices;
            int sectionB = (i + 1) * crosssSectionVertices;

            for (int j = 0; j < crosssSectionVertices - 1; j++)
            {
                int a = sectionA + j;
                int b = sectionA + j + 1;
                int c = sectionB + j;
                int d = sectionB + j + 1;

                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);

                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
    }

    void CreateSection(List<Vector3> vertices, Vector3 center, Vector3 right)
    {
        Vector3 up = Vector3.up;

        float inner = width * 0.5f * innerInset;

        vertices.Add(center + -right * width + up * (wallHeight + verticalOffset));
        vertices.Add(center + -right * width + up * verticalOffset);
        vertices.Add(center + -right * inner);
        vertices.Add(center + right * inner);
        vertices.Add(center + right * width + up * verticalOffset);
        vertices.Add(center + right * width + up * (wallHeight + verticalOffset));
    }

    Vector3 GetForward(int index)
    {
        if (index == 0)
            return (guide.nodes[1].transform.position - guide.nodes[0].transform.position).normalized;

        if (index == guide.nodes.Count - 1)
            return (guide.nodes[index].transform.position - guide.nodes[index - 1].transform.position).normalized;

        Vector3 prev = (guide.nodes[index].transform.position - guide.nodes[index - 1].transform.position).normalized;

        Vector3 next = (guide.nodes[index + 1].transform.position - guide.nodes[index].transform.position).normalized;

        return (prev + next).normalized;
    }
}
