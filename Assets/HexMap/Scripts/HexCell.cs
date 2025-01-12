using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public enum HexDirection
{
    NE, 
    E, 
    SE, 
    SW, 
    W, 
    NW,
    NUM
}
    
public static class HexDirectionExtensions 
{
    public static HexDirection Opposite (this HexDirection direction)
    {
        return (HexDirection)(((int)direction + 3) % (int)HexDirection.NUM);
    }
    
    public static HexDirection Previous (this HexDirection direction) 
    {
        return (HexDirection)(((int)direction - 1 + (int)HexDirection.NUM) % (int)HexDirection.NUM);
    }

    public static HexDirection Next (this HexDirection direction) 
    {
        return (HexDirection)(((int)direction + 1) % (int)HexDirection.NUM);
    }
    
    public static HexDirection Previous2 (this HexDirection direction) 
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }

    public static HexDirection Next2 (this HexDirection direction) 
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}

public class HexCell : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    HexCoordinates coordinates;
    
    [SerializeField]
    HexCell[] neighbors;
    
    Canvas canvas;
    Text coordText;
    
    public Color Color 
    {
        get
        {
            return color;
        }
        set
        {
            if (color == value)
            {
                return;
            }
            color = value;
            Refresh();
        }
    }
    Color color;
    
    public HexGridChunk chunk;
    
    public int Elevation 
    {
        get 
        {
            return elevation;
        }
        set 
        {
            if (elevation == value)
            {
                return;
            }
            
            elevation = value;
            
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
            
            transform.localPosition = position;
            
            if (hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation) 
            {
                RemoveOutgoingRiver();
            }
            if (hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation)
            {
                RemoveIncomingRiver();
            }
            
            Refresh();
        }
    }
    int elevation = 0;
    
    public Vector3 Position 
    {
        get 
        {
            return transform.localPosition;
        }
    }
    
    private void Awake()
    {
        canvas = GetComponentInChildren<Canvas>();
        coordText = GetComponentInChildren<Text>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Refresh () 
    {
        if (chunk) 
        {
            chunk.Refresh();
            
            for (int i = 0; i < neighbors.Length; i++) 
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk) 
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }
    
    public HexCoordinates Coordinate
    {
        get
        {
            return coordinates;
        }

        set
        {
            coordinates = value;
            coordText.text = coordinates.ToStringOnSeparateLines();
        }
    }
    
    public HexCell GetNeighbor (HexDirection direction)
    {
        return neighbors[(int)direction];
    }
    
    public void SetNeighbor (HexDirection direction, HexCell cell) 
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }
    
    public HexEdgeType GetEdgeType (HexDirection direction) 
    {
        var neighbor = GetNeighbor(direction);

        return HexMetrics.GetEdgeType
        (
            elevation, neighbor != null ? neighbor.elevation : 0
        );
    }
    
    public HexEdgeType GetEdgeType (HexCell otherCell) 
    {
        return HexMetrics.GetEdgeType
        (
            elevation, otherCell.elevation
        );
    }
    
    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;
    
    public bool HasIncomingRiver 
    {
        get 
        {
            return hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver 
    {
        get
        {
            return hasOutgoingRiver;
        }
    }

    public HexDirection IncomingRiver 
    {
        get 
        {
            return incomingRiver;
        }
    }

    public HexDirection OutgoingRiver
    {
        get 
        {
            return outgoingRiver;
        }
    }
    
    public bool HasRiver
    {
        get 
        {
            return hasIncomingRiver || hasOutgoingRiver;
        }
    }
    
    public bool HasRiverBeginOrEnd 
    {
        get
        {
            return hasIncomingRiver != hasOutgoingRiver;
        }
    }
    
    public bool HasRiverThroughEdge (HexDirection direction) 
    {
        return hasIncomingRiver && incomingRiver == direction 
               || hasOutgoingRiver && outgoingRiver == direction;
    }
    
    public void SetOutgoingRiver (HexDirection direction) 
    {
        if (hasOutgoingRiver && outgoingRiver == direction) 
        {
            return;
        }
        
        HexCell neighbor = GetNeighbor(direction);
        if (!neighbor || elevation < neighbor.elevation) 
        {
            return;
        }
        
        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction) 
        {
            RemoveIncomingRiver();
        }
        
        hasOutgoingRiver = true;
        outgoingRiver = direction;
        RefreshSelfOnly();
        
        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
    }
    
    public void RemoveOutgoingRiver () 
    {
        if (!hasOutgoingRiver) 
        {
            return;
        }
        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    
    public void RemoveIncomingRiver () 
    {
        if (!hasIncomingRiver) 
        {
            return;
        }
        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }
    
    public void RemoveRiver () 
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }
    
    void RefreshSelfOnly () 
    {
        chunk.Refresh();
    }
    
    public float StreamBedY 
    {
        get 
        {
            return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
        }
    }
    
    public float RiverSurfaceY 
    {
        get 
        {
            return (elevation + HexMetrics.riverSurfaceElevationOffset) * HexMetrics.elevationStep;
        }
    }

    public float RiverSurfaceHeight
    {
        get
        {
            return RiverSurfaceY - StreamBedY; 
        }
    }
    
    public void ShowUI (bool visible) 
    {
        canvas.gameObject.SetActive(visible);
    }
}
