using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MultiplayerGameObjectAnimator
{
    private List<int> savedPreviousSpeedsFifo = new List<int>();
    private uint speedFifoSize = 10;
    private Animator gameObjectAnimator;
    private int maxSpeed;

    public MultiplayerGameObjectAnimator(Animator a_gameObjectAnimator, int a_maxSpeed)
    {
        gameObjectAnimator = a_gameObjectAnimator;
        maxSpeed = a_maxSpeed;
    }

    public void animateGameObject()
    {
        debug_print_list();
        if(savedPreviousSpeedsFifo.Count < 5)
        {
            return;
        }
        if(savedPreviousSpeedsFifo[savedPreviousSpeedsFifo.Count - 1] == 0)
        {
            gameObjectAnimator.SetInteger("speed", 0);
            gameObjectAnimator.SetFloat("runAnimMultiplier", 1);
            savedPreviousSpeedsFifo.Clear();
            return;
        }
        int medianSpeed = getMedianSpeed();
        gameObjectAnimator.SetInteger("speed", medianSpeed);
        gameObjectAnimator.SetFloat("runAnimMultiplier", (float)medianSpeed/(float)maxSpeed);
    }
    public void registerSpeed(int speed)
    {
        if(savedPreviousSpeedsFifo.Count >= speedFifoSize)
        {
            savedPreviousSpeedsFifo.RemoveAt(0);
        }
        savedPreviousSpeedsFifo.Add(Mathf.Min(speed, maxSpeed));
    }

    private int getMedianSpeed()
    {
        List<int> sortedSpeedList = new List<int>(savedPreviousSpeedsFifo);
        sortedSpeedList.Sort();
        if (sortedSpeedList.Count % 2 == 1)
        {
            int medianIndex = Mathf.FloorToInt(sortedSpeedList.Count / 2);
            return sortedSpeedList[medianIndex];
        }
        else
        {
            int medianIndex = sortedSpeedList.Count / 2;
            return (sortedSpeedList[medianIndex] + sortedSpeedList[medianIndex - 1]) / 2;
        }
    }


    private void debug_print_list()
    {
        string result = "List contents: ";
        foreach (var item in savedPreviousSpeedsFifo)
        {
            result += item.ToString() + ", ";
        }
        Debug.Log(result);
    }
}
