using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Kakera; // Pastikan ini sesuai dengan namespace Anda

namespace Kakera
{
    // Script ini bertanggung jawab untuk memuat gambar dari galeri/penyimpanan
    public class ImageLoader : MonoBehaviour
    {
        [SerializeField]
        // Pastikan Anda memiliki referensi ke script Unimgpicker di Inspector
        private Unimgpicker imagePicker;
        
        // STATIC: List untuk menyimpan semua tekstur yang diunggah
        public static List<Texture2D> texturesToUpload = new List<Texture2D>();
        
        // STATIC: Referensi ke Analyze script untuk memicu unggahan setelah selesai memilih
        public Analyze analyzeScript; 
        
        // Menu visual untuk interaksi
        public GameObject menuAfterLoading; 
        public GameObject menuBeforeLoading;

        void Awake()
        {
            // Pastikan List dikosongkan saat Awake
            texturesToUpload.Clear(); 

            // Listener yang dipanggil ketika gambar berhasil dipilih
            imagePicker.Completed += (string path) =>
            {
                StartCoroutine(LoadImage(path));
            };
        }

        // Panggil ini dari tombol UI untuk memulai proses pemilihan
        public void OnPressShowPicker()
        {
            imagePicker.Show("Select Image (Select 1 per Floor)", "unimgpicker");
        }

        // Panggil ini dari tombol UI untuk mengirim semua gambar yang telah dipilih
        public void OnPressSendAllImages()
        {
            if (texturesToUpload.Count > 0)
            {
                // Panggil coroutine untuk mengirim semua gambar
                analyzeScript.sendToServerLoadedImage();
            }
            else
            {
                Debug.LogError("No images selected yet.");
                // Tampilkan pesan error jika perlu
            }
        }
        
        private IEnumerator LoadImage(string path)
        {
            var url = "file://" + path;
            var unityWebRequestTexture = UnityWebRequestTexture.GetTexture(url);
            yield return unityWebRequestTexture.SendWebRequest();

            if (unityWebRequestTexture.isNetworkError || unityWebRequestTexture.isHttpError)
            {
                Debug.LogError("Failed to load texture url: " + url + " Error: " + unityWebRequestTexture.error);
            }
            else
            {
                var texture = ((DownloadHandlerTexture)unityWebRequestTexture.downloadHandler).texture;
                if (texture == null)
                {
                    Debug.LogError("Failed to load texture.");
                }
                else 
                {
                    // TAMBAHAN KRUSIAL: Tambahkan tekstur ke List
                    texturesToUpload.Add(texture);
                    Debug.Log("Image added. Total images: " + texturesToUpload.Count);
                    
                    // Contoh feedback visual (misalnya, ganti menu atau tampilkan jumlah file yang dipilih)
                    if (menuBeforeLoading != null) menuBeforeLoading.SetActive(false);
                    if (menuAfterLoading != null) menuAfterLoading.SetActive(true);
                }
            }
        }
    }
}
