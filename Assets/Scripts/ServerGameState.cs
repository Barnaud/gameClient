using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ServerGameObjectState
{
    Vector3 position;
    private int actionId;
    private int actionFrame;

    public ServerGameObjectState(Vector3 position, int actionId = 0, int actionFrame = 0)
    {
        this.position = position;
        this.actionId = actionId;
        this.actionFrame = actionFrame;
    }

    public ServerGameObjectState()
    {
    }

    public Vector3 Position => position; 
    public int ActionId => actionId;
    public int ActionFrame => actionFrame; 

    public void setPosition(Vector3 newPosition)
    {
        position = newPosition;
    }

    public void setActionState(int newActionId, int newActionFrame = 0)
    {
        actionId = newActionId;
        actionFrame = newActionFrame;
    }

}

public class ServerGameState 
{

    const int oneGameObjectByteSize = 24;
    const int stateHeaderByteSize = 9; //size of server sent states content that is not about a gameobject.
    const int deltaHeaderByteSize = 9; //size of server sent states content that is not about a gameobject.


    private Int64 timestamp;
    private Dictionary<uint, ServerGameObjectState> gameObjectsStates = new Dictionary<uint, ServerGameObjectState>();

    public long Timestamp => timestamp;
    public Dictionary<uint, ServerGameObjectState> GameObjectsStates  => gameObjectsStates;

    public static ServerGameState fromServerStateRequest(byte[] serverSentState)
    {
        ServerGameState serverStateToReturn = new ServerGameState();
        serverStateToReturn.timestamp = BitConverter.ToInt64(serverSentState, 1);

        int objectCount = (serverSentState.Length - stateHeaderByteSize) / oneGameObjectByteSize;
        for (int readGameObjectRank = 0; readGameObjectRank < objectCount; readGameObjectRank++)
        {
            int readGameObjectStartIndex = stateHeaderByteSize + (readGameObjectRank * oneGameObjectByteSize);
            KeyValuePair<uint, ServerGameObjectState> deserializedGameObject = readOneGameObjectState(ref serverSentState, readGameObjectStartIndex);
            serverStateToReturn.gameObjectsStates.Add(deserializedGameObject.Key, deserializedGameObject.Value);
        }

        return serverStateToReturn;


    }

    public static ServerGameState fromServerDeltaRequest(byte[] serverSentDelta)
    {
        ServerGameState serverStateToReturn = new ServerGameState();
        serverStateToReturn.timestamp = BitConverter.ToInt64(serverSentDelta, 1);
        KeyValuePair<uint, ServerGameObjectState> deserializedGameObject = readOneGameObjectState(ref serverSentDelta, deltaHeaderByteSize);
        serverStateToReturn.gameObjectsStates.Add(deserializedGameObject.Key, deserializedGameObject.Value);
        return serverStateToReturn;


    }

    static KeyValuePair<uint, ServerGameObjectState> readOneGameObjectState(ref byte[] serverSentState, int startIndex)
    {


        ServerGameObjectState GameObjectToReturn = new ServerGameObjectState();
        Vector3 readGameObjectPosition = new Vector3();

        const int idIndex = 0;
        const int xIndex = 4;
        const int yIndex = 8;
        const int zIndex = 12;
        const int actionIdIndex = 16;
        const int actionFrameIndex = 20;

        uint gameObjectid = BitConverter.ToUInt32(serverSentState, startIndex + idIndex);
        readGameObjectPosition.x = BitConverter.ToSingle(serverSentState, startIndex + xIndex);
        readGameObjectPosition.y = BitConverter.ToSingle(serverSentState, startIndex + yIndex);
        readGameObjectPosition.z = BitConverter.ToSingle(serverSentState, startIndex + zIndex);
        int gameObjectActionId = BitConverter.ToInt32(serverSentState, startIndex + actionIdIndex);
        int gameObjectActionFrame = BitConverter.ToInt32(serverSentState, startIndex + actionFrameIndex);

        GameObjectToReturn.setPosition(readGameObjectPosition);
        GameObjectToReturn.setActionState(gameObjectActionId, gameObjectActionFrame);

        return new KeyValuePair<uint, ServerGameObjectState>(gameObjectid, GameObjectToReturn);


    }

}
