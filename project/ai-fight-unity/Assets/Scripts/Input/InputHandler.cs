using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace dev.susybaka.TurnBasedGame.Input
{
    public class InputHandler : MonoBehaviour
    {
        private GameManager gameManager;

        private Vector2 _movementInput;
        public Vector2 MovementInput => _movementInput;

        private bool _jumpInput;
        public bool JumpInput => _jumpInput;

        private bool _jumpHoldInput;
        public bool JumpHoldInput => _jumpHoldInput;

        private bool _jumpReleaseInput;
        public bool JumpReleaseInput => _jumpReleaseInput;

        private bool _pauseInput;
        public bool PauseInput => _pauseInput;

        private bool _interactInput;
        public bool InteractInput => _interactInput;

        private bool _interactHoldInput;
        public bool InteractHoldInput => _interactHoldInput;

        private bool _sprintInput;
        public bool SprintInput => _sprintInput;

        private bool _sprintHoldInput;
        public bool SprintHoldInput => _sprintHoldInput;

        private bool _backInput;
        public bool BackInput => _backInput;

        private bool _backHoldInput;
        public bool BackHoldInput => _backHoldInput;

        private bool _confirmInput;
        public bool ConfirmInput => _confirmInput;

        private bool _confirmHoldInput;
        public bool ConfirmHoldInput => _confirmHoldInput;
        
        private bool _nextInput;
        private bool _nextHoldInput;
        private bool _nextReleaseInput;
        private bool storedNextInput = false;
        public bool NextInput => _nextInput;
        public bool NextHoldInput => _nextHoldInput;
        public bool NextReleaseInput => _nextReleaseInput;

        private bool _previousInput;
        private bool _previousHoldInput;
        private bool _previousReleaseInput;
        private bool storedPreviousInput = false;
        public bool PreviousInput => _previousInput;
        public bool PreviousHoldInput => _previousHoldInput;
        public bool PreviousReleaseInput => _previousReleaseInput;

        private PlayerInput playerInput;

        private InputAction movementAction;
        private InputAction jumpAction;
        private InputAction pauseAction;
        private InputAction interactAction;
        private InputAction sprintAction;
        private InputAction backAction;
        private InputAction confirmAction;

        private bool initialized = false;
        private const float defaultDeadzone = 0.2f;

        public void Initialize(GameManager manager)
        {
            if (initialized)
                return;

            initialized = true;
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
            backAction = playerInput.actions["Cancel"];
            confirmAction = playerInput.actions["Submit"];
        }

        private void UpdateInputs()
        {
            _movementInput = movementAction.ReadValue<Vector2>();
            _jumpInput = jumpAction.WasPressedThisFrame();
            _jumpHoldInput = jumpAction.IsPressed();
            _jumpReleaseInput = jumpAction.WasReleasedThisFrame();
            _pauseInput = pauseAction.WasPressedThisFrame();
            _interactInput = interactAction.WasPressedThisFrame();
            _interactHoldInput = interactAction.IsPressed();
            _sprintInput = sprintAction.WasPressedThisFrame();
            _sprintHoldInput = sprintAction.IsPressed();
            _backInput = backAction.WasPressedThisFrame();
            _backHoldInput = backAction.IsPressed();
            _confirmInput = confirmAction.WasPressedThisFrame();
            _confirmHoldInput = confirmAction.IsPressed();
            DeriveInputsFromVector2(-1 * MovementInput, ref _nextInput, ref _nextHoldInput, ref _nextReleaseInput, ref storedNextInput);
            DeriveInputsFromVector2(MovementInput, ref _previousInput, ref _previousHoldInput, ref _previousReleaseInput, ref storedPreviousInput);
        }

        private void DeriveInputsFromVector2(Vector2 v, ref bool input, ref bool inputHold, ref bool inputRelease, ref bool inputStored)
        {
            bool currentNextInput = v.y > defaultDeadzone;
            input = !inputStored && currentNextInput;
            inputHold = currentNextInput;
            inputRelease = inputStored && !currentNextInput;
            inputStored = currentNextInput;
        }

        public void SetInputLayer(string layerName)
        {
            playerInput.SwitchCurrentActionMap(layerName);
        }
    }
}