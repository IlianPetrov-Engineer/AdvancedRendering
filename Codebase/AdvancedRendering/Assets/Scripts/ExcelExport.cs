using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class ExcelExport : MonoBehaviour
{
    [SerializeField] private string testLabel = "";

    [Tooltip("Which run number this is (1-10). Changes the output filename.")]
    [SerializeField] private int runNumber = 1;

    [Tooltip("How many frames to record before stopping and writing the CSV.")]
    [SerializeField] private int targetFrames = 1000;

    [Header("References")]
    [SerializeField] private ChannelGuide guide;

    private List<BenchmarkFrame> frames = new List<BenchmarkFrame>();
    private bool finished = false;

    private void Update()
    {
        if (finished) return;

        BenchmarkFrame frameData = new BenchmarkFrame
        {
            frame = Time.frameCount,
            deltaTime = Time.deltaTime,
            particleCount = guide.particles.Count,
            channelCollisions = guide.channelCollisionsThisFrame,
            sphereCollisions = guide.sphereCollisionsThisFrame
        };

        frames.Add(frameData);

        if (frames.Count >= targetFrames)
        {
            WriteCSV();
            finished = true;
            Debug.Log($"[ExcelExport] Done — {frames.Count} frames written.");
        }
    }

    void WriteCSV()
    {
        string path = Path.Combine(Application.dataPath,$"{testLabel}_run{runNumber}.csv");

        using (StreamWriter tw = new StreamWriter(path, false))
        {
            tw.WriteLine("Frame,DeltaTime,ParticleCount,ChannelCollisions,SphereCollisions");

            foreach (BenchmarkFrame f in frames)
            {
                tw.WriteLine(
                    $"{f.frame}," +
                    $"{f.deltaTime.ToString("F6", CultureInfo.InvariantCulture)}," +
                    $"{f.particleCount}," +
                    $"{f.channelCollisions}," +
                    $"{f.sphereCollisions}"
                );
            }
        }
    }
}

[System.Serializable]
public struct BenchmarkFrame
{
    public int frame;
    public float deltaTime;
    public int particleCount;
    public int channelCollisions;
    public int sphereCollisions;
}
