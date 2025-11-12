using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Kakera; // Untuk mengakses ImageLoader

public class Analyze : MonoBehaviour
{ 
    // Variabel UI
    public GameObject loadingMenu;
    public GameObject errorMenu;
    public GameObject errorMenuLoading;
    public GameObject startGameMenu;

    // Data statis untuk Builder
    public static string data;
    
    // Variabel Coroutine
    UnityWebRequest res;
    
    // Alamat API
    private const string API_URL = "https://Shyandon-floorplan3d-api.hf.space/predict";
    
    // Fungsi yang dipanggil dari UI untuk memulai unggahan
    public void sendToServerLoadedImage()
    {
        StartCoroutine(UploadAllImages());
    }

    // Coroutine untuk mengunggah semua gambar
    IEnumerator UploadAllImages()
    {
        // 1. Pastikan ada gambar yang dipilih
        if (ImageLoader.texturesToUpload.Count == 0)
        {
            Debug.LogError("Tidak ada gambar untuk diunggah. Pilih gambar terlebih dahulu.");
            errorMenu.SetActive(true);
            yield break;
        }

        loadingMenu.SetActive(true);
        
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        
        // 2. Iterasi dan konversi semua gambar ke form data
        int imageIndex = 0;
        foreach (Texture2D texture in ImageLoader.texturesToUpload)
        {
            // Pastikan format RGB24
            Texture2D snap = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            snap.SetPixels(texture.GetPixels());
            snap.Apply();
            
            // Konversi ke PNG bytes
            byte[] bytes = snap.EncodeToPNG();
            
            // Tambahkan ke form data
            formData.Add(new MultipartFormFileSection("image", bytes, $"floor_{imageIndex}.png", "image/png"));
            imageIndex++;
            
            // Hapus tekstur sementara dari memori
            Object.Destroy(snap);
        }
        
        // 3. Kirim request ke server
        UnityWebRequest www = UnityWebRequest.Post(API_URL, formData);
        yield return www.SendWebRequest();
        res = www;

        // 4. Reset list textures
        ImageLoader.texturesToUpload.Clear();
    }
    
    private void Update()
    {
        if (res != null)
        {
            if (res.isNetworkError || res.isHttpError)
            {
                // Error
                loadingMenu.SetActive(false);
                errorMenuLoading.SetActive(true);
                Debug.LogError("API Error: " + res.error + "\nResponse: " + res.downloadHandler.text);
            }
            else
            {
                // Sukses
                Debug.Log("Form upload complete! Response received.");
                data = res.downloadHandler.text; 
                
                startGameMenu.SetActive(true);
                gameObject.SetActive(false);
            }
            res = null;
        }
    }
    
    // Panggil ini untuk memulai scene/proses Building
    public void LoadScene()
    {
        GameObject builder = new GameObject("Building_Builder");
        builder.AddComponent<Builder>();
    }
}
