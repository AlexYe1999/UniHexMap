using UnityEngine;
using UnityEditor;

[System.Serializable]
public struct HexCoordinates 
{
    [SerializeField]
    private int x, z;
    
    public int X
    {
        get
        {
            return x;
        }
    }

    public int Z
    {
        get
        {
            return z;
        }
    }

    public int Y
    {
        get 
        {
            return -X - Z;
        }
    }
    
    public HexCoordinates (int x, int z) 
    {
        this.x = x;
        this.z = z;
    }
    
    public static HexCoordinates FromOffsetCoordinates (int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition (Vector3 position) 
    {
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x;
        
        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;
        
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x -y);

        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x -y - iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
            
            //Debug.LogWarning("rounding error!");
        }
        
        return new HexCoordinates(iX, iZ);
    }
    
    public override string ToString()
    {
        return string.Format("( {0} , {1} , {2} )", X, Y, Z);
    }

    public string ToStringOnSeparateLines () 
    {
        return string.Format("{0}\n{1}\n{2}", X, Y, Z);
    }
}

[CustomPropertyDrawer(typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer 
{
    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
    {
        HexCoordinates coordinates = new HexCoordinates
        (
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue
        );
        
        EditorGUI.LabelField( position, label );
        position = EditorGUI.PrefixLabel(position, label);
        GUI.Label(position, coordinates.ToString());
    }
}