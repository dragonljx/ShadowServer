
using System.Collections.Generic;
using UnityEngine;

public class DelaunayTriangulation
{
    #region 工具

    private static float sq(float v)
    {
        return v * v;
    }

    #endregion 工具

    private List<Triangle> triangles;
    private List<Vector3> rectVertices;

    /// <summary>
    /// 所有顶点
    /// </summary>
    public List<Vector3> vertices;

    /// <summary>
    /// 超级三角形 包含了所有点云的点
    /// </summary>
    private List<Vector3> superVertices;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DelaunayTriangulation()
    {
        superVertices = new List<Vector3>();
        vertices = new List<Vector3>();
        triangles = new List<Triangle>();

        rectVertices = new List<Vector3>();
    }

    public void Setup(Rect rect,Vector3[] ve)
    {
        Clear();

        Vector3 center = rect.center;
        float width = rect.width;
        float height = rect.height;

        float radius = Mathf.Sqrt((width * width) + (height * height)) / 2.0f * 1.25f;

        //最大外切三角形
        Vector3 v1 = new Vector3(center.x - Mathf.Sqrt(3) * radius, center.y - radius);
        Vector3 v2 = new Vector3(center.x + Mathf.Sqrt(3) * radius, center.y - radius);
        Vector3 v3 = new Vector3(center.x, center.y + 2.0f * radius);

        superVertices.Add(v1);
        superVertices.Add(v2);
        superVertices.Add(v3);

        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);

        Triangle t = new Triangle(this, 0, 1, 2);
        triangles.Add(t);
        int count = ve.Length;
        // Area rect
        rectVertices.Add(ve[0]);
        rectVertices.Add(ve[count / 2]);
        rectVertices.Add(ve[count-1]);
        //rectVertices.Add((new Vector3(rect.xMin, rect.yMax)));
        
        Add(ve[0]);
        Add(ve[count/2]);
        Add(ve[count-1]);
        //Add(rectVertices[3]);
    }

    void Clear()
    {
        superVertices.Clear();
        vertices.Clear();
        triangles.Clear();
        rectVertices.Clear();
    }

    public class Triangle
    {
        private Vector3 center;//本三角形的外切圆中心点
        private float sqrRadius;//本三角形的半径

        private DelaunayTriangulation dt;

        int[] triIndexs;

        public Triangle(DelaunayTriangulation dt_, int id1, int id2, int id3)
        {
            try
            {
                dt = dt_;
                triIndexs = new int[3] { id1, id2, id3 };
                List<Vector3> vertices = dt.vertices;
                Vector3 v1 = vertices[id1];
                Vector3 v2 = vertices[id2];
                Vector3 v3 = vertices[id3];
                //三角形的外切圆中心点
                float c = 2.0f * ((v2.x - v1.x) * (v3.y - v1.y) - (v2.y - v1.y) * (v3.x - v1.x));
                float x = ((v3.y - v1.y) * (sq(v2.x) - sq(v1.x) + sq(v2.y) - sq(v1.y)) + (v1.y - v2.y) * (sq(v3.x) - sq(v1.x) + sq(v3.y) - sq(v1.y))) / c;
                float y = ((v1.x - v3.x) * (sq(v2.x) - sq(v1.x) + sq(v2.y) - sq(v1.y)) + (v2.x - v1.x) * (sq(v3.x) - sq(v1.x) + sq(v3.y) - sq(v1.y))) / c;
                Vector3 center = new Vector3(x, y, 0);
                v1.z = 0;
                center.z = 0;
                //三角形外切圆的半径
                float radiuqSqr = Vector3.SqrMagnitude(v1 - center);

                this.center = center;
                this.sqrRadius = radiuqSqr;
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
                throw;
            }
          
        }

        /// <summary>
        /// 划分三角形的顺序
        /// </summary>
        /// <param name="newIndex"></param>
        /// <returns></returns>
        public List<Triangle> Divide(int newIndex)
        {
            try
            {
                List<Triangle> tris = new List<Triangle>();
                for (int i = 0; i < 3; i++)
                {
                    int j = (i == 2) ? 0 : i + 1;
                    Triangle tri = new Triangle(dt, triIndexs[i], triIndexs[j], newIndex);
                    tris.Add(tri);
                    Debug.Log(j);

                }
                return tris;
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
                throw;
            }

        }

        /// <summary>
        /// 判断这个点是否在本三角形外切圆之内
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool IsInCircle(Vector3 v)
        {
            v.z = 0;
            center.z = 0;
            return Vector3.SqrMagnitude(v - center) < sqrRadius;
        }

        /// <summary>
        /// 传入的点是否与本三角形共有点
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsContain(int index)
        {
            for (int i = 0; i < 3; i++)
            {
                if (triIndexs[i] == index)
                {
                    return true;
                }
            }
            return false;
        }
        public int[] GetTriIndexs()
        {
            return triIndexs;
        }
    }

    /// <summary>
    /// 添加点
    /// </summary>
    /// <param name="v"></param>
    public void Add(Vector3 v)
    {
        int vIndex = this.vertices.Count;

        // addvertex 添加顶点
        this.vertices.Add(v);

        List<Triangle> nextTriangles = new List<Triangle>();
        List<Triangle> newTriangles = new List<Triangle>();//新增加的三角
        //从总三角形中取出三角形，来与每一个点进行匹配，如果该点在当前三角形外切圆内则与三角形的三个点连线
        //生成新的三角形，如果该点不在三角形之内则将三角形添加入nextTriangles
        for (int ti = 0; ti < this.triangles.Count; ti++)
        {
            Triangle tri = this.triangles[ti];
            if (tri.IsInCircle(v))
            {
                newTriangles.AddRange(tri.Divide(vIndex));
            }
            else
            {
                nextTriangles.Add(tri);
            }
        }

        //遍历新生成的三角形，判断他们是否与存储的三角形顶点是否是一个，如果拥有同一个点
        //则计算该点是否在三角形外切圆内，如果在的话则抛弃该三角形
        for (int ti = 0; ti < newTriangles.Count; ti++)
        {
            Triangle tri = newTriangles[ti];
            bool isIllegal = false;

            for (int vi = 0; vi < this.vertices.Count; vi++)
            {
                if (this.IsIllegalTriangle(tri, vi))
                {
                    isIllegal = true;//如果tri与vi有公共点则抛弃该三角形
                    break;
                }
            }
            if (!isIllegal)
            {
                nextTriangles.Add(tri);
            }
        }
        
        this.triangles = nextTriangles;//重新更新三角形
    }

    /// <summary>
    /// 判断三角形和点是否有公共点
    /// </summary>
    /// <param name="t"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool IsIllegalTriangle(Triangle t, int index)
    {
        if (t.IsContain(index))
        {
            return false;
        }
        Vector3 v = vertices[index];
        return t.IsInCircle(v);//如果传入的点与当前三角形没有公共点，计算该点是否在三角形外切圆之内。
    }

    /// <summary>
    /// 获取三角形
    /// </summary>
    /// <returns></returns>
    public List<Triangle> GetTriangles()
    {

        List<Triangle> ts = new List<Triangle>();
        for (int ti = 0; ti < this.triangles.Count; ti++)
        {
            Triangle t = triangles[ti];
            bool hasSuperVertex = false;
            for (int vi = 0; vi < 3; vi++)
            {
                if (t.IsContain(vi))
                {
                    hasSuperVertex = true;
                }
            }
            if (!hasSuperVertex)
            {
                ts.Add(t);
            }
        }
        return ts;
    }



    public List<Vector3> GetVertices()
    {
        return this.vertices;
    }
    public List<Vector3> GetSuperVertices()
    {
        return this.superVertices;
    }
    public List<Vector3> GetRectVertices()
    {
        return this.rectVertices;
    }
}