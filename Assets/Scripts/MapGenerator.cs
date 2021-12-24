using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum MapShape
{
    Block,
    Circle,
    PerlinBlock,
    PerlinCircle,
}

[System.Serializable]
public class MapChangedEvent : UnityEvent<Map[,]>
{

}

public class Map
{
    public int[,] data;

    public Map(int dx, int dy)
    {
        data = new int[dx, dy];
    }
}

public class MapGenerator : MonoBehaviour
{
    // Dimensions of the map
    public int dX = 1;

    public int dY = 1;

    public int chunkX = 1;

    public int chunkY = 1;

    private int chunkDx;

    private int chunkDy;

    // Map raw data
    public Map[,] maps;

    // Map shape
    public MapShape mapShape = MapShape.Block;

    // the threshold to separate visible and invisible nodes
    [Range(0, 100)]
    public int threshold;

    // Perlin Noise configuration

    //The number of cycles of the basic noise pattern that are repeated
    public float scale = 1.0f;

    // Use for circular map
    private float fitRadius;

    // Use for random
    private string randomSeed;

    public MapChangedEvent mapChanged;

    private void Awake()
    {
        // Find the fit radius
        fitRadius = Mathf.Min(dX, dY) / 2f;

        if (dX % chunkX != 0 || dY % chunkY != 0)
        {
            Debug.LogError("Chunk dimension and map dimension is not divisable");
        }

        chunkDx = dX / chunkX;
        chunkDy = dY / chunkY;
    }

    private void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            GenerateMap();
        }
    }


    private void GenerateMap ()
    {
        if (dX <= 0 || dY <= 0)
        {
            Debug.LogError("Map dimension is not valid");
            return;
        }

        maps = new Map[chunkX, chunkY];

        switch (mapShape)
        {
            case MapShape.Block:
                FillMapRandom(checkFBlock);
                break;
            case MapShape.Circle:
                FillMapRandom(checkFCircle);
                break;
            case MapShape.PerlinBlock:
                FillMapPerlin(checkFBlock);
                break;
            case MapShape.PerlinCircle:
                FillMapPerlin(checkFCircle);
                break;
        }

        // Emit event
        mapChanged.Invoke(maps);
    }


    private void FillMapRandom (checkF checkingFunc)
    {
        // Generate new seed
        randomSeed = Time.time.ToString();

        System.Random random = new System.Random(randomSeed.GetHashCode());

        for (int mx = 0; mx < chunkX; ++mx)
        {
            for (int my = 0; my < chunkY; ++my)
            {
                maps[mx, my] = new Map(chunkDx, chunkDy);

                for (int x = chunkDx * mx; x < chunkDx + chunkDx * mx; ++x)
                {
                    for (int y = chunkDy * my; y < chunkDy + chunkDy * my; ++y)
                    {
                        int isoValue = random.Next(0, 100);

                        maps[mx, my].data[x - chunkDx * mx, y - chunkDy * my] = checkingFunc(isoValue, x, y) ? isoValue : isoValue * -1;
                    }
                }
            }
        }
    }

    private void FillMapPerlin (checkF checkingFunc)
    {
        for (int mx = 0; mx < chunkX; ++mx)
        {
            for (int my = 0; my < chunkY; ++my)
            {
                maps[mx, my] = new Map(chunkDx, chunkDy);

                for (int x = chunkDx * mx; x < chunkDx + chunkDx * mx; ++x)
                {
                    for (int y = chunkDy * my; y < chunkDy + chunkDy * my; ++y)
                    {
                        int isoValue = (int)(Mathf.PerlinNoise((float)x / chunkDx * scale, (float)y / chunkDy * scale) * 100);

                        maps[mx, my].data[x - chunkDx * mx, y - chunkDy * my] = checkingFunc(isoValue, x, y) ? isoValue : isoValue * -1;
                    }
                }
            }
        }
    }

    //public void Deforming (int x, int y)
    //{
    //    if (x < 0 || x >= dX || y < 0 | y >= dY)
    //    {
    //        Debug.LogError("Deforming point is not valid: x = " + x + ", y = " + y);
    //        return;
    //    }

    //    for (int i = x - 2; i < x + 2; ++i)
    //    {
    //        if (i < 0 || i >= dX) continue;

    //        for (int j = y - 2; j < y + 2; ++j)
    //        {
    //            if (j < 0 || j >= dY) continue;

    //            if (Mathf.Pow(i - x, 2) + Mathf.Pow(j - y, 2) <= 4)
    //            {
    //                if (map[i, j] > 0) map[i, j] *= -1;
    //            }
    //        }
    //    }
        
    //    // Emit map changed
    //    mapChanged.Invoke(map);
    //}

    /*Delegate Methods*/
    private delegate bool checkF(int isoValue, int x, int y);

    private bool checkFBlock (int isoValue, int x, int y)
    {
        return isoValue >= threshold;
    }

    private bool checkFCircle (int isoValue, int x, int y)
    {
        return (Mathf.Pow(x - fitRadius, 2) + Mathf.Pow(y - fitRadius, 2)) <= Mathf.Pow(fitRadius, 2) && isoValue >= threshold;
    }

    //private void OnDrawGizmos()
    //{
    //    if (map != null)
    //    {
    //        for (int x = 0; x < nodeCountX; ++x)
    //        {
    //            for (int y = 0; y < nodeCountY; ++y)
    //            {
    //                Gizmos.color = map[x, y] < 0 ? Color.black : Color.white;
    //                Vector3 pos = new Vector3(-nodeCountX / 2 + x + 0.5f, -nodeCountY / 2 + y + 0.5f);
    //                Gizmos.DrawCube(pos, Vector3.one);
    //            }
    //        }
    //    }
    //}
}