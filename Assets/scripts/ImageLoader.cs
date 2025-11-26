using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Kakera;

namespace Kakera
{
    public class ImageLoader : MonoBehaviour
    {
        [Header("Image Picker")]
        [SerializeField] private Unimgpicker imagePicker;

        [Header("Carousel Settings")]
        public RectTransform carouselContent;  // Drag Content di ScrollView
        public GameObject imagePrefab;         // UI Image prefab

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
            if (imagePicker != null)
                imagePicker.Show("Select Image", "unimgpicker");
            else
                Debug.LogWarning("ImagePicker not assigned!");
#endif
        }

        // ------------ WEBGL ----------
        public void OnWebGLImagePicked(string base64)
        {
            byte[] bytes = System.Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);

            AddImageToList(tex);
        }

        // ------------ EDITOR / ANDROID / iOS ----------
        private IEnumerator LoadImage(string path)
        {
            var url = "file://" + path;
            var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed: " + request.error);
            }
            else
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                AddImageToList(tex);
            }
        }

        // ------------ CORE FUNCTION (ADD IMAGE + SHOW CAROUSEL) ----------
        private void AddImageToList(Texture2D tex)
        {
            texturesToUpload.Add(tex);

            // aktifkan UI
            if (menuBeforeLoading != null) menuBeforeLoading.SetActive(false);
            if (menuAfterLoading != null) menuAfterLoading.SetActive(true);

            // tampilkan dalam carousel
            AddImageToCarousel(tex);

            Debug.Log("Image added. Total: " + texturesToUpload.Count);
        }

        // ------------ CREATE UI IMAGE IN CAROUSEL ----------
        private void AddImageToCarousel(Texture2D tex)
        {
            GameObject imgObj = Instantiate(imagePrefab, carouselContent);
            Image uiImage = imgObj.GetComponent<Image>();

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            uiImage.sprite = sprite;
        }

        public void OnPressSendAllImages()
        {
            if (texturesToUpload.Count > 0)
                analyzeScript.sendToServerLoadedImage();
            else
                Debug.LogError("No images selected.");
        }
    }
}
