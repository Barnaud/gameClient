using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameObject : MonoBehaviour
{

    static uint lastUid = 0;
    public static Dictionary<uint,  GameObject> dict = new Dictionary<uint, GameObject>();

    static uint assignUid(GameObject objectToAssign)
    {
        uint newUid = ++lastUid;
        dict[newUid] = objectToAssign;
        return newUid;
    }

    protected Animator characterAnim;
    protected Rigidbody characterRb;
    protected Vector2 dest;
    protected bool shouldMove = false;

    protected Vector3 serverRequestedPosition;
    protected bool shouldUpdateToServerPosition;
    protected float speed;

    protected Animator playerAnim;

    private System.Diagnostics.Stopwatch diagStopWatch;

    public uint uid;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Creating gameObject in start");
        //uid = assignUid(this);
        //Temp until auto uid assign
        dict[uid] = this;
        //end temp
        characterAnim = GetComponent<Animator>();
        characterRb = GetComponent<Rigidbody>();
        dest = VectorUtils.toVec2(transform.position);
        playerAnim = GetComponent<Animator>();
        diagStopWatch = new System.Diagnostics.Stopwatch();
        diagStopWatch.Start();

        //moveToLocationStraight(new Vector2(5, 6));
    }

    private void Update()
    {
        Debug.Log($"Transform.position: {transform.position.ToString("F4")}");
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        Debug.Log($"Transform.position in fixed: {transform.position.ToString("F4")}");

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
        if (shouldUpdateToServerPosition)
        {
            this.moveByNextTick(VectorUtils.toVec2(this.serverRequestedPosition));
            shouldUpdateToServerPosition = false;

        }
        Vector2 movementVector = this.dest - VectorUtils.toVec2(transform.position);
        if (movementVector.magnitude > 0 &&  Time.fixedDeltaTime * speed > 0)
        {
            playerAnim.SetBool("isWalking", true);
            //Debug.Log($"transform.position: {transform.position}");
            if (Vector2.Distance(VectorUtils.toVec2(transform.position), this.dest) <= Time.fixedDeltaTime * speed)
            {
                Debug.Log("Case 1");
                characterRb.MovePosition(VectorUtils.toVec3(dest, transform.position.y));
            }
            else
            {
                Debug.Log("Case 2");
                //Debug.Log($"magnitude: {movementVector.magnitude}");
                movementVector.Normalize();
                //Debug.Log($"movementVector(normalized): {movementVector.ToString("F4")}");
                //Debug.Log($"movementVector(vec3 - normalized): {VectorUtils.toVec3(movementVector)}");


                movementVector *= Time.fixedDeltaTime * speed;
                characterRb.MovePosition(transform.position + VectorUtils.toVec3(movementVector));
 

            }
        }
        else
        {
            Debug.Log("Case 3");
            playerAnim.SetBool("isWalking", false);
            characterRb.MovePosition(transform.position);
        }



    }

    void moveToLocationStraight(Vector2 arg_dest, float moveSpeed = 0)
    {
        this.shouldMove = true;
        this.dest = arg_dest;
        this.speed = moveSpeed;
    }

    public void setServerRequestedPosition(Vector3 newRequestedPosition)
    {
        Debug.Log($"newRequestedPosition: {newRequestedPosition.ToString("F4")}");
        serverRequestedPosition = newRequestedPosition;
        shouldUpdateToServerPosition = true;
    }
    void moveByNextTick(Vector2 arg_dest)
    {

        Vector2 vec2Position = VectorUtils.toVec2(this.transform.position);
        float distance = Vector2.Distance(arg_dest, vec2Position);
        float Calculatedspeed = distance / ServerConstants.tick_duration;

        this.moveToLocationStraight(arg_dest, Calculatedspeed);
    }
}

