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
    private ReliableRequester reliableRequester;

    IPEndPoint serverEndpoint;

    private bool isSyncing = true;
    private double msLatency;
    private DateTime beforeClientSend;

    public GameObject playerCharacterPrefab;
    public GameObject otherCharacterPrefab;
    private int playerUid = 0;
    private GameObject playerCharacter;
    //tmp
    private GameObject newGameObject;
    //end tmp
    private ControllablePlayer characterControllablePlayer;

    public static NetworkAdapter networkAdapterInstance = null;

    // Start is called before the first frame update
    void Start()
    {
        reliableRequester = new ReliableRequester(client, networkedClock);
        Debug.Log("Connecting to server");
        serverAddress = IPAddress.Parse(ServerConstants.url);
        serverEndpoint = new IPEndPoint(serverAddress, ServerConstants.server_port);
        client.Connect(ServerConstants.url, ServerConstants.server_port);

        NetworkAdapter.networkAdapterInstance = this;

        byte[] data = { 1, 0, 0 };
        reliableRequester.sendRequest(data, 0, 2000);

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

    private void Update()
    {
        if(playerUid != 0 && !playerCharacter)
        {
            instantiatePlayer(playerUid);
            //playerUid = 0;
        }
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
                KeyValuePair<Int64, Dictionary<uint, Vector3>> newState = decodeReceivedGameState(receivedData);
                foreach (KeyValuePair<uint, Vector3> oneVectorPosition in newState.Value)
                {
                    if (oneVectorPosition.Key == playerUid)
                    {
                        //current object is the player
                        characterControllablePlayer.setServerRequestedPosition(newState.Key, oneVectorPosition.Value);
                    }
                    else if(MultiplayerGameObject.dict.ContainsKey(oneVectorPosition.Key)) {
                        //current object is not a player, but an already instantiated object
                        MultiplayerGameObject.dict[oneVectorPosition.Key].setServerRequestedPosition(oneVectorPosition.Value);
                    }
                    else
                    {
                        //current object is new to client and needs to be instantiated
                        instantiateGameObject(oneVectorPosition.Key, oneVectorPosition.Value);
                    }
                }
                break;
            case 5:
                short requestId = BitConverter.ToInt16(receivedData, 1);
                byte[] originalRequest = reliableRequester.handleResponse(requestId);
                routeServerResponse(originalRequest, receivedData);
                break;
            default:
                Debug.Log("Received unknown request type. Ignoring");
                break;


        }
        
    }

    private void routeServerResponse(byte[] originalRequest, byte[] response)
    {
        byte requestType = originalRequest[0];
        switch (requestType)
        {
            case 1:
                int uid = BitConverter.ToInt32(response, 3);
                playerUid = uid;
                Debug.Log($"Received player uid from server: {playerUid}");
                break;
            default:
                Debug.Log($"Received response from server for request with unknow requestType ID: {requestType} ");
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
                this.routeServerData(receivedData);
            }
        }
        catch(Exception ex)
        {
            Debug.Log($"Error in receiveCallbalk: ${ex.Message}");
        }

    }

    //Data format: server_timestamp[8], gameObjectId[4], x[4], y[4], z[4] => buffer size = (gameObjects.size() * 4) + 1
    private KeyValuePair<Int64, Dictionary<uint, Vector3>> decodeReceivedGameState(byte[] receivedData)
    {
        Dictionary<uint, Vector3> decodedData = new Dictionary<uint, Vector3>();
        //Debug.Log("data size: ");
        //Debug.Log(receivedData.Length);
        Int64 server_timestamp = BitConverter.ToInt64(receivedData, 1);
        int objectsCount = (receivedData.Length - 9 )/16;
        for(int i = 0; i<objectsCount; i++)
        {
            uint objectIndex = BitConverter.ToUInt32(receivedData, 9 + (16 * i));
            float x = BitConverter.ToSingle(receivedData, 13 + (16 * i));
            float y = BitConverter.ToSingle(receivedData, 17 + (16 * i));
            float z = BitConverter.ToSingle(receivedData, 21 + (16 * i));
            //Debug.Log("Got position: ");
            //Debug.Log(x);
            //Debug.Log(y);
            //Debug.Log(z);
            decodedData[objectIndex] = new Vector3(x, y, z);
        }

        return new KeyValuePair<Int64, Dictionary<uint, Vector3>>(server_timestamp, decodedData);

    }

    public void sendPlayerGameState(ControllablePlayer player)
    {
        /*Data to send format: 
         * requestType[1]: 4, serverTime[8], x[4], y[4], z[4], r[4]
        */
        byte[] dataToSend = new byte[25];
        dataToSend[0] = 4;
        //push to data as byte list to dataToSend
        Buffer.BlockCopy(BitConverter.GetBytes(networkedClock.getRemoteTimestampMs()), 0, dataToSend, 1, 8);
        Buffer.BlockCopy(BitConverter.GetBytes(player.transform.position.x), 0, dataToSend, 9, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(player.transform.position.y), 0, dataToSend, 13, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(player.transform.position.z), 0, dataToSend, 17, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(player.transform.rotation.y * Mathf.Rad2Deg), 0, dataToSend, 21, 4);

        client.Send(dataToSend, 25);
    }

    public bool isReady()
    {
        return this.networkedClock.isReady();
    }

    private void OnDestroy()
    {
        client.Dispose();
        this.client.Close();
    }

    private void instantiatePlayer(int uid)
    {
        if (!playerCharacter)
        {
            Debug.Log($"Instantiating player with uid: {uid}");
            playerCharacter = Instantiate(playerCharacterPrefab, new Vector3(0,0,0), Quaternion.identity);
        }
        characterControllablePlayer = playerCharacter.GetComponent<ControllablePlayer>();
        characterControllablePlayer.uid = uid;
        characterControllablePlayer.networkedClock = networkedClock;

    }

    private void instantiateGameObject(uint uid, Vector3 position)
    {
        Debug.Log($"Instantiating gameObject with uid: {uid}");
        newGameObject = Instantiate(otherCharacterPrefab, position, Quaternion.identity);
        MultiplayerGameObject newMultiplayerGameObject = newGameObject.GetComponent<MultiplayerGameObject>();
        newMultiplayerGameObject.setUid(uid);
    }
}
