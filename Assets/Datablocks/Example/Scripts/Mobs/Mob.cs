using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Simple Mob class
/// </summary>
public class Mob : MonoBehaviour
{
    public MobDatablock datablock;

    public DemoUI demoUI;

    public List<ItemDatablock> loot;

    private void OnMouseDown()
    {
        Kill();
    }

    private void Kill()
    {
        loot = new List<ItemDatablock>(datablock.lootTable);

        demoUI.OnMobKilled();
    }
}