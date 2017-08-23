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
        verticesRemaining = resolutionDepth % verticesNumber;

        coordinatesAll = new Vector3[resolutionDepth];
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

        for (int i = 0; i < resolutionDepth; i++)
        {
            if (float.IsInfinity(camSpace[i].X) || float.IsInfinity(camSpace[i].Y) || float.IsInfinity(camSpace[i].Z))
            {
                coordinatesAll[i] = Vector3.zero;
                continue;
            }

            coordinatesAll[i].x = -camSpace[i].X;
            coordinatesAll[i].y = camSpace[i].Y;
            coordinatesAll[i].z = camSpace[i].Z;
        }
        for (int i = 0; i < 4; i++)
        {
            if (i == 4 - 1)
            {
                CloudReverse(meshAll[i], coordinatesAll, i * verticesNumber, verticesRemaining,i);
                //filterAll[i].mesh = meshAll[i];
                break;
            }
            CloudReverse(meshAll[i], coordinatesAll, i * verticesNumber, verticesNumber,i);
            //filterAll[i].mesh = meshAll[i];

        }
    }
    /// <summary>
    /// 点云模型生成
    /// </summary>
    public void CloudReverse(Mesh mesh, Vector3[] vertices, int beginIndex, int pointsNum,int meshNum)
    {
        //异步执行以下内容
        Loom.RunAsync(() => {
        Vector3[] points = new Vector3[pointsNum];
        int[] indecies = new int[pointsNum];

            int len = vertices.Length;
            int triangleNum = len - 2;
            int[] triangles = new int[triangleNum * 3];

            for (int i = 0; i < triangleNum; i++)
            {
                points[i] = vertices[beginIndex + i];
                //indecies[i] = i;
                int start = i * 3;
                triangles[start] = 0;
                triangles[start + 1] = i + 1;
                triangles[start + 2] = i + 2;

            }
            //调用loom在update中执行下列方法，达到主线程执行的功能
            Loom.QueueOnMainThread(() => {
                mesh.Clear();
                mesh.vertices = points;
                mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
                
                filterAll[meshNum].mesh = mesh;
            });
        });


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