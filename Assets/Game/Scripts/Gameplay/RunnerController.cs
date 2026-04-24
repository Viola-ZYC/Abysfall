using System;
using UnityEngine;

namespace EndlessRunner
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class RunnerController : MonoBehaviour
    {
        [SerializeField] private RunnerConfig config;
        [SerializeField] private InputRouter input;
        [SerializeField] private HitStopper hitStopper;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private bool clampHorizontal = true;
        [SerializeField] private float minX = -3f;
        [SerializeField] private float maxX = 3f;
        [SerializeField] private float boundaryPadding = 0f;
        [SerializeField] private bool useVisualBoundsForClamp = true;
        [SerializeField] private bool resetTransformOnReset = true;
        [SerializeField] private bool spawnAboveCameraOnReset = true;
        [SerializeField, Min(0f)] private float spawnAboveCameraPadding = 0.5f;

        private Rigidbody2D body;
        private Collider2D runnerCollider;
        private Renderer runnerRenderer;
        private CharacterAbilityController abilityController;
        private CameraFollow2D cameraFollow;
        private Vector3 initialWorldPosition;
        private Quaternion initialWorldRotation;
        private float currentGravityScale;
        private int currentHealth;
        private float slowTimer;
        private float speedMultiplier = 1f;
        private readonly RaycastHit2D[] sweepHits = new RaycastHit2D[8];
        private float abilitySpeedMultiplier = 1f;
        private float abilityGravityMultiplier = 1f;
        private float abilityBrakeImpulseBonus = 0f;
        private float abilityHitStopBonus = 0f;
        private int abilityMaxHealthBonus = 0;
        private Vector2 currentAcceleration;
        private Vector2 lastVelocitySample;
        private bool hasVelocitySample;
        private int impulsePreserveFrames;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => config != null ? Mathf.Max(1, config.maxHealth + abilityMaxHealthBonus) : Mathf.Max(1, currentHealth);
        public Vector2 CurrentVelocity => body != null ? body.linearVelocity : Vector2.zero;
        public Vector2 CurrentAcceleration => currentAcceleration;
        public event Action<int, int> HealthChanged;
        public event Action<CreatureBase> CreatureStomped;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            runnerCollider = GetComponent<Collider2D>();
            runnerRenderer = GetComponent<Renderer>();
            abilityController = GetComponent<CharacterAbilityController>();
            cameraFollow = FindAnyObjectByType<CameraFollow2D>();
            if (body != null)
            {
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            initialWorldPosition = transform.position;
            initialWorldRotation = transform.rotation;

            ResetRunner();
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.TouchReleased -= OnTouchReleased;
                input.TouchReleased += OnTouchReleased;
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.TouchReleased -= OnTouchReleased;
            }
        }

        private void Update()
        {
            if (config == null)
            {
                return;
            }

            if (!IsRunning())
            {
                SetSimulation(false);
                return;
            }

            SetSimulation(true);
            if (impulsePreserveFrames <= 0)
            {
                UpdateGravity();
            }
            UpdateSlowTimer();
        }

        private void FixedUpdate()
        {
            if (!IsRunning() || config == null)
            {
                ResetKinematicsSample();
                return;
            }

            UpdateHorizontalMovement();
            ClampHorizontalPosition();
            ClampFallSpeed();
            SweepForHazards();
            UpdateKinematicsSample();
        }

        public void ResetRunner()
        {
            if (config == null || body == null)
            {
                return;
            }

            currentGravityScale = config.baseGravityScale;
            body.gravityScale = currentGravityScale;
            Vector3 resetPosition = GetResetWorldPosition();
            if (resetTransformOnReset)
            {
                transform.position = resetPosition;
                transform.rotation = initialWorldRotation;
                body.position = resetPosition;
                body.rotation = initialWorldRotation.eulerAngles.z;
            }
            body.linearVelocity = Vector2.zero;
            ResetKinematicsSample();
            slowTimer = 0f;
            speedMultiplier = 1f;
            impulsePreserveFrames = 0;
            currentHealth = MaxHealth;
            HealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        private Vector3 GetResetWorldPosition()
        {
            Vector3 position = initialWorldPosition;
            if (!spawnAboveCameraOnReset)
            {
                return position;
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindAnyObjectByType<CameraFollow2D>();
            }

            Camera targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindAnyObjectByType<Camera>();
            }

            if (targetCamera == null)
            {
                return position;
            }

            float cameraY = cameraFollow != null ? cameraFollow.RunStartPosition.y : targetCamera.transform.position.y;
            float topY;
            if (targetCamera.orthographic)
            {
                topY = cameraY + targetCamera.orthographicSize;
            }
            else
            {
                float distance = Mathf.Abs(targetCamera.transform.position.z);
                Vector3 viewport = new Vector3(0.5f, 1f, distance);
                topY = targetCamera.ViewportToWorldPoint(viewport).y;
            }

            position.y = topY + GetVerticalHalfExtent() + Mathf.Max(0f, spawnAboveCameraPadding);
            return position;
        }

        public void OnAttackHit(CreatureBase creature)
        {
            if (creature == null || !creature.IsAlive)
            {
                return;
            }

            ApplyContactSlow();
            creature.OnHitByAttack();
            AudioManager.Instance?.PlayStomp();
            CreatureStomped?.Invoke(creature);
        }

        private bool IsRunning()
        {
            return GameManager.Instance == null || GameManager.Instance.State == GameState.Running;
        }

        private void UpdateGravity()
        {
            float gravityIncrease = GetGravityIncreasePerSecond();
            float targetMax = config.maxGravityScale;
            if (targetMax > 0f && targetMax > config.baseGravityScale)
            {
                float normalized = Mathf.InverseLerp(config.baseGravityScale, targetMax, currentGravityScale);
                float damp = 1f - normalized;
                currentGravityScale += gravityIncrease * damp * Time.deltaTime;
                currentGravityScale = Mathf.Min(currentGravityScale, targetMax);
            }
            else
            {
                currentGravityScale += gravityIncrease * Time.deltaTime;
                if (targetMax > 0f)
                {
                    currentGravityScale = Mathf.Min(currentGravityScale, targetMax);
                }
            }

            body.gravityScale = currentGravityScale * Mathf.Max(0.1f, abilityGravityMultiplier);
        }

        private float GetGravityIncreasePerSecond()
        {
            if (config == null)
            {
                return 0f;
            }

            if (!config.useScoreBasedGravity)
            {
                return Mathf.Max(0f, config.gravityIncreasePerSecond);
            }

            int score = 0;
            if (scoreManager != null)
            {
                score = scoreManager.Score;
            }
            else if (ScoreManager.Instance != null)
            {
                score = ScoreManager.Instance.Score;
            }

            int threshold1 = Mathf.Max(0, config.gravityScoreThreshold1);
            int threshold2 = Mathf.Max(threshold1, config.gravityScoreThreshold2);
            if (score < threshold1)
            {
                return Mathf.Max(0f, config.gravityIncreaseStage1);
            }

            if (score < threshold2)
            {
                return Mathf.Max(0f, config.gravityIncreaseStage2);
            }

            return Mathf.Max(0f, config.gravityIncreaseStage3);
        }

        private void UpdateHorizontalMovement()
        {
            if (impulsePreserveFrames > 0)
            {
                impulsePreserveFrames--;
                return;
            }

            float maxHorizontalSpeed = GetEffectiveHorizontalSpeed();
            float desiredX = 0f;

            if (!TryGetPointerFollowVelocity(maxHorizontalSpeed, out desiredX))
            {
                float horizontal = input != null ? input.Horizontal : 0f;
                desiredX = horizontal * maxHorizontalSpeed;
            }

            if (clampHorizontal && body != null)
            {
                GetEffectiveHorizontalBounds(out float effectiveMinX, out float effectiveMaxX);
                float x = body.position.x;

                // If player is already on the boundary, ignore input that pushes further into the wall.
                if ((x <= effectiveMinX + 0.0001f && desiredX < 0f) ||
                    (x >= effectiveMaxX - 0.0001f && desiredX > 0f))
                {
                    desiredX = 0f;
                }
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = desiredX;
            body.linearVelocity = velocity;
        }

        private float GetEffectiveHorizontalSpeed()
        {
            if (config == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, config.horizontalSpeed * speedMultiplier * Mathf.Max(0.1f, abilitySpeedMultiplier));
        }

        private bool TryGetPointerFollowVelocity(float maxHorizontalSpeed, out float desiredVelocityX)
        {
            desiredVelocityX = 0f;

            if (body == null || input == null || !input.HasMovementPointer || maxHorizontalSpeed <= 0f)
            {
                return false;
            }

            GetEffectiveHorizontalBounds(out float effectiveMinX, out float effectiveMaxX);
            float targetX = GetPointerTargetX(input.MovementPointerScreenPosition.x, effectiveMinX, effectiveMaxX);
            float deltaX = targetX - body.position.x;
            if (Mathf.Abs(deltaX) <= 0.01f)
            {
                desiredVelocityX = 0f;
                return true;
            }

            float responsiveness = Mathf.Max(0f, config.horizontalFollowResponsiveness) *
                                   speedMultiplier *
                                   Mathf.Max(0.1f, abilitySpeedMultiplier);

            if (responsiveness <= 0f)
            {
                desiredVelocityX = Mathf.Sign(deltaX) * maxHorizontalSpeed;
                return true;
            }

            desiredVelocityX = Mathf.Clamp(deltaX * responsiveness, -maxHorizontalSpeed, maxHorizontalSpeed);
            return true;
        }

        private static float GetPointerTargetX(float screenX, float minWorldX, float maxWorldX)
        {
            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0f)
            {
                safeArea = new Rect(0f, 0f, Mathf.Max(1f, Screen.width), Mathf.Max(1f, Screen.height));
            }

            float normalizedX = Mathf.InverseLerp(safeArea.xMin, safeArea.xMax, screenX);
            return Mathf.Lerp(minWorldX, maxWorldX, Mathf.Clamp01(normalizedX));
        }

        private void ClampFallSpeed()
        {
            if (config.maxFallSpeed <= 0f)
            {
                return;
            }

            if (body.linearVelocity.y < -config.maxFallSpeed)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, -config.maxFallSpeed);
            }
        }

        private void ClampHorizontalPosition()
        {
            if (!clampHorizontal || body == null)
            {
                return;
            }

            GetEffectiveHorizontalBounds(out float effectiveMinX, out float effectiveMaxX);

            float currentX = body.position.x;
            float clampedX = Mathf.Clamp(currentX, effectiveMinX, effectiveMaxX);
            body.position = new Vector2(clampedX, body.position.y);

            Vector2 velocity = body.linearVelocity;
            if (currentX <= effectiveMinX + 0.0001f && velocity.x < 0f)
            {
                velocity.x = 0f;
            }
            else if (currentX >= effectiveMaxX - 0.0001f && velocity.x > 0f)
            {
                velocity.x = 0f;
            }

            body.linearVelocity = velocity;
        }

        private float GetHorizontalHalfWidth()
        {
            float halfWidth = 0f;

            if (runnerCollider == null)
            {
                runnerCollider = GetComponent<Collider2D>();
            }

            if (runnerCollider != null)
            {
                halfWidth = runnerCollider.bounds.extents.x;
            }

            if (useVisualBoundsForClamp)
            {
                if (runnerRenderer == null)
                {
                    runnerRenderer = GetComponent<Renderer>();
                }

                if (runnerRenderer != null)
                {
                    halfWidth = Mathf.Max(halfWidth, runnerRenderer.bounds.extents.x);
                }
            }

            return halfWidth;
        }

        private float GetVerticalHalfExtent()
        {
            float halfHeight = 0f;

            if (runnerCollider == null)
            {
                runnerCollider = GetComponent<Collider2D>();
            }

            if (runnerCollider != null)
            {
                halfHeight = runnerCollider.bounds.extents.y;
            }

            if (useVisualBoundsForClamp)
            {
                if (runnerRenderer == null)
                {
                    runnerRenderer = GetComponent<Renderer>();
                }

                if (runnerRenderer != null)
                {
                    halfHeight = Mathf.Max(halfHeight, runnerRenderer.bounds.extents.y);
                }
            }

            return halfHeight;
        }

        private void GetEffectiveHorizontalBounds(out float effectiveMinX, out float effectiveMaxX)
        {
            float halfWidth = GetHorizontalHalfWidth();
            effectiveMinX = minX + halfWidth + Mathf.Max(0f, boundaryPadding);
            effectiveMaxX = maxX - halfWidth - Mathf.Max(0f, boundaryPadding);
            if (effectiveMinX > effectiveMaxX)
            {
                float center = (minX + maxX) * 0.5f;
                effectiveMinX = center;
                effectiveMaxX = center;
            }
        }

        private void UpdateSlowTimer()
        {
            if (slowTimer <= 0f)
            {
                speedMultiplier = 1f;
                return;
            }

            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                slowTimer = 0f;
                speedMultiplier = 1f;
            }
        }

        private void ApplyContactSlow()
        {
            if (config == null)
            {
                return;
            }

            float multiplier = Mathf.Clamp(config.contactSlowMultiplier, 0.1f, 1f);
            speedMultiplier = Mathf.Min(speedMultiplier, multiplier);
            slowTimer = Mathf.Max(slowTimer, Mathf.Max(0f, config.contactSlowDuration));

            if (body != null)
            {
                float damp = Mathf.Clamp(config.contactVelocityDamp, 0.1f, 1f);
                Vector2 velocity = body.linearVelocity;
                velocity.x *= multiplier;
                velocity.y *= damp;
                body.linearVelocity = velocity;
            }
        }

        private void ResetAccelerationState()
        {
            if (config == null)
            {
                return;
            }

            currentGravityScale = config.baseGravityScale;
            if (body != null)
            {
                body.gravityScale = currentGravityScale * Mathf.Max(0.1f, abilityGravityMultiplier);
            }

            ResetKinematicsSample();
        }

        private void SweepForHazards()
        {
            if (body == null)
            {
                return;
            }

            Vector2 delta = body.linearVelocity * Time.fixedDeltaTime;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = true;
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));

            int hitCount = body.Cast(delta.normalized, filter, sweepHits, delta.magnitude);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = sweepHits[i].collider;
                if (hit == null)
                {
                    continue;
                }

                if (!TryHandleHazardContact(hit))
                {
                    continue;
                }
            }
        }

        private bool IsFalling()
        {
            return body != null && body.linearVelocity.y <= 0f;
        }

        private void SetSimulation(bool enabled)
        {
            if (body == null || body.simulated == enabled)
            {
                return;
            }

            body.simulated = enabled;
            if (!enabled)
            {
                ResetKinematicsSample();
            }

            if (enabled)
            {
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            }
        }

        private void UpdateKinematicsSample()
        {
            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            if (hasVelocitySample && Time.fixedDeltaTime > Mathf.Epsilon)
            {
                currentAcceleration = (velocity - lastVelocitySample) / Time.fixedDeltaTime;
            }
            else
            {
                currentAcceleration = Vector2.zero;
            }

            lastVelocitySample = velocity;
            hasVelocitySample = true;
        }

        private void ResetKinematicsSample()
        {
            currentAcceleration = Vector2.zero;
            lastVelocitySample = body != null ? body.linearVelocity : Vector2.zero;
            hasVelocitySample = false;
        }

        private void ApplyDamage(int amount)
        {
            if (amount <= 0 || currentHealth <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            AudioManager.Instance?.PlayDamage();
            HealthChanged?.Invoke(currentHealth, MaxHealth);

            if (currentHealth <= 0)
            {
                GameManager.Instance?.GameOver();
            }
        }

        private void ApplyBrake()
        {
            Vector2 velocity = body.linearVelocity;
            if (config.resetVerticalVelocity)
            {
                velocity.y = 0f;
            }

            body.linearVelocity = velocity;

            float impulse = config.brakeUpwardImpulse + abilityBrakeImpulseBonus;
            if (impulse > 0f)
            {
                body.AddForce(Vector2.up * impulse, ForceMode2D.Impulse);
            }

            float hitStopDuration = config.hitStopDuration + abilityHitStopBonus;
            if (hitStopper != null && hitStopDuration > 0f)
            {
                hitStopper.Trigger(hitStopDuration);
            }
        }

        public void AddAbilityModifiers(RunnerAbilityModifiers modifiers)
        {
            abilitySpeedMultiplier *= Mathf.Max(0.1f, modifiers.speedMultiplier);
            abilityGravityMultiplier *= Mathf.Max(0.1f, modifiers.gravityMultiplier);
            abilityBrakeImpulseBonus += modifiers.brakeImpulseBonus;
            abilityHitStopBonus += modifiers.hitStopBonus;

            if (modifiers.maxHealthBonus != 0)
            {
                abilityMaxHealthBonus += modifiers.maxHealthBonus;
                currentHealth = Mathf.Clamp(currentHealth + modifiers.maxHealthBonus, 1, MaxHealth);
                HealthChanged?.Invoke(currentHealth, MaxHealth);
            }
        }

        public void RemoveAbilityModifiers(RunnerAbilityModifiers modifiers)
        {
            float speedDiv = Mathf.Max(0.1f, modifiers.speedMultiplier);
            float gravityDiv = Mathf.Max(0.1f, modifiers.gravityMultiplier);
            abilitySpeedMultiplier /= speedDiv;
            abilityGravityMultiplier /= gravityDiv;
            abilityBrakeImpulseBonus -= modifiers.brakeImpulseBonus;
            abilityHitStopBonus -= modifiers.hitStopBonus;

            if (modifiers.maxHealthBonus != 0)
            {
                abilityMaxHealthBonus -= modifiers.maxHealthBonus;
                currentHealth = Mathf.Clamp(currentHealth, 1, MaxHealth);
                HealthChanged?.Invoke(currentHealth, MaxHealth);
            }
        }

        public void ClearAbilityModifiers()
        {
            abilitySpeedMultiplier = 1f;
            abilityGravityMultiplier = 1f;
            abilityBrakeImpulseBonus = 0f;
            abilityHitStopBonus = 0f;
            abilityMaxHealthBonus = 0;
            currentHealth = Mathf.Clamp(currentHealth, 1, MaxHealth);
            HealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void ResetVelocity()
        {
            if (body == null)
            {
                return;
            }

            body.linearVelocity = Vector2.zero;
        }

        public void ApplyHorizontalImpulse(float impulse)
        {
            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = impulse;
            body.linearVelocity = velocity;
            impulsePreserveFrames = 2;
        }

        public void ApplyCharacter(RunnerConfig newConfig, Sprite characterSprite)
        {
            bool configChanged = false;

            if (newConfig != null && config != newConfig)
            {
                config = newConfig;
                configChanged = true;
            }

            if (characterSprite != null)
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = characterSprite;
                    runnerRenderer = spriteRenderer;
                }
            }

            if (!configChanged)
            {
                return;
            }

            if (!IsRunning())
            {
                ResetRunner();
                return;
            }

            currentHealth = Mathf.Clamp(currentHealth, 1, MaxHealth);
            HealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void SetHorizontalBounds(float newMinX, float newMaxX)
        {
            if (newMinX > newMaxX)
            {
                (newMinX, newMaxX) = (newMaxX, newMinX);
            }

            minX = newMinX;
            maxX = newMaxX;
        }

        public void GetHorizontalBounds(out float currentMinX, out float currentMaxX)
        {
            currentMinX = minX;
            currentMaxX = maxX;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleContact(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other);
        }

        private void HandleContact(Collider2D collider)
        {
            if (collider == null)
            {
                return;
            }

            if (TryHandleHazardContact(collider))
            {
                return;
            }

            CreatureBase creature = collider.GetComponent<CreatureBase>();
            if (creature == null)
            {
                return;
            }

            if (IsFalling())
            {
                OnAttackHit(creature);
                return;
            }

            GameManager.Instance?.GameOver();
        }

        private bool TryHandleHazardContact(Collider2D collider)
        {
            if (collider == null)
            {
                return false;
            }

            CreatureBase creature = collider.GetComponent<CreatureBase>();
            if (!IsHazardCreature(creature))
            {
                return false;
            }

            if (creature == null || !creature.IsAlive)
            {
                return true;
            }

            ApplyDamage(1);
            ApplyContactSlow();
            creature.OnHitByAttack();
            return true;
        }

        private void ResetToInitialMotionState()
        {
            slowTimer = 0f;
            speedMultiplier = 1f;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            ResetAccelerationState();
            impulsePreserveFrames = 1;
            if (body != null)
            {
                body.gravityScale = 0f;
            }
        }

        private static bool IsHazardCreature(CreatureBase creatureBase)
        {
            SpecialCreature creature = creatureBase as SpecialCreature;
            return creature != null && creature.IsHazard();
        }

        private void OnTouchReleased()
        {
            if (!IsRunning())
            {
                return;
            }

            if (abilityController == null)
            {
                abilityController = GetComponent<CharacterAbilityController>();
            }

            abilityController?.TryUseAbility();
        }
    }
}
