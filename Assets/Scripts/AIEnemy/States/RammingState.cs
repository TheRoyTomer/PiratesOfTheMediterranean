using UnityEngine;

public sealed class RammingState : AIState
{
    private Vector3 interceptPoint;
    private Vector3 initialApproachDirection;
    private bool hasInterceptPoint;

    public RammingState(AIController controller) : base(controller) { }

    public void Begin(Vector3 newInterceptPoint)
    {
        interceptPoint = newInterceptPoint;
        interceptPoint.y = Controller.transform.position.y;

        Vector3 toIntercept =
            interceptPoint - Controller.transform.position;

        toIntercept.y = 0f;

        if (toIntercept.sqrMagnitude > 0.001f)
        {
            initialApproachDirection =
                toIntercept.normalized;

            hasInterceptPoint = true;
        }
        else
        {
            hasInterceptPoint = false;
        }
    }

    public override void Tick(Vector3 toTarget, float distance)
    {
        if (!hasInterceptPoint)
        {
            Controller.ChangeState(
                AIController.CombatMode.Reposition
            );

            return;
        }

        Vector3 toIntercept =
            interceptPoint - Controller.transform.position;

        toIntercept.y = 0f;

        // עברנו את מישור נקודת המפגש.
        // ה-RAM הסתיים, בין אם הייתה פגיעה ובין אם היה פספוס.
        if (Vector3.Dot(
                toIntercept,
                initialApproachDirection) <= 0f)
        {
            hasInterceptPoint = false;

            Controller.ChangeState(
                AIController.CombatMode.Reposition
            );

            return;
        }

        Vector3 desiredDirection = toIntercept;

        // כשה-AI כבר קרוב לשחקן,
        // מותר לבצע תיקון קטן ישירות לכיוון הספינה.
        if (distance <= Controller.RamTerminalAdjustDistance)
        {
            Vector3 flatToTarget = toTarget;
            flatToTarget.y = 0f;

            if (flatToTarget.sqrMagnitude > 0.001f)
            {
                float targetAngle =
                    Vector3.Angle(
                        Controller.transform.forward,
                        flatToTarget
                    );

                bool correctionSmallEnough =
                    targetAngle <=
                    Controller.RamTerminalAdjustMaxAngle;

                bool correctionBlocked =
                    Controller.ObstacleAvoidance
                        .IsDirectionBlocked(flatToTarget);

                if (correctionSmallEnough &&
                    !correctionBlocked)
                {
                    desiredDirection = flatToTarget;
                }
            }
        }

        float angle =
            Vector3.SignedAngle(
                Controller.transform.forward,
                desiredDirection,
                Vector3.up
            );

        Controller.DesiredSteering =
            Controller.CalculateSteering(
                angle,
                Controller.AlignmentTolerance,
                Controller.FullSteeringAngle
            );

        Controller.DesiredThrottle = 1f;
    }
}