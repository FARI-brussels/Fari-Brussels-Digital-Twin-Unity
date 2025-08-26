using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO.Compression;
using TransitRealtime;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public enum ApiDataType
{
    Json,
    GTFS,
    GTFS_RT
}
public class ApiClientv2
{
    private string apiKey;


    //Instantiate ApiClient with Api Key
    public ApiClientv2(string key = "")
    {
        apiKey = key;
    }

    //Get Data funcion (Api url, callback function, dataType {Json, GTFS, GTFS_RT...}, if GTFS.zip = path where you want to store it)
    public IEnumerator GetApiData<T>(string url, Action<T> callback, ApiDataType dataType, string localGTFSFolder = "")
    {
        switch (dataType)
        {
            //is GTFS classic
            case ApiDataType.GTFS:
                yield return DownloadGTFS(url, callback as Action<Dictionary<string, string>>, localGTFSFolder);
                break;
            //is GTFS_RT
            case ApiDataType.GTFS_RT:
                yield return FetchBinaryGTFS(url, callback);
                break;
            //is Json
            case ApiDataType.Json:
            default:
                yield return FetchJsonDataThreaded(url, callback);
                break;
        }
    }

    private IEnumerator FetchJsonDataThreaded<T>(string url, Action<T> callback)
    {
        //Call response and info
        string jsonResponse = null;
        bool requestCompleted = false;
        bool requestSuccess = false;

        //
        // 1: Dowload JSON (Main Thread)

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            //is apiKey ?
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }

            //Send Web request
            yield return request.SendWebRequest();

            //Error check
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"API Error: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
                //Callback with no data
                callback(default);
                yield break;
            }
            else
            {
                //Get JSON response in text format
                jsonResponse = request.downloadHandler.text;
                //Bool Success
                requestSuccess = true;
            }
        }

        //If request nok then return default
        if (!requestSuccess)
        {
            callback(default);
            yield break;
        }

        //
        // 2 : Deserialization in other thread != main thread

        T deserializedData = default(T);
        bool deserializationCompleted = false;
        bool deserializationSuccess = false;

        // Multi thread deserialization
        Task.Run(() =>
        {
            try
            {
                //Settings for deserialization
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    //Choose converter (LineString or Point or other define in GeoGeometry.cs) 
                    Converters = new List<JsonConverter> { new GeoGeometryConverter() }
                };
                //Deserialization of ALL the data (Big operation) 
                deserializedData = JsonConvert.DeserializeObject<T>(jsonResponse, settings);
                deserializationSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Erreur de parsing JSON: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                deserializationSuccess = false;
            }
            finally
            {
                deserializationCompleted = true;
            }
        });

        // Wait until deserialization finished 
        yield return new WaitUntil(() => deserializationCompleted);

        // Callback data (Main data) 
        if (deserializationSuccess)
        {
            callback(deserializedData);
        }
        else
        {
            callback(default);
        }
    }
    
    private IEnumerator FetchBinaryGTFS<T>(string url, Action<T> callback)
    {
        Debug.Log("GTFS-RT Parser");

        // 1 : Dowload GTFS-Binary
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);
                //request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            //If request NSuccess then callback null 
            if (request.result != UnityWebRequest.Result.Success)
            {

                Debug.LogError($" API Error : {request.error}");
                callback(default);
            }
            //Else request Susccess then get byte data
            else
            {
                //GTFS-RT = Protobuf = Binary Data
                try
                {
                    //Store binary data
                    byte[] binaryData = request.downloadHandler.data;
                    if (binaryData == null || binaryData.Length == 0)
                    {
                        Debug.LogError("Binary data empty");
                        callback(default);
                    }

                    //Parse data with GTFS-RT/protobuf data parser from google
                    FeedMessage feed = FeedMessage.Parser.ParseFrom(binaryData);

                    if (feed == null)
                    {
                        Debug.LogError("FeedMessage null after parsing");
                        callback(default);
                        yield break;
                    }

                    var geoJson = ConvertFeedToGeoJson(feed);

                    callback((T)(object)geoJson);
                }
                catch (Google.Protobuf.InvalidProtocolBufferException protobufEx)
                {
                    // If data is not in good format
                    Debug.LogError($"Protobuf Error: {protobufEx.Message}");
                    Debug.LogError($"Data: {System.Convert.ToBase64String(request.downloadHandler.data)}");
                    callback(default);
                }
                catch (Exception e)
                {
                    //Other Exception for debugging
                    Debug.LogError($"Erreur parsing Protobuf: {e.Message}");
                    callback(default);
                }

            }
        }
    }
    private JsonCollection<JsonFeature<GeoGeometryPoint>> ConvertFeedToGeoJson(FeedMessage feed)
    {
        var features = new List<JsonFeature<GeoGeometryPoint>>();

        foreach (var entity in feed.Entity)
        {
            if (entity.Vehicle?.Vehicle != null && entity.Vehicle?.Position != null)
            {
                var properties = new JObject
                {
                    ["vehicle_id"] = entity.Vehicle.Vehicle.Id,
                    ["wheelchair_accessible"] = entity.Vehicle.Vehicle.HasWheelchairAccessible,
                    ["trip_id"] = entity.Vehicle.Trip.TripId
                };

                var geometry = new GeoGeometryPoint
                {
                    type = "Point",
                    coordinates = new List<double>
                {
                    entity.Vehicle.Position.Longitude,
                    entity.Vehicle.Position.Latitude
                }
                };

                var feature = new JsonFeature<GeoGeometryPoint>
                {
                    id = entity.Vehicle.Vehicle.Id,
                    type = "Feature",
                    properties = properties,
                    geometry = geometry
                };

                features.Add(feature);
            }
        }

        return new JsonCollection<JsonFeature<GeoGeometryPoint>>
        {
            type = "FeatureCollection",
            features = features
        };
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


