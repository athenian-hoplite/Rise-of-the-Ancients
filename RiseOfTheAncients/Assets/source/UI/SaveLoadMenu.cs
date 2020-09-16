using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SaveLoadMenu : MonoBehaviour {

	const int mapFileVersion = 0;

    public Text menuLabel, actionButtonLabel, inputPlaceholder;
    public InputField nameInput;
    public RectTransform listContent;
	public SaveLoadItem itemPrefab;

	public HexGrid hexGrid;
    bool saveMode;

	public void Open (bool saveMode) {
        this.saveMode = saveMode;
        if (saveMode) {
			menuLabel.text = "Save Map";
			actionButtonLabel.text = "Save";
		}
		else {
			menuLabel.text = "Load Map";
			actionButtonLabel.text = "Load";
		}

        FillList(); // Get Files
		this.gameObject.SetActive(true);
		HexMapCamera.Locked = true;
	}

	public void Close () {
		this.gameObject.SetActive(false);
		HexMapCamera.Locked = false;
	}

    string GetSelectedPath () {
		string mapName = nameInput.text;
		if (mapName.Length == 0) {
			return null;
		}
		return Path.Combine(Application.persistentDataPath, mapName + ".map");
	}

    public void Action () {

		string path = GetSelectedPath();
		if (path == null) {
			return;
		}

		if (saveMode) {
			Save(path);
		}
		else {
			Load(path);
		}

		Close();
	}

    public void Delete () {
		string path = GetSelectedPath();
		if (path == null) {
			return;
		}
		if (File.Exists(path)) {
			File.Delete(path);
		}
        nameInput.text = "";
		FillList();
	}

    public void Save (string path) {
		using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
			writer.Write(mapFileVersion); // Save format version
			hexGrid.Save(writer);
		}
	}

	public void Load (string path) {
        if ( ! File.Exists(path)) {
			Debug.LogError("File does not exist " + path);
			return;
		}

		using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
			int header = reader.ReadInt32(); // Read save format version
			if (header == mapFileVersion) {
				hexGrid.Load(reader);
				HexMapCamera.ValidatePosition(); // Camera may be in invalid position if map size change
			}
			else {
				Debug.LogWarning("Unknown map format " + header);
			}
		}
	}

    void FillList () {
        // Destroy old data
        for (int i = 0; i < listContent.childCount; i++) {
			Destroy(listContent.GetChild(i).gameObject);
		}

		string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        for (int i = 0; i < paths.Length; i++) {
			SaveLoadItem item = Instantiate(itemPrefab);
			item.menu = this;
			item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
			item.transform.SetParent(listContent, false);
		}
	}

    public void SelectItem (string name) {
		nameInput.text = name;
	}

}