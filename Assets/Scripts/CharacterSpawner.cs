
using Unity.Netcode;
using UnityEngine;

public class CharacterSpawner : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterDatabase characterDatabase;
    public Transform[] spawnPoints;
    public override void OnNetworkSpawn()
    {
        if (!IsServer) { return; }

        foreach (var client in MatchplayNetworkServer.Instance.ClientData)
        {
            var character = characterDatabase.GetCharacterById(client.Value.characterId);
            if (character != null)
            {
                if (client.Value.characterId == 1)
                {
                    var characterInstance = Instantiate(character.GameplayPrefab, spawnPoints[0].position, Quaternion.identity);
                    characterInstance.SpawnAsPlayerObject(client.Value.clientId);
                }
                else if (client.Value.characterId == 2)
                {
                    var characterInstance = Instantiate(character.GameplayPrefab, spawnPoints[1].position, Quaternion.identity);
                    characterInstance.SpawnAsPlayerObject(client.Value.clientId);
                }
                else if (client.Value.characterId == 3)
                {
                    var characterInstance = Instantiate(character.GameplayPrefab, spawnPoints[2].position, Quaternion.identity);
                    characterInstance.SpawnAsPlayerObject(client.Value.clientId);
                }
                else if (client.Value.characterId == 4)
                {
                    var characterInstance = Instantiate(character.GameplayPrefab, spawnPoints[3].position, Quaternion.identity);
                    characterInstance.SpawnAsPlayerObject(client.Value.clientId);
                }
                //var spawnPos = new Vector3(Random.Range(0, 0f), 0f, Random.Range(0, 0f));
                //var characterInstance = Instantiate(character.GameplayPrefab, spawnPos, Quaternion.identity);
                //characterInstance.SpawnAsPlayerObject(client.Value.clientId);
            }
        }
    }
}
