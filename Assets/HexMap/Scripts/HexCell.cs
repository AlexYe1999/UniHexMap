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
}

public class HexCell : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    HexCoordinates coordinates;
    
    [SerializeField]
    HexCell[] neighbors;
    
    Text coordText;
    
    public Color color;
    
    int elevation;
    
    public int Elevation 
    {
        get 
        {
            return elevation;
        }
        set 
        {
            elevation = value;
            
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            transform.localPosition = position;
        }
    }
    
    private void Awake()
    {
        coordText = GetComponentInChildren<Text>();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
