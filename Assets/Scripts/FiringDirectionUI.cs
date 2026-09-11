using UnityEngine;
using UnityEngine.UI;

public class FiringDirectionUI : MonoBehaviour
{
    [SerializeField] private FiringDirectionController firingDirectionController;
    [SerializeField] private Image directionArrow;

    private void Update()
    {
        switch (firingDirectionController.SelectedDirection)
        {
            case FiringDirection.Front:
                directionArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            case FiringDirection.Right:
                directionArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                break;

            case FiringDirection.Left:
                directionArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
        }
    }
}