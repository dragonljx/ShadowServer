using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Windows.Kinect;

public class TreatingData : MonoBehaviour
{
    private static TreatingData _instance;
    KinectSensor _Sensor;

    private int resolutionDepth;
    private ushort[] depthData;
    private int verticesNumber = 60000;
    private int verticesRemaining;
    private Vector3[] coordinatesAll;
    private Vector3[] temporaryCoordinate;
    private Mesh[] meshAll;
    private GameObject[] objAll;
    private MeshFilter[] filterAll;
    public Material mater;

    public float Z = 0;


    public static TreatingData Instance { get { return _instance; } }
    private void Start()
    {
        _instance = this;
        _Sensor = KinectSensor.GetDefault();
        if (!_Sensor.IsOpen)
        {
            _Sensor.Open();
        }
        resolutionDepth = 512 * 424;
        verticesRemaining = resolutionDepth % verticesNumber;

        temporaryCoordinate = new Vector3[resolutionDepth];
        meshAll = new Mesh[4];
        objAll = new GameObject[4];
        filterAll = new MeshFilter[4];
        for (int i = 0; i < 4; i++)
        {
            objAll[i] = new GameObject();
            objAll[i].name = i.ToString();
            filterAll[i] = objAll[i].AddComponent<MeshFilter>();
            objAll[i].AddComponent<MeshRenderer>().material = mater;
            meshAll[i] = new Mesh();
        }


    }

    
    public static string savePath = "点云数据.data";
    private void Update()
    {
        //用来保存点云数据
        if (Input.GetKeyDown(KeyCode.S))
        {
            FileStream fs = new FileStream(savePath, FileMode.OpenOrCreate);
            BinaryWriter binWriter = new BinaryWriter(fs);
            byte[] by = new byte[coordinatesAll.Length * 12];
            for (int i = 0; i < coordinatesAll.Length; i++)
            {

                    //vertices[beginIndex + i]
                    //点的xyz数据
                    byte[] ve3 = new byte[3 * sizeof(float)];
                    //存入xyz
                    Buffer.BlockCopy(BitConverter.GetBytes(coordinatesAll[i].x),
                        0, ve3, 0 * sizeof(float), sizeof(float));

                    Buffer.BlockCopy(BitConverter.GetBytes(coordinatesAll[i].y),
                        0, ve3, 1 * sizeof(float), sizeof(float));

                    Buffer.BlockCopy(BitConverter.GetBytes(coordinatesAll[i].z),
                        0, ve3, 2 * sizeof(float), sizeof(float));

                    //将保存的坐标数据存入
                    ve3.CopyTo(by, i *12);
            }

            binWriter.Write(by, 0, by.Length);

            binWriter.Close();
            fs.Close();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            byte[] point;

            FileStream fs = new FileStream(savePath, FileMode.Open);
            BinaryReader binReader = new BinaryReader(fs);

            point = new byte[fs.Length];
            binReader.Read(point, 0, (int)fs.Length);

            binReader.Close();
            fs.Close();

            int verticesLength = point.Length / 12;

           Vector3[]vector = new Vector3[verticesLength];

            int index = 0;
            for (int i = 0; i < verticesLength; i++)
            {
                Vector3 v = Vector3.zero;
                v.x = BitConverter.ToSingle(point, index++ * 4);
                v.y = BitConverter.ToSingle(point, index++ * 4);
                v.z = BitConverter.ToSingle(point, index++ * 4);

                vector[i] = v;
            }
            TransCoordinate(vector);

        }
    }
    public void TransCoordinate(Vector3[] depth)
    {

        if (depth.Length < verticesNumber)
        {
            CloudReverseNow(meshAll[0], depth, 0, depth.Length, 0);
        }
        else
        {
            int num = depth.Length / verticesNumber;
            int remaining = depth.Length % verticesNumber;
            for (int i = 0; i <= num; i++)
            {
                if (i == num)
                {
                    CloudReverseNow(meshAll[i], depth, i * verticesNumber, remaining, i);
                    break;
                }
                CloudReverseNow(meshAll[i], depth, i * verticesNumber, verticesNumber, i);
            }
        }
    }
    /// <summary>
    /// 深度图坐标转换
    /// </summary>
    public void TransCoordinate(ushort[] depth)
    {
        if (depth == null)
        {
            return;
        }

        CameraSpacePoint[] camSpace = new CameraSpacePoint[depth.Length];
        _Sensor.CoordinateMapper.MapDepthFrameToCameraSpace(depth, camSpace);

        //计数器 计算有多少有效存入的坐标
        int count = 0;
        //修改动态添加mesh
        for (int i = 0; i < resolutionDepth; i++)
        {
            if (float.IsInfinity(camSpace[i].X) || float.IsInfinity(camSpace[i].Y) || float.IsInfinity(camSpace[i].Z) || i % 2 == 0)
            {
                //temporaryCoordinate[i] = Vector3.zero;
                continue;
            }
            if (camSpace[i].Z >Z)
            {
                //temporaryCoordinate[i] = Vector3.zero;
                continue;
            }
            count++;
            temporaryCoordinate[count].x = -camSpace[i].X;
            temporaryCoordinate[count].y = camSpace[i].Y;
            temporaryCoordinate[count].z = camSpace[i].Z;
        }
        //初始化坐标
        coordinatesAll = new Vector3[count];
        //将有效坐标存入
        Array.Copy(temporaryCoordinate, 0, coordinatesAll, 0, count);

        if (count < verticesNumber)
        {
            CloudReverse(meshAll[0], coordinatesAll, 0, count, 0);
        }
        else
        {
            int num = count / verticesNumber;
            int remaining = count % verticesNumber;
            for (int i = 0; i <= num; i++)
            {
                if (i == num)
                {
                    CloudReverse(meshAll[i], coordinatesAll, i * verticesNumber, remaining, i);
                    break;
                }
                CloudReverse(meshAll[i], coordinatesAll, i * verticesNumber, verticesNumber, i);
            }
        }
    }
    /// <summary>
    /// 点云模型生成
    /// </summary>
    public void CloudReverse(Mesh mesh, Vector3[] vertices, int beginIndex, int pointsNum,int meshNum)
    {

        //异步执行以下内容
        Loom.RunAsync(() =>
        {
            Vector3[] points = new Vector3[pointsNum];
            int[] indecies = new int[pointsNum];
            for (int i = 0; i < pointsNum; i++)
            {
                points[i] = vertices[beginIndex + i];
                indecies[i] = i;
            }
            //调用loom在update中执行下列方法，达到主线程执行的功能
            Loom.QueueOnMainThread(() =>
            {
                mesh.Clear();
                mesh.vertices = points;
                mesh.SetIndices(indecies, MeshTopology.Points, 0);
                filterAll[meshNum].mesh = mesh;
            });
        });

    }
    /// <summary>
    /// 修改读取点云数据使用
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="vertices"></param>
    /// <param name="beginIndex"></param>
    /// <param name="pointsNum"></param>
    /// <param name="meshNum"></param>
    public void CloudReverseNow(Mesh mesh, Vector3[] vertices, int beginIndex, int pointsNum, int meshNum)
    {

        Vector3[] points = new Vector3[pointsNum];
        int[] indecies = new int[pointsNum];
        for (int i = 0; i < pointsNum; i++)
        {
            points[i] = vertices[beginIndex + i];
            indecies[i] = i;
        }

        mesh.Clear();
        mesh.vertices = points;
        mesh.SetIndices(indecies, MeshTopology.Points, 0);
        filterAll[meshNum].mesh = mesh;

    }
    public void SetUpServer(string ip, int port)
    {
        ReceiveCallBack back = delegate (ushort[] data)
        {
            TransCoordinate(data);
            Debug.Log("回调");
        };

        Server.Instance.ServerInit(back, ip, port);
    }

}