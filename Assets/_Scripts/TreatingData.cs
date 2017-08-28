using System;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

public class TreatingData : MonoBehaviour
{
    private static TreatingData _instance;
    private KinectSensor _Sensor;

    private int resolutionDepth;
    private int verticesNumber = 60000;

    /// <summary>
    /// 所有顶点坐标
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

    /// <summary>
    /// 三角剖分
    /// </summary>
    private DelaunayTriangulation delaunayTriangulation;

    private Rect rect;

    public static TreatingData Instance { get { return _instance; } }

    private void Start()
    {
        _instance = this;
        _Sensor = KinectSensor.GetDefault();
        delaunayTriangulation = new DelaunayTriangulation();

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
            count++;
            temporaryCoordinate[count].x = -camSpace[i].X;
            temporaryCoordinate[count].y = camSpace[i].Y;
            temporaryCoordinate[count].z = camSpace[i].Z;
        }
        //初始化坐标
        coordinatesAll = new Vector3[count];
        //将有效坐标存入
        Array.Copy(temporaryCoordinate, 0, coordinatesAll, 0, 10);

        #region 计算出点云的最大边界

        float minX = 0;
        float maxX = 0;
        float minY = 0;
        float maxY = 0;

        for (int j = 0; j < coordinatesAll.Length; j++)
        {
            if (coordinatesAll[j].x < minX)
            {
                minX = coordinatesAll[j].x;
            }
            if (coordinatesAll[j].x > maxX)
            {
                maxX = coordinatesAll[j].x;
            }
            if (coordinatesAll[j].y < minY)
            {
                minY = coordinatesAll[j].y;
            }
            if (coordinatesAll[j].x > maxY)
            {
                maxY = coordinatesAll[j].y;
            }
        }
        //Debug.Log("X最大值：" + maxX + "X最小值：" + minX + "Y最大值：" + maxY + "Y最小值：" + maxY);
        rect = new Rect(minX, minY, Mathf.Abs(minX) + Mathf.Abs(maxX), Mathf.Abs(minY) + Mathf.Abs(maxY));

        #endregion 计算出点云的最大边界

        #region 点云初始化

        delaunayTriangulation.Setup(rect);
        for (int i = 0; i < coordinatesAll.Length; i++)
        {
            delaunayTriangulation.Add(coordinatesAll[i]);
        }

        #endregion 点云初始化

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
    public void CloudReverse(Mesh mesh, Vector3[] vertices, int beginIndex, int pointsNum, int meshNum)
    {
        //异步执行以下内容
        Loom.RunAsync(() =>
        {
           
            List<DelaunayTriangulation.Triangle> triangles = delaunayTriangulation.GetTriangles();//获取三角形
            List<Vector3> t_vertices = GetVertices();//获取顶点

            List<int> tris = new List<int>();
            for (var ti = 0; ti < triangles.Count; ti++)
            {
                DelaunayTriangulation.Triangle tri = triangles[ti];
                int[] triIndexs = tri.GetTriIndexs();
                tris.AddRange(triIndexs);
            }

            //调用loom在update中执行下列方法，达到主线程执行的功能
            Loom.QueueOnMainThread(() =>
            {
                mesh.Clear();
                mesh.vertices = t_vertices.ToArray();
                mesh.triangles = tris.ToArray();

                mesh.RecalculateNormals();
                filterAll[meshNum].mesh = mesh;
            });
        });
    }

    /// <summary>
    /// 获取所有顶点 包括超级三角形 与矩形点
    /// </summary>
    /// <returns></returns>
    private List<Vector3> GetVertices()
    {
        List<Vector3> t_vertices = new List<Vector3>();
        t_vertices.AddRange(delaunayTriangulation.GetSuperVertices());
        t_vertices.AddRange(delaunayTriangulation.GetRectVertices());
        t_vertices.AddRange(coordinatesAll);

        return t_vertices;
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