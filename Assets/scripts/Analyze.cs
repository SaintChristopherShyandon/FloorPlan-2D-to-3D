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
    public GameObject tips; // Asumsi tipsDone ada di MenuFunc
    
    // Data statis untuk Builder
    public static string data;
    
    // Variabel Coroutine
    UnityWebRequest res;
    
    // Alamat API
    private const string API_URL = "http://localhost:5000"; // Ganti jika Anda menghosting di tempat lain
    
    // Fungsi yang dipanggil dari UI untuk memulai unggahan
    public void sendToServerLoadedImage()
    {
        // Hanya satu fungsi yang digunakan (untuk gambar yang dimuat)
        StartCoroutine(UploadAllImages());
    }

    // Fungsi lama sendToServer() (untuk webcam) dihapus/diabaikan sesuai permintaan
    
    // Coroutine untuk mengunggah semua gambar
    IEnumerator UploadAllImages()
    {
        // 1. Persiapan
        if (ImageLoader.texturesToUpload.Count == 0)
        {
            Debug.LogError("Tidak ada gambar untuk diunggah. Pilih gambar terlebih dahulu.");
            errorMenu.SetActive(true);
            yield break;
        }

        loadingMenu.SetActive(true);
        tips.SetActive(true);
        
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        
        // 2. Iterasi dan Konversi Semua Gambar ke Form Data
        int imageIndex = 0;
        foreach (Texture2D texture in ImageLoader.texturesToUpload)
        {
            // PENTING: Membuat salinan tekstur untuk memastikan format yang benar (RGB24)
            Texture2D snap = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            snap.SetPixels(texture.GetPixels());
            snap.Apply();
            
            // Konversi ke PNG bytes
            byte[] bytes = snap.EncodeToPNG();
            
            // Tambahkan ke form data dengan kunci "image" (penting, sesuai dengan API Python)
            // API Python menggunakan getlist('image'), jadi kuncinya harus sama
            formData.Add(new MultipartFormFileSection("image", bytes, $"floor_{imageIndex}.png", "image/png"));
            imageIndex++;
            
            // Hapus tekstur sementara dari memori
            Object.Destroy(snap);
        }
        
        // 3. Kirim Permintaan
        UnityWebRequest www = UnityWebRequest.Post(API_URL, formData);
        
        yield return www.SendWebRequest();
        res = www; // Simpan referensi hasil

        // 4. Reset List Textures (Opsional, untuk unggahan berikutnya)
        ImageLoader.texturesToUpload.Clear();
    }
    
    private void Update()
    {
        // Asumsi MenuFunc.tipsDone adalah boolean yang benar ketika Tips selesai ditampilkan
        bool tipsDone = true; // Ganti ini dengan logika Tips yang benar jika perlu

        if (res != null && tipsDone)
        {
            if (res.isNetworkError || res.isHttpError)
            {
                // Penanganan Error
                loadingMenu.SetActive(false);
                errorMenuLoading.SetActive(true);
                Debug.LogError("API Error: " + res.error + "\nResponse: " + res.downloadHandler.text);
            }
            else
            {
                // Sukses
                Debug.Log("Form upload complete! Response received.");
                
                // Simpan JSON respon (berisi semua data lantai)
                data = res.downloadHandler.text; 
                
                // Transisi UI
                startGameMenu.SetActive(true);
                gameObject.SetActive(false);
                tips.SetActive(false);
            }
            res = null;
        }
    }
    
    // Panggil ini untuk memulai scene/proses Building
    public void LoadScene()
    {
        // Buat GameObject Builder dan pasang script Builder
        // (Ini meniru cara kerja SceneManager.LoadScene jika dilakukan dalam satu scene)
        GameObject builder = new GameObject("Building_Builder");
        builder.AddComponent<Builder>();
    }
}
