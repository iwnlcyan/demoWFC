using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WFC_CellState 
{
    public bool isCollapsed;
    public Vector3 cellCoordinate;
    public List<WFC_SingleState> superPosition = new List<WFC_SingleState>();
}
[System.Serializable]
public class WFC_SingleState
{
    public GameObject prefab;
    public int rotationIndex;
    public string front_SocketCode;
    public string back_SocketCode;
    public string right_SocketCode;
    public string left_SocketCode;
}
