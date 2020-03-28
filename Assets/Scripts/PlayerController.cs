using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public NetworkClient client;
    private Vector3 position;

    private void Start()
    {
        InvokeRepeating("Tick", 1, 0.03f);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            position += transform.TransformVector(Vector3.forward) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            position -= transform.TransformVector(Vector3.forward) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            position += transform.TransformVector(Vector3.right) * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            position -= transform.TransformVector(Vector3.right) * Time.deltaTime;
        }

    }

    private void Tick()
    {
        client.SendPosition(position);
    }
}
