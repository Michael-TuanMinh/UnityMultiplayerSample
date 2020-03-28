using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkMessages;
using NetworkObjects;
using System;
using System.Text;
using System.Collections.Generic;

public class NetworkClient : MonoBehaviour
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public string serverIP;
    public ushort serverPort;

    [SerializeField] GameObject playerObject;

    private string myID;
    private Dictionary<string, GameObject> myList = new Dictionary<string, GameObject>();

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        var endpoint = NetworkEndPoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
    }

    void SendToServer(string message)
    {
        var writer = m_Driver.BeginSend(m_Connection);
        NativeArray<byte> bytes = new NativeArray<byte>(Encoding.ASCII.GetBytes(message), Allocator.Temp);
        writer.WriteBytes(bytes);
        m_Driver.EndSend(writer);
    }

    void OnConnect()
    {
        Debug.Log("We are now connected to the server");
    }

    void OnData(DataStreamReader stream)
    {
        NativeArray<byte> bytes = new NativeArray<byte>(stream.Length, Allocator.Temp);
        stream.ReadBytes(bytes);
        string recMsg = Encoding.ASCII.GetString(bytes.ToArray());
        NetworkHeader header = JsonUtility.FromJson<NetworkHeader>(recMsg);

        switch (header.cmd)
        {
            case Commands.HANDSHAKE:
                HandshakeMsg hsMsg = JsonUtility.FromJson<HandshakeMsg>(recMsg);
                myID = hsMsg.player.id;
                break;

            case Commands.PLAYER_CONNECT:
                PlayerConnectMsg pcMsg = JsonUtility.FromJson<PlayerConnectMsg>(recMsg);
                SpawnPlayers(pcMsg.newPlayer);
                break;
            case Commands.PLAYER_UPDATE:
                PlayerUpdateMsg puMsg = JsonUtility.FromJson<PlayerUpdateMsg>(recMsg);
                UpdatePlayers(puMsg.players);
                break;
            
            case Commands.DISCONNECT:
                PlayerDisconnect dropMsg = JsonUtility.FromJson<PlayerDisconnect>(recMsg);
                foreach (NetworkObjects.NetworkPlayer p in dropMsg.droppedPlayers)
                {
                   DestroyPlayers(p.id);
                }
                break;
            case Commands.SERVER_UPDATE:
                ServerUpdateMsg suMsg = JsonUtility.FromJson<ServerUpdateMsg>(recMsg);
                foreach (var it in suMsg.players)
                {
                    SpawnPlayers(it);
                }
                break;
            default:
                Debug.Log("Unrecognized message received!");
                break;
        }
    }

    void Disconnect()
    {
        m_Connection.Disconnect(m_Driver);
        m_Connection = default(NetworkConnection);
    }

    public void OnDestroy()
    {
        m_Driver.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        cmd = m_Connection.PopEvent(m_Driver, out stream);
        while (cmd != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                OnConnect();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                OnData(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Disconnect();
            }

            cmd = m_Connection.PopEvent(m_Driver, out stream);
        }
    }

    void SpawnPlayers(NetworkObjects.NetworkPlayer p)
    {
        if (myList.ContainsKey(p.id)) return;

        GameObject temp = Instantiate(playerObject, Vector3.zero, Quaternion.identity);
        temp.GetComponent<MeshRenderer>().material.color = p.cubeColor;

        if (p.id == myID) temp.AddComponent<PlayerController>().client = this;
      
        myList.Add(p.id, temp);
    }

    void DestroyPlayers(string id)
    {
        if (myList.ContainsKey(id))
        {
            GameObject temp = myList[id];
            myList.Remove(id);
            Destroy(temp);
        }
    }

    void UpdatePlayers(List<NetworkObjects.NetworkPlayer> players)
    {
        foreach (NetworkObjects.NetworkPlayer p in players)
        {
            if (myList.ContainsKey(p.id))
            {
                myList[p.id].transform.position = p.cubePos;
            }
        }

    }

    public void SendPosition(Vector3 pos)
    {
        PlayerInputMsg m = new PlayerInputMsg();
        m.position = pos;
        SendToServer(JsonUtility.ToJson(m));
    }
}
