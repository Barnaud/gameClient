using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ControllablePlayer : MonoBehaviour
{
    private const float deadZone = 0.4f;
    private Rigidbody PlayerRb;
    private Animator playerAnimator;
    public float moveSpeed = 1f;
    // Start is called before the first frame update
    void Start()
    {
        PlayerRb = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float inputLen = getInputLen();
        float verticalAxis = (-1) * Input.GetAxis("Vertical");
        float horizontalAxis = Input.GetAxis("Horizontal");

        if(inputLen > 1)
        {
            verticalAxis /= inputLen;
            horizontalAxis /= inputLen;
            inputLen = 1;
        }

        float instantSpeed = moveSpeed * inputLen;
        if (inputLen > deadZone)
        {
            playerAnimator.SetInteger("speed", Mathf.RoundToInt(instantSpeed * 100));
            playerAnimator.SetFloat("runAnimMultiplier", inputLen);
            Quaternion rotation = Quaternion.Euler(0, getInputRotation(), 0);
            PlayerRb.MoveRotation(rotation);

            Vector3 movementVect = new Vector3(verticalAxis, 0, horizontalAxis) * moveSpeed * Time.fixedDeltaTime;
            PlayerRb.MovePosition(transform.position + movementVect);
        }
        else
        {
            playerAnimator.SetInteger("speed", 0);
            playerAnimator.SetFloat("runAnimMultiplier", 1f);
        }

        
    }

    float getInputRotation()
    {
        float returnedAngle;
        returnedAngle = (-1) * Mathf.Atan(Input.GetAxis("Vertical") / Input.GetAxis("Horizontal")) * 180f / Mathf.PI;
        if(Input.GetAxis("Horizontal") < 0)
        {
            returnedAngle -= 180;
        }
        return returnedAngle;
    }

    float getInputLen()
    {
        return Mathf.Sqrt(Mathf.Pow(Input.GetAxis("Vertical"), 2f) + Mathf.Pow(Input.GetAxis("Horizontal"), 2f));
    }
}
