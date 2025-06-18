using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PumpHose : MonoBehaviour
{
    public float lifespan = 0.2f;

    private void Start()
    {
        Debug.Log("Pump spawned at: " + transform.position);

        Destroy(gameObject, lifespan); // Auto-destroy after short time
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Pump collided with: " + other.name + ", tag: " + other.tag);

        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Pump hit enemy: " + other.name);

            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.Inflate();
            }
        }
        else
        {
            Debug.Log("Pump collided with non-enemy: " + other.name);
        }
    }
}
