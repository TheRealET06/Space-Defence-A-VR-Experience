using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Brian
{
    public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action entered;
        public Action exited;

        public void OnPointerEnter(PointerEventData eventData) => entered?.Invoke();

        public void OnPointerExit(PointerEventData eventData) => exited?.Invoke();

        void OnDisable() => exited?.Invoke();
    }
}
