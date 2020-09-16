using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using ROTA.Memory;
using ROTA.Utils;

public class HexUnit : MonoBehaviour
{
	public HexGrid Grid { get; set; } // ! Meh

    public static HexUnit unitPrefab;

    public HexCell Location {
		get {
			return location;
		}
		set {
            if (location) {
				Grid.DecreaseVisibility(location, visionRange);
				location.Unit = null;
			}
			location = value;
            value.Unit = this;
			Grid.IncreaseVisibility(value, visionRange);
			transform.localPosition = value.Position;

			Grid.MakeChildOfColumn(transform, value.ColumnIndex);
		}
	}
    
	public float Orientation {
		get {
			return orientation;
		}
		set {
			orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}

	public int Speed { get { return 24; } }

	public int VisionRange { get { return 3; } }

    HexCell location, currentTravelLocation; // ! Why have a separate
	float orientation;
    List<HexCell> pathToTravel;

    const float travelSpeed = 4f;
    const float rotationSpeed = 180f;
	const int visionRange = 3;

    void OnEnable () {
		if (location) {
			transform.localPosition = location.Position;
			if (currentTravelLocation) {
				Grid.IncreaseVisibility(location, visionRange);
				Grid.DecreaseVisibility(currentTravelLocation, visionRange);
				currentTravelLocation = null;
			}
		}
	}

    IEnumerator TravelPath () {
		Vector3 a, b, c = pathToTravel[0].Position;
        yield return LookAt(pathToTravel[1].Position); // First face correct direction

		if ( ! currentTravelLocation) {
			currentTravelLocation = pathToTravel[0];
		}
		Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
		int currentColumn = currentTravelLocation.ColumnIndex;


		float t = Time.deltaTime * travelSpeed; // First frame movement
		for (int i = 1; i < pathToTravel.Count; i++) {

			currentTravelLocation = pathToTravel[i];

			a = c;
			b = pathToTravel[i - 1].Position;

			int nextColumn = currentTravelLocation.ColumnIndex;
			if (currentColumn != nextColumn) {
				if (nextColumn < currentColumn - 1) {
					a.x -= HexMetrics.InnerDiameter * HexMetrics.WrapSize;
					b.x -= HexMetrics.InnerDiameter * HexMetrics.WrapSize;
				}
				else if (nextColumn > currentColumn + 1) {
					a.x += HexMetrics.InnerDiameter * HexMetrics.WrapSize;
					b.x += HexMetrics.InnerDiameter * HexMetrics.WrapSize;
				}
				Grid.MakeChildOfColumn(transform, nextColumn);
				currentColumn = nextColumn;
			}

			c = (b + currentTravelLocation.Position) * 0.5f;
			Grid.IncreaseVisibility(pathToTravel[i], VisionRange);


			for (; t < 1f; t += Time.deltaTime * travelSpeed) {
				transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f; // Avoid tilting up when going uphill
				transform.localRotation = Quaternion.LookRotation(d);
				yield return null;
			}
			Grid.DecreaseVisibility(pathToTravel[i], visionRange);
			t -= 1f;
		}

		currentTravelLocation = null;

		a = c;
		b = location.Position; // We can simply use the destination here.
		c = b;
		Grid.IncreaseVisibility(location, visionRange);
		for (; t < 1f; t += Time.deltaTime * travelSpeed) {
			transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f; // Avoid tilting up when going uphill
			transform.localRotation = Quaternion.LookRotation(d);
			yield return null;
		}

        transform.localPosition = location.Position; // Make sure we get exactly there
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.GLRestore(pathToTravel);
		pathToTravel = null;
	}

    /// <summary>
    /// Rotates the unit to face the given point (based on rotationSpeed).
    /// </summary>
    IEnumerator LookAt (Vector3 point) {

		if (HexMetrics.Wrapping) {
			float xDistance = point.x - transform.localPosition.x;
			if (xDistance < -HexMetrics.InnerRadius * HexMetrics.WrapSize) {
				point.x += HexMetrics.InnerDiameter * HexMetrics.WrapSize;
			}
			else if (xDistance > HexMetrics.InnerRadius * HexMetrics.WrapSize) {
				point.x -= HexMetrics.InnerDiameter * HexMetrics.WrapSize;
			}
		}

		point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);

        float angle = Quaternion.Angle(fromRotation, toRotation);

        if (angle > 0f)
        {
            float speed = rotationSpeed / angle; // Keep angular speed constant
		
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed) {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }

            transform.LookAt(point);
            orientation = transform.localRotation.eulerAngles.y;
        }

	}

    public void Travel (List<HexCell> path) {
		location.Unit = null;
		location = path[path.Count - 1];  // ! We shouldnt automatically get there ... game state is incoherent
		location.Unit = this;
        pathToTravel = path;

		// Stupid, stopall coroutine lols
        StopAllCoroutines();
		StartCoroutine(TravelPath());
	}

	public int GetMoveCost (HexCell fromCell, HexCell toCell, HexDirection direction) {
		
		HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
		
		if (edgeType == HexEdgeType.Cliff) {
			return -1;
		}

		int moveCost;

		if (fromCell.HasRoadThroughEdge(direction)) {
			moveCost = 1;
		}
		else if (fromCell.Walled != toCell.Walled) {
			return -1;
		}
		else {
			moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
			moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
		}

		return moveCost;
	}

    public void ValidateLocation () {
		transform.localPosition = location.Position;
	}

    public void Die () {
		if (location) {
			Grid.DecreaseVisibility(location, visionRange);
		}
		location.Unit = null;
		Destroy(gameObject);
	}

    public void Save (BinaryWriter writer) {
		location.Coordinates.Save(writer);
		writer.Write(orientation);
	}
    
    public bool IsValidDestination (HexCell cell) {
		return cell.IsExplored && ! cell.IsUnderwater && ! cell.Unit;
	}

    public static void Load (BinaryReader reader, HexGrid grid) {
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
	}

}
