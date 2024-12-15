using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public int chunkCountX = 4, chunkCountZ = 4; 
    
    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;
    
    public HexCell cellPrefab;
    public HexGridChunk chunkPrefab;
    
    public Texture2D noiseSource;
    
    HexCell[] cells;
    HexGridChunk[] chunks;
    
    int cellCountX, cellCountZ;
    
    void Awake () 
    {
        HexMetrics.noiseSource = noiseSource;
        
        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }
    
    void OnEnable () 
    {
        HexMetrics.noiseSource = noiseSource;
    }
    
    public void ShowUI (bool visible)
    {
        for (int i = 0; i < chunks.Length; i++) 
        {
            chunks[i].ShowUI(visible);
        }
    }
    
    void CreateChunks ()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++) 
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }
    
    void CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];
        
        var index = 0;
        for (int z = 0; z < cellCountZ; z++) 
        {
            for (int x = 0; x < cellCountX; x++) 
            {
                CreateCell(x, z, index++);
            }
        }
    }
    
    void CreateCell (int x, int z, int i) 
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        cell.Coordinate = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Color = defaultColor;
        
        if (x > 0) 
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }
        
        AddCellToChunk(x, z, cell);
    }
	
    void AddCellToChunk (int x, int z, HexCell cell) 
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];
        
        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }
    
    public HexCell GetCell (Vector3 position) 
    {
        HexCoordinates coordinates = 
            HexCoordinates.FromPosition(transform.InverseTransformPoint(position));
        
        return cells[coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2];
    }
    
    public HexCell GetCell (HexCoordinates coordinates) 
    {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ) 
        {
            return null;
        }
        
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }
        return cells[x + z * cellCountX];
    }
    
}
