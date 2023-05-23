using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class WFC_Main : MonoBehaviour
{
    public List<GameObject> allPrefabs;
    public Vector3 grid;
    public float cellSize;
    public float spawnEvery;

    private Vector3 boundingUnit;
    private Dictionary<string, List<Vector3>> sockets = new Dictionary<string, List<Vector3>>();
    private List<WFC_SingleState> superPosition = new List<WFC_SingleState>();
    private List<WFC_CellState> allCells = new List<WFC_CellState>();
    private List<WFC_CellState> cellToProcess = new List<WFC_CellState>();
    [SerializeField] private WFC_Spawned_Data_List allSpawnedPrefab = new WFC_Spawned_Data_List();
    private int collapsed;
    private float timer;
    // Start is called before the first frame update
    void Start()
    {
        collapsed = 0;
        boundingUnit = new Vector3(cellSize / 2, 0, cellSize / 2);
        processPrefab();
        allocatePossiblitySpace();
        triggerCollapse();
        propogateCollapse();
        collapsed++;
        timer = spawnEvery;
    }

    // Update is called once per frame
    void Update()
    {
        timer = timer - Time.deltaTime;
        if (timer <= 0 && collapsed<allCells.Count)
        {
            timer = spawnEvery;
            getLowestEntropyCellAndSpawn();
            propogateCollapse();
        }
    }

    private void OnApplicationQuit()
    {
        string json = JsonUtility.ToJson(allSpawnedPrefab, true);
        File.WriteAllText(Application.dataPath + "/CustomAssets/WFC/Prototype/Spawned.json", json);
    }

    private void processPrefab()
    {
        for (int i = 0; i < allPrefabs.Count; i++)
        {
            GameObject g = allPrefabs[i];
            g.transform.position = Vector3.zero;
            g.transform.rotation = Quaternion.Euler(g.transform.localEulerAngles.x, g.transform.localEulerAngles.y + 0, g.transform.localEulerAngles.z);
            processMesh(g);
            g.transform.rotation = Quaternion.Euler(g.transform.localEulerAngles.x, g.transform.localEulerAngles.y + 90, g.transform.localEulerAngles.z);
            processMesh(g);
            g.transform.rotation = Quaternion.Euler(g.transform.localEulerAngles.x, g.transform.localEulerAngles.y + 90, g.transform.localEulerAngles.z);
            processMesh(g);
            g.transform.rotation = Quaternion.Euler(g.transform.localEulerAngles.x, g.transform.localEulerAngles.y + 90, g.transform.localEulerAngles.z);
            processMesh(g);
        }
    }

    private void processMesh(GameObject prefab)
    {
        examineMesh(prefab, (int)prefab.transform.localEulerAngles.y);
    }

    private void examineMesh(GameObject prefab, int rotationIndex)
    {
        MeshFilter m = prefab.GetComponentInChildren<MeshFilter>();
        List<Vector3> allPositionsx_posbnd = new List<Vector3>();
        List<Vector3> allPositionsx_negbnd = new List<Vector3>();
        List<Vector3> allPositionsz_posbnd = new List<Vector3>();
        List<Vector3> allPositionsz_negbnd = new List<Vector3>();
        Vector3 roundedVertexPosition = new Vector3();

        for (int i = 0; i < m.sharedMesh.vertices.Length; i++)
        {
            roundedVertexPosition.x = (float)System.Math.Round(prefab.transform.TransformPoint(m.sharedMesh.vertices[i]).x, 1);
            roundedVertexPosition.y = (float)System.Math.Round(prefab.transform.TransformPoint(m.sharedMesh.vertices[i]).y, 1);
            roundedVertexPosition.z = (float)System.Math.Round(prefab.transform.TransformPoint(m.sharedMesh.vertices[i]).z, 1);

            if (!allPositionsx_posbnd.Contains(roundedVertexPosition) && roundedVertexPosition.x == boundingUnit.x)
            {
                allPositionsx_posbnd.Add(roundedVertexPosition);
            }

            if (!allPositionsx_negbnd.Contains(roundedVertexPosition) && roundedVertexPosition.x == -boundingUnit.x)
            {
                allPositionsx_negbnd.Add(roundedVertexPosition);
            }

            if (!allPositionsz_posbnd.Contains(roundedVertexPosition) && roundedVertexPosition.z == boundingUnit.z)
            {
                allPositionsz_posbnd.Add(roundedVertexPosition);
            }

            if (!allPositionsz_negbnd.Contains(roundedVertexPosition) && roundedVertexPosition.z == -boundingUnit.z)
            {
                allPositionsz_negbnd.Add(roundedVertexPosition);
            }
        }

        WFC_SingleState ss = new WFC_SingleState();
        ss.prefab = prefab;
        ss.rotationIndex = rotationIndex;
        ss.right_SocketCode = GetSocketCode(allPositionsx_posbnd);
        ss.left_SocketCode = GetSocketCode(allPositionsx_negbnd);
        ss.back_SocketCode = GetSocketCode(allPositionsz_negbnd);
        ss.front_SocketCode = GetSocketCode(allPositionsz_posbnd);
        superPosition.Add(ss);
    }

    private string GetSocketCode(List<Vector3> source)
    {
        string socketCode = null;
        float hashSocket = 0;

        for (int i = 0; i < source.Count - 1; i++)
        {
            hashSocket += hashVertexPos(new Vector3(Mathf.Abs(source[i].x), Mathf.Abs(source[i].y), Mathf.Abs(source[i].z)));
        }
        socketCode = System.Math.Round(hashSocket, 0).ToString();
        if (socketCode != null)
        {
            return socketCode;
        }
        else
        {
            return "-1";
        }
    }

    private float hashVertexPos(Vector3 pos)
    {
        return ((pos.x * 47) + (pos.y * 53) + (pos.z * 59));
    }

    private void allocatePossiblitySpace()
    {
        for (int i = 0; i <= grid.x; i += (int)cellSize)
        {
            for (int j = 0; j <= grid.z; j += (int)cellSize)
            {
                WFC_CellState ci = new WFC_CellState();
                ci.cellCoordinate = new Vector3(i, 0, j);
                ci.isCollapsed = false;
                ci.superPosition.AddRange(superPosition);
                allCells.Add(ci);
            }
        }
    }

    private void triggerCollapse()
    {
        int randomCellIndex = Random.Range(0, allCells.Count);
        /*for (int i=0; i<allCells.Count; i++)
        {
            if(allCells[i].cellCoordinate == Vector3.zero)
            {
                randomCellIndex = i;
                break;
            }
        }*/
        Vector3 randomCell = allCells[randomCellIndex].cellCoordinate;
        int randomStateIndex = Random.Range(0, allCells[randomCellIndex].superPosition.Count);
        WFC_SingleState ss = allCells[randomCellIndex].superPosition[randomStateIndex];
        spawn(ss.prefab, randomCell, ss.rotationIndex);
        allCells[randomCellIndex].isCollapsed = true;
        foreach (WFC_SingleState wss in allCells[randomCellIndex].superPosition.ToArray())
        {
            if (wss!= ss)
            {
                allCells[randomCellIndex].superPosition.Remove(wss);
            }
        }
        cellToProcess.Add(allCells[randomCellIndex]);
    }

    private void propogateCollapse()
    {
        Vector3 frontCellDelta = new Vector3(0, 0, cellSize);
        Vector3 backCellDelta = new Vector3(0, 0, -cellSize);
        Vector3 leftCellDelta = new Vector3(-cellSize, 0, 0);
        Vector3 rightCellDelta = new Vector3(cellSize, 0, 0);

        List<string> toProcessFrontSockets = new List<string>();
        List<string> toProcessBackSockets = new List<string>();
        List<string> toProcessLeftSockets = new List<string>();
        List<string> toProcessRightSockets = new List<string>();

        foreach (WFC_CellState toProceess in cellToProcess.ToArray())
        {
            foreach (WFC_SingleState tps in toProceess.superPosition)
            {
                if (!toProcessFrontSockets.Contains(tps.front_SocketCode))
                {
                    toProcessFrontSockets.Add(tps.front_SocketCode);
                }
                if (!toProcessBackSockets.Contains(tps.back_SocketCode))
                {
                    toProcessBackSockets.Add(tps.back_SocketCode);
                }
                if (!toProcessLeftSockets.Contains(tps.left_SocketCode))
                {
                    toProcessLeftSockets.Add(tps.left_SocketCode);
                }
                if (!toProcessRightSockets.Contains(tps.right_SocketCode))
                {
                    toProcessRightSockets.Add(tps.right_SocketCode);
                }
            }

            foreach (WFC_CellState CellState in allCells.ToArray())
            {
                if (CellState.cellCoordinate == (toProceess.cellCoordinate + frontCellDelta) && !CellState.isCollapsed)
                {
                    foreach (WFC_SingleState adjCellSS in CellState.superPosition.ToArray())
                    {
                        if (!toProcessFrontSockets.Contains(adjCellSS.back_SocketCode))
                        {
                            CellState.superPosition.Remove(adjCellSS);
                            cellToProcess.Add(CellState);
                        }
                    }
                }

                if (CellState.cellCoordinate == (toProceess.cellCoordinate + leftCellDelta) && !CellState.isCollapsed)
                {
                    foreach (WFC_SingleState adjCellSS in CellState.superPosition.ToArray())
                    {
                        if (!toProcessLeftSockets.Contains(adjCellSS.right_SocketCode))
                        {
                            CellState.superPosition.Remove(adjCellSS);
                            cellToProcess.Add(CellState);
                        }
                    }
                }

                if (CellState.cellCoordinate == (toProceess.cellCoordinate + backCellDelta) && !CellState.isCollapsed)
                {
                    foreach (WFC_SingleState adjCellSS in CellState.superPosition.ToArray())
                    {
                        if (!toProcessBackSockets.Contains(adjCellSS.front_SocketCode))
                        {
                            CellState.superPosition.Remove(adjCellSS);
                            cellToProcess.Add(CellState);
                        }
                    }
                }

                if (CellState.cellCoordinate == (toProceess.cellCoordinate + rightCellDelta) && !CellState.isCollapsed)
                {
                    foreach (WFC_SingleState adjCellSS in CellState.superPosition.ToArray())
                    {
                        if (!toProcessRightSockets.Contains(adjCellSS.left_SocketCode))
                        {
                            CellState.superPosition.Remove(adjCellSS);
                            cellToProcess.Add(CellState);
                        }
                    }
                }
            }
            cellToProcess.Remove(toProceess);
        }
    }

    private void getLowestEntropyCellAndSpawn()
    {
        int lowestCount = allPrefabs.Count * 400;
        WFC_CellState lowestEntropyCellState = new WFC_CellState();
        WFC_SingleState ss = new WFC_SingleState();
        foreach (WFC_CellState ci in allCells)
        {
            if (!ci.isCollapsed && lowestCount > ci.superPosition.Count)
            {
                lowestCount = ci.superPosition.Count;
                lowestEntropyCellState = ci;
            }
        }
        if (lowestCount > 1)
        {
            lowestCount = Random.Range(0, lowestCount);
        }
        ss = lowestEntropyCellState.superPosition[lowestCount];
        if (lowestEntropyCellState!=null)
        {
            spawn(ss.prefab, lowestEntropyCellState.cellCoordinate, ss.rotationIndex);
            collapsed++;
        }
        foreach (WFC_SingleState wss in lowestEntropyCellState.superPosition.ToArray())
        {
            if (wss != ss)
            {
                lowestEntropyCellState.superPosition.Remove(wss);
            }
        }
        lowestEntropyCellState.isCollapsed = true;
        cellToProcess.Add(lowestEntropyCellState);
    }

    private void spawn(GameObject prefab, Vector3 position, int rotationIndex)
    {
        GameObject pf = GameObject.Instantiate(prefab, position, Quaternion.identity);
        pf.transform.Rotate(0, rotationIndex, 0);
        WFC_Spawned_Data wsd = new WFC_Spawned_Data();
        wsd.prefabName = prefab.name;
        wsd.rotation = pf.transform.rotation;
        wsd.position = position;
        allSpawnedPrefab.wsdList.Add(wsd);
    }
}
