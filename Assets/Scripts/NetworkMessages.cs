using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkMessages
{
    public enum Commands
    {
        PLAYER_CONNECT,
        HANDSAKE,
        PLAYER_UPDATE,
        PLAYER_INPUT,
        SERVER_UPDATE,
        DISCONNECT,
    }

    [System.Serializable]
    public class NetworkHeader
    {
        public Commands cmd;
    }

    [System.Serializable]
    public class PlayerUpdateMsg : NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> players;
        public PlayerUpdateMsg()
        {
            cmd = Commands.PLAYER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    };

    [System.Serializable]
    public class PlayerInputMsg : NetworkHeader
    {
        public Vector3 position;

        public PlayerInputMsg()
        {
            cmd = Commands.PLAYER_INPUT;
        }
    }

    [System.Serializable]
    public class PlayerConnectMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer newPlayer;
        public PlayerConnectMsg()
        {
            cmd = Commands.PLAYER_CONNECT;
            newPlayer = new NetworkObjects.NetworkPlayer();
        }
    }

    [System.Serializable]
    public class ServerUpdateMsg : NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> players;
        public ServerUpdateMsg()
        {
            cmd = Commands.SERVER_UPDATE;
            players = new List<NetworkObjects.NetworkPlayer>();
        }
    }

    [System.Serializable]
    public class HandshakeMsg : NetworkHeader
    {
        public NetworkObjects.NetworkPlayer player;

        public HandshakeMsg()
        {
            cmd = Commands.HANDSAKE;
            player = new NetworkObjects.NetworkPlayer();
        }
    }

    [System.Serializable]
    public class PlayerDisconnect : NetworkHeader
    {
        public List<NetworkObjects.NetworkPlayer> droppedPlayers;
        public PlayerDisconnect()
        {
            cmd = Commands.DISCONNECT;
            droppedPlayers = new List<NetworkObjects.NetworkPlayer>();
        }
        public PlayerDisconnect(List<NetworkObjects.NetworkPlayer> playerList)
        {      // Constructor
            cmd = Commands.DISCONNECT;
            droppedPlayers = playerList;
        }
    }

}

namespace NetworkObjects
{
    [System.Serializable]
    public class NetworkObject
    {
        public string id;
    }
    [System.Serializable]
    public class NetworkPlayer : NetworkObject
    {
        public Color cubeColor;
        public Vector3 cubePos;

        public NetworkPlayer()
        {
            cubeColor = new Color();
        }
    }
}
