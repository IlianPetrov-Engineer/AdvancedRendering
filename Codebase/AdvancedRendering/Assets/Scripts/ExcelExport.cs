using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.ParticleSystem;

public class ExcelExport : MonoBehaviour
{
    string fileName = "";
    public List<int> testData = new List<int>();

    private List<BenchmarkFrame> frames = new List<BenchmarkFrame>();

    [SerializeField] private ChannelGuide guide;

    private int channelCollisionCount;

    private int particleCollisionCount;

    void Start()
    {
        fileName = Application.dataPath + "/test.csv";
    }

    private void Update()
    {
        if (testData.Count < 10)
        {
            for (int i = 0; i < 10; i++)
                testData.Add(i);
        }

        if (testData.Count == 10)
            WriteCSV();

        //BenchmarkFrame frameData = new BenchmarkFrame
        //{
        //    frame = Time.frameCount,
        //    deltaTime = Time.deltaTime,
        //    fps = 1f / Time.deltaTime,
        //    particleCount = particles.Count,
        //    channelCollisions = channelCollisionCount,
        //    particleCollisions = particleCollisionCount
        //};

        //frames.Add(frameData);
    }

    void WriteCSV()
    {
        if (testData.Count <= 0)
            return;

        TextWriter tw = new StreamWriter(fileName, false);

        tw.WriteLine("Test number");
        tw.Close();

        tw = new StreamWriter(fileName, true);

        for (int i = 0; i < testData.Count; i++)
        {
            tw.WriteLine(testData[i]);
        }

        tw.Close();

        //TextWriter tw =
        //new StreamWriter(fileName, false);

        //tw.WriteLine(
        //    "Frame,DeltaTime,FPS,ParticleCount,ChannelCollisions,ParticleCollisions"
        //);

        //for (int i = 0; i < frames.Count; i++)
        //{
        //    BenchmarkFrame f = frames[i];

        //    tw.WriteLine(
        //        $"{f.frame}," +
        //        $"{f.deltaTime}," +
        //        $"{f.fps}," +
        //        $"{f.particleCount}," +
        //        $"{f.channelCollisions}," +
        //        $"{f.particleCollisions}"
        //    );
        //}

        //tw.Close();
    }
}

[System.Serializable]
public struct BenchmarkFrame
{
    public int frame;
    public float deltaTime;
    public float fps;
    public int particleCount;
    public int channelCollisions;
    public int particleCollisions;
}
