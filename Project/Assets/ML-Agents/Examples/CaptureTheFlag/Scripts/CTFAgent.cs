using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Unity.MLAgents.Policies;

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

public enum CTFPriority
{
    KillEnemyWithFlag,
    KillEnemyWithoutFlag,
    CaptureFlag,
    ReturnFlag
}

public class CTFAgent : Agent
{
    [HideInInspector] public GameObject allyFlag;
    [HideInInspector] public GameObject enemyFlag;

    public Animator animator;
    public int teamNumber = 0;

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
    public bool endEpisodeOnWallCollision = false;
    public bool isChangingTag = false;

    [Header("Rewards")]
    public float rewardScaleFactorForAngle = .0001f;
    public float rewardScaleFactorForDistance = .0001f;
    public float rewardScaleFactorForAngleOnTrigger = .005f;
    public float rewardForExceedingMaxSteps = -.5f;
    public float rewardOnWallCollision = -12f;
    public float rewardOnOtherFlagCollision = -3f;
    public float rewardForKillingEnemy = 1f;
    public float rewardForKillingEnemyWithFlag = 6f;
    public float rewardForBeingKilled = -1f;
    public float rewardForNotMoving = -0.001f;
    public float rewardOverTime = -0.00001f;

    [Header("Priorities")]
    public CTFPriority bothFlagsCapturedIHaveAFlag = CTFPriority.KillEnemyWithFlag;
    public CTFPriority bothFlagsCapturedIDontHaveAFlag = CTFPriority.KillEnemyWithFlag;
    public CTFPriority onlyEnemyFlagCapturedIHaveAFlag = CTFPriority.ReturnFlag;
    public CTFPriority onlyEnemyFlagCapturedIDontHaveAFlag = CTFPriority.KillEnemyWithoutFlag;
    public CTFPriority onlyAllyFlagCaptured = CTFPriority.CaptureFlag;
    public CTFPriority noFlagsCaptured = CTFPriority.CaptureFlag;

    private CTFPlatform m_platform;
    private BehaviorParameters behaviorParameters;
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
        behaviorParameters = GetComponent<BehaviorParameters>();
        AssignFlags();
        ChangeTag();
    }

    private void Update()
    {
        ChangeTag();
    }

    private void ChangeTag()
    {
        if(!isChangingTag)
        {
            return;
        }
        if(myTeam == CTFTeam.Red)
        {
            if (IHaveAFlag)
            {
                gameObject.tag = "redAgentWithFlag";
            }
            else
            {
                gameObject.tag = "redAgent";
            }
        }
        else
        {
            if (IHaveAFlag)
            {
                gameObject.tag = "blueAgentWithFlag";
            }
            else
            {
                gameObject.tag = "blueAgent";
            }
        }
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

    private void SetTeams()
    {
        myTeam = m_platform.Teams[teamNumber];
        AssignFlags();
    }

    public void Respawn()
    {

        if (myTeam == CTFTeam.Red)
        {
            transform.position = new Vector3(Random.Range(m_platform.redSpawnBounds[0].position.x, m_platform.redSpawnBounds[1].position.x), m_platform.redSpawnBounds[0].position.y, Random.Range(m_platform.redSpawnBounds[0].position.z, m_platform.redSpawnBounds[1].position.z));
        }
        else
        {
            transform.position = new Vector3(Random.Range(m_platform.blueSpawnBounds[0].position.x, m_platform.blueSpawnBounds[1].position.x), m_platform.blueSpawnBounds[0].position.y, Random.Range(m_platform.blueSpawnBounds[0].position.z, m_platform.blueSpawnBounds[1].position.z));
        }
        transform.localRotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        myFlag.SetActive(false);
        IHaveAFlag = false;
    }

    public override void OnEpisodeBegin()
    {
        //Debug.Log("total reward: " + totalReward + " for name: " + name);
        totalReward = 0;
        SetTeams();
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

    private void GiveIntermediateRewards(CTFPriority p)
    {
        if(p == CTFPriority.CaptureFlag)
        {
            GiveRewardBasedOnDistanceToFlag(CTFAllyEnemy.Enemy);
            GiveRewardBasedOnLookingDirectionFlag(CTFAllyEnemy.Enemy);
        }
        else if(p == CTFPriority.ReturnFlag)
        {
            GiveRewardBasedOnDistanceToFlag(CTFAllyEnemy.Ally);
            GiveRewardBasedOnLookingDirectionFlag(CTFAllyEnemy.Ally);
        }
        else if(p == CTFPriority.KillEnemyWithFlag)
        {
            GiveRewardBasedOnDistanceToEnemyWithFlag();
            GiveRewardBasedOnLookingDirectionEnemyWithFlag();
        }
        else if(p == CTFPriority.KillEnemyWithoutFlag)
        {
            GiveRewardBasedOnDistanceToEnemyWithoutFlag();
            GiveRewardBasedOnLookingDirectionEnemyWithoutFlag();
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
                if(IHaveAFlag)
                {
                    GiveIntermediateRewards(bothFlagsCapturedIHaveAFlag);
                }
                else
                {
                    GiveIntermediateRewards(bothFlagsCapturedIDontHaveAFlag);
                }
            }
            else
            {
                if (IHaveAFlag)
                {
                    GiveIntermediateRewards(onlyEnemyFlagCapturedIHaveAFlag);
                }
                else
                {
                    GiveIntermediateRewards(onlyEnemyFlagCapturedIDontHaveAFlag);
                }
            }
        }
        else
        {
            if (allyFlagCaptured)
            {
                GiveIntermediateRewards(onlyAllyFlagCaptured);
            }
            else
            {
                GiveIntermediateRewards(noFlagsCaptured);
            }
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
        IHaveAFlag = false;
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
                    Debug.Log("winning team: " + behaviorParameters.TeamId);
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
        if (collision.transform.CompareTag("wall") || collision.transform.CompareTag("outerWall"))
        {
            if (endEpisodeOnWallCollision)
            {
                AddReward(rewardOnWallCollision);
                m_platform.EndEpisode();
            }
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

    private void GiveRewardBasedOnDistanceToEnemyWithoutFlag()
    {
        CTFAgent enemyWithoutFlag = m_platform.GetEnemyWithoutFlag(myTeam == CTFTeam.Red ? CTFTeam.Blue : CTFTeam.Red);
        if (enemyWithoutFlag != null)
        {
            float d = -GetDistanceToObject(enemyWithoutFlag.gameObject);
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

    private void GiveRewardBasedOnLookingDirectionEnemyWithoutFlag()
    {
        CTFAgent enemyWithoutFlag = m_platform.GetEnemyWithoutFlag(myTeam == CTFTeam.Red ? CTFTeam.Blue : CTFTeam.Red);
        if (enemyWithoutFlag != null)
        {
            float d = -GetAngleToObject(enemyWithoutFlag.gameObject);
            AddReward(d * rewardScaleFactorForAngle);
            //Debug.Log("Angle reward for " + myTeam + ": " + d * rewardScaleFactorForAngle);
            totalReward += d * rewardScaleFactorForAngle;
        }
    }


}
