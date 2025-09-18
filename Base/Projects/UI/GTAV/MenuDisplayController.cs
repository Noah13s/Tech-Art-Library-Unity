using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuDisplayController : MonoBehaviour
{
    [SerializeField] private GameObject[] menuDisplay;

    public void DisplayMenu(int menuIndex)
    {
        for (int i = 0; i < menuDisplay.Length; i++)
        {
            if (i == menuIndex)
                menuDisplay[i].SetActive(true);
            else
                menuDisplay[i].SetActive(false);
        }
    }
}
