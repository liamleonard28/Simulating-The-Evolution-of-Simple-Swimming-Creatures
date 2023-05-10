using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void OpenSimulationScene()
    {
        SceneManager.LoadScene("Simulation");
    }

    public static void OpenExperimentScene()
    {
        SceneManager.LoadScene("Experiment");
    }

    public static void OpenCreatureScene()
    {
        SceneManager.LoadScene("Creature");
    }

    public static void OpenMenuScene()
    {
        SceneManager.LoadScene("Menu");
    }
}
