using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Linq;

/*
	Data format : requestType[1], {data}
	resquestType = 1: User login
	requestType = 2: get RTT (server request and response):
	clientTimestamp[8]
	requestType = 3: Sending server-computed gameState
	tick_id[1], gameObjectId[4], x[4], y[4], z[4] => buffer size = (gameObjects.size() * 4) + 1

*/

public class NetworkAdapter : MonoBehaviour
{
    private UdpClient client = new UdpClient();
    private IPAddress serverAddress;
    private byte[] receiveBytes;
    private Stopwatch requestsTimer = new Stopwatch();
    private NetworkedClock networkedClock = new NetworkedClock();

    IPEndPoint serverEndpoint;

    private bool isSyncing = true;
    private double msLatency;
    private DateTime beforeClientSend;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Connecting to server");
        serverAddress = IPAddress.Parse(ServerConstants.url);
        serverEndpoint = new IPEndPoint(serverAddress, ServerConstants.server_port);
        client.Connect(ServerConstants.url, ServerConstants.server_port);

        networkedClock.init(client);

        Debug.Log("Sent 3 bytes");
        /*      while (true)
              {
                  client.ReceiveAsync().ContinueWith((Task<UdpReceiveResult> receiveBytes) =>
                  {
                      Debug.Log("Received byte: ");
                      Debug.Log(receiveBytes);
                  });

              }*/

        client.BeginReceive(receiveCallback, null);

        //client.Close();
        
    }

    void sendData(byte[] dataToSend)
    {
        int dataSize = dataToSend.Length;
        Debug.Log($"Data size: {dataSize}");
        requestsTimer.Start();
        client.Send(dataToSend, dataSize);
        requestsTimer.Stop();
        Debug.Log($"Sending udp packet took: {requestsTimer.ElapsedMilliseconds} ms");


    }

    // Update is called once per frame
    private void routeServerData(byte[] receivedData)
    {
        switch (receivedData[0]) {
            case 2:
                //Client RTT response
                Int64 receiveTimeStamp = NetworkedClock.getLocalTimestampMs();
                Debug.Log("Received response to client RTT");
                Int64 serverTimeStamp = BitConverter.ToInt64(receivedData, 1);
                Int64 sendTimeStamp = BitConverter.ToInt64(receivedData, 9);
                networkedClock.handleTTSRes(sendTimeStamp, receiveTimeStamp, serverTimeStamp);
                break;
            case 3:
                Dictionary<uint, Vector3> newState = decodeReceivedData(receivedData);
                foreach (KeyValuePair<uint, Vector3> oneVectorPosition in newState)
                {
                    MultiplayerGameObject.dict[oneVectorPosition.Key].setServerRequestedPosition(oneVectorPosition.Value);
                }
                break;
            default:
                Debug.Log("Received unknown request type. Ignoring");
                break;


        }
        
    }

    private void receiveCallback(IAsyncResult res)
    {
        try
        {
            byte[] receivedData = client.EndReceive(res, ref serverEndpoint);
            if (isSyncing)
            {
                msLatency = (DateTime.Now - beforeClientSend).TotalMilliseconds;
                isSyncing = false;
                Debug.Log($"Initialization done: {msLatency} ms of latency");
            }
            client.BeginReceive(receiveCallback, null);
            Debug.Log("Received data from server;");
            if (receivedData.Length > 1)
            {
                Debug.Log("Going Here");
                this.routeServerData(receivedData);
                Debug.Log("Going There");
            }
        }
        catch
        {
            Debug.Log("Error in receiveCallbalk");
        }

    }

    //Data format: tick_id[1], gameObjectId[4], x[4], y[4], z[4] => buffer size = (gameObjects.size() * 4) + 1
    private Dictionary<uint, Vector3> decodeReceivedData(byte[] receivedData)
    {
        Dictionary<uint, Vector3> decodedData = new Dictionary<uint, Vector3>();
        //Debug.Log("data size: ");
        //Debug.Log(receivedData.Length);
        int objectsCount = (receivedData.Length -2 )/16;
        for(int i = 0; i<objectsCount; i++)
        {
            uint objectIndex = BitConverter.ToUInt32(receivedData, 2 + (16 * i));
            float x = BitConverter.ToSingle(receivedData, 6 + (16 * i));
            float y = BitConverter.ToSingle(receivedData, 10 + (16 * i));
            float z = BitConverter.ToSingle(receivedData, 14 + (16 * i));
            //Debug.Log("Got position: ");
            //Debug.Log(x);
            //Debug.Log(y);
            //Debug.Log(z);
            decodedData[objectIndex] = new Vector3(x, y, z);
        }

        return decodedData;

    }

    private void OnDestroy()
    {
        client.Dispose();
        this.client.Close();
    }
}
