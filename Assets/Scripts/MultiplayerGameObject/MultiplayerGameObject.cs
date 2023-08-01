using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Represents an object being controlled by the server
 */
public class MultiplayerGameObject : MonoBehaviour
{

    static uint lastUid = 0;
    public static Dictionary<uint,  MultiplayerGameObject> dict = new Dictionary<uint, MultiplayerGameObject>();

    static uint assignUid(MultiplayerGameObject objectToAssign)
    {
        uint newUid = ++lastUid;
        dict[newUid] = objectToAssign;
        return newUid;
    }

    protected Rigidbody characterRb;
    protected Vector2 dest;

    protected Vector3? serverRequestedPosition;
    protected bool shouldUpdateToServerPosition;
    protected float speed;

    protected Animator animComponent;

    private System.Diagnostics.Stopwatch diagStopWatch;

    private MultiplayerGameObjectAnimator customAnimator;

    public uint uid;
    public int maxSpeed = 200;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Creating gameObject in start");
        characterRb = GetComponent<Rigidbody>();
        dest = VectorUtils.toVec2(transform.position);
        animComponent = GetComponent<Animator>();
        customAnimator = new MultiplayerGameObjectAnimator(animComponent, maxSpeed);
        diagStopWatch = new System.Diagnostics.Stopwatch();
        diagStopWatch.Start();

        //moveToLocationStraight(new Vector2(5, 6));
    }

    public void setUid(uint new_uid)
    {
        uid = new_uid;
        dict[uid] = this;
    }

    private void Update()
    {
        //Debug.Log($"Transform.position: {transform.position.ToString("F4")}");
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        //Debug.Log($"Transform.position in fixed: {transform.position.ToString("F4")}");

        // Debug.Log("Fixed update");
        //Debug.Log($"Time between fixedUpdates: {diagStopWatch.Elapsed}");
        //Debug.Log($"fixedDeltaTime: {Time.fixedDeltaTime}");
        //Debug.Log($"deltaTime: {Time.deltaTime}");
        //diagStopWatch.Restart();
        //Debug.Log($"Time.fixedDeltaTime: {Time.fixedDeltaTime}");
        /*
                Vector2 movementVector = this.dest - VectorUtils.toVec2(transform.position);
                if (Vector2.Distance(VectorUtils.toVec2(transform.position), this.dest) <= Time.deltaTime * speed)
                {
                    characterRb.MovePosition(VectorUtils.toVec3(dest));
                }
                else
                {
                    movementVector.Normalize();
                    movementVector *= Time.deltaTime * speed;
                    characterRb.MovePosition(transform.position + VectorUtils.toVec3(movementVector));
                }
        */
        //animComponent.SetInteger("speed", Mathf.RoundToInt(speed * 100));

        //If server sent the position of a gameObject, register it, so it is applied on the gameObject afterwards
        if (shouldUpdateToServerPosition && serverRequestedPosition is Vector3 serverRequestedPositionValue)
        {
            Debug.Log($"Will move to position by newt tick: {serverRequestedPositionValue}");
            this.moveByNextTick(VectorUtils.toVec2(serverRequestedPositionValue));
            shouldUpdateToServerPosition = false;
            customAnimator.registerSpeed(Mathf.RoundToInt(speed * 100));
            customAnimator.animateGameObject();
        }


        Vector2 movementVector = this.dest - VectorUtils.toVec2(transform.position);
        if (movementVector.magnitude > 0 && Time.fixedDeltaTime * speed > 0)
        {
            //animComponent.SetFloat("runAnimMultiplier", speed / maxSpeed * 100);
            Debug.Log(VectorUtils.toVec3(this.dest));

            //Debug.Log("isWalking: true");
            //Debug.Log($"transform.position: {transform.position}");
            if (Vector2.Distance(VectorUtils.toVec2(transform.position), this.dest) <= Time.fixedDeltaTime * speed)
            {
                //Debug.Log("Case 1");
                characterRb.MovePosition(VectorUtils.toVec3(dest, transform.position.y));
            }
            else
            {
                //transform.LookAt(VectorUtils.toVec3(this.dest), Vector3.up);
                characterRb.MoveRotation(Quaternion.LookRotation(VectorUtils.toVec3(movementVector)));

                //Debug.Log("Case 2");
                //Debug.Log($"magnitude: {movementVector.magnitude}");
                movementVector.Normalize();
                //Debug.Log($"movementVector(normalized): {movementVector.ToString("F4")}");
                //Debug.Log($"movementVector(vec3 - normalized): {VectorUtils.toVec3(movementVector)}");


                movementVector *= Time.fixedDeltaTime * speed;
                characterRb.MovePosition(transform.position + VectorUtils.toVec3(movementVector));
 

            }
        }

    }

    void moveToLocationStraight(Vector2 arg_dest, float moveSpeed = 0)
    {
        this.dest = arg_dest;
        this.speed = moveSpeed;
    }

    public void setServerRequestedPosition(Vector3? newRequestedPosition)
    {
        //Debug.Log($"newRequestedPosition: {newRequestedPosition.ToString("F4")}");
        if (newRequestedPosition is Vector3 newRequestedPositionValue)
        {
            serverRequestedPosition = newRequestedPositionValue;
        }
        shouldUpdateToServerPosition = true;
    }

    public void setServerRequestedAction(int actionId, int actionFrame = 0)
    {
        //TODO
    }

    void moveByNextTick(Vector2 arg_dest)
    {

        Vector2 vec2Position = VectorUtils.toVec2(this.transform.position);
        float distance = Vector2.Distance(arg_dest, vec2Position);
        float Calculatedspeed = distance / ServerConstants.tick_duration;

        this.moveToLocationStraight(arg_dest, Calculatedspeed);
    }

    public void destroyGameObject()
    {
        Destroy(gameObject);
    }
}

