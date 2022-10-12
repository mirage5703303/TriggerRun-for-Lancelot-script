using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Opsive.Shared.Input;
using Opsive.Shared.Integrations.InputSystem;
using Opsive.Shared.StateSystem;
using Opsive.Shared.Utility;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Character.Effects;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Items;
using Opsive.UltimateCharacterController.Traits;
using Opsive.UltimateCharacterController.ThirdPersonController.Character;
using Opsive.UltimateCharacterController.FirstPersonController.Items;
using Opsive.UltimateCharacterController.AddOns.Multiplayer.Character;
using Opsive.UltimateCharacterController.AddOns.Multiplayer.PhotonPun.Traits;
using Opsive.UltimateCharacterController.ThirdPersonController.Items;
using Opsive.UltimateCharacterController.Objects.ItemAssist;
using Opsive.UltimateCharacterController.Items.Actions;

namespace IEdgeGamesEditor {

    using IEdgeGames;
    
    /// <summary>
    /// TODO: This class need to be fixed, this is just a fast implementation
    /// </summary>
    public static class CharacterManagerEditor {

        [MenuItem("Tools/Pause Editor Application")]
        static void PauseEditorApp() {
            EditorApplication.isPaused = true;
        }

        [MenuItem("Tools/Setup Character")]
        static void SetupCharacter() {
            if (Application.isPlaying) {
                Debug.LogWarning("Cannot set up character while application is playing.");
                return;
            }

            var selection = Selection.activeGameObject;
            var template = EditorResources.Load<GameObject>("IEdgeShooterGame_Character_Items.prefab");
            Animator animator;

            if (!selection || !(animator = selection.GetComponent<Animator>()) || !template)
                return;

            EditorUtility.SetDirty(selection);

            //Object.DestroyImmediate(selection.GetComponent<UnityInput>());
            //selection.AddComponent<UnityInputSystem>();

            var myName = selection.name;
            var myInput = selection.GetComponent<UnityInput>();
            var templateInput = template.GetComponent<UnityInput>();

            myInput.LookSensitivity = templateInput.LookSensitivity;
            myInput.SmoothLookSteps = templateInput.SmoothLookSteps;
            myInput.SmoothLookWeight = templateInput.SmoothLookWeight;
            myInput.SmoothExponent = templateInput.SmoothExponent;
            myInput.LookAccelerationThreshold = templateInput.LookAccelerationThreshold;
            myInput.States = new List<State>(templateInput.States).ToArray();

            //var playerInput = selection.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            //playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Opsive/Shared/Input/InputSystem/CharacterInput.inputactions");

            //if (!selection.GetComponent<RemotePlayerPerspectiveMonitor>()) {
                Object.DestroyImmediate(selection.GetComponent<CharacterHealth>());
                Object.DestroyImmediate(selection.GetComponent<PunAttributeMonitor>());
                Object.DestroyImmediate(selection.GetComponent<AttributeManager>());
                Object.DestroyImmediate(selection.GetComponent<CharacterRespawner>());
                Object.DestroyImmediate(selection.GetComponent<Inventory>());
                Object.DestroyImmediate(selection.GetComponent<CharacterLayerManager>());
                Object.DestroyImmediate(selection.GetComponent<ItemSetManager>());
                //Object.DestroyImmediate(selection.GetComponent<UnityEngine.InputSystem.PlayerInput>());

                selection.AddComponent<TRCharacter>();
                selection.AddComponent<AttributeManager>().GetCopyOf(template.GetComponent<AttributeManager>());
            //selection.AddComponent<PerspectiveMonitor>();
            //selection.AddComponent<PerspectiveMonitor>().GetCopyOf(template.GetComponent<PerspectiveMonitor>());
            selection.AddComponent<PunAttributeMonitor>();
            //selection.AddComponent<RemotePlayerPerspectiveMonitor>().GetCopyOf(template.GetComponent<RemotePlayerPerspectiveMonitor>());
            //selection.AddComponent<RemotePlayerPerspectiveMonitor>();

            var healthEx = selection.AddComponent<CharacterHealthEx>().GetCopyOf(template.GetComponent<CharacterHealthEx>());
                selection.AddComponent<CharacterRespawnerEx>().GetCopyOf(template.GetComponent<CharacterRespawnerEx>());
                selection.AddComponent<Inventory>().GetCopyOf(template.GetComponent<Inventory>());
                selection.AddComponent<CharacterLayerManager>().GetCopyOf(template.GetComponent<CharacterLayerManager>());
                selection.AddComponent<ItemSetManager>().GetCopyOf(template.GetComponent<ItemSetManager>());
                //selection.AddComponent<UnityEngine.InputSystem.PlayerInput>().GetCopyOf(template.GetComponent<UnityEngine.InputSystem.PlayerInput>());

                var hitBox = healthEx.Hitboxes.FirstOrDefault();
                var hitBoxFieldInfo = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                hitBoxFieldInfo.SetValue(hitBox, animator.GetBoneTransform(HumanBodyBones.Head).GetComponent<Collider>());
            //}

            // Setup invisible material for URP
            /*var invisibleMaterial = EditorResources.Load<Material>("InvisibleShadowCaster_URP.mat");

            EditorApplication.delayCall += () => {
                selection.GetComponent<PerspectiveMonitor>().InvisibleMaterial = invisibleMaterial;
                selection.GetComponent<RemotePlayerPerspectiveMonitor>().InvisibleMaterial = invisibleMaterial;
            };*/

            var itemsInstance = (PrefabUtility.InstantiatePrefab(EditorResources.Load<GameObject>("IEdgeShooterGame_Character_Items.prefab")) as GameObject).transform;
            var itemsInstance_l = itemsInstance.Find("Items_l");
            var itemsInstance_r = itemsInstance.Find("Items_r");
            var itemsInstance_i = itemsInstance.Find("Items");


            var shoulder_r = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            var hand_l = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var hand_r = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var lowerLeg_r = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);


            itemsInstance.parent = selection.transform;
            PrefabUtility.UnpackPrefabInstance(itemsInstance.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);


            itemsInstance.Find("Attachment_l").name = "Attachment";
            itemsInstance.Find("Arrow Attachment_r").name = "Arrow Attachment";

            if (!shoulder_r.Find("Holster")) itemsInstance.Find("Holster").parent = shoulder_r;
            if (!hand_l.Find("Attachment")) itemsInstance.Find("Attachment").parent = hand_l;
            if (!hand_r.Find("Arrow Attachment")) itemsInstance.Find("Arrow Attachment").parent = hand_r;


            SetParentAndReset(hand_l, hand_l.Find("Attachment"));
            SetParentAndReset(hand_r, hand_r.Find("Arrow Attachment"));


            // First person items
            /*var fpsItems_main_l = itemsInstance.Find("First Person Objects/Items_main_fps_l");
            var fpsItems_main_r = itemsInstance.Find("First Person Objects/Items_main_fps_r");
            //var fpsItems_l = itemsInstance.Find("First Person Objects/Items_fps_l");
            //var fpsItems_r = itemsInstance.Find("First Person Objects/Items_fps_r");
            var ch = selection.transform.Find("First Person Objects").GetComponentInChildren<CharacterHierarchy>();*/
            //Transform parent;

            //if (parent = ch.slor_l.transform)
                /*fpsItems_main_l.GetChilds().ToList().ForEach(child => child.parent = ch.slor_l.transform);

            //if (parent = fpsObjects.FirstOrDefault(c => c.arms == FPSArms.Both).hand_r.Find("Items"))
            Debug.LogWarning(fpsItems_main_r.GetChilds().ToList().cou);
                fpsItems_main_r.GetChilds().ToList().ForEach(child => child.parent = ch.slor_r.transform);*/

            /*if (parent = fpsObjects.FirstOrDefault(c => c.arms == FPSArms.Both).hand_l.Find("Items"))
                fpsItems_l.GetChilds().ToList().ForEach(child => child.parent = parent);

            if (parent = fpsObjects.FirstOrDefault(c => c.arms == FPSArms.Both).hand_r.Find("Items"))
                fpsItems_r.GetChilds().ToList().ForEach(child => child.parent = parent);*/


            // Third person items
            itemsInstance_l.GetChilds().ToList().ForEach(child => {
                var items = hand_l.Find("Items");

                if (items.Find(child.name))
                    child = items.Find(child.name);

                if (child.name == "FragGrenade" || child.name == "Bow")
                    SetParentAndReset(items, child);
                else
                    child.parent = items;
            });

            itemsInstance_r.GetChilds().ToList().ForEach(child => {
                var items = hand_r.Find("Items");

                if (items.Find(child.name))
                    child = items.Find(child.name);

                if (child.name == "Fireball" || child.name == "ParticleStream" || child.name == "Ricochet" || child.name == "Heal" || child.name == "Teleport")
                    SetParentAndReset(items, child);
                else
                    child.parent = items;
            });


            // Item definitions
            ParentItemDefinitions(selection.transform, hand_l, hand_r, lowerLeg_r, itemsInstance_i);

            var fpsItems_main_l = itemsInstance.Find("First Person Objects/Items_l");
            var fpsItems_main_r = itemsInstance.Find("First Person Objects/Items_r");
            var ch = selection.transform.Find("First Person Objects").GetComponentInChildren<CharacterHierarchy>();
            ch.slor_l.gameObject.GetChilds().DestroyImmediate();
            ch.slor_r.gameObject.GetChilds().DestroyImmediate();
            fpsItems_main_l.GetChilds().ToList().ForEach(child => child.parent = ch.slor_l.transform);
            fpsItems_main_r.GetChilds().ToList().ForEach(child => child.parent = ch.slor_r.transform);


            Object.DestroyImmediate(itemsInstance.gameObject);
            selection.name = myName;
        }

        //[MenuItem("Tools/Setup Character Items")]
        static void SetupCharacterItems() {
            var animator = Selection.activeGameObject.GetComponent<Animator>();
            var hand_l = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var hand_r = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var lowerLeg_r = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            ParentItemDefinitions(Selection.activeGameObject.transform, hand_l, hand_r, lowerLeg_r, Selection.activeGameObject.transform.Find("Items"), false);
        }

        static void ParentItemDefinitions(Transform selection, Transform hand_l, Transform hand_r, Transform lowerLeg_r, Transform itemsInstance, bool parentItems = true) {
            var myItems = selection.Find("Items");
            var arms = selection.GetComponentInChildren<CharacterHierarchy>();
            //var arms = ch.FirstOrDefault(a => a.arms == FPSArms.Both);
            //var arms_l = ch.FirstOrDefault(a => a.arms == FPSArms.Left);
            //var arms_r = ch.FirstOrDefault(a => a.arms == FPSArms.Right);

            itemsInstance.GetChilds().ToList().ForEach(child => {
                if (myItems.Find(child.name))
                    child = myItems.Find(child.name);

                switch (child.name) {
                    /*case "Bow":
                        var arrowAttachment = hand_r.Find("Arrow Attachment");
                        var firePoint = arrowAttachment.childCount > 0 ? arrowAttachment.GetChild(0) : null;
                        var wp = child.GetComponent<ThirdPersonShootableWeaponProperties>();

                        if (!firePoint) {
                            wp.FirePointLocation = SetParentAndReset(arrowAttachment, new GameObject("Fire Point").transform);
                            var wpInfo = wp.GetType().GetField("m_FirePointAttachmentLocation", BindingFlags.Instance | BindingFlags.NonPublic);
                            wpInfo.SetValue(wp, arrowAttachment);
                        }
                        break;*/

                    case "AssaultRifle":
                    case "SniperRifle":
                        child.GetComponent<ThirdPersonShootableWeaponProperties>().ReloadableClipAttachment = hand_l;
                        child.GetComponent<FirstPersonShootableWeaponProperties>().ReloadableClipAttachment = arms.hand_l;
                        child.GetComponent<FirstPersonPerspectiveItem>().Object = arms.gameObject;
                        break;

                    case "RightFragGrenade":
                        child.GetComponent<ThirdPersonGrenadeItemProperties>().PinAttachmentLocation = hand_l;
                        child.GetComponent<FirstPersonGrenadeItemProperties>().PinAttachmentLocation = arms.hand_l;
                        child.GetComponent<FirstPersonPerspectiveItem>().Object = arms.gameObject;
                        break;

                    case "LeftFragGrenade":
                    case "Shotgun":
                    case "Knife":
                    case "Fireball":
                    case "Ricochet":
                    case "Flashlight":
                        child.GetComponent<FirstPersonPerspectiveItem>().Object = arms.gameObject;
                        break;

                    case "Sword":
                    case "Katana":
                        var weaponProperty = child.GetComponents<ThirdPersonMeleeWeaponProperties>().Where(p => p.Hitboxes.Length > 1).FirstOrDefault();
                        var hitBox = weaponProperty.Hitboxes[1];
                        var hbFieldInfo = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                        hbFieldInfo.SetValue(hitBox, lowerLeg_r.GetComponent<Collider>());

                        var fpsWP = child.GetComponents<FirstPersonMeleeWeaponProperties>().Where(p => p.Hitboxes.Length > 1).FirstOrDefault();
                        hitBox = fpsWP.Hitboxes[1];
                        hbFieldInfo = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                        hbFieldInfo.SetValue(hitBox, lowerLeg_r.GetComponent<Collider>());

                        child.GetComponent<FirstPersonPerspectiveItem>().Object = arms.gameObject;
                        //child.GetComponent<FirstPersonPerspectiveItem>().VisibleItem.GetComponent<ShieldCollider>().Shield = child.GetComponent<Shield>();
                        break;

                    case "RightPistol":
                        var fpsPI = child.GetComponent<FirstPersonPerspectiveItem>();
                        fpsPI.Object = arms.gameObject;

                        hbFieldInfo = typeof(FirstPersonPerspectiveItem).GetField("m_AdditionalControlObjects", BindingFlags.Instance | BindingFlags.NonPublic);
                        hbFieldInfo.SetValue(fpsPI, new GameObject[] { arms.gameObject });
                        break;

                    case "Bow":
                    case "LeftPistol":
                    case "Shield":
                        if (child.name == "Bow") {
                            var arrowAttachment = new GameObject("Arrow Attachment").transform;
                            //arrowAttachment.transform.parent = arms.hand_r;
                            SetParentAndReset(arms.hand_r, arrowAttachment);
                            var firePoint = new GameObject("Fire Point").transform;
                            SetParentAndReset(arrowAttachment, firePoint);

                            var bpswp = child.GetComponent<FirstPersonShootableWeaponProperties>();
                            var wpInfo = bpswp.GetType().GetField("m_FirePointAttachmentLocation", BindingFlags.Instance | BindingFlags.NonPublic);
                            wpInfo.SetValue(bpswp, arrowAttachment);
                            bpswp.FirePointLocation = firePoint;
                        }

                        child.GetComponent<FirstPersonPerspectiveItem>().Object = arms.gameObject;
                        //child.GetComponent<FirstPersonPerspectiveItem>().VisibleItem.GetComponent<ShieldCollider>().Shield = child.GetComponent<Shield>();
                        break;

                    case "RocketLauncher":
                        var attachmentRL = new GameObject("Attachment").transform;
                        attachmentRL.parent = arms.hand_l;
                        attachmentRL.localPosition = Vector3.zero;
                        attachmentRL.localRotation = Quaternion.identity;
                        child.GetComponent<FirstPersonShootableWeaponProperties>().ReloadProjectileAttachment = attachmentRL;
                        child.GetComponent<FirstPersonPerspectiveItem>().Object = arms.gameObject;
                        break;

                    case "ParticleStream":
                    case "Heal":
                    case "Teleport":
                        child.GetComponent<ThirdPersonMagicItemProperties>().OriginLocation = selection.transform;
                        child.GetComponent<FirstPersonMagicItemProperties>().OriginLocation = selection.transform;
                        child.GetComponent<FirstPersonPerspectiveItem>().Object = arms.gameObject;
                        break;
                }

                if (parentItems)
                    SetParentAndReset(myItems, child);
            });
        }

        [MenuItem("Tools/Setup Locomotion")]
        static void SetupLocomotion() {
            if (Application.isPlaying) {
                Debug.LogWarning("Cannot set up character while application is playing.");
                return;
            }

            var selection = Selection.gameObjects;
            UltimateCharacterLocomotion locomotion;

            foreach (var character in selection) {
                if (!character || !(locomotion = character.GetComponent<UltimateCharacterLocomotion>()))
                    return;

                EditorUtility.SetDirty(character);
                locomotion.AddDefaultSmoothedBones();
            }
        }

        [MenuItem("Tools/Weapon Properties/Reset TPS-FPS Non Firing")]
        static void ResetTPSFPSNonFiring() {
            var source = Selection.activeGameObject;

            if (source.name != "Items")
                return;

            EditorUtility.SetDirty(source.transform.parent);

            var nonFiringWeapons = new [] {
                "LeftFragGrenade",
                "Fireball",
                "ParticleStream",
                "Ricochet",
                "Heal",
                "Teleport",
            };

            foreach (Transform t in source.transform) {
                if (!nonFiringWeapons.Contains(t.name))
                    continue;

                var tps_weapon = t.GetComponent<ThirdPersonPerspectiveItem>().Object.transform;
                var fps_weapon = t.GetComponent<FirstPersonPerspectiveItem>().VisibleItem.transform;

                tps_weapon.localPosition = Vector3.zero;
                tps_weapon.localRotation = Quaternion.identity;
                tps_weapon.localScale = Vector3.one;

                fps_weapon.localPosition = Vector3.zero;
                fps_weapon.localRotation = Quaternion.identity;
                fps_weapon.localScale = Vector3.one;
            }
        }

        [MenuItem("Tools/Weapon Properties/Fix TPS-FPS Sound Properties")]
        static void FixTPSFPSWeaponSoundProperties() {
            var sources = Selection.gameObjects;

            if (sources == null || sources.Length == 0)
                return;

            foreach (var character in sources) {
                EditorUtility.SetDirty(character);

                foreach (Transform t in character.transform.Find("Items")) {
                    var tps_weapon = t.GetComponent<ThirdPersonPerspectiveItem>().Object;
                    var fps_weapon = t.GetComponent<FirstPersonPerspectiveItem>().VisibleItem;
                    var tps_audioSource = tps_weapon ? tps_weapon.transform.GetComponent<AudioSource>() : null;
                    var fps_audioSource = fps_weapon ? fps_weapon.transform.GetComponent<AudioSource>() : null;

                    if (tps_audioSource)
                        tps_audioSource.playOnAwake = false;
                    if (fps_audioSource)
                        fps_audioSource.playOnAwake = false;
                }
            }
        }

        [MenuItem("Tools/Weapon Properties/Copy TPS Properties")]
        static void CopyTPSWeaponProperties() {
            var source = Selection.gameObjects.FirstOrDefault(s => !PrefabUtility.IsPartOfAnyPrefab(s));
            var target = Selection.gameObjects.FirstOrDefault(s => PrefabUtility.IsPartOfAnyPrefab(s));

            if (!source || !target)
                return;

            EditorUtility.SetDirty(target);

            var animatorSource = source.transform.parent.GetComponent<Animator>();
            var animatorTarget = target.transform.parent.GetComponent<Animator>();

            try {
                var holsterSource = animatorSource.GetBoneTransform(HumanBodyBones.RightShoulder).Find("Holster");
                var holsterTarget = animatorTarget.GetBoneTransform(HumanBodyBones.RightShoulder).Find("Holster");

                if (holsterSource && holsterTarget) {
                    holsterTarget.localPosition = holsterSource.localPosition;
                    holsterTarget.localRotation = holsterSource.localRotation;
                    holsterTarget.localScale = holsterSource.localScale;
                }
            }
            catch { }

            try {
                var arrowPointSource = animatorSource.GetBoneTransform(HumanBodyBones.RightHand).Find("Arrow Attachment");
                var arrowPointTarget = animatorTarget.GetBoneTransform(HumanBodyBones.RightHand).Find("Arrow Attachment");

                if (arrowPointSource && arrowPointTarget) {
                    arrowPointTarget.localPosition = arrowPointSource.localPosition;
                    arrowPointTarget.localRotation = arrowPointSource.localRotation;
                    arrowPointTarget.localScale = arrowPointSource.localScale;
                }
            }
            catch { }

            foreach (Transform t in source.transform) {
                var sourceTPSWeapon = t.GetComponent<ThirdPersonPerspectiveItem>();
                var targetTPSWeapon = target.transform.Find(t.name).GetComponent<ThirdPersonPerspectiveItem>();

                var sw = sourceTPSWeapon.Object.transform;
                var tw = targetTPSWeapon.Object.transform;

                if (sw.parent.name == "Holster")
                    continue;

                tw.localPosition = sw.localPosition;
                tw.localRotation = sw.localRotation;
                tw.localScale = sw.localScale;

                var ik_target_source = sourceTPSWeapon.NonDominantHandIKTarget;
                var ik_target_target = targetTPSWeapon.NonDominantHandIKTarget;

                if (ik_target_source && ik_target_target) {
                    ik_target_target.localPosition = ik_target_source.localPosition;
                    ik_target_target.localRotation = ik_target_source.localRotation;
                    ik_target_target.localScale = ik_target_source.localScale;
                }
            }
        }

        [MenuItem("Tools/Weapon Properties/Copy FPS Properties")]
        static void CopyFPSWeaponProperties() {
            var source = Selection.gameObjects.FirstOrDefault(s => !PrefabUtility.IsPartOfAnyPrefab(s));
            var target = Selection.gameObjects.FirstOrDefault(s => PrefabUtility.IsPartOfAnyPrefab(s));

            if (!source || !target)
                return;

            EditorUtility.SetDirty(target);

            try {
                var arrowPointSource = source.transform.Find("Teleport").GetComponent<FirstPersonPerspectiveItem>().VisibleItem.transform.parent.parent.Find("Arrow Attachment");
                var arrowPointTarget = target.transform.Find("Teleport").GetComponent<FirstPersonPerspectiveItem>().VisibleItem.transform.parent.parent.Find("Arrow Attachment");

                if (arrowPointSource && arrowPointTarget) {
                    arrowPointTarget.localPosition = arrowPointSource.localPosition;
                    arrowPointTarget.localRotation = arrowPointSource.localRotation;
                    arrowPointTarget.localScale = arrowPointSource.localScale;
                }
            }
            catch { }

            foreach (Transform t in source.transform) {
                var sourceFPSWeapon = t.GetComponent<FirstPersonPerspectiveItem>();
                var targetFPSWeapon = target.transform.Find(t.name).GetComponent<FirstPersonPerspectiveItem>();

                targetFPSWeapon.PivotPositionOffset = sourceFPSWeapon.PivotPositionOffset;
                targetFPSWeapon.PivotRotationOffset = sourceFPSWeapon.PivotRotationOffset;

                var sw = sourceFPSWeapon.VisibleItem.transform;
                var tw = targetFPSWeapon.VisibleItem.transform;

                tw.localPosition = sw.localPosition;
                tw.localRotation = sw.localRotation;
                tw.localScale = sw.localScale;
            }
        }

        [MenuItem("Tools/FPS Weapon Look At Center")]
        static void FPSWeaponLookAtCenter() {
            var selection = Selection.activeGameObject;

            if (!selection)
                return;

            selection.transform.rotation = Camera.main.transform.rotation;
        }

        [MenuItem("Tools/FPS Weapon Center")]
        static void FPSWeaponCenter() {
            var selection = Selection.activeGameObject;

            if (!selection)
                return;

            selection.transform.position = Camera.main.transform.position;
        }

        static Transform SetParentAndReset(Transform parent, Transform target) {
            target.parent = parent;
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            return target;
        }






        [MenuItem("Tools/Weapon Properties/Setup Default FPS Presets")]
        static void SetupDefaultFPSPresets() {
            var sources = Selection.gameObjects;

            if (sources == null || sources.Length == 0)
                return;

            foreach (var character in sources) {
                EditorUtility.SetDirty(character);

                var items = character.transform.Find("Items");
                var AimAssaultRifle_FPS = AssetDatabase.LoadAssetAtPath<Preset>("Assets/_TriggeRun/Presets/AimAssaultRifle_FPS.asset");
                var RunAssaultRifle_FPS = AssetDatabase.LoadAssetAtPath<Preset>("Assets/_TriggeRun/Presets/RunAssaultRifle_FPS.asset");
                var RunRocketLauncher_FPS = AssetDatabase.LoadAssetAtPath<Preset>("Assets/_TriggeRun/Presets/RunRocketLauncher_FPS.asset");
                var RunKatana_FPS = AssetDatabase.LoadAssetAtPath<Preset>("Assets/_TriggeRun/Presets/RunKatana_FPS.asset");
                var RunSword_FPS = AssetDatabase.LoadAssetAtPath<Preset>("Assets/_TriggeRun/Presets/RunSword_FPS.asset");


                var g = items.Find("RightFragGrenade").GetComponent<FirstPersonPerspectiveItem>();
                g.PivotPositionOffset = new Vector3(.3f, g.PivotPositionOffset.y, -.05f);
                g.PivotRotationOffset = new Vector3(0f, -66.67f, 0f);

                SetFPSWeaponState(items.Find("AssaultRifle"), "Aim", AimAssaultRifle_FPS);
                SetFPSWeaponState(items.Find("AssaultRifle"), "Run", RunAssaultRifle_FPS);
                SetFPSWeaponState(items.Find("RocketLauncher"), "Run", RunRocketLauncher_FPS);
                SetFPSWeaponState(items.Find("Katana"), "Run", RunKatana_FPS);
                SetFPSWeaponState(items.Find("Sword"), "Run", RunSword_FPS);
            }
        }

        static void SetFPSWeaponState(Transform weaponTransform, string stateName, Preset preset) {
            var weapon = weaponTransform.GetComponent<FirstPersonPerspectiveItem>();

            foreach (var s in weapon.States)
                if (s.Name == stateName)
                    s.Preset = preset;
        }
    }
}
