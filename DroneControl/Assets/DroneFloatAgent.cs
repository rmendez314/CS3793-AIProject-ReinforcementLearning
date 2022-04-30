using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DroneFloatAgent : Agent
{
    //public GameObject Marker;
    public GameObject Target;
    public float StartingHeightMin = -1f;
    public float StartingHeightMax = 1f;
    public float UpForce = 10f;
    public float RewardScale = 1f;
    float previousDistance = 0f;

    Rigidbody MarkerRigidBody;
    //BehaviorParameters behaviorParams;

    void Start()
    {
        MarkerRigidBody = GetComponent<Rigidbody>();
        //MarkerRigidBody = Marker.GetComponent<Rigidbody>();
        //behaviorParams = GetComponent<BehaviorParameters>();
    }

    // How to reinitialize when the game is reset. The Start() of an ML Agent
    public override void OnEpisodeBegin()
    {
        if (MarkerRigidBody != null)
        {
            //MarkerRigidBody.velocity = Vector3.zero;
            //this.MarkerRigidBody.rotation = Quaternion.identity;
            this.transform.rotation = Quaternion.identity;
            this.MarkerRigidBody.angularVelocity = Vector3.zero;
            this.MarkerRigidBody.velocity = Vector3.zero;
        }

        this.transform.localPosition = new Vector3(0, Random.Range(StartingHeightMin, StartingHeightMax), 0);
        Target.transform.localPosition = new Vector3(0, Random.Range(2, 5), 0);
        previousDistance = Vector3.Distance(this.transform.localPosition, Target.transform.localPosition);
    }

    // Tell the ML algorithm everything you can about the current state
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Target.transform.localPosition - this.transform.localPosition); // distance to target
        sensor.AddObservation(MarkerRigidBody.velocity); // Current velocity
        sensor.AddObservation(this.transform.rotation); // Quaternion
    }

    // What to do every step. The Update() of an ML Agent
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // This example only uses continuous space
        //if (behaviorParams.brainParameters.vectorActionSpaceType != SpaceType.Continuous)
        //{
        //    Debug.LogError("Must be continuous state type");
        //    return;
        //}

        //float[] actions
        // Actions, size = 2
        //Vector3 controlSignal = Vector3.zero;
        //controlSignal.y = actionBuffers.ContinuousActions[0];

        //float action_y = actions[0];
        float action_y = actionBuffers.ContinuousActions[0]; // The agent has only one possible action. Up/Down amount
        action_y = Mathf.Clamp(action_y, -1, 1); // Bound the action input from -1 to 1
        action_y = action_y * UpForce; // Scale in put to marker speed
        if (MarkerRigidBody != null)
        {
            MarkerRigidBody.AddForce(0, action_y, 0);
        }

        //var reward = CalculateReward(this.transform.position.y, Target.transform.position.y);
        //SetReward(reward);
        var distance = Vector3.Distance(this.transform.position, Target.transform.position);
        var improvement = previousDistance - distance;
        previousDistance = distance;
        var reward = RewardScale * improvement;

        // Reached target
        if (distance < 0.25f)
        {
            //SetReward(1.0f);
            EndEpisode();
        }

        // Fell off platform
        else if (this.transform.localPosition.y < 0)
        {
            EndEpisode();
        }
    }


    public float CalculateReward(float markerY, float targetY)
    {
        var distance = Vector3.Distance(this.transform.position, Target.transform.position);
        var improvement = previousDistance - distance;
        previousDistance = distance;

        return RewardScale * improvement;
    }



}
