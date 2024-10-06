using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour 
{
	public Color[] colors;

	public HexGrid hexGrid;

	void Awake ()
    {
		SelectColor(0);
	}

	void Update () 
    {
		if (Input.GetMouseButton(0)) 
        {
			HandleInput();
		}
	}

	void HandleInput () 
    {
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit)) 
        {
			EditCell(hexGrid.GetCell(hit.point));
		}
	}
	
	void EditCell (HexCell cell) 
	{
		cell.color = activeColor;
		cell.Elevation = activeElevation;
		hexGrid.Refresh();
	}
	
	Color activeColor;
	
	public void SelectColor (int index) 
    {
		activeColor = colors[index];
	}

	int activeElevation = 0;
	
	public void SetElevation (float elevation) 
	{
		activeElevation = (int)elevation;
	}
}