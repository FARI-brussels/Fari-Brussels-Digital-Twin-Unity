using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SimulationRequester : MonoBehaviour
{
    // Exemple d'appel POST vers ton serveur local
    public void StartSimulation()
    {
        StartCoroutine(PostSimulation("http://localhost:8000/simulate", "{ \"param\": \"valeur\" }"));
    }

    private IEnumerator PostSimulation(string url, string jsonBody)
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Réponse: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Erreur POST: " + request.error);
            }
        }
    }
}
