using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public enum CTFTeam
{
    Blue,
    Red
}

public class PushAgentEscape : Agent
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
    public float rewardScaleFactor = .001f;

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
        if(myTeam == CTFTeam.Red)
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

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(Random.Range(zBound.x, zBound.y), yBoundAgent, Random.Range(xBound.x, xBound.y));
        enemyFlag.transform.localPosition = new Vector3(Random.Range(zBound.x, zBound.y), yBoundFlag, Random.Range(xBound.x, xBound.y));
        myFlag.SetActive(false);
        enemyFlag.SetActive(true);
        allyFlag.SetActive(true);
        IHaveAFlag = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(IHaveAFlag);

        if (!IHaveAFlag)
        {
            GiveRewardBasedOnDistanceToFlag();
        }
    }

    private void GiveRewardBasedOnDistanceToFlag()
    {
        float distanceToEnemyFlag = Vector3.Distance(transform.localPosition, enemyFlag.transform.localPosition);
        float reward = -distanceToEnemyFlag;
        AddReward(reward * rewardScaleFactor);
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
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

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);

        if (StepCount >= MaxStep - 1)
        {
            AddReward(-.5f);
            EndEpisode();
        }
    }

    void OnCollisionEnter(Collision col)
    {
        //if (col.transform.CompareTag("lock"))
        //{
        //    if (IHaveAKey)
        //    {
        //        myKey.SetActive(false);
        //        IHaveAKey = false;
        //        m_GameController.UnlockDoor();
        //    }
        //}
        //if (col.transform.CompareTag("dragon"))
        //{
        //    m_GameController.KilledByBaddie(this, col);
        //    myKey.SetActive(false);
        //    IHaveAKey = false;
        //}
        //if (col.transform.CompareTag("portal"))
        //{
        //    m_GameController.TouchedHazard(this);
        //}
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
        //if we find a key and it's parent is the main platform we can pick it up
        if (col.transform.CompareTag("flag") && col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            if(col.GetComponent<Flag>().team == myTeam)
            {
                return;
            }
            PickUpEnemyFlag();
            AddReward(1f);
            //EndEpisode();
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
