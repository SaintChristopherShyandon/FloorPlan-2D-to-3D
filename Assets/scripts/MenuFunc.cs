using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class MenuFunc : MonoBehaviour
{
    public GameObject p;
    public Camera mainCamera;
    public Camera topCamera;
    public Button okButton;
    public Button retryButton;
    public InputField textField;
    public GameObject lineDrawer;
    public GameObject plane;
    public GameObject hamburgerbutton;
    public GameObject okButtonSpawner;
    public GameObject retryButtonSpawner;
    public static bool okButtonIsPressed;
    public Button spawnButton;
    public Camera painterCamera;
    public GameObject player;

    public void Quit()
    {
        Application.Quit();
    }

    public void spawn()
    {
        switchToTopCamera();
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void hamburger()
    {
        p.SetActive(!p.activeSelf);
    }

    public void switchToTopCamera()
    {
        mainCamera.enabled = false;
        topCamera.enabled = true;
    }

    public void switchToMainCamera()
    {
        mainCamera.enabled = true;
        topCamera.enabled = false;
    }

    public void restartLineDrawer()
    {
        lineDrawer.GetComponent<LineRenderer>().SetPosition(0, Vector3.zero);
        lineDrawer.GetComponent<LineRenderer>().SetPosition(1, Vector3.zero);
        lineDrawer.GetComponent<LineDrawer>().enabled = true;
    }

    public void retry()
    {
        okButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        textField.gameObject.SetActive(false);
        lineDrawer.GetComponent<LineRenderer>().SetPosition(0, Vector3.zero);
        lineDrawer.GetComponent<LineRenderer>().SetPosition(1, Vector3.zero);
        lineDrawer.GetComponent<LineDrawer>().enabled = true;
    }

    public void retrySpawner()
    {
        okButtonSpawner.SetActive(false);
        retryButtonSpawner.SetActive(false);
        GameObject.FindGameObjectWithTag("Spawner").GetComponent<Spawner>().postionSelected = false;
    }

    public void moveCameraAndPlane()
    {
        float x = topCamera.transform.position.x;
        float y = topCamera.transform.position.y;
        float z = topCamera.transform.position.z;
        x = x * (1 / Builder.originalScale) * (Builder.yScale);
        z = z * (1 / Builder.originalScale) * (Builder.xScale);
        topCamera.transform.position = new Vector3(x, y, z);

        x = plane.transform.position.x;
        y = plane.transform.position.y;
        z = plane.transform.position.z;
        x = x * (1 / Builder.originalScale) * (Builder.yScale);
        z = z * (1 / Builder.originalScale) * (Builder.xScale);
        plane.transform.position = new Vector3(x, y, z);
    }

    public void settingScale()
    {
        hamburgerbutton.GetComponent<Button>().enabled = false;
    }

    public void cancel()
    {
        okButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        textField.gameObject.SetActive(false);
        lineDrawer.gameObject.SetActive(false);
        okButtonSpawner.SetActive(false);
        retryButtonSpawner.SetActive(false);
        switchToMainCamera();
        moveCameraAndPlane();
        hamburgerbutton.GetComponent<Button>().enabled = true;
    }

    public void ok()
    {
        float distanceInGame = LineDrawer.getDistance();
        float distanceInReality = float.Parse(textField.text);
        distanceInGame = distanceInGame * (1 / Builder.xScale);
        Builder.xScale = distanceInReality / distanceInGame;
        Builder.yScale = distanceInReality / distanceInGame;

        GameObject[] walls = GameObject.FindGameObjectsWithTag("wall");
        foreach (GameObject parent in walls)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            WallMesh wallmesh = parent.GetComponent<WallMesh>();
            parent.transform.position = wallmesh.getCoordinates();
            child.transform.localScale = wallmesh.getScale();
        }

        GameObject[] doors = GameObject.FindGameObjectsWithTag("door");
        foreach (GameObject parent in doors)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            Door door = parent.GetComponent<Door>();
            parent.transform.position = door.getCoordinates();
            child.transform.localScale = door.getScale();
        }

        GameObject[] windows = GameObject.FindGameObjectsWithTag("window");
        foreach (GameObject parent in windows)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            Window window = parent.GetComponent<Window>();
            parent.transform.position = window.getCoordinates();
            child.transform.localScale = window.getScale();
        }

        GameObject[] fillers = GameObject.FindGameObjectsWithTag("wall1");
        foreach (GameObject parent in fillers)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            WallMesh wallmesh = parent.GetComponent<WallMesh>();
            parent.transform.position = wallmesh.getCoordinates();
            child.transform.localScale = wallmesh.getScale();
        }

        UpdateFloorPlanes();

        okButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        textField.gameObject.SetActive(false);
        lineDrawer.gameObject.SetActive(false);
        switchToMainCamera();
        moveCameraAndPlane();
        hamburgerbutton.GetComponent<Button>().enabled = true;
        StartGame();
    }

    public void resetScale()
    {
        float camerScale = Builder.xScale;
        Builder.xScale = Builder.originalScale;
        Builder.yScale = Builder.originalScale;

        GameObject[] walls = GameObject.FindGameObjectsWithTag("wall");
        foreach (GameObject parent in walls)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            WallMesh wallmesh = parent.GetComponent<WallMesh>();
            parent.transform.position = wallmesh.getCoordinates();
            child.transform.localScale = wallmesh.getScale();
        }

        GameObject[] doors = GameObject.FindGameObjectsWithTag("door");
        foreach (GameObject parent in doors)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            Door door = parent.GetComponent<Door>();
            parent.transform.position = door.getCoordinates();
            child.transform.localScale = door.getScale();
        }

        GameObject[] windows = GameObject.FindGameObjectsWithTag("window");
        foreach (GameObject parent in windows)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            Window window = parent.GetComponent<Window>();
            parent.transform.position = window.getCoordinates();
            child.transform.localScale = window.getScale();
        }

        GameObject[] fillers = GameObject.FindGameObjectsWithTag("wall1");
        foreach (GameObject parent in fillers)
        {
            GameObject child = parent.transform.GetChild(0).gameObject;
            WallMesh wallmesh = parent.GetComponent<WallMesh>();
            parent.transform.position = wallmesh.getCoordinates();
            child.transform.localScale = wallmesh.getScale();
        }

        UpdateFloorPlanes();

        float x = topCamera.transform.position.x;
        float y = topCamera.transform.position.y;
        float z = topCamera.transform.position.z;
        x = x * (1 / camerScale) * (Builder.originalScale);
        z = z * (1 / camerScale) * (Builder.originalScale);
        topCamera.transform.position = new Vector3(x, y, z);

        x = plane.transform.position.x;
        y = plane.transform.position.y;
        z = plane.transform.position.z;
        x = x * (1 / camerScale) * (Builder.originalScale);
        z = z * (1 / camerScale) * (Builder.originalScale);
        plane.transform.position = new Vector3(x, y, z);

        StartGame();
    }

    public void sayOkToPosition()
    {
        switchToMainCamera();
        Spawner spawnerComponent = GameObject.FindGameObjectWithTag("Spawner").GetComponent<Spawner>();
        okButtonIsPressed = true;
        okButtonSpawner.SetActive(false);
        retryButtonSpawner.SetActive(false);
    }

    public void StartGame()
    {
        spawnButton.onClick.Invoke();
    }

    public void Customize()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<CanCustomize>().enabled = true;
    }

    public void CancelCustomize()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<CanCustomize>().enabled = false;
    }

    public void changeToPainterCamera()
    {
        Vector3 playerPosition = player.transform.position;
        float playerYRotation = player.transform.rotation.eulerAngles.y;
        painterCamera.transform.rotation = Quaternion.Euler(90, playerYRotation, 0);
        painterCamera.transform.position = new Vector3(playerPosition.x, 6.5f, playerPosition.z);
        GameObject painter = GameObject.Find("Painter");
        global_selection s = painter.GetComponent<global_selection>();
        s.enabled = true;
    }

    public void setIsPaintingToFalse()
    {
        selected_dictionary s = GameObject.Find("Painter").GetComponent<selected_dictionary>();
        s.isPainting = false;
    }

    public void deleteProvisional()
    {
        selected_dictionary s = GameObject.Find("Painter").GetComponent<selected_dictionary>();
        s.DestroyProvisional();
    }

    public void clearProvisional()
    {
        selected_dictionary s = GameObject.Find("Painter").GetComponent<selected_dictionary>();
        s.clearProvisional();
    }

    public void addFurniturePiece()
    {
        GameObject inventorysystem = GameObject.Find("inventorySystem");
        inventorysystem.GetComponent<Inventory>().confirmAddingPiece();
    }

    public void cancelAddingFurniture()
    {
        GameObject inventorysystem = GameObject.Find("inventorySystem");
        inventorysystem.GetComponent<Inventory>().cancel();
    }

    public void deleteFurniture()
    {
        GameObject inventorysystem = GameObject.Find("inventorySystem");
        inventorysystem.GetComponent<Inventory>().delete();
    }

    public void confirmDeletingFurniture()
    {
        GameObject inventorysystem = GameObject.Find("inventorySystem");
        inventorysystem.GetComponent<Inventory>().confirmDeletingFurniture();
    }

    public void cancelDeletingFurniture()
    {
        GameObject inventorysystem = GameObject.Find("inventorySystem");
        inventorysystem.GetComponent<Inventory>().cancelDeletingFurniture();
    }

    public void changeDoor()
    {
        GameObject inventorysystem = GameObject.Find("inventorySystem");
        inventorysystem.GetComponent<Inventory>().changeDoor();
    }

    public void cancelChangedoor()
    {
        GameObject inventorysystem = GameObject.Find("inventorySystem");
        inventorysystem.GetComponent<Inventory>().cancelChangeDoor();
    }

    public static bool tipsDone = false;
    public void tipsAreDone()
    {
        tipsDone = true;
    }

    public void changeControlSpeed(float newControlSpeed)
    {
        GameObject player = GameObject.Find("FPSController");
        if (player != null) player.GetComponent<FirstPersonController>().sliderSpeedMultiplier = newControlSpeed;

        GameObject furnishing = GameObject.Find("Furnishing(Clone)");
        if (furnishing != null) furnishing.GetComponent<FirstPersonController>().sliderSpeedMultiplier = newControlSpeed;
    }

    public void changeLookAroundSpeed(float newControlSpeed)
    {
        GameObject player = GameObject.Find("FPSController");
        if (player != null) player.GetComponent<FirstPersonController>().sliderSpeedLookAroundMultiplier = newControlSpeed;

        GameObject furnishing = GameObject.Find("Furnishing(Clone)");
        if (furnishing != null) furnishing.GetComponent<FirstPersonController>().sliderSpeedLookAroundMultiplier = newControlSpeed;
    }

    public void teleport()
    {
        Teleport.teleport = true;
    }

    public static bool paintedFurnished = false;
    public void paintFurniture()
    {
        paintedFurnished = true;
    }

    public GameObject warningMenu;
    public GameObject warningReset;
    public Button scaleButton;

    public void checkPaintFurniture()
    {
        if (paintedFurnished)
        {
            warningMenu.SetActive(true);
        }
        else
        {
            scaleButton.gameObject.SetActive(true);
            scaleButton.onClick.Invoke();
            scaleButton.gameObject.SetActive(false);
        }
    }

    public void proceedWithPaint()
    {
        paintedFurnished = false;
        foreach (var obj in GameObject.FindGameObjectsWithTag("paint"))
            Destroy(obj);
        foreach (var obj in GameObject.FindGameObjectsWithTag("furniture"))
            Destroy(obj);
        scaleButton.onClick.Invoke();
    }

    public Button reset;
    public void checkPaintFurnitureReset()
    {
        if (paintedFurnished)
        {
            warningReset.SetActive(true);
        }
        else
        {
            reset.onClick.Invoke();
        }
    }

    public void proceedWithPaintReset()
    {
        paintedFurnished = false;
        foreach (var obj in GameObject.FindGameObjectsWithTag("paint"))
            Destroy(obj);
        foreach (var obj in GameObject.FindGameObjectsWithTag("furniture"))
            Destroy(obj);
        reset.onClick.Invoke();
    }

    // ----------------------- Floor Surface Update -----------------------
    private void UpdateFloorPlanes()
    {
        const float FLOOR_HEIGHT = 2.5f;
        const float thickness = 0.1f;
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj == null || !obj.name.StartsWith("Floor_")) continue;

            Transform floorContainer = obj.transform;
            int floorIndex = 0;
            string[] parts = obj.name.Split('_');
            if (parts.Length >= 2) int.TryParse(parts[1], out floorIndex);

            bool foundAny = false;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (Transform child in floorContainer)
            {
                if (child.name == "FloorSurface") continue;
                Renderer r = child.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    Bounds b = r.bounds;
                    minX = Mathf.Min(minX, b.min.x);
                    maxX = Mathf.Max(maxX, b.max.x);
                    minZ = Mathf.Min(minZ, b.min.z);
                    maxZ = Mathf.Max(maxZ, b.max.z);
                    foundAny = true;
                }
            }

            if (!foundAny) continue;

            float centerX = (minX + maxX) / 2f;
            float centerZ = (minZ + maxZ) / 2f;
            float sizeX = Mathf.Max(0.01f, maxX - minX);
            float sizeZ = Mathf.Max(0.01f, maxZ - minZ);

            Transform existing = floorContainer.Find("FloorSurface");
            GameObject floorPlane = existing != null
                ? existing.gameObject
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (existing == null)
            {
                floorPlane.name = "FloorSurface";
                floorPlane.transform.SetParent(floorContainer, false);
            }

            float yOffset = floorIndex * FLOOR_HEIGHT;
            floorPlane.transform.position = new Vector3(centerX, yOffset - (thickness / 2f), centerZ);
            floorPlane.transform.localScale = new Vector3(sizeX, thickness, sizeZ);

            var renderer = floorPlane.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.material.color = Color.grey;
        }
    }
}
