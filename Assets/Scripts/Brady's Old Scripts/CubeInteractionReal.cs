// using UnityEngine;
// using UnityEngine.InputSystem;

// public class CubeInteractionReal : MonoBehaviour
// {
//     public InputActionReference clickAction; // drag in your "Click" action from the Input Actions asset
//     private Renderer cubeRenderer;

//     public int holdCount = 0;

//     void OnEnable()
//     {
//         clickAction.action.Enable();
//     }

//     void OnDisable()
//     {
//         clickAction.action.Disable();
//     }

//     void Start()
//     {
//         cubeRenderer = GetComponent<Renderer>();
//     }

//     void Update()
//     {
//         // If left mouse button is held down


//         if (clickAction.action.IsPressed())
//         {
//             holdCount += 1;
//             if (holdCount == 50)
//             {
//                 Debug.Log("Mining Cube");
//                 cubeRenderer.enabled = false; // Hide while holding
//                 // holdCount = 0;
//             }
//         }

//         // if (clickAction.action.WasReleasedThisFrame() && holdCount != 50)
//         // {
//         //     Debug.Log("Stopped Mining Cube");
//         //     holdCount = 0;

//         // }
//     }
// }
