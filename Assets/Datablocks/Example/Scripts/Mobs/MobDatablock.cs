using System.Collections.Generic;
using UnityEngine;

[DatablockCategory("Example")]
public class MobDatablock : Datablock
{
    public float attack;
    public float defense;

    public List<ItemDatablock> lootTable;
    public GameObject model;

    [DatablockNull("model")]
    public Color modelColor;
}