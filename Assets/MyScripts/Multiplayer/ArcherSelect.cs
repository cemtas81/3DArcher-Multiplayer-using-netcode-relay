using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ArcherSelect : MonoBehaviour
{
    public GameObject ready;
    public Button[] characterButtons;
    private bool hasSelected = false;

    private void Start()
    {
        foreach (var button in characterButtons)
        {
            button.onClick.AddListener(() => SelectChar(button.transform.GetSiblingIndex()));
        }
    }

    public void SelectChar(int characterIndex)
    {
        if (hasSelected) return;

        if (NetworkCharacterSelection.Instance == null)
        {
            Debug.LogError("NetworkCharacterSelection instance not found!");
            return;
        }

        if (!NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("Not connected as a client!");
            return;
        }

        NetworkCharacterSelection.Instance.SetCharacterSelectionServerRpc(characterIndex);
        Debug.Log($"Character {characterIndex} selected by client {NetworkManager.Singleton.LocalClientId}");

        ready.SetActive(true);
        hasSelected = true;

        // Disable other buttons
        foreach (var button in characterButtons)
        {
            button.interactable = false;
        }
    }

    public void LoadScene(string sceneName)
    {
        if (ready.activeSelf)
        {
            SceneManager.LoadScene(sceneName,LoadSceneMode.Additive);
        }
    }
}