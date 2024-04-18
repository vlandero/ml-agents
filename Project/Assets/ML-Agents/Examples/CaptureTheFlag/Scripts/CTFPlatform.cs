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

    public List<GameObject> redAgents = new List<GameObject>();
    public List<GameObject> blueAgents = new List<GameObject>();

    private Vector2 zBoundRed = new Vector2(-11f, -9f);
    private Vector2 zBoundBlue = new Vector2(9f, 11f);
    private Vector2 xBound = new Vector2(-11f, 11f);

    [Header("Agent Rewards")]
    public float rewardForCapture = 10f;
    public float rewardForBeingCaptured = -10f;
    public float rewardForTake = 1f;
    public float rewardForBeingTaken = -1f;

    public void ResetAgentsLists()
    {
        redAgents = new List<GameObject>();
        blueAgents = new List<GameObject>();

        foreach (Transform child in transform)
        {
            if (child.CompareTag("agent"))
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

    private void Start()
    {
        ResetCaptured();
        ResetAgentsLists();
        RandomSpawnFlags();
    }

    public void RandomSpawnFlags()
    {
        blueFlag.transform.localPosition = new Vector3(Random.Range(xBound.x, xBound.y), 0.17f, Random.Range(zBoundRed.x, zBoundRed.y));
        redFlag.transform.localPosition = new Vector3(Random.Range(xBound.x, xBound.y), 0.17f, Random.Range(zBoundBlue.x, zBoundBlue.y));
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
        foreach (var agent in redAgents)
        {
            agent.GetComponent<CTFAgent>().EndEpisode();
        }

        foreach (var agent in blueAgents)
        {
            agent.GetComponent<CTFAgent>().EndEpisode();
        }

        ResetCaptured();
        ResetAgentsLists();
        RandomSpawnFlags();
        blueScore = 0;
        redScore = 0;
        blueFlag.SetActive(true);
        redFlag.SetActive(true);
    }

    public void CaptureFlagRewards(CTFTeam team, float m)
    {
        if (team == CTFTeam.Red)
        {
            foreach (var agent in redAgents)
            {
                agent.GetComponent<CTFAgent>().AddReward(rewardForCapture - rewardForCapture * m);
            }

            foreach (var agent in blueAgents)
            {
                agent.GetComponent<CTFAgent>().AddReward(rewardForBeingCaptured);
            }
        }
        else
        {
            foreach (var agent in blueAgents)
            {
                agent.GetComponent<CTFAgent>().AddReward(rewardForCapture - rewardForCapture * m);
            }

            foreach (var agent in redAgents)
            {
                agent.GetComponent<CTFAgent>().AddReward(rewardForBeingCaptured);
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
}
