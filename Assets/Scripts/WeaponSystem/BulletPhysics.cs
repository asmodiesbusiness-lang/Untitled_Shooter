using UnityEngine;

/// <summary>
/// Simple bullet physics with hard values - no animation curves
/// Bullets fly straight for a distance, then gravity kicks in
/// </summary>
public static class BulletPhysics
{
    public static bool FireBullet(
        Vector3 origin,
        Vector3 direction,
        WeaponData weaponData,
        int layerMask,
        out RaycastHit hit,
        out Vector3[] pathPoints)
    {
        float velocity = weaponData.muzzleVelocity;
        float straightDistance = weaponData.straightFlightDistance;
        float gravity = weaponData.GetEffectiveGravity();
        float maxRange = weaponData.range;
        int segments = weaponData.trajectorySegments;

        return FireBallisticRay(
            origin, direction, velocity, gravity,
            straightDistance, maxRange, segments,
            layerMask, out hit, out pathPoints
        );
    }

    public static bool FireBallisticRay(
        Vector3 origin,
        Vector3 direction,
        float velocity,
        float gravity,
        float straightFlightDistance,
        float maxRange,
        int segments,
        int layerMask,
        out RaycastHit hit,
        out Vector3[] pathPoints)
    {
        pathPoints = new Vector3[segments + 1];
        pathPoints[0] = origin;

        direction = direction.normalized;
        Vector3 currentPos = origin;
        Vector3 currentVelocity = direction * velocity;

        float distanceTraveled = 0f;
        float timeStep = maxRange / (velocity * segments);

        for (int i = 1; i <= segments; i++)
        {
            Vector3 previousPos = currentPos;

            // Only apply gravity AFTER straight flight distance
            if (distanceTraveled > straightFlightDistance)
            {
                float dropDistance = distanceTraveled - straightFlightDistance;
                float gravityRamp = Mathf.Clamp01(dropDistance / 50f);
                float effectiveGravity = gravity * gravityRamp;
                currentVelocity += Vector3.down * effectiveGravity * timeStep;
            }

            currentPos += currentVelocity * timeStep;
            distanceTraveled += (currentPos - previousPos).magnitude;
            pathPoints[i] = currentPos;

            // Raycast this segment
            Vector3 segmentDir = currentPos - previousPos;
            float segmentLength = segmentDir.magnitude;

            if (segmentLength > 0.001f)
            {
                if (Physics.Raycast(previousPos, segmentDir.normalized, out hit, segmentLength, layerMask))
                {
                    pathPoints[i] = hit.point;
                    System.Array.Resize(ref pathPoints, i + 1);
                    return true;
                }
            }

            if (distanceTraveled > maxRange || currentPos.y < origin.y - 100f)
            {
                System.Array.Resize(ref pathPoints, i + 1);
                hit = new RaycastHit();
                return false;
            }
        }

        hit = new RaycastHit();
        return false;
    }

    public static float CalculateDamage(WeaponData weaponData, float distance)
    {
        return weaponData.CalculateDamageAtDistance(distance);
    }
}