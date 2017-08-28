using System;
using UnityEngine;
using Windows.Kinect;

public class TreatingData : MonoBehaviour
{
    private static TreatingData _instance;
    KinectSensor _Sensor;

    private int resolutionDepth;
    private ushort[] depthData;
    private int verticesNumber = 60000;
    /// <summary>
    /// 所有坐标
    /// </summary>
    private Vector3[] coordinatesAll;
    /// <summary>
    /// 暂存坐标
    /// </summary>
    private Vector3[] temporaryCoordinate;
    private Mesh[] meshAll;
    private GameObject[] objAll;
    private MeshFilter[] filterAll;
    public Material mater;

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
        //初始化暂存坐标
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

        //for (int i = 0; i < resolutionDepth; i++)
        //{
        //    if (float.IsInfinity(camSpace[i].X) || float.IsInfinity(camSpace[i].Y) || float.IsInfinity(camSpace[i].Z))
        //    {
        //        coordinatesAll[i] = Vector3.zero;
        //        continue;
        //    }

        //    coordinatesAll[i].x = -camSpace[i].X;
        //    coordinatesAll[i].y = camSpace[i].Y;
        //    coordinatesAll[i].z = camSpace[i].Z;
        //}
        //计数器 计算有多少有效存入的坐标
        int count = 0;
        //修改动态添加mesh
        for (int i = 0; i < resolutionDepth; i++)
        {

            if (float.IsInfinity(camSpace[i].X) || float.IsInfinity(camSpace[i].Y) || float.IsInfinity(camSpace[i].Z) || i % 2 == 0)
            {
                //coordinatesAll[i] = Vector3.zero;
                continue;
            }

            temporaryCoordinate[i].x = -camSpace[i].X;
            temporaryCoordinate[i].y = camSpace[i].Y;
            temporaryCoordinate[i].z = camSpace[i].Z;
            count++;
        }
        //初始化坐标
        coordinatesAll = new Vector3[count];
        //将有效坐标存入
        //Buffer.BlockCopy(temporaryCoordinate, 0, coordinatesAll, 0, count-1);
        Array.Copy(temporaryCoordinate, 0, coordinatesAll, 0,count);
        if (count < verticesNumber)
        {
            CloudReverse(meshAll[0], coordinatesAll, 0, count, 0);
        }
        else
        {
            int num = count / verticesNumber;
            int remaining = count % verticesNumber;
            for (int i = 0; i < num; i++)
            {
                if (i == num - 1)
                {
                    CloudReverse(meshAll[i], coordinatesAll, i * verticesNumber, remaining, i);
                    //filterAll[i].mesh = meshAll[i];
                    break;
                }
                CloudReverse(meshAll[i], coordinatesAll, i * verticesNumber, verticesNumber, i);
                //filterAll[i].mesh = meshAll[i];

            }
        }



    }


    int[] i0;
    Vector3[] v0;
    private void Update()
    {
        filterAll[0].mesh.Clear();
        filterAll[0].mesh.vertices = v0;
        //filterAll[0].mesh.SetIndices(i0, MeshTopology.Triangles, 0);
        filterAll[0].mesh.triangles = i0;
        //filterAll[0].mesh = mesh;
    }
    /// <summary>
    /// 点云模型生成
    /// </summary>
    public void CloudReverse(Mesh mesh, Vector3[] vertices, int beginIndex, int pointsNum, int meshNum)
    {
        //异步执行以下内容
        //Loom.RunAsync(() => {
        Vector3[] points = new Vector3[pointsNum];
        int[] indecies = new int[pointsNum];

        int len = vertices.Length;
        int f = 0, b = len - 1;
        int[] triangles = new int[vertices.Length * 3];

        for (int i = 0; i < pointsNum; i++)
        {
            points[i] = vertices[beginIndex + i];
            //indecies[i] = i;
            if (i % 2 == 1)
            {
                triangles[3 * i - 3] = f++;
                triangles[3 * i - 2] = f;
                triangles[3 * i - 1] = b;


            }
            else
            {
                triangles[3 * i - 1] = b--;
                triangles[3 * i - 2] = b;
                triangles[3 * i - 3] = f;


            }

        }

        i0 = triangles;
        v0 = points;

        //调用loom在update中执行下列方法，达到主线程执行的功能
        //Loom.QueueOnMainThread(() => {
        //mesh.Clear();
        //mesh.vertices = points;
        //mesh.SetIndices(triangles, MeshTopology.Triangles, 0);

        //filterAll[meshNum].mesh = mesh;
        //});
        //});


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