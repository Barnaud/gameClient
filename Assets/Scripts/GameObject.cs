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
    protected Vector2 previousServerPosition;

    public uint uid;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Creating gameObject in start");
        uid = assignUid(this);
        characterAnim = GetComponent<Animator>();
        characterRb = GetComponent<Rigidbody>();
        dest = VectorUtils.toVec2(transform.position);
        previousServerPosition = dest;

        //moveToLocationStraight(new Vector2(5, 6));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
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
            if (Vector2.Distance(VectorUtils.toVec2(transform.position), this.dest) <= Time.fixedDeltaTime * speed)
            {
                characterRb.MovePosition(VectorUtils.toVec3(dest));
            }
            else
            {
                Debug.Log($"magnitude: {movementVector.magnitude}");
                movementVector.Normalize();
                Debug.Log($"movementVector(normalized): {movementVector.ToString("F4")}");
                Debug.Log($"movementVector(vec3 - normalized): {VectorUtils.toVec3(movementVector)}");
                Debug.Log($"transform.position: {transform.position}");


                movementVector *= Time.fixedDeltaTime * speed;
                Debug.Log($"movementVector(vec3 - normalized - changed): {VectorUtils.toVec3(movementVector)}");
                characterRb.MovePosition(transform.position + VectorUtils.toVec3(movementVector));
            }
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
        serverRequestedPosition = newRequestedPosition;
        shouldUpdateToServerPosition = true;
    }
    void moveByNextTick(Vector2 arg_dest, int extrapolateTicks = 1)
    {
        Vector2 vec2Position = VectorUtils.toVec2(this.transform.position);
        float distance = Vector2.Distance(arg_dest, vec2Position);
        float Calculatedspeed = distance / ServerConstants.tick_duration;
        //make vector longer in case the next tick is not received
        Debug.Log($"Arg_dest_1: {arg_dest.ToString("F4")}");
        Debug.Log($"previousServerPosition: {previousServerPosition.ToString("F4")}");
        Vector2 extrapoled_dest = ((arg_dest - previousServerPosition) * (extrapolateTicks + 1)) + previousServerPosition;
        Debug.Log($"vec2Position: {vec2Position.ToString("F4")}");
        Debug.Log($"extrapoleddest_2: {extrapoled_dest.ToString("F4")}");
        this.moveToLocationStraight(extrapoled_dest, Calculatedspeed);
        previousServerPosition = arg_dest;
    }
}

