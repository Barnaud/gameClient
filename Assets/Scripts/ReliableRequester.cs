using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Threading.Tasks;
using System;

public class ReliableRequester
{
    UdpClient client;
    short currentRequestId = 0;
    Dictionary<short, byte[]> pendingRequests = new Dictionary<short, byte[]>();
    NetworkedClock networkedClock;
    // Start is called before the first frame update
    
    public ReliableRequester(UdpClient arg_client, NetworkedClock arg_networkedClock)
    {
        client = arg_client;
        networkedClock = arg_networkedClock;

    }


    /**Sends a requests that needs to be acked
     * if ack is not received, it will be sent again RTT * 1.5 later until a ack is received
     * WARNING: always set data[1] and data[2] empty, it will contain request id
     * **/
    public void sendRequest(byte[] data, int maxRetries = 0, int retryEveryMs=0)
    {
        //data[1] = (byte) ++currentRequestId;
        short requestId = ++currentRequestId;
        byte[] currentRequestBytes = BitConverter.GetBytes(requestId);
        data[1] = currentRequestBytes[0];
        data[2] = currentRequestBytes[1];
        int dataSize = data.Length;
        pendingRequests.Add(requestId, data);
        client.Send(data, dataSize);
        pendingRequests[data[1]] = data;
        Task sendTask = Task.Run(async () =>
        {
            int retries = 1;
            await Task.Delay(retryEveryMs != 0 ? retryEveryMs : getRequestDelay());
            while((maxRetries == 0 || retries <= maxRetries) && pendingRequests.ContainsKey(requestId)){
                client.Send(data, dataSize);
                retries++;
                Debug.Log($"Retrying to send request with id: {requestId}");
                await Task.Delay(getRequestDelay());
            }
        });


    }

    public byte[] handleResponse(short requestId)
    {
        Debug.Log($"Got ACK from request {requestId}");
        byte[] originalResponse = pendingRequests[requestId];
        pendingRequests.Remove(requestId);
        return originalResponse;
    }

    public void handleResponse(byte[] requestId)
    {   if (requestId.Length == 2)
        {
            handleResponse(BitConverter.ToInt16(requestId, 0));
        }
        else
        {
            Debug.LogWarning("Passed invalid requestId (size) to relaibleRequester.handleResponse");
        }
    }

    private int getRequestDelay()
    {
        if (!networkedClock.isReady())
        {
            return 50;
        }
        return (int)(networkedClock.getMedianRtt() * 1.5);
    }
/*    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }*/
}
