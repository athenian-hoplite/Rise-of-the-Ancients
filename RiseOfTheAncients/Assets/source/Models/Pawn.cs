using System.Collections;
using ROTA.Utils;
using UnityEngine;

public class Pawn : MonoBehaviour
{
    private HexCell m_location;
    private float m_orientation;
    private float m_rotationSpeed = 180f;

    public void Init(HexCell location, float orientation)
    {
        TeleportTo(location);
        SetOrientation(orientation);
    }

    public void SetOrientation(float orientation)
    {
        m_orientation = orientation;
		transform.localRotation = Quaternion.Euler(0f, orientation, 0f);
    }

    public void LookAt(Vector3 point)
    {
        new CoroutineTask(_LookAt(point));
    }

    public void TeleportTo(HexCell location)
    {
        m_location = location;
        transform.localPosition = location.Position;
        HexGrid.INSTANCE.MakeChildOfColumn(transform, location.ColumnIndex);
    }

    /// <summary>
    /// Rotates the pawn to face the given point.
    /// </summary>
    private IEnumerator _LookAt(Vector3 point) 
    {
		if (HexMetrics.Wrapping) // Account for wrapping when choosing where to look
        {
			float xDistance = point.x - transform.localPosition.x;
			if (xDistance < -HexMetrics.InnerRadius * HexMetrics.WrapSize) 
            {
				point.x += HexMetrics.InnerDiameter * HexMetrics.WrapSize;
			}
			else if (xDistance > HexMetrics.InnerRadius * HexMetrics.WrapSize) 
            {
				point.x -= HexMetrics.InnerDiameter * HexMetrics.WrapSize;
			}
		}

		point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);

        float angle = Quaternion.Angle(fromRotation, toRotation);

        if (angle > 0f)
        {
            float speed = m_rotationSpeed / angle; // Keep angular speed constant
		
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed) 
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }

            transform.LookAt(point);
            m_orientation = transform.localRotation.eulerAngles.y;
        }
	}

}
