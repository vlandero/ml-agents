using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

public enum CTFTeam
{
    Blue,
    Red
}
public class CTFAgent : Agent
{
    [HideInInspector] public GameObject allyFlag;
    [HideInInspector] public GameObject enemyFlag;

    [Header("Agent Stats")]
    public CTFTeam myTeam;
    public GameObject myFlag;
    public bool IHaveAFlag;

    [Header("Materials")]
    public Material blueMaterial;
    public Material redMaterial;

    [Header("Agent Settings")]
    public float rewardScaleFactor = .0001f;

    private CTFPlatform m_platform;
    private Vector2 zBound = new Vector2(-11f, 11f);
    private Vector2 xBound = new Vector2(-11f, 11f);
    private float yBoundAgent = 0.58f;
    private float yBoundFlag = 0.17f;
    private PushBlockSettings m_PushBlockSettings;
    private Rigidbody m_AgentRb;


    
    private void Start()
    {
        m_platform = GetComponentInParent<CTFPlatform>();
        m_AgentRb = GetComponent<Rigidbody>();
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        AssignFlags();
    }

    private void AssignFlags()
    {
        if (myTeam == CTFTeam.Red)
        {
            allyFlag = m_platform.redFlag;
            enemyFlag = m_platform.blueFlag;
        }
        else
        {
            enemyFlag = m_platform.redFlag;
            allyFlag = m_platform.blueFlag;
        }
    }

    private void RandomizeTeam()
    {
        myTeam = (CTFTeam)Random.Range(0, 2);
        AssignFlags();
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(Random.Range(zBound.x, zBound.y), yBoundAgent, Random.Range(xBound.x, xBound.y));
        transform.localRotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        myFlag.SetActive(false);
        IHaveAFlag = false;
        //RandomizeTeam();
        enemyFlag.transform.localPosition = new Vector3(Random.Range(zBound.x, zBound.y), yBoundFlag, Random.Range(xBound.x, xBound.y));
        allyFlag.transform.localPosition = new Vector3(Random.Range(zBound.x, zBound.y), yBoundFlag, Random.Range(xBound.x, xBound.y));
        enemyFlag.SetActive(true);
        allyFlag.SetActive(true);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        bool allyFlagCaptured;
        bool enemyFlagCaptured;
        if (myTeam == CTFTeam.Red)
        {
            allyFlagCaptured = m_platform.redFlagCaptured;
            enemyFlagCaptured = m_platform.blueFlagCaptured;
        }
        else
        {
            allyFlagCaptured = m_platform.blueFlagCaptured;
            enemyFlagCaptured = m_platform.redFlagCaptured;
        }
        sensor.AddObservation(myTeam == CTFTeam.Red ? 1 : 0);
        sensor.AddObservation(allyFlagCaptured);
        sensor.AddObservation(enemyFlagCaptured);
        sensor.AddObservation(IHaveAFlag);


        if (!enemyFlagCaptured)
        {
            GiveRewardBasedOnDistanceToEnemyFlag();
            GiveRewardBasedOnLookingDirectionEnemyFlag();
            LookingToTargetPositiveReward();
        }
    }

    private float GetDistanceToEnemyFlag()
    {
        float distanceToEnemyFlag = Vector3.Distance(transform.localPosition, enemyFlag.transform.localPosition);
        float d = distanceToEnemyFlag;
        return d;
    }

    private float GetAngleToEnemyFlag()
    {
        float angle = Vector3.Angle(transform.forward, enemyFlag.transform.localPosition - transform.localPosition);
        float d = Mathf.Abs(angle);
        return d;
    }

    private void GiveRewardBasedOnDistanceToEnemyFlag()
    {
        float d = -GetDistanceToEnemyFlag();
        AddReward(d * rewardScaleFactor);
    }

    private void GiveRewardBasedOnLookingDirectionEnemyFlag()
    {
        float d = -GetAngleToEnemyFlag();
        AddReward(d * rewardScaleFactor);
    }

    private void LookingToTargetPositiveReward()
    {
        bool isFacingEnemyFlag = IsFacingTarget(enemyFlag.transform.position, 15f);
        bool isMovingTowardsEnemyFlag = Vector3.Dot((enemyFlag.transform.position - transform.position).normalized, m_AgentRb.velocity.normalized) > 0;

        if (isFacingEnemyFlag && isMovingTowardsEnemyFlag)
        {
            AddReward(.08f);
        }
    }

    private bool IsFacingTarget(Vector3 targetPosition, float thresholdDegrees)
    {
        Vector3 toTarget = (targetPosition - transform.position).normalized;

        float angleToTarget = Vector3.Angle(transform.forward, toTarget);

        return angleToTarget <= thresholdDegrees;
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);

        if (StepCount >= MaxStep - 1)
        {
            AddReward(-.5f);
            EndEpisode();
        }
    }

    private void PickUpEnemyFlag()
    {
        print("Picked up flag");
        myFlag.SetActive(true);
        IHaveAFlag = true;
        enemyFlag.SetActive(false);
    }

    void OnTriggerEnter(Collider col)
    {
        if ((col.transform.CompareTag("redflag") || col.transform.CompareTag("blueflag")) && col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            if (col.GetComponent<Flag>().team == myTeam)
            {
                return;
            }
            PickUpEnemyFlag();
            AddReward(10f - GetAngleToEnemyFlag() * 0.005f);
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("wall"))
        {
            AddReward(-.01f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }
}
