using UnityEngine;

public class PlayerAmmoCarry : MonoBehaviour
{
    public bool CarryingAmmo { get; private set; }

    public void PickUp()
    {
        CarryingAmmo = true;
    }

    public void ConsumeAll()
    {
        CarryingAmmo = false;
    }
}