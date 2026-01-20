using System;
using System.Numerics;
using UnityEngine;

public class Main : MonoBehaviour
{
    public float Mass = 1;
    public float MaxForce = 1;
    public float MaxSpeed = 0.01f;
    public float SlowingDistance = 10.0f;
    public GameObject vehicleModel;
    public GameObject targetModel;

    private VehicleData vehicleData;
    private Vector3 target;

    // We set up the internal data structure for the vehicleData here
    // based on editor values entered.
    void Start()
    {
        vehicleData.mass = Mass;
        vehicleData.max_force = MaxForce;
        vehicleData.max_speed = MaxSpeed;
        vehicleData.velocity = Vector3.zero;
        vehicleData.pos = vehicleModel.transform.position;

    }

    // Update is called once per frame
    void Update()
    {
        // make sure to reset the goal if somebody moves it
        target = targetModel.transform.position;

        // calculates the seek values for the vehicle
        Seek(ref vehicleData, target);

        // once the data in the program changes, we
        // can then update the model to show it
        vehicleModel.transform.position = vehicleData.pos;
        vehicleModel.transform.forward = Vector3.Normalize(vehicleData.velocity);
    }


    /// <summary>
    /// The famous seek technique as originally described in Craig Reynold's 1999 GDC paper at:
    /// https://www.red3d.com/cwr/steer/gdc99/
    /// Note that his original paper has position and target reversed if we want to head
    /// toward the target.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="target"></param>
    public void Seek(ref VehicleData data, Vector3 target)
    {
        Vector3 desired_velocity = Vector3.Normalize((target - data.pos) * data.max_speed);
        Vector3 steering = desired_velocity - data.velocity;
        steering = truncateLength(steering, data.max_force);
        Vector3 acceleration = steering / data.mass;
        data.velocity = truncateLength(data.velocity + acceleration, data.max_speed);
        data.pos = data.pos + data.velocity;
    }

    /// <summary>
    /// The famous flee steering technique as originally described in Craig Reynold's 1999 GDC paper at:
    /// https://www.red3d.com/cwr/steer/gdc99/
    /// </summary>
    /// <param name="data"></param>
    /// <param name="target"></param>
    public void Flee(ref VehicleData data, Vector3 target)
    {
        Vector3 desired_velocity = Vector3.Normalize((data.pos - target) * data.max_speed);
        Vector3 steering = desired_velocity - data.velocity;
        steering = truncateLength(steering, data.max_force);
        Vector3 acceleration = steering / data.mass;
        data.velocity = truncateLength(data.velocity + acceleration, data.max_speed);
        data.pos = data.pos + data.velocity;
    }

    /// <summary>
    /// The famous arrive steering technique as originally described in Craig Reynold's 1999 GDC paper at:
    /// https://www.red3d.com/cwr/steer/gdc99/
    /// </summary>
    /// <param name="data"></param>
    /// <param name="target"></param>
    public void Arrive(ref VehicleData data, Vector3 target)
    {
        Vector3 target_offset = target - data.pos;
        float distance = target_offset.magnitude;
        float ramped_speed = data.max_speed * (distance / SlowingDistance);
        float clipped_speed = Math.Min(ramped_speed, data.max_speed);

        Vector3 desired_velocity = (clipped_speed / distance) * target_offset;
        Vector3 steering = desired_velocity - data.velocity;
        steering = truncateLength(steering, data.max_force);
        Vector3 acceleration = steering / data.mass;
        data.velocity = truncateLength(data.velocity + acceleration, data.max_speed);
        data.pos = data.pos + data.velocity;
    }

    // This is a clamping function so that the vector 
    // input does not get larger than the maxLength entered
    public Vector3 truncateLength(Vector3 vector, float maxLength)
    {
        float maxLengthSquared = maxLength * maxLength;
        float vecLengthSquared = vector.sqrMagnitude;
        if (vecLengthSquared <= maxLengthSquared)
            return vector;
        else
            return (vector) * (maxLength / Mathf.Sqrt((vecLengthSquared)));
    }
}
