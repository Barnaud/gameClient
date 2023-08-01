using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

enum changeTypes : byte
{
    none=0,
    created=1,
    updated=2, 
    deleted=3
}

enum dataId: byte
{
    position=1,
    actionId=2,
    endOfObject=0xff
}
public class StateDelta
{
    changeTypes changeType;
    uint uid;

    Vector3? newPosition = null;
    int? newActionId = null;
    int? newActionFrame = null;

    internal changeTypes ChangeType => changeType; 
    public uint Uid => uid;
    public Vector3? NewPosition => newPosition;
    public int? NewActionId => newActionId;
    public int? NewActionFrame => newActionFrame;

    public static StateDelta fromServerBuffer(ref byte[] serverBuffer, ref int cursor)
    {
        StateDelta deltaToReturn = new StateDelta();
        deltaToReturn.uid = BitConverter.ToUInt32(serverBuffer, cursor);
        cursor += 4;
        deltaToReturn.changeType = (changeTypes) serverBuffer[cursor++];
        while (cursor<serverBuffer.Length)
        {
            dataId oneDataId = (dataId)serverBuffer[cursor++];
            switch (oneDataId)
            {
                case dataId.endOfObject:
                    return deltaToReturn;
                    break;
                case dataId.position:
                    deltaToReturn.newPosition = StateDelta.positionFromServerBuffer(ref serverBuffer, ref cursor);
                    break;
                case dataId.actionId:
                    Tuple<int, int> newActionInfo = StateDelta.actionInfoFromServerBuffer(ref serverBuffer, ref cursor);
                    deltaToReturn.newActionId = newActionInfo.Item1;
                    deltaToReturn.newActionFrame = newActionInfo.Item2;
                    break;

            }

        }
        throw new Exception("Found incomplete statedelta (did not finish with 0xff)");
    }

    static Vector3 positionFromServerBuffer(ref byte[] serverBuffer, ref int cursor)
    {
        Vector3 positionToReturn = new Vector3();
        positionToReturn.x = BitConverter.ToSingle(serverBuffer, cursor);
        cursor += 4;
        positionToReturn.y = BitConverter.ToSingle(serverBuffer, cursor);
        cursor += 4;
        positionToReturn.z = BitConverter.ToSingle(serverBuffer, cursor);
        cursor += 4;
        return positionToReturn;
    }

    static Tuple<int, int> actionInfoFromServerBuffer(ref byte[] serverBuffer, ref int cursor)
    {
        int actionId = BitConverter.ToInt32(serverBuffer, cursor);
        cursor += 4;
        int actionframe = BitConverter.ToInt32(serverBuffer, cursor);
        cursor += 4;
        return new Tuple<int, int>(actionId, actionframe);
    }

}

public class ServerStatesDelta
{
    const int headerByteSize = 33;

    Int64 timestamp;
    List<StateDelta> gameObjectsStateDelta = new List<StateDelta>();

    public List<StateDelta> GameObjectsStateDelta => gameObjectsStateDelta;
    public static ServerStatesDelta fromServerBuffer(byte[] serverBuffer)
    {
        ServerStatesDelta serverStateDeltaToReturn = new ServerStatesDelta();
        serverStateDeltaToReturn.timestamp =  BitConverter.ToInt64(serverBuffer, 1);
        int cursor = headerByteSize;
        while (cursor < serverBuffer.Length)
        {
            serverStateDeltaToReturn.gameObjectsStateDelta.Add(StateDelta.fromServerBuffer(ref serverBuffer, ref cursor));
        }
        return serverStateDeltaToReturn;

    }

}
