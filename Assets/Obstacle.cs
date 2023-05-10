using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)){
            gameObject.GetComponent<Rigidbody2D>().AddForce(Vector3.up * Time.fixedDeltaTime * 200 * gameObject.GetComponent<Rigidbody2D>().mass);
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)){
            gameObject.GetComponent<Rigidbody2D>().AddForce(Vector3.up * Time.fixedDeltaTime * -200 * gameObject.GetComponent<Rigidbody2D>().mass);
        }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)){
            gameObject.GetComponent<Rigidbody2D>().AddForce(Vector3.right * Time.fixedDeltaTime * -200 * gameObject.GetComponent<Rigidbody2D>().mass);
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)){
            gameObject.GetComponent<Rigidbody2D>().AddForce(Vector3.right * Time.fixedDeltaTime * 200 * gameObject.GetComponent<Rigidbody2D>().mass);
        }
    }
}
