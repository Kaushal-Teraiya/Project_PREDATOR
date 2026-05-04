using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    private WeaponRuntimeInstance currentWeapon;
    private TextMeshProUGUI ammoText;

    void Awake()
    {
        ammoText = GetComponent<TextMeshProUGUI>();
    }

    public void SetWeapon(WeaponRuntimeInstance weaponRuntimeInstance)
    {
        currentWeapon = weaponRuntimeInstance;
    }

    public void UpdateAmmo()
    {
        if (ammoText == null || currentWeapon == null)
        {
            Debug.Log("[AmmoUI] Ammo text or currentWeapon is Null.");
            return;
        }
        string currentAmmo = currentWeapon.currentAmmo.ToString();
        string currentMag = currentWeapon.remainingMagazines.ToString();
        ammoText.text = currentAmmo + " / " + currentMag;
    }

    void Update()
    {
        if (currentWeapon == null)
        {
            Debug.Log("[AmmoUI] Current weapon is null inside update");
            return;
        }

        UpdateAmmo();
    }
}
