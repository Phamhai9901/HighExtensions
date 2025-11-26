using UnityEngine;

public class EllipseLayout : MonoBehaviour
{
    [SerializeField] private float radiusX = 5f; // Bán kính trục X
    private float radiusY => radiusX * 0.65f; // Bán kính trục Y
    [SerializeField] private float startAngle = 0f; // Góc bắt đầu (độ)
    public void UpdateLayout()
    {
        // Lấy tất cả các transform con
        Transform[] elements = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            elements[i] = transform.GetChild(i);
        }

        int elementCount = Mathf.Max(1, transform.childCount); // Đảm bảo số lượng tối thiểu là 1

        // Tính góc giữa các element
        float angleStep = 360f / elementCount;

        for (int i = 0; i < elements.Length; i++)
        {
            if (i < elementCount)
            {
                // Tính góc hiện tại (chuyển đổi sang radian)
                float angle = (startAngle + i * angleStep) * Mathf.Deg2Rad;

                // Tính vị trí theo công thức elipse
                float x = radiusX * Mathf.Cos(angle);
                float y = radiusY * Mathf.Sin(angle);

                // Đặt vị trí cho element
                elements[i].localPosition = new Vector3(x, y, 0);
                elements[i].gameObject.SetActive(true);
            }
            else
            {
                // Ẩn các element thừa
                elements[i].gameObject.SetActive(false);
            }
        }
    }
}
