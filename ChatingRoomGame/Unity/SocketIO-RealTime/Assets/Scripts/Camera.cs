using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    public Transform OwnerPlayer;
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var HasTarget = GameObject.FindWithTag("Target");

        if(HasTarget == null)
           return;
        else   
           OwnerPlayer = HasTarget.transform;

        transform.position = new Vector3(OwnerPlayer.position.x , OwnerPlayer.position.y  , transform.position.z);
    }
}
