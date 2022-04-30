using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DroneHoverAgent : Agent
{
    public GameObject Target;
    public GameObject FLThruster;
    public GameObject FRThruster;
    public GameObject RLThruster;
    public GameObject RRThruster;

    public float StartingHeightMin = -1f;
    public float StartingHeightMax = 1f;
    public float GraceDistance = 0.1f;
    public float UpForce = 10f;
    public float RewardScale = 1f;
    public float EnergyRewardScale = -0.01f;

    float previousDistance = 0f;

    Rigidbody MarkerRigidBody;
    Vector3 startPosition;

    void Start()
    {
        MarkerRigidBody = gameObject.GetComponent<Rigidbody>();
        startPosition = transform.position;
    }

    // How to reinitialize when the game is reset. The Start() of an ML Agent
    public override void OnEpisodeBegin()
    {
        if (MarkerRigidBody != null)
        {
            MarkerRigidBody.velocity = Vector3.zero;
            MarkerRigidBody.rotation = Quaternion.identity;
        }

        transform.position = new Vector3(Random.Range(-1,1), Random.Range(StartingHeightMin, StartingHeightMax), Random.Range(-1,1));
        Target.transform.position = new Vector3(Random.Range(-1,1), Random.Range(StartingHeightMin, StartingHeightMax)+0.5f, Random.Range(-1,1));
        previousDistance = Vector3.Distance(transform.position, Target.transform.position);
    }

    // Tell the ML algorithm everything you can about the current state
    public override void CollectObservations(VectorSensor sensor)
    {
        var heading = Target.transform.position - transform.position;
        var distance = heading.magnitude;
        var direction = heading / distance;
        sensor.AddObservation(direction);
        sensor.AddObservation(MarkerRigidBody.velocity); // Current velocity
        sensor.AddObservation(transform.rotation); // Quaternion
    }

    // What to do every step. The Update() of an ML Agent
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float actionFR = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f) * UpForce; // One of many actions
        float actionFL = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f) * UpForce;
        float actionRR = Mathf.Clamp(actionBuffers.ContinuousActions[2], -1f, 1f) * UpForce;
        float actionRL = Mathf.Clamp(actionBuffers.ContinuousActions[3], -1f, 1f) * UpForce;

        // Calculate the absolute value of the energy used
        var energyUsed = (Mathf.Abs(actionFR) + Mathf.Abs(actionFL) + Mathf.Abs(actionRR) + Mathf.Abs(actionRL)) / UpForce;

        if (MarkerRigidBody != null)
        {
            MarkerRigidBody.AddForceAtPosition(transform.up * actionFR, FRThruster.transform.position);
            MarkerRigidBody.AddForceAtPosition(transform.up * actionFL, FLThruster.transform.position);
            MarkerRigidBody.AddForceAtPosition(transform.up * actionRR, RRThruster.transform.position);
            MarkerRigidBody.AddForceAtPosition(transform.up * actionRL, RLThruster.transform.position);
        }

        //var distance = Vector3.Distance(transform.position, Target.transform.position);
        //var distance = Vector3.Distance(transform.position, Target.transform.position);
        //var improvement = previousDistance - distance;
        //if (distance < GraceDistance)
        //{
        //    improvement = Mathf.Max(0f, improvement);
        //}
        //previousDistance = distance;
        //var improvementPortion = RewardScale * improvement;
        //var energyPortion = EnergyRewardScale * energyUsed;
        //var reward = improvementPortion + energyPortion;
        //AddReward(reward);

        var reward = CalculateReward(transform.position.y, Target.transform.position.y, energyUsed);
        AddReward(reward);

        // Reached target
        //if (distance < 0.1f)
        //{
        //SetReward(1.0f);
        //AddReward(1.0f);
        //EndEpisode();
        //}

        // Fell off platform
        if (transform.position.y < 0)
        {
            EndEpisode();
        }
    }

    public float CalculateReward(float markerY, float targetY, float energyUsed)
    {
        var distance = Vector3.Distance(transform.position, Target.transform.position);
        var improvement = previousDistance - distance;
        if (distance < GraceDistance) improvement = Mathf.Max(0f, improvement); // If within a grace area, it's still better than nothing
        previousDistance = distance;

        var improvementPortion = RewardScale * improvement;
        var energyPortion = EnergyRewardScale * energyUsed;

        return improvementPortion + energyPortion;
    }

}

