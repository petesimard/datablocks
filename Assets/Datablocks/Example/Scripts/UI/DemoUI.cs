using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
///     Demo UI for Datablocks
/// </summary>
public class DemoUI : MonoBehaviour
{
    public Text itemName;
    public Text itemSellPrice;
    public Text itemDescription;
    public Text itemType;

    public Transform spawnPoint;

    private Mob spawnedMob;

    public CanvasGroup lootWindow;
    public GridLayoutGroup lootGrid;
    public CanvasGroup tooltipWindow;

    public void SpawnMob(MobDatablock mobDatablock)
    {
        if(spawnedMob)
            Destroy(spawnedMob.gameObject);

        var mobModel = Instantiate(mobDatablock.model) as GameObject;

        mobModel.GetComponent<Renderer>().material.color = mobDatablock.modelColor;

        spawnedMob = mobModel.GetComponent<Mob>();
        spawnedMob.datablock = mobDatablock;
        spawnedMob.demoUI = this;

        mobModel.transform.SetParent(spawnPoint);
        mobModel.transform.localPosition = Vector3.zero;

        CloseLootWindow();
    }

    internal void OnMobKilled()
    {
        lootWindow.alpha = 1;
        lootWindow.interactable = true;
        lootWindow.blocksRaycasts = true;

        // Clear existing
        foreach (Transform child in lootGrid.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (var lootItem in spawnedMob.loot)
        {
            var lootBoxGameObject = new GameObject(lootItem.name, new Type[] { typeof(RawImage)});
            lootBoxGameObject.transform.SetParent(lootGrid.transform);

            var lootBox = lootBoxGameObject.AddComponent<LootBox>();

            var button = lootBoxGameObject.AddComponent<Button>();
            button.onClick.AddListener(new UnityAction(() => ShowTooltip(lootBox)));

            lootBox.SetItem(lootItem);
        }

        Destroy(spawnedMob.gameObject);
    }

    public void CloseLootWindow()
    {
        lootWindow.alpha = 0;
        lootWindow.interactable = false;
        lootWindow.blocksRaycasts = false;

        tooltipWindow.alpha = 0;
        tooltipWindow.interactable = false;
        tooltipWindow.blocksRaycasts = false;

    }

    private void ShowTooltip(LootBox lootBox)
    {
        tooltipWindow.alpha = 1;
        tooltipWindow.interactable = true;
        tooltipWindow.blocksRaycasts = true;

        itemName.text = lootBox.GetItem().name;
        itemSellPrice.text = new Money(lootBox.GetItem().sellPrice).ToString();
        itemDescription.text = ItemDescription(lootBox.GetItem());
        itemType.text = lootBox.GetItem().itemType.ToString();
    }

    private string ItemDescription(ItemDatablock item)
    {
        string desc = "";
        switch (item.itemType)
        {
            case ItemDatablock.ItemType.Armor:
            case ItemDatablock.ItemType.Helm:
            case ItemDatablock.ItemType.Shield:
            case ItemDatablock.ItemType.Shoulder:
                desc += "Armor: " + item.armor;
                break;
            case ItemDatablock.ItemType.Arrow:
                desc += "Damage: " + item.damage;
                break;
            case ItemDatablock.ItemType.Bag:
                desc += "Bag slots: " + item.bagSize;
                break;
            case ItemDatablock.ItemType.OneHandedWeapon:
            case ItemDatablock.ItemType.TwoHandedWeapon:
            case ItemDatablock.ItemType.Bow:
                desc += "Damage: " + item.damage;
                break;
        }

        desc += "\n" + item.flavorText;

        return desc;
    }
}