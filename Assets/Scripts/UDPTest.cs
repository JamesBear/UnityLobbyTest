using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPTest : MonoBehaviour {

    public const int MAX_MESSAGES = 100000;

    public bool btnTestSend;
    public bool btnTestStartListening;

    private bool stopListening = false;
    bool[] messageStates;
    string[] messages;
    int lastMessageIndex = -1;
    int lastMessageIndex2 = -1;
    Thread thread;
    UdpClient listener;

    // Use this for initialization
    void Start() {
        messageStates = new bool[MAX_MESSAGES];
        messages = new string[MAX_MESSAGES];

        for (int i = 0; i < MAX_MESSAGES; i++)
            messageStates[i] = false;
    }

    // Update is called once per frame
    void Update() {
        if (btnTestSend)
        {
            btnTestSend = false;
            TestSend();
        }

        if (btnTestStartListening)
        {
            btnTestStartListening = false;
            thread = new Thread(() => TestStartListening());
            thread.Start();
        }

        for (int i = lastMessageIndex+1; messageStates[i] == true; i ++, lastMessageIndex++)
        {
            Debug.Log(messages[i]);
        }
    }

    void CheckNewMessages()
    {
    }

    void TestSend()
    {
        IPAddress ip = IPAddress.Parse("127.0.0.1");
        IPEndPoint endpoint = new IPEndPoint(ip, 10003);
        string text_to_send = "呵呵";
        byte[] send_buffer = Encoding.UTF8.GetBytes(text_to_send);

        Socket sending_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
        ProtocolType.Udp);

        sending_socket.SendTo(send_buffer, send_buffer.Length, SocketFlags.None, endpoint);
    }

    void OnReceive(System.IAsyncResult result)
    {
        
    }

    void TestStartListening()
    {
        int listenPort = 10003;
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
                received_data = Encoding.UTF8.GetString(receive_byte_array, 0, receive_byte_array.Length);
                ThreadSafeLog(string.Format("data follows \n{0}\n\n", received_data));
            }
        }
        catch (System.Exception e)
        {
            ThreadSafeLog("network exception: " + e.ToString());
        }
        listener.Close();
    }

    void ThreadSafeLog(object message)
    {
        lastMessageIndex2++;
        messages[lastMessageIndex2] = ""+message;
        messageStates[lastMessageIndex2] = true;
    }

    void OnApplicationQuit()
    {
        stopListening = true;
        listener.Close();
        thread.Abort();
    }
}
