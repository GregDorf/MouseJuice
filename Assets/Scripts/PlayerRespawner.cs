using UnityEngine;
using System.Collections;

public class PlayerRespawner : MonoBehaviour
{
    [SerializeField] private float respawnTime = 2.0f;
    [SerializeField] private GameObject player;

    public void StartRespawnSequence()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        if (player != null)
        {
            player.SetActive(true);
            PlayerHP hpScript = player.GetComponent<PlayerHP>();
            if (hpScript != null) hpScript.ResetHP();
        }
    }
}