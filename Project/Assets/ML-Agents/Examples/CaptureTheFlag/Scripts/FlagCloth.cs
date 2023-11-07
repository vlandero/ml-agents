using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagCloth : MonoBehaviour
{
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material redMaterial;

    [SerializeField] private CTFAgent agent;
    private void Start()
    {
        if(agent.myTeam == CTFTeam.Blue)
        {
            GetComponent<Renderer>().material = redMaterial;
        }
        else
        {
            GetComponent<Renderer>().material = blueMaterial;
        }
    }
}
