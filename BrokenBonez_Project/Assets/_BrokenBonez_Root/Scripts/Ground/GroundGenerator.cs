using UnityEngine;
using System.Collections.Generic;

public class GroundGenerator : MonoBehaviour
{
    [Header("Initial Segment")]
    [SerializeField] GroundSegment initialSegmentPrefab;
    [SerializeField] int initialSegmentCount = 2;

    [Header("Segments")]
    [SerializeField] GroundSegment[] segmentPrefabs;
    [SerializeField] int activeSegmentCount = 3;

    [Header("Configuration")]
    [SerializeField] float groundYOffset = -2.80224f;
    [SerializeField] float spawnAheadX = 5f;
    [SerializeField] Camera cam;
    [SerializeField] WorldScroller worldScroller;

    List<GroundSegment> activeSegments = new List<GroundSegment>();

    void Start()
    {
        float spawnX = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x;

        // Segmentos iniciales fijos
        for (int i = 0; i < initialSegmentCount; i++)
        {
            GroundSegment segment = SpawnSpecificSegment(initialSegmentPrefab, spawnX);
            spawnX += segment.width;
        }

        // Segmentos aleatorios hasta cubrir la pantalla
        for (int i = 0; i < activeSegmentCount; i++)
        {
            GroundSegment segment = SpawnSegment(spawnX);
            spawnX += segment.width;
        }
    }

    void Update()
    {
        if (activeSegments.Count == 0) return;

        float leftEdge = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f)).x;
        float rightEdge = cam.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x + spawnAheadX;

        GroundSegment first = activeSegments[0];
        float firstRightEdge = first.CurrentX + first.width;

        if (firstRightEdge < leftEdge)
        {
            GroundSegment last = activeSegments[activeSegments.Count - 1];
            float newX = last.CurrentX + last.width;

            RemoveFromWorldScroller(first);
            Destroy(first.gameObject);
            activeSegments.RemoveAt(0);

            SpawnSegment(newX);
        }

        while (activeSegments.Count > 0)
        {
            GroundSegment last = activeSegments[activeSegments.Count - 1];
            float lastRightEdge = last.CurrentX + last.width;
            if (lastRightEdge < rightEdge)
                SpawnSegment(lastRightEdge);
            else
                break;
        }
    }

    GroundSegment SpawnSpecificSegment(GroundSegment prefab, float x)
    {
        GroundSegment segment = Instantiate(prefab,
            new Vector3(x, groundYOffset, 0f),
            Quaternion.identity);

        activeSegments.Add(segment);
        AddToWorldScroller(segment);
        return segment;
    }

    GroundSegment SpawnSegment(float x)
    {
        GroundSegment prefab = PickRandomSegment();
        GroundSegment segment = Instantiate(prefab,
            new Vector3(x, groundYOffset, 0f),
            Quaternion.identity);

        Debug.Log($"Spawneado: {prefab.gameObject.name} en X: {x:F2}");

        activeSegments.Add(segment);
        AddToWorldScroller(segment);
        return segment;
    }

    GroundSegment PickRandomSegment()
    {
        float total = 0f;
        foreach (var p in segmentPrefabs)
            total += p.spawnProbability;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var p in segmentPrefabs)
        {
            cumulative += p.spawnProbability;
            if (roll <= cumulative) return p;
        }

        return segmentPrefabs[0];
    }

    void AddToWorldScroller(GroundSegment segment)
    {
        var list = new List<Rigidbody2D>(worldScroller.groundObjects);
        foreach (var rb in segment.rigidbodies)
            if (rb != null) list.Add(rb);
        worldScroller.groundObjects = list.ToArray();
    }

    void RemoveFromWorldScroller(GroundSegment segment)
    {
        var list = new List<Rigidbody2D>(worldScroller.groundObjects);
        foreach (var rb in segment.rigidbodies)
            list.Remove(rb);
        worldScroller.groundObjects = list.ToArray();
    }
}