using UnityEngine;

public class MenuDisplayController : MonoBehaviour
{
    [SerializeField] private GameObject[] menuDisplay;

    public void DisplayMenu(int menuIndex)
    {
        if (menuDisplay == null || menuDisplay.Length == 0)
        {
            Debug.LogWarning("MenuDisplay array is empty or not assigned.", this);
            return;
        }

        for (int i = 0; i < menuDisplay.Length; i++)
        {
            GameObject menu = menuDisplay[i];
            if (menu == null)
            {
                Debug.LogWarning($"Menu at index {i} is not assigned.", this);
                continue;
            }

            menu.SetActive(i == menuIndex);
        }
    }
}
