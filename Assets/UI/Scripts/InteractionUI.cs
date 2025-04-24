using UnityEngine;

namespace HorrorGame
{
    public class InteractionUI : MonoBehaviour
    {
        private InteractionUIBuilder uiBuilder;
        private InteractablePoint currentPoint;

        public void Initialize(InteractionUIBuilder builder)
        {
            uiBuilder = builder;
        }

        public void Show(InteractablePoint point)
        {
            currentPoint = point;
            if (uiBuilder != null)
            {
                uiBuilder.Show(point, point.transform.position);
            }
        }

        public void Hide()
        {
            if (uiBuilder != null)
            {
                uiBuilder.Hide();
            }
            currentPoint = null;
        }

        public void ShowNarrativeText(string text)
        {
            if (uiBuilder != null)
            {
                uiBuilder.ShowNarrativeText(text);
            }
        }
    }
}