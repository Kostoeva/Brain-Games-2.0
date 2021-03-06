﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maze : MonoBehaviour {

    public MazeCell cellPrefab;
    public int maxRoomSize = 15;
    public IntVector2 size;
    public float generationStepDelay;
    public MazePassage passagePrefab;
    public int mazeWallMultiplier;
    public MazeWall[] wallPrefabs;
    public MazeDoor doorPrefab;
    [Range(0f, 1f)]
    public float doorProbability;
    public MazeRoomSettings[] roomSettings;
    public GameObject[] mazeObstacles;

    private MazeCell[,] cells;

    private List<MazeRoom> rooms = new List<MazeRoom>();
    private int currentRoom;

    private MazeRoom CreateRoom(int indexToExclude)
    {
        MazeRoom newRoom = ScriptableObject.CreateInstance<MazeRoom>();
        newRoom.settingsIndex = Random.Range(0, roomSettings.Length);
        if (newRoom.settingsIndex == indexToExclude)
        {
            newRoom.settingsIndex = (newRoom.settingsIndex + 1) % roomSettings.Length;
        }
        newRoom.settings = roomSettings[newRoom.settingsIndex];
        rooms.Add(newRoom);
        return newRoom;
    }

    public MazeCell GetCell(IntVector2 coordinates)
    {
        return cells[coordinates.x, coordinates.z];
    }

    //public IEnumerator Generate()
    //{
    //    WaitForSeconds delay = new WaitForSeconds(generationStepDelay);
    //    cells = new MazeCell[size.x, size.z];
    //    List<MazeCell> activeCells = new List<MazeCell>();
    //    DoFirstGenerationStep(activeCells);
    //    while (activeCells.Count > 0)
    //    {
    //        yield return delay;
    //        DoNextGenerationStep(activeCells);
    //    }
    //}

    public void Generate()
    {
        //WaitForSeconds delay = new WaitForSeconds(generationStepDelay);
        cells = new MazeCell[size.x, size.z];
        List<MazeCell> activeCells = new List<MazeCell>();
        DoFirstGenerationStep(activeCells);
        while (activeCells.Count > 0)
        {
            //yield return delay;
            DoNextGenerationStep(activeCells);
        }
        for (int i = 0; i < rooms.Count; i++)
        {
            CreateObstacleInRoom(rooms[i]);
            //rooms[i].Hide();
        }
    }

    private void CreateObstacleInRoom(MazeRoom room)
    {
        GameObject obstacle = mazeObstacles[Random.Range(0, mazeObstacles.Length)];
        MazeCell cell = room.RandomCell();
        obstacle = Instantiate(obstacle, this.transform);
        obstacle.transform.parent = cell.transform;
        obstacle.transform.localPosition = Vector3.zero;
        obstacle.transform.localScale = Vector3.one * 0.25f;
        room.obstacle = obstacle.GetComponent(typeof(Obstacle)) as Obstacle;
    }

    private void DoFirstGenerationStep(List<MazeCell> activeCells)
    {
        MazeCell newCell = CreateCell(RandomCoordinates);
        newCell.Initialize(CreateRoom(-1));
        activeCells.Add(newCell);
    }

    private void CreatePassageInSameRoom(MazeCell cell, MazeCell otherCell, MazeDirection direction)
    {
        MazePassage passage = Instantiate(passagePrefab, this.transform) as MazePassage;
        passage.Initialize(cell, otherCell, direction);
        passage = Instantiate(passagePrefab, this.transform) as MazePassage;
        passage.Initialize(otherCell, cell, direction.GetOpposite());
        if (cell.room != otherCell.room)
        {
            MazeRoom roomToAssimilate = otherCell.room;
            cell.room.Assimilate(roomToAssimilate);
            rooms.Remove(roomToAssimilate);
            Destroy(roomToAssimilate);
        }
    }

    private void DoNextGenerationStep(List<MazeCell> activeCells)
    {
        //int currentIndex = 0;
        //int currentIndex = activeCells.Count/2;
        int currentIndex = Mathf.Max(activeCells.Count - 4, 0);
        //int currentIndex = activeCells.Count - 1;
        MazeCell currentCell = activeCells[currentIndex];
        if (currentCell.IsFullyInitialized)
        {
            activeCells.RemoveAt(currentIndex);
            return;
        }
        MazeDirection direction = currentCell.RandomUninitializedDirection;
        IntVector2 coordinates = currentCell.coordinates + direction.ToIntVector2();
        if (ContainsCoordinates(coordinates))
        {
            MazeCell neighbor = GetCell(coordinates);
            if (neighbor == null)
            {
                neighbor = CreateCell(coordinates);
                CreatePassage(currentCell, neighbor, direction);
                activeCells.Add(neighbor);
            }
            else if (currentCell.room.settingsIndex == neighbor.room.settingsIndex)
            {
                CreatePassageInSameRoom(currentCell, neighbor, direction);
            }
            else
            {
                CreateWall(currentCell, neighbor, direction);
                // No longer remove the cell here.
            }
        }
        else
        {
            CreateWall(currentCell, null, direction);
            // No longer remove the cell here.
        }
    }

    private MazeCell CreateCell(IntVector2 coordinates)
    {
        MazeCell newCell = Instantiate(cellPrefab, this.transform) as MazeCell;
        cells[coordinates.x, coordinates.z] = newCell;
        newCell.coordinates = coordinates;
        newCell.name = "Maze Cell " + coordinates.x + ", " + coordinates.z;
        newCell.transform.parent = transform;
        newCell.transform.localPosition =
            new Vector3(coordinates.x - size.x * 0.5f + 0.5f, 0f, coordinates.z - size.z * 0.5f + 0.5f);
        return newCell;
    }

    private void CreatePassage(MazeCell cell, MazeCell otherCell, MazeDirection direction)
    {
        MazePassage prefab = null;
        if (cell.room.cells.Count > maxRoomSize)
        {
            prefab = doorPrefab;
        }
        else
        {
            prefab = Random.value < doorProbability ? doorPrefab : passagePrefab;
        }
        MazePassage passage = Instantiate(prefab, this.transform) as MazePassage;
        passage.Initialize(cell, otherCell, direction);
        passage = Instantiate(prefab, this.transform) as MazePassage;
        if (passage is MazeDoor)
        {
            otherCell.Initialize(CreateRoom(cell.room.settingsIndex));
        }
        else
        {
            otherCell.Initialize(cell.room);
        }
        passage.Initialize(otherCell, cell, direction.GetOpposite());
    }

    private void CreateWall(MazeCell cell, MazeCell otherCell, MazeDirection direction)
    {
        MazeWall wall = Instantiate(wallPrefabs[Mathf.Max(0, Random.Range(-mazeWallMultiplier, wallPrefabs.Length))], this.transform) as MazeWall;
        wall.Initialize(cell, otherCell, direction);
        if (otherCell != null)
        {
            wall = Instantiate(wallPrefabs[Mathf.Max(0, Random.Range(-mazeWallMultiplier, wallPrefabs.Length))], this.transform) as MazeWall;
            wall.Initialize(otherCell, cell, direction.GetOpposite());
        }
    }

    public IntVector2 RandomCoordinates
    {
        get
        {
            return new IntVector2(Random.Range(0, size.x), Random.Range(0, size.z));
        }
    }

    public bool ContainsCoordinates(IntVector2 coordinate)
    {
        return coordinate.x >= 0 && coordinate.x < size.x && coordinate.z >= 0 && coordinate.z < size.z;
    }
}
