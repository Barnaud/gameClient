using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class GameStateStore
{

    static KeyValuePair<long, Vector3> nullState = new KeyValuePair<long, Vector3>(0, Vector3.zero);
    //private KeyValuePair<Int64, float[]> = new 
    private List<KeyValuePair<Int64, Vector3>> internalQueue = new List<KeyValuePair<Int64, Vector3>>();
    private int queueSize;

    public GameStateStore(int a_queueSize)
    {
        queueSize = a_queueSize;

    }
    public void pushState(KeyValuePair<Int64, Vector3> oneState)
    {
        if(internalQueue.Count >= queueSize)
        {
            internalQueue.RemoveAt(0);
        }
        internalQueue.Add(oneState);

    }

    public Int64 getLastStateIndex(Int64 timestamp)
    {
            for(int i = internalQueue.Count - 1; i>=0; i--)
            {
                if (internalQueue[i].Key <= timestamp)
                {
                    return i;

                }

            }
        return -1;

    }

    public KeyValuePair<Int64, Vector3> getLastState(Int64 timestamp)
    {
        int index = (int) getLastStateIndex(timestamp);
        if(index < 0)
        {
            return nullState;
        }
        return internalQueue[index];
    }

    public List<KeyValuePair<Int64, Vector3>> getQueueSince(Int64 timestamp)
    {
        int index = (int) getLastStateIndex(timestamp);
        return internalQueue.GetRange(index, internalQueue.Count - index);
    }

}
