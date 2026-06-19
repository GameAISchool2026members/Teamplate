using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.12f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float obstacleAvoidanceRadius = 1.49f;
    [SerializeField] private float obstacleAvoidanceStrength = 1.25f;

    [Header("Pathfinding")]
    [SerializeField] private float cellSize = 0.75f;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float searchPadding = 8f;
    [SerializeField] private int lookAheadWaypoints = 2;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private Transform player;
    private Rigidbody2D rb;
    private readonly List<Vector2> currentPath = new List<Vector2>();
    private int pathIndex;
    private float nextRepathTime;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        TryFindPlayer();
    }

    private void FixedUpdate()
    {
        TryFindPlayer();

        if (player == null || rb == null)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            return;
        }

        if (Time.time >= nextRepathTime || pathIndex >= currentPath.Count)
        {
            RebuildPath();
        }

        FollowPath();
    }

    private void TryFindPlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            return;
        }

        PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            player = playerMovement.transform;
        }
    }

    private void RebuildPath()
    {
        currentPath.Clear();
        pathIndex = 0;
        nextRepathTime = Time.time + repathInterval;

        List<Vector2> newPath = CalculatePath(rb.position, player.position);
        if (newPath != null)
        {
            currentPath.AddRange(newPath);
        }
    }

    private void FollowPath()
    {
        if (currentPath.Count == 0)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
            return;
        }

        while (pathIndex < currentPath.Count && Vector2.Distance(rb.position, currentPath[pathIndex]) <= stoppingDistance)
        {
            pathIndex++;
        }

        if (pathIndex >= currentPath.Count)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
            return;
        }

        Vector2 targetPoint = GetBestSteeringPoint();
        Vector2 direction = (targetPoint - rb.position).normalized;
        Vector2 avoidance = GetObstacleAvoidance();
        Vector2 desiredDirection = (direction + avoidance).normalized;
        Vector2 desiredVelocity = desiredDirection * moveSpeed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);
    }

    private Vector2 GetObstacleAvoidance()
    {
        Vector2 avoidance = Vector2.zero;
        Collider2D[] nearbyObstacles = Physics2D.OverlapCircleAll(rb.position, obstacleAvoidanceRadius, obstacleMask);

        foreach (Collider2D obstacle in nearbyObstacles)
        {
            if (obstacle == null)
                continue;

            if (obstacle.attachedRigidbody == rb)
                continue;

            if (player != null && (obstacle.transform == player || obstacle.transform.IsChildOf(player)))
                continue;

            Vector2 closestPoint = obstacle.ClosestPoint(rb.position);
            Vector2 awayFromObstacle = rb.position - closestPoint;
            float distance = awayFromObstacle.magnitude;

            if (distance <= 0.001f)
                continue;

            float weight = (obstacleAvoidanceRadius - distance) / obstacleAvoidanceRadius;
            avoidance += awayFromObstacle.normalized * Mathf.Clamp01(weight) * obstacleAvoidanceStrength;
        }

        return Vector2.ClampMagnitude(avoidance, obstacleAvoidanceStrength);
    }

    private Vector2 GetBestSteeringPoint()
    {
        Vector2 bestPoint = currentPath[pathIndex];
        int maxIndex = Mathf.Min(currentPath.Count - 1, pathIndex + lookAheadWaypoints);

        for (int i = maxIndex; i >= pathIndex; i--)
        {
            if (!Physics2D.Linecast(rb.position, currentPath[i], obstacleMask))
            {
                bestPoint = currentPath[i];
                break;
            }
        }

        return bestPoint;
    }

    private List<Vector2> CalculatePath(Vector2 start, Vector2 goal)
    {
        Vector2 minBounds = new Vector2(
            Mathf.Min(start.x, goal.x) - searchPadding,
            Mathf.Min(start.y, goal.y) - searchPadding
        );

        Vector2 maxBounds = new Vector2(
            Mathf.Max(start.x, goal.x) + searchPadding,
            Mathf.Max(start.y, goal.y) + searchPadding
        );

        int width = Mathf.Max(1, Mathf.CeilToInt((maxBounds.x - minBounds.x) / cellSize));
        int height = Mathf.Max(1, Mathf.CeilToInt((maxBounds.y - minBounds.y) / cellSize));

        Node[,] grid = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 worldPoint = new Vector2(
                    minBounds.x + (x + 0.5f) * cellSize,
                    minBounds.y + (y + 0.5f) * cellSize
                );

                bool walkable = IsWalkable(worldPoint, goal);
                grid[x, y] = new Node(worldPoint, walkable, x, y);
            }
        }

        Node startNode = GetNodeForPoint(grid, minBounds, start);
        Node goalNode = GetNodeForPoint(grid, minBounds, goal);

        if (startNode == null || goalNode == null || !goalNode.walkable)
            return null;

        List<Node> openList = new List<Node> { startNode };
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0f;
        startNode.hCost = GetDistance(startNode, goalNode);

        while (openList.Count > 0)
        {
            Node currentNode = GetLowestCostNode(openList);
            openList.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == goalNode)
            {
                return RetracePath(startNode, goalNode);
            }

            foreach (Node neighbour in GetNeighbours(grid, currentNode))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;

                float newMovementCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCost < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCost;
                    neighbour.hCost = GetDistance(neighbour, goalNode);
                    neighbour.parent = currentNode;

                    if (!openList.Contains(neighbour))
                    {
                        openList.Add(neighbour);
                    }
                }
            }
        }

        return null;
    }

    private bool IsWalkable(Vector2 worldPoint, Vector2 goal)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldPoint, Vector2.one * (cellSize * 0.7f), 0f, obstacleMask);

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            if (hit.attachedRigidbody == rb)
                continue;

            if (player != null && (hit.transform == player || hit.transform.IsChildOf(player)))
                continue;

            if (Vector2.Distance(worldPoint, goal) <= cellSize * 0.75f)
                continue;

            return false;
        }

        return true;
    }

    private Node GetNodeForPoint(Node[,] grid, Vector2 minBounds, Vector2 point)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((point.x - minBounds.x) / cellSize), 0, grid.GetLength(0) - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((point.y - minBounds.y) / cellSize), 0, grid.GetLength(1) - 1);
        return grid[x, y];
    }

    private IEnumerable<Node> GetNeighbours(Node[,] grid, Node node)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        if (node.gridX > 0)
            yield return grid[node.gridX - 1, node.gridY];

        if (node.gridX < width - 1)
            yield return grid[node.gridX + 1, node.gridY];

        if (node.gridY > 0)
            yield return grid[node.gridX, node.gridY - 1];

        if (node.gridY < height - 1)
            yield return grid[node.gridX, node.gridY + 1];

        if (node.gridX > 0 && node.gridY > 0)
            yield return grid[node.gridX - 1, node.gridY - 1];

        if (node.gridX > 0 && node.gridY < height - 1)
            yield return grid[node.gridX - 1, node.gridY + 1];

        if (node.gridX < width - 1 && node.gridY > 0)
            yield return grid[node.gridX + 1, node.gridY - 1];

        if (node.gridX < width - 1 && node.gridY < height - 1)
            yield return grid[node.gridX + 1, node.gridY + 1];
    }

    private float GetDistance(Node a, Node b)
    {
        return Vector2.Distance(a.worldPosition, b.worldPosition);
    }

    private Node GetLowestCostNode(List<Node> nodes)
    {
        Node bestNode = nodes[0];
        float bestScore = bestNode.FCost;

        for (int i = 1; i < nodes.Count; i++)
        {
            float score = nodes[i].FCost;
            if (score < bestScore)
            {
                bestScore = score;
                bestNode = nodes[i];
            }
        }

        return bestNode;
    }

    private List<Vector2> RetracePath(Node startNode, Node goalNode)
    {
        List<Vector2> path = new List<Vector2>();
        Node currentNode = goalNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;

            if (currentNode == null)
                break;
        }

        path.Reverse();
        return path;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player caught!");
        }
    }

    private sealed class Node
    {
        public readonly Vector2 worldPosition;
        public readonly bool walkable;
        public readonly int gridX;
        public readonly int gridY;
        public float gCost = float.PositiveInfinity;
        public float hCost = float.PositiveInfinity;
        public Node parent;

        public Node(Vector2 worldPosition, bool walkable, int gridX, int gridY)
        {
            this.worldPosition = worldPosition;
            this.walkable = walkable;
            this.gridX = gridX;
            this.gridY = gridY;
        }

        public float FCost => gCost + hCost;
    }
}