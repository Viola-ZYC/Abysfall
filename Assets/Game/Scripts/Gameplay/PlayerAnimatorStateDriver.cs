using UnityEngine;

namespace EndlessRunner
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorStateDriver : MonoBehaviour
    {
        [SerializeField] private RunnerController runner;
        [SerializeField] private float verticalMoveThreshold = 0.05f;
        [SerializeField] private float horizontalFacingThreshold = 0.01f;
        [SerializeField] private bool positiveXFacesRight = true;

        private Animator animator;
        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;

        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (runner == null)
            {
                runner = GetComponent<RunnerController>();
            }

            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            Vector2 velocity = ResolveVelocity();
            bool isJumping = velocity.y > verticalMoveThreshold;
            bool isFalling = velocity.y < -verticalMoveThreshold;

            animator.SetBool(IsJumpingHash, isJumping);
            animator.SetBool(IsFallingHash, isFalling);
            UpdateFacing(velocity.x);
        }

        private Vector2 ResolveVelocity()
        {
            if (runner != null)
            {
                return runner.CurrentVelocity;
            }

            if (body != null)
            {
                return body.linearVelocity;
            }

            return Vector2.zero;
        }

        private void UpdateFacing(float velocityX)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (velocityX > horizontalFacingThreshold)
            {
                SetFacingRight(true);
            }
            else if (velocityX < -horizontalFacingThreshold)
            {
                SetFacingRight(false);
            }
        }

        private void SetFacingRight(bool faceRight)
        {
            // SpriteRenderer.flipX mirrors the sprite around local Y axis.
            spriteRenderer.flipX = positiveXFacesRight ? !faceRight : faceRight;
        }
    }
}
