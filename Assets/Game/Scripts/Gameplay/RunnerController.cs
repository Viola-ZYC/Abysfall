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
        [SerializeField] private bool clampHorizontal = true;
        [SerializeField] private float minX = -3f;
        [SerializeField] private float maxX = 3f;
        [SerializeField] private float boundaryPadding = 0f;
        [SerializeField] private bool useVisualBoundsForClamp = true;
        [SerializeField] private bool resetTransformOnReset = true;

        private Rigidbody2D body;
        private Collider2D runnerCollider;
        private Renderer runnerRenderer;
        private CharacterAbilityController abilityController;
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

        public int CurrentHealth => currentHealth;
        public int MaxHealth => config != null ? Mathf.Max(1, config.maxHealth + abilityMaxHealthBonus) : Mathf.Max(1, currentHealth);
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            runnerCollider = GetComponent<Collider2D>();
            runnerRenderer = GetComponent<Renderer>();
            abilityController = GetComponent<CharacterAbilityController>();
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
            UpdateGravity();
            UpdateSlowTimer();
        }

        private void FixedUpdate()
        {
            if (!IsRunning() || config == null)
            {
                return;
            }

            UpdateHorizontalMovement();
            ClampHorizontalPosition();
            ClampFallSpeed();
            SweepForObstacles();
        }

        public void ResetRunner()
        {
            if (config == null || body == null)
            {
                return;
            }

            currentGravityScale = config.baseGravityScale;
            body.gravityScale = currentGravityScale;
            if (resetTransformOnReset)
            {
                transform.position = initialWorldPosition;
                transform.rotation = initialWorldRotation;
                body.position = initialWorldPosition;
                body.rotation = initialWorldRotation.eulerAngles.z;
            }
            body.linearVelocity = Vector2.zero;
            slowTimer = 0f;
            speedMultiplier = 1f;
            currentHealth = MaxHealth;
            HealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void OnAttackHit(Enemy enemy)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return;
            }

            ApplyBrake();
            enemy.OnHitByAttack();
        }

        private bool IsRunning()
        {
            return GameManager.Instance == null || GameManager.Instance.State == GameState.Running;
        }

        private void UpdateGravity()
        {
            float targetMax = config.maxGravityScale;
            if (targetMax > 0f && targetMax > config.baseGravityScale)
            {
                float normalized = Mathf.InverseLerp(config.baseGravityScale, targetMax, currentGravityScale);
                float damp = 1f - normalized;
                currentGravityScale += config.gravityIncreasePerSecond * damp * Time.deltaTime;
                currentGravityScale = Mathf.Min(currentGravityScale, targetMax);
            }
            else
            {
                currentGravityScale += config.gravityIncreasePerSecond * Time.deltaTime;
                if (targetMax > 0f)
                {
                    currentGravityScale = Mathf.Min(currentGravityScale, targetMax);
                }
            }

            body.gravityScale = currentGravityScale * Mathf.Max(0.1f, abilityGravityMultiplier);
        }

        private void UpdateHorizontalMovement()
        {
            float horizontal = input != null ? input.Horizontal : 0f;
            float speed = config.horizontalSpeed * speedMultiplier * Mathf.Max(0.1f, abilitySpeedMultiplier);
            float desiredX = horizontal * speed;

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

        private void ApplyObstacleSlow()
        {
            if (config == null)
            {
                return;
            }

            float multiplier = Mathf.Clamp(config.obstacleSlowMultiplier, 0.1f, 1f);
            speedMultiplier = Mathf.Min(speedMultiplier, multiplier);
            slowTimer = Mathf.Max(slowTimer, Mathf.Max(0f, config.obstacleSlowDuration));

            if (body != null)
            {
                float damp = Mathf.Clamp(config.obstacleVelocityDamp, 0.1f, 1f);
                Vector2 velocity = body.linearVelocity;
                velocity.x *= multiplier;
                velocity.y *= damp;
                body.linearVelocity = velocity;
            }
        }

        private void SweepForObstacles()
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

                Obstacle obstacle = hit.GetComponent<Obstacle>();
                if (obstacle == null || !obstacle.Consume())
                {
                    continue;
                }

                ApplyDamage(1);
                ApplyObstacleSlow();
                DespawnObstacle(obstacle.gameObject);
            }
        }

        private void DespawnObstacle(GameObject obstacleObject)
        {
            if (obstacleObject == null)
            {
                return;
            }

            if (ObjectPool.Instance != null && ObjectPool.Instance.IsPooled(obstacleObject))
            {
                ObjectPool.Instance.Release(obstacleObject);
            }
            else
            {
                Destroy(obstacleObject);
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
            if (enabled)
            {
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            }
        }

        private void ApplyDamage(int amount)
        {
            if (amount <= 0 || currentHealth <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
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
            if (collision.collider.GetComponent<Obstacle>() != null)
            {
                Obstacle obstacle = collision.collider.GetComponent<Obstacle>();
                if (obstacle == null || !obstacle.Consume())
                {
                    return;
                }

                ApplyDamage(1);
                ApplyObstacleSlow();
                DespawnObstacle(obstacle.gameObject);
                return;
            }

            Enemy enemy = collision.collider.GetComponent<Enemy>();
            if (enemy == null)
            {
                return;
            }

            if (IsFalling())
            {
                OnAttackHit(enemy);
                return;
            }

            GameManager.Instance?.GameOver();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Obstacle>() != null)
            {
                Obstacle obstacle = other.GetComponent<Obstacle>();
                if (obstacle == null || !obstacle.Consume())
                {
                    return;
                }

                ApplyDamage(1);
                ApplyObstacleSlow();
                DespawnObstacle(obstacle.gameObject);
                return;
            }

            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy == null)
            {
                return;
            }

            if (IsFalling())
            {
                OnAttackHit(enemy);
                return;
            }

            GameManager.Instance?.GameOver();
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
