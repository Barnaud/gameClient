using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MultiplayerGameObject
{

    public float default_speed;
    // Start is called before the first frame update

    void moveToLocationStraight(Vector2 arg_dest, float moveSpeed = 0)
    {
        this.shouldMove = true;
        this.dest = arg_dest;
        this.speed = moveSpeed != 0 ? moveSpeed : this.default_speed;
    }


}
