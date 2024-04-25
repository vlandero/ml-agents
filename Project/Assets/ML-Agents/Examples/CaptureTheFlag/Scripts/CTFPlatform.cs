using System.Collections.Generic;
using UnityEngine;

public class CTFPlatform : MonoBehaviour
{
    public GameObject blueFlag;
    public GameObject redFlag;

    public bool blueFlagCaptured;
    public bool redFlagCaptured;

    public int blueScore = 0;
    public int redScore = 0;

    public Dictionary<int, CTFTeam> Teams = new Dictionary<int, CTFTeam>();

    public List<GameObject> redAgents = new List<GameObject>();
    public List<GameObject> blueAgents = new List<GameObject>();

    public bool isRandomizingTeam = false;

    [Header("Agent Rewards")]
    public float rewardForCapture = 10f;
    public float rewardForBeingCaptured = -10f;
    public float rewardForTake = 1f;
    public float rewardForBeingTaken = -1f;

    [Header("Spawn Points")]
    [HideInInspector] public Transform[] redSpawnBounds;
    [HideInInspector] public Transform[] blueSpawnBounds;
    public GameObject redSpawn;
    public GameObject blueSpawn;

    private void Awake()
    {
        GameObject C1Red = redSpawn.transform.GetChild(0).gameObject;
        GameObject C2Red = redSpawn.transform.GetChild(1).gameObject;

        GameObject C1Blue = blueSpawn.transform.GetChild(0).gameObject;
        GameObject C2Blue = blueSpawn.transform.GetChild(1).gameObject;

        redSpawnBounds = new Transform[] { C1Red.transform, C2Red.transform };
        blueSpawnBounds = new Transform[] { C1Blue.transform, C2Blue.transform };
    }
    private void Start()
    {
        Teams.Add(0, CTFTeam.Red);
        Teams.Add(1, CTFTeam.Blue);
        ResetCaptured();
        ResetAgentsLists();
        RandomSpawnFlags();        
    }
    
    public void ResetAgentsLists()
    {
        redAgents = new List<GameObject>();
        blueAgents = new List<GameObject>();

        foreach (Transform child in transform)
        {
            if (child.CompareTag("agent") || child.CompareTag("redAgent") || child.CompareTag("redAgentWithFlag") || child.CompareTag("blueAgentWithFlag") || child.CompareTag("blueAgent"))
            {
                var comp = child.GetComponent<CTFAgent>();
                if (comp && comp.myTeam == CTFTeam.Red)
                {
                    redAgents.Add(child.gameObject);
                }
                else
                {
                    blueAgents.Add(child.gameObject);
                }
            }
        }
    }

    private void RandomizeTeams()
    {
        Teams[0] = (CTFTeam)Random.Range(0, 2);
        Teams[1] = Teams[0] == CTFTeam.Red ? CTFTeam.Blue : CTFTeam.Red;
    }

    public void RandomSpawnFlags()
    {
        blueFlag.transform.position = new Vector3(Random.Range(blueSpawnBounds[0].position.x, blueSpawnBounds[1].position.x), blueSpawnBounds[0].position.y, Random.Range(blueSpawnBounds[0].position.z, blueSpawnBounds[1].position.z));
        redFlag.transform.position = new Vector3(Random.Range(redSpawnBounds[0].position.x, redSpawnBounds[1].position.x), redSpawnBounds[0].position.y, Random.Range(redSpawnBounds[0].position.z, redSpawnBounds[1].position.z));
    }

    public void AddScore(CTFTeam team)
    {
        if(team == CTFTeam.Red)
        {
            redScore++;
            return;
        }
        blueScore++;
    }

    public void ResetCaptured()
    {
        blueFlagCaptured = false;
        redFlagCaptured = false;
    }

    public void EndEpisode()
    {
        ResetCaptured();
        RandomSpawnFlags();
        blueScore = 0;
        redScore = 0;
        blueFlag.SetActive(true);
        redFlag.SetActive(true);
        if(isRandomizingTeam)
        {
            RandomizeTeams();
        }
        foreach (var agent in redAgents)
        {
            agent.GetComponent<CTFAgent>().EndEpisode();
        }

        foreach (var agent in blueAgents)
        {
            agent.GetComponent<CTFAgent>().EndEpisode();
        }
        ResetAgentsLists();
    }

    public void CaptureFlagRewards(CTFTeam team, float m)
    {
        if (team == CTFTeam.Red)
        {
            foreach (var agent in redAgents)
            {
                var ctfa = agent.GetComponent<CTFAgent>();
                ctfa.AddReward(rewardForCapture - rewardForCapture * m);
                ctfa.totalReward += rewardForCapture - rewardForCapture * m;
            }

            foreach (var agent in blueAgents)
            {
                var ctfa = agent.GetComponent<CTFAgent>();
                ctfa.AddReward(rewardForBeingCaptured);
                ctfa.totalReward += rewardForBeingCaptured;
            }
        }
        else
        {
            foreach (var agent in blueAgents)
            {
                var ctfa = agent.GetComponent<CTFAgent>();
                ctfa.AddReward(rewardForCapture - rewardForCapture * m);
                ctfa.totalReward += rewardForCapture - rewardForCapture * m;
            }

            foreach (var agent in redAgents)
            {
                var ctfa = agent.GetComponent<CTFAgent>();
                ctfa.AddReward(rewardForBeingCaptured);
                ctfa.totalReward += rewardForBeingCaptured;
            }
        }
    }

    public void TakeFlagRewards(CTFTeam team, float m)
    {
        if (team == CTFTeam.Red)
        {
            foreach (var agent in redAgents)
            {
                var ctfa = agent.GetComponent<CTFAgent>();
                ctfa.AddReward(rewardForTake - rewardForTake * m);
                ctfa.totalReward += rewardForTake - rewardForTake * m;
            }

            foreach (var agent in blueAgents)
            {
                var ctfa = agent.GetComponent<CTFAgent>();
                ctfa.AddReward(rewardForBeingTaken);
                ctfa.totalReward += rewardForBeingTaken;
            }
        }
        else
        {
            foreach (var agent in blueAgents)
            {
                var ctfa = agent.GetComponent<CTFAgent>();
                ctfa.AddReward(rewardForTake - rewardForTake * m);
                ctfa.totalReward += rewardForTake - rewardForTake * m;
            }

            foreach (var agent in redAgents)
            {
                var ctfa = agent.GetComponent<CTFAgent>();
                ctfa.AddReward(rewardForBeingTaken);
                ctfa.totalReward += rewardForBeingTaken;
            }
        }
    }

    public CTFAgent GetEnemyWithFlag(CTFTeam team)
    {
        if (team == CTFTeam.Red)
        {
            foreach (var agent in redAgents)
            {
                if (agent.GetComponent<CTFAgent>().IHaveAFlag)
                {
                    return agent.GetComponent<CTFAgent>();
                }
            }
        }
        else
        {
            foreach (var agent in blueAgents)
            {
                if (agent.GetComponent<CTFAgent>().IHaveAFlag)
                {
                    return agent.GetComponent<CTFAgent>();
                }
            }
        }

        return null;
    }

    public CTFAgent GetEnemyWithoutFlag(CTFTeam team)
    {
        if (team == CTFTeam.Red)
        {
            foreach (var agent in redAgents)
            {
                if (!agent.GetComponent<CTFAgent>().IHaveAFlag)
                {
                    return agent.GetComponent<CTFAgent>();
                }
            }
        }
        else
        {
            foreach (var agent in blueAgents)
            {
                if (!agent.GetComponent<CTFAgent>().IHaveAFlag)
                {
                    return agent.GetComponent<CTFAgent>();
                }
            }
        }

        return null;
    }
}
