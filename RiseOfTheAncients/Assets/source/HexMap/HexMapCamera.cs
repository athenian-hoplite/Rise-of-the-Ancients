using UnityEngine;

public class HexMapCamera : MonoBehaviour {

	static HexMapCamera instance;

	public static bool Locked {
		set {
			instance.enabled = !value;
		}
	}

	Transform Swivel, Stick;

    float Zoom = 1f;
    public float StickMinZoom, StickMaxZoom;
    public float SwivelMinZoom, SwivelMaxZoom;
    public float MoveSpeedMinZoom, MoveSpeedMaxZoom;
	public float RotationSpeed;
	float RotationAngle;

	public HexGrid Grid;

	void Awake () {
		instance = this;
		Swivel = transform.GetChild(0);
		Stick = Swivel.GetChild(0);
	}

    void Update () {
		float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
		if (zoomDelta != 0f) {
			AdjustZoom(zoomDelta);
		}

		float rotationDelta = Input.GetAxis("Rotation");
		if (rotationDelta != 0f) {
			AdjustRotation(rotationDelta);
		}

        float xDelta = Input.GetAxis("Horizontal"); // Reads both arrows and A D
		float zDelta = Input.GetAxis("Vertical"); // Reads both arrows and W S
		if (xDelta != 0f || zDelta != 0f) {
			AdjustPosition(xDelta, zDelta);
		}
	}

	public static void ValidatePosition () {
		instance.AdjustPosition(0f, 0f);
	}
	
	void AdjustZoom (float delta) {
		Zoom = Mathf.Clamp01(Zoom + delta);

        float distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, Zoom);
		Stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, Zoom);
		Swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
	}

	void AdjustRotation (float delta) {
		RotationAngle += delta * RotationSpeed * Time.deltaTime;
		if (RotationAngle < 0f) {
			RotationAngle += 360f;
		}
		else if (RotationAngle >= 360f) {
			RotationAngle -= 360f;
		}
		this.transform.localRotation = Quaternion.Euler(0f, RotationAngle, 0f);
	}

    void AdjustPosition (float xDelta, float zDelta) {
        // Normalizing prevents diagonal movement being faster
		// Using the local rotation to determine direction keeps movement consistent with camera rotation
		Vector3 direction = this.transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
		float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
		float distance = Mathf.Lerp(MoveSpeedMinZoom, MoveSpeedMaxZoom, Zoom) * damping * Time.deltaTime;

		Vector3 position = this.transform.localPosition;
		position += direction * distance;
		transform.localPosition = Grid.Wrapping ? WrapPosition(position) : ClampPosition(position);
	}

	Vector3 ClampPosition (Vector3 position) {
		float xMax = (Grid.CellCountX - 0.5f) * HexMetrics.InnerDiameter;
		float zMax = (Grid.CellCountZ - 1) * (1.5f * HexMetrics.OuterRadius);
		
		position.z = Mathf.Clamp(position.z, 0f, zMax);
		position.x = Mathf.Clamp(position.x, 0f, xMax);

		return position;
	}

	Vector3 WrapPosition (Vector3 position) {
		float width = Grid.CellCountX * HexMetrics.InnerDiameter;
		while (position.x < 0f) {
			position.x += width;
		}
		while (position.x > width) {
			position.x -= width;
		}

		float zMax = (Grid.CellCountZ - 1) * (1.5f * HexMetrics.OuterRadius);
		position.z = Mathf.Clamp(position.z, 0f, zMax);

		Grid.CenterMap(position.x);
		return position;
	}

}