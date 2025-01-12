using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;


public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;

    public HexMesh terrain;
    public HexMesh river;
    
    void Awake () 
    {
        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }
    
    void LateUpdate () 
    {
        Triangulate();
        enabled = false;
    }
    
    public void ShowUI (bool visible) 
    {
        foreach (var cell in cells)
        {
            cell.ShowUI(visible);
        }  
    }
    
    public void Refresh () 
    {
        enabled = true;
    }
    
    public void AddCell (int index, HexCell cell)
    {
        cell.chunk = this;
        cells[index] = cell;
        cell.transform.SetParent(transform, false);
    }
    
    void Triangulate ()
    {
        terrain.Clear();
        river.Clear();
        
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        
        terrain.Apply();
        river.Apply();
    }
    
        public void Triangulate (HexCell cell) 
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

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction)) 
            {
                edge.v3.y = cell.StreamBedY;
                
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, edge);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, edge);
                }
            }  
            else 
            {
                TriangulateAdjacentToRiver(direction, cell, center, edge);
            }
        }
        else
        {
            terrain.TriangulateEdgeFan(center, edge, cell.Color);    
        }

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, edge);
        }
    }
    
    public void TriangulateConnection (
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

        EdgeVertices e1 = new EdgeVertices(v1, v2);
        EdgeVertices e2 = new EdgeVertices(v4, v3);
        
        if (cell.HasRiverThroughEdge(direction)) 
        {
            e1.v3.y = cell.StreamBedY;
            e2.v3.y = neighbor.StreamBedY;
            
            TriangulateRiverQuad(
                e1.v2, e1.v4, e2.v2, e2.v4,
                cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                cell.HasIncomingRiver && cell.IncomingRiver == direction
            );
        }
        
        terrain.TriangulateEdgeStrip(
            e1, cell.Color,
            e2, neighbor.Color);
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
        Color preC = cell.Color;
        
        float cellDeltaY = cell.StreamBedY - cell.Position.y;
        float neighborDeltaY = neighbor.StreamBedY - neighbor.Position.y;
        float averageY = (cellDeltaY + neighborDeltaY) * 0.5f;
        
        float cellRiverDeltaY = cell.RiverSurfaceY - cell.Position.y;
        float neighborRiverDeltaY = neighbor.RiverSurfaceY - neighbor.Position.y;
        float averageRiverY = (cellRiverDeltaY + neighborRiverDeltaY) * 0.5f;
        
        for (var step = 1; step <= HexMetrics.terraceSteps; step++)
        {
            Vector3 interp1 = HexMetrics.TerraceLerp(begin1, end1, step); 
            Vector3 interp2 = HexMetrics.TerraceLerp(begin2, end2, step);
            Color interpC = HexMetrics.TerraceLerp(cell.Color, neighbor.Color, step);
        
            EdgeVertices e1 = new EdgeVertices(preV1, preV2);
            EdgeVertices e2 = new EdgeVertices(interp1, interp2);
            
            EdgeVertices e3 = new EdgeVertices(preV1, preV2);
            EdgeVertices e4 = new EdgeVertices(interp1, interp2);
            
            if (cell.HasRiverThroughEdge(direction)) 
            {
                if (step == 1)
                {
                    e1.v3.y += cellDeltaY;
                    e3.v2.y += cellRiverDeltaY;
                }
                else
                {
                    e1.v3.y += averageY;
                    e3.v2.y += averageRiverY;
                }
                
                if (step == HexMetrics.terraceSteps)
                {
                    e2.v3.y += neighborDeltaY;
                    e4.v2.y += neighborRiverDeltaY;
                }
                else
                {
                    e2.v3.y += averageY;
                    e4.v2.y += averageRiverY;
                }
                
                TriangulateRiverQuad(
                    e3.v2, e3.v4, e4.v2, e4.v4,
                    e3.v2.y, e4.v2.y,
                    cell.HasIncomingRiver && cell.IncomingRiver == direction
                );
                
            }
            
            terrain.TriangulateEdgeStrip(e1, preC, e2, interpC);
            
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
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }
    
    void TriangulateCornerTerrace (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(beginCell.Color, c3, c4);
        
        for (var step = 2; step <= HexMetrics.terraceSteps; step++) 
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, step);
            v4 = HexMetrics.TerraceLerp(begin, right, step);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, step);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, step);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }
    }
    
    void TriangulateCornerTerraceCliff (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        float t = 0.0f;
        float deltaElevation = rightCell.Elevation - beginCell.Elevation;

        if (deltaElevation > 0.0f)
        {
            t = leftCell.Elevation / deltaElevation;
        }
        else
        {
            t = (beginCell.Elevation - leftCell.Elevation) / -deltaElevation;
        }
        
        Vector3 midPointV = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), t);
        Color midPointColor = Color.Lerp(beginCell.Color, rightCell.Color, t);
        
        TriangulateBoundaryTriangle(
            begin, beginCell.Color,
            left, leftCell.Color,
            midPointV, midPointColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell.Color,
                right, rightCell.Color,
                midPointV, midPointColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), midPointV);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, midPointColor);   
        }
    }
    
    public void TriangulateCornerCliffTerrace (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        
        float t = 0.0f;
        float deltaElevation = leftCell.Elevation - beginCell.Elevation;

        if (deltaElevation > 0.0f)
        {
            t = rightCell.Elevation / deltaElevation;
        }
        else
        {
            t = (beginCell.Elevation - rightCell.Elevation) / -deltaElevation;
        }
        
        Vector3 midPointV = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), t);
        Color midPointColor = Color.Lerp(beginCell.Color, leftCell.Color, t);

        TriangulateBoundaryTriangle(
            right, rightCell.Color,
            begin, beginCell.Color,
            midPointV, midPointColor);

        if (rightCell.GetEdgeType(leftCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell.Color,
                right, rightCell.Color,
                midPointV, midPointColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), midPointV);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, midPointColor);   
        }
    }
    
    public void TriangulateBoundaryTriangle (
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
            
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(preV), HexMetrics.Perturb(interpV), mid);
            terrain.AddTriangleColor(preColor, interpC, midColor);

            preV = interpV;
            preColor = interpC;
        }
    }
    
    void TriangulateAdjacentToRiver (HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) 
    {
        if (cell.HasRiverThroughEdge(direction.Next())) 
        {
            if (cell.HasRiverThroughEdge(direction.Previous())) 
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.innerToOuter * 0.5f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2())) 
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }
        
        EdgeVertices m = new EdgeVertices
        (
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );
        
        terrain.TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        terrain.TriangulateEdgeFan(center, m, cell.Color);
    }
    
        void TriangulateWithRiver (HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite())) 
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next())) 
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2())) 
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
        }
        else 
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }
        center = Vector3.Lerp(centerL, centerR, 0.5f);
        
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f),
            1f / 6f);
        
        m.v3.y = center.y = e.v3.y;
        
        terrain.TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        
        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(cell.Color);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(cell.Color);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(cell.Color);
        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(cell.Color);
        
        bool reversed = cell.IncomingRiver == direction;
        TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, reversed);
        TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, reversed);
    }
    
    void TriangulateWithRiverBeginOrEnd (HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) 
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f));
        
        m.v3.y = e.v3.y;
        
        terrain.TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        terrain.TriangulateEdgeFan(center, m, cell.Color);
        
        bool reversed = cell.HasIncomingRiver;
		TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, reversed);
        
        center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
        river.AddTriangle(center, m.v2, m.v4);
        if (reversed) 
        {
            river.AddTriangleUV(new Vector2(0.5f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else 
        {
            river.AddTriangleUV(new Vector2(0.5f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        }
    }
    
    void TriangulateRiverQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, bool reversed) 
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, reversed);
    }
    
    void TriangulateRiverQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, bool reversed)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        river.AddQuad(v1, v2, v3, v4);

        if (reversed)
        {
            river.AddQuadUV(1f, 0f, 1f, 0f);
        }
        else
        {
            river.AddQuadUV(0f, 1f, 0f, 1f);
        }
    }
    
    
}