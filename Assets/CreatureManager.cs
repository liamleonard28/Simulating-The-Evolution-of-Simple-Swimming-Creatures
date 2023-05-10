using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreatureManager : MonoBehaviour
{
    [SerializeField]
    TMP_InputField DNAInput;
    [SerializeField]
    TMP_InputField DNAOutput;
    [SerializeField]
    GameObject creature;

    Creature curCreature;

    public void Start()
    {
        curCreature = Instantiate(creature, new Vector2(0,0), Quaternion.identity).GetComponent<Creature>();
        curCreature.RandomiseDNA(-1);
    }

    public void LoadCreature()
    {
        curCreature.Despawn();
        FluidField.ResetSimulation();
        curCreature.SetDNA(DNAInput.text);
        DNAOutput.text = curCreature.GetDNAString();
        curCreature.Spawn();
    }

    public void RandomCreature()
    {
        curCreature.Despawn();
        FluidField.ResetSimulation();
        curCreature.RandomiseDNA(-1);
        DNAOutput.text = curCreature.GetDNAString();
        curCreature.Spawn();
    }

    public void MutateCreature()
    {
        curCreature.Despawn();
        FluidField.ResetSimulation();
        curCreature.Mutate(-1);
        DNAOutput.text = curCreature.GetDNAString();
        curCreature.Spawn();
    }
}
