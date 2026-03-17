using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WitchHouseRectMapGenerator_v2 : MonoBehaviour
{
    [Header("General")]
    public string rootName = "Generated_WitchHouseMap_V2";
    public bool deleteOldMapFirst = true;
    public bool createPlayerStart = true;

    [Header("Wall Settings")]
    public float wallHeight = 3f;
    public float wallThickness = 0.25f;
    public float floorThickness = 0.1f;
    public float doorWidth = 2f;
    public float doorHeight = 2.2f;

    [Header("Room Sizes (X,Z)")]
    public Vector2 startRoomSize = new Vector2(8f, 6f);
    public Vector2 entryCorridorSize = new Vector2(4f, 8f);
    public Vector2 mainHallSize = new Vector2(10f, 10f);
    public Vector2 room1Size = new Vector2(8f, 8f);
    public Vector2 room2Size = new Vector2(8f, 8f);
    public Vector2 upperCorridorSize = new Vector2(12f, 4f);
    public Vector2 smallRoomSize = new Vector2(8f, 6f);
    public Vector2 finalRoomSize = new Vector2(8f, 6f);

    [Header("Optional Materials")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material markerMaterial;

    private Transform root;

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (deleteOldMapFirst) DeleteGeneratedMap();

        root = new GameObject(rootName).transform;
        root.SetParent(transform, false);

        // Layout: rectangular rooms with an L-shaped progression.
        // StartRoom -> EntryCorridor -> MainHall -> (Room1 / Room2 / UpperCorridor)
        // UpperCorridor -> SmallRoom / FinalRoom

        Vector3 startCenter = new Vector3(-10f, 0f, -8f);
        Vector3 entryCorridorCenter = new Vector3(0f, 0f, -8f);
        Vector3 mainHallCenter = new Vector3(0f, 0f, 2f);
        Vector3 room1Center = new Vector3(-10f, 0f, 2f);
        Vector3 room2Center = new Vector3(10f, 0f, 2f);
        Vector3 upperCorridorCenter = new Vector3(0f, 0f, 11f);
        Vector3 smallRoomCenter = new Vector3(-10f, 0f, 11f);
        Vector3 finalRoomCenter = new Vector3(10f, 0f, 11f);

        BuildRoomWithDoors(
            "StartRoom", startCenter, startRoomSize,
            north: true, south: true, east: true, west: true,
            northDoor: false, southDoor: false, eastDoor: true, westDoor: false);

        BuildRoomWithDoors(
            "EntryCorridor", entryCorridorCenter, entryCorridorSize,
            north: true, south: true, east: true, west: true,
            northDoor: true, southDoor: false, eastDoor: false, westDoor: true);

        BuildRoomWithDoors(
            "MainHall", mainHallCenter, mainHallSize,
            north: true, south: true, east: true, west: true,
            northDoor: true, southDoor: true, eastDoor: true, westDoor: true);

        BuildRoomWithDoors(
            "Room1", room1Center, room1Size,
            north: true, south: true, east: true, west: true,
            northDoor: false, southDoor: false, eastDoor: true, westDoor: false);

        BuildRoomWithDoors(
            "Room2", room2Center, room2Size,
            north: true, south: true, east: true, west: true,
            northDoor: false, southDoor: false, eastDoor: false, westDoor: true);

        BuildRoomWithDoors(
            "UpperCorridor", upperCorridorCenter, upperCorridorSize,
            north: true, south: true, east: true, west: true,
            northDoor: false, southDoor: true, eastDoor: true, westDoor: true);

        BuildRoomWithDoors(
            "SmallRoom", smallRoomCenter, smallRoomSize,
            north: true, south: true, east: true, west: true,
            northDoor: false, southDoor: false, eastDoor: true, westDoor: false);

        BuildRoomWithDoors(
            "FinalRoom", finalRoomCenter, finalRoomSize,
            north: true, south: true, east: true, west: true,
            northDoor: false, southDoor: false, eastDoor: false, westDoor: true);

        if (createPlayerStart)
        {
            CreatePlayerStart(startCenter + new Vector3(-1.5f, 1f, 0f));
        }
    }

    [ContextMenu("Delete Generated Map")]
    public void DeleteGeneratedMap()
    {
        Transform existing = transform.Find(rootName);
        if (existing == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(existing.gameObject);
        else
            Destroy(existing.gameObject);
#else
        Destroy(existing.gameObject);
#endif
    }

    private void BuildRoomWithDoors(
        string roomName,
        Vector3 center,
        Vector2 size,
        bool north,
        bool south,
        bool east,
        bool west,
        bool northDoor,
        bool southDoor,
        bool eastDoor,
        bool westDoor)
    {
        Transform roomRoot = new GameObject(roomName).transform;
        roomRoot.SetParent(root, false);

        CreateFloor(roomRoot, center, size, roomName + "_Floor");

        float hx = size.x * 0.5f;
        float hz = size.y * 0.5f;

        if (north) CreateHorizontalWall(roomRoot, center + new Vector3(0f, 0f, hz), size.x, roomName + "_NorthWall", northDoor);
        if (south) CreateHorizontalWall(roomRoot, center + new Vector3(0f, 0f, -hz), size.x, roomName + "_SouthWall", southDoor);
        if (east) CreateVerticalWall(roomRoot, center + new Vector3(hx, 0f, 0f), size.y, roomName + "_EastWall", eastDoor);
        if (west) CreateVerticalWall(roomRoot, center + new Vector3(-hx, 0f, 0f), size.y, roomName + "_WestWall", westDoor);
    }

    private void CreateFloor(Transform parent, Vector3 center, Vector2 size, string objName)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = objName;
        floor.transform.SetParent(parent, false);
        floor.transform.position = center + new Vector3(0f, -floorThickness * 0.5f, 0f);
        floor.transform.localScale = new Vector3(size.x, floorThickness, size.y);
        ApplyMaterial(floor, floorMaterial);
    }

    private void CreateHorizontalWall(Transform parent, Vector3 center, float width, string wallName, bool withDoor)
    {
        if (!withDoor)
        {
            CreateCube(parent, wallName, center + new Vector3(0f, wallHeight * 0.5f, 0f), new Vector3(width, wallHeight, wallThickness), wallMaterial);
            return;
        }

        float sideWidth = (width - doorWidth) * 0.5f;
        float sideOffset = (doorWidth + sideWidth) * 0.5f;
        float topHeight = wallHeight - doorHeight;

        if (sideWidth > 0.01f)
        {
            CreateCube(parent, wallName + "_Left", center + new Vector3(-sideOffset, wallHeight * 0.5f, 0f), new Vector3(sideWidth, wallHeight, wallThickness), wallMaterial);
            CreateCube(parent, wallName + "_Right", center + new Vector3(sideOffset, wallHeight * 0.5f, 0f), new Vector3(sideWidth, wallHeight, wallThickness), wallMaterial);
        }

        if (topHeight > 0.01f)
        {
            CreateCube(parent, wallName + "_Top", center + new Vector3(0f, doorHeight + topHeight * 0.5f, 0f), new Vector3(doorWidth, topHeight, wallThickness), wallMaterial);
        }
    }

    private void CreateVerticalWall(Transform parent, Vector3 center, float depth, string wallName, bool withDoor)
    {
        if (!withDoor)
        {
            CreateCube(parent, wallName, center + new Vector3(0f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, depth), wallMaterial);
            return;
        }

        float sideDepth = (depth - doorWidth) * 0.5f;
        float sideOffset = (doorWidth + sideDepth) * 0.5f;
        float topHeight = wallHeight - doorHeight;

        if (sideDepth > 0.01f)
        {
            CreateCube(parent, wallName + "_Upper", center + new Vector3(0f, wallHeight * 0.5f, sideOffset), new Vector3(wallThickness, wallHeight, sideDepth), wallMaterial);
            CreateCube(parent, wallName + "_Lower", center + new Vector3(0f, wallHeight * 0.5f, -sideOffset), new Vector3(wallThickness, wallHeight, sideDepth), wallMaterial);
        }

        if (topHeight > 0.01f)
        {
            CreateCube(parent, wallName + "_Top", center + new Vector3(0f, doorHeight + topHeight * 0.5f, 0f), new Vector3(wallThickness, topHeight, doorWidth), wallMaterial);
        }
    }

    private void CreatePlayerStart(Vector3 position)
    {
        GameObject start = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        start.name = "PlayerStart";
        start.transform.SetParent(root, false);
        start.transform.position = position;
        start.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        ApplyMaterial(start, markerMaterial);
    }

    private void CreateCube(Transform parent, string objName, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objName;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        ApplyMaterial(go, material);
    }

    private void ApplyMaterial(GameObject go, Material mat)
    {
        if (mat == null) return;
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = mat;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WitchHouseRectMapGenerator_v2))]
public class WitchHouseRectMapGeneratorV2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8);

        WitchHouseRectMapGenerator_v2 generator = (WitchHouseRectMapGenerator_v2)target;

        if (GUILayout.Button("Generate Map"))
        {
            generator.GenerateMap();
            EditorUtility.SetDirty(generator);
        }

        if (GUILayout.Button("Delete Generated Map"))
        {
            generator.DeleteGeneratedMap();
            EditorUtility.SetDirty(generator);
        }
    }
}
#endif
