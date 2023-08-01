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

enum clientRequestTypes: byte
{
    userLogin = 1,
    getRtt = 2,
    sendClientState = 4,
    ackRequest = 5
};

enum serverResponseTypes: byte
{
    getRtt = 2,
    sendServerState=3,
    ackRequest = 5,
    sendServerDelta=6,
};


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

    private Int64 lastServerStateUpdateTimestamp = 0;

    public GameObject playerCharacterPrefab;
    public GameObject otherCharacterPrefab;
    private int playerUid = 0;
    private GameObject playerCharacter;
    private List<KeyValuePair<uint, ServerGameObjectState>> scheduledGameObjectInstantiations = new List<KeyValuePair<uint, ServerGameObjectState>>();
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

        byte[] data = { (byte)clientRequestTypes.userLogin, 0, 0 };
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

    void Update()
    {
        if(playerUid != 0 && !playerCharacter)
        {
            instantiatePlayer(playerUid);
            //playerUid = 0;
        }
        instantiateScheduledGameObjects();
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
    void routeServerData(byte[] receivedData)
    {
        switch (receivedData[0]) {
            case (byte)serverResponseTypes.getRtt:
                //Client RTT response
                Int64 receiveTimeStamp = NetworkedClock.getLocalTimestampMs();
                Debug.Log("Received response to client RTT");
                Int64 serverTimeStamp = BitConverter.ToInt64(receivedData, 1);
                Int64 sendTimeStamp = BitConverter.ToInt64(receivedData, 9);
                networkedClock.handleTTSRes(sendTimeStamp, receiveTimeStamp, serverTimeStamp);
                break;
            case (byte)serverResponseTypes.sendServerState:
                this.ackRequest(ref receivedData);
                ServerGameState newState = ServerGameState.fromServerStateRequest(receivedData);
                if(newState.Timestamp < lastServerStateUpdateTimestamp)
                {
                    Debug.Log("Ignored state because it's too old");
                    return;
                }
                lastServerStateUpdateTimestamp = newState.Timestamp;
                //KeyValuePair<Int64, Dictionary<uint, Vector3>> newState = decodeReceivedGameState(receivedData);
                foreach (KeyValuePair<uint, ServerGameObjectState> oneGameObjectWithUid in newState.GameObjectsStates)
                {
                    uint oneObjectUid = oneGameObjectWithUid.Key;
                    ServerGameObjectState oneGameObject = oneGameObjectWithUid.Value;
                    if (oneObjectUid == playerUid)
                    {
                        //current object is the player
                        characterControllablePlayer.setServerRequestedPosition(newState.Timestamp, oneGameObject.Position);
                    }
                    else if(MultiplayerGameObject.dict.ContainsKey(oneObjectUid)) {
                        //current object is not a player, but an already instantiated object
                        MultiplayerGameObject.dict[oneObjectUid].setServerRequestedPosition(oneGameObject.Position);
                    }
                    else
                    {
                        //current object is new to client and needs to be instantiated
                        scheduleGameObjectInstantiation(oneObjectUid, oneGameObject);
                    }
                }
                break;
            case (byte)serverResponseTypes.ackRequest:
                short requestId = BitConverter.ToInt16(receivedData, 1);
                byte[] originalRequest = reliableRequester.handleResponse(requestId);
                routeServerResponse(originalRequest, receivedData);
                break;

            case (byte)serverResponseTypes.sendServerDelta:
                ackRequest(ref receivedData);
                ServerGameState characterGameState = ServerGameState.fromServerDeltaRequest(receivedData);
                Debug.Log(characterGameState);
                if(characterGameState.Timestamp < lastServerStateUpdateTimestamp)
                {
                    Debug.Log("Ignored delta because it's too old");
                    return; 
                }

                List<uint> notYetUpdatedGameObjectsPositions = MultiplayerGameObject.dict.Keys.ToList<uint>();
                characterControllablePlayer.setServerRequestedPosition(characterGameState.Timestamp, characterGameState.GameObjectsStates[(uint)playerUid].Position);

                ServerStatesDelta serverStateDelta = ServerStatesDelta.fromServerBuffer(receivedData);
                foreach(StateDelta oneDelta in serverStateDelta.GameObjectsStateDelta)
                {
                    switch (oneDelta.ChangeType)
                    {
                        case (changeTypes.created):
                            Debug.Log("Received created");
                            if(!(oneDelta.NewPosition is Vector3 newObjectPosition))
                            {
                                Debug.Log("Not all values were given to instantiate this object. Ignoring it for now");
                                continue;
                            }
                            //Todo: vérifier que l'objet avec cet uid n'est pas déjà instantié avant d'en re-instantier un, sinon, ignorer (on reçevra une requête "update dès que l'ack arrivera au serveur)
                            ServerGameObjectState newGameObjectState = new ServerGameObjectState(newObjectPosition, oneDelta.NewActionId ?? 0, oneDelta.NewActionFrame ?? 0);
                            scheduleGameObjectInstantiation(oneDelta.Uid, newGameObjectState);
                            break;
                        case (changeTypes.updated):
                            //TODO: vérifier que l'objet existe bien, sinon, ignorer requête (l'objet a probablement été supprimé avant, le serveur n'aurait pas envoyé de "update" si on n'avait 
                            if (oneDelta.NewPosition is Vector3 updatedObjectPosition)
                            {
                                MultiplayerGameObject.dict[oneDelta.Uid].setServerRequestedPosition(updatedObjectPosition);
                            }
                            else
                            {
                                notYetUpdatedGameObjectsPositions.Remove(oneDelta.Uid);
                            }
                            if (oneDelta.NewActionId is int updatedObjectActionId)
                            {
                                MultiplayerGameObject.dict[oneDelta.Uid].setServerRequestedAction(updatedObjectActionId, oneDelta.NewActionFrame ?? 0);
                            }
                            break;
                        case (changeTypes.deleted):
                            MultiplayerGameObject.dict[oneDelta.Uid].destroyGameObject();
                            notYetUpdatedGameObjectsPositions.Remove(oneDelta.Uid);
                            break;
                    }
                }

                foreach(uint oneNotUpdatedGameObjectUid in notYetUpdatedGameObjectsPositions)
                {
                    MultiplayerGameObject.dict[oneNotUpdatedGameObjectUid].setServerRequestedPosition(null);
                }

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
            case (byte)clientRequestTypes.userLogin:
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
            Debug.LogError($"Error in receiveCallbalk: {ex.Message}");
        }

    }

    private void ackRequest(ref byte[] receivedData, int timestampIndex=1)
    {
        byte[] responseBuffer = new byte[9];
        responseBuffer[0] = (byte) clientRequestTypes.ackRequest; 
        for(int i = timestampIndex; i <= timestampIndex + 7; i++)
        {
            responseBuffer[i] = receivedData[i];
        }
        this.sendData(responseBuffer);
    }

    public void sendPlayerGameState(ControllablePlayer player)
    {
        /*Data to send format: 
         * requestType[1]: 4, serverTime[8], x[4], y[4], z[4], r[4]
        */
        byte[] dataToSend = new byte[25];
        dataToSend[0] = (byte)clientRequestTypes.sendClientState;
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

    private void scheduleGameObjectInstantiation(uint gameObjectId, ServerGameObjectState scheduledGameObject)
    {
        Debug.Log($"Scheduled instantation of gameObject with uid: ${gameObjectId}");
        scheduledGameObjectInstantiations.Add(new KeyValuePair<uint, ServerGameObjectState>(gameObjectId, scheduledGameObject));
    }

    private void instantiateScheduledGameObjects()
    {
        foreach(KeyValuePair<uint, ServerGameObjectState> oneObjectToInstantiate in scheduledGameObjectInstantiations)
        {
            if (!MultiplayerGameObject.dict.ContainsKey(oneObjectToInstantiate.Key))
            {
                Debug.Log($"Instantiating GameObject with uid: ${oneObjectToInstantiate.Key}");
                instantiateGameObject(oneObjectToInstantiate.Key, oneObjectToInstantiate.Value.Position);
            }
        }
        scheduledGameObjectInstantiations.Clear();
    }

    private void instantiateGameObject(uint uid, Vector3 position)
    {
        Debug.Log($"Instantiating gameObject with uid: {uid}");
        GameObject newGameObject = Instantiate(otherCharacterPrefab, position, Quaternion.identity);
        MultiplayerGameObject newMultiplayerGameObject = newGameObject.GetComponent<MultiplayerGameObject>();
        newMultiplayerGameObject.setUid(uid);
    }
}
