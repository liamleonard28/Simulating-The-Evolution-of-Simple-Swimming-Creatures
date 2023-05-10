using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Experiment : MonoBehaviour
{

    [SerializeField]
    FluidField fluid;
    
    [SerializeField]
    TMP_InputField populationInput;
    [SerializeField]
    TMP_InputField saveNameInput;
    [SerializeField]
    TMP_InputField loadNameInput;
    [SerializeField]
    TMP_InputField ticksInput;
    [SerializeField]
    TextMeshProUGUI experimentOutput;

    [SerializeField]
    string fileToRead;
    [SerializeField]
    string sampleDNA;

    [SerializeField]
    GameObject creature;
    [SerializeField]
    int populationSize = 10;
    List<Creature> population = new List<Creature>();

    [SerializeField]
    int currentCreature;
    int ticks;
    int numTicks = 500;
    [SerializeField]
    int generation;
    string experiment = "";

    List<CreatureDNA> species = new List<CreatureDNA>();

    bool inProgress = false;

    public void NewExperiment()
    {
        experiment = saveNameInput.text;
        currentCreature = 0;
        ticks = 0;
        generation = 0;
        populationSize = int.Parse(populationInput.text);
        numTicks = int.Parse(ticksInput.text);
        inProgress = false;
        RandomisePopulation();

        fluid.ResetAttributes();

        inProgress = true;
        population[0].Spawn();
    }

    void RandomisePopulation()
    {
        population = new List<Creature>();

        for (int i=0; i<populationSize; i++)
        {
            Creature newCreature = Instantiate(creature, new Vector2(0,0), Quaternion.identity).GetComponent<Creature>();
            newCreature.transform.parent = transform;
            newCreature.RandomiseDNA(i);
            species.Add(newCreature.GetDNA());
            population.Add(newCreature);
        }
    }

    void NewGeneration()
    {
        Debug.Log("new gen");
        generation++;
        
        for (int i=0; i<populationSize/2; i++)
        {
            population[i+populationSize/2].SetDNA(population[i].GetDNAString());
            Debug.Log("pre mute");
            population[i+populationSize/2].Mutate(species.Count);
            species.Add(population[i+populationSize/2].GetDNA());
            Debug.Log("post mute");
            Debug.Log("change: "+(population[i].DNA != population[i+populationSize/2].DNA));
        }
    }

    public void LoadExperiment()
    {
        for (int i=0; i<population.Count; i++)
        {
            population[i].Kill();
        }
        
        population = new List<Creature>();

        experiment = loadNameInput.text;

        currentCreature = 0;

        string text = System.IO.File.ReadAllText(@"./Experiments/" + experiment + @"/ExperimentData");
        ExperimentData experimentData = JsonUtility.FromJson<ExperimentData>(text);

        species = new List<CreatureDNA>();

        text = System.IO.File.ReadAllText(@"./Experiments/" + experiment + @"/Species");
        string[] lines = text.Split("\n");
        for (int i=0; i<lines.Length-1; i++)
        {
            species.Add(JsonUtility.FromJson<CreatureDNA>(lines[i]));
        }

        generation = experimentData.generation;

        text = System.IO.File.ReadAllText(@"./Experiments/" + experiment + @"/Generation" + generation.ToString());
        lines = text.Split("\n");
        for (int i=0; i<lines.Length-1; i+=2)
        {
            Creature newCreature = Instantiate(creature, new Vector2(0,0), Quaternion.identity).GetComponent<Creature>();
            newCreature.transform.parent = transform;
            newCreature.SetDNA(lines[i]);
            population.Add(newCreature);
        }

        populationSize = experimentData.population;
        numTicks = experimentData.ticks;

        NewGeneration();
        
        fluid.ResetAttributes(experimentData.viscosity, experimentData.diffusion, experimentData.scale);

        inProgress = true;
        population[0].Spawn();
    }

    void SaveExperiment()
    {
        ExperimentData experimentData = new ExperimentData();

        experimentData.viscosity = fluid.viscosity;
        experimentData.diffusion = fluid.diffusion;
        experimentData.scale = fluid.scale;
        experimentData.population = populationSize;
        experimentData.generation = generation;
        experimentData.ticks = numTicks;

        System.IO.Directory.CreateDirectory(@"./Experiments/" + experiment);

        string textForFile = JsonUtility.ToJson(experimentData);
        System.IO.File.WriteAllText(@"./Experiments/" + experiment + @"/ExperimentData", textForFile);

        textForFile = "";
        for (int i=0; i<species.Count; i++)
        {
            textForFile+=JsonUtility.ToJson(species[i])+"\n";
        }
        System.IO.File.WriteAllText(@"./Experiments/" + experiment + @"/Species", textForFile);

        textForFile = "";
        for (int i=0; i<populationSize; i++)
        {
            textForFile += population[i].GetDNAString() + "\n" + population[i].fitness.ToString() +" (Rank: " + i.ToString() + ")\n";
        }
        System.IO.File.WriteAllText(@"./Experiments/" + experiment + @"/Generation" + generation.ToString(), textForFile);
    }

    void FixedUpdate()
    {
        if (inProgress)
        {
            if (ticks < numTicks)
            {
                ticks++;
            }
            else
            {
                ticks = 0;
                if (currentCreature < populationSize - 1)
                {
                    population[currentCreature].Despawn();
                    currentCreature++;
                    FluidField.ResetSimulation();
                    population[currentCreature].Spawn();
                }
                else
                {
                    population[currentCreature].Despawn();
                    currentCreature = 0;
                    FluidField.ResetSimulation();

                    population.Sort((a,b) => b.fitness.CompareTo(a.fitness));
                    SaveExperiment();

                    NewGeneration();

                    population[0].Spawn();
                }
            }
        }
        experimentOutput.text = "Experiment: " + experiment + "\nCurrent Generation: " + generation.ToString() + "\nCurrent Creature: " + currentCreature.ToString() + "/" + populationSize.ToString();
    }
}

[System.Serializable]
public class ExperimentData
{
    public int scale = 1;
    public float viscosity = 0;
    public float diffusion = 0;
    public int population = 0;
    public int generation = 0;
    public int ticks = 0;
}