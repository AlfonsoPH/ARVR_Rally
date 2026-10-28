// Player.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public Transform aimTarget;
    public float speed = 3f;

    [Header("Accuracy Corner Markers")]
    public Transform opponentBackLeft;
    public Transform opponentBackRight;

    [Header("Visuals & Animation")]
    public Transform visual;
    public Animator animator;
    public string moveAnimParam = "AnimationPar";
    public float visualTurnSpeed = 720f;

    private Rigidbody rb;
    private ShotModifier shotModifier;
    bool inKitchen = false;
    private bool hitting;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        shotModifier = GetComponent<ShotModifier>();
        if (shotModifier == null)
            Debug.LogWarning("ShotModifier component missing from Player!");

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.F) && !inKitchen)
        {
            hitting = true;
        }
        else if (Input.GetKeyUp(KeyCode.F))
        {
            hitting = false;
        }

        Vector3 moveDir = new Vector3(v, 0, -h);
        bool isMoving = !hitting && moveDir.sqrMagnitude > 0.0001f;

        if (hitting)
        {
            aimTarget.Translate(moveDir * speed * Time.deltaTime, Space.World);
        }

        if (isMoving)
        {
            if (rb != null)
            {
                Vector3 newPosition = rb.position + moveDir * speed * Time.deltaTime;
                rb.MovePosition(newPosition);
            }

            if (visual != null)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                visual.rotation = Quaternion.RotateTowards(
                    visual.rotation, targetRot, visualTurnSpeed * Time.deltaTime);
            }
        }

        if (animator != null)
            animator.SetInteger(moveAnimParam, isMoving ? 1 : 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Kitchen"))
        {
            inKitchen = true;
            Debug.Log("Player entered kitchen - volleying disabled!");
        }

        if (other.CompareTag("Ball") && !inKitchen)
        {
            var paddle = PaddleDatabase.Instance != null
                ? PaddleDatabase.Instance.ActivePaddle
                : null;

            float force = paddle != null ? paddle.GetShotSpeed() : 15f;

            Vector3 dir = aimTarget.position - transform.position;
            Vector3 rawVelocity = dir.normalized * force + new Vector3(0, 6, 0);

            if (shotModifier != null && paddle != null)
                rawVelocity = shotModifier.ApplyPaddleEffects(rawVelocity, paddle);

            other.GetComponent<Rigidbody>().linearVelocity = rawVelocity;

            var bc = other.GetComponent<BallController>();
            if (bc != null) bc.MarkHit("Player");
        }
    }

    // Accuracy 10 = zero scatter, lands exactly on aim target
    // Accuracy 0  = up to 3 units of random scatter
    Vector3 ApplyAccuracyScatter(Vector3 target, PaddleData paddle)
    {
        float accuracyStat = paddle != null ? paddle.accuracy : 0f;

        // At accuracy 10: maxScatter = 0 (exact)
        // At accuracy 0:  maxScatter = 3 units
        float maxScatter = Mathf.Lerp(3f, 0f, accuracyStat / 10f);

        if (Mathf.Approximately(maxScatter, 0f))
            return target; // perfect accuracy, no scatter

        // Random offset in the horizontal plane only
        Vector2 randomCircle = Random.insideUnitCircle * maxScatter;
        Vector3 scattered = target + new Vector3(randomCircle.x, 0, randomCircle.y);

        Debug.Log($"[Accuracy] Scatter: {maxScatter:F2} units | stat: {accuracyStat}");
        return scattered;
    }

    Vector3 CalculateLaunchDirection(Vector3 origin, Vector3 target, float speed)
    {
        Vector3 toTarget = target - origin;
        float horizontalDist = new Vector3(toTarget.x, 0, toTarget.z).magnitude;
        float heightDiff = toTarget.y;
        float g = Mathf.Abs(Physics.gravity.y);
        float speedSq = speed * speed;

        float disc = speedSq * speedSq
                   - g * (g * horizontalDist * horizontalDist
                   + 2f * heightDiff * speedSq);

        float angle;
        if (disc < 0f)
        {
            angle = 35f * Mathf.Deg2Rad;
            Debug.LogWarning("[SHOT] Target out of ballistic range, using fallback angle");
        }
        else
        {
            angle = Mathf.Atan((speedSq - Mathf.Sqrt(disc)) / (g * horizontalDist));
        }

        Vector3 horizontalDir = new Vector3(toTarget.x, 0, toTarget.z).normalized;
        return horizontalDir * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Kitchen"))
        {
            inKitchen = false;
            Debug.Log("Player left kitchen - volleying enabled!");
        }
    }
}