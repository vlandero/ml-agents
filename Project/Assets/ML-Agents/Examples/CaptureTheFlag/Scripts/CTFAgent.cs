using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Unity.Sentis.Layers;
using TMPro;

public enum CTFTeam
{
    Blue,
    Red
}

public enum CTFAllyEnemy
{
    Ally,
    Enemy
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

    [Header("Rewards")]
    public float rewardScaleFactorForAngle = .0001f;
    public float rewardScaleFactorForDistance = .0001f;
    public float rewardForMovingToTarget = .08f;
    public float rewardForCapturingEnemyFlag = .9f;
    public float rewardForReturningFlag = 10f;
    public float rewardScaleFactorForAngleOnTrigger = .005f;
    public float rewardForExceedingMaxSteps = -.5f;
    public float rewardOnWallCollision = -.01f;
    public float rewardOnOtherFlagCollision = -3f;
    public float rewardOnHittingEnemyWithoutFlag = .5f;
    public float rewardOnHittingEnemyWithFlag = 7f;
    public float rewardForMovingToEnemyWithFlag = .1f;
    public float rewardForLookingToEnemyWithFlag = .1f;

    private CTFPlatform m_platform;
    private Vector2 xBound = new Vector2(-11f, 11f);
    private Vector2 xBoundPlayer = new Vector2(-11f, 11f);
    private Vector2 zBound = new Vector2(-11f, 11f);
    //private Vector2 zBoundRed = new Vector2(-11f, -2.5f);
    //private Vector2 zBoundBlue = new Vector2(2.5f, 11f);
    private Vector2 zBoundRed = new Vector2(-11f, -9f);
    private Vector2 zBoundBlue = new Vector2(9f, 11f);
    private float yBoundAgent = 0.58f;
    private float yBoundFlag = 0.17f;
    private PushBlockSettings m_PushBlockSettings;
    private Rigidbody m_AgentRb;

    private bool allyFlagCaptured;
    private bool enemyFlagCaptured;
    private GameObject playerToInteractWith = null;

    private float totalReward = 0;


    private void Start()
    {
        m_platform = GetComponentInParent<CTFPlatform>();
        m_AgentRb = GetComponent<Rigidbody>();
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        AssignFlags();
    }

    private void AssignFlags()
    {
        var clothComponent = myFlag.GetComponentInChildren<FlagCloth>();
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
        clothComponent.ResetTeam();
    }

    private void RandomizeTeam()
    {
        myTeam = (CTFTeam)Random.Range(0, 2);
        AssignFlags();
    }

    public void Respawn()
    {
        if (myTeam == CTFTeam.Red)
        {
            transform.localPosition = new Vector3(Random.Range(xBound.x, xBound.y), yBoundAgent, Random.Range(zBoundBlue.x, zBoundBlue.y));
        }
        else
        {
            transform.localPosition = new Vector3(Random.Range(xBound.x, xBound.y), yBoundAgent, Random.Range(zBoundRed.x, zBoundRed.y));
        }
        transform.localRotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        myFlag.SetActive(false);
        IHaveAFlag = false;
    }

    public void RandomSpawnFlags()
    {
        if (myTeam == CTFTeam.Red)
        {
            enemyFlag.transform.localPosition = new Vector3(Random.Range(xBound.x, xBound.y), yBoundFlag, Random.Range(zBoundRed.x, zBoundRed.y));
            allyFlag.transform.localPosition = new Vector3(Random.Range(xBound.x, xBound.y), yBoundFlag, Random.Range(zBoundBlue.x, zBoundBlue.y));
        }
        else
        {
            enemyFlag.transform.localPosition = new Vector3(Random.Range(xBound.x, xBound.y), yBoundFlag, Random.Range(zBoundBlue.x, zBoundBlue.y));
            allyFlag.transform.localPosition = new Vector3(Random.Range(xBound.x, xBound.y), yBoundFlag, Random.Range(zBoundRed.x, zBoundRed.y));
        }
    }

    public override void OnEpisodeBegin()
    {
        RandomizeTeam();
        Respawn();
        m_platform.ResetCaptured();
        m_platform.ResetAgentsLists();
        //RandomSpawnFlags();

        enemyFlag.SetActive(true);
        allyFlag.SetActive(true);
    }

    private void AssignCapturedFlags()
    {
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
    }   

    private GameObject GetEnemyWithFlag()
    {
        if(myTeam == CTFTeam.Red)
        {
            foreach (var agent in m_platform.blueAgents)
            {
                if(agent.GetComponent<CTFAgent>().IHaveAFlag)
                {
                    return agent;
                }
            }
        }
        else
        {
            foreach (var agent in m_platform.redAgents)
            {
                if (agent.GetComponent<CTFAgent>().IHaveAFlag)
                {
                    return agent;
                }
            }
        }
        return null;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        AssignCapturedFlags();
        sensor.AddObservation(myTeam == CTFTeam.Red ? 1 : 0);
        sensor.AddObservation(allyFlagCaptured);
        sensor.AddObservation(enemyFlagCaptured);
        sensor.AddObservation(IHaveAFlag);

        if (enemyFlagCaptured)
        {
            if (allyFlagCaptured)
            {
                GameObject enemyWithFlag = GetEnemyWithFlag();
                MovingToTargetPositiveReward(enemyWithFlag.transform.localPosition, rewardForMovingToEnemyWithFlag);
                GiveRewardBasedOnLookingDirectionToTarget(enemyWithFlag, rewardForLookingToEnemyWithFlag);
            }
            else
            {
                if (IHaveAFlag)
                {
                    GiveRewardBasedOnDistanceToFlag(CTFAllyEnemy.Ally);
                    GiveRewardBasedOnLookingDirectionFlag(CTFAllyEnemy.Ally);
                    MovingToTargetPositiveReward(allyFlag.transform.position, rewardForMovingToTarget);
                }
                else
                {
                }
            }
        }
        else
        {
            GiveRewardBasedOnDistanceToFlag(CTFAllyEnemy.Enemy);
            GiveRewardBasedOnLookingDirectionFlag(CTFAllyEnemy.Enemy);
            MovingToTargetPositiveReward(enemyFlag.transform.position, rewardForMovingToTarget);
        }
        totalReward = 0;
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
            case 7:
                if (playerToInteractWith)
                {
                    CTFAgent enemyCTFAgent = playerToInteractWith.GetComponent<CTFAgent>();
                    if (enemyCTFAgent && enemyCTFAgent.myTeam != myTeam)
                    {
                        Debug.Log("SHOULD NOT GET HERE CASE 7");
                        if (IHaveAFlag && enemyCTFAgent.IHaveAFlag)
                        {
                            AddReward(rewardOnHittingEnemyWithFlag);
                            EndEpisode();
                            //enemyCTFAgent.Respawn();
                            //enemyFlag.SetActive(true);
                        }
                        else
                        {
                            AddReward(rewardOnHittingEnemyWithoutFlag);
                        }
                    }
                }
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
            AddReward(rewardForExceedingMaxSteps);
            EndEpisode();
        }
    }

    private void FinishCapture()
    {
        m_platform.AddScore(myTeam);
        enemyFlag.SetActive(true);
        myFlag.SetActive(false);
    }

    private void OnTriggerEnter(Collider col)
    {
        string enemyTag = myTeam == CTFTeam.Red ? "blueflag" : "redflag";
        string allyTag = myTeam == CTFTeam.Red ? "redflag" : "blueflag";
        if (col.CompareTag("interactionSphere"))
        {
            playerToInteractWith = col.gameObject.transform.parent.gameObject;
        }
        if(col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            if(col.transform.CompareTag(allyTag))
            {
                if (!enemyFlagCaptured)
                {
                    AddReward(rewardOnOtherFlagCollision);
                }
                else if(IHaveAFlag && !allyFlagCaptured)
                {
                    FinishCapture();
                    AddReward(rewardForReturningFlag - GetAngleToObject(col.gameObject) * rewardScaleFactorForAngleOnTrigger);
                    EndEpisode();
                }
            }
            else if (col.transform.CompareTag(enemyTag))
            {
                if (!enemyFlagCaptured)
                {
                    PickUpEnemyFlag();
                    AddReward(rewardForCapturingEnemyFlag - GetAngleToObject(col.gameObject) * rewardScaleFactorForAngleOnTrigger);
                    //EndEpisode();
                }
            }
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.CompareTag("interactionSphere"))
        {
            playerToInteractWith = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("wall"))
        {
            AddReward(rewardOnWallCollision);
            EndEpisode();
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
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[0] = 7;
        }
    }


    private void CaptureEnemyFlag()
    {
        if (myTeam == CTFTeam.Red)
        {
            m_platform.blueFlagCaptured = true;
        }
        else
        {
            m_platform.redFlagCaptured = true;
        }
    }

    private void PickUpEnemyFlag()
    {
        myFlag.SetActive(true);
        IHaveAFlag = true;
        enemyFlag.SetActive(false);
        CaptureEnemyFlag();
    }

    private float GetDistanceToFlag(CTFAllyEnemy c)
    {
        float d;
        if (c == CTFAllyEnemy.Ally)
        {
            d = Vector3.Distance(transform.localPosition, allyFlag.transform.localPosition);
        }
        else
        {
            d = Vector3.Distance(transform.localPosition, enemyFlag.transform.localPosition);
        }
        return d;
    }

    private float GetAngleToFlag(CTFAllyEnemy c)
    {
        float angle;
        if (c == CTFAllyEnemy.Ally)
        {
            angle = Vector3.Angle(transform.forward, allyFlag.transform.localPosition - transform.localPosition);
        }
        else
        {
            angle = Vector3.Angle(transform.forward, enemyFlag.transform.localPosition - transform.localPosition);
        }
        float d = Mathf.Abs(angle);
        return d;
    }

    private float GetAngleToObject(GameObject obj)
    {
        float angle = Vector3.Angle(transform.forward, obj.transform.localPosition - transform.localPosition);
        float d = Mathf.Abs(angle);
        return d;
    }

    private void GiveRewardBasedOnDistanceToFlag(CTFAllyEnemy c)
    {
        float d = -GetDistanceToFlag(c);
        //Debug.Log("Adding reward for distance " + d * rewardScaleFactorForDistance);
        AddReward(d * rewardScaleFactorForDistance);
        totalReward += d * rewardScaleFactorForDistance;
    }

    private void GiveRewardBasedOnLookingDirectionFlag(CTFAllyEnemy c)
    {
        float d = -GetAngleToFlag(c);
        //Debug.Log("Adding reward for angle " + d * rewardScaleFactorForAngle);
        AddReward(d * rewardScaleFactorForAngle);
        totalReward += d * rewardScaleFactorForAngle;
    }

    private void GiveRewardBasedOnDistanceToTarget(Vector3 target, float scaleFactor)
    {
        float d = -Vector3.Distance(transform.localPosition, target);
        AddReward(d * scaleFactor);
        totalReward += d * scaleFactor;
    }

    private void GiveRewardBasedOnLookingDirectionToTarget(GameObject target, float scaleFactor)
    {
        float d = -GetAngleToObject(target);
        AddReward(d * scaleFactor);
        totalReward += d * scaleFactor;
    }

    private void MovingToTargetPositiveReward(Vector3 targetPosition, float scaleFactor)
    {
        Vector3 toTarget = (targetPosition - transform.position).normalized;
        float dotProduct = Vector3.Dot(toTarget, m_AgentRb.velocity.normalized);
        //Debug.Log("adding dotproduct reward " + dotProduct * scaleFactor);
        AddReward(dotProduct * scaleFactor);
        totalReward += dotProduct * scaleFactor;
    }
}
