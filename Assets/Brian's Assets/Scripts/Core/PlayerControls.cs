using UnityEngine;
using UnityEngine.InputSystem;

namespace Brian
{
    public class PlayerControls : MonoBehaviour
    {
        public struct Binding
        {
            public string label;
            public string xrName;
            public string xrPath;
        }

        public static readonly Binding SelectBinding = new Binding
        {
            label = "Place tower / select tower or Mining Tower",
            xrName = "Right trigger", xrPath = "<XRController>{RightHand}/triggerPressed"
        };

        public static readonly Binding CancelBinding = new Binding
        {
            label = "Cancel building / close panel",
            xrName = "Right B", xrPath = "<XRController>{RightHand}/secondaryButton"
        };

        public static readonly Binding MenuBinding = new Binding
        {
            label = "Show / hide hand menu",
            xrName = "Left X", xrPath = "<XRController>{LeftHand}/primaryButton"
        };

        public static readonly Binding[] All = { SelectBinding, CancelBinding, MenuBinding };

        public static PlayerControls Instance { get; private set; }

        [SerializeField] Transform rightAim;
        [SerializeField] Transform leftHand;

        public InputAction Select { get; private set; }
        public InputAction Cancel { get; private set; }
        public InputAction Menu { get; private set; }

        public Transform LeftHand => leftHand;

        void Awake()
        {
            Instance = this;
            Select = Make(SelectBinding);
            Cancel = Make(CancelBinding);
            Menu = Make(MenuBinding);
        }

        void OnDestroy()
        {
            Select?.Dispose();
            Cancel?.Dispose();
            Menu?.Dispose();
            if (Instance == this) Instance = null;
        }

        static InputAction Make(Binding b)
        {
            var action = new InputAction(b.label, InputActionType.Button);
            action.AddBinding(b.xrPath);
            action.Enable();
            return action;
        }

        public Ray AimRay
        {
            get
            {
                if (rightAim != null) return new Ray(rightAim.position, rightAim.forward);
                var cam = Camera.main;
                return cam != null ? new Ray(cam.transform.position, cam.transform.forward) : new Ray(Vector3.zero, Vector3.forward);
            }
        }
    }
}
