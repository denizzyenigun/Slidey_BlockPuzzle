using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class MyBlockController : MonoBehaviour
{
    public List<Transform> ListPiece => listPiece; // Blokun parçalarýný içeren liste
    [SerializeField] private List<Transform> listPiece = new List<Transform>(); // Blok parçalarýný saklayan özel liste

    private bool isDragging = false;
    private Vector2 offset;
    private GameObject clickedObject;
    private List<Vector2> oldPositions = new List<Vector2>();
    private List<Vector2> newPositions = new List<Vector2>();

    private void Start()
    {
        // StartCoroutine(MoveDown());
        // MoveDown();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }

        if (isDragging)
        {
            HandleDrag();
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopDragging();
        }
    }

    private void HandleClick()
    {
        // Raycast to determine which object was clicked
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        // Check if the ray hits a collider
        if (hit.collider != null)
        {
            // Get the clicked object
            clickedObject = hit.collider.gameObject;

            // Start dragging
            isDragging = true;

            // Get the grid coordinates of the clicked location
            Vector2 clickedPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int clickedX = Mathf.RoundToInt(clickedPosition.x);
            int clickedY = Mathf.RoundToInt(clickedPosition.y);

            offset = new Vector2(clickedX, clickedY);

            int childCount = clickedObject.transform.childCount;
            // Update the grid positions
            for (int i = 0; i < childCount; i++)
            {
                Transform child = clickedObject.transform.GetChild(i);
                oldPositions.Add(new Vector2(Mathf.Abs(child.position.x), Mathf.Abs(child.position.y)));
                //Debug.Log("Old: " + oldPositions[i].x);
            }
        }
    }

    private void HandleDrag()
    {
        newPositions.Clear();

        // offset = 7
        // newPosition = -1
        Vector2 newPosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
        int childCount = clickedObject.transform.childCount;

        newPosition.x = Mathf.RoundToInt(newPosition.x);
        newPosition.x += offset.x;

        // Round to the nearest integer for grid-based movement
        int newX = Mathf.RoundToInt(newPosition.x);

        int tmpX = newX;
        int objX = Mathf.RoundToInt(clickedObject.transform.position.x);

        for (int i = 0; i < childCount; i++)
        {
            int xOffset = (objX > tmpX) ? -i : i;
            tmpX = Mathf.Abs(newX + xOffset);

            if (IsWithinGridBounds(tmpX, Mathf.RoundToInt(clickedObject.transform.position.y))
               )
            {
                newPositions.Add(new Vector2(tmpX, clickedObject.transform.position.y));
                Debug.Log("NewPositionX: " + tmpX);
            }
        }

        if (CanMoveToPosition(clickedObject, newX) && newPositions.Count > 0)
        {
            float loc = newX - offset.x;

            UpdateGridPositions(clickedObject, newX);
            clickedObject.transform.localPosition = new Vector3(clickedObject.transform.localPosition.x + loc,
                clickedObject.transform.localPosition.y, 0);

            //  Debug.Log("NewPositionX: " + newPosition.x);
            //  Debug.Log("Offset: " + offset.x);
            //  Debug.Log("loc: " + loc);
        }
    }

    private bool CanMoveToPosition(GameObject obj, int newX)
    {
        int objY = Mathf.RoundToInt(obj.transform.position.y);
        int objX = Mathf.RoundToInt(obj.transform.position.x);
        int childCount = obj.transform.childCount;
        int newPositionX = newX;

        List<Vector2> emptyLocations = new List<Vector2>();

        // find empty locations
        for (int i = 0; i < 8; i++)
        {
            var valX = i;

            if (IsWithinGridBounds(valX, objY))
            {
                if (!GameManager.Instance.Grid[valX, objY])
                {
                    emptyLocations.Add(new Vector2(valX, objY));
                }
            }
        }

        // Debug for empty locations

        foreach (var loc in emptyLocations)
        {
            Debug.Log("Empty X: " + loc.x + "Empty Y:" + loc.y);
        }

        if (emptyLocations.Count <= 0 || !emptyLocations.Contains(new Vector2(newPositionX, objY))) return false;

        /*
            for (int i = 0; i < childCount; i++)
            {
                if (objX > newPositionX)
                {
                    newPositionX -= i;
                    if (emptyLocations.Contains(new Vector2(newPositionX, objY)))
                    {
                        Debug.Log("newPositionX- :" + newPositionX + " oldPositionX: " + newX + " i:" + i);
                    }
                    else
                    {
                        return false;
                    }

                    if (GameManager.Instance.Grid[newPositionX, objY] ||
                        IsWithinGridBounds(newPositionX, objY) == false)
                    {
                        return false;
                    }
                }
                else
                {
                    newPositionX += i;
                    if (emptyLocations.Contains(new Vector2(newPositionX, objY)))
                    {
                        Debug.Log("newPositionX+ :" + newPositionX + " oldPositionX: " + newX + " i:" + i);
                    }
                    else
                    {
                        return false;
                    }

                    if (GameManager.Instance.Grid[newPositionX, objY] ||
                        IsWithinGridBounds(newPositionX, objY) == false)
                    {
                        return false;
                    }
                }
            }
    */

        Vector2 firstEmptyPos = new Vector2(newPositionX, Mathf.RoundToInt(objY));
        bool test = false;
        for (int i = 0; i < childCount; i++)
        {
            if (i == 0) // Check only the first piece
            {
                if (!emptyLocations.Contains(firstEmptyPos) ||
                    !IsWithinGridBounds(Mathf.RoundToInt(firstEmptyPos.x), objY))
                    return false;
            }
            else // Skip checking self positions for the other pieces
            {
                Debug.Log("Parça " + (i) + " için yeni " + newPositions[i].x + " uygun değil.");

                int xOffset = (objX > newPositionX) ? -i : i;
                int tmpX = newPositionX + xOffset;

                if (newPositions.Contains(new Vector2(tmpX, objY)) || oldPositions.Contains(new Vector2(tmpX, objY)))
                {
                    test = true;
                }


                /* foreach (var pos in newPositions)
                 {
                     if (oldPositions.Contains(pos))
                     {
                         test = true;
                         break;
                     }
                 }
   */
                if (test == false) return false;
                //  if (!oldPositions.Contains(newPositions[i])) return false;
                //  if (!IsWithinGridBounds(Mathf.RoundToInt(newPositions[i].x), objY)) return false;
            }
        }


        Debug.Log("New loc is correct!");

        // All checks passed, can move to the new position
        return true;
    }

    private void UpdateGridPositions(GameObject obj, int newX)
    {
        int objY = Mathf.RoundToInt(obj.transform.position.y);

        // Update the old grid positions to false (empty)
        for (int i = 0; i < oldPositions.Count; i++)
        {
            int oldX = Mathf.RoundToInt(oldPositions[i].x);
            //   Debug.Log("Old: " + oldX);

            if (oldX >= 0 && oldX < GameManager.Instance.Grid.GetLength(0))
            {
                Debug.Log("OldX Updated: " + oldX);
                GameManager.Instance.Grid[oldX, objY] = false;
            }
        }

        // Update the new grid positions to true (occupied)
        for (int i = 0; i < newPositions.Count; i++)
        {
            int newloc = Mathf.RoundToInt(newPositions[i].x);

            if (newloc >= 0 && newloc < GameManager.Instance.Grid.GetLength(0))
            {
                Debug.Log("New Updated: " + newloc);
                GameManager.Instance.Grid[newloc, objY] = true;
            }
        }

        newPositions.Clear();
        oldPositions.Clear();
    }

    private bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && x < GameManager.Instance.Grid.GetLength(0) && y >= 0 &&
               y < GameManager.Instance.Grid.GetLength(1);
    }

    private void StopDragging()
    {
        // Reset dragging variables
        isDragging = false;
        clickedObject = null;
        offset = Vector2.zero;
        newPositions.Clear();
        oldPositions.Clear();
    }

    IEnumerator MoveDown()
    {
        var preview = GetPreviewPosition(); // Bloðun aþaðýdaki pozisyonunu alýr
        var isMovable =
            GameManager.Instance.IsInside(preview); // Bloðun hareket edilebilir olup olmadýðýný kontrol eder
        if (isMovable)
        {
            Move(); // Bloðu aþaðý doðru hareket ettirir
        }
        else
        {
            foreach (var piece in listPiece)
            {
                int x = Mathf.RoundToInt(piece.position.x);
                int y = Mathf.RoundToInt(piece.position.y);
                if (x < 0 || x >= GameManager.Instance.Grid.GetLength(0) || y < 0 ||
                    y >= GameManager.Instance.Grid.GetLength(1)) yield break;

                GameManager.Instance.Grid[x, y] = true; // Grid pozisyonunu isgal eder
            }

            GameManager.Instance.UpdateRemoveObjectController(); // Bloklarý kaldýrma iþlemini günceller
        }
    }

    public List<Vector2> GetPreviewPosition()
    {
        var result = new List<Vector2>();
        foreach (var piece in listPiece)
        {
            var position = piece.position;
            position.y++;
            result.Add(position);
        }

        return result;
    }

    // set block position
    private void Move()
    {
        Vector3 position = transform.position;
        position.y++;
        transform.position = position;

        // set grid position is empty, that means you can spawn that position
        int x = Mathf.RoundToInt(position.x);
        int y = Mathf.RoundToInt(position.y + 1);
        if (x < 0 || x >= 8 || y < 0 || y >= 10) return;
        GameManager.Instance.Grid[x, y] = false;
    }
}