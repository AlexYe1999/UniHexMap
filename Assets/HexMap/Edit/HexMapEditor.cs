﻿using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour 
{
	public HexGrid hexGrid;
	
	public Color[] colors;
	
	void Awake ()
    {
		SelectColor(-1);
	}

	bool isDrag;
	HexDirection dragDirection;
	HexCell previousCell;
	
	void Update () 
    {
		if (Input.GetMouseButton(0)) 
        {
			HandleInput();
		}
		else
		{
			previousCell = null;
		}
	}

	void HandleInput () 
    {
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit)) 
        {
	        HexCell currentCell = hexGrid.GetCell(hit.point);
	        if (previousCell && previousCell != currentCell) 
	        {
		        ValidateDrag(currentCell);
	        }
	        else 
	        {
		        isDrag = false;
	        }
	        EditCells(currentCell);
	        
	        previousCell = currentCell;
		}
		else
		{
			previousCell = null;
		}
	}
	
	void ValidateDrag (HexCell currentCell) 
	{
		for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++) 
		{
			if (previousCell.GetNeighbor(dragDirection) == currentCell) 
			{
				isDrag = true;
				return;
			}
		}
		isDrag = false;
	}
	
	void EditCells (HexCell center) 
	{
		int centerX = center.Coordinate.X;
		int centerZ = center.Coordinate.Z;

		for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) 
		{
			for (int x = centerX - r; x <= centerX + brushSize; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
		
		for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) 
		{
			for (int x = centerX - brushSize; x <= centerX + r; x++) 
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
	}
	
	void EditCell (HexCell cell) 
	{
		if (cell)
		{
			if (applayColor)
			{
				cell.Color = activeColor;	
			}

			if (applyElevation)
			{
				cell.Elevation = activeElevation;	
			}	
			
			if (riverMode == OptionalToggle.No)
			{
				cell.RemoveRiver();
			}
			else if (isDrag && riverMode == OptionalToggle.Yes) 
			{
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell) 
				{
					otherCell.SetOutgoingRiver(dragDirection);
				}
			}
		}
	}

	public void ShowUI (bool visible) 
	{
		hexGrid.ShowUI(visible);
	}
	
	bool applayColor = false;
	Color activeColor;

	public void SelectColor(int index)
	{
		applayColor = index >= 0;

		if (applayColor)
		{
			activeColor = colors[index];
		}
	}

	bool applyElevation = true;
	public void SetApplyElevation (bool toggle)
	{
		applyElevation = toggle;
	}
	
	int activeElevation = 0;
	
	public void SetElevation (float elevation) 
	{
		activeElevation = (int)elevation;
	}
	
	int brushSize = 0;
	public void SetBrushSize (float size) 
	{
		brushSize = (int)size;
	}
	
	enum OptionalToggle 
	{
		Ignore, Yes, No
	}
	
	OptionalToggle riverMode;
	public void SetRiverMode (int mode) 
	{
		riverMode = (OptionalToggle)mode;
	}
}