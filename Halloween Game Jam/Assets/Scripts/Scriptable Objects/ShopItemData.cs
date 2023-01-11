using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Shop Item Data", menuName = "ScriptableObjects/Froguelike/Shop Item Data", order = 1)]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public string description;

    public Sprite icon;
    
    public int maxLevel;

    public List<StatValue> statIncreaseList;
}
