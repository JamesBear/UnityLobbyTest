using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    class LobbyPlayer
    {
        public string name;
        public string guid;
        public bool isMaster;
        public IPEndPoint endPoint;
        public bool started;
        
    }

    enum LobbyState
    {
        NoLobby,
        InLobby,
        InGame,
    }

    class Lobby
    {
        public List<LobbyPlayer> players;
        public bool isMaster;

        // for clients
        public int maxCount;
        public int currentCount;
        public LobbyPlayer master;

        public Lobby()
        {
            players = new List<LobbyPlayer>();
        }
    }

    public const int APP_IDENTIFIER = 333323422;
    public const int MAX_MESSAGES = 100000;
    public const int DEFAULT_PORT = 13241;
    public const int DEBUG_PORT = 13242; // for debugging on single PC
    public const int MAX_PLAYER_COUNT = 8;

    public bool useDebugPort;
    public bool autoJoin;
    public bool btnTestSend;
    public bool btnTestStartListening;
    public bool btnTestCreateLobby;
    public bool btnTestJoinRandomLobby;
    public bool btnRequestRoomList;
    public bool btnTestStartGame;

    private bool stopListening = false;
    bool[] messageStates;
    string[] messages;
    int lastMessageIndex = -1;
    int lastMessageIndex2 = -1;
    Thread thread;
    UdpClient listener;
    string guidStr;

    Lobby lobby;
    LobbyState lobbyState;
    LobbyPlayer master;
    List<Lobby> lobbyList = new List<Lobby>();

    void Awake()
    {
        instance = this;
    }

    // Use this for initialization
    void Start()
    {
        messageStates = new bool[MAX_MESSAGES];
        messages = new string[MAX_MESSAGES];
        guidStr = System.Guid.NewGuid().ToString();
        lobbyState = LobbyState.NoLobby;

        for (int i = 0; i < MAX_MESSAGES; i++)
            messageStates[i] = false;
        
    }

    int GetPort()
    {
        if (useDebugPort)
            return DEBUG_PORT;
        else
            return DEFAULT_PORT;
    }

    // Update is called once per frame
    void Update()
    {
        if (btnTestSend)
        {
            btnTestSend = false;
            TestSend();
        }

        if (btnTestStartListening)
        {
            btnTestStartListening = false;
            if (listener == null)
            {
                thread = new Thread(() => StartListening());
                thread.Start();
            }
        }

        if (btnTestCreateLobby)
        {
            btnTestCreateLobby = false;
            CreateLobby();
        }

        if (btnTestJoinRandomLobby)
        {
            btnTestJoinRandomLobby = false;
            JoinRandomLobby();
        }

        if (btnRequestRoomList)
        {
            btnRequestRoomList = false;
            RequestRoomList();
        }

        if (btnTestStartGame)
        {
            btnTestStartGame = false;
            StartGame();
            NotifyStartGame();
        }

        for (int i = lastMessageIndex + 1; messageStates[i] == true; i++, lastMessageIndex++)
        {
            Debug.Log(messages[i]);
        }
    }

    void CheckNewMessages()
    {

    }

    void RequestRoomList()
    {
        LobbyMessage message = new LobbyMessage();
        FillBasicInfo(message);
        message.requestRoomList = true;
        BroadcastMessage(message);
    }

    void JoinRandomLobby()
    {
        if (lobbyList.Count == 0)
        {
            Debug.Log("no lobby");
            return;
        }
        var index = UnityEngine.Random.Range(0, lobbyList.Count);
        var targetLobby = lobbyList[index];
        RequestJoinLobby(targetLobby);
    }

    void FillBasicInfo(NetworkProtocol message)
    {
        message.appIdentifier = APP_IDENTIFIER;
        message.myPort = GetPort();
        message.senderGuid = guidStr;
    }

    void RequestJoinLobby(Lobby targetLobby)
    {
        LobbyMessage message = new LobbyMessage();
        FillBasicInfo(message);
        message.iWantToJoin = true;
        message.masterGuid = targetLobby.master.guid;
        SendUdpMessage(targetLobby.master.endPoint, message);
    }

    void CreateLobby()
    {
        lobby = new Lobby();
        lobby.players.Add(new LobbyPlayer { guid = guidStr, endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10003), isMaster = true, name = "" });
        lobby.isMaster = true;
        lobby.master = lobby.players[0];
        lobbyState = LobbyState.InLobby;
        BroadcastLobby();
    }

    void BroadcastLobby()
    {
        byte[] send_buffer = new byte[1000];
        LobbyMessage lobbyMessage = new LobbyMessage { appIdentifier = APP_IDENTIFIER, senderGuid = guidStr,
            messageType = (byte)NetworkMessageType.LobbyMessage, currentPlayerCount = lobby.players.Count, maxPlayerCount = MAX_PLAYER_COUNT,
            masterGuid = guidStr, myPort = GetPort()};
        int length = lobbyMessage.ToByteArray(send_buffer);
        BroadcastMessage(send_buffer, length);
    }

    void BroadcastMessage(NetworkProtocol message)
    {
        IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, DEFAULT_PORT);
        IPEndPoint broadcastEndPoint2 = new IPEndPoint(IPAddress.Broadcast, DEBUG_PORT);
        
        using (UdpClient udpClient = new UdpClient())
        {
            byte[] buffer = new byte[1000];
            int length = message.ToByteArray(buffer);
            udpClient.Send(buffer, length, broadcastEndPoint);
            udpClient.Send(buffer, length, broadcastEndPoint2);
        }
    }

    void BroadcastMessage(byte[] buffer, int length)
    {
        IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, DEFAULT_PORT);
        IPEndPoint broadcastEndPoint2 = new IPEndPoint(IPAddress.Broadcast, DEBUG_PORT);

        using (UdpClient sender = new UdpClient())
        {
            sender.Send(buffer, length, broadcastEndPoint);
            sender.Send(buffer, length, broadcastEndPoint2);
        }
    }

    void TestSend()
    {
        IPAddress ip = IPAddress.Parse("127.0.0.1");
        IPEndPoint endpoint = new IPEndPoint(ip, GetPort());
        IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, GetPort());
        //string text_to_send = "呵呵";
        //byte[] send_buffer = Encoding.UTF8.GetBytes(text_to_send);
        byte[] send_buffer = new byte[1000];
        LobbyMessage lobbyMessage = new LobbyMessage { appIdentifier = 1234, senderGuid = guidStr, messageType = 1, currentPlayerCount = 1, maxPlayerCount = 9 };
        int length = lobbyMessage.ToByteArray(send_buffer);
        UdpClient sender = new UdpClient();

        //Socket sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
        //ProtocolType.Udp);

        //sending_socket.SendTo(send_buffer, length, SocketFlags.Broadcast, broadcastEndPoint);
        sender.Send(send_buffer, length, broadcastEndPoint);
    }

    void OnReceive(System.IAsyncResult result)
    {

    }

    void StartListening()
    {
        int listenPort = GetPort();
        listener = new UdpClient(listenPort);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
        byte[] receive_byte_array;

        try
        {
            while (stopListening == false)
            {
                receive_byte_array = listener.Receive(ref groupEP);
                ThreadSafeLog(string.Format("Received a udp message from {0}", groupEP.ToString()));
                //received_data = Encoding.UTF8.GetString(receive_byte_array, 0, receive_byte_array.Length);
                OnNetworkMessage(receive_byte_array, groupEP);
            }
        }
        catch (System.Exception e)
        {
            ThreadSafeLog("network exception: " + e.ToString());
        }
        listener.Close();
    }

    void TestStartListening()
    {
        int listenPort = GetPort();
        listener = new UdpClient(listenPort);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
        string received_data;
        byte[] receive_byte_array;
        TcpClient tcpClient = new TcpClient();

        try
        {

            while (stopListening == false)
            {
                receive_byte_array = listener.Receive(ref groupEP);
                ThreadSafeLog(string.Format("Received a broadcast from {0}", groupEP.ToString()));
                //received_data = Encoding.UTF8.GetString(receive_byte_array, 0, receive_byte_array.Length);
                LobbyMessage lobbyMessage = new LobbyMessage(receive_byte_array);
                ThreadSafeLog(string.Format("data follows \n{0}\n\n", lobbyMessage.ToString()));
            }
        }
        catch (System.Exception e)
        {
            ThreadSafeLog("network exception: " + e.ToString());
        }
        listener.Close();
    }

    void OnNetworkMessage(byte[] udpMessage, IPEndPoint remoteEndPoint)
    {
        int _appIdentifier;
        byte _messageType;
        string _senderGuid;
        if (!NetworkProtocol.TryParseBasicInfo(udpMessage, out _appIdentifier, out _messageType))
            return;

        if (_appIdentifier != APP_IDENTIFIER)
            return;

        switch (_messageType)
        {
            case (int)NetworkMessageType.LobbyMessage:
                LobbyMessage lobbyMessage = new LobbyMessage();
                lobbyMessage.Parse(udpMessage);
                ProcessLobbyMessage(lobbyMessage, remoteEndPoint);
                break;
            default:
                ThreadSafeLog("unkown message type: " + _messageType);
                break;
        }
    }

    void SendUdpMessage(IPEndPoint endPoint, NetworkProtocol message)
    {
        using (UdpClient udpClient = new UdpClient())
        {
            byte[] buffer = new byte[1000];
            int length = message.ToByteArray(buffer);
            udpClient.Send(buffer, length, endPoint);
        }
    }

    void NotifyJoinSuccess(LobbyPlayer targetPlayer)
    {
        LobbyMessage lobbyMessage = new LobbyMessage
        {
            appIdentifier = APP_IDENTIFIER,
            senderGuid = guidStr,
            messageType = (byte)NetworkMessageType.LobbyMessage,
            currentPlayerCount = lobby.players.Count,
            maxPlayerCount = MAX_PLAYER_COUNT,
            masterGuid = guidStr,
            myPort = GetPort(),
            iWantToJoin = false,
            joinStatus = 1
        };
        SendUdpMessage(targetPlayer.endPoint, lobbyMessage);
    }

    void ProcessLobbyMessage(LobbyMessage lobbyMessage, IPEndPoint remoteEndPoint)
    {
        if (lobbyMessage.requestRoomList)
        {
            if (lobby != null && lobby.isMaster && lobbyState == LobbyState.InLobby)
                BroadcastLobby();
        }
        else if (lobbyMessage.startGame && lobbyState == LobbyState.InLobby)
        {
            if (lobbyMessage.senderGuid == lobby.master.guid)
            {
                StartGame();
                NotifyStartGame();
            }
        }
        else if (lobbyMessage.startGame && lobbyState == LobbyState.InGame && lobby.isMaster)
        {
            var targetPlayer = lobby.players.Find(item => item.guid == lobbyMessage.senderGuid);
            if (targetPlayer != null)
                targetPlayer.started = true;
            CheckReady();
        }
        else if (lobby != null && lobby.isMaster) // I am the lobby master
        {
            if (lobbyMessage.masterGuid != guidStr)
            {
                return;
            }

            if (lobbyMessage.iWantToJoin)
            {
                var targetPlayer = lobby.players.Find(item => item.guid == lobbyMessage.senderGuid);
                if (targetPlayer == null && lobby.players.Count < MAX_PLAYER_COUNT)
                {
                    targetPlayer = new LobbyPlayer { guid = lobbyMessage.senderGuid,
                        endPoint = new IPEndPoint(remoteEndPoint.Address, lobbyMessage.myPort), isMaster = false };
                    lobby.players.Add(targetPlayer);
                    NotifyJoinSuccess(targetPlayer);
                    ThreadSafeLog(string.Format("player on {0} joined.", targetPlayer.endPoint));
                }
            }

        }
        else if (lobbyState == LobbyState.NoLobby) // then I'm just a guest player
        {
            if (lobbyMessage.iWantToJoin == false)
            {
                var targetLobby = lobbyList.Find(item => item.master.guid == lobbyMessage.senderGuid);
                if (targetLobby == null)
                {

                    var newLobby = new Lobby
                    {
                        currentCount = lobbyMessage.currentPlayerCount,
                        maxCount = lobbyMessage.maxPlayerCount,
                        isMaster = false,
                        master = new LobbyPlayer { guid = lobbyMessage.senderGuid, endPoint = new IPEndPoint(remoteEndPoint.Address, lobbyMessage.myPort), isMaster = true }
                    };
                    lobbyList.Add(newLobby);
                    ThreadSafeLog("Found lobby on " + newLobby.master.endPoint.ToString());
                    if (autoJoin)
                    {
                        RequestJoinLobby(newLobby);
                    }
                }
                else if (targetLobby.master.guid == lobbyMessage.senderGuid)
                {
                    targetLobby.currentCount = lobbyMessage.currentPlayerCount;
                    if (lobbyMessage.joinStatus == 1)
                    {
                        lobby = targetLobby;
                        master = lobby.master;
                        lobbyState = LobbyState.InLobby;
                        ThreadSafeLog("Successfully joined lobby: " + lobby.master.endPoint.ToString());
                    }
                }
            }
        }
    }

    void NotifyStartGame()
    {
        LobbyMessage message = new LobbyMessage();
        FillBasicInfo(message);
        message.masterGuid = lobby.master.guid;
        message.startGame = true;
        if (lobby.isMaster)
            BroadcastMessage(message);
        else
            SendUdpMessage(lobby.master.endPoint, message);
    }

    void CheckReady()
    {
        bool allStarted = true;
        foreach (var player in lobby.players)
        {
            if (player.isMaster == false && player.started == false)
            {
                allStarted = false;
                break;
            }
        }

        if (allStarted)
        {
            ThreadSafeLog("all players ready to go");
        }
    }

    void StartGame()
    {
        ThreadSafeLog("Game started.");
        lobbyState = LobbyState.InGame;
    }

    void ThreadSafeLog(object message)
    {
        lastMessageIndex2++;
        messages[lastMessageIndex2] = "" + message;
        messageStates[lastMessageIndex2] = true;
    }

    void OnApplicationQuit()
    {
        stopListening = true;
        listener.Close();
        thread.Abort();
    }
}
