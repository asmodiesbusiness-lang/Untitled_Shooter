using UnityEngine;

/// <summary>
/// Handles ballistic trajectory calculations for realistic bullet physics
/// Uses segmented raycasts along a parabolic curve instead of straight line
/// </summary>
public class BallisticTrajectory
{
    /// <summary>
    /// Fire a ballistic raycast that follows a curved path with gravity
    /// </summary>
    public static bool FireBallisticRay(
        Vector3 origin,
        Vector3 direction,
        float velocity,
        float gravity,
        float maxDistance,
        int segments,
        int layerMask,
        out RaycastHit hit,
        out Vector3[] pathPoints)
    {
        pathPoints = new Vector3[segments + 1];
        pathPoints[0] = origin;

        Vector3 currentPoint = origin;
        Vector3 currentVelocity = direction.normalized * velocity;

        float timeStep = 0.02f; // Fixed time step for physics simulation
        float totalTime = 0f;
        float maxTime = maxDistance / velocity; // Maximum flight time

        // Safety limits
        const float MAX_DROP_DISTANCE = 500f; // If bullet drops 500m below origin, stop
        const float ABSOLUTE_MAX_TIME = 10f; // Never simulate longer than 10 seconds

        int actualSegments = 0;

        // Simulate ballistic path
        for (int i = 1; i <= segments; i++)
        {
            // Physics: velocity changes due to gravity
            Vector3 gravityAccel = Vector3.down * gravity * timeStep;
            currentVelocity += gravityAccel;

            // New position based on velocity
            Vector3 nextPoint = currentPoint + (currentVelocity * timeStep);

            // Safety check: Stop if bullet dropped too far below origin
            if (nextPoint.y < origin.y - MAX_DROP_DISTANCE)
            {
                // Bullet fell out of world
                hit = new RaycastHit();
                System.Array.Resize(ref pathPoints, i);
                return false;
            }

            pathPoints[i] = nextPoint;
            actualSegments = i;

            // Raycast between segments
            Vector3 segmentDirection = nextPoint - currentPoint;
            float segmentLength = segmentDirection.magnitude;

            if (segmentLength > 0.001f) // Avoid zero-length raycasts
            {
                if (Physics.Raycast(currentPoint, segmentDirection.normalized, out hit, segmentLength, layerMask))
                {
                    // Hit something!
                    pathPoints[i] = hit.point;
                    System.Array.Resize(ref pathPoints, i + 1); // Trim unused points
                    return true;
                }
            }

            // Check if we've traveled too far or too long
            totalTime += timeStep;
            float distanceTraveled = Vector3.Distance(origin, nextPoint);

            if (distanceTraveled > maxDistance || totalTime > maxTime || totalTime > ABSOLUTE_MAX_TIME)
            {
                hit = new RaycastHit();
                System.Array.Resize(ref pathPoints, i + 1);
                return false;
            }

            currentPoint = nextPoint;
        }

        // Reached max segments without hitting anything
        hit = new RaycastHit();
        System.Array.Resize(ref pathPoints, actualSegments + 1);
        return false;
    }

    /// <summary>
    /// Calculate damage falloff based on distance
    /// </summary>
    public static float CalculateDamageFalloff(float distance, AnimationCurve damageCurve, float baseDamage)
    {
        // Curve goes from 0 (close) to 1 (max range)
        // Returns multiplier (0-1)
        float normalizedDistance = Mathf.Clamp01(distance / 100f); // Normalize to 0-100m
        float multiplier = damageCurve.Evaluate(normalizedDistance);
        return baseDamage * multiplier;
    }

    /// <summary>
    /// Simple ballistic prediction (no raycasting) for visualization
    /// </summary>
    public static Vector3 CalculateBallisticPoint(Vector3 origin, Vector3 velocity, float gravity, float time)
    {
        return origin + (velocity * time) + (0.5f * Vector3.down * gravity * time * time);
    }

    /// <summary>
    /// Get the ballistic arc points for visualization (tracers, debug)
    /// </summary>
    public static Vector3[] GetBallisticPath(Vector3 origin, Vector3 direction, float velocity, float gravity, float maxTime, int samples)
    {
        Vector3[] points = new Vector3[samples];
        Vector3 initialVelocity = direction.normalized * velocity;

        for (int i = 0; i < samples; i++)
        {
            float t = (maxTime / samples) * i;
            points[i] = CalculateBallisticPoint(origin, initialVelocity, gravity, t);
        }

        return points;
    }
}