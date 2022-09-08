using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firesplash.UnityAssets.SocketIO;
using SimpleJSON;
using UnityEngine.EventSystems;

public class ServerControl : MonoBehaviour
{
    public SocketIOCommunicator sioCom;
    public GameObject player;
    public HexGrindLayout hgl;

    public Transform currentLocationTr;
    public GameObject currentLocationOb;
    public GameObject lastLocationOb;
    public List<GameObject> movableTiles;

    bool myTurn = true;
    bool moving = false;

    // Start is called before the first frame update
    void Start()
    {
        sioCom.Instance.On("connect", (payload) =>
        {
            Debug.Log("Connected, id: " + sioCom.Instance.SocketID);
            sioCom.Instance.Emit("CREATEPLAYER");
        });

        sioCom.Instance.On("InstancePlayer", (playerInfo) =>
        {
            JSONNode node = JSON.Parse(playerInfo);

            GameObject go = Instantiate(player, hgl.tiles[node["spawnIndex"]].transform.position, Quaternion.identity);
            PlayerController pc = go.GetComponent<PlayerController>();
            go.name = "Player" + node["socketId"];
            pc.mySOcketId = node["socketId"];
            currentLocationOb = hgl.tiles[node["spawnIndex"]].gameObject;
            currentLocationTr = currentLocationOb.transform;
            go.transform.position = currentLocationTr.position;
            player = go;
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
                                    currentLocationOb = hit.collider.gameObject;
                                    //Conquest new tile
                                    Move();
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
        Collider[] cl = currentLocationOb.GetComponent<HexRenderer>().CheckNearbyTiles();

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
        player.transform.position = currentLocationOb.transform.position;
        ClearMovableTiles();
    }
    private void ClearMovableTiles()
    {
        foreach (GameObject go in movableTiles)
        {
            go.GetComponent<HexRenderer>().NotMovable();
        }
        movableTiles.Clear();
    }

}
