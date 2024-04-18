using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereTrigger : MonoBehaviour
{
    public CTFAgent agent;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("agent"))
        {
            agent.playerToInteractWith = other.gameObject.GetComponent<CTFAgent>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("agent"))
        {
            agent.playerToInteractWith = null;
        }
    }
}
