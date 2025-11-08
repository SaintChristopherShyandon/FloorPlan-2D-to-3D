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

#if UNITY_EDITOR || (!UNITY_WEBGL && (UNITY_ANDROID || UNITY_IOS))
            if (imagePicker != null)
            {
                imagePicker.Completed += (string path) =>
                {
                    StartCoroutine(LoadImage(path));
                };
            }
#endif
        }

        public void OnPressShowPicker()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // --- WEBGL FILE PICKER ---
            Application.ExternalEval(@"
                var input = document.createElement('input');
                input.type = 'file';
                input.accept = 'image/*';
                input.onchange = function(e) {
                    var file = e.target.files[0];
                    var reader = new FileReader();
                    reader.onload = function(event) {
                        var base64Data = event.target.result.split(',')[1];
                        SendMessage('" + nameof(ImageLoader) + @"', 'OnWebGLImagePicked', base64Data);
                    };
                    reader.readAsDataURL(file);
                };
                input.click();
            ");
#else
            // --- EDITOR / MOBILE PICKER ---
            if (imagePicker != null)
                imagePicker.Show("Select Image (Select 1 per Floor)", "unimgpicker");
            else
                Debug.LogWarning("ImagePicker not assigned in Inspector!");
#endif
        }

        // WebGL callback
        public void OnWebGLImagePicked(string base64)
        {
            byte[] bytes = System.Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            texturesToUpload.Add(tex);

            Debug.Log("✅ Image added (WebGL). Total: " + texturesToUpload.Count);

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

            if (unityWebRequestTexture.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load texture: " + unityWebRequestTexture.error);
            }
            else
            {
                var texture = ((DownloadHandlerTexture)unityWebRequestTexture.downloadHandler).texture;
                if (texture != null)
                {
                    texturesToUpload.Add(texture);
                    Debug.Log("✅ Image added (Editor/Mobile). Total: " + texturesToUpload.Count);

                    if (menuBeforeLoading != null) menuBeforeLoading.SetActive(false);
                    if (menuAfterLoading != null) menuAfterLoading.SetActive(true);
                }
            }
        }
    }
}
