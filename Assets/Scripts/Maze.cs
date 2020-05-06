using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : MonoBehaviour {
    [System.Serializable]
    public class Cell
    {
        public bool visited;
        public GameObject north;//1
        public GameObject east;//2
        public GameObject west;//3
        public GameObject south;//4
    }

    public GameObject wall;
    public GameObject plane;
    public float wallLength = 1.0f;
    public int xSize = 5;
    public int ySize = 5;
    private Vector3 initialPos;
    private GameObject wallHolder;
    public Cell[] cells;
    private int currentCell = 0;
    private int totalCell;
    private int visitedCells = 0;
    private bool startedBuilding = false;
    private int currentNeighbor = 0;
    private List<int> lastCells;
    private int backingUp = 0;
    private int wallToBreak;

	// Use this for initialization
	void Start () {
        CreateMaze();	
	}

    void CreateWalls()
    {
        wallHolder = new GameObject();
        wallHolder.name = "Maze";

        initialPos = new Vector3((-xSize / 2) + wallLength / 2, 0.0f, (-ySize / 2) + wallLength / 2);
        Vector3 myPos = initialPos;
        GameObject tempWall;

        GameObject.Find("Camera").transform.position= new Vector3(0,Mathf.Max(xSize, ySize),0);
        
        for(int i = 0; i < ySize; i++)
        {
            for(int j = 0; j <= xSize; j++)
            {
                myPos = new Vector3(initialPos.x + (j * wallLength) - wallLength / 2, 0.0f, initialPos.z + (i * wallLength) - wallLength / 2);
                tempWall = Instantiate(wall, myPos, Quaternion.identity) as GameObject;
                tempWall.transform.parent = wallHolder.transform;
            }
        }

        for (int i = 0; i <= ySize; i++)
        {
            for (int j = 0; j < xSize; j++)
            {
                myPos = new Vector3(initialPos.x + (j * wallLength), 0.0f, initialPos.z + (i * wallLength) - wallLength);
                tempWall = Instantiate(wall, myPos, Quaternion.Euler(0.0f,90.0f,0.0f)) as GameObject;
                tempWall.transform.parent = wallHolder.transform;
            }
        }

    }

	void CreateCells()
    {
        lastCells = new List<int>();
        lastCells.Clear();
        totalCell = xSize * ySize;
        GameObject[] allWalls;
        int children = wallHolder.transform.childCount;
        allWalls = new GameObject[children];
        cells = new Cell[totalCell];
        int eastWestProcess = 0;
        int childProcess = 0;
        int termCount = 0;
        //Gets all the children
        for(int i = 0; i < children; i++)
        {
            allWalls[i] = wallHolder.transform.GetChild(i).gameObject;
        }

        for(int cellprocess = 0; cellprocess < cells.Length; cellprocess++)
        {

            if (termCount == xSize)
            {
                eastWestProcess++;
                termCount = 0;
            }

            cells[cellprocess] = new Cell();
            cells[cellprocess].east = allWalls[eastWestProcess];
            cells[cellprocess].south = allWalls[childProcess + (xSize + 1) * ySize];

            eastWestProcess++;

            termCount++;
            childProcess++;
            cells[cellprocess].west = allWalls[eastWestProcess];
            cells[cellprocess].north = allWalls[childProcess + ((xSize + 1) * ySize) + xSize - 1];

        }
    }

    void CreateMaze()
    {
        CreateWalls();
        CreateCells();

        while (visitedCells < totalCell)
        {
            if (startedBuilding)
            {
                GiveMeNeighbor();
                if(cells[currentNeighbor].visited==false && cells[currentCell].visited == true)
                {
                    BreakWall();
                    cells[currentNeighbor].visited = true;
                    visitedCells++;
                    lastCells.Add(currentCell);
                    currentCell = currentNeighbor;
                    if (lastCells.Count > 0)
                    {
                        backingUp = lastCells.Count - 1;
                    }

                }
            }
            else
            {
                currentCell = Random.Range(0, totalCell);
                cells[currentCell].visited = true;
                visitedCells++;
                startedBuilding = true;
            }
        }

        float xPosSet = 0, yPosSet = 0;
        GameObject myPlane = Instantiate(plane);
        myPlane.transform.localScale = new Vector3(xSize / 10.0f, 1f, ySize / 10.0f);
        if (xSize % 2 != 0)
        {
            xPosSet = 0.5f;
        }
        if (ySize % 2 == 0)
        {
            yPosSet = -0.5f;
        }
        myPlane.transform.position = new Vector3(xPosSet, -0.5f, yPosSet);
        myPlane.transform.parent = wallHolder.transform;
    }

    void BreakWall()
    {
        switch (wallToBreak)
        {
            case 1: Destroy(cells[currentCell].north); break;
            case 2: Destroy(cells[currentCell].east); break;
            case 3: Destroy(cells[currentCell].west); break;
            case 4: Destroy(cells[currentCell].south); break;
        }
    }

    void GiveMeNeighbor()
    {
        int length = 0;
        int[] neighbors = new int[4];
        int[] connectingWall = new int[4];
        int check = 0;
        check = ((currentCell + 1)/xSize);
        check -= 1;
        check *= xSize;
        check += xSize;
        //west
        if (currentCell + 1 < totalCell && (currentCell + 1) != check)
        {
            if (cells[currentCell + 1].visited == false)
            {
                neighbors[length] = currentCell + 1;
                connectingWall[length] = 3;
                length++;
            }
        }
        //east
        if (currentCell - 1 >= 0 && currentCell != check)
        {
            if (cells[currentCell - 1].visited == false)
            {
                neighbors[length] = currentCell - 1;
                connectingWall[length] = 2;
                length++;
            }
        }

        //north
        if (currentCell + xSize < totalCell)
        {
            if (cells[currentCell + xSize].visited == false)
            {
                neighbors[length] = currentCell + xSize;
                connectingWall[length] = 1;
                length++;
            }
        }

        //south
        if (currentCell - xSize >= 0)
        {
            if (cells[currentCell - xSize].visited == false)
            {
                neighbors[length] = currentCell - xSize;
                connectingWall[length] = 4;
                length++;
            }
        }

        if (length != 0)
        {
            int theChosenOne = Random.Range(0, length);
            currentNeighbor = neighbors[theChosenOne];
            wallToBreak = connectingWall[theChosenOne];
        }
        else
        {
            if (backingUp > 0)
            {
                currentCell = lastCells[backingUp];
                backingUp--;
            }
        }

    }

    // Update is called once per frame
    void Update () {
		
	}
}
