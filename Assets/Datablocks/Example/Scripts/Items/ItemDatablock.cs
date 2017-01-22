using System;
using System.Collections.Generic;
using UnityEngine;

[DatablockCategory("Example")]
public class ItemDatablock : Datablock
{
    public enum ArmorType
    {
        Cloth,
        Leather,
        Plate,
        Naked
    }

    public enum BindType
    {
        None,
        BindOnPickup,
        BindOnEquip
    }

    public enum ItemQuality
    {
        None,
        Damaged,
        Flawed,
        Average,
        Fine,
        Superior,
        Masterwork,
    }

    public enum ItemType
    {
        None,
        Armor,
        OneHandedWeapon,
        TwoHandedWeapon,
        Bow,
        Bag,
        LightSource,
        Reagent,
        Quest,
        Shield,
        Helm,
        Money,
        Arrow,
        Shoulder,
        Food,
        Potion,
        Clicky
    }

    public enum Slot
    {
        None,
        Armor,
        Weapon,
        OffHand,
        Light,
        Helm,
        UNUSED,
        Food,
        Shoulder,
        INVALID
    };

    public enum WeaponDamageType
    {
        Slash,
        Blunt,
        Pierce,
    }

    public enum WeaponType
    {
        LongSword,
        Dagger,
        Mace,
        Bow,
        Hammer,
        TwoHandedSword,
        ShortSword,
        Axe
    }

    public Texture Icon;

    public BindType bindType;
    
    public Slot equipmentSlot;
    [Order(1)] public string flavorText;


    public int sellPrice;

    public bool stackable;

    [DatablockBool("stackable")]
    public int maxStack;

    public ItemType itemType;

    [DatablockEnum("itemType", ItemType.Bag)]
    public int bagSize;


    #region Clickies and Potions
    [DatablockFoldout("Clicky")]

    [ItemDatablockItemType(new[] {ItemType.Potion, ItemType.Clicky})] public bool consumeOnUse;

    [ItemDatablockItemType(new[] {ItemType.Potion, ItemType.Clicky})] public string useInteractText;
    [ItemDatablockItemType(new[] {ItemType.Potion, ItemType.Clicky})] public string useText;
    [ItemDatablockItemType(new[] {ItemType.Potion, ItemType.Clicky})] public float useTime;

    #endregion

    #region Armor
    [DatablockFoldout("Armor")]
    [ItemDatablockItemType(new[] {ItemType.Armor, ItemType.Helm, ItemType.Shield, ItemType.Shoulder})] public int armor;

    [ItemDatablockItemType(new[] {ItemType.Armor})] public ArmorType armorType;

    #endregion

    #region Weapon

    [DatablockFoldout("Weapon")]
    [DatablockEnum("equipmentSlot", Slot.Weapon)] 
    public int damage;

    [ItemDatablockEquipmentSlot(Slot.Weapon)] public float weaponDelay;

    [ItemDatablockEquipmentSlot(Slot.Weapon)] public WeaponType weaponType;

    #endregion
}