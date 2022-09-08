using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public string mySOcketId;
    public HexGrindLayout hgl;
    public Transform currentLocationTr;
    public GameObject currentLocationOb;
    public GameObject lastLocationOb;
    public List<GameObject> movableTiles;
    public int startIndex;

    public bool myTurn;
    bool moving = false;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
