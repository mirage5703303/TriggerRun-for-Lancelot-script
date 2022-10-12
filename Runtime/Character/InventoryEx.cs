using System;
using System.Collections.Generic;
using System.Linq;
using Opsive.Shared.Inventory;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Items;
using Opsive.UltimateCharacterController.Items.Actions;
using Sirenix.OdinInspector;
using TrickCore;
using UnityEngine;

namespace IEdgeGames
{
    public class InventoryEx : Inventory
    {
        /// <summary>
        /// True if we only drop the active weapon on death
        /// </summary>
        public bool DropOnlyActiveWeapon;
        
        /// <summary>
        /// The slots we drop
        /// </summary>
        public List<int> DropableSlotIds;

        public List<ItemDefinitionBase> UndropableItemIdentifiers;
        
        public ItemDefinitionAmount[] ArchivedDefaultItemLoadout;
        private UltimateCharacterLocomotion _ucl;

        private void OnEnable()
        {
            _ucl = GetComponent<UltimateCharacterLocomotion>();
            Opsive.Shared.Events.EventHandler.RegisterEvent<Item, IItemIdentifier, int>(m_GameObject, "OnItemUseConsumableItemIdentifier", OnUseConsumableItemIdentifier);
        }

        private void OnDisable()
        {
            Opsive.Shared.Events.EventHandler.UnregisterEvent<Item, IItemIdentifier, int>(m_GameObject, "OnItemUseConsumableItemIdentifier", OnUseConsumableItemIdentifier);
        }

        private void OnUseConsumableItemIdentifier(Item arg1, IItemIdentifier arg2, int arg3)
        {
            if (arg3 == 0 && GetItemIdentifierAmount(arg2) == 0)
            {
                // swap weapon
                /*if (_ucl.ItemAbilities.FirstOrDefault(ability => ability is EquipNext) is { } equipNext)
                {
                    _ucl.TryStartAbility(equipNext, true, true);
                }*/
                var activeItem = GetActiveItem(arg1.SlotID);
                /*var weapon = activeItem.GetComponent<ShootableWeapon>();
                HasItem()
                var unequipItem = GetActiveItem(arg1.SlotID).ItemIdentifier;
                //UnequipItem(unequipItem, arg1.SlotID);
                var equipableItems = GetAllItemIdentifiers().Where(identifier => identifier != unequipItem && identifier.GetItemDefinition().GetItemCategory() is Category category && category.name is "Items").ToList();
                var itemToEquip = equipableItems.FirstOrDefault();
                if (itemToEquip != null) EquipItem(itemToEquip, arg1.SlotID, true);*/
                if (activeItem != null)
                {
                    RemoveItem(activeItem.ItemIdentifier, activeItem.SlotID, 1, false);
                    
                    /*var equipableItems = GetAllItemIdentifiers().Where(identifier => identifier != arg2 && identifier != activeItem.ItemIdentifier && GetItemIdentifierAmount(identifier) > 0).ToList();
                    var itemToEquip = equipableItems.FirstOrDefault();
                    if (itemToEquip != null) EquipItem(itemToEquip, arg1.SlotID, false);*/
                }
            }
        }

        [Button]
        public void DebugInventory()
        {
            var allItemIdentifiers = GetAllItemIdentifiers();
            if (allItemIdentifiers.Count > 0)
            {
                for (var index = 0; index < allItemIdentifiers.Count; index++)
                {
                    IItemIdentifier identifier = allItemIdentifiers[index];
                    for (int slotId = 0; slotId < SlotCount; slotId++)
                    {
                        var item = GetActiveItem(slotId);
                        if (item != null && item.ItemIdentifier == identifier)
                        {
                            Debug.Log($"x{GetItemIdentifierAmount(identifier)} ID{item.ItemIdentifier.ID} (Slot:{slotId})");
                        }
                    }
                }
            }
            else
            {
                Debug.Log("No inventory items");
            }
        }

        public override void LoadDefaultLoadout()
        {
            var player = GetComponent<TRCharacter>();
            async void HandleLoadingPlayerLoadout()
            {
                // wait until we have a preset set
                await UnityTrickTask.WaitUntil(() => player.CurrentPreset != null, 100);
                
                Debug.Log(player + ": Preset is set, give loadout");

                List<ItemDefinitionAmount> itemsToGive = new List<ItemDefinitionAmount>();
                
                player.CurrentPreset.ValidateDirtyCheck();
                var items = player.CurrentPreset.ItemIds
                    .Select(i => TriggeRunGameManager.Instance.ItemDefinitions.Find(definition => definition.id == i))
                    .Where(definition => definition != null).ToList();


                foreach (ItemDefinition item in items)
                {
                    itemsToGive.Add(new ItemDefinitionAmount(item.MainItem, 1));
                    if (item.AmmoItem != null) itemsToGive.Add(new ItemDefinitionAmount(item.AmmoItem, item.AmmoAmount));
                }

                foreach (ItemDefinitionAmount definitionAmount in itemsToGive)
                {
                    for (int i = 0; i < m_DefaultLoadout.Length; ++i) {
                        Pickup(definitionAmount.ItemIdentifier, definitionAmount.Amount, -1, true, false);
                        Debug.Log("Give weapon: " + definitionAmount.ItemDefinition.name + " x" + definitionAmount.Amount);
                    }
                }

                if (itemsToGive.Count == 0) base.LoadDefaultLoadout();
            }
            
            HandleLoadingPlayerLoadout();
        }

        protected override void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            enabled = false;
            
            if (DropOnlyActiveWeapon)
            {
                foreach (int slotId in DropableSlotIds)
                {
                    var activeItem = GetActiveItem(slotId);
                    if (activeItem != null && activeItem.ItemIdentifier is ItemType itemType && UndropableItemIdentifiers.All(itemIdentifier => activeItem.ItemIdentifier.ID != itemIdentifier.CreateItemIdentifier().ID))
                    {
                        DropItem(activeItem, 1, false, false);
                        Debug.Log($"[OnDeath] Drop item: {itemType.ID} - {itemType.name}");
                    }
                }
            }
            else
            {
                // just call the base, since it will handle the drop all weapons if it was toggled
                base.OnDeath(position, force, attacker);
            }
        }
    }
}