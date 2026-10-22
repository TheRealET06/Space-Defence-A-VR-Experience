using UnityEngine;

public class SimpleWaypointFollower : MonoBehaviour
{
    [SerializeField]
    Transform[] waypoints;

    [SerializeField]
    float moveSpeed = 1f;

    [SerializeField]
    float reachDistance = 0.05f;

    [SerializeField]
    bool loop = true;

    int currentIndex;

    void Update()
    {
        if (
            waypoints == null ||
            waypoints.Length == 0
        )
        {
            return;
        }

        Transform target =
            waypoints[currentIndex];

        if (target == null)
            return;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime
            );

        Vector3 direction =
            target.position -
            transform.position;

        if (
            direction.sqrMagnitude >
            0.001f
        )
        {
            transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );
        }

        if (
            Vector3.Distance(
                transform.position,
                target.position
            ) <= reachDistance
        )
        {
            currentIndex++;

            if (
                currentIndex >=
                waypoints.Length
            )
            {
                currentIndex =
                    loop ? 0 : waypoints.Length - 1;
            }
        }
    }
}