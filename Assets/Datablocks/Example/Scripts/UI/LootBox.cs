using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LootBox : MonoBehaviour
{
    private RawImage image;
    private ItemDatablock item;

    void Awake()
    {
        image = GetComponent<RawImage>();
    }

    public void SetItem(ItemDatablock itemDatablock)
    {
        this.item = itemDatablock;
        image.texture = itemDatablock.Icon;
    }


    public ItemDatablock GetItem()
    {
        return item;
    }
}
