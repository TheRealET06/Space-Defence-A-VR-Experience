using System;
using System.Collections.Generic;
using UnityEngine;

/// Put this on the enemy prefab. Moves the enemy through a list of world-space
/// waypoints at a constant speed. Movement is done with the transform, so the
/// enemy follows the path regardless of any colliders on it or on the path objects.
public class PathEnemyFollower : MonoBehaviour
{
    [SerializeField] float speed = 0.5f;            // meters per second
    [SerializeField] bool faceDirection = true;
    [SerializeField] float turnSpeed = 360f;        // degrees per second
    [SerializeField] bool destroyOnArrival = true;

    /// Fired when the final waypoint is reached (before any destroy).
    public event Action<PathEnemyFollower> ReachedEnd;

    public float Speed { get => speed; set => speed = value; }

    List<Vector3> waypoints;
    Vector3 up = Vector3.up;
    int next;

    public void Initialize(List<Vector3> points, Vector3 upDirection)
    {
        if (points == null || points.Count == 0) return;

        waypoints = points;
        up = upDirection;
        transform.position = points[0];
        next = 1;
    }

    void Update()
    {
        if (waypoints == null || next >= waypoints.Count) return;

        // Move, carrying leftover distance across waypoints so corners don't slow it down.
        float remaining = speed * Time.deltaTime;
        while (remaining > 0f && next < waypoints.Count)
        {
            Vector3 target = waypoints[next];
            float dist = Vector3.Distance(transform.position, target);

            if (dist <= remaining)
            {
                transform.position = target;
                remaining -= dist;
                next++;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, target, remaining);
                remaining = 0f;
            }
        }

        if (faceDirection && next < waypoints.Count)
        {
            Vector3 dir = waypoints[next] - transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion want = Quaternion.LookRotation(dir.normalized, up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
            }
        }

        if (next >= waypoints.Count)
        {
            ReachedEnd?.Invoke(this);
            if (destroyOnArrival) Destroy(gameObject);
        }
    }
}