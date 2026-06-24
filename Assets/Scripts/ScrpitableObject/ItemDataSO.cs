using UnityEngine;

[CreateAssetMenu(fileName = "New_Item", menuName = "ScriptableObjects/Item")]
public class ItemDataSO : ScriptableObject
{
    public string name;
    public Sprite icon;
    public ItemType type; 
    public int maxStack = 100;
}

public enum ItemType
{
    Ore,
    Ingot,
    Component,
    Tool,
    Consumable,
    Building
}