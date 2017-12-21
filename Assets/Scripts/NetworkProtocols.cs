using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NetworkMessageType
{
    None = 0,
    LobbyMessage,

}

public abstract class NetworkProtocol
{

    public bool isValid
    {
        get { return _isValid; }
    }
    protected bool _isValid;

    public int appIdentifier;
    public byte messageType;
    public string senderGuid;
    public int myPort;

    public static bool TryParseBasicInfo(byte[] udpMessage, out int _appIndentifier, out byte _messageType)
    {

        _appIndentifier = 0;
        _messageType = 0;
        if (udpMessage.Length < 5)
            return false;

        int index = 0;
        index = PullItem(udpMessage, ref _appIndentifier, index);
        index = PullItem(udpMessage, ref _messageType, index);

        return true;
    }

    public NetworkProtocol()
    {
        _isValid = true;
    }

    public NetworkProtocol(byte[] udpMessage)
    {
        _isValid = true;
        Parse(udpMessage);
    }

    public virtual int Parse(byte[] udpMessage)
    {
        int startIndex = 0;

        if (udpMessage.Length < 12)
        {
            _isValid = false;
            return startIndex;
        }

        startIndex = PullItem(udpMessage, ref appIdentifier, startIndex);
        startIndex = PullItem(udpMessage, ref messageType, startIndex);
        startIndex = PullItem(udpMessage, ref senderGuid, startIndex);
        startIndex = PullItem(udpMessage, ref myPort, startIndex);

        return startIndex;
    }

    protected static int PullItem(byte[] buffer, ref int value, int startIndex)
    {
        value = System.BitConverter.ToInt32(buffer, startIndex);
        startIndex += sizeof(int);
        return startIndex;
    }

    protected static int PullItem(byte[] buffer, ref byte value, int startIndex)
    {
        value = buffer[startIndex];
        startIndex += 1;
        return startIndex;
    }

    protected static int PullItem(byte[] buffer, ref bool value, int startIndex)
    {
        value = buffer[startIndex] == 0 ? false : true;
        startIndex += 1;
        return startIndex;
    }

    protected static int PullItem(byte[] buffer, ref string value, int startIndex)
    {
        int stringBytes = 0;
        startIndex = PullItem(buffer, ref stringBytes, startIndex);
        if (stringBytes == 0)
            value = "";
        else 
            value = System.Text.Encoding.UTF8.GetString(buffer, startIndex, stringBytes);

        startIndex += stringBytes;
        return startIndex;
    }

    protected static int PushItem(byte[] buffer, int value, int startIndex)
    {
        var bytes = System.BitConverter.GetBytes(value);
        System.Array.Copy(bytes, 0, buffer, startIndex, bytes.Length);
        return startIndex + bytes.Length;
    }

    protected static int PushItem(byte[] buffer, byte value, int startIndex)
    {
        buffer[startIndex] = value;
        return startIndex + 1;
    }

    protected static int PushItem(byte[] buffer, bool value, int startIndex)
    {
        buffer[startIndex] = value?(byte)1:(byte)0;
        return startIndex + 1;
    }

    protected static int PushItem(byte[] buffer, string value, int startIndex)
    {
        if (string.IsNullOrEmpty(value))
        {
            startIndex = PushItem(buffer, (int)0, startIndex);
            return startIndex;
        }
        else
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);

            startIndex = PushItem(buffer, bytes.Length, startIndex);

            System.Array.Copy(bytes, 0, buffer, startIndex, bytes.Length);
            return startIndex + bytes.Length;
        }
    }

    public virtual int ToByteArray(byte[] buffer)
    {
        int index = 0;
        index = PushItem(buffer, appIdentifier, index);
        index = PushItem(buffer, messageType, index);
        index = PushItem(buffer, senderGuid, index);
        index = PushItem(buffer, myPort, index);


        return index;
    }

    public override string ToString()
    {
        string str = string.Format("appIdentifier: {0}, senderID: {1}, messageType: {2}", appIdentifier, senderGuid, messageType);

        

        return str;
    }
}

public class LobbyMessage : NetworkProtocol
{
    public string masterGuid;
    public int maxPlayerCount;
    public int currentPlayerCount;
    public bool iWantToJoin;
    public int joinStatus; // 0: broadcast, 1: successfully joined, 2: failed to join
    public bool requestRoomList;
    public bool startGame;

    public LobbyMessage()
        : base()
    {
        messageType = (byte)NetworkMessageType.LobbyMessage;
    }

    public LobbyMessage(byte[] udpMessage)
        : base(udpMessage)
    {
        messageType = (byte)NetworkMessageType.LobbyMessage;
    }

    public override int Parse(byte[] udpMessage)
    {
        int index = base.Parse(udpMessage);
        if (!_isValid)
            return index;

        //index = PullItem(udpMessage, ref guid, index);
        index = PullItem(udpMessage, ref masterGuid, index);
        index = PullItem(udpMessage, ref maxPlayerCount, index);
        index = PullItem(udpMessage, ref currentPlayerCount, index);
        index = PullItem(udpMessage, ref iWantToJoin, index);
        index = PullItem(udpMessage, ref joinStatus, index);
        index = PullItem(udpMessage, ref requestRoomList, index);
        index = PullItem(udpMessage, ref startGame, index);

        return index;
    }

    public override int ToByteArray(byte[] buffer)
    {
        int index = base.ToByteArray(buffer);
        //index = PushItem(buffer, guid, index);
        index = PushItem(buffer, masterGuid, index);
        index = PushItem(buffer, maxPlayerCount, index);
        index = PushItem(buffer, currentPlayerCount, index);
        index = PushItem(buffer, iWantToJoin, index);
        index = PushItem(buffer, joinStatus, index);
        index = PushItem(buffer, requestRoomList, index);
        index = PushItem(buffer, startGame, index);

        return index;
    }

    public override string ToString()
    {
        string str = base.ToString();

        str += string.Format(", room players: {0}/{1}, joined = {2}", currentPlayerCount, maxPlayerCount, joinStatus);

        return str;
    }
}
