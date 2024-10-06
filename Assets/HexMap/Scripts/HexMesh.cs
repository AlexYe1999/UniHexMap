using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour 
{
    Mesh hexMesh;   
    List<Vector3> vertices;
    List<int> triangles;
    List<Color> colors;
    
    MeshCollider meshCollider;
    
    void Awake () 
    {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        
        hexMesh.name = "Hex Mesh";
        vertices = new List<Vector3>();
        colors = new List<Color>();
        triangles = new List<int>();
    }
    
    public void Triangulate (HexCell[] cells) 
    {
        hexMesh.Clear();
        vertices.Clear();
        triangles.Clear();
        colors.Clear();
        
        for (int i = 0; i < cells.Length; i++) 
        {
            Triangulate(cells[i]);
        }
        
        hexMesh.vertices = vertices.ToArray();
        hexMesh.colors = colors.ToArray();
        hexMesh.triangles = triangles.ToArray();
        hexMesh.RecalculateNormals();
        
        meshCollider.sharedMesh = hexMesh;
    }
	
    void Triangulate (HexCell cell) 
    {
        for (HexDirection d = HexDirection.NE; d < HexDirection.NUM; d++) 
        {
            Triangulate(d, cell);
        }
    }

    void Triangulate(HexDirection direction, HexCell cell)
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.color);
        
        TriangulateConnection(direction, cell);
    }

    void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) 
    {
        int vertexIndex = vertices.Count;
        
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    
    void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(v1);
        vertices.Add(v2);
        vertices.Add(v3);
        vertices.Add(v4);
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }
    
    void AddTriangleColor (Color color)
    {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }
    
    void AddTriangleColor (Color c1, Color c2, Color c3)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }
    
    void AddQuadColor (Color c1, Color c2) 
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }
    
    void AddQuadColor (Color c1, Color c2, Color c3, Color c4)
    {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }
    
    void TriangulateConnection (
        HexDirection direction, 
        HexCell cell) 
    {
        if (direction <= HexDirection.SE)
        {
            HexCell neighbor = cell.GetNeighbor(direction);
            if (neighbor != null) 
            {
                switch (cell.GetEdgeType(direction))
                {
                    case HexEdgeType.Flat:
                    case HexEdgeType.Cliff:
                    {
                        TriangulateBridge(direction, cell, neighbor);   
                        break;
                    }
                    case HexEdgeType.Slope:
                    {
                        TriangulateEdgeTerraces(direction, cell, neighbor);
                        break;
                    }
                }

                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (direction <= HexDirection.E && nextNeighbor != null) 
                {
                    Vector3 cellV = cell.transform.localPosition + HexMetrics.GetSecondSolidCorner(direction);
                    Vector3 neighborV = neighbor.transform.localPosition + HexMetrics.GetFirstSolidCorner(direction.Opposite());
                    Vector3 nextNeighborV = nextNeighbor.transform.localPosition + HexMetrics.GetSecondSolidCorner(direction.Next().Opposite());

                    List<HexCell> cells = new List<HexCell>
                    {
                        cell,
                        neighbor,
                        nextNeighbor
                    };

                    cells.Sort((a, b) => a.Elevation.CompareTo(b.Elevation));

                    if (cells[0] == cell)
                    {
                        TriangulateCorner(
                            neighborV, neighbor, 
                            nextNeighborV, nextNeighbor, 
                            cellV, cell);
                    }
                    else if(cells[0] == neighbor)
                    {
                        TriangulateCorner(
                            nextNeighborV, nextNeighbor, 
                            cellV, cell, 
                            neighborV, neighbor);
                    }
                    else
                    {
                        TriangulateCorner(
                            cellV, cell, 
                            neighborV, neighbor, 
                            nextNeighborV, nextNeighbor);
                    }
                }
            }
        }
    }

    void TriangulateBridge(
        HexDirection direction,
        HexCell cell,
        HexCell neighbor)
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
        
        Vector3 neighborCenter = neighbor.transform.localPosition;
        Vector3 v3 = neighborCenter + HexMetrics.GetFirstSolidCorner(direction.Opposite());
        Vector3 v4 = neighborCenter + HexMetrics.GetSecondSolidCorner(direction.Opposite());
            
        AddQuad(v1, v2, v4, v3);
        AddQuadColor(cell.color, neighbor.color);
    }
    
    void TriangulateEdgeTerraces (
        HexDirection direction,
        HexCell cell,
        HexCell neighbor) 
    {
        Vector3 center = cell.transform.localPosition;
        Vector3 begin1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 begin2 = center + HexMetrics.GetSecondSolidCorner(direction);
        
        Vector3 neighborCenter = neighbor.transform.localPosition;
        Vector3 end1 = neighborCenter + HexMetrics.GetSecondSolidCorner(direction.Opposite());
        Vector3 end2 = neighborCenter + HexMetrics.GetFirstSolidCorner(direction.Opposite());

        Vector3 preV1 = begin1;
        Vector3 preV2 = begin2;
        Color preC = cell.color;
        
        for (var step = 1; step <= HexMetrics.terraceSteps; step++)
        {
            Vector3 interp1 = HexMetrics.TerraceLerp(begin1, end1, step); 
            Vector3 interp2 = HexMetrics.TerraceLerp(begin2, end2, step);
            Color interpC = HexMetrics.TerraceLerp(cell.color, neighbor.color, step);
        
            AddQuad(preV1, preV2, interp1, interp2);
            AddQuadColor(preC, interpC);

            preV1 = interp1;
            preV2 = interp2;
            preC = interpC;
        }
    }
    
    void TriangulateCorner (
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell,
        Vector3 bottom, HexCell bottomCell) 
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);
        
        if (leftEdgeType == HexEdgeType.Slope) 
        {
            if (rightEdgeType == HexEdgeType.Slope) 
            {
                TriangulateCornerTerrace(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
            else if (rightEdgeType == HexEdgeType.Flat) 
            {
                TriangulateCornerTerrace
                (
                    left, leftCell, 
                    right, rightCell, 
                    bottom, bottomCell
                );
            }
            else {
                TriangulateCornerTerraceCliff
                (
                    bottom, bottomCell, 
                    left, leftCell, 
                    right, rightCell
                );
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope) 
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerrace
                (
                    right, rightCell, 
                    bottom, bottomCell, 
                    left, leftCell
                );
            }
            else 
            {
                TriangulateCornerCliffTerrace
                (
                    bottom, bottomCell, 
                    left, leftCell, 
                    right, rightCell
                );
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) 
        {
            if (leftCell.Elevation < rightCell.Elevation) 
            {
                TriangulateCornerCliffTerrace
                (
                    right, rightCell, 
                    bottom, bottomCell, 
                    left, leftCell
                );
            }
            else 
            {
                TriangulateCornerTerraceCliff
                (
                    left, leftCell, 
                    right, rightCell, 
                    bottom, bottomCell
                );
            }
        }
        else 
        {
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }
    }
    
    void TriangulateCornerTerrace (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.color, c3, c4);
        
        for (var step = 2; step <= HexMetrics.terraceSteps; step++) 
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, step);
            v4 = HexMetrics.TerraceLerp(begin, right, step);
            c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, step);
            c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, step);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }
    }
    
    void TriangulateCornerTerraceCliff (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float t = (float)leftCell.Elevation / (rightCell.Elevation - beginCell.Elevation);
        Vector3 midPointV = Vector3.Lerp(begin, right, t);
        Color midPointColor = Color.Lerp(beginCell.color, rightCell.color, t);

        TriangulateBoundaryTriangle(
            begin, beginCell.color,
            left, leftCell.color,
            midPointV, midPointColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell.color,
                right, rightCell.color,
                midPointV, midPointColor);
        }
        else
        {
            AddTriangle(left, right, midPointV);
            AddTriangleColor(leftCell.color, rightCell.color, midPointColor);   
        }
    }
    
    void TriangulateCornerCliffTerrace (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float t = (float)rightCell.Elevation / (leftCell.Elevation - beginCell.Elevation);
        Vector3 midPointV = Vector3.Lerp(begin, left, t);
        Color midPointColor = Color.Lerp(beginCell.color, leftCell.color, t);

        TriangulateBoundaryTriangle(
            right, rightCell.color,
            begin, beginCell.color,
            midPointV, midPointColor);

        if (rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell.color,
                right, rightCell.color,
                midPointV, midPointColor);
        }
        else
        {
            AddTriangle(left, right, midPointV);
            AddTriangleColor(leftCell.color, rightCell.color, midPointColor);   
        }
    }
    
    void TriangulateBoundaryTriangle (
        Vector3 begin, Color beginColor,
        Vector3 end, Color endColor,
        Vector3 mid, Color midColor) 
    {
        Vector3 preV = begin;
        Color preColor = beginColor;
        for (var step = 0; step <= HexMetrics.terraceSteps; step++) 
        {
            var interpV = HexMetrics.TerraceLerp(begin, end, step);
            var interpC = HexMetrics.TerraceLerp(beginColor, endColor, step);
            
            AddTriangle(preV, interpV, mid);
            AddTriangleColor(preColor, interpC, midColor);

            preV = interpV;
            preColor = interpC;
        }
    }
}