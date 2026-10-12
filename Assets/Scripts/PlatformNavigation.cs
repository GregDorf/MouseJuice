using UnityEngine;
using UnityEngine.Tilemaps;
using Pathfinding;

public class PlatformNavigation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AstarPath astarPath;
    [SerializeField] private Tilemap groundTilemap;

    private void Start()
    {
        UpdateNavigation();
    }

    public void UpdateNavigation()
    {
        if (astarPath == null)
        {
            Debug.LogError($"{name}: AstarPath is not assigned.");
            return;
        }

        if (groundTilemap == null)
        {
            Debug.LogError($"{name}: Ground Tilemap is not assigned.");
            return;
        }

        astarPath.Scan();

        GridGraph graph = astarPath.data.gridGraph;

        if (graph == null)
        {
            Debug.LogError("PlatformNavigation: Grid Graph not found.");
            return;
        }

        int walkableCount = 0;
        int unwalkableCount = 0;

        // 1. Первый проход: устанавливаем проходимость (Walkable) для каждого узла
        for (int z = 0; z < graph.depth; z++)
        {
            for (int x = 0; x < graph.width; x++)
            {
                GridNodeBase node = graph.GetNode(x, z);

                if (node == null)
                    continue;

                Vector3 worldPosition = (Vector3)node.position;

                Vector3Int currentCell = groundTilemap.WorldToCell(worldPosition);

                Vector3Int cellBelow = currentCell + Vector3Int.down;
                Vector3Int cellHere = currentCell;
                Vector3Int cellAbove = currentCell + Vector3Int.up;

                bool hasGroundBelow = groundTilemap.HasTile(cellBelow);
                bool hasGroundHere = groundTilemap.HasTile(cellHere);
                bool hasGroundAbove = groundTilemap.HasTile(cellAbove);

                // Узел доступен только если под ним земля, а в самой клетке и над ней пусто
                bool isWalkable =
                    hasGroundBelow &&
                    !hasGroundHere &&
                    !hasGroundAbove;

                node.Walkable = isWalkable;

                if (isWalkable)
                    walkableCount++;
                else
                    unwalkableCount++;
            }
        }

        // 2. Второй проход: пересчитываем соединения (соседей) для каждого узла с учетом новой проходимости
        for (int z = 0; z < graph.depth; z++)
        {
            for (int x = 0; x < graph.width; x++)
            {
                graph.CalculateConnections(x, z);
            }
        }

        Debug.Log(
            $"PlatformNavigation: " +
            $"Walkable={walkableCount}, " +
            $"Unwalkable={unwalkableCount}"
        );
    }
}