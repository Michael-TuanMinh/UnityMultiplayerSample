using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using System.Text;
using System.Collections.Generic;

public class NetworkServer : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public ushort serverPort;
    private NativeList<NetworkConnection> m_Connections;
    private ServerUpdateMsg serverMessage;
    private List<float> timer = new List<float>();

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = serverPort;
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port " + serverPort);
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

        serverMessage = new ServerUpdateMsg();

        InvokeRepeating("SendPosition", 1, 0.03f);
    }

    void SendToClient(string message, NetworkConnection c)
    {
        if (!c.IsCreated)
        {
            Debug.LogError("Connection not created");
            return;
        }
        var writer = m_Driver.BeginSend(NetworkPipeline.Null, c);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

   

    void OnConnect(NetworkConnection c)
    {
        HandshakeMsg m = new HandshakeMsg();
        m.player.id = c.InternalId.ToString();
        m.player.cubeColor = new Color(Random.Range(0, 1.0f), Random.Range(0, 1.0f), Random.Range(0, 1.0f));
        SendToClient(JsonUtility.ToJson(m), c);

        NetworkObjects.NetworkPlayer newPlayer = new NetworkObjects.NetworkPlayer();
        newPlayer.id = c.InternalId.ToString();
        newPlayer.cubeColor = m.player.cubeColor;
        serverMessage.players.Add(newPlayer);
        timer.Add(0.0f);

        for (int i = 0; i < m_Connections.Length; i++)
        {
            PlayerConnectMsg p = new PlayerConnectMsg();
            p.newPlayer.id = c.InternalId.ToString();
            p.newPlayer.cubeColor = m.player.cubeColor;
            SendToClient(JsonUtility.ToJson(p), m_Connections[i]);
        }

        SendToClient(JsonUtility.ToJson(serverMessage), c);
        m_Connections.Add(c);
        Debug.Log("Accepted a connection");
    }

    void OnData(DataStreamReader stream, int i)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        int index = FindPlayerById(m_Connections[i].InternalId.ToString());

        switch (header.cmd)
        {
            case Commands.PLAYER_INPUT:
                PlayerInputMsg input = JsonUtility.FromJson<PlayerInputMsg>(recMsg);
                serverMessage.players[index].cubePos = input.position;
                timer[index] = Time.deltaTime;
                break;

            default:
                Debug.Log("SERVER ERROR: Unrecognized message received!");
                break;
        }
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    void DropClients()
    {
        PlayerDisconnect list = new PlayerDisconnect();
        for (int i = 0; i < serverMessage.players.Count; i++)
        {
            timer[i] += Time.deltaTime;
            if (timer[i] >= 5.0f)
            {
                int index = FindConnectionById(serverMessage.players[i].id);
                if (index >= 0)
                {
                    m_Connections[index] = default(NetworkConnection);
                }
                    

                list.droppedPlayers.Add(serverMessage.players[i]);
                serverMessage.players.RemoveAt(i);
                //timer[i] = 0;
                i--;

                Debug.Log("Unrespone for more than 5 seconds");
            }
        }

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        if (list.droppedPlayers.Count > 0)
        {
            for (int i = 0; i < m_Connections.Length; i++)
            {
                SendToClient(JsonUtility.ToJson(list), m_Connections[i]);
            }

            Debug.Log("Player Disconnected");
        }
    }

    int FindConnectionById(string id)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (m_Connections[i].InternalId.ToString() == id)
                return i;
        }
        return -1;
    }

    int FindPlayerById(string id)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (serverMessage.players[i].id == id)
                return i;
        }
        return -1;
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        // AcceptNewConnections
        NetworkConnection c = m_Driver.Accept();
        while (c != default(NetworkConnection))
        {
            OnConnect(c);

            // Check if there is another new connection
            c = m_Driver.Accept();
        }

        DropClients();

        // Read Incoming Messages
        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);

            NetworkEvent.Type cmd;
            cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            while (cmd != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    OnData(stream, i);
                }
                cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream);
            }
        }
    }

    void SendPosition()
    {
        PlayerUpdateMsg m = new PlayerUpdateMsg();
        foreach (NetworkObjects.NetworkPlayer p in serverMessage.players)
        {
            NetworkObjects.NetworkPlayer temp = new NetworkObjects.NetworkPlayer();
            temp.id = p.id;
            temp.cubePos = p.cubePos;
            m.players.Add(temp);
        }
        foreach (NetworkConnection c in m_Connections)
        {
            SendToClient(JsonUtility.ToJson(m), c);
        }
    }
}
