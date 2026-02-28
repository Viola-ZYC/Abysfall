using UnityEngine;

namespace EndlessRunner
{
    /// <summary>
    /// 角色能力执行器：
    /// - None：无特殊能力
    /// - SingleAirJumpOnBlock：空中可额外跳跃一次，踩到方块后刷新次数
    ///
    /// 说明：该脚本会由 CharacterManager 在生成角色后自动配置参数。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterAbilityController : MonoBehaviour
    {
        [Header("Input (PC)")]
        [SerializeField] private bool enableKeyboardTrigger = true;
        [SerializeField] private KeyCode keyboardJumpKey = KeyCode.Space;

        [Header("Refresh Rule (踩到方块刷新)")]
        [SerializeField] private string refreshTag = "Block";
        [SerializeField] private bool useLayerRefresh = false;
        [SerializeField] private LayerMask refreshLayers;
        [SerializeField] private bool allowObstacleComponentAsRefresh = true;
        [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.2f;

        private Rigidbody2D body;
        private CharacterAbilityType abilityType = CharacterAbilityType.None;
        private float airJumpImpulse = 8f;
        private int maxAirJumpCharges = 1;
        private int remainingAirJumpCharges = 0;

        public CharacterAbilityType CurrentAbilityType => abilityType;
        public bool IsAirJumpCharacter => abilityType == CharacterAbilityType.SingleAirJumpOnBlock;
        public int RemainingAirJumpCharges => remainingAirJumpCharges;

        /// <summary>
        /// 参数：当前剩余次数、最大次数。
        /// UI 可监听这个事件来刷新按钮文案。
        /// </summary>
        public event System.Action<int, int> AirJumpChargeChanged;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (!enableKeyboardTrigger || !IsAirJumpCharacter)
            {
                return;
            }

            if (Input.GetKeyDown(keyboardJumpKey))
            {
                TryUseAbility();
            }
        }

        /// <summary>
        /// 通用配置入口（推荐用于“能力/装备方案”系统）。
        /// </summary>
        public void Configure(CharacterAbilityType newAbilityType, float newAirJumpImpulse, int newAirJumpCharges)
        {
            abilityType = newAbilityType;
            airJumpImpulse = Mathf.Max(0f, newAirJumpImpulse);
            maxAirJumpCharges = Mathf.Max(1, newAirJumpCharges);
            remainingAirJumpCharges = IsAirJumpCharacter ? maxAirJumpCharges : 0;
            AirJumpChargeChanged?.Invoke(remainingAirJumpCharges, maxAirJumpCharges);
        }

        /// <summary>
        /// 兼容 CharacterDefinition 的配置入口。
        /// </summary>
        public void Configure(CharacterDefinition definition)
        {
            if (definition == null)
            {
                Configure(CharacterAbilityType.None, 8f, 1);
                return;
            }

            Configure(definition.AbilityType, definition.AirJumpImpulse, definition.AirJumpCharges);
        }

        /// <summary>
        /// 供 UI 跳跃按钮直接调用（安卓触屏）或 PC 输入调用。
        /// </summary>
        public bool TryUseAbility()
        {
            if (!IsAirJumpCharacter || body == null)
            {
                return false;
            }

            if (remainingAirJumpCharges <= 0)
            {
                return false;
            }

            if (!IsGameRunning())
            {
                return false;
            }

            // 先清除向下速度，再给一个向上的冲量，手感更稳定。
            Vector2 velocity = body.linearVelocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            body.linearVelocity = velocity;
            body.AddForce(Vector2.up * airJumpImpulse, ForceMode2D.Impulse);

            remainingAirJumpCharges--;
            AirJumpChargeChanged?.Invoke(remainingAirJumpCharges, maxAirJumpCharges);
            return true;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryRefreshByCollision(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryRefreshByCollision(collision);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsAirJumpCharacter || !IsRefreshCollider(other))
            {
                return;
            }

            RefreshCharges();
        }

        private void TryRefreshByCollision(Collision2D collision)
        {
            if (!IsAirJumpCharacter || collision == null || collision.collider == null)
            {
                return;
            }

            if (!IsRefreshCollider(collision.collider))
            {
                return;
            }

            // 只有从“上往下踩到”时才刷新，避免侧面蹭到就刷新。
            bool landedFromTop = false;
            int count = collision.contactCount;
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                if (contact.normal.y > minGroundNormalY)
                {
                    landedFromTop = true;
                    break;
                }
            }

            if (!landedFromTop)
            {
                return;
            }

            RefreshCharges();
        }

        private bool IsRefreshCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(refreshTag) && collider.CompareTag(refreshTag))
            {
                return true;
            }

            if (useLayerRefresh && ((1 << collider.gameObject.layer) & refreshLayers.value) != 0)
            {
                return true;
            }

            if (allowObstacleComponentAsRefresh && collider.GetComponent<Obstacle>() != null)
            {
                return true;
            }

            return false;
        }

        private void RefreshCharges()
        {
            remainingAirJumpCharges = maxAirJumpCharges;
            AirJumpChargeChanged?.Invoke(remainingAirJumpCharges, maxAirJumpCharges);
        }

        private static bool IsGameRunning()
        {
            return GameManager.Instance == null || GameManager.Instance.State == GameState.Running;
        }
    }
}
