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
    public int uid;
    public NetworkedClock networkedClock;

    private int faulty_ticks_count=0;
    private Int64 lastHandledServerTickTimestamp = 0;

    public Int64 lastServerTickTimestamp = 0;
    public Vector3 lastServerPosition;



    private GameStateStore gameStateStore = new GameStateStore(ServerConstants.saved_player_positions);


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("creating controllable player");
        PlayerRb = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();

        GameObject Camera = GameObject.FindGameObjectWithTag("MainCamera");
        Camera.AddComponent<CameraController>().objectToFollow = this.gameObject;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Handle taking server response in account

        handleServerState(lastServerTickTimestamp, lastServerPosition);

        //Handle user input

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

        //Handle sending position to server

        if (NetworkAdapter.networkAdapterInstance.isReady()) {
            KeyValuePair<Int64, Vector3> timedGameState = new KeyValuePair<Int64, Vector3>(networkedClock.getRemoteTimestampMs(), this.transform.position);
            gameStateStore.pushState(timedGameState);
            NetworkAdapter.networkAdapterInstance.sendPlayerGameState(this);
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

    public void setServerRequestedPosition(Int64 timestamp, Vector3 position)
    {
        if (timestamp <= lastHandledServerTickTimestamp)
        {
            Debug.Log("Ignoring tick since a more recent one was received before");
            return;
        }
        this.lastHandledServerTickTimestamp= timestamp;

        this.lastServerTickTimestamp = timestamp;
        this.lastServerPosition = position;
    }

    public void handleServerState(Int64 timestamp, Vector3 position)
    {
        Debug.Log("Handling server state");

        timestamp = timestamp - (networkedClock.getMedianRtt() / 2);
        KeyValuePair<Int64, Vector3> lastState = gameStateStore.getLastState(timestamp);

        if(lastState.Key == 0)
        {
            Debug.Log("lastState is empty?");
            return;
        }
        if(Vector3.Distance(position, lastState.Value) > ServerConstants.reconciliation_distance_treshold)
        {
            Debug.Log($"Found faulty tick {faulty_ticks_count}");
            faulty_ticks_count++;
            if(faulty_ticks_count > ServerConstants.faulty_ticks_before_reconciliation)
            {
                reconciliate(ref timestamp, position);
                faulty_ticks_count = 0;
                return;
            }
        }
        else
        {
            faulty_ticks_count = 0;
        }

    }

    void reconciliate(ref Int64 timestamp, Vector3 serverPosition)
    {
        Debug.Log("Reconciliation required");
        List<KeyValuePair<Int64, Vector3>> movesToReproduce = gameStateStore.getQueueSince(timestamp);
        Vector3 previsousPosition = transform.position;
        transform.position = serverPosition;
        foreach(KeyValuePair<Int64, Vector3> onePosition in movesToReproduce)
        {
            Vector3 delta = onePosition.Value - previsousPosition;
            transform.position = transform.position + delta;
            previsousPosition = transform.position;
        }
    }
}
