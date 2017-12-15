using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetworkProtocol
{
    public bool isValid
    {
        get { return _isValid; }
    }
    protected bool _isValid;

    public int appIdentifier;
    public int roomIdentifier;
    public byte messageType;


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

        if (udpMessage.Length < 9)
        {
            _isValid = false;
            return startIndex;
        }

        startIndex = PullItem(udpMessage, ref appIdentifier, startIndex);
        startIndex = PullItem(udpMessage, ref roomIdentifier, startIndex);
        startIndex = PullItem(udpMessage, ref messageType, startIndex);

        return startIndex;
    }

    protected int PullItem(byte[] buffer, ref int value, int startIndex)
    {
        value = System.BitConverter.ToInt32(buffer, startIndex);
        startIndex += sizeof(int);
        return startIndex;
    }

    protected int PullItem(byte[] buffer, ref byte value, int startIndex)
    {
        value = buffer[startIndex];
        startIndex += 1;
        return startIndex;
    }

    protected int PullItem(byte[] buffer, ref string value, int startIndex)
    {
        int stringBytes = 0;
        startIndex = PullItem(buffer, ref stringBytes, startIndex);
        value = System.Text.Encoding.UTF8.GetString(buffer, startIndex, stringBytes);

        startIndex += stringBytes;
        return startIndex;
    }

    protected int PushItem(byte[] buffer, int value, int startIndex)
    {
        var bytes = System.BitConverter.GetBytes(value);
        System.Array.Copy(bytes, 0, buffer, startIndex, bytes.Length);
        return startIndex + bytes.Length;
    }

    protected int PushItem(byte[] buffer, byte value, int startIndex)
    {
        buffer[startIndex] = value;
        return startIndex + 1;
    }

    protected int PushItem(byte[] buffer, string value, int startIndex)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);

        startIndex = PushItem(buffer, bytes.Length, startIndex);

        System.Array.Copy(bytes, 0, buffer, startIndex, bytes.Length);
        return startIndex + bytes.Length;
    }

    public virtual int ToByteArray(byte[] buffer)
    {
        int index = 0;
        index = PushItem(buffer, appIdentifier, index);
        index = PushItem(buffer, roomIdentifier, index);
        index = PushItem(buffer, messageType, index);


        return index;
    }

    public override string ToString()
    {
        string str = string.Format("appIdentifier: {0}, roomIdentifier: {1}, messageType: {2}", appIdentifier, roomIdentifier, messageType);

        

        return str;
    }
}

public class LobbyMessage : NetworkProtocol
{
    public string guid;

    public LobbyMessage()
        : base()
    {

    }

    public LobbyMessage(byte[] udpMessage)
        : base(udpMessage)
    {

    }

    public override int Parse(byte[] udpMessage)
    {
        int index = base.Parse(udpMessage);
        if (!_isValid)
            return index;

        index = PullItem(udpMessage, ref guid, index);

        return index;
    }

    public override int ToByteArray(byte[] buffer)
    {
        int index = base.ToByteArray(buffer);
        index = PushItem(buffer, guid, index);

        return index;
    }

    public override string ToString()
    {
        string str = base.ToString();

        str += string.Format(", guid: {0}", guid);

        return str;
    }
}