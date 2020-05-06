using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GridEnvironment : Environment {

    //MAZE VARIABLES
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
    public int minStepsRequired=999999;
    private int totalCell;
    private int visitedCells = 0;
    private bool startedBuilding = false;
    private int currentNeighbor = 0;
    private List<int> lastCells;
    private List<float> visitedCell;
    private int backingUp = 0;
    public float initialX, initialY;
    private int wallToBreak;
    //END OF MAZE VARIABLES

    public List<GameObject> actorObjs;
    public string[] players;
    public GameObject visualAgent;
    int numGoals;
    int gridSize;
    int[] objectPositions;
    float episodeReward;

	// Use this for initialization
	void Start () {
        maxSteps = xSize*ySize*2;
        waitTime = 0.001f;
        BeginNewGame();	
	}
	
    //BeginNewGame makes a new grid to start learning process
    public void BeginNewGame()
    {
        print("OK");
        xSize = (int)GameObject.Find("xsizesetter").GetComponent<Slider>().value;
        ySize = (int)GameObject.Find("ysizesetter").GetComponent<Slider>().value;
        numGoals = 1;

        gridSize = xSize*ySize;
        if (GameObject.Find("Maze"))
        {
            DestroyImmediate(GameObject.Find("Maze"));
        }
        print("oK1");
        foreach(GameObject actor in actorObjs)
        {
            DestroyImmediate(actor);
        }
        print("All right!");
        SetUp();
        agent = new InternalAgent();
        agent.SendParameters(envParameters);
        Reset();
    }

    //SetUp will establish the grid
    public override void SetUp()
    {
        envParameters = new EnvironmentParameters()
        {
            observation_size = 0,
            state_size = gridSize,
            action_descriptions = new List<string>() { "Up", "Down", "Left", "Right" },
            action_size = 4,
            env_name = "GridWorld",
            action_space_type = "discrete",
            state_space_type = "discrete",
            num_agents = 1
        };

        List<string> playerList = new List<string>();
        actorObjs = new List<GameObject>();
        playerList.Add("Agent");
        for (int i = 0; i < numGoals; i++)
        {
            playerList.Add("goal");
        }
        players = playerList.ToArray();
        //Camera cam = GameObject.Find("Camera").GetComponent<Camera>();
        //cam.transform.position = new Vector3((gridSize - 1), gridSize, -(gridSize - 1) / 2f);
        //cam.orthographicSize = (gridSize + 5f) / 2f;
        CreateMaze(); //Used to create the Maze

    }

    // Update is called once per frame
    void Update () {
        waitTime = 1.0f - GameObject.Find("Slider").GetComponent<Slider>().value;
        RunMdp();
	}

    //collectState is used to get the current state of agent and transform it into a discrete
    //integer state and then return the state list
    //TBD
    public override List<float> collectState()
    {

        List<float> state = new List<float>();
        foreach(GameObject actor in actorObjs)
        {
            if(actor.tag == "Agent")
            {
                initialX = (-xSize /2.0f) + 0.5f + 0.5f * (xSize % 2);
                initialY = (-ySize /2.0f) + 0.5f * (ySize % 2);
                float pointX = actor.transform.position.x - initialX;
                float pointY = actor.transform.position.z - initialY;
                float point = pointY * xSize + pointX;
                state.Add(point);
                if (!visitedCell.Contains(point))
                {
                    visitedCell.Add(point);
                }
            }
        }
        return state;
    }

    //LoadSpheres is used to draw the value estimated spheres on grid 
    //(purely representational)

    public void LoadSpheres()
    {
        GameObject[] values = GameObject.FindGameObjectsWithTag("value");
        foreach(GameObject value in values)
        {
            Destroy(value);
        }

        float[] value_estimates = agent.GetValue();
        for(int i = 0; i < gridSize; i++)
        {
            GameObject value = (GameObject)GameObject.Instantiate(Resources.Load("value"));
            float x = (i%xSize) + initialX;
            float y = Mathf.Floor(i/xSize) + initialY;
            value.transform.position = new Vector3(x, 0.0f, y);
            value.transform.localScale = new Vector3(value_estimates[i] / 10f, value_estimates[i] / 10f, value_estimates[i] / 10f);
            if (value_estimates[i] < 0)
            {
                Material newMat = Resources.Load("negative_mat", typeof(Material)) as Material;
                value.GetComponent<Renderer>().material = newMat;
            }
        }
    }

    //Reset is used to reset the episode by placing the objects at their original positions.
    //reward = 0;
    //currentStep = 0;
    //episodeCount++;
    //done = false;
    //acceptingSteps = false;
    //All Above steps done by base.Reset();

    public override void Reset()
    {
        base.Reset();

        visitedCell = new List<float>();
        foreach (GameObject actor in actorObjs)
        {
            Destroy(actor);
        }
        actorObjs = new List<GameObject>();
        for(int i = 0; i < players.Length; i++)
        {
            float x = ((objectPositions[i]) / (2.0f*ySize)) + 0.5f*(-objectPositions[i]/gridSize) + 0.5f*(xSize%2);
            float y = ((objectPositions[i]) / (2.0f * xSize)) -1 + 0.5f * (ySize % 2);
            if (objectPositions[i] < 0)
            {
                y = ((objectPositions[i]) / (2.0f * xSize)) + 0.5f * (ySize % 2);
            }
            GameObject actorObj = (GameObject)GameObject.Instantiate(Resources.Load(players[i]));
            actorObj.transform.position = new Vector3(x, 0.0f, y);
            actorObjs.Add(actorObj);
            if(players[i] == "Agent")
            {
                visualAgent = actorObj;
            }
        }

        episodeReward = 0;
        EndReset();
    }

    //Middlestep is the most important function in the program
    //it allows the agent to take actions, and set rewards accordingly.
    public override void MiddleStep(int action)
    {
        reward = -0.05f;
        // 0 - Forward, 1 - Backward, 2 - Left, 3 - Right
        if (action == 3)
        {
            Collider[] blockTest = Physics.OverlapBox(new Vector3(visualAgent.transform.position.x + 0.75f, 0, visualAgent.transform.position.z), new Vector3(0.3f, 0.3f, 0.3f));
            if (blockTest.Where(col => col.gameObject.tag == "wall").ToArray().Length == 0)
            {
                visualAgent.transform.position = new Vector3(visualAgent.transform.position.x + 1, 0, visualAgent.transform.position.z);
            }
            else
            {
                reward = -0.5f;
            }

        }
        if (action == 2)
        {
            Collider[] blockTest = Physics.OverlapBox(new Vector3(visualAgent.transform.position.x - 0.75f, 0, visualAgent.transform.position.z), new Vector3(0.3f, 0.3f, 0.3f));
            if (blockTest.Where(col => col.gameObject.tag == "wall").ToArray().Length == 0)
            {
                visualAgent.transform.position = new Vector3(visualAgent.transform.position.x - 1, 0, visualAgent.transform.position.z);
            }
            else
            {
                reward = -0.5f;
            }

        }
        if (action == 0)
        {
            Collider[] blockTest = Physics.OverlapBox(new Vector3(visualAgent.transform.position.x, 0, visualAgent.transform.position.z+ 0.75f), new Vector3(0.3f, 0.3f, 0.3f));
            if (blockTest.Where(col => col.gameObject.tag == "wall").ToArray().Length == 0)
            {
                visualAgent.transform.position = new Vector3(visualAgent.transform.position.x, 0, visualAgent.transform.position.z+1);
            }
            else
            {
                reward = -0.5f;
            }

        }
        if (action == 1)
        {
            Collider[] blockTest = Physics.OverlapBox(new Vector3(visualAgent.transform.position.x, 0, visualAgent.transform.position.z - 0.75f), new Vector3(0.3f, 0.3f, 0.3f));
            if (blockTest.Where(col => col.gameObject.tag == "wall").ToArray().Length == 0)
            {
                visualAgent.transform.position = new Vector3(visualAgent.transform.position.x, 0, visualAgent.transform.position.z - 1);
            }
            else
            {
                reward = -0.5f;
            }
        }
        float pointX = visualAgent.transform.position.x - initialX;
        float pointY = visualAgent.transform.position.z - initialY;
        if (visitedCell.Contains(pointY * xSize + pointX))
        {
            reward = -0.3f;
        }
        Collider[] hitObjects = Physics.OverlapBox(visualAgent.transform.position, new Vector3(0.3f,0.3f,0.3f));
        if (hitObjects.Where(col => col.gameObject.tag == "goal").ToArray().Length == 1)
        {
            reward = 5;
            if (currentStep < minStepsRequired)
            {
                minStepsRequired = currentStep;
            }
            done = true;//end of episode
        }
        

        LoadSpheres();
        episodeReward += reward;
        GameObject.Find("Rtxt").GetComponent<Text>().text = "Episode Reward: " + episodeReward.ToString("F2");

    }
    //MAZE MAKING FUNCTIONS
    void CreateWalls()
    {
        wallHolder = new GameObject();
        wallHolder.name = "Maze";

        initialPos = new Vector3((-xSize / 2) + wallLength / 2, 0.0f, (-ySize / 2) + wallLength / 2);
        Vector3 myPos = initialPos;
        GameObject tempWall;

        GameObject.Find("Camera").transform.position = new Vector3(0, Mathf.Max(xSize, ySize), 0);

        for (int i = 0; i < ySize; i++)
        {
            for (int j = 0; j <= xSize; j++)
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
                tempWall = Instantiate(wall, myPos, Quaternion.Euler(0.0f, 90.0f, 0.0f)) as GameObject;
                tempWall.transform.parent = wallHolder.transform;
            }
        }
        print("Walls Created");

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
        for (int i = 0; i < children; i++)
        {
            allWalls[i] = wallHolder.transform.GetChild(i).gameObject;
        }

        for (int cellprocess = 0; cellprocess < cells.Length; cellprocess++)
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
        print("Cells Created");
    }

    void CreateMaze()
    {
        visitedCells = 0;
        CreateWalls();
        CreateCells();

        while (visitedCells < totalCell)
        {
            if (startedBuilding)
            {
                GiveMeNeighbor();
                if (cells[currentNeighbor].visited == false && cells[currentCell].visited == true)
                {
                    BreakWall();
                    print("Wall Broken");
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
        HashSet<int> numbers = new HashSet<int>();
        foreach(string player in players)
        {
            if (player == "Agent")
                numbers.Add(-gridSize);
            else
                numbers.Add(gridSize);
        }

        objectPositions = numbers.ToArray();
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
        check = ((currentCell + 1) / xSize);
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
}
