using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour
{
    [SerializeField]
    GameObject node;
    [SerializeField]
    GameObject limb;

    [SerializeField]
    public float creatureScale;

    [SerializeField]
    public CreatureDNA DNA;
    public List<GameObject> limbs;
    public List<GameObject> nodes;

    public List<Vector2> nodeStartPositions = new List<Vector2>();

    public Vector2 startCentreOfMass;
    public float fitness = 0;

    public void Spawn()
    {
        SpawnRecursively(FluidField.GetCentre(), Random.Range(0f, 360f), DNA.rootLimb);
        FluidField.AddCreature(gameObject);

        startCentreOfMass = GetCentreOfMass();
        Animate();
    }

    public void Display(Vector2 position)
    {
        SpawnRecursively(new Vector2(0, 0), 0, DNA.rootLimb);

        Vector2 centre = GetAveragePosition(GetNodePositions());
        
        Vector2 offset = position - centre;
        
        for (int i=0; i<limbs.Count; i++)
        {
            limbs[i].transform.position += (Vector3)offset;
        }

        for (int i=0; i<nodes.Count; i++)
        {
            nodes[i].transform.position += (Vector3)offset;
        }
    }

    public void Animate()
    {
        for (int i=0; i<limbs.Count; i++)
        {
            Movement movement = limbs[i].GetComponent<Movement>();

            if(movement != null)
            {
                movement.Animate();
            }
        }
    }

    void SpawnRecursively(Vector2 position, float angle, RootLimbDNA rootLimbDNA)
    {
        GameObject rootLimb = Instantiate(limb, position,  Quaternion.Euler(0, 0, angle));
        rootLimb.transform.localScale = new Vector3(creatureScale, rootLimbDNA.length * creatureScale, 1);
        rootLimb.GetComponent<Rigidbody2D>().mass = rootLimbDNA.length * creatureScale * creatureScale;
        rootLimb.transform.parent = transform;
        limbs.Add(rootLimb);

        Vector2 node1Position = position + (Vector2)rootLimb.transform.up * 0.5f * creatureScale * (1 + rootLimbDNA.length);
        GameObject node1 = Instantiate(node, node1Position,  Quaternion.Euler(0, 0, angle));
        node1.transform.localScale = new Vector3(creatureScale, creatureScale, 1);
        node1.GetComponent<Rigidbody2D>().mass = Mathf.PI * 0.25f * creatureScale * creatureScale;
        node1.transform.parent = transform;
        FixedJoint2D nodeJoint1 = node1.AddComponent<FixedJoint2D>();
        nodeJoint1.connectedBody = rootLimb.GetComponent<Rigidbody2D>();
        nodes.Add(node1);

        Vector2 node2Position = position - (Vector2)rootLimb.transform.up * 0.5f * creatureScale * (1 + rootLimbDNA.length);
        GameObject node2 = Instantiate(node, node2Position,  Quaternion.Euler(0, 0, angle + 180));
        node2.transform.localScale = new Vector3(creatureScale, creatureScale, 1);
        node2.GetComponent<Rigidbody2D>().mass = Mathf.PI * 0.25f * creatureScale * creatureScale;
        node2.transform.parent = transform;
        FixedJoint2D nodeJoint2 = node2.AddComponent<FixedJoint2D>();
        nodeJoint2.connectedBody = rootLimb.GetComponent<Rigidbody2D>();
        nodes.Add(node2);

        for (int i=0; i<rootLimbDNA.node1Children.Count; i++)
        {
            LimbDNA childLimbDNA = DNA.limbs[rootLimbDNA.node1Children[i]];
            Vector2 childPosition = node1Position + (Vector2)(Quaternion.Euler(0, 0, childLimbDNA.angle) * rootLimb.transform.up) * 0.5f * creatureScale * (1 + childLimbDNA.length);
            float childAngle = angle + childLimbDNA.angle;
            SpawnRecursively(childPosition, childAngle, childLimbDNA, rootLimb.GetComponent<Rigidbody2D>());
        }
        for (int i=0; i<rootLimbDNA.node2Children.Count; i++)
        {
            LimbDNA childLimbDNA = DNA.limbs[rootLimbDNA.node2Children[i]];
            Vector2 childPosition = node2Position + (Vector2)(Quaternion.Euler(0, 0, 180 + childLimbDNA.angle) * rootLimb.transform.up) * 0.5f * creatureScale * (1 + childLimbDNA.length);
            float childAngle = angle + 180 + childLimbDNA.angle;
            SpawnRecursively(childPosition, childAngle, childLimbDNA, rootLimb.GetComponent<Rigidbody2D>());
        }
    }

    void SpawnRecursively(Vector2 position, float angle, LimbDNA curLimbDNA, Rigidbody2D parentLimbBody)
    {
        GameObject curLimb = Instantiate(limb, position,  Quaternion.Euler(0, 0, angle));
        curLimb.transform.localScale = new Vector3(creatureScale, curLimbDNA.length * creatureScale, 1);
        curLimb.GetComponent<Rigidbody2D>().mass = curLimbDNA.length * creatureScale * creatureScale;
        curLimb.transform.parent = transform;

        if (curLimbDNA.hinged)
        {
            HingeJoint2D joint = curLimb.AddComponent<HingeJoint2D>();
            joint.connectedBody = parentLimbBody;
            joint.anchor = new Vector2(0, -(0.5f + 0.5f/(curLimbDNA.length * creatureScale)));
            joint.useMotor = true;
            Movement limbMovement = curLimb.AddComponent<Movement>();
            limbMovement.SetCycle(joint, curLimbDNA.cycle, creatureScale);
        }
        else
        {
            FixedJoint2D joint = curLimb.AddComponent<FixedJoint2D>();
            joint.connectedBody = parentLimbBody;
            joint.anchor = new Vector2(0, -(0.5f + 0.5f/(curLimbDNA.length * creatureScale)));
        }

        limbs.Add(curLimb);

        Vector2 nodePosition = position + (Vector2)curLimb.transform.up * 0.5f * creatureScale * (1 + curLimbDNA.length);
        GameObject curNode = Instantiate(node, nodePosition,  Quaternion.Euler(0, 0, angle));
        curNode.transform.localScale = new Vector3(creatureScale, creatureScale, 1);
        curNode.GetComponent<Rigidbody2D>().mass = Mathf.PI * 0.25f * creatureScale * creatureScale;
        curNode.transform.parent = transform;
        FixedJoint2D nodeJoint = curNode.AddComponent<FixedJoint2D>();
        nodeJoint.connectedBody = curLimb.GetComponent<Rigidbody2D>();
        nodes.Add(curNode);

        for (int i=0; i<curLimbDNA.children.Count; i++)
        {
            LimbDNA childLimbDNA = DNA.limbs[curLimbDNA.children[i]];
            Vector2 childPosition = nodePosition + (Vector2)(Quaternion.Euler(0, 0, childLimbDNA.angle) * curLimb.transform.up) * 0.5f * creatureScale * (1 + childLimbDNA.length);
            float childAngle = angle + childLimbDNA.angle;

            SpawnRecursively(childPosition, childAngle, childLimbDNA, curLimb.GetComponent<Rigidbody2D>());
        }
    }

    public void Despawn()
    {
        fitness = (GetCentreOfMass() - startCentreOfMass).magnitude;

        for (int i=0; i<limbs.Count; i++)
        {
            Destroy(limbs[i]);
        }

        for (int i=0; i<nodes.Count; i++)
        {
            Destroy(nodes[i]);
        }

        limbs = new List<GameObject>();
        nodes = new List<GameObject>();
    }

    public void Kill()
    {
        Destroy(gameObject);
    }

    public CreatureDNA GetDNA()
    {
        return DNA;
    }

    public string GetDNAString()
    {
        return JsonUtility.ToJson(DNA);
    }

    public void SetDNA(string newDNA)
    {
        newDNA.Replace(" ", "");
        newDNA.Replace("\n", "");
        SetDNA(JsonUtility.FromJson<CreatureDNA>(newDNA));
    }

    public void SetDNA(CreatureDNA newDNA)
    {
        DNA = newDNA;
    }

    public void RandomiseDNA(int species)
    {
        DNA = new CreatureDNA();

        DNA.species = species;

        DNA.rootLimb = new RootLimbDNA();
        DNA.rootLimb.length = Random.Range(2f, 10f);

        int numLimbs = Random.Range(3, 6);
        for (int i=1; i<numLimbs; i++)
        {
            int parentLimb = Random.Range(-2, DNA.limbs.Count);
            if (parentLimb == -2)
            {
                DNA.rootLimb.node1Children.Add(DNA.limbs.Count);
                DNA.limbs.Add(RandomLimb(-2));
            }
            else if (parentLimb == -1)
            {
                DNA.rootLimb.node2Children.Add(DNA.limbs.Count);
                DNA.limbs.Add(RandomLimb(-1));
            }
            else
            {
                DNA.limbs[parentLimb].children.Add(DNA.limbs.Count);
                DNA.limbs.Add(RandomLimb(parentLimb));
            }
        }
    }

    LimbDNA RandomLimb(int parentLimb)
    {
        LimbDNA newLimb = new LimbDNA();

        newLimb.parent = parentLimb;
        newLimb.length = Random.Range(2f, 10f);
        newLimb.angle = Random.Range(-180f, 180f);
        
        if (Random.Range(0, 2) == 0)
        {
            newLimb.hinged = true;

            int numMovements = Random.Range(1, 6);
            for (int i=0; i<numMovements; i++)
            {
                newLimb.cycle.Add(RandomMovement());
            }

        }
        else
        {
            newLimb.hinged = false;
        }

        return newLimb;
    }

    MovementDNA RandomMovement()
    {
        MovementDNA newMovement = new MovementDNA();

        newMovement.speed = Random.Range(-50f, 50f);
        newMovement.duration = Random.Range(1, 100);

        return newMovement;
    }

    public void Mutate(int species)
    {
        DNA.ancestor = DNA.species;
        DNA.species = species;

        int mutation = Random.Range(0, 16);

        string before = GetDNAString();

        if (mutation <= 1)
        {
            MutateGainLimb(Random.Range(-2, DNA.limbs.Count));
        }
        else if (mutation == 2 && DNA.limbs.Count > 1)
        {
            MutateLoseLimb(Random.Range(0, DNA.limbs.Count));
        }
        else if (mutation == 3 && DNA.limbs.Count > 1)
        {
            MutateLoseLimbSegment(Random.Range(0, DNA.limbs.Count));
        }
        else if (mutation <= 9)
        {
            MutateLimbLength(Random.Range(-1, DNA.limbs.Count));
        }
        else if (mutation <= 15)
        {
            MutateJoint(Random.Range(0, DNA.limbs.Count));
        }

        if (DNA.limbs.Count < 2)
        {
            SetDNA(before);
        }
    }

    void MutateGainLimb(int parentLimb)
    {
        if (parentLimb == -2)
        {
            DNA.rootLimb.node1Children.Add(DNA.limbs.Count);
            DNA.limbs.Add(RandomLimb(-2));
        }
        else if (parentLimb == -1)
        {
            DNA.rootLimb.node2Children.Add(DNA.limbs.Count);
            DNA.limbs.Add(RandomLimb(-1));
        }
        else
        {
            DNA.limbs[parentLimb].children.Add(DNA.limbs.Count);
            DNA.limbs.Add(RandomLimb(parentLimb));
        }
    }

    void MutateLoseLimbSegment(int childSegment)
    {
        int parentSegment = DNA.limbs[childSegment].parent;
        RecursivelyRemoveSegmentAndResetLimbs(parentSegment, childSegment);
    }

    void MutateLoseLimb(int limbToLose)
    {
        RecursivelyRemoveAndResetLimbs(limbToLose);
    }

    void RecursivelyRemoveAndResetLimbs(int limbToRemove)
    {
        List<LimbDNA> oldLimbs = DNA.limbs;
        DNA.limbs = new List<LimbDNA>();

        List<int> oldNode1Children = DNA.rootLimb.node1Children;
        DNA.rootLimb.node1Children = new List<int>();
        oldNode1Children.Remove(limbToRemove);
        List<int> oldNode2Children = DNA.rootLimb.node2Children;
        DNA.rootLimb.node2Children = new List<int>();
        oldNode2Children.Remove(limbToRemove);

        for (int i=0; i<oldNode1Children.Count; i++)
        {
            LimbDNA childLimbDNA = oldLimbs[oldNode1Children[i]];
            childLimbDNA.parent = -2;
            DNA.rootLimb.node1Children.Add(RecursivelyRemoveAndResetLimbs(oldLimbs, limbToRemove, childLimbDNA));
        }
        for (int i=0; i<oldNode2Children.Count; i++)
        {
            LimbDNA childLimbDNA = oldLimbs[oldNode2Children[i]];
            childLimbDNA.parent = -1;
            DNA.rootLimb.node2Children.Add(RecursivelyRemoveAndResetLimbs(oldLimbs, limbToRemove, childLimbDNA));
        }
    }

    int RecursivelyRemoveAndResetLimbs(List<LimbDNA> oldLimbs, int limbToRemove, LimbDNA curLimbDNA)
    {
        List<int> oldChildren = curLimbDNA.children;
        curLimbDNA.children = new List<int>();
        int limbIndex = DNA.limbs.Count;
        DNA.limbs.Add(curLimbDNA);

        oldChildren.Remove(limbToRemove);

        for (int i=0; i<oldChildren.Count; i++)
        {
            LimbDNA childLimbDNA = oldLimbs[oldChildren[i]];
            childLimbDNA.parent = limbIndex;
            DNA.limbs[limbIndex].children.Add(RecursivelyRemoveAndResetLimbs(oldLimbs, limbToRemove, childLimbDNA));
        }

        return limbIndex;
    }

    void RecursivelyRemoveSegmentAndResetLimbs(int parentSegment, int childSegment)
    {
        List<LimbDNA> oldLimbs = DNA.limbs;
        DNA.limbs = new List<LimbDNA>(); 

        List<int> oldNode1Children = DNA.rootLimb.node1Children;
        DNA.rootLimb.node1Children = new List<int>();
        List<int> oldNode2Children = DNA.rootLimb.node2Children;
        DNA.rootLimb.node2Children = new List<int>();

        if (parentSegment == -2)
        {
            DNA.rootLimb.length = oldLimbs[childSegment].length;
            oldNode1Children = oldLimbs[childSegment].children;
        }
        else if (parentSegment == -1)
        {
            DNA.rootLimb.length = oldLimbs[childSegment].length;
            oldNode2Children = oldLimbs[childSegment].children;
        }

        for (int i=0; i<oldNode1Children.Count; i++)
        {
            LimbDNA childSegmentDNA;
            if (oldNode1Children[i] == parentSegment)
            {
                childSegmentDNA = oldLimbs[childSegment];
            }
            else
            {
                childSegmentDNA = oldLimbs[oldNode1Children[i]];
            }
            childSegmentDNA.parent = -2;
            int newIndex = RecursivelyRemoveSegmentAndResetLimbs(oldLimbs, parentSegment, childSegment, childSegmentDNA);
            DNA.rootLimb.node1Children.Add(newIndex);
        }


        for (int i=0; i<oldNode2Children.Count; i++)
        {
            LimbDNA childSegmentDNA;
            if (oldNode2Children[i] == parentSegment)
            {
                childSegmentDNA = oldLimbs[childSegment];
            }
            else
            {
                childSegmentDNA = oldLimbs[oldNode2Children[i]];
            }
            childSegmentDNA.parent = -1;
            int newIndex = RecursivelyRemoveSegmentAndResetLimbs(oldLimbs, parentSegment, childSegment, childSegmentDNA);
            DNA.rootLimb.node2Children.Add(newIndex);
        }
    }

    int RecursivelyRemoveSegmentAndResetLimbs(List<LimbDNA> oldLimbs, int parentSegment, int childSegment, LimbDNA curSegmentDNA)
    {
        List<int> oldChildren = curSegmentDNA.children;
        curSegmentDNA.children = new List<int>();
        int segmentIndex = DNA.limbs.Count;
        DNA.limbs.Add(curSegmentDNA);

        for (int i=0; i<oldChildren.Count; i++)
        {
            LimbDNA childSegmentDNA;
            if (oldChildren[i] == parentSegment)
            {
                childSegmentDNA = oldLimbs[childSegment];
            }
            else
            {
                childSegmentDNA = oldLimbs[oldChildren[i]];
            }
            childSegmentDNA.parent = segmentIndex;
            int newIndex = RecursivelyRemoveSegmentAndResetLimbs(oldLimbs, parentSegment, childSegment, childSegmentDNA);
            DNA.limbs[segmentIndex].children.Add(newIndex);
        }

        return segmentIndex;
    }

    Vector2 GetCentreOfMass()
    {
        float massTotal = 0;
        Vector2 positionTotal = new Vector2(0, 0);

        for (int i=0; i<nodes.Count; i++)
        {
            if (nodes[i] != null)
            {
                float mass = nodes[i].GetComponent<Rigidbody2D>().mass;
                massTotal += mass;
                positionTotal += (Vector2)nodes[i].transform.position * mass;
            }
            
        }

        for (int i=0; i<limbs.Count; i++)
        {
            if (limbs[i] != null)
            {
                float mass = limbs[i].GetComponent<Rigidbody2D>().mass;
                massTotal += mass;
                positionTotal += (Vector2)limbs[i].transform.position * mass;
            }
        }

        if (massTotal == 0)
        {
            return Vector2.zero;
        }

        return positionTotal / massTotal;
    }

    void MutateLimbLength(int limb)
    {
        if (limb == -1)
        {
            DNA.rootLimb.length = Mathf.Max(1, DNA.rootLimb.length + Random.Range(-2f, 2f));
        }
        else
        {
            DNA.limbs[limb].length = Mathf.Max(1, DNA.limbs[limb].length + Random.Range(-2f, 2f));
        }
    }

    void MutateJoint(int limb)
    {
        int mutation;
        if (DNA.limbs[limb].hinged)
        {
            mutation = Random.Range(0, 4);
        }
        else
        {
            mutation = Random.Range(0, 1);
        }

        if (mutation == 0)
        {
            MutateJointType(limb);
        }
        else if (mutation == 1)
        {
            MutateJointAngle(limb);
        }
        else
        {
            MutateMovement(limb);
        }
    }

    void MutateJointType(int limb)
    {
        if (DNA.limbs[limb].hinged)
        {
            DNA.limbs[limb].hinged = false;
            DNA.limbs[limb].cycle = new List<MovementDNA>();
        }
        else
        {
            DNA.limbs[limb].hinged = true;

            int numMovements = Random.Range(1, 6);
            for (int i=0; i<numMovements; i++)
            {
                DNA.limbs[limb].cycle.Add(RandomMovement());
            }
        }
    }

    void MutateJointAngle(int limb)
    {
        DNA.limbs[limb].angle += Random.Range(-20f, 20f);
    }

    void MutateMovement(int limb)
    {
        int mutation = Random.Range(0, 7);
        
        if (mutation == 0)
        {
            MutateGainMovement(limb, Random.Range(0, DNA.limbs[limb].cycle.Count + 1));
        }
        else if (mutation == 1 && DNA.limbs[limb].cycle.Count > 1)
        {
            MutateLoseMovement(limb, Random.Range(0, DNA.limbs[limb].cycle.Count));
        }
        else if (mutation <= 3)
        {
            MutateMovementSpeed(limb, Random.Range(0, DNA.limbs[limb].cycle.Count));
        }
        else if (mutation <= 5)
        {
            MutateMovementDuration(limb, Random.Range(0, DNA.limbs[limb].cycle.Count));
        }
        else if (mutation <= 6)
        {
            MutateReorderMovement(limb, Random.Range(0, DNA.limbs[limb].cycle.Count), Random.Range(0, DNA.limbs[limb].cycle.Count));
        }
    }

    void MutateGainMovement(int limb, int stage)
    {
        DNA.limbs[limb].cycle.Insert(stage, RandomMovement());
    }

    void MutateLoseMovement(int limb, int stage)
    {
        DNA.limbs[limb].cycle.RemoveAt(stage);
    }

    void MutateMovementSpeed(int limb, int stage)
    {
        DNA.limbs[limb].cycle[stage].speed += Random.Range(-10f, 10f);
    }

    void MutateMovementDuration(int limb, int stage)
    {
        DNA.limbs[limb].cycle[stage].duration += Mathf.Max(1, Random.Range(-5, 6));
    }

    void MutateReorderMovement(int limb, int stage1, int stage2)
    {
        MovementDNA temp = DNA.limbs[limb].cycle[stage1];
        DNA.limbs[limb].cycle[stage1] = DNA.limbs[limb].cycle[stage2];
        DNA.limbs[limb].cycle[stage2] = temp;
    }

    public Vector2 GetAveragePosition(List<Vector2> positions)
    {
        Vector2 positionTotal = new Vector2(0, 0);

        for (int i=0; i<nodes.Count; i++)
        {
            positionTotal += positions[i];
        }

        return positionTotal / positions.Count;
    }

    List<Vector2> GetNodePositions()
    {
        List<Vector2> nodePositions = new List<Vector2>();

        for (int i=0; i<nodes.Count; i++)
        {
            nodePositions.Add((Vector2)nodes[i].transform.position);
        }

        return nodePositions;
    }

    void RecordNodeStartPositions()
    {
        nodeStartPositions = GetNodePositions();
    }

    public float GetMinimumDistanceTravelled(List<Vector2> startPositions, List<Vector2> endPositions)
    {
        Vector2 direction = (GetAveragePosition(endPositions) - GetAveragePosition(startPositions)).normalized;

        if (direction == Vector2.zero)
        {
            return 0;
        }

        float minimum = float.MaxValue;

        for (int i=0; i<endPositions.Count; i++)
        {
            Vector2 change = endPositions[i] - startPositions[i];
            float distance;
            if (direction.x == 0)
            {
                distance = change.y / direction.y;
            }
            else if (direction.y == 0)
            {
                distance = change.x / direction.x;
            }
            else
            {
                distance = Mathf.Min(change.x / direction.x, change.y / direction.y);
            }

            if (distance < minimum)
            {
                minimum = distance;
            }
        }

        return minimum;
    }
}

[System.Serializable]
public class CreatureDNA
{
    public int species = -1;
    public int ancestor = -1;
    public RootLimbDNA rootLimb = new RootLimbDNA();
    public List<LimbDNA> limbs = new List<LimbDNA>();
}

[System.Serializable]
public class RootLimbDNA
{
    public float length = 1;
    public List<int> node1Children = new List<int>();
    public List<int> node2Children = new List<int>();
}

[System.Serializable]
public class LimbDNA
{
    public float length = 1;
    public int parent = -1;
    public List<int> children = new List<int>();
    public float angle = 0;
    public bool hinged = false;
    public List<MovementDNA> cycle = new List<MovementDNA>();
}

[System.Serializable]
public class MovementDNA
{
    public float speed = 0;
    public float duration = 0;
}