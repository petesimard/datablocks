# Datablocks

Unity game data manager for Unity using hierarchical structures. **Data Inheritance!**.

[Documentation](http://unitydatablocks.com/docs)

[Official Website](http://unitydatablocks.com/)

[Unity Forums](https://forum.unity3d.com/threads/released-datablocks-revolutionary-game-data-manager.321254/)

## Example Usage ##

The following code defines a Datablock that can then be used via the built in editor and used for data inheritance.

    public class SpellDatablock : Datablock
    {
        public int manaCost;
        public string description;
        public float castingTime;
    }