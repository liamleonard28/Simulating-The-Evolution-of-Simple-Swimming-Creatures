using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantTorque : MonoBehaviour
{
    public float force = 1;
    Rigidbody2D body;
    // Start is called before the first frame update
    void Start()
    {
        body = gameObject.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        body.AddTorque(force * Time.fixedDeltaTime, ForceMode2D.Impulse);
    }
}
