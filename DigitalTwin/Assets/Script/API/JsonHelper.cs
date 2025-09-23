using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

public class JsonHelper
{
    ///
    /// Deserialization multi-threaded of JSON file
    ///

    public static Task<T> DeserializeAsync<T>(string json, List<JsonConverter> converters = null)
    {
        //Run other thread
        return Task.Run(() =>
        {
            try
            {
                //Settings rules
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    //Define a converter
                    Converters = converters ?? new List<JsonConverter>()
                };
                //Deserialization
                return JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (Exception e)
            {
                Debug.LogError($"Erreur de parsing JSON: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                return default;
            }
        });
    }
}
