using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System;
using System.ComponentModel;


public class NetworkAdapter : MonoBehaviour
{
    private UdpClient client = new UdpClient();
    private IPAddress serverAddress;
    private byte[] receiveBytes;
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

        byte[] sendBytes = { 2, 8, 6 };
        beforeClientSend = DateTime.Now;
        client.Send(sendBytes, 3);
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

    // Update is called once per frame
    void Update()
    {
        
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
                Dictionary<uint, Vector3> newState = decodeReceivedData(receivedData);
                foreach (KeyValuePair<uint, Vector3> oneVectorPosition in newState)
                {
                    GameObject.dict[oneVectorPosition.Key].setServerRequestedPosition(oneVectorPosition.Value);
                }
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
        int objectsCount = (receivedData.Length -1 )/16;
        for(int i = 0; i<objectsCount; i++)
        {
            uint objectIndex = BitConverter.ToUInt32(receivedData, 1 + (16 * i));
            float x = BitConverter.ToSingle(receivedData, 5 + (16 * i));
            float y = BitConverter.ToSingle(receivedData, 9 + (16 * i));
            float z = BitConverter.ToSingle(receivedData, 13 + (16 * i));
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
