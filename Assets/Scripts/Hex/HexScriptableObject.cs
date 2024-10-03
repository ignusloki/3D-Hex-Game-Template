using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Hex", menuName = "Hex", order = 1)]
public class HexScriptableObject : ScriptableObject {
    
    public Biome type;
    public int travelCost;
    public bool passable;

}
