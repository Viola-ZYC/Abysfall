using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EndlessRunner.EditorTools
{
    public static class PlayerVisualAnimationBuilder
    {
        private const string AutoBuildKey = "EndlessRunner.PlayerVisualAnimationBuilder.AutoBuilt";
        private const string ProcessedDir = "Assets/Game/Art/Sprite/Player/Processed";
        private const string AnimDir = "Assets/Game/Art/Sprite/Player/Animations";
        private const string IdleSpritePath = ProcessedDir + "/player_idle_cropped.png";
        private const string MoveSpritePath = ProcessedDir + "/player_cropped.png";
        private const string JumpSpritePath = ProcessedDir + "/player_jump_cropped.png";
        private const string FallSpritePath = ProcessedDir + "/player_fall_cropped.png";
        private const string IdleClipPath = AnimDir + "/Player_Idle.anim";
        private const string MoveClipPath = AnimDir + "/Player_Move.anim";
        private const string JumpClipPath = AnimDir + "/Player_Jump.anim";
        private const string FallClipPath = AnimDir + "/Player_Fall.anim";
        private const string ControllerPath = AnimDir + "/Player.controller";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [InitializeOnLoadMethod]
        private static void TryAutoBuildOnce()
        {
            // Keep animator setup as an explicit menu action to avoid re-attaching Animator automatically.
            if (Application.isBatchMode || EditorPrefs.GetBool(AutoBuildKey, false))
            {
                return;
            }
        }

        [MenuItem("Tools/Player/Build Visual Animator")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(AnimDir);
            ConfigureSpriteImporter(IdleSpritePath);
            ConfigureSpriteImporter(MoveSpritePath);
            ConfigureSpriteImporter(JumpSpritePath);
            ConfigureSpriteImporter(FallSpritePath);

            Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(IdleSpritePath);
            Sprite move = AssetDatabase.LoadAssetAtPath<Sprite>(MoveSpritePath);
            Sprite jump = AssetDatabase.LoadAssetAtPath<Sprite>(JumpSpritePath);
            Sprite fall = AssetDatabase.LoadAssetAtPath<Sprite>(FallSpritePath);
            if (idle == null || move == null || jump == null || fall == null)
            {
                throw new FileNotFoundException("Processed player sprites are missing. Please ensure cropped PNG files exist.");
            }

            AnimationClip idleClip = CreateSingleSpriteClip(IdleClipPath, "Player_Idle", idle);
            AnimationClip moveClip = CreateSingleSpriteClip(MoveClipPath, "Player_Move", move);
            AnimationClip jumpClip = CreateSingleSpriteClip(JumpClipPath, "Player_Jump", jump);
            AnimationClip fallClip = CreateSingleSpriteClip(FallClipPath, "Player_Fall", fall);
            AnimatorController controller = CreateAnimatorController(idleClip, moveClip, jumpClip, fallClip);

            AttachToPlayerInScene(controller, idle);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Player visual animator build completed.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Sprite not found at: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static AnimationClip CreateSingleSpriteClip(string clipPath, string clipName, Sprite sprite)
        {
            AssetDatabase.DeleteAsset(clipPath);
            AnimationClip clip = new AnimationClip
            {
                name = clipName,
                frameRate = 12f
            };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keys =
            {
                new ObjectReferenceKeyframe { time = 0f, value = sprite },
                new ObjectReferenceKeyframe { time = 1f / 12f, value = sprite }
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            SerializedObject clipSerialized = new SerializedObject(clip);
            SerializedProperty settings = clipSerialized.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                settings.FindPropertyRelative("m_LoopTime").boolValue = true;
                settings.FindPropertyRelative("m_KeepOriginalPositionY").boolValue = true;
            }

            clipSerialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(clip, clipPath);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        }

        private static AnimatorController CreateAnimatorController(
            AnimationClip idleClip,
            AnimationClip moveClip,
            AnimationClip jumpClip,
            AnimationClip fallClip)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            while (stateMachine.states.Length > 0)
            {
                stateMachine.RemoveState(stateMachine.states[0].state);
            }

            AddBoolParam(controller, "IsRunning");
            AddBoolParam(controller, "IsMoving");
            AddBoolParam(controller, "IsJumping");
            AddBoolParam(controller, "IsFalling");

            AnimatorState idle = stateMachine.AddState("Idle", new Vector3(280f, 120f, 0f));
            AnimatorState move = stateMachine.AddState("Move", new Vector3(520f, 120f, 0f));
            AnimatorState jump = stateMachine.AddState("Jump", new Vector3(280f, 320f, 0f));
            AnimatorState fall = stateMachine.AddState("Fall", new Vector3(520f, 320f, 0f));
            idle.motion = idleClip;
            move.motion = moveClip;
            jump.motion = jumpClip;
            fall.motion = fallClip;
            stateMachine.defaultState = idle;

            AnimatorStateTransition idleToMove = CreateTransition(idle, move);
            idleToMove.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

            AnimatorStateTransition moveToIdle = CreateTransition(move, idle);
            moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
            moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFalling");
            moveToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            AnimatorStateTransition jumpToFall = CreateTransition(jump, fall);
            jumpToFall.AddCondition(AnimatorConditionMode.If, 0f, "IsFalling");

            AnimatorStateTransition jumpToMove = CreateTransition(jump, move);
            jumpToMove.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
            jumpToMove.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFalling");
            jumpToMove.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            jumpToMove.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            AnimatorStateTransition jumpToIdle = CreateTransition(jump, idle);
            jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
            jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFalling");
            jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            AnimatorStateTransition fallToMove = CreateTransition(fall, move);
            fallToMove.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFalling");
            fallToMove.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
            fallToMove.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            fallToMove.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            AnimatorStateTransition fallToIdle = CreateTransition(fall, idle);
            fallToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFalling");
            fallToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
            fallToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            fallToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsRunning");

            AnimatorStateTransition anyToJump = stateMachine.AddAnyStateTransition(jump);
            ConfigureTransition(anyToJump);
            anyToJump.AddCondition(AnimatorConditionMode.If, 0f, "IsJumping");

            AnimatorStateTransition anyToFall = stateMachine.AddAnyStateTransition(fall);
            ConfigureTransition(anyToFall);
            anyToFall.AddCondition(AnimatorConditionMode.If, 0f, "IsFalling");

            AnimatorStateTransition anyToIdleWhenStopped = stateMachine.AddAnyStateTransition(idle);
            ConfigureTransition(anyToIdleWhenStopped);
            anyToIdleWhenStopped.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddBoolParam(AnimatorController controller, string name)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                {
                    return;
                }
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Bool);
        }

        private static AnimatorStateTransition CreateTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureTransition(transition);
            return transition;
        }

        private static void ConfigureTransition(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.exitTime = 0f;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
        }

        private static void AttachToPlayerInScene(AnimatorController controller, Sprite idleSprite)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new FileNotFoundException("Player object not found in SampleScene.");
            }

            SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = idleSprite;
                EditorUtility.SetDirty(spriteRenderer);
            }

            Animator animator = player.GetComponent<Animator>();
            if (animator == null)
            {
                animator = player.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);

            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
