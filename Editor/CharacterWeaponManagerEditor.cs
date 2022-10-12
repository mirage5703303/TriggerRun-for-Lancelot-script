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
using Opsive.UltimateCharacterController.Character.Identifiers;

namespace IEdgeGamesEditor {

    using IEdgeGames;

    public static class CharacterWeaponManagerEditor {

        [MenuItem("Tools/Character/Restore Deafult Weapons")]
        static void RestoreDeafultWeapons() {
            var characters = Selection.gameObjects;

            if (characters == null || characters.Length == 0)
                return;

            foreach (var character in characters) {
                var itemContainer = character.transform.Find("Items");

                if (!itemContainer)
                    continue;

                var item = itemContainer.Find("AssaultRifle");
                var itemTPS = item.GetComponentInChildren<ThirdPersonPerspectiveItem>();
                var itemFPS = item.GetComponentInChildren<FirstPersonPerspectiveItem>();
                var tpsContainer = itemTPS.Object.transform.parent;
                var fpsContainer = itemFPS.VisibleItem.transform.parent;
                Transform tpsWeapon = null;
                Transform fpsWeapon = null;

                for (var i = 0; i < tpsContainer.childCount; i++) {
                    var child = tpsContainer.GetChild(i);

                    if (!child.gameObject.activeInHierarchy) {
                        tpsWeapon = child;
                        tpsWeapon.gameObject.SetActive(true);
                        break;
                    }
                }

                for (var i = 0; i < fpsContainer.childCount; i++) {
                    var child = fpsContainer.GetChild(i);

                    if (!child.gameObject.activeInHierarchy) {
                        fpsWeapon = child;
                        fpsWeapon.gameObject.SetActive(true);
                        break;
                    }
                }

                if (!tpsWeapon || !fpsWeapon)
                    continue;

                EditorUtility.SetDirty(character);

                var animator = character.GetComponent<Animator>();
                var fps = character.transform.Find("First Person Objects").GetComponentInChildren<CharacterHierarchy>();
                var tps_arrow_attachment = animator.GetBoneTransform(HumanBodyBones.RightHand).Find("Arrow Attachment");
                var fps_arrow_attachment = fps.transform.GetChilds(true).FirstOrDefault(c => c.name.StartsWith("Arrow Attachment"));
                var holster = animator.GetBoneTransform(HumanBodyBones.RightShoulder).Find("Holster");
                item = itemContainer.GetChilds().FirstOrDefault(c => (c.name.StartsWith(tpsWeapon.name) || (c.name.StartsWith("RightPistol") && tpsWeapon.name.StartsWith("Pistol"))) 
                                                                     && c.GetSiblingIndex() <= 18);

                itemTPS = item.GetComponentInChildren<ThirdPersonPerspectiveItem>();
                itemFPS = item.GetComponentInChildren<FirstPersonPerspectiveItem>();
                var itemShield = item.GetComponent<Shield>();
                var itemTPSProps = itemTPS.GetComponent<ThirdPersonShootableWeaponProperties>();
                var itemFPSProps = itemFPS.GetComponent<FirstPersonShootableWeaponProperties>();
                var itemTPSMeleeProps = itemTPS.GetComponents<ThirdPersonMeleeWeaponProperties>();
                var itemFPSMeleeProps = itemFPS.GetComponents<FirstPersonMeleeWeaponProperties>();

                itemTPS.Object.SetActive(false);
                itemFPS.VisibleItem.SetActive(false);
                itemTPS.Object = tpsWeapon.gameObject;
                itemFPS.VisibleItem = fpsWeapon.gameObject;

                itemTPS.NonDominantHandIKTarget = tpsWeapon.GetWeaponChild("IK Target");

                if (item.name.StartsWith("Bow")) {
                    itemTPSProps.FirePointLocation = tps_arrow_attachment.GetChild(0);
                    var wpInfo = itemTPSProps.GetType().GetField("m_FirePointAttachmentLocation", BindingFlags.Instance | BindingFlags.NonPublic);
                    wpInfo.SetValue(itemTPSProps, tps_arrow_attachment);

                    itemFPSProps.FirePointLocation = fps_arrow_attachment.GetChild(0);
                    wpInfo = itemFPSProps.GetType().GetField("m_FirePointAttachmentLocation", BindingFlags.Instance | BindingFlags.NonPublic);
                    wpInfo.SetValue(itemFPSProps, fps_arrow_attachment);
                }
                else if (item.name.StartsWith("RocketLauncher")) {
                    itemTPSProps.FirePointLocation = tpsWeapon.GetWeaponChild("Fire Point");
                    itemTPSProps.SmokeLocation = tpsWeapon.GetWeaponChild("Smoke");
                    itemTPSProps.ReloadProjectileAttachment = animator.GetBoneTransform(HumanBodyBones.LeftHand).Find("Attachment");

                    itemFPSProps.FirePointLocation = fpsWeapon.GetWeaponChild("Fire Point");
                    itemFPSProps.SmokeLocation = fpsWeapon.GetWeaponChild("Smoke");
                    itemFPSProps.ReloadProjectileAttachment = fps.hand_l.Find("Attachment");
                }
                else if (!item.name.StartsWith("Sword")) {
                    itemTPSProps.FirePointLocation = tpsWeapon.GetWeaponChild("Fire Point");
                    itemTPSProps.MuzzleFlashLocation = tpsWeapon.GetWeaponChild("Muzzle Flash");
                    itemTPSProps.ShellLocation = tpsWeapon.GetWeaponChild("Shell Eject Point");

                    itemFPSProps.FirePointLocation = fpsWeapon.GetWeaponChild("Fire Point");
                    itemFPSProps.MuzzleFlashLocation = fpsWeapon.GetWeaponChild("Muzzle Flash");
                    itemFPSProps.ShellLocation = fpsWeapon.GetWeaponChild("Shell Eject Point");

                    if (!item.name.StartsWith("Shotgun")) {
                        itemTPSProps.ReloadableClip = tpsWeapon.GetWeaponChild("Clip") ? tpsWeapon.GetWeaponChild("Clip") : itemTPSProps.FirePointLocation;
                        itemFPSProps.ReloadableClip = fpsWeapon.GetWeaponChild("Clip") ? fpsWeapon.GetWeaponChild("Clip") : itemFPSProps.FirePointLocation;
                    }

                    if (!item.name.StartsWith("Pistol") && !item.name.StartsWith("Shotgun")) {
                        itemTPSProps.ReloadableClipAttachment = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                        itemFPSProps.ReloadableClipAttachment = fps.hand_l;
                    }

                    if (item.name.StartsWith("AssaultRifle")) {
                        itemTPS.HolsterTarget = holster;
                        itemTPSProps.ScopeCamera = tpsWeapon.GetComponentsInChildren<Camera>().FirstOrDefault()?.gameObject;
                        itemFPSProps.ScopeCamera = fpsWeapon.GetComponentsInChildren<Camera>().FirstOrDefault()?.gameObject;
                    }
                }

                // TPS
                foreach (var meleeProp in itemTPSMeleeProps) {
                    if (item.name.StartsWith("Sword"))
                        meleeProp.TrailLocation = tpsWeapon.GetWeaponChild("Trail");

                    var collider = tpsWeapon.GetComponent<BoxCollider>();

                    if (collider) {
                        var hitbox = meleeProp.Hitboxes[0];
                        var hbfield = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                        hbfield.SetValue(hitbox, collider);
                    }
                    else
                        Debug.LogWarning($"No Box collider in weapon {tpsWeapon.name}", tpsWeapon);

                    if (meleeProp.Hitboxes.Length > 1) {
                        var hitbox = meleeProp.Hitboxes[1];
                        var hbfield = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                        hbfield.SetValue(hitbox, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).GetComponent<Collider>());
                    }
                }

                // FPS
                foreach (var meleeProp in itemFPSMeleeProps) {
                    if (item.name.StartsWith("Sword"))
                        meleeProp.TrailLocation = fpsWeapon.GetWeaponChild("Trail");

                    var collider = fpsWeapon.GetComponent<BoxCollider>();

                    if (collider) {
                        var hitbox = meleeProp.Hitboxes[0];
                        var hbfield = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                        hbfield.SetValue(hitbox, collider);
                    }
                    else
                        Debug.LogWarning($"No Box collider in weapon {fpsWeapon.name}", fpsWeapon);

                    if (meleeProp.Hitboxes.Length > 1) {
                        var hitbox = meleeProp.Hitboxes[1];
                        var hbfield = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                        hbfield.SetValue(hitbox, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).GetComponent<Collider>());
                    }
                }
            }
        }

        [MenuItem("Tools/Character/Extract Selected Weapons")]
        static void ExtractSelectedWeapons() {
            var sources = Selection.gameObjects;

            if (sources == null || sources.Length == 0)
                return;

            foreach (var i in sources) {
                if (i.transform.parent.name != "Items")
                    continue;

                var parent = new GameObject(i.transform.parent.parent.name).transform;
                var item = Object.Instantiate(i, parent);

                var itemFPS = item.GetComponent<FirstPersonPerspectiveItem>();
                var itemTPS = item.GetComponent<ThirdPersonPerspectiveItem>();

                if (!itemFPS && !itemTPS)
                    continue;

                EditorUtility.SetDirty(parent);

                Object.Instantiate(itemFPS.VisibleItem, parent);
                Object.Instantiate(itemTPS.Object, parent);
            }
        }

        [MenuItem("Tools/Character/Setup AssaultRifle Camera On Selected Characters")]
        static void SetupAssaultRifleCameraOnSelectedCharacters() {
            var characters = Selection.gameObjects;

            if (characters == null || characters.Length == 0)
                return;

            var fps_texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/_TriggeRun/Textures/FirstPersonScope_TR.renderTexture");
            var tps_texture = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/_TriggeRun/Textures/ThirdPersonScope_TR.renderTexture");

            foreach (var character in characters) {
                var items = character.transform.Find("Items");

                if (items)
                    EditorUtility.SetDirty(character);

                foreach (Transform t in items) {
                    var itemFPS = t.GetComponent<FirstPersonShootableWeaponProperties>();
                    var itemTPS = t.GetComponent<ThirdPersonShootableWeaponProperties>();

                    if (!t.name.StartsWith("AssaultRifle") || !itemFPS || !itemTPS)
                        continue;

                    if (!itemFPS.ScopeCamera || !itemTPS.ScopeCamera) {
                        Debug.LogWarning(t.name, t);
                        continue;
                    }

                    itemFPS.ScopeCamera.GetComponent<Camera>().targetTexture = fps_texture;
                    itemTPS.ScopeCamera.GetComponent<Camera>().targetTexture = tps_texture;
                }
            }
        }

        [MenuItem("Tools/Character/Add Selected Weapons Item Definitions")]
        static void AddSelectedWeaponsItemDefinitions() {
            var characters = Selection.gameObjects;

            if (characters == null || characters.Length == 0)
                return;

            var itemCollection = AssetDatabase.LoadAssetAtPath<ItemCollection>("Assets/Opsive/UltimateCharacterController/Demo/Inventory/DemoItemCollection.asset");

            foreach (var character in characters) {
                var items = character.transform.Find("Items");

                if (items)
                    EditorUtility.SetDirty(character);
                else
                    continue;

                foreach (Transform t in items) {
                    if (!t.name.Contains("(") && !t.name.EndsWith(")"))
                        continue;

                    var item = t.GetComponent<Item>();
                    var shootableWeapon = t.GetComponent<ShootableWeapon>();
                    //var tps_perspective = t.GetComponent<ThirdPersonPerspectiveItem>();
                    //var fps_perspective = t.GetComponent<FirstPersonPerspectiveItem>();
                    var tps_shootableWeapon = t.GetComponent<ThirdPersonShootableWeaponProperties>();
                    var fps_shootableWeapon = t.GetComponent<FirstPersonShootableWeaponProperties>();


                    var tps = t.GetComponent<ThirdPersonPerspectiveItem>();

                    if (tps && tps.HolsterTarget)
                        tps.HolsterTarget = null;


                    if (!item)
                        continue;

                    /*if (tps_perspective) {
                        if (tps_perspective.Object) {
                            tps_perspective.Object.transform.localPosition = Vector3.zero;
                            tps_perspective.Object.transform.localRotation = Quaternion.identity;
                        }
                        if (fps_perspective.VisibleItem) {
                            fps_perspective.VisibleItem.transform.localPosition = Vector3.zero;
                            fps_perspective.VisibleItem.transform.localRotation = Quaternion.identity;
                        }
                    }*/

                    if (tps_shootableWeapon) {
                        tps_shootableWeapon.LookSensitivity = -1f;
                        var m_LookSensitivity = fps_shootableWeapon.GetType().GetField("m_LookSensitivity", BindingFlags.Instance | BindingFlags.NonPublic);
                        m_LookSensitivity.SetValue(fps_shootableWeapon, -1f);
                    }

                    item.ItemDefinition = itemCollection.ItemTypes.FirstOrDefault(c => c.name == t.name);
                    item.DropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_TriggeRun/Prefabs/Pickups/Pickup_{t.name}.prefab");

                    if (!item.ItemDefinition)
                        Debug.LogWarning("No item definition for " + t.name, t);

                    if (!item.DropPrefab)
                        Debug.LogWarning("No drop prefab for " + t.name, t);

                    if (shootableWeapon) {
                        var itemBulletName = t.name.Before(" (") + $"Bullet ({t.name.Between("(", ")")})";
                        shootableWeapon.ConsumableItemDefinition = itemCollection.ItemTypes.FirstOrDefault(c => c.name == itemBulletName);

                        if (!shootableWeapon.ConsumableItemDefinition)
                            Debug.LogWarning("No consumable item definition for " + t.name, t);
                    }
                }
            }
        }

        static Transform GetWeaponChild(this Transform t, string name) {
            return t.GetChilds().FirstOrDefault(t => t.name.StartsWith(name));
        }

        [MenuItem("Tools/Character/Setup Unique Weapons On Selected Characters")]
        static void SetupUniqueWeaponsOnSelectedCharacters() {
            var characters = Selection.gameObjects;

            if (!characters.All(c => c.transform.Find("Items")))
                return;

            var itemPaths = AssetDatabase.GetAllAssetPaths()
                                         .Where(p => p.StartsWith("Assets/_TriggeRun/Editor Resources/Items/"))
                                         .OrderBy(p => p)
                                         .ToArray();

            if (itemPaths == null || itemPaths.Length == 0)
                return;

            foreach (var character in characters) {
                var characterItems = character.transform.Find("Items");

                if (!characterItems)
                    continue;

                var animator = character.GetComponent<Animator>();
                var holster = animator.GetBoneTransform(HumanBodyBones.RightShoulder).Find("Holster");
                var items_l = animator.GetBoneTransform(HumanBodyBones.LeftHand).Find("Items");
                var items_r = animator.GetBoneTransform(HumanBodyBones.RightHand).Find("Items");
                var fps = character.transform.Find("First Person Objects").GetComponentInChildren<CharacterHierarchy>();
                var tps_arrow_attachment = animator.GetBoneTransform(HumanBodyBones.RightHand).Find("Arrow Attachment");
                var fps_arrow_attachment = fps.transform.GetChilds(true).FirstOrDefault(c => c.name.StartsWith("Arrow Attachment"));
                var itemsetManager = character.GetComponent<ItemSetManager>();
                var itemCollection = AssetDatabase.LoadAssetAtPath<ItemCollection>("Assets/Opsive/UltimateCharacterController/Demo/Inventory/DemoItemCollection.asset");

                EditorUtility.SetDirty(character);

                foreach (var itemPath in itemPaths) {

                    /*// TODO: Remove old references 
                    var oldItem = character.transform.Find($"Items/{AssetDatabase.LoadAssetAtPath<GameObject>(itemPath).name}");

                    //if (!oldItem)
                        //oldItem = character.transform.Find("Items/Sword (Valkyrie)");

                    if (oldItem) {
                        var oldItemName = oldItem.name;

                        var itemTPS1 = oldItem.GetComponent<ThirdPersonPerspectiveItem>();
                        var itemFPS1 = oldItem.GetComponent<FirstPersonPerspectiveItem>();

                        if (itemTPS1.Object) Object.DestroyImmediate(itemTPS1.Object);
                        if (itemFPS1.VisibleItem) Object.DestroyImmediate(itemFPS1.VisibleItem);
                        Object.DestroyImmediate(oldItem.gameObject);
                    }

                    continue;*/

                    var itemParent = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(itemPath)) as GameObject;//Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(itemPath), characterItems);
                    var item = itemParent.GetComponentInChildren<Item>().transform;
                    var itemShield = item.GetComponent<Shield>();
                    var itemTPS = item.GetComponentInChildren<ThirdPersonPerspectiveItem>();
                    var itemFPS = item.GetComponentInChildren<FirstPersonPerspectiveItem>();
                    var itemTPSProps = itemTPS.GetComponent<ThirdPersonShootableWeaponProperties>();
                    var itemFPSProps = itemFPS.GetComponent<FirstPersonShootableWeaponProperties>();
                    var itemTPSMeleeProps = itemTPS.GetComponents<ThirdPersonMeleeWeaponProperties>();
                    var itemFPSMeleeProps = itemFPS.GetComponents<FirstPersonMeleeWeaponProperties>();

                    if (!item || !itemTPS || !itemFPS)
                        continue;

                    PrefabUtility.UnpackPrefabInstance(itemParent, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

                    if (itemFPS.AdditionalControlObjects.Length == 1)
                        itemFPS.AdditionalControlObjects[0] = fps.gameObject;

                    // Setup parents
                    itemParent.GetChilds().ToList().ForEach(c => {
                        c.name = itemParent.name.Replace("(Clone)", "").Trim();

                        if (c.GetComponent<Item>()) {
                            c.transform.parent = characterItems;

                            var itemCategory = itemsetManager.CategoryItemSets.FirstOrDefault(c => c.CategoryName == "Items");

                            if (c.name.StartsWith("Bow")) {
                                if (!itemCategory.ItemSetList.SelectMany(i => i.Slots).Where(s => s != null).ToList().Exists(s => s.name == c.name))
                                    itemCategory.ItemSetList.Add(new ItemSet {
                                        State = c.name.Before(" ("),
                                        Slots = new Opsive.Shared.Inventory.ItemDefinitionBase[] {
                                            null,
                                            itemCollection.ItemTypes.FirstOrDefault(i => i.name == c.name)
                                        }
                                    });
                            }
                            else {
                                if (!itemCategory.ItemSetList.SelectMany(i => i.Slots).Where(s => s != null).ToList().Exists(s => s.name == c.name))
                                    itemCategory.ItemSetList.Add(new ItemSet {
                                        State = c.name.Before(" ("),
                                        Slots = new Opsive.Shared.Inventory.ItemDefinitionBase[] {
                                            itemCollection.ItemTypes.FirstOrDefault(i => i.name == c.name),
                                            null
                                        }
                                    });
                            }
                        }
                        else {
                            if (c.name.StartsWith("Sword"))
                                c.GetComponent<ShieldCollider>().Shield = itemShield;

                            var isTPS = c.GetComponent<ThirdPersonObject>() != null;

                            if (c.name.StartsWith("Bow")) {
                                if (isTPS)
                                    c.transform.parent = items_l;
                                else
                                    c.transform.parent = fps.slor_l.transform;
                            }
                            else {
                                if (isTPS)
                                    c.transform.parent = items_r;
                                else
                                    c.transform.parent = fps.slor_r.transform;
                            }

                            if (isTPS) {
                                itemTPS.Object = c;
                                itemTPS.NonDominantHandIKTarget = c.transform.GetWeaponChild("IK Target");

                                if (c.name.StartsWith("AssaultRifle"))
                                    itemTPS.HolsterTarget = holster;

                                if (itemTPSProps) {
                                    if (c.name.StartsWith("Bow")) {
                                        if (tps_arrow_attachment.childCount == 0)
                                            new GameObject("Fire Point").transform.parent = tps_arrow_attachment;

                                        itemTPSProps.FirePointLocation = tps_arrow_attachment.GetChild(0);
                                        var wpInfo = itemTPSProps.GetType().GetField("m_FirePointAttachmentLocation", BindingFlags.Instance | BindingFlags.NonPublic);
                                        wpInfo.SetValue(itemTPSProps, tps_arrow_attachment);
                                    }
                                    else if (c.name.StartsWith("RocketLauncher")) {
                                        itemTPSProps.FirePointLocation = c.transform.GetWeaponChild("Fire Point");
                                        itemTPSProps.SmokeLocation = c.transform.GetWeaponChild("Smoke");
                                        itemTPSProps.ReloadProjectileAttachment = animator.GetBoneTransform(HumanBodyBones.LeftHand).Find("Attachment");
                                    }
                                    else {
                                        itemTPSProps.FirePointLocation = c.transform.GetWeaponChild("Fire Point");
                                        itemTPSProps.MuzzleFlashLocation = c.transform.GetWeaponChild("Muzzle Flash");
                                        itemTPSProps.ShellLocation = c.transform.GetWeaponChild("Shell Eject Point");

                                        if (!c.name.StartsWith("Shotgun"))
                                            itemTPSProps.ReloadableClip = c.transform.GetWeaponChild("Clip") ? c.transform.GetWeaponChild("Clip") : itemTPSProps.FirePointLocation;
                                        
                                        if (!c.name.StartsWith("Pistol") && !c.name.StartsWith("Shotgun"))
                                            itemTPSProps.ReloadableClipAttachment = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                                        
                                        if (c.name.StartsWith("AssaultRifle")) {
                                            itemTPSProps.ScopeCamera = c.GetComponentsInChildren<Camera>().FirstOrDefault()?.gameObject;

                                            if (!itemTPSProps.ScopeCamera) {
                                                var camera = new GameObject("Camera").AddComponent<Camera>();
                                                itemTPSProps.ScopeCamera = camera.gameObject;
                                                camera.transform.parent = c.transform;
                                                camera.gameObject.SetActive(false);
                                            }
                                        }
                                    }
                                }

                                foreach (var meleeProp in itemTPSMeleeProps) {
                                    if (c.name.StartsWith("Sword") && !meleeProp.TrailLocation)
                                        meleeProp.TrailLocation = c.transform.GetWeaponChild("Trail");

                                    var collider = c.GetComponent<BoxCollider>();

                                    if (!collider)
                                        collider = c.GetComponentsInChildren<BoxCollider>().FirstOrDefault();

                                    if (collider) {
                                        var hitbox = meleeProp.Hitboxes[0];
                                        var hbfield = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                                        hbfield.SetValue(hitbox, collider);
                                    }
                                    else
                                        Debug.LogWarning($"No Box collider in weapon {c.name}", c);

                                    if (meleeProp.Hitboxes.Length > 1) {
                                        var hitbox = meleeProp.Hitboxes[1];
                                        var hbfield = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                                        hbfield.SetValue(hitbox, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).GetComponent<Collider>());
                                    }
                                }
                            }
                            else {
                                itemFPS.Object = fps.gameObject;
                                itemFPS.VisibleItem = c;

                                if (itemFPSProps) {
                                    if (c.name.StartsWith("Bow")) {
                                        if (fps_arrow_attachment.childCount == 0)
                                            new GameObject("Fire Point").transform.parent = fps_arrow_attachment;

                                        itemFPSProps.FirePointLocation = fps_arrow_attachment.GetChild(0);
                                        var wpInfo = itemFPSProps.GetType().GetField("m_FirePointAttachmentLocation", BindingFlags.Instance | BindingFlags.NonPublic);
                                        wpInfo.SetValue(itemFPSProps, fps_arrow_attachment);
                                    }
                                    else if (c.name.StartsWith("RocketLauncher")) {
                                        itemFPSProps.FirePointLocation = c.transform.GetWeaponChild("Fire Point");
                                        itemFPSProps.SmokeLocation = c.transform.GetWeaponChild("Smoke");
                                        itemFPSProps.ReloadProjectileAttachment = fps.hand_l.Find("Attachment");
                                    }
                                    else {
                                        itemFPSProps.FirePointLocation = c.transform.GetWeaponChild("Fire Point");
                                        itemFPSProps.MuzzleFlashLocation = c.transform.GetWeaponChild("Muzzle Flash");
                                        itemFPSProps.ShellLocation = c.transform.GetWeaponChild("Shell Eject Point");

                                        if (!c.name.StartsWith("Shotgun"))
                                            itemFPSProps.ReloadableClip = c.transform.GetWeaponChild("Clip") ? c.transform.GetWeaponChild("Clip") : itemFPSProps.FirePointLocation;

                                        if (!c.name.StartsWith("Pistol") && !c.name.StartsWith("Shotgun"))
                                            itemFPSProps.ReloadableClipAttachment = fps.hand_l;

                                        if (c.name.StartsWith("AssaultRifle")) {
                                            itemFPSProps.ScopeCamera = c.GetComponentsInChildren<Camera>().FirstOrDefault()?.gameObject;

                                            if (!itemFPSProps.ScopeCamera) {
                                                var camera = new GameObject("Camera").AddComponent<Camera>();
                                                itemFPSProps.ScopeCamera = camera.gameObject;
                                                camera.transform.parent = c.transform;
                                                camera.gameObject.SetActive(false);
                                            }
                                        }
                                    }
                                }

                                foreach (var meleeProp in itemFPSMeleeProps) {
                                    if (c.name.StartsWith("Sword") && !meleeProp.TrailLocation)
                                        meleeProp.TrailLocation = c.transform.GetWeaponChild("Trail");

                                    var collider = c.GetComponent<BoxCollider>();

                                    if (!collider)
                                        collider = c.GetComponentsInChildren<BoxCollider>().FirstOrDefault();

                                    if (collider) {
                                        var hitbox = meleeProp.Hitboxes[0];
                                        var hbfield = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                                        hbfield.SetValue(hitbox, collider);
                                    }
                                    else
                                        Debug.LogWarning($"No Box collider in weapon {c.name}", c);

                                    if (meleeProp.Hitboxes.Length > 1) {
                                        var hitbox = meleeProp.Hitboxes[1];
                                        var hbfield = typeof(Hitbox).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                                        hbfield.SetValue(hitbox, animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).GetComponent<Collider>());
                                    }
                                }
                            }
                        }
                    });

                    Object.DestroyImmediate(itemParent);
                }
            }
        }
    }
}
