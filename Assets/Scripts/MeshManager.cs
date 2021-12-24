using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshManager : MonoBehaviour
{
    public float cellSize = 1.0f;

    public MapGenerator mapGenerator;

    public MeshGenerator meshPrefab;

    private float mapSizeX;

    private float mapSizeY;

    private float chunkSizeX;

    private float chunkSizeY;

    private MeshGenerator[,] meshes;

    private Plane[] planes;

    private Map[,] maps;

    private void Awake()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("Map is not defined!");
            return;
        }

        mapSizeX = cellSize * mapGenerator.dX;

        mapSizeY = cellSize * mapGenerator.dY;

        chunkSizeX = mapSizeX / mapGenerator.chunkX;

        chunkSizeY = mapSizeY / mapGenerator.chunkY;

        planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        meshes = new MeshGenerator[mapGenerator.chunkX, mapGenerator.chunkY];

        for (int cx = 0; cx < mapGenerator.chunkX; ++cx)
        {
            for (int cy = 0; cy < mapGenerator.chunkY; ++cy)
            {
                MeshGenerator mesh = Instantiate(meshPrefab);
                mesh.gameObject.transform.SetParent(transform);
                meshes[cx, cy] = mesh;
            }
        }
    }

    public void LoadMeshes (Map[,] maps)
    {
        this.maps = maps;

        GenerateMeshes();
    }

    public void LoadMesh (Map map, int mx, int my)
    {
        if (maps == null)
        {
            Debug.LogError("Map is not defined");
            return;
        }

        if (mx < 0 || mx >= maps.GetLength(0) || my < 0 || my >= maps.GetLength(1))
        {
            Debug.LogError("Maps dimension is not valid. mx: " + mx + ", my: " + my + ", dmx: " + maps.GetLength(0) + ", dmy: " + maps.GetLength(1));
            return;
        }

        maps[mx, my] = map;

        GenerateMesh(mx, my, true);
    }

    private void GenerateMeshes ()
    {
        for (int mx = 0; mx < mapGenerator.chunkX; ++mx)
        {
            for (int my = 0; my < mapGenerator.chunkY; ++my)
            {
                GenerateMesh(mx, my);
            }
        }
    }

    private void GenerateMesh (int mx, int my, bool reload = false)
    {
        if (isChunkVisibleToCamera(mx, my))
        {
            if (reload)
            {
                meshes[mx, my].isMeshReady = false;
                meshes[mx, my].isCollisionReady = false;
            }

            meshes[mx, my].GenerateMesh(maps[mx, my].data, cellSize, (chunkSizeX / 2) + mx * chunkSizeX, (chunkSizeY / 2) + my * chunkSizeY);
        }
        else
        {
            meshes[mx, my].DestroyMesh();
        }
    }

    private bool isChunkVisibleToCamera (int x, int y)
    {
        Vector2 center = new Vector2((chunkSizeX / 2) + x * chunkSizeX, (chunkSizeY / 2) + y * chunkSizeY);
        Vector2 extents = new Vector2(chunkSizeX + 20, chunkSizeY + 20);

        Bounds bounds = new Bounds(center, extents);

        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    private void Update()
    {
        planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        GenerateMeshes();
    }
}
