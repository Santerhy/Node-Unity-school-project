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
    public List<Material> claimMaterials;
    public Button moveButton;

    public Button superClaimButton;
    public Text superClaimText;
    private int superClaimCounter = 2;

    public Text turnText;

    public Text roundsText;

    public GameObject endPanel;
    public List<Text> endTexts;

    public List<Text> scoresList;

    bool myTurn = false;
    bool moving = false;

    public List<int> tilesClaimStatus;

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
            pc.claimMaterial = claimMaterials[pc.claimNumber];
            go.GetComponent<Renderer>().material = pc.myMaterial;
            players.Add(go);
            ActivatePlayerScores();

            if (myPlayer == null)
            {
                myPlayer = go;
                myPlayerController = pc;
            }

            roundsText.text = "Rounds left: " + node["round"].ToString();

            //hgl.tiles[node["spawnIndex"]].GetComponent<HexRenderer>().Claim(pc.claimNumber, pc.claimMaterial);

            ClaimOneTile();
            UpdateTilesToList();
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
            pc.claimMaterial = claimMaterials[pc.claimNumber];
            go.GetComponent<Renderer>().material = pc.myMaterial;
            players.Add(go);


            hgl.tiles[node["spawnIndex"]].GetComponent<HexRenderer>().Claim(pc.claimNumber, pc.claimMaterial);
        });

        sioCom.Instance.On("STARTTURN", (data) =>
        {
            //int rounds = int.Parse(data);
            myTurn = true;
            moveButton.gameObject.SetActive(true);
            superClaimButton.gameObject.SetActive(true);

            //roundsText.text = "Rounds left: " + rounds.ToString();
        });

        sioCom.Instance.On("ENDTURN", (data) =>
        {
            myTurn = false;
            moveButton.gameObject.SetActive(false);
            superClaimButton.gameObject.SetActive(false);

            sioCom.Instance.Emit("TURNENDED");
        });

        sioCom.Instance.On("CHANGETURNTEXT", (data) =>
        {
            int turn = int.Parse(data);
            
            ChangeTurnText(turn);
        });

        sioCom.Instance.On("GAMEOVER", (scores) =>
        {
            myTurn = false;
            List<int> s = new List<int>();
            JSONNode node = JSON.Parse(scores);

            for (int i = 0; i < 4; i++)
            {
                s.Add(node[i]);
                Debug.Log("Scores beg " + node[i]);
            }

            GameOver(s);
        });

        sioCom.Instance.On("UPDATESCORES", (data) =>
        {
            JSONNode node = JSON.Parse(data);
            List<int> scores = new List<int>();
            /*
            for (int i = 0; i < 4; i++)
            {
                scores.Add(node[i]);
                Debug.Log("player " + i.ToString() + " scores: " + node[i]);
            }
            */

            scores.Add(node[0]);
            scores.Add(node[1]);
            scores.Add(node[2]);
            scores.Add(node[3]);

            UpdateScores(scores);
        });

        sioCom.Instance.On("MOVEPLAYER", (playerInfo) =>
        {
            JSONNode node = JSONNode.Parse(playerInfo);
            if (node["name"] != myPlayerController.mySocketId)
            {
                foreach (GameObject pl in players)
                {
                    if (pl.GetComponent<PlayerController>().mySocketId == node["name"])
                    {
                        Debug.Log("Player found");
                        pl.transform.position = hgl.tiles[node["location"]].transform.position;
                        pl.GetComponent<PlayerController>().currentLocationOb = hgl.tiles[node["location"]];
                        pl.GetComponent<PlayerController>().currentLocationTr = hgl.tiles[node["location"]].transform;
                    }
                }
            }
        });

        sioCom.Instance.On("INSTANTIATEFIELD", (data) =>
        {
            UpdateTilesToList();
        });

        sioCom.Instance.On("UPDATETILESFROMSERVER", (tileData) =>
        {
            JSONArray node = (JSONArray)JSON.Parse(tileData);
            for (int i = 0; i < tilesClaimStatus.Count; i++)
            {
                tilesClaimStatus[i] = node[i];
            }
            UpdateListToTiles();
        });

        sioCom.Instance.On("UPDATEROUNDS", (data) =>
        {
            int round = int.Parse(data);
            roundsText.text = "Rounds left: " + round.ToString();
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
                                    if (CheckTileOccupation(hit.collider.gameObject))
                                    {
                                        myPlayerController.currentLocationOb = hit.collider.gameObject;
                                        Move();
                                    }
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

    private bool CheckTileOccupation(GameObject col)
    {
        for (int i = 0; i < players.Count - 1; i++)
        {
            if (col == players[i].GetComponent<PlayerController>().currentLocationOb)
                return false;
        }

        return true;
    }

    private void GameOver(List<int> scores)
    {
        int[,] scoresArray = new int[players.Count, 2];
        for (int i = 0; i < players.Count; i++) {
            scoresArray[i, 0] = i;
            scoresArray[i, 1] = scores[i];
            endTexts[i].gameObject.SetActive(true);
            Debug.Log("scores " + scoresArray[i,0]);
        }

        scoresArray = SortArray(scoresArray);

        Debug.Log(scoresArray);

        endPanel.gameObject.SetActive(true);

        int c;
        for (int i = 0; i < players.Count; i++)
        {
            c = (players.Count -1 ) - i;
            endTexts[i].text += "Player " + (scoresArray[i, 0] + 1).ToString() + ", Scores: " + scoresArray[i, 1].ToString();
            switch (scoresArray[i,0])
            {
                case 0:
                    endTexts[i].color = Color.red;
                    break;
                case 1:
                    endTexts[i].color = Color.blue;
                    break;
                case 2:
                    endTexts[i].color = Color.yellow;
                    break;
                case 3:
                    endTexts[i].color = Color.green;
                    break;
            }
        }
    }

    private int[,] SortArray(int[,] array)
    {
        for (int j = 0; j < array.GetLength(0) -1; j++)
        {
            for (int i = 0; i < array.GetLength(0) - 1; i++)
            {
                if (array[i,1] < array[i+1, 1])
                {
                    int tempIndex = array[i, 0];
                    int tempValue = array[i, 1];
                    array[i, 0] = array[i + 1, 0];
                    array[i, 1] = array[i + 1, 1];
                    array[i + 1, 0] = tempIndex;
                    array[i + 1, 1] = tempValue;
                }
            }
        }
        return array;
        
    }

    private void UpdateTilesToList()
    {
        for (int i = 0; i < hgl.tiles.Count; i++)
        {
            tilesClaimStatus[i] = hgl.tiles[i].gameObject.GetComponent<HexRenderer>().claimedBy;
        }

        JSONArray tileData = new JSONArray();
        for (int i = 0; i < tilesClaimStatus.Count; i++)
        {
            tileData[i] = tilesClaimStatus[i];
            if (i > 145)
                Debug.Log("i = " + i.ToString() + " claimde by " + tilesClaimStatus[i]);
        }
        string data = tileData.ToString();
        sioCom.Instance.Emit("UPDATEFIELDTOLIST", data, true);
    }

    private void UpdateListToTiles()
    {
        for (int i = 0; i < tilesClaimStatus.Count; i++)
        {
            HexRenderer hr = hgl.tiles[i].GetComponent<HexRenderer>();

            if (tilesClaimStatus[i] != -1)
            {
                hr.SetMaterial(claimMaterials[tilesClaimStatus[i]]);
                hr.claimedBy = tilesClaimStatus[i];
            }
        }
    }

    public void SuperClaim()
    {
        if (superClaimCounter > 0)
        {
            myPlayerController.currentLocationOb.GetComponent<HexRenderer>().SuperClaim(myPlayerController.claimNumber, myPlayerController.claimMaterial);
            superClaimCounter--;
            superClaimText.text = "Super Claim, uses: (" + superClaimCounter.ToString() + ")";

            if (superClaimCounter == 0)
                superClaimButton.image.color = Color.red;
        }
    }

    private void ActivatePlayerScores()
    {
        for (int i = 0; i < players.Count; i++)
        {
            scoresList[i].gameObject.active = true;
        }
    }

    public void ShowMovableTile()
    {
        Collider[] cl = myPlayer.GetComponent<PlayerController>().currentLocationOb.GetComponent<HexRenderer>().CheckNearbyTiles();

        foreach (Collider c in cl)
        {
            if (c.gameObject.CompareTag("Tile") && c.gameObject != myPlayerController.currentLocationOb)
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
        myPlayerController.currentLocationTr = myPlayerController.currentLocationOb.transform;
        ClearMovableTiles();

        JSONObject moveData = new JSONObject();
        moveData.Add("name", myPlayerController.mySocketId);
        moveData.Add("location", hgl.tiles.IndexOf(myPlayerController.currentLocationOb));
        string plData = moveData.ToString();
        sioCom.Instance.Emit("MOVE", plData, false);

        UpdateTilesToList();

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
        myPlayerController.currentLocationOb.GetComponent<HexRenderer>().Claim(myPlayerController.claimNumber, myPlayerController.claimMaterial);
    }

    private void ChangeTurnText(int player)
    {
        switch (player)
        {
            case 0:
                turnText.color = Color.red;
                turnText.text = "Player 1 turn";
                break;
            case 1:
                turnText.color = Color.blue;
                turnText.text = "Player 2 turn";
                break;
            case 2:
                turnText.color = Color.yellow;
                turnText.text = "Player 3 turn";
                break;
            case 3:
                turnText.color= Color.green;
                turnText.text = "Player 4 turn";
                break;
        }
    }

    private void UpdateScores(List<int> scores)
    {
        for (int i = 0; i<scoresList.Count; i++)
        {
            scoresList[i].text = "P" + (i + 1).ToString() + ": " + scores[i];
        }
    }
}
