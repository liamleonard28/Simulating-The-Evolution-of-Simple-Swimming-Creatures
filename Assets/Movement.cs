using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField]
    List<MovementDNA> cycle;
    int stage = 0;
    int ticks = 0;
    HingeJoint2D joint;
    bool animated = false;
    float scale = 1;

    public void SetCycle(HingeJoint2D newJoint, List<MovementDNA> newCycle, float creatureScale)
    {
        joint = newJoint;
        cycle = newCycle;
        scale = creatureScale;
    }

    public void Animate()
    {
        animated = true;
        SetSpeed(cycle[0].speed);
    }

    public void SetSpeed(float speed)
    {   
        JointMotor2D motor = joint.motor;
        motor.maxMotorTorque = 100 * scale * scale;
        motor.motorSpeed = speed * scale;
        joint.motor = motor;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (animated)
        {
            if (ticks >= cycle[stage].duration)
            {
                ticks = 0;
                stage = (stage + 1) % cycle.Count;
                SetSpeed(cycle[stage].speed);
            }
            else
            {
                ticks++;
            }
        }
    }
}
