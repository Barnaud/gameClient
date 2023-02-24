using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;


public class NetworkedClock
{
    private UdpClient udpClient;
    private int[] measuredRTTs = new int[9];
    private int[] measuredClockDeltas = new int[9];
    private char measuredRttCount = (char) 0;
    private bool hasEnoughValues = false;

    //void requestRTT(client)
    public void init(UdpClient client)
    {
        udpClient = client;
        char validMeasureCount = (char) 0;
        /*        for (char i = (char) 0; i < 9; i++)
                {
                    requestTTS();
                    Task.Delay(100).Wait();



                }*/

        Task requestMultipleTTS = Task.Run(async () =>
        {
            while (true)
            {
                requestTTS();
                await Task.Delay(100);

            }
        });



    }

    public static Int64 getLocalTimestampMs()
    {
        return (Int64)(DateTime.UtcNow.Subtract(
        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        ).TotalMilliseconds);

    }

    public Int64 getRemoteTimestampMs()
    {
        Debug.Log($"LocalTimeStamp: {NetworkedClock.getLocalTimestampMs()}");
        Debug.Log($"Delta: {getMedianClockDeltas()}");
        return NetworkedClock.getLocalTimestampMs() + getMedianClockDeltas();
    }

    private void requestTTS()
    {
        byte[] request = new byte[9];
        request[0] = 2;
        Buffer.BlockCopy(BitConverter.GetBytes(getLocalTimestampMs()), 0, request, 1, 8);
        udpClient.Send(request, 9);

    }

    public void handleTTSRes(Int64 sendTimeStamp, Int64 receiveTimeStamp, Int64 serverTimeStamp)
    {
        int rtt = (int)(receiveTimeStamp - sendTimeStamp);
        int clocksDelta =(int) ((serverTimeStamp + (rtt / 2)) - receiveTimeStamp);
     /*   if (!hasEnoughValues)
        {
            measuredRTTs[++measuredRttCount] = rtt;
            measuredClockDeltas[measuredRttCount] = clocksDelta;
            if (measuredRttCount >= 7)
            {
                hasEnoughValues = true;
            }

        }*/
        /*else
        {*/
        measuredRTTs[measuredRttCount] = rtt;
        measuredClockDeltas[measuredRttCount] = clocksDelta;
        measuredRttCount = (char)(++measuredRttCount % 9);
        hasEnoughValues = hasEnoughValues || measuredRttCount == 0;
        if (hasEnoughValues)
        {
            Debug.Log($"MedianRTT: {getMedianRtt()}");
        }
        //Debug.Log($"Median RTT: {getMedianRtt()}");
        //}

    }

    public int getMedianRtt()
    {
        if (!hasEnoughValues)
        {
            return -1;
        }
        int[] sortedRTTs = (int[])measuredRTTs.Clone();
        Array.Sort(sortedRTTs);
        return sortedRTTs[4];
    }

    public bool isReady()
    {
        return hasEnoughValues;
    }

    private int getMedianClockDeltas()
    {
        if (!hasEnoughValues)
        {
            Debug.LogWarning("Trying to get medianClockDelta while MetworkedClock is not initialized");
            return 0;
        }
        int[] sortedDeltas = (int[])measuredClockDeltas.Clone();
        Array.Sort(sortedDeltas);
        return sortedDeltas[4];
    }

    private Int64 getServerTime()
    {
        if (hasEnoughValues)
        {
            return getLocalTimestampMs() + getMedianClockDeltas();
        }
        Debug.LogWarning("Tried to get server time while networkedClock is not initialized");
        return getLocalTimestampMs();
    }



}
