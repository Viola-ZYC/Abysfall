using System;
using System.Collections.Generic;
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
        private const string SpriteSheetPath = "Assets/Game/Resources/Art/Sprite/Player/Player_Graphics.png";
        private const string AnimDir = "Assets/Game/Resources/Art/Sprite/Player/Animations";
        private const string IdleClipPath = AnimDir + "/Player_Idle.anim";
        private const string JumpClipPath = AnimDir + "/Player_Jump.anim";
        private const string FallClipPath = AnimDir + "/Player_Fall.anim";
        private const string LegacyMoveClipPath = AnimDir + "/Player_Run.anim";
        private const string LegacyAttackClipPath = AnimDir + "/Player_Attack.anim";
        private const string ControllerPath = AnimDir + "/Player_Graphics.controller";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        private static readonly int[] IdleFrames = { 0, 1, 2, 3, 4, 5 };
        private static readonly int[] JumpFrames = { 74, 75, 76, 77 };
        private static readonly int[] FallFrames = { 82, 83, 84, 85 };

        [MenuItem("Tools/Player/Build Visual Animator")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(AnimDir);
            ConfigureSpriteImporter(SpriteSheetPath);
            AssetDatabase.DeleteAsset(LegacyMoveClipPath);
            AssetDatabase.DeleteAsset(LegacyAttackClipPath);

            Dictionary<int, Sprite> spritesByIndex = LoadSpritesByIndex(SpriteSheetPath);
            AnimationClip idleClip = CreateFrameClip(IdleClipPath, "Player_Idle", spritesByIndex, IdleFrames, 8f, true);
            AnimationClip jumpClip = CreateFrameClip(JumpClipPath, "Player_Jump", spritesByIndex, JumpFrames, 12f, false);
            AnimationClip fallClip = CreateFrameClip(FallClipPath, "Player_Fall", spritesByIndex, FallFrames, 10f, true);

            AnimatorController controller = CreateAnimatorController(idleClip, jumpClip, fallClip);
            AttachToPlayerInScene(controller, spritesByIndex[IdleFrames[0]]);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Player animations and Animator Controller generated from Player_Graphics.");
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
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        private static Dictionary<int, Sprite> LoadSpritesByIndex(string spriteSheetPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
            Dictionary<int, Sprite> spritesByIndex = new Dictionary<int, Sprite>();
            foreach (UnityEngine.Object asset in assets)
            {
                Sprite sprite = asset as Sprite;
                if (sprite == null)
                {
                    continue;
                }

                int index = TryParseSpriteIndex(sprite.name);
                if (index < 0)
                {
                    continue;
                }

                spritesByIndex[index] = sprite;
            }

            if (spritesByIndex.Count == 0)
            {
                throw new FileNotFoundException($"No sliced sprites were found in {spriteSheetPath}. Open Sprite Editor and make sure slicing is applied.");
            }

            return spritesByIndex;
        }

        private static int TryParseSpriteIndex(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return -1;
            }

            int separator = spriteName.LastIndexOf('_');
            if (separator < 0 || separator >= spriteName.Length - 1)
            {
                return -1;
            }

            string suffix = spriteName.Substring(separator + 1);
            int index;
            return int.TryParse(suffix, out index) ? index : -1;
        }

        private static AnimationClip CreateFrameClip(
            string clipPath,
            string clipName,
            IReadOnlyDictionary<int, Sprite> spritesByIndex,
            IReadOnlyList<int> frameIndices,
            float frameRate,
            bool loop)
        {
            AssetDatabase.DeleteAsset(clipPath);
            if (frameIndices.Count == 0)
            {
                throw new ArgumentException($"No frame indices configured for {clipName}.");
            }

            List<Sprite> frames = new List<Sprite>(frameIndices.Count);
            foreach (int index in frameIndices)
            {
                if (!spritesByIndex.TryGetValue(index, out Sprite sprite) || sprite == null)
                {
                    throw new FileNotFoundException($"Frame Player_Graphics_{index} is missing. Check Sprite slicing result.");
                }

                frames.Add(sprite);
            }

            AnimationClip clip = new AnimationClip
            {
                name = clipName,
                frameRate = Mathf.Max(1f, frameRate)
            };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Count + 1];
            float secondsPerFrame = 1f / clip.frameRate;
            for (int i = 0; i < frames.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i * secondsPerFrame,
                    value = frames[i]
                };
            }

            // Duplicate the last frame once so the final key is held for one frame duration.
            keys[keys.Length - 1] = new ObjectReferenceKeyframe
            {
                time = frames.Count * secondsPerFrame,
                value = frames[frames.Count - 1]
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            SerializedObject clipSerialized = new SerializedObject(clip);
            SerializedProperty settings = clipSerialized.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                settings.FindPropertyRelative("m_LoopTime").boolValue = loop;
                settings.FindPropertyRelative("m_KeepOriginalPositionY").boolValue = true;
            }

            clipSerialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(clip, clipPath);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        }

        private static AnimatorController CreateAnimatorController(
            AnimationClip idleClip,
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

            AddBoolParam(controller, "IsJumping");
            AddBoolParam(controller, "IsFalling");

            AnimatorState idle = stateMachine.AddState("Idle", new Vector3(280f, 120f, 0f));
            AnimatorState jump = stateMachine.AddState("Jump", new Vector3(280f, 320f, 0f));
            AnimatorState fall = stateMachine.AddState("Fall", new Vector3(520f, 320f, 0f));
            idle.motion = idleClip;
            jump.motion = jumpClip;
            fall.motion = fallClip;
            stateMachine.defaultState = idle;

            AnimatorStateTransition idleToJump = CreateTransition(idle, jump);
            idleToJump.AddCondition(AnimatorConditionMode.If, 0f, "IsJumping");

            AnimatorStateTransition idleToFall = CreateTransition(idle, fall);
            idleToFall.AddCondition(AnimatorConditionMode.If, 0f, "IsFalling");

            AnimatorStateTransition jumpToFall = CreateTransition(jump, fall);
            jumpToFall.AddCondition(AnimatorConditionMode.If, 0f, "IsFalling");

            AnimatorStateTransition jumpToIdle = CreateTransition(jump, idle);
            jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");
            jumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFalling");

            AnimatorStateTransition fallToJump = CreateTransition(fall, jump);
            fallToJump.AddCondition(AnimatorConditionMode.If, 0f, "IsJumping");

            AnimatorStateTransition fallToIdle = CreateTransition(fall, idle);
            fallToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsFalling");
            fallToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsJumping");

            AnimatorStateTransition anyToJump = stateMachine.AddAnyStateTransition(jump);
            ConfigureTransition(anyToJump);
            anyToJump.AddCondition(AnimatorConditionMode.If, 0f, "IsJumping");

            AnimatorStateTransition anyToFall = stateMachine.AddAnyStateTransition(fall);
            ConfigureTransition(anyToFall);
            anyToFall.AddCondition(AnimatorConditionMode.If, 0f, "IsFalling");

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

            PlayerAnimatorStateDriver stateDriver = player.GetComponent<PlayerAnimatorStateDriver>();
            if (stateDriver == null)
            {
                stateDriver = player.AddComponent<PlayerAnimatorStateDriver>();
                EditorUtility.SetDirty(stateDriver);
            }

            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
