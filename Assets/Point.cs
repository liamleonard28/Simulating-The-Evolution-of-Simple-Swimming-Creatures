using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Point : MonoBehaviour
{
    public int index;

    Rigidbody2D collidingBody;

    void OnTriggerEnter2D(Collider2D collision)
    {
        GetCollidingBody(collision);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        GetCollidingBody(collision);
    }

    void GetCollidingBody(Collider2D collision)
    {
        if (collision.OverlapPoint((Vector2)transform.position))
        {
            collidingBody = collision.gameObject.GetComponent<Rigidbody2D>();
            FluidField.SetSolidCell(index, 1);
        }
    }

    public void AddForce(Vector2 force)
    {
        if (collidingBody == null)
        {
            return;
        }
        collidingBody.AddForceAtPosition(force * Time.fixedDeltaTime, transform.position, ForceMode2D.Impulse);
    }

    public Vector2 GetVelocity()
    {
        if (collidingBody == null)
        {
            return Vector2.zero;
        }
        return (Vector2)collidingBody.GetPointVelocity(transform.position);
    }

}
