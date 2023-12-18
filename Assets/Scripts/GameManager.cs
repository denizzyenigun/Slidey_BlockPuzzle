using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public MyBlockController Current { get; set; } // �u anki blokun referans�n� tutar
    public bool[,] Grid { get; private set; } // Oyun alan�n�n gridini temsil eden bir �ok boyutlu dizi
    private const int GridSizeX = 8; // Grid boyutu X ekseni
    private const int GridSizeY = 10; // Grid boyutu Y ekseni
    public float GameSpeed => gameSpeed;
    [SerializeField, Range(.1f, 1f)] private float gameSpeed = 1; // Oyun h�z�, aral�k ile s�n�rlanm��
    [SerializeField] private List<MyBlockController> listPrefabs; // Blok prefablar� listesi

    private List<MyBlockController> _listHistory = new List<MyBlockController>(); // Oyun ge�mi�ini saklayan liste

    #region Test

    public bool IsOpenTest; // Test modunun a��k veya kapal� oldu�unu belirten bir bayrak
    [SerializeField] SpriteRenderer displayDataPrefabs; // Test i�in kullan�lan veri prefab�

    private SpriteRenderer[,]
        previewDisplay = new SpriteRenderer[GridSizeX, GridSizeY]; // Test i�in kullan�lan veri ��elerini saklayan dizi

    public List<int> PieceCountList = new List<int>();
    private int selectedBlock;
    public int spawnCount = 1;

    private void UpdateDisplayPreview()
    {
        if (!IsOpenTest) return; // Test modu kapal�ysa i�lemi atla
        for (int i = 0; i < GridSizeX; i++)
        {
            for (int j = 0; j < GridSizeY; j++)
            {
                var active = Grid[i, j];
                var sprite = previewDisplay[i, j];

                sprite.color = active ? Color.green : Color.red; // Veriyi ye�il veya k�rm�z� olarak g�r�nt�le
            }
        }
    }

    #endregion

    private void Awake()
    {
        Instance = this; // Singleton �rne�i ayarla
        Grid = new bool[GridSizeX, GridSizeY]; // Grid'i ba�lat

        if (IsOpenTest)
        {
            for (int i = 0; i < GridSizeX; i++)
            {
                for (int j = 0; j < GridSizeY; j++)
                {
                    var sprite = Instantiate(displayDataPrefabs, transform);
                    sprite.transform.position = new Vector3(i, j, 0);

                    previewDisplay[i, j] = sprite; // Test verisi ��elerini olu�tur ve sakla
                }
            }
        }
    }

    private void Start()
    {
        StartCoroutine(SpawnTimer());
    }

    IEnumerator SpawnTimer()
    {
        while (true) // Infinite loop to keep spawning every 3 seconds
        {
            var count = Random.Range(1, Instance.spawnCount);
            var blockRange = Random.Range(0, 6);

            var num = 0;

            foreach (var counter in Instance.PieceCountList)
            {
                num += counter;
            }

            if (num < blockRange)
            {
                for (int i = 0; i < count; i++)
                {
                    Instance.Spawn(); // Yeni bir blok oluþturur
                }
            }
            else
            {
                StopCoroutine(SpawnTimer());
              //  Debug.Log("Block Range reached!");
            }

            yield return new WaitForSeconds(1f);
        }
    }


    public void Spawn()
    {
        // selected block information
        selectedBlock = Random.Range(0, listPrefabs.Count); // select a random block
        var blockController = listPrefabs[selectedBlock];
        int pieceCount = blockController.ListPiece.Count;

        // random spawn position
        float randomX = Random.Range(3, 10); // last position on the grid for a spawn
        Vector3 spawnPosition = new Vector3(randomX, 9, 0);

        // check the available positions on the grid
        foreach (var occupiedList in blockController.GetPreviewPosition())
        {
            // get our block position
            int x = Mathf.RoundToInt(occupiedList.x);
            int y = Mathf.RoundToInt(occupiedList.y);

            // Out of bounds
            if (x < 0 || x >= GridSizeX || y < 0 || y >= GridSizeY) continue;

            //Hit something
            if (Grid[1, 0] || Grid[2, 0] || Grid[3, 0])
            {
                Debug.Log("that position is not available x:" + x + " y:" + y);
            }
            else
            {
                // spawn a new block
                var newBlock = Instantiate(blockController);
                Current = newBlock; // Set as the current block

                _listHistory.Add(newBlock); // Add to the block history

                // Iterate through each child object of the new block
                foreach (Transform childObject in newBlock.transform)
                {
                    // Iterate through each child of the childObject
                    foreach (Transform child in childObject.transform)
                    {
                        var px = Mathf.RoundToInt(child.position.x);
                        var py = Mathf.RoundToInt(child.position.y);

                        // Ensure the grid indices are within bounds
                        if (px >= 0 && px < Grid.GetLength(0) && py >= 0 && py < Grid.GetLength(1))
                        {
                            Grid[px, py] = true;
                           // Debug.Log("SpawnedX: " + px + " SpawnedY: " + py);
                        }
                        else
                        {
                            Debug.LogError("Grid indices out of bounds.");
                        }
                    }
                }
                
                break;
            }
        }

        UpdateDisplayPreview(); // Test verisi g�r�nt�lemeyi g�ncelle

        PieceCountList.Add(blockController.ListPiece.Count);
    }

    // grid icinde olup, olmadigini kontrol et
    public bool IsInside(List<Vector2> listCoordinate)
    {
        foreach (var coordinate in listCoordinate)
        {
            int x = Mathf.RoundToInt(coordinate.x);
            int y = Mathf.RoundToInt(coordinate.y);

            if (x < 0 || x >= GridSizeX || y < 0 || y >= GridSizeY)
            {
                // Out of bounds
                return false;
            }

            if (Grid[x, y])
            {
                //Hit something
                return false;
            }
        }

        return true; // Blo�un grid i�inde oldu�unu belirt
    }

    // satir tamamen dolu mu kontrol et
    private bool IsFullRow(int index)
    {
        for (int i = 0; i < GridSizeX; i++)
        {
            if (!Grid[i, index])
                return false;
        }

        return true;
    }

    public void UpdateRemoveObjectController()
    {
        for (int i = 0; i < GridSizeY; i++)
        {
            var isFull = IsFullRow(i);
            if (isFull)
            {
                //Remove
                foreach (var myBlock in _listHistory)
                {
                    var willDestroy = new List<Transform>();
                    foreach (var piece in myBlock.ListPiece)
                    {
                        int y = Mathf.RoundToInt(piece.position.y);
                        if (y == i)
                        {
                            //Add Remove
                            willDestroy.Add(piece);
                        }
                        else if (y > i)
                        {
                            //Move Down
                            var position = piece.position;
                            position.y--;
                            piece.position = position;
                        }
                    }

                    //Remove
                    foreach (var item in willDestroy)
                    {
                        myBlock.ListPiece.Remove(item);
                        Destroy(item.gameObject);
                    }
                }

                //ChangeData
                for (int j = 0; j < GridSizeX; j++)
                    Grid[j, i] = false;

                for (int j = i + 1; j < GridSizeY; j++)
                for (int k = 0; k < GridSizeX; k++)
                    Grid[k, j - 1] = Grid[k, j];

                //Call Again
                UpdateRemoveObjectController();
                return;
            }
        }
    }
}