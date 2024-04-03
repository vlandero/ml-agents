using System.Collections;
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
}
