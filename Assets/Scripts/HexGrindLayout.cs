using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGrindLayout : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int gridSize;

    [Header("Tile Settings")]
    public float outerSize = 2f;
    public float innerSize = 0f;
    public float height = -1f;
    public bool isFlatTopped = true;
    public Material material;

    public GameObject hr;
    public List<GameObject> tiles;

    private void OnEnable()
    {
        LayoutGrid();
    }

    private void LayoutGrid()
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                GameObject tile = Instantiate(hr, Vector3.zero, Quaternion.identity);
                tile.transform.position = GetPositionForHexFromCoordinate(new Vector2Int(x, y));

                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.isFlatTopped = isFlatTopped;
                hexRenderer.outerSize = outerSize;
                hexRenderer.innerSize = innerSize;
                hexRenderer.height = height;
                hexRenderer.SetMaterial(material);
                hexRenderer.DrawMesh();

                tile.transform.SetParent(transform, true);
                tiles.Add(tile);
            }
        }
    }

    public Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
    {
        int column = coordinate.x;
        int row = coordinate.y;
        float width;
        float height;
        float xPosition;
        float yPosition;
        bool shouldOffset;
        float horizontalDistance;
        float verticalDistance;
        float offset;
        float size = outerSize;

        shouldOffset = (column % 2) == 0;
        width = 2f * size;
        height = Mathf.Sqrt(3f) * size;

        horizontalDistance = width * (3f / 4f);
        verticalDistance = height;

        offset = (shouldOffset) ? height / 2 : 0;
        xPosition = column * horizontalDistance;
        yPosition = (row * verticalDistance) - offset;

        return new Vector3(xPosition, 0, -yPosition);

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
