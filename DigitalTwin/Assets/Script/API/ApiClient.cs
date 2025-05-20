using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO.Compression;
using Google.Protobuf;
using TransitRealtime;

public class ApiClient
{
    private string apiKey;


    public ApiClient(string key = "")
    {
        apiKey = key; 
    }

    public IEnumerator GetApiData<T>(string url, Action<T> callback, bool isGTFS = false, string localGTFSFolder = "", bool isGTFS_RT = false)
    {
        if (isGTFS && !isGTFS_RT)
        {
            yield return DownloadGTFS(url, callback as Action<Dictionary<string, string>>, localGTFSFolder);
        }
        else if (!isGTFS && isGTFS_RT)
        {
            yield return FetchBinaryGTFS(url, callback as Action<FeedMessage>);
        }
        else
        {
            yield return FetchJsonData(url, callback);
        }
    }
    private IEnumerator FetchJsonData<T>(string url, Action<T> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                callback(default);// Return null in case of error
            }
            else
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new GeoGeometryConverter() }
                    };
                    T data = JsonConvert.DeserializeObject<T>(jsonResponse);
                    Debug.Log("Hello");
                    callback(data);

                }
                catch (Exception e)
                {
                    Debug.LogError($"Erreur de parsing JSON: {e.Message}");
                    Debug.LogError($"Stack trace: {e.StackTrace}");
                    callback(default);
                }
            }
        }
    }

    private IEnumerator DownloadGTFS(string url, Action<Dictionary<string, string>> callback, string localGTFSFolder)
    {
        Debug.Log("GTFS Parser");
        string filePath = Path.Combine(localGTFSFolder, "gtfs.zip");

        if (File.Exists(filePath))
        {
            Debug.Log("Le fichier GTFS existe déjà, extraction des données...");
            byte[] existingZipData = File.ReadAllBytes(filePath);
            Dictionary<string, string> extractedFiles = ExtractGTFSFromMemory(existingZipData);
            callback(extractedFiles);
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Erreur API: {request.error}");
                callback(null);
            }
            else
            {
                byte[] zipData = request.downloadHandler.data;
                // Extraction du fichier en mémoire
                Dictionary<string, string> extractedFiles = ExtractGTFSFromMemory(zipData);
                SaveGtfsZipEditor(zipData, localGTFSFolder);
                callback(extractedFiles);


            }
        }
    }
    private IEnumerator FetchBinaryGTFS(string url, Action<FeedMessage> callback)
    {
        Debug.Log("GTFS-RT Parser");
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);
                //request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {

                Debug.LogError($"Erreur API: {request.error}");
                callback(null);
            }
            else
            {
                try
                {
                    byte[] binaryData = request.downloadHandler.data;
                    if (binaryData == null || binaryData.Length == 0)
                    {
                        Debug.LogError("Les données binaires sont vides.");
                        callback(null);
                    }

                    FeedMessage feed = FeedMessage.Parser.ParseFrom(binaryData);
                    if (feed == null)
                    {
                        Debug.LogError("FeedMessage est null après le parsing.");
                        callback(null);
                        yield break;
                    }
                    callback(feed);
                }
                catch (Google.Protobuf.InvalidProtocolBufferException protobufEx)
                {
                    // Cette exception peut être levée si les données ne sont pas au bon format.
                    Debug.LogError($"Erreur Protobuf: {protobufEx.Message}");
                    Debug.LogError($"Données: {System.Convert.ToBase64String(request.downloadHandler.data)}");
                    callback(null);
                }
                catch (Exception e)
                {
                    // Capture toute autre exception pour déboguer plus précisément
                    Debug.LogError($"Erreur parsing Protobuf: {e.Message}");
                    callback(null);
                }

            }
        }
    }

    private Dictionary<string, string> ExtractGTFSFromMemory(byte[] zipData)
    {
        Dictionary<string, string> extractedFiles = new Dictionary<string, string>();

        using (MemoryStream zipStream = new MemoryStream(zipData))
        {
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".txt", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (StreamReader reader = new StreamReader(entry.Open()))
                        {
                            string fileContent = reader.ReadToEnd();
                            extractedFiles[entry.FullName] = fileContent;
                        }
                    }
                }
            }
        }
        return extractedFiles;
    }


    private void SaveGtfsZipEditor(byte[] zipData, string folderPath)
    {

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "gtfs.zip");
        File.WriteAllBytes(filePath, zipData);
        Debug.Log($"GTFS ZIP enregistré à : {filePath}");
    }
}
