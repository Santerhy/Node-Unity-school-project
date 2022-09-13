using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firesplash.UnityAssets.SocketIO;
using SimpleJSON;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ServerControl : MonoBehaviour
{
    public SocketIOCommunicator sioCom;
    public List<GameObject> players;
    public GameObject player;

    [SerializeField]
    private GameObject myPlayer;
    [SerializeField]
    private PlayerController myPlayerController;

    public HexGrindLayout hgl;

    public Transform currentLocationTr;
    public GameObject currentLocationOb;
    public GameObject lastLocationOb;
    public List<GameObject> movableTiles;

    public List<Material> playerColors;
    private Material myMaterial;
    public Button moveButton;

    bool myTurn = false;
    bool moving = false;

    int claimNumber;

    // Start is called before the first frame update
    void Start()
    {
        sioCom.Instance.On("connect", (payload) =>
        {
            Debug.Log("Connected, id: " + sioCom.Instance.SocketID);
            sioCom.Instance.Emit("CREATEPLAYER");
        });

        sioCom.Instance.On("INSTANCEPLAYER", (playerInfo) =>
        {
            JSONNode node = JSON.Parse(playerInfo);

            GameObject go = Instantiate(player, hgl.tiles[node["spawnIndex"]].transform.position, Quaternion.identity);
            PlayerController pc = go.GetComponent<PlayerController>();
            pc.claimNumber = node["team"];
            go.name = node["socketId"];
            pc.mySocketId = node["socketId"];
            pc.currentLocationOb = hgl.tiles[node["spawnIndex"]].gameObject;
            pc.currentLocationTr = pc.currentLocationOb.transform;
            go.transform.position = pc.currentLocationTr.position;
            pc.myMaterial = playerColors[pc.claimNumber];
            go.GetComponent<Renderer>().material = pc.myMaterial;
            players.Add(go);

            if (myPlayer == null)
            {
                myPlayer = go;
                myPlayerController = pc;
            }

            ClaimOneTile();
        });

        sioCom.Instance.On("INSTANCEOTHERS", (playerInfo) =>
        {
            JSONNode node = JSON.Parse(playerInfo);

            GameObject go = Instantiate(player, hgl.tiles[node["spawnIndex"]].transform.position, Quaternion.identity);
            PlayerController pc = go.GetComponent<PlayerController>();
            pc.claimNumber = node["team"];
            go.name = node["socketId"];
            pc.mySocketId = node["socketId"];
            pc.currentLocationOb = hgl.tiles[node["spawnIndex"]].gameObject;
            pc.currentLocationTr = pc.currentLocationOb.transform;
            go.transform.position = pc.currentLocationTr.position;
            pc.myMaterial = playerColors[pc.claimNumber];
            go.GetComponent<Renderer>().material = pc.myMaterial;
            players.Add(go);
        });

        sioCom.Instance.On("STARTTURN", (data) =>
        {
            myTurn = true;
            moveButton.gameObject.SetActive(true);
        });

        sioCom.Instance.On("ENDTURN", (data) =>
        {
            myTurn = false;
            moveButton.gameObject.SetActive(false);

            sioCom.Instance.Emit("TURNENDED");
        });

        sioCom.Instance.On("MOVEPLAYER", (playerInfo) =>
        {
            JSONNode node = JSONNode.Parse(playerInfo);
            Debug.Log("player location: " + node["location"]);
            if (node["name"] != myPlayerController.mySocketId)
            {
                foreach (GameObject pl in players)
                {
                    Debug.Log("playername: " + pl.GetComponent<PlayerController>().mySocketId + ", node: " + node["name"]);
                    if (pl.GetComponent<PlayerController>().mySocketId == node["name"])
                    {
                        Debug.Log("Player found");
                        pl.transform.position = hgl.tiles[node["location"]].transform.position;
                    }
                }
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (myTurn)
        {
            if (moving)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        RaycastHit hit;
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                        {
                            if (hit.collider.gameObject.CompareTag("Tile"))
                            {
                                if (movableTiles.Contains(hit.collider.gameObject))
                                {
                                    myPlayerController.currentLocationOb = hit.collider.gameObject;
                                    //Conquest new tile
                                    Move();
                                    //ClaimOneTile();
                                }
                            }
                        }
                    }
                    {

                    }
                }
            }
        }
    }

    public void ShowMovableTile()
    {
        Collider[] cl = myPlayer.GetComponent<PlayerController>().currentLocationOb.GetComponent<HexRenderer>().CheckNearbyTiles();

        foreach (Collider c in cl)
        {
            if (c.gameObject.CompareTag("Tile"))
            {
                movableTiles.Add(c.gameObject);
                c.gameObject.GetComponent<HexRenderer>().IsMovable();
            }
        }
        moving = true;

    }

    private void Move()
    {
        moving = false;
        myPlayer.transform.position = myPlayerController.currentLocationOb.transform.position;
        ClearMovableTiles();

        JSONObject moveData = new JSONObject();
        moveData.Add("name", myPlayerController.mySocketId);
        moveData.Add("location", hgl.tiles.IndexOf(myPlayerController.currentLocationOb));
        string plData = moveData.ToString();
        sioCom.Instance.Emit("MOVE", plData, false);


        sioCom.Instance.Emit("PLAYERENDTURN", "testidata", true);
    }
    private void ClearMovableTiles()
    {
        foreach (GameObject go in movableTiles)
        {
            if (go != myPlayerController.currentLocationOb)
                go.GetComponent<HexRenderer>().NotMovable();
            else
                ClaimOneTile();
        }
        movableTiles.Clear();
    }

    private void ClaimOneTile()
    {
        myPlayerController.currentLocationOb.GetComponent<HexRenderer>().Claim(myPlayerController.claimNumber, myPlayerController.myMaterial);
    }

}
