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
        Vector3 center = cell.Position;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        var edge = new EdgeVertices(v1, v2);
        
        TriangulateEdgeFan(center, edge, cell.color);

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, edge);
        }
    }

    void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) 
    {
        int vertexIndex = vertices.Count;
        
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }
    
    void AddTriangleUnperturbed (Vector3 v1, Vector3 v2, Vector3 v3) 
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
        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        vertices.Add(Perturb(v4));
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
        HexCell cell, 
        EdgeVertices e1)
    {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

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
            Vector3 cellV = cell.Position + HexMetrics.GetSecondSolidCorner(direction);
            Vector3 neighborV = neighbor.Position + HexMetrics.GetFirstSolidCorner(direction.Opposite());
            Vector3 nextNeighborV = nextNeighbor.Position + HexMetrics.GetSecondSolidCorner(direction.Next().Opposite());
            
            List<Tuple<HexCell, Vector3>> cells = new List<Tuple<HexCell, Vector3>>
            {
                new Tuple<HexCell, Vector3>(cell, cellV),
                new Tuple<HexCell, Vector3>(neighbor, neighborV),
                new Tuple<HexCell, Vector3>(nextNeighbor, nextNeighborV)
            };

            cells.Sort((a, b) => a.Item1.Elevation.CompareTo(b.Item1.Elevation));

            Vector3 delta1 = cells[1].Item2 - cells[0].Item2;
            Vector3 delta2 = cells[2].Item2 - cells[0].Item2;
            if (delta1.x * delta2.z - delta1.z * delta2.x < 0.0f)
            {
                (cells[1], cells[2]) = (cells[2], cells[1]);
            }
            
            TriangulateCorner(
                cells[2].Item2, cells[2].Item1, 
                cells[1].Item2, cells[1].Item1, 
                cells[0].Item2, cells[0].Item1);
        }
    }

    void TriangulateBridge(
        HexDirection direction,
        HexCell cell,
        HexCell neighbor)
    {
        Vector3 center = cell.Position;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
        
        Vector3 neighborCenter = neighbor.Position;
        Vector3 v3 = neighborCenter + HexMetrics.GetFirstSolidCorner(direction.Opposite());
        Vector3 v4 = neighborCenter + HexMetrics.GetSecondSolidCorner(direction.Opposite());
        
        TriangulateEdgeStrip(
            new EdgeVertices(v1, v2), cell.color,
            new EdgeVertices(v4, v3), neighbor.color);
    }
    
    void TriangulateEdgeTerraces (
        HexDirection direction,
        HexCell cell,
        HexCell neighbor) 
    {
        Vector3 center = cell.Position;
        Vector3 begin1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 begin2 = center + HexMetrics.GetSecondSolidCorner(direction);
        
        Vector3 neighborCenter = neighbor.Position;
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
        
            TriangulateEdgeStrip(
                new EdgeVertices(preV1, preV2), preC,
                new EdgeVertices(interp1, interp2), interpC);

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
                TriangulateCornerTerrace
                (
                    bottom, bottomCell, 
                    left, leftCell, 
                    right, rightCell
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
            else 
            {
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
        float t = (float)leftCell.Elevation / Math.Abs(rightCell.Elevation - beginCell.Elevation);
        Vector3 midPointV = Vector3.Lerp(Perturb(begin), Perturb(right), t);
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
            AddTriangleUnperturbed(Perturb(left), Perturb(right), midPointV);
            AddTriangleColor(leftCell.color, rightCell.color, midPointColor);   
        }
    }
    
    void TriangulateCornerCliffTerrace (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float t = (float)rightCell.Elevation / Math.Abs(leftCell.Elevation - beginCell.Elevation);
        Vector3 midPointV = Vector3.Lerp(Perturb(begin), Perturb(left), t);
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
            AddTriangleUnperturbed(Perturb(left), Perturb(right), midPointV);
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
            
            AddTriangleUnperturbed(Perturb(preV), Perturb(interpV), mid);
            AddTriangleColor(preColor, interpC, midColor);

            preV = interpV;
            preColor = interpC;
        }
    }
    
    void TriangulateEdgeFan (Vector3 center, EdgeVertices edge, Color color) 
    {
        AddTriangle(center, edge.v1, edge.v2);
        AddTriangleColor(color);
        AddTriangle(center, edge.v2, edge.v3);
        AddTriangleColor(color);
        AddTriangle(center, edge.v3, edge.v4);
        AddTriangleColor(color);
    }
    
    void TriangulateEdgeStrip (
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2)
    {
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        AddQuadColor(c1, c2);
        AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        AddQuadColor(c1, c2);
        AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        AddQuadColor(c1, c2);
    }
    
    Vector3 Perturb (Vector3 position)
    {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }
}