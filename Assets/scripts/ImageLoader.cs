using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Kakera;

namespace Kakera
{
    public class ImageLoader : MonoBehaviour
    {
        [SerializeField] private Unimgpicker imagePicker;

        public static List<Texture2D> texturesToUpload = new List<Texture2D>();

        public Analyze analyzeScript; 
        public GameObject menuAfterLoading; 
        public GameObject menuBeforeLoading;

        void Awake()
        {
            texturesToUpload.Clear();

#if !UNITY_WEBGL
            // Listener hanya untuk Android/iOS
            imagePicker.Completed += (string path) =>
            {
                StartCoroutine(LoadImage(path));
            };
#endif
        }

        public void OnPressShowPicker()
        {
#if UNITY_WEBGL
            // Jalankan JavaScript file picker di WebGL
            Application.ExternalEval(@"
                var input = document.createElement('input');
                input.type = 'file';
                input.accept = 'image/*';
                input.onchange = function(e) {
                    var file = e.target.files[0];
                    var reader = new FileReader();
                    reader.onload = function(event) {
                        var base64Data = event.target.result.split(',')[1];
                        SendMessage('" + gameObject.name + @"', 'OnWebGLImagePicked', base64Data);
                    };
                    reader.readAsDataURL(file);
                };
                input.click();
            ");
#else
            // Android/iOS native picker
            imagePicker.Show("Select Image", "unimgpicker");
#endif
        }

        // Callback dari JavaScript (WebGL)
        public void OnWebGLImagePicked(string base64)
        {
            byte[] bytes = System.Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            texturesToUpload.Add(tex);

            Debug.Log("Image added (WebGL). Total images: " + texturesToUpload.Count);

            if (menuBeforeLoading != null) menuBeforeLoading.SetActive(false);
            if (menuAfterLoading != null) menuAfterLoading.SetActive(true);
        }

        public void OnPressSendAllImages()
        {
            if (texturesToUpload.Count > 0)
            {
                analyzeScript.sendToServerLoadedImage();
            }
            else
            {
                Debug.LogError("No images selected yet.");
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
                if (texture != null)
                {
                    texturesToUpload.Add(texture);
                    Debug.Log("Image added (Mobile). Total images: " + texturesToUpload.Count);
                    if (menuBeforeLoading != null) menuBeforeLoading.SetActive(false);
                    if (menuAfterLoading != null) menuAfterLoading.SetActive(true);
                }
            }
        }
    }
}
