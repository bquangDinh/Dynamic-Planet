using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MeshDeformingEvent : UnityEvent<int, int>
{

}

public class Cell
{
    public ControlNode topLeft, topRight, bottomRight, bottomLeft;
    public Node centreTop, centreRight, centreBottom, centreLeft;
    public int configuration;

    public Cell(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft)
    {
        this.topLeft = topLeft;
        this.topRight = topRight;
        this.bottomRight = bottomRight;
        this.bottomLeft = bottomLeft;

        centreTop = topLeft.right;
        centreRight = bottomRight.above;
        centreBottom = bottomLeft.right;
        centreLeft = bottomLeft.above;

        if (topLeft.active)
        {
            configuration += 8;
        }

        if (topRight.active)
        {
            configuration += 4;
        }

        if (bottomRight.active)
        {
            configuration += 2;
        }

        if (bottomLeft.active)
        {
            configuration += 1;
        }
    }
}

public class CellGrid
{
    public Cell[,] cells;

    public float MapHeight { get; private set; }

    public float MapWidth { get; private set; }

    public CellGrid(int[,] map, float cellSize, float originX, float originY)
    {
        Debug.Log("OriX: " + originX + ", OriY:" + originY);

        int nodeCountX = map.GetLength(0);
        int nodeCountY = map.GetLength(1);

        MapWidth = nodeCountX * cellSize;
        MapHeight = nodeCountY * cellSize;

        Debug.Log("W: " + MapWidth + ", H: " + MapHeight);

        ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

        for (int x = 0; x < nodeCountX; ++x)
        {
            for (int y = 0; y < nodeCountY; ++y)
            {
                Vector3 pos = new Vector3(-MapWidth / 2 + x * cellSize + originX - cellSize / 2, -MapHeight / 2 + y * cellSize + originY - cellSize / 2);
                controlNodes[x, y] = new ControlNode(pos, map[x, y], cellSize);
            }
        }

        cells = new Cell[nodeCountX - 1, nodeCountY - 1];

        for (int x = 0; x < nodeCountX - 1; ++x)
        {
            for (int y = 0; y < nodeCountY - 1; ++y)
            {
                cells[x, y] = new Cell(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
            }
        }
    }
}

public class Node
{
    public Vector3 position;
    public int vertexIndex = -1;

    public Node(Vector3 position)
    {
        this.position = position;
    }
}

public class ControlNode : Node
{
    public bool active;
    public int isoValue;
    public Vector3 pos;

    public Node above, right;

    public ControlNode(Vector3 position, int isoValue, float cellSize) : base(position)
    {
        active = isoValue >= 0;
        this.isoValue = Mathf.Abs(isoValue);
        pos = position;

        above = new Node(position + Vector3.up * cellSize / 2f);
        right = new Node(position + Vector3.right * cellSize / 2f);
    }
}

public class MeshGenerator : MonoBehaviour
{
    public CellGrid cellGrid;

    private List<Vector3> vertices;

    private List<int> triangles;

    public MeshDeformingEvent meshDeformingEvent;

    public PolygonCollider2D colliderPrefab;

    public bool isMeshReady = false;

    public bool isCollisionReady = false;

    private void Start()
    {
    }

    public void GenerateMesh(int[,] map, float cellSize, float originX = 0, float originY = 0)
    {
        if (isMeshReady) return;

        cellGrid = new CellGrid(map, cellSize, originX, originY);

        if (vertices == null)
        {
            vertices = new List<Vector3>();
        } else
        {
            vertices.Clear();
        }
        

        if (triangles == null)
        {
            triangles = new List<int>();
        } else
        {
            triangles.Clear();
        }

        for (int x = 0; x < cellGrid.cells.GetLength(0); ++x)
        {
            for (int y = 0; y < cellGrid.cells.GetLength(1); ++y)
            {
                TriangulateCell(cellGrid.cells[x, y]);
            }
        }

        Mesh mesh = new Mesh();

        GetComponent<MeshFilter>().mesh.Clear();

        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        isMeshReady = true;
        isCollisionReady = true;

        // Update collision
        BoxCollider box = GetComponent<BoxCollider>();
        box.size = new Vector3(cellGrid.cells.GetLength(0) * cellSize, cellGrid.cells.GetLength(1) * cellSize);
        box.center = new Vector3(originX, originY);
    }

    public void DestroyMesh ()
    {
        GetComponent<MeshFilter>().mesh.Clear();

        isMeshReady = false;
    }

    void TriangulateCell(Cell cell)
    {
        switch (cell.configuration)
        {
            // no point selected
            case 0:
                break;

            // 1 point selected
            case 1:
                MeshFromPoints(cell.centreBottom, cell.bottomLeft, cell.centreLeft);
                AttachPolygonCollider(cell.centreBottom, cell.bottomLeft, cell.centreLeft);
                break;
            case 2:
                MeshFromPoints(cell.centreRight, cell.bottomRight, cell.centreBottom);
                AttachPolygonCollider(cell.centreRight, cell.bottomRight, cell.centreBottom);
                break;
            case 4:
                MeshFromPoints(cell.centreTop, cell.topRight, cell.centreRight);
                AttachPolygonCollider(cell.centreTop, cell.topRight, cell.centreRight);
                break;
            case 8:
                MeshFromPoints(cell.topLeft, cell.centreTop, cell.centreLeft);
                AttachPolygonCollider(cell.topLeft, cell.centreTop, cell.centreLeft);
                break;

            // 2 points selected
            case 3:
                MeshFromPoints(cell.centreRight, cell.bottomRight, cell.bottomLeft, cell.centreLeft);
                AttachPolygonCollider(cell.centreRight, cell.bottomRight, cell.bottomLeft, cell.centreLeft);
                break;
            case 6:
                MeshFromPoints(cell.centreTop, cell.topRight, cell.bottomRight, cell.centreBottom);
                AttachPolygonCollider(cell.centreTop, cell.topRight, cell.bottomRight, cell.centreBottom);
                break;
            case 9:
                MeshFromPoints(cell.topLeft, cell.centreTop, cell.centreBottom, cell.bottomLeft);
                AttachPolygonCollider(cell.topLeft, cell.centreTop, cell.centreBottom, cell.bottomLeft);
                break;
            case 12:
                MeshFromPoints(cell.topLeft, cell.topRight, cell.centreRight, cell.centreLeft);
                AttachPolygonCollider(cell.topLeft, cell.topRight, cell.centreRight, cell.centreLeft);
                break;
            case 5:
                MeshFromPoints(cell.centreTop, cell.topRight, cell.centreRight, cell.centreBottom, cell.bottomLeft, cell.centreLeft);
                AttachPolygonCollider(cell.centreTop, cell.topRight, cell.centreRight, cell.centreBottom, cell.bottomLeft, cell.centreLeft);
                break;
            case 10:
                MeshFromPoints(cell.topLeft, cell.centreTop, cell.centreRight, cell.bottomRight, cell.centreBottom, cell.centreLeft);
                AttachPolygonCollider(cell.topLeft, cell.centreTop, cell.centreRight, cell.bottomRight, cell.centreBottom, cell.centreLeft);
                break;

            // 3 points selected
            case 7:
                MeshFromPoints(cell.centreTop, cell.topRight, cell.bottomRight, cell.bottomLeft, cell.centreLeft);
                AttachPolygonCollider(cell.centreTop, cell.topRight, cell.bottomRight, cell.bottomLeft, cell.centreLeft);
                break;
            case 11:
                MeshFromPoints(cell.topLeft, cell.centreTop, cell.centreRight, cell.bottomRight, cell.bottomLeft);
                AttachPolygonCollider(cell.topLeft, cell.centreTop, cell.centreRight, cell.bottomRight, cell.bottomLeft);
                break;
            case 13:
                MeshFromPoints(cell.topLeft, cell.topRight, cell.centreRight, cell.centreBottom, cell.bottomLeft);
                AttachPolygonCollider(cell.topLeft, cell.topRight, cell.centreRight, cell.centreBottom, cell.bottomLeft);
                break;
            case 14:
                MeshFromPoints(cell.topLeft, cell.topRight, cell.bottomRight, cell.centreBottom, cell.centreLeft);
                AttachPolygonCollider(cell.topLeft, cell.topRight, cell.bottomRight, cell.centreBottom, cell.centreLeft);
                break;

            // 4 points selected
            case 15:
                MeshFromPoints(cell.topLeft, cell.topRight, cell.bottomRight, cell.bottomLeft);
                break;
        }
    }

    void MeshFromPoints(params Node[] nodes)
    {
        AssignVertices(nodes);

        if (nodes.Length >= 3)
        {
            CreateTriangle(nodes[0], nodes[1], nodes[2]);
        }

        if (nodes.Length >= 4)
        {
            CreateTriangle(nodes[0], nodes[2], nodes[3]);
        }

        if (nodes.Length >= 5)
        {
            CreateTriangle(nodes[0], nodes[3], nodes[4]);
        }

        if (nodes.Length >= 6)
        {
            CreateTriangle(nodes[0], nodes[4], nodes[5]);
        }
    }

    void AttachPolygonCollider (params Node[] nodes)
    {
        if (isMeshReady) return;

        PolygonCollider2D collider = Instantiate(colliderPrefab);

        Vector2[] points = new Vector2[nodes.GetLength(0)];

        for (int i = 0; i < nodes.GetLength(0); ++i)
        {
            points[i] = new Vector2(nodes[i].position.x, nodes[i].position.y);
        }

        collider.points = points;

        collider.gameObject.transform.SetParent(gameObject.transform);
    }

    void AssignVertices(Node[] nodes)
    {
        for (int i = 0; i < nodes.Length; ++i)
        {
            if (nodes[i].vertexIndex == -1)
            {
                // hasn't been assigned
                nodes[i].vertexIndex = vertices.Count;

                // update the verticle list
                vertices.Add(nodes[i].position);
            }
        }
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);
    }

    private void Update()
    {
        //if (Input.GetMouseButton(0))
        //{
        //    RaycastHit hitInfo;
        //    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))
        //    {
        //        if (hitInfo.collider.gameObject == gameObject)
        //        {
        //            Deforming(transform.InverseTransformPoint(hitInfo.point));
        //        }
        //    }
        //}
    }

    private void Deforming (Vector3 pos)
    {
        //float halfSizeX = (cellGrid.cells.GetLength(0) * cellSize) / 2;
        //float halfSizeY = (cellGrid.cells.GetLength(1) * cellSize) / 2;

        //int cellX = (int)((pos.x + halfSizeX) / cellSize);
        //int cellY = (int)((pos.y + halfSizeY) / cellSize);

        ////Debug.Log(cellX + ", " + cellY);

        //meshDeformingEvent.Invoke(cellX, cellY);
    }

    //private void OnDrawGizmos()
    //{
    //    if (cellGrid != null)
    //    {
    //        for (int x = 0; x < cellGrid.cells.GetLength(0); ++x)
    //        {
    //            for (int y = 0; y < cellGrid.cells.GetLength(1); ++y)
    //            {
    //                Gizmos.color = (cellGrid.cells[x, y].topLeft.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(cellGrid.cells[x, y].topLeft.position, Vector3.one * 0.9f);

    //                Gizmos.color = (cellGrid.cells[x, y].topRight.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(cellGrid.cells[x, y].topRight.position, Vector3.one * 0.9f);

    //                Gizmos.color = (cellGrid.cells[x, y].bottomRight.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(cellGrid.cells[x, y].bottomRight.position, Vector3.one * 0.9f);

    //                Gizmos.color = (cellGrid.cells[x, y].bottomLeft.active) ? Color.black : Color.white;
    //                Gizmos.DrawCube(cellGrid.cells[x, y].bottomLeft.position, Vector3.one * 0.9f);

    //                Gizmos.color = Color.grey;
    //                Gizmos.DrawCube(cellGrid.cells[x, y].centreTop.position, Vector3.one * 0.15f);
    //                Gizmos.DrawCube(cellGrid.cells[x, y].centreRight.position, Vector3.one * 0.15f);
    //                Gizmos.DrawCube(cellGrid.cells[x, y].centreBottom.position, Vector3.one * 0.15f);
    //                Gizmos.DrawCube(cellGrid.cells[x, y].centreLeft.position, Vector3.one * 0.15f);
    //            }
    //        }
    //    }
    //}
}