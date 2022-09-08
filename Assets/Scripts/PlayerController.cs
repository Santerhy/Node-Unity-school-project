using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public HexGrindLayout hgl;
    public Transform currentLocationTr;
    public GameObject currentLocationOb;
    public GameObject lastLocationOb;
    public List<GameObject> movableTiles;

    public bool myTurn;
    bool moving = false;
    // Start is called before the first frame update
    void Start()
    {
        currentLocationOb = hgl.tiles[0];
        currentLocationTr = currentLocationOb.transform;
        myTurn = true;
        transform.position = currentLocationTr.position;
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

    //OnClick function
    public void ShowMovableTile()
    {
        Collider[] cl =  currentLocationOb.GetComponent<HexRenderer>().CheckNearbyTiles();
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
        transform.position = currentLocationOb.transform.position;
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
