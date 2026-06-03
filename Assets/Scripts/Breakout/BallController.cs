using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    private const float DirectionEpsilon = 0.0001f;
    private const float VelocitySqrThreshold = 0.001f;
    private const float SideWallNormalThreshold = 0.7f;
    private const float WaterDropSpawnOffset = 0.12f;
    private const float FlameTrailSpawnOffset = 0.05f;
    private const float SteamBurstDirectionOffset = 0.22f;
    private const float RollingThunderSpawnOffset = 0.12f;
    private static readonly Vector2[] SteamBurstDirections =
    {
        new Vector2(1f, 0f),
        new Vector2(1f, 1f),
        new Vector2(0f, 1f),
        new Vector2(-1f, 1f),
        new Vector2(-1f, 0f),
        new Vector2(-1f, -1f),
        new Vector2(0f, -1f),
        new Vector2(1f, -1f)
    };
    private static readonly Vector2[] WaterDropDirections =
    {
        new Vector2(1f, 1f),
        new Vector2(-1f, 1f),
        new Vector2(1f, -1f),
        new Vector2(-1f, -1f)
    };

    [Header("Type")]
    [SerializeField] private BallTypeData typeData;

    [Header("Movement")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float minimumVerticalDirection = 0.2f;

    [Header("Anti-Stuck")]
    [SerializeField] private bool ignoreOtherBallCollisions = false;
    [SerializeField] private float unstuckNudgeDistance = 0.05f;
    [SerializeField] private float axisStuckDelay = 5f;
    [SerializeField] private float wallStickRecoveryDelay = 0.15f;
    [SerializeField] private float minimumAxisSpeed = 0.05f;
    [SerializeField] private float verticalNudgeStrength = 0.25f;
    [SerializeField] private float minimumHorizontalDirection = 0.2f;
    [SerializeField] private bool logWallRecovery;

    [Header("Paddle Bounce")]
    [SerializeField] private float paddleHorizontalInfluence = 0.7f;
    [SerializeField, Range(0f, 0.95f)] private float maxPaddleHorizontalDirection = 0.6f;
    [SerializeField, Range(0f, 1f)] private float paddleTopContactNormalMinY = 0.35f;
    [SerializeField, Min(0f)] private float paddleTopSurfacePadding = 0.02f;

    [Header("Loss Rules")]
    [SerializeField] private float bottomKillY = -6f;

    [Header("3D Visual")]
    [SerializeField, Min(0.01f)] private float ballVisualRadius = 0.5f;
    [SerializeField, Min(0f)] private float ballRollSpeedMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private bool freezeMovement = false;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapSTId = Shader.PropertyToID("_BaseMap_ST");

    private Rigidbody2D rb;
    private Collider2D ballCollider;
    private MeshRenderer ballMeshRenderer;
    private Transform ballVisualTransform;
    private TrailRenderer trailRenderer;
    private Material defaultMaterial;
    private MaterialPropertyBlock propertyBlock;
    private Color currentBallColor = Color.white;
    private int animCurrentFrame;
    private float animFrameTimer;
    private float trailColorCycleTimer;
    private Vector3 baseLocalScale;
    private Vector3 typeBaseScale;
    private bool launched;
    private bool isForceStopped;
    private bool hasBeenLost;
    private bool passThroughBricks;
    private bool passThroughBalls;
    private bool destroyAfterCurrentBrickHit;
    private int remainingBrickBounces = -1;
    private float nextWaterDropAllowedTime;
    private float nextFlameTrailAllowedTime;
    private float nextFertilePatchAllowedTime;
    private float steamBurstTimeRemaining;
    private float timedEffectActivationTime;
    private float speedBoostMultiplier = 1f;
    private float blackoutTimer;
    private int firstAidHealingAccumulated;
    private float speedBoostLerpRate;
    private bool hasChainLightning;
    private float chainLightningTimeRemaining;
    private int chainLightningDamage;
    private float chainLightningRadius;
    private Color chainLightningBoltColor;
    private float chainLightningBoltWidth;
    private float chainLightningBoltLifetime;
    private int chainLightningBoltSegments;
    private float chainLightningBoltNoise;
    private bool suppressTimedSpawnEffects;
    private BallTypeData.DirectionRestraint movementRestraint;
    private BallTypeData.DirectionRestraint movementRestraintOverride;
    private bool destroyOnWallHit;
    private float cycloneCurveSign = 1f;
    private Vector2 travelDirection = Vector2.up;
    private Vector2 lastVelocity;
    private float noVerticalMovementTime;
    private float wallStickTime;
    private float rollingThunderCurrentScaleMultiplier;
    private Vector2 lastWallNormal;
    private readonly HashSet<Collider2D> brickTriggersInside = new HashSet<Collider2D>();

    public System.Action<BallController> BallLost;

    public bool IsLaunched => launched;
    public BallTypeData TypeData => typeData;
    public bool PassThroughBallsEnabled => passThroughBalls;

    public void StopMovement()
    {
        launched = false;
        isForceStopped = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ApplyLevelCompletePauseVisual(float grayscaleBlend, float alphaMultiplier)
    {
        if (ballMeshRenderer != null)
        {
            float gray = currentBallColor.grayscale;
            Color pausedColor = new Color(gray, gray, gray, currentBallColor.a * Mathf.Clamp01(alphaMultiplier));
            propertyBlock.SetColor(BaseColorId, Color.Lerp(currentBallColor, pausedColor, Mathf.Clamp01(grayscaleBlend)));
            ballMeshRenderer.SetPropertyBlock(propertyBlock);
        }

        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<Collider2D>();
        ballMeshRenderer = GetComponentInChildren<MeshRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();
        if (ballMeshRenderer != null)
        {
            defaultMaterial = ballMeshRenderer.sharedMaterial;
            ballVisualTransform = ballMeshRenderer.transform;
        }
        propertyBlock = new MaterialPropertyBlock();
        baseLocalScale = transform.localScale;
    }

    private void Start()
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ApplyTypeData();
    }

    private void Update()
    {
        if (!hasBeenLost && transform.position.y < bottomKillY)
        {
            LoseBall();
            return;
        }

        if (launched && !hasBeenLost)
        {
            if (Time.time < timedEffectActivationTime)
            {
                return;
            }

            UpdateSpeedBoost();
            TrySpawnFlameTrailOverTime();
            TrySpawnFertilePatchOverTime();
            TrySpawnSteamBurstOverTime();
            TryApplyBlackout();
            UpdateChainLightning();
        }
        if (isForceStopped)
        {
            return;
        }

        Vector2 currentVelocity = rb.linearVelocity;

        if (currentVelocity.sqrMagnitude > VelocitySqrThreshold)
        {
            travelDirection = currentVelocity.normalized;
            lastVelocity = currentVelocity;
        }
        else if (travelDirection.sqrMagnitude < VelocitySqrThreshold)
        {
            travelDirection = Vector2.up;
        }

        UpdateVerticalAxisRecovery(currentVelocity);
        ApplyCycloneCurvature();

        ApplyVelocity();
        UpdateRollingRotation();
        UpdateTextureAnimation();
        UpdateTrailColorCycle();
    }

    public void Launch(Vector2 direction)
    {
        Launch(direction, enforceMinimumVerticalDirection: true);
    }

    private void Launch(Vector2 direction, bool enforceMinimumVerticalDirection)
    {
        if (launched)
        {
            return;
        }

        launched = true;
        SetTravelDirection(direction, defaultYSign: 1f, enforceMinimumVerticalDirection);
        InitializeRollingThunderCycle();
        ScheduleTimedSpawnEffects();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount == 0)
        {
            return;
        }

        if (TryIgnoreOtherBallCollision(collision))
        {
            return;
        }

        if (ShouldIgnoreBrickCollision(collision))
        {
            return;
        }

        if (collision.gameObject.CompareTag("Paddle"))
        {
            BreakoutSoundController.PlayPaddleHitSfx();

            if (TryHandleTaggedCollisionEffects(collision.gameObject, Vector2.zero))
            {
                return;
            }

            ContactPoint2D paddleContact = collision.GetContact(0);
            ApplyPaddleBounce(paddleContact.collider.bounds, paddleContact.normal);
            return;
        }

        ContactPoint2D contact = collision.GetContact(0);
        bool hitWall = collision.gameObject.CompareTag("SideWall") || collision.gameObject.CompareTag("TopWall");
        if (hitWall)
        {
            BreakoutSoundController.PlayWallHitSfx();
        }

        Vector2 sideWallDirection = contact.normal.x >= 0f ? Vector2.right : Vector2.left;
        if (TryHandleTaggedCollisionEffects(collision.gameObject, sideWallDirection))
        {
            return;
        }

        UpdateWallStickState(contact.normal, collision.gameObject);
        ReflectAndSetDirection(contact.normal);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.contactCount == 0)
        {
            return;
        }

        if (!IsEligibleForWallRecovery(collision.gameObject))
        {
            return;
        }

        ContactPoint2D contact = collision.GetContact(0);
        if (!IsSideWall(contact.normal))
        {
            return;
        }

        bool isVerticallyLocked = Mathf.Abs(rb.linearVelocity.x) <= minimumAxisSpeed;
        if (!isVerticallyLocked)
        {
            wallStickTime = 0f;
            return;
        }

        wallStickTime += Time.fixedDeltaTime;
        lastWallNormal = contact.normal;

        if (wallStickTime >= wallStickRecoveryDelay)
        {
            RecoverFromWallStick(lastWallNormal);
            wallStickTime = 0f;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        wallStickTime = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BottomBoundary"))
        {
            LoseBall();
            return;
        }

        if (passThroughBricks && other.isTrigger && !other.CompareTag("Paddle") && !other.CompareTag("SideWall") && !other.CompareTag("TopWall"))
        {
            return;
        }

        if (passThroughBricks && other.TryGetComponent<BrickController>(out BrickController brick))
        {
            if (brickTriggersInside.Add(other))
            {
                brick.ApplyBallHit(this);
            }

            return;
        }

        if (passThroughBricks && other.TryGetComponent<BallController>(out _))
        {
            return;
        }

        if (passThroughBricks)
        {
            if (other.CompareTag("Paddle"))
            {
                BreakoutSoundController.PlayPaddleHitSfx();
            }
            else if (other.CompareTag("SideWall") || other.CompareTag("TopWall"))
            {
                BreakoutSoundController.PlayWallHitSfx();
            }

            Vector2 sideWallDirection = transform.position.x >= other.bounds.center.x ? Vector2.right : Vector2.left;
            if (TryHandleTaggedCollisionEffects(other.gameObject, sideWallDirection))
            {
                return;
            }

            BounceOffTriggerCollider(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        brickTriggersInside.Remove(other);
    }

    private void LoseBall()
    {
        if (hasBeenLost)
        {
            return;
        }

        hasBeenLost = true;
        BallLost?.Invoke(this);
        Destroy(gameObject);
    }

    private void SetTravelDirection(Vector2 direction, float defaultYSign, bool enforceMinimumVerticalDirection = true)
    {
        travelDirection = NormalizeDirection(direction, defaultYSign, enforceMinimumVerticalDirection);
        ApplyVelocity();
    }

    private Vector2 NormalizeDirection(Vector2 direction, float defaultYSign, bool enforceMinimumVerticalDirection = true)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude > DirectionEpsilon ? direction.normalized : Vector2.up;

        if (enforceMinimumVerticalDirection && Mathf.Abs(normalizedDirection.y) < minimumVerticalDirection)
        {
            float ySign = Mathf.Sign(normalizedDirection.y == 0f ? defaultYSign : normalizedDirection.y);
            if (Mathf.Approximately(ySign, 0f))
            {
                ySign = 1f;
            }

            normalizedDirection.y = ySign * minimumVerticalDirection;
            normalizedDirection.Normalize();
        }

        return normalizedDirection;
    }

    private void ApplyVelocity()
    {
        if (freezeMovement)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = travelDirection;
        if (movementRestraint == BallTypeData.DirectionRestraint.HorizontalOnly)
        {
            dir.y = 0f;
            if (Mathf.Abs(dir.x) < DirectionEpsilon)
            {
                dir.x = lastVelocity.x >= 0f ? 1f : -1f;
            }
            dir = dir.normalized;
        }
        else if (movementRestraint == BallTypeData.DirectionRestraint.VerticalOnly)
        {
            dir.x = 0f;
            if (Mathf.Abs(dir.y) < DirectionEpsilon)
            {
                dir.y = 1f;
            }
            dir = dir.normalized;
        }
        rb.linearVelocity = dir * GetCurrentSpeed();
        lastVelocity = rb.linearVelocity;
    }

    private float GetCurrentSpeed()
    {
        return Mathf.Max(0f, speed) * Mathf.Max(1f, speedBoostMultiplier);
    }

    private void UpdateRollingRotation()
    {
        if (ballVisualTransform == null)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude < VelocitySqrThreshold)
        {
            return;
        }

        Vector2 dir = velocity.normalized;
        float effectiveRadius = Mathf.Max(0.01f, ballVisualRadius * transform.localScale.x);
        Vector3 rollingAxis = new Vector3(dir.y, -dir.x, 0f);
        float typeSpeed = typeData != null ? typeData.MovementSpeed : 1f;
        float degreesPerSecond = (velocity.magnitude / effectiveRadius) * Mathf.Rad2Deg * ballRollSpeedMultiplier * typeSpeed;
        ballVisualTransform.Rotate(rollingAxis, degreesPerSecond * Time.deltaTime, Space.World);
    }

    private void ApplyTrailColor(Color color)
    {
        if (trailRenderer == null) return;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trailRenderer.colorGradient = gradient;
    }

    private void UpdateTextureAnimation()
    {
        if (ballMeshRenderer == null || typeData == null || !typeData.AnimateTexture)
        {
            return;
        }

        int columns = Mathf.Max(1, typeData.AnimFrameColumns);
        int rows = Mathf.Max(1, typeData.AnimFrameRows);
        int totalFrames = columns * rows;

        animFrameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(0.01f, typeData.AnimFrameRate);
        if (animFrameTimer >= frameDuration)
        {
            animFrameTimer -= frameDuration;
            animCurrentFrame = (animCurrentFrame + 1) % totalFrames;
        }

        float tileX = 1f / columns;
        float tileY = 1f / rows;
        int col = animCurrentFrame % columns;
        int row = animCurrentFrame / columns;
        float offsetX = col * tileX;
        float offsetY = 1f - (row + 1) * tileY;

        propertyBlock.SetVector(BaseMapSTId, new Vector4(tileX, tileY, offsetX, offsetY));
        ballMeshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void UpdateTrailColorCycle()
    {
        if (typeData == null || typeData.TrailColorSampling != BallTypeData.TrailColorMode.ManualCycle) return;
        Color[] colors = typeData.TrailColors;
        if (colors == null || colors.Length < 2) return;
        trailColorCycleTimer = (trailColorCycleTimer + Time.deltaTime * typeData.TrailColorCycleRate) % colors.Length;
        int fromIndex = Mathf.FloorToInt(trailColorCycleTimer);
        int toIndex = (fromIndex + 1) % colors.Length;
        ApplyTrailColor(Color.Lerp(colors[fromIndex], colors[toIndex], trailColorCycleTimer - fromIndex));
    }

    private void UpdateSpeedBoost()
    {
        if (speedBoostMultiplier <= 1f)
        {
            speedBoostMultiplier = 1f;
            speedBoostLerpRate = 0f;
            return;
        }

        if (speedBoostLerpRate <= 0f)
        {
            speedBoostMultiplier = 1f;
            return;
        }

        speedBoostMultiplier = Mathf.MoveTowards(speedBoostMultiplier, 1f, speedBoostLerpRate * Time.deltaTime);
    }

    private void ApplyTemporarySpeedBoost(float peakMultiplier, float lerpDuration)
    {
        float clampedPeakMultiplier = Mathf.Max(1f, peakMultiplier);
        float clampedDuration = Mathf.Max(0.01f, lerpDuration);
        speedBoostMultiplier = Mathf.Max(speedBoostMultiplier, clampedPeakMultiplier);
        speedBoostLerpRate = (speedBoostMultiplier - 1f) / clampedDuration;
    }

    public void SetTimedSpawnEffectsSuppressed(bool suppressed)
    {
        suppressTimedSpawnEffects = suppressed;
        if (suppressTimedSpawnEffects)
        {
            steamBurstTimeRemaining = 0f;
        }
    }

    public void ApplyChainLightning(float duration, int damage, float radius,
        Color boltColor, float boltWidth, float boltLifetime, int boltSegments, float boltNoise)
    {
        float clampedDuration = Mathf.Max(0.01f, duration);
        chainLightningTimeRemaining = Mathf.Max(chainLightningTimeRemaining, clampedDuration);
        chainLightningDamage = Mathf.Max(1, damage);
        chainLightningRadius = Mathf.Max(0.1f, radius);
        chainLightningBoltColor = boltColor;
        chainLightningBoltWidth = Mathf.Max(0.005f, boltWidth);
        chainLightningBoltLifetime = Mathf.Max(0.05f, boltLifetime);
        chainLightningBoltSegments = Mathf.Max(2, boltSegments);
        chainLightningBoltNoise = Mathf.Max(0f, boltNoise);
        hasChainLightning = true;
    }

    public void TryFireChainLightning(BrickController sourceBrick)
    {
        if (!hasChainLightning || sourceBrick == null)
        {
            return;
        }

        float radiusSqr = chainLightningRadius * chainLightningRadius;
        Vector3 origin = sourceBrick.transform.position;
        BrickController target = null;
        int candidateCount = 0;

        BrickController[] allBricks = Object.FindObjectsByType<BrickController>(FindObjectsSortMode.None);
        for (int i = 0; i < allBricks.Length; i++)
        {
            BrickController candidate = allBricks[i];
            if (candidate == sourceBrick || candidate.CurrentHitPoints <= 0)
            {
                continue;
            }
            if ((candidate.transform.position - origin).sqrMagnitude <= radiusSqr)
            {
                candidateCount++;
                if (Random.Range(0, candidateCount) == 0)
                {
                    target = candidate;
                }
            }
        }

        if (target == null)
        {
            return;
        }

        target.ApplyDirectEffectDamage(chainLightningDamage);
        LightningBoltEffect.Spawn(
            origin,
            target.transform.position,
            chainLightningBoltColor,
            chainLightningBoltWidth,
            chainLightningBoltLifetime,
            chainLightningBoltSegments,
            chainLightningBoltNoise);
    }

    private void UpdateChainLightning()
    {
        if (!hasChainLightning)
        {
            return;
        }

        chainLightningTimeRemaining -= Time.deltaTime;
        if (chainLightningTimeRemaining <= 0f)
        {
            hasChainLightning = false;
        }
    }

    public void ResetSpawnedRuntimeState()
    {
        launched = false;
        hasBeenLost = false;
        destroyAfterCurrentBrickHit = false;
        remainingBrickBounces = -1;
        nextWaterDropAllowedTime = 0f;
        nextFlameTrailAllowedTime = 0f;
        nextFertilePatchAllowedTime = 0f;
        steamBurstTimeRemaining = 0f;
        timedEffectActivationTime = 0f;
        blackoutTimer = 0f;
        firstAidHealingAccumulated = 0;
        speedBoostMultiplier = 1f;
        speedBoostLerpRate = 0f;
        hasChainLightning = false;
        chainLightningTimeRemaining = 0f;
        travelDirection = Vector2.up;
        lastVelocity = Vector2.zero;
        noVerticalMovementTime = 0f;
        wallStickTime = 0f;
        rollingThunderCurrentScaleMultiplier = 1f;
        lastWallNormal = Vector2.zero;
        movementRestraintOverride = BallTypeData.DirectionRestraint.None;
        brickTriggersInside.Clear();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ScheduleTimedSpawnEffects()
    {
        if (suppressTimedSpawnEffects || typeData == null)
        {
            nextFlameTrailAllowedTime = 0f;
            nextFertilePatchAllowedTime = 0f;
            steamBurstTimeRemaining = 0f;
            timedEffectActivationTime = 0f;
            speedBoostMultiplier = 1f;
            speedBoostLerpRate = 0f;
            return;
        }

        timedEffectActivationTime = Time.time + Mathf.Max(0f, typeData.TimedEffectInitialDelay);

        if (typeData.CreatesFlameTrail)
        {
            nextFlameTrailAllowedTime = timedEffectActivationTime + Mathf.Max(0.01f, typeData.FlameTrailSpawnInterval);
        }
        else
        {
            nextFlameTrailAllowedTime = 0f;
        }

        if (typeData.CreatesFertileLand)
        {
            nextFertilePatchAllowedTime = timedEffectActivationTime + Mathf.Max(0.01f, typeData.FertilePatchSpawnInterval);
        }
        else
        {
            nextFertilePatchAllowedTime = 0f;
        }

        if (typeData.CreatesSteamBurst)
        {
            steamBurstTimeRemaining = Mathf.Max(0f, timedEffectActivationTime - Time.time) + GetRandomSteamBurstInterval();
        }
        else
        {
            steamBurstTimeRemaining = 0f;
        }
    }

    private bool TryHandleTaggedCollisionEffects(GameObject hitObject, Vector2 sideWallDirection)
    {
        if (hitObject == null)
        {
            return false;
        }

        if (hitObject.CompareTag("Paddle"))
        {
            TrySpawnLinearProjectile(Vector2.up);
            TryApplyShockTherapyOnWallHit(hitObject);
            return TryDestroyOnWallHit();
        }

        if (hitObject.CompareTag("SideWall"))
        {
            TrySpawnLinearProjectile(sideWallDirection);
            TryApplyShockTherapyOnWallHit(hitObject);
            return TryDestroyOnWallHit();
        }

        if (hitObject.CompareTag("TopWall"))
        {
            if (typeData != null && typeData.LinearProjectileIncludesTopWall)
            {
                TrySpawnLinearProjectile(Vector2.down);
            }

            TryApplyShockTherapyOnWallHit(hitObject);
            return TryDestroyOnWallHit();
        }

        return TryDestroyOnWallHit();
    }

    private bool TryDestroyOnWallHit()
    {
        if (!destroyOnWallHit)
        {
            return false;
        }

        Destroy(gameObject);
        return true;
    }

    private void UpdateVerticalAxisRecovery(Vector2 currentVelocity)
    {
        if (movementRestraint != BallTypeData.DirectionRestraint.None)
        {
            return;
        }

        if (Mathf.Abs(currentVelocity.y) <= minimumAxisSpeed)
        {
            noVerticalMovementTime += Time.fixedDeltaTime;
        }
        else
        {
            noVerticalMovementTime = 0f;
        }

        if (noVerticalMovementTime >= axisStuckDelay)
        {
            ApplyVerticalNudge();
            noVerticalMovementTime = 0f;
        }
    }

    private void ApplyVerticalNudge()
    {
        float verticalSign = Mathf.Sign(travelDirection.y);
        if (Mathf.Approximately(verticalSign, 0f))
        {
            verticalSign = Random.value < 0.5f ? -1f : 1f;
        }

        Vector2 nudgedDirection = new Vector2(travelDirection.x, travelDirection.y + verticalSign * verticalNudgeStrength);
        SetTravelDirection(nudgedDirection, verticalSign);
    }

    private void ApplyCycloneCurvature()
    {
        if (!launched || hasBeenLost || typeData == null || !typeData.CreatesCyclone)
        {
            return;
        }

        if (movementRestraint != BallTypeData.DirectionRestraint.None)
        {
            return;
        }

        float curveStrength = Mathf.Max(0f, typeData.CycloneCurveStrength);
        if (curveStrength <= 0f)
        {
            return;
        }

        float rotationDegrees = curveStrength * cycloneCurveSign * Time.deltaTime;
        Vector2 curvedDirection = (Vector2)(Quaternion.Euler(0f, 0f, rotationDegrees) * travelDirection);
        float ySign = Mathf.Sign(curvedDirection.y);
        if (Mathf.Approximately(ySign, 0f))
        {
            ySign = Mathf.Sign(travelDirection.y);
            if (Mathf.Approximately(ySign, 0f))
            {
                ySign = 1f;
            }
        }

        travelDirection = NormalizeDirection(curvedDirection, ySign, enforceMinimumVerticalDirection: true);
    }

    public void SetTypeData(BallTypeData newTypeData)
    {
        typeData = newTypeData;
        ApplyTypeData();
    }

    private void ApplyTypeData()
    {
        if (typeData == null)
        {
            passThroughBricks = false;
            passThroughBalls = false;
            cycloneCurveSign = 1f;
            if (ballCollider != null)
            {
                ballCollider.isTrigger = false;
            }

            typeBaseScale = baseLocalScale;
            transform.localScale = typeBaseScale;

            if (ballMeshRenderer != null)
            {
                ballMeshRenderer.material = defaultMaterial;
                currentBallColor = Color.white;
                propertyBlock.Clear();
                ballMeshRenderer.SetPropertyBlock(propertyBlock);
            }

            animCurrentFrame = 0;
            animFrameTimer = 0f;
            trailColorCycleTimer = 0f;
            brickTriggersInside.Clear();
            movementRestraint = movementRestraintOverride;
            destroyOnWallHit = false;
            return;
        }

        speed = Mathf.Max(0f, typeData.MovementSpeed);
        passThroughBricks = typeData.PassThroughBricks;
        passThroughBalls = typeData.PassThroughBalls;
        cycloneCurveSign = Random.value < 0.5f ? -1f : 1f;
        movementRestraint = movementRestraintOverride != BallTypeData.DirectionRestraint.None
            ? movementRestraintOverride
            : typeData.MovementRestraint;
        destroyOnWallHit = typeData.DestroyOnWall;
        remainingBrickBounces = typeData.Bounces;
        nextWaterDropAllowedTime = 0f;
        nextFlameTrailAllowedTime = 0f;
        nextFertilePatchAllowedTime = 0f;
        steamBurstTimeRemaining = 0f;
        blackoutTimer = typeData.BlackoutInterval;
        firstAidHealingAccumulated = 0;
        animCurrentFrame = 0;
        animFrameTimer = 0f;
        trailColorCycleTimer = 0f;

        if (ballCollider != null)
        {
            ballCollider.isTrigger = passThroughBricks;
        }

        brickTriggersInside.Clear();

        if (ballMeshRenderer != null)
        {
            if (typeData.BallMaterial != null)
            {
                ballMeshRenderer.material = typeData.BallMaterial;
            }
            Material activeMaterial = typeData.BallMaterial != null ? typeData.BallMaterial : defaultMaterial;
            float alpha = activeMaterial != null && activeMaterial.HasProperty(BaseColorId)
                ? activeMaterial.GetColor(BaseColorId).a : 1f;
            currentBallColor = new Color(1f, 1f, 1f, alpha);
            propertyBlock.SetColor(BaseColorId, currentBallColor);
            propertyBlock.SetVector(BaseMapSTId, new Vector4(1f, 1f, 0f, 0f));
            ballMeshRenderer.SetPropertyBlock(propertyBlock);
        }

        Color resolvedTrailColor = (typeData.TrailColorSampling == BallTypeData.TrailColorMode.ManualCycle
            && typeData.TrailColors != null && typeData.TrailColors.Length > 0)
            ? typeData.TrailColors[0]
            : typeData.TrailColor;
        if (ballMeshRenderer != null)
        {
            currentBallColor = new Color(resolvedTrailColor.r, resolvedTrailColor.g, resolvedTrailColor.b, currentBallColor.a);
            propertyBlock.SetColor(BaseColorId, currentBallColor);
            ballMeshRenderer.SetPropertyBlock(propertyBlock);
        }
        ApplyTrailColor(resolvedTrailColor);

        float sizeMultiplier = Mathf.Clamp(typeData.Size, 0.25f, 3f);
        typeBaseScale = baseLocalScale * sizeMultiplier;
        transform.localScale = typeBaseScale;
        InitializeRollingThunderCycle();
    }

    private void BounceOffTriggerCollider(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (other.CompareTag("Paddle"))
        {
            Vector2 approximateNormal = ((Vector2)transform.position - (Vector2)other.bounds.center).normalized;
            ApplyPaddleBounce(other.bounds, approximateNormal);
            return;
        }

        Vector2 closestPoint = other.ClosestPoint(transform.position);
        Vector2 normal = (Vector2)transform.position - closestPoint;

        if (normal.sqrMagnitude < 0.0001f)
        {
            Vector2 centerDelta = (Vector2)transform.position - (Vector2)other.bounds.center;
            if (Mathf.Abs(centerDelta.x) > Mathf.Abs(centerDelta.y))
            {
                normal = new Vector2(Mathf.Sign(centerDelta.x), 0f);
            }
            else
            {
                normal = new Vector2(0f, Mathf.Sign(centerDelta.y));
            }
        }

        ReflectAndSetDirection(normal);
    }

    private void ApplyPaddleBounce(Bounds paddleBounds, Vector2 contactNormal)
    {
        if (!IsTopPaddleContact(paddleBounds, contactNormal))
        {
            // Side/bottom paddle contacts should not redirect ball sideways.
            // Push ball above paddle and send it upward.
            float yAbovePaddle = paddleBounds.max.y + 0.01f;
            if (ballCollider != null)
            {
                yAbovePaddle += ballCollider.bounds.extents.y;
            }

            rb.position = new Vector2(rb.position.x, yAbovePaddle);
            SetTravelDirection(Vector2.up, defaultYSign: 1f);
            return;
        }

        float halfWidth = Mathf.Max(paddleBounds.extents.x, 0.01f);
        float offset = transform.position.x - paddleBounds.center.x;
        float normalizedOffset = Mathf.Clamp(offset / halfWidth, -1f, 1f);
        float horizontal = normalizedOffset * paddleHorizontalInfluence;
        horizontal = Mathf.Clamp(horizontal, -maxPaddleHorizontalDirection, maxPaddleHorizontalDirection);
        Vector2 bounceDirection = new Vector2(horizontal, 1f);

        if (Mathf.Abs(bounceDirection.y) < minimumVerticalDirection)
        {
            bounceDirection.y = minimumVerticalDirection;
        }

        SetTravelDirection(bounceDirection, defaultYSign: 1f);
    }

    private bool IsTopPaddleContact(Bounds paddleBounds, Vector2 contactNormal)
    {
        bool normalIndicatesTopHit = contactNormal.y >= paddleTopContactNormalMinY;
        bool ballIsAtOrAboveTopSurface = transform.position.y >= paddleBounds.max.y - paddleTopSurfacePadding;
        return normalIndicatesTopHit || ballIsAtOrAboveTopSurface;
    }

    private void ApplyWallEscapeBias(ref Vector2 reflectedDirection, Vector2 surfaceNormal)
    {
        if (!IsSideWall(surfaceNormal))
        {
            return;
        }

        float awayFromWallSign = Mathf.Sign(surfaceNormal.x);
        if (Mathf.Abs(reflectedDirection.x) < minimumHorizontalDirection)
        {
            reflectedDirection.x = awayFromWallSign * minimumHorizontalDirection;
            reflectedDirection.Normalize();
        }
    }

    private void UpdateWallStickState(Vector2 normal, GameObject collisionObject)
    {
        if (!IsEligibleForWallRecovery(collisionObject) || !IsSideWall(normal))
        {
            wallStickTime = 0f;
            return;
        }

        lastWallNormal = normal;
    }

    private void RecoverFromWallStick(Vector2 wallNormal)
    {
        float ySign = Mathf.Sign(travelDirection.y);
        if (Mathf.Approximately(ySign, 0f))
        {
            ySign = 1f;
        }

        Vector2 escapeDirection = new Vector2(Mathf.Sign(wallNormal.x) * minimumHorizontalDirection, ySign);
        rb.position += wallNormal.normalized * unstuckNudgeDistance;
        SetTravelDirection(escapeDirection, ySign);

        if (logWallRecovery)
        {
            Debug.Log($"Wall recovery applied on {name}. normal={wallNormal}, dir={escapeDirection.normalized}", this);
        }
    }

    private bool IsEligibleForWallRecovery(GameObject collisionObject)
    {
        if (collisionObject == null)
        {
            return false;
        }

        if (collisionObject.CompareTag("Paddle"))
        {
            return false;
        }

        if (collisionObject.TryGetComponent<BrickController>(out _))
        {
            return false;
        }

        if (collisionObject.TryGetComponent<BallController>(out _))
        {
            return false;
        }

        return true;
    }

    private bool TryIgnoreOtherBallCollision(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent<BallController>(out BallController otherBall))
        {
            return false;
        }

        if (!ignoreOtherBallCollisions && !passThroughBalls && !otherBall.PassThroughBallsEnabled)
        {
            return false;
        }

        if (ballCollider != null)
        {
            Physics2D.IgnoreCollision(ballCollider, collision.collider, true);
        }

        return true;
    }

    private bool ShouldIgnoreBrickCollision(Collision2D collision)
    {
        if (!passThroughBricks || !collision.gameObject.TryGetComponent<BrickController>(out _))
        {
            return false;
        }

        if (ballCollider != null)
        {
            Physics2D.IgnoreCollision(ballCollider, collision.collider, true);
        }

        return true;
    }

    private void ReflectAndSetDirection(Vector2 normal)
    {
        Vector2 surfaceNormal = normal.sqrMagnitude > DirectionEpsilon ? normal.normalized : Vector2.up;
        Vector2 reflected = Vector2.Reflect(GetIncomingDirection(), surfaceNormal);
        ApplyWallEscapeBias(ref reflected, surfaceNormal);

        if (Mathf.Abs(reflected.y) < minimumVerticalDirection)
        {
            float ySign = Mathf.Sign(reflected.y == 0f ? -surfaceNormal.y : reflected.y);
            reflected.y = ySign * minimumVerticalDirection;
        }

        SetTravelDirection(reflected, defaultYSign: -surfaceNormal.y);
    }

    private Vector2 GetIncomingDirection()
    {
        return lastVelocity.sqrMagnitude > VelocitySqrThreshold ? lastVelocity.normalized : travelDirection;
    }

    private bool IsSideWall(Vector2 normal)
    {
        return Mathf.Abs(normal.x) > SideWallNormalThreshold;
    }

    public bool ConsumeBrickBounce()
    {
        if (hasBeenLost)
        {
            return false;
        }

        if (remainingBrickBounces < 0)
        {
            return true;
        }

        if (remainingBrickBounces <= 0)
        {
            destroyAfterCurrentBrickHit = true;
            return true;
        }

        remainingBrickBounces--;
        return true;
    }

    public void FinalizeBrickHit()
    {
        if (!destroyAfterCurrentBrickHit || hasBeenLost)
        {
            return;
        }

        destroyAfterCurrentBrickHit = false;
        LoseBall();
    }

    public void TrySpawnWaterDropsFromBrickHit()
    {
        if (typeData == null || !typeData.CreatesWaterDrops || typeData.WaterDropletType == null)
        {
            return;
        }

        if (Time.time < nextWaterDropAllowedTime)
        {
            return;
        }

        nextWaterDropAllowedTime = Time.time + typeData.WaterDropCooldown;

        for (int i = 0; i < WaterDropDirections.Length; i++)
        {
            Vector2 direction = WaterDropDirections[i].normalized;
            Vector3 spawnPosition = transform.position + (Vector3)(direction * WaterDropSpawnOffset);
            BallController spawnedBall = Instantiate(this, spawnPosition, Quaternion.identity);
            spawnedBall.ResetSpawnedRuntimeState();
            spawnedBall.SetTypeData(typeData.WaterDropletType);

            Collider2D spawnedCollider = spawnedBall.GetComponent<Collider2D>();
            if (ballCollider != null && spawnedCollider != null)
            {
                Physics2D.IgnoreCollision(ballCollider, spawnedCollider, true);
            }
            spawnedBall.Launch(direction);
        }
    }

    public void RegisterRollingThunderBrickHit()
    {
        if (!launched || hasBeenLost)
        {
            return;
        }

        TryAdvanceRollingThunderFromBrickHit();
    }

    private void TrySpawnSteamBurstOverTime()
    {
        if (typeData == null || !typeData.CreatesSteamBurst || suppressTimedSpawnEffects)
        {
            return;
        }

        steamBurstTimeRemaining -= Time.deltaTime;
        if (steamBurstTimeRemaining > 0f)
        {
            return;
        }

        SpawnSteamBurst();
        steamBurstTimeRemaining = GetRandomSteamBurstInterval();
    }

    private void TryApplyBlackout()
    {
        if (typeData == null || !typeData.CreatesBlackout)
        {
            return;
        }

        blackoutTimer -= Time.deltaTime;
        if (blackoutTimer > 0f)
        {
            return;
        }

        blackoutTimer = typeData.BlackoutInterval;

        BrickController[] allBricks = Object.FindObjectsByType<BrickController>(FindObjectsSortMode.None);
        foreach (BrickController brick in allBricks)
        {
            brick.ApplyDirectEffectDamage(typeData.BlackoutDamage);
            LightningBoltEffect.SpawnBlackout(transform.position, brick.transform.position, typeData);
        }
    }

    public void NotifyFirstAidBrickHit()
    {
        if (typeData == null || !typeData.CreatesFirstAid || hasBeenLost) return;
        if (typeData.FirstAidHealPerHit > 0 && PlayerStats.HasInstance)
            PlayerStats.Instance.Heal(typeData.FirstAidHealPerHit);
        firstAidHealingAccumulated += typeData.FirstAidHealPerHit;
        if (firstAidHealingAccumulated >= typeData.FirstAidHealThreshold)
        {
            firstAidHealingAccumulated = 0;
            TriggerFirstAidExplosion();
        }
    }

    private void TriggerFirstAidExplosion()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, typeData.FirstAidExplosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<BrickController>(out BrickController brick))
                brick.ApplyDirectEffectDamage(typeData.FirstAidExplosionDamage);
        }
    }

    private void TryApplyShockTherapyOnWallHit(GameObject wallObject)
    {
        if (wallObject == null || typeData == null || !typeData.CreatesShockTherapy)
        {
            return;
        }

        bool isShockWall = wallObject.CompareTag("SideWall") || wallObject.CompareTag("TopWall") || wallObject.CompareTag("Paddle");
        if (!isShockWall)
        {
            return;
        }

        BrickController[] allBricks = Object.FindObjectsByType<BrickController>(FindObjectsSortMode.None);
        if (allBricks != null && allBricks.Length > 0)
        {
            int minTargets = Mathf.Max(1, typeData.ShockTherapyMinTargets);
            int maxTargets = Mathf.Max(minTargets, typeData.ShockTherapyMaxTargets);
            int randomTargetCount = Random.Range(minTargets, maxTargets + 1);
            int shocksToApply = Mathf.Min(randomTargetCount, allBricks.Length);
            int shockDamage = Mathf.Max(1, typeData.ShockTherapyDamage);

            for (int i = 0; i < shocksToApply; i++)
            {
                int randomIndex = Random.Range(i, allBricks.Length);
                BrickController selectedBrick = allBricks[randomIndex];
                allBricks[randomIndex] = allBricks[i];
                allBricks[i] = selectedBrick;

                if (selectedBrick != null)
                {
                    selectedBrick.ApplyDirectEffectDamage(shockDamage);
                    LightningBoltEffect.SpawnShockTherapy(transform.position, selectedBrick.transform.position, typeData);
                }
            }
        }

        int healAmount = Mathf.Max(0, typeData.ShockTherapyHealAmount);
        if (healAmount > 0 && PlayerStats.HasInstance)
        {
            PlayerStats.Instance.Heal(healAmount);
        }
    }

    private void SpawnSteamBurst()
    {
        BallTypeData burstBallType = typeData != null ? typeData.SteamBurstBallType : null;
        if (burstBallType == null)
        {
            return;
        }

        int burstCount = Mathf.Max(2, typeData.SteamBurstBallCount);
        float spawnRadius = Mathf.Max(0.01f, typeData.SteamBurstSpawnRadius);

        ApplyTemporarySpeedBoost(typeData.SteamBurstSpeedMultiplier, typeData.SteamBurstSpeedLerpDuration);

        for (int i = 0; i < burstCount; i++)
        {
            Vector2 direction = SteamBurstDirections[i % SteamBurstDirections.Length].normalized;
            Vector3 spawnPosition = transform.position + (Vector3)(direction * spawnRadius);
            SpawnBallCopy(burstBallType, spawnPosition, direction);
        }

        ReverseHorizontalTravelDirection();
    }

    // Called by BrickController when a pressure burst spawns droplets.
    public BallController SpawnDropletAt(BallTypeData spawnedTypeData, Vector3 worldPosition, Vector2 launchDirection)
    {
        return SpawnBallCopy(spawnedTypeData, worldPosition, launchDirection);
    }

    public void SetMovementRestraint(BallTypeData.DirectionRestraint restraint)
    {
        movementRestraint = restraint;
        movementRestraintOverride = restraint;
    }

    private void TrySpawnLinearProjectile(Vector2 launchDirection)
    {
        if (typeData == null || !typeData.CreatesLinearProjectile || hasBeenLost)
        {
            return;
        }

        const float WaveSpawnOffset = 0.18f;
        Vector2 normalizedDirection = launchDirection.sqrMagnitude > DirectionEpsilon ? launchDirection.normalized : Vector2.up;
        Vector3 spawnPosition = transform.position + (Vector3)(normalizedDirection * WaveSpawnOffset);
        LinearProjectileEntity.Spawn(
            spawnPosition,
            normalizedDirection,
            ballCollider,
            typeData.LinearProjectileSprite,
            typeData.LinearProjectileAnimSprites,
            typeData.LinearProjectileAnimFrameRate,
            typeData.LinearProjectileColor,
            typeData.LinearProjectileSize,
            typeData.LinearProjectileSpeed,
            typeData.LinearProjectileDamage,
            typeData.LinearProjectileAppliesBurn,
            typeData.LinearProjectileBurnDamage,
            typeData.LinearProjectileBurnTickInterval,
            typeData.LinearProjectileBurnHitCount,
            typeData.LinearProjectileAppliesCrack,
            typeData.LinearProjectileCrackShatterDamage,
            typeData.LinearProjectileCrackShatterRadius,
            typeData.LinearProjectileAppliesRoot,
            typeData.LinearProjectileRootDuration,
            typeData.LinearProjectileRootSpeedMultiplier,
            typeData.LinearProjectileHitsBeforeDestroy);
    }

    private BallController SpawnBallCopy(BallTypeData spawnedTypeData, Vector3 spawnPosition, Vector2 launchDirection, bool enforceMinimumVerticalDirection = true)
    {
        if (spawnedTypeData == null)
        {
            return null;
        }

        BallController spawnedBall = Instantiate(this, spawnPosition, Quaternion.identity);
        spawnedBall.ResetSpawnedRuntimeState();
        spawnedBall.SetTypeData(spawnedTypeData);
        // Always size relative to base scale 1, never inheriting the spawning ball's current scale.
        spawnedBall.baseLocalScale = Vector3.one;
        float spawnedSize = Mathf.Clamp(spawnedTypeData.Size, 0.25f, 3f);
        spawnedBall.typeBaseScale = new Vector3(spawnedSize, spawnedSize, spawnedSize);
        spawnedBall.transform.localScale = spawnedBall.typeBaseScale;

        Collider2D spawnedCollider = spawnedBall.GetComponent<Collider2D>();
        if (ballCollider != null && spawnedCollider != null)
        {
            Physics2D.IgnoreCollision(ballCollider, spawnedCollider, true);
        }

        spawnedBall.Launch(launchDirection, enforceMinimumVerticalDirection);
        return spawnedBall;
    }

    private void ReverseHorizontalTravelDirection()
    {
        float ySign = Mathf.Sign(travelDirection.y == 0f ? 1f : travelDirection.y);
        float reversedX = -travelDirection.x;

        if (Mathf.Abs(reversedX) < minimumHorizontalDirection)
        {
            reversedX = (travelDirection.x >= 0f ? -1f : 1f) * minimumHorizontalDirection;
        }

        Vector2 redirected = new Vector2(reversedX, travelDirection.y);
        SetTravelDirection(redirected, ySign);
    }

    private float GetRandomSteamBurstInterval()
    {
        if (typeData == null)
        {
            return 0f;
        }

        float minInterval = Mathf.Max(0.1f, Mathf.Min(typeData.SteamBurstMinInterval, typeData.SteamBurstMaxInterval));
        float maxInterval = Mathf.Max(minInterval, Mathf.Max(typeData.SteamBurstMinInterval, typeData.SteamBurstMaxInterval));
        return Random.Range(minInterval, maxInterval);
    }

    private void TrySpawnFlameTrailOverTime()
    {
        if (typeData == null || !typeData.CreatesFlameTrail)
        {
            return;
        }

        if (Time.time < nextFlameTrailAllowedTime)
        {
            return;
        }

        nextFlameTrailAllowedTime = Time.time + typeData.FlameTrailSpawnInterval;

        float spawnOffsetY = ballCollider != null ? ballCollider.bounds.extents.y + FlameTrailSpawnOffset : FlameTrailSpawnOffset;
        Vector3 spawnPosition = transform.position - new Vector3(0f, spawnOffsetY, 0f);
        float flameScale = transform.lossyScale.x * typeData.FlameTrailSizeMultiplier;
        Sprite flameSprite = typeData.FlameTrailSprite != null ? typeData.FlameTrailSprite : typeData.BallSprite;
        FlameTrailProjectile.Spawn(
            spawnPosition,
            ballCollider,
            flameSprite,
            typeData.FlameTrailAnimSprites,
            typeData.FlameTrailAnimFrameRate,
            typeData.FlameTrailColor,
            flameScale,
            typeData.FlameTrailRiseSpeed,
            typeData.FlameTrailLifetime,
            typeData.FlameTrailImpactDamage,
            typeData.FlameTrailBurnDamage,
            typeData.FlameTrailBurnTickInterval,
            typeData.FlameTrailBurnHitCount);
    }

    private void TrySpawnFertilePatchOverTime()
    {
        if (typeData == null || !typeData.CreatesFertileLand)
        {
            return;
        }

        if (Time.time < nextFertilePatchAllowedTime)
        {
            return;
        }

        nextFertilePatchAllowedTime = Time.time + typeData.FertilePatchSpawnInterval;

        float spawnOffsetY = ballCollider != null ? ballCollider.bounds.extents.y + FlameTrailSpawnOffset : FlameTrailSpawnOffset;
        Vector3 spawnPosition = transform.position - new Vector3(0f, spawnOffsetY, 0f);
        float patchScale = transform.lossyScale.x * typeData.FertilePatchSizeMultiplier;
        Sprite patchSprite = typeData.FertilePatchSprite != null ? typeData.FertilePatchSprite : typeData.BallSprite;
        FertilePatchProjectile.Spawn(
            spawnPosition,
            ballCollider,
            patchSprite,
            typeData.FertilePatchAnimSprites,
            typeData.FertilePatchAnimFrameRate,
            typeData.FertilePatchColor,
            patchScale,
            typeData.FertilePatchRiseSpeed,
            typeData.FertilePatchLifetime,
            typeData.EarthCrack,
            typeData.FertilePatchCrackShatterDamage,
            typeData.FertilePatchCrackShatterRadius,
            typeData.AppliesRoot,
            typeData.FertilePatchRootRadius,
            typeData.FertilePatchRootDuration,
            typeData.FertilePatchRootSpeedMultiplier);
    }

    private void InitializeRollingThunderCycle()
    {
        if (typeData == null || !typeData.CreatesRollingThunder)
        {
            rollingThunderCurrentScaleMultiplier = 1f;
            if (typeBaseScale.sqrMagnitude > 0f)
            {
                transform.localScale = typeBaseScale;
            }

            return;
        }

        rollingThunderCurrentScaleMultiplier = Mathf.Max(1f, typeData.RollingThunderStartScaleMultiplier);
        transform.localScale = typeBaseScale * rollingThunderCurrentScaleMultiplier;
    }

    private void TryAdvanceRollingThunderFromBrickHit()
    {
        if (typeData == null || !typeData.CreatesRollingThunder)
        {
            return;
        }

        float startScale = Mathf.Max(1f, typeData.RollingThunderStartScaleMultiplier);
        float maxScale = Mathf.Max(startScale, typeData.RollingThunderMaxScaleMultiplier);
        float growthAmount = Mathf.Max(0.01f, typeData.RollingThunderGrowthAmount);

        rollingThunderCurrentScaleMultiplier = Mathf.Clamp(rollingThunderCurrentScaleMultiplier + growthAmount, startScale, maxScale);
        transform.localScale = typeBaseScale * rollingThunderCurrentScaleMultiplier;

        if (rollingThunderCurrentScaleMultiplier < maxScale)
        {
            return;
        }

        SpawnRollingThunderBall();
        InitializeRollingThunderCycle();
    }

    private void SpawnRollingThunderBall()
    {
        BallTypeData spawnedType = typeData != null && typeData.RollingThunderSpawnBallType != null
            ? typeData.RollingThunderSpawnBallType
            : typeData;
        if (spawnedType == null)
        {
            return;
        }

        Vector2 direction = GetRandomRollingThunderDirection();
        Vector3 spawnPosition = transform.position + (Vector3)(direction * RollingThunderSpawnOffset);
        SpawnBallCopy(spawnedType, spawnPosition, direction);
    }

    private Vector2 GetRandomRollingThunderDirection()
    {
        float minAngle = typeData != null ? typeData.RollingThunderMinLaunchAngle : 0f;
        float maxAngle = typeData != null ? typeData.RollingThunderMaxLaunchAngle : 360f;

        if (maxAngle < minAngle)
        {
            (minAngle, maxAngle) = (maxAngle, minAngle);
        }

        float angleRadians = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
        if (direction.sqrMagnitude <= DirectionEpsilon)
        {
            return Vector2.up;
        }

        return direction.normalized;
    }
}
