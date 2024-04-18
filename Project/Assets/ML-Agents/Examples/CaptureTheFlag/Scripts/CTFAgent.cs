using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;

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

    public Animator animator;
    public bool isRandomizingTeam = false;

    [Header("Agent Stats")]
    public CTFTeam myTeam;
    public GameObject myFlag;
    public bool IHaveAFlag;

    [Header("Materials")]
    public Material blueMaterial;
    public Material redMaterial;

    [Header("Controls")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode rotateLeftKey = KeyCode.A;
    public KeyCode rotateRightKey = KeyCode.D;
    public KeyCode attackKey = KeyCode.E;

    [Header("Episode Information")]
    public bool endEpisodeOnTakeFlag = false;

    [Header("Rewards")]
    public float rewardScaleFactorForAngle = .0001f;
    public float rewardScaleFactorForDistance = .0001f;
    public float rewardScaleFactorForAngleOnTrigger = .005f;
    public float rewardForExceedingMaxSteps = -.5f;
    //public float rewardOnWallCollision = -.01f;
    public float rewardOnOtherFlagCollision = -3f;
    public float rewardForKillingEnemy = 1f;
    public float rewardForKillingEnemyWithFlag = 6f;
    public float rewardForBeingKilled = -1f;
    public float rewardForNotMoving = -0.001f;
    public float rewardOverTime = -0.00001f;

    private CTFPlatform m_platform;
    private Vector2 xBound = new Vector2(-11f, 11f);
    private Vector2 zBoundRed = new Vector2(-11f, -9f);
    private Vector2 zBoundBlue = new Vector2(9f, 11f);
    private float yBoundAgent = 0.58f;
    private PushBlockSettings m_PushBlockSettings;
    private Rigidbody m_AgentRb;

    private bool allyFlagCaptured;
    private bool enemyFlagCaptured;
    public CTFAgent playerToInteractWith = null;

    public float totalReward = 0;

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

    public override void OnEpisodeBegin()
    {
        //Debug.Log("total reward: " + totalReward + " for team: " + myTeam);
        totalReward = 0;
        if(isRandomizingTeam)
        {
            RandomizeTeam();
        }
        Respawn();
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
                // give reward based on distance to enemy, looking direction to enemy. he should kill the enemy
                GiveRewardBasedOnDistanceToEnemyWithFlag();
                GiveRewardBasedOnLookingDirectionEnemyWithFlag();
            }
            else
            {
                if (IHaveAFlag)
                {
                    GiveRewardBasedOnDistanceToFlag(CTFAllyEnemy.Ally);
                    GiveRewardBasedOnLookingDirectionFlag(CTFAllyEnemy.Ally);
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
        }
        //totalReward = 0;
        AddReward(rewardOverTime);
        totalReward += rewardOverTime;
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        bool hasMoved = false;
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];

        switch (action)
        {                
            case 1:
                hasMoved = true;
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                hasMoved = true;
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                hasMoved = true;
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                hasMoved = true;
                dirToGo = transform.right * 0.75f;
                break;
            case 7:
                if (playerToInteractWith != null && playerToInteractWith.myTeam != myTeam)
                {
                    animator.SetTrigger("Attack");
                    if(playerToInteractWith.IHaveAFlag)
                    {
                        allyFlag.SetActive(true);
                        if(myTeam == CTFTeam.Red)
                        {
                            m_platform.redFlagCaptured = false;
                        }
                        else
                        {
                            m_platform.blueFlagCaptured = false;
                        }
                        AddReward(rewardForKillingEnemyWithFlag);
                        totalReward += rewardForKillingEnemyWithFlag;

                    }
                    else
                    {
                        AddReward(rewardForKillingEnemy);
                        totalReward += rewardForKillingEnemy;
                    }
                    playerToInteractWith.Respawn();
                    playerToInteractWith.AddReward(rewardForBeingKilled);
                    playerToInteractWith.totalReward += rewardForBeingKilled;
                }
                hasMoved = true;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_PushBlockSettings.agentRunSpeed,
            ForceMode.VelocityChange);
        if(!hasMoved)
        {
            //Debug.Log("Not moved " + myTeam);
            AddReward(rewardForNotMoving);
            totalReward += rewardForNotMoving;
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);

        if (StepCount >= MaxStep - 1)
        {
            Debug.Log("Reached max steps");
            AddReward(rewardForExceedingMaxSteps);
            m_platform.EndEpisode();
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
        if(gameObject.activeInHierarchy)
        {
            if(col.transform.CompareTag(allyTag))
            {
                if (!enemyFlagCaptured)
                {
                    AddReward(rewardOnOtherFlagCollision);
                    totalReward += rewardOnOtherFlagCollision;
                }
                else if(IHaveAFlag && !allyFlagCaptured)
                {
                    FinishCapture();
                    var angleToFlag = GetAngleToFlag(CTFAllyEnemy.Ally);
                    m_platform.CaptureFlagRewards(myTeam, angleToFlag * rewardScaleFactorForAngleOnTrigger);
                    Debug.Log("winning team: " + myTeam);
                    m_platform.EndEpisode();
                }
            }
            else if (col.transform.CompareTag(enemyTag))
            {
                if (!enemyFlagCaptured)
                {
                    PickUpEnemyFlag();
                    var angleToFlag = GetAngleToFlag(CTFAllyEnemy.Enemy);
                    m_platform.TakeFlagRewards(myTeam, angleToFlag * rewardScaleFactorForAngleOnTrigger);
                    if(endEpisodeOnTakeFlag)
                    {
                        m_platform.EndEpisode();
                    }
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
            //AddReward(rewardOnWallCollision);
            // m_platform.EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(rotateRightKey))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(forwardKey))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(rotateLeftKey))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(backwardKey))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(attackKey))
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

    private float GetDistanceToObject(GameObject o)
    {
        return Vector3.Distance(transform.localPosition, o.transform.localPosition);
    }

    private float GetDistanceToFlag(CTFAllyEnemy c)
    {
        float d;
        if (c == CTFAllyEnemy.Ally)
        {
            d = GetDistanceToObject(allyFlag);
        }
        else
        {
            d = GetDistanceToObject(enemyFlag);
        }
        return d;
    }

    private float GetAngleToFlag(CTFAllyEnemy c)
    {
        float angle;
        if (c == CTFAllyEnemy.Ally)
        {
            angle = GetAngleToObject(allyFlag);
        }
        else
        {
            angle = GetAngleToObject(enemyFlag);
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

    private void GiveRewardBasedOnDistanceToEnemyWithFlag()
    {
        CTFAgent enemyWithFlag = m_platform.GetEnemyWithFlag(myTeam == CTFTeam.Red ? CTFTeam.Blue : CTFTeam.Red);
        if (enemyWithFlag != null)
        {
            float d = -GetDistanceToObject(enemyWithFlag.gameObject);
            AddReward(d * rewardScaleFactorForDistance);
            //Debug.Log("Distance reward: " + myTeam + ": " + d * rewardScaleFactorForDistance);
            totalReward += d * rewardScaleFactorForDistance;
        }
    }

    private void GiveRewardBasedOnLookingDirectionEnemyWithFlag()
    {
        CTFAgent enemyWithFlag = m_platform.GetEnemyWithFlag(myTeam == CTFTeam.Red ? CTFTeam.Blue : CTFTeam.Red);
        if (enemyWithFlag != null)
        {
            float d = -GetAngleToObject(enemyWithFlag.gameObject);
            AddReward(d * rewardScaleFactorForAngle);
            //Debug.Log("Angle reward for " + myTeam + ": " + d * rewardScaleFactorForAngle);
            totalReward += d * rewardScaleFactorForAngle;
        }
    }

    
}
