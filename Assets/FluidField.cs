using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FluidField : MonoBehaviour
{
    [SerializeField]
    TMP_InputField viscosityInput;
    [SerializeField]
    TMP_InputField diffusionInput;
    [SerializeField]
    TMP_InputField scaleInput;
    [SerializeField]
    Toggle turbineToggle;
    [SerializeField]
    Toggle flowToggle;

    // Prefabs
    [SerializeField]
    GameObject point;
    [SerializeField]
    GameObject cell;
    [SerializeField]
    GameObject obstacle;
    [SerializeField]
    GameObject turbine;

    [SerializeField]
    bool isExperiment = false;

    // GameOject Arrays
    static GameObject[] points;
    static GameObject[] cells;
    static List<GameObject> obstacles = new List<GameObject>();

    // Velocity attributes
    static Vector2[] velocities;
    static Vector2[] pressures;
    static Vector2[] forces;
    // Dye attributes
    static float[] dye;
    static float[] sources;
    // solid attributes
    static int[] solidCells;
    static Vector2[] solidVelocities;
    static Vector2[] solidForces;

    // Fluid attributes
    [SerializeField]
    public float viscosity;
    [SerializeField]
    public float diffusion;

    static bool flow = false;

    // Simulation size attributes
    [SerializeField]
    public int scale;
    static int N;
    static int numPoints;

    // Shader attributes
    [SerializeField]
    ComputeShader Shader;
    static int kernel;
    // Shader buffers
    static ComputeBuffer oldVelocityBuffer;
    static ComputeBuffer newVelocityBuffer;
    static ComputeBuffer pressureBuffer;
    static ComputeBuffer forceBuffer;
    static ComputeBuffer solidBuffer;
    static ComputeBuffer solidVelocityBuffer;
    static ComputeBuffer solidForceBuffer;
    static ComputeBuffer oldDyeBuffer;
    static ComputeBuffer newDyeBuffer;
    static ComputeBuffer sourceBuffer;

    static public int index(int x, int y)
    {
        return ((x)+(N+2)*(y));
    }

    // Start is called before the first frame update
    void Start()
    {
        SetAttributes();
    }

    void SetAttributes()
    {
        // set size attributes
        N = scale * 8;
        numPoints = (N+2) * (N+2);
        
        points = new GameObject[numPoints];
        cells = new GameObject[numPoints];

        // instantiate points and cells
        for (int y=0; y<N+2; y++)
        {
            for (int x=0; x<N+2; x++)
            {
                GameObject newPoint = Instantiate(point, new Vector2(x,y), Quaternion.identity);
                GameObject newCell = Instantiate(cell, new Vector2(x,y), Quaternion.identity);

                newPoint.transform.parent = transform;
                newCell.transform.parent = transform;

                int i = index(x, y);

                points[i] = newPoint;
                cells[i] = newCell;
            }
        }
        
        // scale and position camera
        Camera.main.orthographicSize = (N+2) / 1.75f;
        Camera.main.transform.position = new Vector3((N+1)*0.5f, (N+1)*0.5f, -10);

        // instantiate simulation fields
        velocities = new Vector2[numPoints];
        pressures = new Vector2[numPoints];
        forces = new Vector2[numPoints];
        dye = new float[numPoints];
        sources = new float[numPoints];
        solidCells = new int[numPoints];
        solidVelocities = new Vector2[numPoints];
        solidForces = new Vector2[numPoints];

        // instantiate shader buffers
        oldVelocityBuffer = new ComputeBuffer(numPoints, sizeof(float) * 2);
        newVelocityBuffer = new ComputeBuffer(numPoints, sizeof(float) * 2);
        pressureBuffer = new ComputeBuffer(numPoints, sizeof(float) * 2);
        forceBuffer = new ComputeBuffer(numPoints, sizeof(float) * 2);
        solidBuffer = new ComputeBuffer(numPoints, sizeof(int));
        solidVelocityBuffer = new ComputeBuffer(numPoints, sizeof(float) * 2);
        solidForceBuffer = new ComputeBuffer(numPoints, sizeof(float) * 2);
        oldDyeBuffer = new ComputeBuffer(numPoints, sizeof(float));
        newDyeBuffer = new ComputeBuffer(numPoints, sizeof(float));
        sourceBuffer = new ComputeBuffer(numPoints, sizeof(float));

        // set shader constants
        Shader.SetInt("N", N);
        Shader.SetFloat("viscosity", viscosity);
        Shader.SetFloat("diffusion", diffusion);
        Shader.SetFloat("deltaTime", Time.fixedDeltaTime);

        if (!isExperiment)
        {
            if (turbineToggle.isOn)
            {
                AddTurbine();
            }

            if (flowToggle.isOn)
            {
                flow = true;
            }
            else
            {
                flow = false;
            }
        }

        for (int i=0; i<numPoints; i++)//remove
        {
            solidCells[i] = -1;
        }
    }

    public void ResetAttributes()
    {
        ResetAttributes(float.Parse(viscosityInput.text), float.Parse(diffusionInput.text), int.Parse(scaleInput.text));
    }

    public void ResetAttributes(float newViscosity, float newDiffusion, int newScale)
    {
        viscosity = newViscosity;
        diffusion = newDiffusion;
        scale = newScale;

        oldVelocityBuffer.Dispose();
        newVelocityBuffer.Dispose();
        pressureBuffer.Dispose();
        forceBuffer.Dispose();
        solidBuffer.Dispose();
        solidVelocityBuffer.Dispose();
        solidForceBuffer.Dispose();
        oldDyeBuffer.Dispose();
        newDyeBuffer.Dispose();
        sourceBuffer.Dispose();

        for (int i=0; i<numPoints; i++)
        {
            Destroy(cells[i]);
            Destroy(points[i]);
        }

        for (int i=0; i<obstacles.Count; i++)
        {
            Destroy(obstacles[i]);
        }

        obstacles = new List<GameObject>();

        SetAttributes();
    }

    public static void ResetSimulation()
    {
        //instantiate simulation fields
        velocities = new Vector2[numPoints];
        pressures = new Vector2[numPoints];
        forces = new Vector2[numPoints];
        dye = new float[numPoints];
        sources = new float[numPoints];
        solidCells = new int[numPoints];
        // solidVolumes = new float[numPoints];
        solidVelocities = new Vector2[numPoints];
        solidForces = new Vector2[numPoints];

        obstacles = new List<GameObject>();

        for (int i=0; i<numPoints; i++)//remove
        {
            solidCells[i] = -1;
        }
    }

    public static void SetSolidCell(int i, int val)
    {
        solidCells[i] = val;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1) && !isExperiment)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            int x = (int)mousePos.x;
            int y = (int)mousePos.y;
            
            if (x >= 3 && x <= N - 2 && y >= 3 && y <= N - 2 )
            {
                AddObstacle(x, y, 4, 4);
            }
        }

        for (int i=0; i<numPoints; i++)
        {
            if (solidCells[i] != -1)
            {
                Vector2 velocity = solidForces[i]*20;
                Debug.DrawLine(points[i].transform.position, points[i].transform.position + new Vector3(velocity.x, velocity.y, 0)*0.5f, Color.black);
                points[i].GetComponent<SpriteRenderer>().color = Color.black;
            }
            else
            {
                Vector2 velocity = velocities[i]*20;
                Debug.DrawLine(points[i].transform.position, points[i].transform.position + new Vector3(velocity.x, velocity.y, 0)*0.5f, Color.red);
                points[i].GetComponent<SpriteRenderer>().color = Color.red;
            }

            float quantity = dye[i];
            if (solidCells[i] != -1)
            {
                cells[i].GetComponent<SpriteRenderer>().color = new Color(1-quantity, 1, 0);
            }
            else
            {
                cells[i].GetComponent<SpriteRenderer>().color = new Color(1-quantity, 1-quantity, 1-quantity);
            }
        }
    }

    void FixedUpdate()
    {
        solidBuffer.SetData(solidCells);

        // solid step
        GetSolidCells();
        SetSolidVelocities();

        solidVelocityBuffer.SetData(solidVelocities);

        // velocity step
        GetForces();
        AddForces();

        DiffuseVelocity();
        ProjectVelocity();
        // AddSolidForces();

        AdvectVelocity();
        ProjectVelocity();
        AddSolidForces();

        // dye step
        GetSources();
        AddSources();
        DiffuseDye();
        AdvectDye();

        for (int i=0; i<numPoints; i++)
        {
            solidCells[i] = -1;
        }
        solidBuffer.SetData(solidCells);
    }

    void GetSolidCells()
    {
        for (int i=0; i<numPoints; i++)
        {
            solidCells[i] = -1;
            for (int o = 0; o < obstacles.Count; o++)
            {
                if (obstacles[o].GetComponent<Collider2D>().OverlapPoint((Vector2)points[i].transform.position))
                {
                    solidCells[i] = (o);
                }
            }
        }

        solidBuffer.SetData(solidCells);
    }

    static void SetSolidVelocities()
    {
        for (int i=0; i<numPoints; i++)
        {
            if (solidCells[i] != -1)
            {
                solidVelocities[i] = (Vector2)obstacles[solidCells[i]].GetComponent<Rigidbody2D>().GetPointVelocity(points[i].transform.position) / N;
                velocities[i] = new Vector2(0, 0);//solidVelocities[i];
            }
            else
            {
                solidVelocities[i] = new Vector2(0, 0);
            }
        }

        solidVelocityBuffer.SetData(solidVelocities);
    }

    static void AddSolidForces()
    {
        for (int i=0; i<numPoints; i++)
        {
            if (solidCells[i] != -1)
            {
                float mass = obstacles[solidCells[i]].GetComponent<Rigidbody2D>().mass;
                obstacles[solidCells[i]].GetComponent<Rigidbody2D>().AddForceAtPosition(((solidForces[i])) * N * Time.fixedDeltaTime, points[i].transform.position, ForceMode2D.Impulse);
            }
        }
    }

    static void GetForces()
    {
        forces = new Vector2[numPoints];

        if (flow)
        {
            int min = (int)(N/2)-1;
            for(int y = min; y < min + 2; y++)
            {
                for(int x = min + 8 + 5; x < min + 2 + 8 + 5; x++)
                {
                    forces[index(x,y)] += new Vector2(-15, 0);
                }
            }
        }

        forceBuffer.SetData(forces);
    }

    void AddForces()
    {
        oldVelocityBuffer.SetData(velocities);

        kernel = Shader.FindKernel("addForce");
        Shader.SetBuffer(kernel, "force", forceBuffer);
        Shader.SetBuffer(kernel, "oldVelocity", oldVelocityBuffer);
        Shader.SetBuffer(kernel, "newVelocity", newVelocityBuffer);
        Shader.Dispatch(kernel, scale, scale, 1);
        newVelocityBuffer.GetData(velocities);
    }

    void DiffuseVelocity()
    {
        oldVelocityBuffer.SetData(velocities);

        kernel = Shader.FindKernel("diffuseVelocity");
        Shader.SetBuffer(kernel, "oldVelocity", oldVelocityBuffer);
        Shader.SetBuffer(kernel, "newVelocity", newVelocityBuffer);
        Shader.SetBuffer(kernel, "solidCell", solidBuffer);
        Shader.SetBuffer(kernel, "solidVelocity", solidVelocityBuffer);
        for (int i=0; i<20; i++)
        {
            Shader.Dispatch(kernel, scale, scale, 1);
            newVelocityBuffer.GetData(velocities);
            SetVelocityBoundary();
            newVelocityBuffer.SetData(velocities);
        }
    }

    void AdvectVelocity()
    {
        //swap velocities
        oldVelocityBuffer.SetData(velocities);

        kernel = Shader.FindKernel("advectVelocity");
        Shader.SetBuffer(kernel, "oldVelocity", oldVelocityBuffer);
        Shader.SetBuffer(kernel, "newVelocity", newVelocityBuffer);
        Shader.SetBuffer(kernel, "solidCell", solidBuffer);
        Shader.SetBuffer(kernel, "solidVelocity", solidVelocityBuffer);
		Shader.Dispatch(kernel, scale, scale, 1);
        newVelocityBuffer.GetData(velocities);
        SetVelocityBoundary();
        newVelocityBuffer.SetData(velocities);
    }

    static void SetVelocityBoundary(int mode = 0)
    {
        for (int i=1 ; i<=N ; i++)
        {
            velocities[index(0, i)] = new Vector2(-velocities[index(1, i)].x, velocities[index(1, i)].y);
            velocities[index(N+1, i)] = new Vector2(-velocities[index(N, i)].x, velocities[index(N, i)].y);
            velocities[index(i, 0 )] = new Vector2(velocities[index(i, 1)].x, -velocities[index(i, 1)].y);
            velocities[index(i, N+1)] = new Vector2(velocities[index(i, N)].x, -velocities[index(i, N)].y);
        }

        if (mode == 0)
        {
            velocities[index(0, 0 )] = 0.5f * (velocities[index(1, 0 )] + velocities[index(0, 1)]);
            velocities[index(0, N+1)] = 0.5f * (velocities[index(1, N+1)] + velocities[index(0, N )]);
            velocities[index(N+1, 0 )] = 0.5f * (velocities[index(N, 0 )] + velocities[index(N+1, 1)]);
            velocities[index(N+1, N+1)] = 0.5f * (velocities[index(N, N+1)] + velocities[index(N+1, N )]);
        }
    }

    void ProjectVelocity()
    {
        solidForces = new Vector2[numPoints];
        solidForceBuffer.SetData(solidForces);

        kernel = Shader.FindKernel("solvePressure");
        Shader.SetBuffer(kernel, "pressure", pressureBuffer);
        Shader.SetBuffer(kernel, "newVelocity", newVelocityBuffer);
        Shader.SetBuffer(kernel, "solidCell", solidBuffer);
        Shader.SetBuffer(kernel, "solidVelocity", solidVelocityBuffer);
        Shader.SetBuffer(kernel, "solidForce", solidForceBuffer);
		Shader.Dispatch(kernel, scale, scale, 1);
        solidForceBuffer.GetData(solidForces);

        pressureBuffer.GetData(pressures);
        SetPressureBoundary();
        pressureBuffer.SetData(pressures);

        kernel = Shader.FindKernel("solveGradient");
        Shader.SetBuffer(kernel, "pressure", pressureBuffer);
        Shader.SetBuffer(kernel, "newVelocity", newVelocityBuffer);
        Shader.SetBuffer(kernel, "solidCell", solidBuffer);
        Shader.SetBuffer(kernel, "solidVelocity", solidVelocityBuffer);
        Shader.SetBuffer(kernel, "solidForce", solidForceBuffer);
        for (int i=0; i<20; i++)
        {
            Shader.Dispatch(kernel, scale, scale, 1);
            pressureBuffer.GetData(pressures);
            SetPressureBoundary(true);
            pressureBuffer.SetData(pressures);
        }
        solidForceBuffer.GetData(solidForces);

        kernel = Shader.FindKernel("solveIncompressible");
        Shader.SetBuffer(kernel, "pressure", pressureBuffer);
        Shader.SetBuffer(kernel, "newVelocity", newVelocityBuffer);
        Shader.SetBuffer(kernel, "solidCell", solidBuffer);
        Shader.SetBuffer(kernel, "solidVelocity", solidVelocityBuffer);
        Shader.SetBuffer(kernel, "solidForce", solidForceBuffer);
        Shader.Dispatch(kernel, scale, scale, 1);
        newVelocityBuffer.GetData(velocities);
        solidForceBuffer.GetData(solidForces);

        SetVelocityBoundary();
    }
    
    static void SetPressureBoundary(bool skipY = false)
    {
        for (int i=1 ; i<=N ; i++)
        {
            pressures[index(0, i)].x = pressures[index(1, i)].x;
            pressures[index(N+1, i)].x = pressures[index(N, i)].x;
            pressures[index(i, 0 )].x = pressures[index(i, 1)].x;
            pressures[index(i, N+1)].x = pressures[index(i, N)].x;
            if (!skipY)
            {
                pressures[index(0, i)].y = pressures[index(1, i)].y;
                pressures[index(N+1, i)].y = pressures[index(N, i)].y;
                pressures[index(i, 0 )].y = pressures[index(i, 1)].y;
                pressures[index(i, N+1)].y = pressures[index(i, N)].y;
            }
        }

        pressures[index(0, 0 )] = 0.5f * (pressures[index(1, 0 )] + pressures[index(0, 1)]);
        pressures[index(0, N+1)] = 0.5f * (pressures[index(1, N+1)] + pressures[index(0, N )]);
        pressures[index(N+1, 0 )] = 0.5f * (pressures[index(N, 0 )] + pressures[index(N+1, 1)]);
        pressures[index(N+1, N+1)] = 0.5f * (pressures[index(N, N+1)] + pressures[index(N+1, N )]);
    }

    static void GetSources()
    {
        sources = new float[numPoints];

        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            int xMin = (int)mousePos.x - 1;
            int yMin = (int)mousePos.y - 1;
            
            if (xMin >= 1 && xMin <= N - 1 && yMin >= 1 && yMin <= N - 1 )
            {
            
                for(int y = yMin; y < yMin + 2; y++)
                {
                    for(int x = xMin; x < xMin + 2; x++)
                    {
                        sources[index(x,y)] += 20;
                    }
                }
            }
        }

        sourceBuffer.SetData(sources);
    }

    void AddSources()
    {
        oldDyeBuffer.SetData(dye);

        kernel = Shader.FindKernel("addDye");
        Shader.SetBuffer(kernel, "source", sourceBuffer);
        Shader.SetBuffer(kernel, "oldDye", oldDyeBuffer);
        Shader.SetBuffer(kernel, "newDye", newDyeBuffer);
        Shader.Dispatch(kernel, scale, scale, 1);
        newDyeBuffer.GetData(dye);
    }

    void DiffuseDye()
    {
        oldDyeBuffer.SetData(dye);

        kernel = Shader.FindKernel("diffuseDye");
        Shader.SetBuffer(kernel, "oldDye", oldDyeBuffer);
        Shader.SetBuffer(kernel, "newDye", newDyeBuffer);
        Shader.SetBuffer(kernel, "solidCell", solidBuffer);
        for (int i=0; i<20; i++)
        {
            Shader.Dispatch(kernel, scale, scale, 1);
            newDyeBuffer.GetData(dye);
            SetDyeBoundary();
            newDyeBuffer.SetData(dye);
        }
    }

    void AdvectDye()
    {
        oldDyeBuffer.SetData(dye);

        kernel = Shader.FindKernel("advectDye");
        Shader.SetBuffer(kernel, "oldDye", oldDyeBuffer);
        Shader.SetBuffer(kernel, "newDye", newDyeBuffer);
        Shader.SetBuffer(kernel, "newVelocity", newVelocityBuffer);
		Shader.Dispatch(kernel, scale, scale, 1);
        newDyeBuffer.GetData(dye);
        SetDyeBoundary();
        newDyeBuffer.SetData(dye);
    }

    static void SetDyeBoundary()
    {
        for (int i=1; i<=N ; i++)
        {
            dye[index(0, i)] = dye[index(1, i)];
            dye[index(N+1, i)] = dye[index(N, i)];
            dye[index(i, 0 )] = dye[index(i, 1)];
            dye[index(i, N+1)] = dye[index(i, N)];
        }

        dye[index(0, 0 )] = 0.5f * (dye[index(1, 0 )] + dye[index(0, 1)]);
        dye[index(0, N+1)] = 0.5f * (dye[index(1, N+1)] + dye[index(0, N )]);
        dye[index(N+1, 0 )] = 0.5f * (dye[index(N, 0 )] + dye[index(N+1, 1)]);
        dye[index(N+1, N+1)] = 0.5f * (dye[index(N, N+1)] + dye[index(N+1, N )]);
    }

    void AddObstacle(float x, float y, float width, float height)
    {
        GameObject newObstacle = Instantiate(obstacle, new Vector2(x,y), Quaternion.identity);
        newObstacle.transform.parent = transform;
        newObstacle.transform.localScale = new Vector3(width, height, 1);

        newObstacle.GetComponent<Rigidbody2D>().mass = width * height;

        obstacles.Add(newObstacle);
    }

    public static void AddCreature(GameObject creature)
    {
        Creature creatureData = creature.GetComponent<Creature>();

        for (int i=0; i<creatureData.limbs.Count; i++)
        {
            obstacles.Add(creatureData.limbs[i]);
        }

        for (int i=0; i<creatureData.nodes.Count; i++)
        {
            obstacles.Add(creatureData.nodes[i]);
        }
    }

    void AddTurbine()
    {
        GameObject newTurbine = Instantiate(turbine, GetCentre(), Quaternion.identity);
        newTurbine.transform.parent = transform;

        for (int i=0; i<newTurbine.transform.childCount; i++){
            GameObject newObstacle = newTurbine.transform.GetChild(i).gameObject;
            obstacles.Add(newObstacle);
        }
    }

    public static Vector2 GetCentre()
    {
        return new Vector2(0.5f*(N+1),0.5f*(N+1));
    }

    void OnDestroy()
    {
        oldVelocityBuffer.Dispose();
        newVelocityBuffer.Dispose();
        pressureBuffer.Dispose();
        forceBuffer.Dispose();
        solidBuffer.Dispose();
        solidVelocityBuffer.Dispose();
        solidForceBuffer.Dispose();
        oldDyeBuffer.Dispose();
        newDyeBuffer.Dispose();
        sourceBuffer.Dispose();
    }
}