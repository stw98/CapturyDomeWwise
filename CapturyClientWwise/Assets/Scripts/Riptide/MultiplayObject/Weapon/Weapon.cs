using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : ScriptableObject, IMultiplayObject
{
    public ushort Id { get; set; }
    public string Name { get; set; }
    private int WeaponId { get; set; }
    private Sprite AimSprite { get; set; }
}
