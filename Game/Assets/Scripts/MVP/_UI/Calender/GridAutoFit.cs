using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridAutoFit : MonoBehaviour
{
    public int columns = 7;
    public int rows = 4;
    public Vector2 spacing = new Vector2(6, 6);

    IEnumerator Start()
    {
        // Đợi 1 frame để UI layout xong
        yield return new WaitForEndOfFrame();


        GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
        RectTransform rt = GetComponent<RectTransform>();

        float width = rt.rect.width;
        float height = rt.rect.height;

        // DEBUG
        Debug.Log($"Grid size: {width} x {height}");

        float cellWidth = (width - (columns - 1) * spacing.x) / columns;
        float cellHeight = (height - (rows - 1) * spacing.y) / rows;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.spacing = spacing;
    }
}
