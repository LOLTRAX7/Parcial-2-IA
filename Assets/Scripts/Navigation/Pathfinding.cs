using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    public static List<Node> AStar(Node startingNode, Node endNode, bool avoidSafeZones = false)
    {
        if (startingNode == null || endNode == null || startingNode.Blocked || endNode.Blocked)
            return new List<Node>();

        if (avoidSafeZones && (startingNode.IsSafeZone || endNode.IsSafeZone))
            return new List<Node>();

        PriorityQueue<Node> frontier = new PriorityQueue<Node>();
        frontier.Enqueue(startingNode, 0f);

        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        cameFrom.Add(startingNode, null);

        Dictionary<Node, float> costSoFar = new Dictionary<Node, float>();
        costSoFar.Add(startingNode, 0f);

        HashSet<Node> closed = new HashSet<Node>();

        Vector3 endPosition = endNode.transform.position;

        while (frontier.Count != 0)
        {
            Node current = frontier.Dequeue();

            if (current == endNode)
            {
                return ReconstructPath(cameFrom, current);
            }

            if (!closed.Add(current)) continue;

            foreach (Node neighbor in current.Neighbors)
            {
                if (neighbor.Blocked || closed.Contains(neighbor)) continue;
                if (avoidSafeZones && neighbor.IsSafeZone) continue;
                if (avoidSafeZones && Zone.CrossesSafeZone(current.transform.position, neighbor.transform.position, 0.45f)) continue;

                float newCost = costSoFar[current] + current.DistanceTo(neighbor);

                if (cameFrom.ContainsKey(neighbor) && newCost >= costSoFar[neighbor]) continue;

                float heuristic = Vector3.Distance(neighbor.transform.position, endPosition);
                frontier.Enqueue(neighbor, newCost + heuristic);
                cameFrom[neighbor] = current;
                costSoFar[neighbor] = newCost;
            }
        }

        return new List<Node>();
    }

    private static List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        List<Node> path = new List<Node>();

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }

    public static float PathCost(List<Node> path)
    {
        float cost = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            cost += path[i].DistanceTo(path[i + 1]);
        }

        return cost;
    }
}
