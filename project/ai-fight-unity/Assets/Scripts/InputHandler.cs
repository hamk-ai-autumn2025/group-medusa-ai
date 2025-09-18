using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace dev.susybaka.TurnBasedGame.Player
{
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler instance;

        public Vector2 MovementInput { get; private set; }
        public bool JumpInput { get; private set; }
        public bool JumpHoldInput { get; private set; }
        public bool JumpReleaseInput { get; private set; }
        public bool PauseInput { get; private set; }
        public bool InteractInput { get; private set; }
        public bool InteractHoldInput { get; private set; }
        public bool SprintInput { get; private set; }
        public bool SprintHoldInput { get; private set; }

        private PlayerInput playerInput;

        private InputAction movementAction;
        private InputAction jumpAction;
        private InputAction pauseAction;
        private InputAction interactAction;
        private InputAction sprintAction;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            playerInput = GetComponent<PlayerInput>();

            SetupInputActions();
            SetInputLayer("Overworld");
        }

        private void Update()
        {
            UpdateInputs();
        }

        private void SetupInputActions()
        {
            movementAction = playerInput.actions["Movement"];
            jumpAction = playerInput.actions["Jump"];
            pauseAction = playerInput.actions["Pause"];
            interactAction = playerInput.actions["Interact"];
            sprintAction = playerInput.actions["Sprint"];
        }

        private void UpdateInputs()
        {
            MovementInput = movementAction.ReadValue<Vector2>();
            JumpInput = jumpAction.WasPressedThisFrame();
            JumpHoldInput = jumpAction.IsPressed();
            JumpReleaseInput = jumpAction.WasReleasedThisFrame();
            PauseInput = pauseAction.WasPressedThisFrame();
            InteractInput = interactAction.WasPressedThisFrame();
            InteractHoldInput = interactAction.IsPressed();
            SprintInput = sprintAction.WasPressedThisFrame();
            SprintHoldInput = sprintAction.IsPressed();
        }

        public void SetInputLayer(string layerName)
        {
            playerInput.SwitchCurrentActionMap(layerName);
        }
    }
}