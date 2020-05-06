using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;

[RequireComponent(typeof(SocketIOComponent))]
public class ConnectionManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerIDGroup
    {
        public List<string> playerIDList = new List<string>();
    }

    [System.Serializable]
    public class RoomIDGroup
    {
        public List<string> roomIDList = new List<string>();
    }

    public class PlayerData
    {
        public string uid;
        public Player playerObj;
        public Vector3 correctPos;
    }

    [System.Serializable]
    public class PlayerUpdateData
    {
        public float x, y, z;
    }

    public enum ConnectionState
    {
        Disconnected,
        Connected,
        RoleCreate,
        RoleJoin,
        InRoom,
    }

    public ConnectionState connectionState;

    public Player playerObjPref;

    public string ownerID;

    public PlayerIDGroup playerIDGroup;

    public PlayerIDGroup cachePlayerIDGroup;

    public RoomIDGroup roomIDGroup;

    private List<PlayerData> characterList = new List<PlayerData>();

    private PlayerData playerDataOwner;

    private SocketIOComponent socket;

    public string roomName;
    public string playername;

    private bool isRoom;
    public string inputText = "Hello World";
    public string displayText = "";

    private void OnGUI()
    {
        switch(connectionState)
        {
            case ConnectionState.Disconnected: 
            {
                if (GUILayout.Button("Connect") && playername != null)
                {
                    socket.Connect();
                }
                   
                if(socket.IsConnected)
                {
                    connectionState = ConnectionState.Connected;
                }
            
                break;
            }

            case ConnectionState.Connected:
            {
                if(GUILayout.Button("CreateRoom"))
                {
                    connectionState = ConnectionState.RoleCreate;
                }

                if(GUILayout.Button("JoinRoom"))
                {
                    connectionState = ConnectionState.RoleJoin;
                    socket.Emit("OnClientFetchRoomList");
                }
                break;
            }

            case ConnectionState.RoleCreate:
            {
                roomName = GUILayout.TextField(roomName);
                if(GUILayout.Button("CreateRoom"))
                {
                    CreateRoom(roomName);
                }
                break;
            }

            case ConnectionState.RoleJoin:
            {
                foreach(var _roomName in roomIDGroup.roomIDList)
                {
                    if(GUILayout.Button(_roomName))
                    {
                        roomName = _roomName;
                        JoinRoom(_roomName);
                    }

                }
                if(GUILayout.Button("Back to create")){
                    connectionState = ConnectionState.Connected;
                }
                break;
            }

            case ConnectionState.InRoom:
            {
                GUILayout.TextField(ownerID);
                if(GUILayout.Button("LeaveRoom"))
                {
                    LeaveRoom();
                }
                 displayText = GUI.TextArea(new Rect(200, 10, 500, 100), displayText, 50);


                 inputText = GUI.TextField(new Rect(200, 120, 500, 20), inputText, 25);



                 if (GUI.Button(new Rect(200, 160, 50, 30), "Send"))
                {
                JSONObject jSONObject =new JSONObject(JSONObject.Type.OBJECT);

                jSONObject.AddField("message", inputText);

                jSONObject.AddField("uid" , ownerID);

                socket.Emit("message.send", jSONObject);

                Debug.Log("send");

                }
                break;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        socket = GetComponent<SocketIOComponent>();

        socket.On("OnOwnerClientConnect", OnOwnerClientConnect);
        socket.On("OnClientConnect", OnClientConnect);
        socket.On("OnClientFetchPlayerList", OnClientFetchPlayerList);
        socket.On("OnClientDisconnect", OnClientDisconnect);

        socket.On("OnClientCreateRoomSuccess", OnClientCreateRoomSuccess);
        socket.On("OnClientCreateRoomFail", OnClientCreateRoomFail);
        socket.On("OnOwnerClientJoinRoomSuccess", OnOwnerClientJoinRoomSuccess);
        socket.On("OnClientJoinRoomSuccess", OnClientJoinRoomSuccess);
        socket.On("OnClientJoinRoomFail", OnClientJoinRoomFail);

        socket.On("OnClientLeaveRoom", OnClientLeaveRoom);

        socket.On("OnClientFetchRoomList", OnClientFetchRoomList);

        socket.On("OnClientUpdateMoveList", OnClientUpdateMoveList);

        socket.On("message.sent", OnListerMessage);

        cachePlayerIDGroup = new PlayerIDGroup();
    }

    // Update is called once per frame
    void Update()
    {
        DetectPlayerConnect();
        UpdateAllCharacter();
    }

    void UpdateAllCharacter()
    {
        for(int i = 0; i < characterList.Count; i++)
        {
            if (characterList[i].uid == ownerID)
                continue;

            Vector3 currentPos = characterList[i].playerObj.transform.position;
            currentPos = Vector3.Lerp(currentPos, characterList[i].correctPos, 5.0f * Time.deltaTime);

            characterList[i].playerObj.transform.position = currentPos;
        }
    }

    IEnumerator UpdateOwnerPlayerData()
    {
        while(connectionState == ConnectionState.InRoom)
        {
            if(playerDataOwner != null && playerDataOwner.playerObj != null)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();

                Vector3 playerPos = playerDataOwner.playerObj.transform.position;
                data.Add("roomName", roomName);
                data.Add("uid", ownerID);
                data.Add("x", playerPos.x.ToString());
                data.Add("y", playerPos.y.ToString());
                data.Add("z", playerPos.z.ToString());


                JSONObject jsonObj = new JSONObject(data);

                socket.Emit("OnClientUpdateMove", jsonObj);

                yield return new WaitForSeconds(0.01f);
            }

            yield return null;
        }
    }

    private void DetectPlayerConnect()
    {
        if(cachePlayerIDGroup.playerIDList.Count != playerIDGroup.playerIDList.Count)
        {
            bool checkConnect;
            List<string> firstList;
            List<string> secondList;

            if(playerIDGroup.playerIDList.Count > cachePlayerIDGroup.playerIDList.Count)
            {
                firstList = playerIDGroup.playerIDList;
                secondList = cachePlayerIDGroup.playerIDList;
                checkConnect = true;
            }
            else
            {
                firstList = cachePlayerIDGroup.playerIDList;
                secondList = playerIDGroup.playerIDList;
                checkConnect = false;
            }

            foreach(var fID in firstList)
            {
                bool isFound = false;
                foreach(var sID in secondList)
                {
                    if(fID == sID)
                    {
                        isFound = true;
                        break;
                    }
                }

                if(!isFound)
                {
                    if(checkConnect)//Check player connect
                    {
                        //Debug.Log("Player connected : " + fID);
                        CreateCharacter(fID);
                    }
                    else//Check player disconnect
                    {
                        //Debug.Log("Player disconnected : " + fID);
                        DestroyCharacter(fID);
                    }
                }
            }
        }

        cachePlayerIDGroup.playerIDList = playerIDGroup.playerIDList;
    }

    private void CreateCharacter(string uid)
    {
        PlayerData newPlayerData = new PlayerData();

        newPlayerData.uid = uid;
        newPlayerData.playerObj = Instantiate(playerObjPref, Vector3.zero, Quaternion.identity);

        newPlayerData.playerObj.name = "Player : " + uid;

        newPlayerData.playerObj.GetComponentInChildren<Showname>().nametext = uid;

        if (uid == ownerID)
        {
            newPlayerData.playerObj.canControl = true;
            newPlayerData.playerObj.transform.GetChild(3).gameObject.SetActive(true);
            playerDataOwner = newPlayerData;
        }

        characterList.Add(newPlayerData);
    }

    private void DestroyCharacter(string uid)
    {
        for(int i = 0; i < characterList.Count; i++)
        {
            if(characterList[i].uid == uid)
            {
                Destroy(characterList[i].playerObj.gameObject);
                characterList.RemoveRange(i, 1);
                break;
            }
        }
    }

    public void CreateRoom(string newRoomName)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("roomName", newRoomName);
        JSONObject jsonObj = new JSONObject(data);
 
        socket.Emit("OnClientCreateRoom", jsonObj);
    }

    public void JoinRoom(string newRoomName)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("roomName", newRoomName);
        JSONObject jsonObj = new JSONObject(data);

        socket.Emit("OnClientJoinRoom", jsonObj);
    }

    public void LeaveRoom()
    {
        connectionState = ConnectionState.Connected;
        roomName = "";
        socket.Emit("OnClientLeaveRoom");
    }

    private void FetchPlayerList()
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("roomName", roomName);
        JSONObject jsonObj = new JSONObject(data);
        socket.Emit("OnClientFetchPlayerList", jsonObj);
    }

    #region Callback Group
    void OnClientConnect(SocketIOEvent evt)
    {
        Debug.Log("OnClientConnect : "+ evt.data.ToString());
        //socket.Emit("OnClientFetchPlayerList");
    }

    void OnClientDisconnect(SocketIOEvent evt)
    {
        Debug.Log("OnClientDisconnect : " + evt.data.ToString());
        //socket.Emit("OnClientFetchPlayerList");
    }

    void OnOwnerClientConnect(SocketIOEvent evt)
    {
        Debug.Log("OnOwnerClientConnect : " + evt.data.ToString());
    }

    void OnClientFetchPlayerList(SocketIOEvent evt)
    {
        Debug.Log("OnClientFetchPlayerList : "+ evt.data.ToString());

        playerIDGroup = JsonUtility.FromJson <PlayerIDGroup> (evt.data.ToString());
    }

    //======================== Room ===========================
    void OnClientCreateRoomSuccess(SocketIOEvent evt)
    {
        Debug.Log("OnClientCreateRoomSuccess : " + evt.data.ToString());

        connectionState = ConnectionState.InRoom;

        var dictData = evt.data.ToDictionary();

        ownerID = dictData["uid"];

        StartCoroutine(UpdateOwnerPlayerData());

        FetchPlayerList();
    }

    void OnClientCreateRoomFail(SocketIOEvent evt)
    {
        Debug.Log("OnClientCreateRoomFail : " + evt.data.ToString());
    }

    void OnOwnerClientJoinRoomSuccess(SocketIOEvent evt)
    {
        Debug.Log("OnOwnerClientJoinRoomSuccess : " + evt.data.ToString());

        connectionState = ConnectionState.InRoom;

        var dictData = evt.data.ToDictionary();
        
        ownerID = dictData["uid"];

        StartCoroutine(UpdateOwnerPlayerData());

        FetchPlayerList();
    }

    void OnClientJoinRoomSuccess(SocketIOEvent evt)
    {
        Debug.Log("OnClientJoinRoomSuccess : " + evt.data.ToString());

        FetchPlayerList();
    }

    void OnClientJoinRoomFail(SocketIOEvent evt)
    {
        Debug.Log("OnClientJoinRoomFail : " + evt.data.ToString());
    }

    void OnClientLeaveRoom(SocketIOEvent evt)
    {
        Debug.Log("OnClientLeaveRoom : " + evt.data.ToString());
       
        FetchPlayerList();
    }

    void OnClientFetchRoomList(SocketIOEvent evt)
    {
        Debug.Log("OnClientFetchRoomList : " + evt.data.ToString());

        roomIDGroup = JsonUtility.FromJson<RoomIDGroup>(evt.data.ToString());
    }

    void OnClientUpdateMoveList(SocketIOEvent evt)
    {
        var dataDict = evt.data.ToDictionary();

        Debug.Log(evt.data.ToString());

        for(int i = 0; i < characterList.Count; i++)
        {
            var newPlayerUpdateData = JsonUtility.FromJson<PlayerUpdateData>(dataDict[characterList[i].uid]);
            Vector3 newPos = new Vector3(newPlayerUpdateData.x, newPlayerUpdateData.y, newPlayerUpdateData.z);

            if(characterList[i].playerObj.transform.position == Vector3.zero)
            {
                characterList[i].playerObj.transform.position = newPos;
            }

            characterList[i].correctPos = newPos;

            
        }
    }

    void OnListerMessage(SocketIOEvent obj)
    {
        string msg = obj.data["uid"].str + " : " + obj.data["message"].str;

        displayText += msg+System.Environment.NewLine;
    }
    #endregion
}
