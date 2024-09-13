using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class FirstPersonControls : MonoBehaviour
{

    [Header("MOVEMENT SETTINGS")]
    [Space(5)]
    // Public variables to set movement and look speed, and the player camera
    public float moveSpeed = 5; // Speed at which the player moves
    public float lookSpeed = 0.25f; // Sensitivity of the camera movement
    public float gravity = -9.81f; // Gravity value
    public float jumpHeight = 2.0f; // Height of the jump
    public Transform playerCamera; // Reference to the player's camera
                                   // Private variables to store input values and the character controller
    private Vector2 moveInput; // Stores the movement input from the player
    private Vector2 lookInput; // Stores the look input from the player
    private float verticalLookRotation = 0f; // Keeps track of vertical camera rotation for clamping
    private Vector3 velocity; // Velocity of the player
    private CharacterController characterController; // Reference to the CharacterController component
    private bool _lookAround = true;

    [Header("SHOOTING SETTINGS")]
    [Space(5)]
    public GameObject projectilePrefab; // Projectile prefab for shooting
    public Transform firePoint; // Point from which the projectile is fired
    public float projectileSpeed = 20f; // Speed at which the projectile is fired

    [Header("PICKING UP SETTINGS")]
    [Space(5)]
    public Transform holdPosition; // Position where the picked-up object will be held //put the position to where the gun was
    private GameObject heldObject; // Reference to the currently held object
    public float pickUpRange = 3f; // Range within which objects can be picked up //Important for the raycast
    private bool holdingGun = false;
    private bool _clickF = false;

    [Header("CROUCH SETTINGS")]
    [Space(5)]
    public float crouchHeight = 1f;     //How short the player will crouch to 
    public float standingHeight = 2f;   //Make normal height
    public float crouchSpeed = 1.5f;    //Make them slower
    private bool isCrouching = false;   //Check to see if they're crouching

    [Header("INSPECT SETTINGS")]
    [Space(5)]
    public Transform inspectPosition; // Position where the picked-up object will be held //put the position to where the gun was
    private GameObject inspectObject; // Reference to the currently held object
    public float inspectRange = 4f; // Range within which objects can be picked up //Important for the raycast
    //private bool holdingGun = false; Don't think we need this in the inspect mechanism
    private bool _clickG = false;

    [Header("CLIMBING SETTINGS")] [Space(5)]
    //cat climbing up furniture or objects
    public float climbing = 2f;
    public float upwardSpeed = 2f;
    public GameObject climable;

    private void Awake()
    {
        // Get and store the CharacterController component attached to this GameObject
        characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        // Create a new instance of the input actions
        var playerInput = new Controls();

        // Enable the input actions
        playerInput.Player.Enable();

        // Subscribe to the movement input events
        playerInput.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>(); // Update moveInput when movement input is performed
        playerInput.Player.Movement.canceled += ctx => moveInput = Vector2.zero; // Reset moveInput when movement input is canceled

        // Subscribe to the look input events
        playerInput.Player.LookAround.performed += ctx => lookInput = ctx.ReadValue<Vector2>(); // Update lookInput when look input is performed
        playerInput.Player.LookAround.canceled += ctx => lookInput = Vector2.zero; // Reset lookInput when look input is canceled

        // Subscribe to the jump input event
        playerInput.Player.Jump.performed += ctx => Jump(); // Call the Jump method when jump input is performed

        // Subscribe to the shoot input event
        playerInput.Player.Shoot.performed += ctx => Shoot(); // Call the Shoot method when shoot input is performed

        // Subscribe to the pick-up input event
        playerInput.Player.PickUp.performed += ctx => PickUpObject(); // Call the PickUpObject method when pick-up input is performed

        //Subscribe to the Crouch input event
        playerInput.Player.Crouch.performed += ctx => ToggleCrouch();

        //Subscribe to the inspect input event
        playerInput.Player.Inspect.performed += ctx => InspectObject();

        playerInput.Player.RotateObjectY.performed += ctx => RotateObjectYFunction();

        playerInput.Player.RotateObjectX.performed += ctx => RotateObjectXFunction();

        //Subscribe to the climbing input system
        playerInput.Player.Climbing.performed += ctx => Climb(); //whilst climbing
        playerInput.Player.Climbing.canceled += ctx => Climb(); // when not climbing

    }

    private void Update()
    {
        // Call Move and LookAround methods every frame to handle player movement and camera rotation
        Move();
        LookAround();
        ApplyGravity();
    }

    public void Move()
    {
        // Create a movement vector based on the input
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);

        // Transform direction from local to world space
        move = transform.TransformDirection(move);

        float currentSpeed;
        if(isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            currentSpeed = moveSpeed;
        }

        // Move the character controller based on the movement vector and speed
        characterController.Move(move * moveSpeed * Time.deltaTime);
        
        //Climbing when moving
        
    }

    public void LookAround()
    {
        //if (_lookAround == true)
        //{
            // Get horizontal and vertical look inputs and adjust based on sensitivity
            float LookX = lookInput.x * lookSpeed;
            float LookY = lookInput.y * lookSpeed;

            // Horizontal rotation: Rotate the player object around the y-axis
            transform.Rotate(0, LookX, 0);

            // Vertical rotation: Adjust the vertical look rotation and clamp it to prevent flipping
            verticalLookRotation -= LookY;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

            // Apply the clamped vertical rotation to the player camera
            playerCamera.localEulerAngles = new Vector3(verticalLookRotation, 0, 0);
        //}
        

        
    }

    public void ApplyGravity()
    {
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -0.5f; // Small value to keep the player grounded
        }

        velocity.y += gravity * Time.deltaTime; // Apply gravity to the velocity
        characterController.Move(velocity * Time.deltaTime); // Apply the velocity to the character
    }

    public void Jump()
    {
        if (characterController.isGrounded)
        {
            // Calculate the jump velocity
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void Shoot()
    {
        if (holdingGun == true)
        {
            // Instantiate the projectile at the fire point
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // Get the Rigidbody component of the projectile and set its velocity
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            rb.velocity = firePoint.forward * projectileSpeed;

            // Destroy the projectile after 3 seconds
            Destroy(projectile, 3f);
        }
    }

    public void PickUpObject()
    {
        // Check if we are already holding an object
        if (heldObject != null)
        {
            heldObject.GetComponent<Rigidbody>().isKinematic = false; // Enable physics
            heldObject.transform.parent = null;
            holdingGun = false;
        }

        // Perform a raycast from the camera's position forward
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        // Debugging: Draw the ray in the Scene view
        Debug.DrawRay(playerCamera.position, playerCamera.forward * pickUpRange, Color.red, 2f);


        if (Physics.Raycast(ray, out hit, pickUpRange))
        {
            // Check if the hit object has the tag "PickUp"
            if (hit.collider.CompareTag("PickUp"))
            {
                _lookAround = false;
                // Pick up the object
                heldObject = hit.collider.gameObject;
                heldObject.GetComponent<Rigidbody>().isKinematic = true; // Disable physics

                // Attach the object to the hold position
                heldObject.transform.position = holdPosition.position;
                heldObject.transform.rotation = holdPosition.rotation;
                heldObject.transform.parent = holdPosition;
            }
            else if (hit.collider.CompareTag("Gun"))
            {
                _lookAround = false;
                // Pick up the object
                heldObject = hit.collider.gameObject;
                heldObject.GetComponent<Rigidbody>().isKinematic = true; // Disable physics

                // Attach the object to the hold position
                heldObject.transform.position = holdPosition.position;
                heldObject.transform.rotation = holdPosition.rotation;
                heldObject.transform.parent = holdPosition;

                holdingGun = true;
            }
        }
    }

    public void ToggleCrouch()
    {
        if (isCrouching)
        {
            //Stand up
            characterController.height = standingHeight;
            isCrouching = false;
        }
        else
        {
            //Crouch down
            characterController.height = crouchHeight;
            isCrouching = true;
        }
    }

    public void InspectObject()
    {
       
        //check to see if we're holding an object to inspect
        if (inspectObject != null)
        {
            inspectObject.GetComponent<Rigidbody>().isKinematic = false; //We don't want physics when we're inspecting the object, yeah?
            inspectObject.transform.parent = null;
            _lookAround = true;
        }

        //Perform a raycast from  the camera's position forward
        Ray rayInspect = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hitInspect;

        //Debugging the raycast to hit in the scene where it's viewable
        Debug.DrawRay(playerCamera.position, playerCamera.forward * inspectRange, Color.blue, 2f);

        if (Physics.Raycast(rayInspect, out hitInspect, inspectRange))
        {
            //Check if the object you're inspecting has the tag "PickUp" since all the pickup objects can also be inspected
            if (hitInspect.collider.CompareTag("PickUp"))
            {
                _lookAround = false;
                //Pick up the inspectable object
                inspectObject = hitInspect.collider.gameObject;
                inspectObject.GetComponent<Rigidbody>().isKinematic = true; // Disable physics

                // Attach the object to the hold position
                inspectObject.transform.position = inspectPosition.position;
                inspectObject.transform.rotation = inspectPosition.rotation;
                inspectObject.transform.parent = inspectPosition;
                _clickF = true;
                _clickG = true;
            }

            else if (hitInspect.collider.CompareTag("Gun"))
            {
                _lookAround = false;
                // Pick up the gun to inspect
                inspectObject = hitInspect.collider.gameObject;
                inspectObject.GetComponent<Rigidbody>().isKinematic = true; // Disable physics

                // Attach the gun to the hold position
                inspectObject.transform.position = inspectPosition.position;
                inspectObject.transform.rotation = inspectPosition.rotation;
                inspectObject.transform.parent = inspectPosition;
                _clickF = true;
                _clickG = true;
            }
        }


    }

    public void RotateObjectYFunction ()
    {
        bool isRotating = false;
        float rotationSpeed = 100f;
        
        if (_clickF)
        {

            inspectObject.transform.Rotate(Vector3.up * rotationSpeed, Space.Self);
        }
    }

    public void RotateObjectXFunction()
    {
        bool isRotating = false;
        float rotationSpeed = 100f;
        
        if (_clickF)
        {

            inspectObject.transform.Rotate(Vector3.right * rotationSpeed, Space.Self);
        }
    }

    public void Climb()
    {
       void OnCollisionEnter(Collision col)
           {
                    if (col.gameObject.tag == "wall")
                    {
                        transform.position += Vector3.up * Time.deltaTime * upwardSpeed;
                        GetComponent<Rigidbody>().isKinematic = false;
                    }
              
           }
    }

    


}
