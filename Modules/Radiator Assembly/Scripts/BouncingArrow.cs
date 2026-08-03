using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncingArrow : MonoBehaviour
{
    [SerializeField] private float speed = 2.0f; // Speed of movement (adjust in Inspector)
    [SerializeField] private float amplitude = 1.0f; // Amplitude of movement in Y direction (adjust in Inspector)

    private Vector3 startingPosition;

    public enum DirectionAxis { x,y,z};

    public DirectionAxis directionAxis = DirectionAxis.x;
    private void Start()
    {
        startingPosition = transform.localPosition;
    }

    private void Update()
    {
        float Movement = Mathf.Sin(Time.time * speed) * amplitude;
        switch (directionAxis)
        {
            case DirectionAxis.x:
                //transform.position = new Vector3(transform.position.x + Movement, startingPosition.y, transform.position.z);
                transform.localPosition = new Vector3(startingPosition.x + Movement, transform.localPosition.y, transform.localPosition.z);
                break;
            case DirectionAxis.y:
                //transform.position = new Vector3(transform.position.x, startingPosition.y + Movement, transform.position.z);
                transform.localPosition = new Vector3(transform.localPosition.x, startingPosition.y + Movement, transform.localPosition.z);
                break;
            case DirectionAxis.z:
                //transform.position = new Vector3(transform.position.x, startingPosition.y, transform.position.z + Movement);
                transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, startingPosition.z + Movement);
                break;
        }
        
        
    }
}
