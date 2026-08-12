using UnityEngine;

namespace BeeKingdom.WorldMap
{
    // Source d'entree Unity : souris + touches + clavier de deplacement (bureau)
    // et multitouch (mobile). Membre d'un GameObject de la scene de la carte.
    public sealed class UnityWorldInputSource : MonoBehaviour, IWorldInputSource
    {
        public bool PrimaryDown
        {
            get
            {
                if (Input.touchCount > 0)
                {
                    return true;
                }

                return Input.GetMouseButton(0);
            }
        }

        public Vector2 PrimaryPosition
        {
            get
            {
                if (Input.touchCount > 0)
                {
                    return Input.GetTouch(0).position;
                }

                return Input.mousePosition;
            }
        }

        public Vector2 ScreenSize => new Vector2(Screen.width, Screen.height);

        public float ScrollDelta => Input.mouseScrollDelta.y;

        public bool PinchActive
        {
            get
            {
                if (Input.touchCount < 2)
                {
                    return false;
                }

                Touch first = Input.GetTouch(0);
                Touch second = Input.GetTouch(1);
                return first.phase != TouchPhase.Ended && first.phase != TouchPhase.Canceled &&
                       second.phase != TouchPhase.Ended && second.phase != TouchPhase.Canceled;
            }
        }

        public float PinchRatio
        {
            get
            {
                if (Input.touchCount < 2)
                {
                    return 1f;
                }

                float previous = Vector2.Distance(Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition,
                    Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition);
                float current = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                return previous > 0.01f ? current / previous : 1f;
            }
        }

        public Vector2 PinchPivot
        {
            get
            {
                if (Input.touchCount < 2)
                {
                    return Input.mousePosition;
                }

                return (Input.GetTouch(0).position + Input.GetTouch(1).position) * 0.5f;
            }
        }

        public bool MoveLeft => Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        public bool MoveRight => Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        public bool MoveUp => Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        public bool MoveDown => Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }
}
