using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // special effexs
    public ParticleSystem meowSfx;
    // Horizontal movement
    private float Move_X = 0f;
    // Vertical movement
    private float Move_Y = 0f;
    // Player speed
    public float speed = 1;
    // How high player can jump
    public float jumpHeight = 1f;
    // Gravity
    public float gravity = -9.85f;

    private bool isCatGrounded = false;
    private bool spacePressed;
    private bool isMeowing = false;
    private bool isLaying = false;
    private bool isAlive = true;

    // Is cat touching water (a puddle)
    public bool touchingWater = false;
    // Is cat taking effects from having touched a puddle
    public bool takingWaterEffects = false;
    // A float for wetCount, which is used later to make it so player takes 3f seconds of water effects
    public float wetCount = 0f;

    bool isRespawned = false;
    // How many times have you pet the cat
    int petCounter = 0;
    // Multipliers for the UI, which are got from MainMenu.cs
    float playerspeedMultiplier;
    float hungerMultiplier;

    Vector3 direction = Vector3.zero;
    Vector3 startPos;

    // catLeg and groundMask are used to check if cat is grounded
    public Transform catLeg;
    public LayerMask groundMask;
    // Player inputs for movement
    PlayerInputs playerInputs;
    // Reference to the player sprite
    SpriteRenderer catSprite;
    // Reference to player's character controller
    CharacterController catController;
    // Reference to animator
    public Animator animator;
    // Reference to UI (and pause stuff)
    HungerMeter catUI;
    // Reference to PauseMenu for the catspeed, hungerspeed and carspeed multipliers
    PauseMenu optionVariables;
    // Ref to pause menu
    public GameObject pauseCanvas;
    // Ref to win screen
    public GameObject winCanvas;

    // Start is called before the first frame update
    void Start()
    {
        playerInputs = new PlayerInputs();
        playerInputs.MovementMap.Enable();

        // listeners for unity's input system
        playerInputs.MovementMap.Jump.performed += OnJump;
        playerInputs.MovementMap.Meow.performed += OnMeow;
        playerInputs.MovementMap.Lay.performed += OnLay;
        playerInputs.MovementMap.Pet.performed += OnPet;
        playerInputs.MovementMap.Pause.performed += OnPause;
        playerInputs.MenuMap.Pause.performed += OnPause;

        catSprite = GetComponent<SpriteRenderer>();
        catController = GetComponent<CharacterController>();
        catUI = GetComponent<HungerMeter>();

        playerspeedMultiplier = OptionManager.Instance.speedRate;
        hungerMultiplier = OptionManager.Instance.hungerRate;

        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isRespawned)
        {
            // Animator takes floats to determine what animation to use
            animator.SetFloat("AnimSpeed", Mathf.Abs(Move_X));
            animator.SetFloat("jumpSpeed", direction.y);

            // Check if cat is maassa (grounded)
            isCatGrounded = Physics.CheckSphere(catLeg.position, 0.28f, groundMask);

            // If cat isn't grounded, apply gravity and move cat down
            if (!isCatGrounded)
            {
                Move_Y += gravity * Time.deltaTime;
                Move_Y = Mathf.Clamp(Move_Y, -30f, 200f);
            }
            if (!isMeowing)
            {

                if (isAlive)
                {
                    // Read Move inputs
                    Move_X = playerInputs.MovementMap.Move.ReadValue<Vector2>().x;
                }
            }

            // apply to direction Vector
            // slows the character down by half their speed if they've recently touched a puddle
            if (takingWaterEffects)
            {
                direction.x = (Move_X * Time.deltaTime * speed * playerspeedMultiplier) / 2;
            }
            else
            {
                direction.x = Move_X * Time.deltaTime * speed * playerspeedMultiplier;
            }

            direction.y = Move_Y * Time.deltaTime;

            catController.Move(direction);

            // If cat moves left, flip sprite
            if (Move_X < 0 && !catSprite.flipX)
            {
                catSprite.flipX = true;
            }
            // And if cat move right, flip again
            if (Move_X > 0 && catSprite.flipX)
            {
                catSprite.flipX = false;
            }

            // starts counting down the wetcount once player is not in a puddle
            if (!touchingWater && wetCount > 0)
            {
                wetCount -= Time.deltaTime;
            }

            // once wetcount reaches zero, remove the wet slowing effects from the player
            if (wetCount <= 0 && takingWaterEffects)
            {
                takingWaterEffects = false;
                animator.SetBool("catWet", false);
            }

            // If cat is laying, hunger goes down half as fast
            if (isLaying)
            {
                catUI.hungerSpeed = 0.25f * hungerMultiplier;
            }

            // If cat is not laying, hunger speed returns back to normal
            else
            {
                catUI.hungerSpeed = 0.5f * hungerMultiplier;
            }

            // If cat moves at all in any direction on the X axis, cat is not considered to be laying for hungerSpeed purposes
            if (Move_X > 0 || Move_X < 0)
            {
                isLaying = false;
            }
        }

        // if player dies and is respawned, move it back to start and set any wetcounters to 0
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, startPos, 1000 * Time.deltaTime);

            if (wetCount > 0)
            {
                wetCount -= Time.deltaTime;
            }
            else
            {
                isRespawned = false;
                GetComponent<Collider>().enabled = true;
            }
        }
    }

    void OnJump(InputAction.CallbackContext context)
    {
        // Cat jumps on jump and its "isJumping" bool is set to true
        if (isCatGrounded && isAlive)
        {
            Move_Y = Mathf.Sqrt(jumpHeight * gravity * -2);
            animator.SetBool("isJumping", true);
            animator.SetBool("spacePressed", true);
            isLaying = false;
            isMeowing = false;
            meowSfx.Stop();
        }
    }

    void OnMeow(InputAction.CallbackContext context)
    {
        if (isCatGrounded && isAlive)
        {
            animator.SetTrigger("meowButton");
            Move_X = 0;
            isMeowing = true;
            meowSfx.Play();
        }
    }

    void OnLay(InputAction.CallbackContext context)
    {
        if (isCatGrounded && isAlive)
        {
            animator.SetTrigger("layButton");
            Move_X = 0;
            isLaying = true;
        }
    }

    void OnPet(InputAction.CallbackContext context)
    {
        if (!isLaying && !isMeowing)
        {
            animator.SetTrigger("petCat");
            petCounter++;

            // go away kai :eyes:
            switch (petCounter)
            {
                case 21:
                    catUI.hunger += 21;
                    break;
                case 69:
                    catUI.hunger += 69;
                    break;
                case 112:
                    catUI.hunger += 112;
                    break;
                case 420:
                    catUI.hunger += 420;
                    break;
                case 666:
                    catUI.hunger += 666;
                    break;
                case 1000:
                    catUI.hunger += 1000;
                    break;
                case 2021:
                    // kai, s ja s :cactus:
                    catUI.hunger += 2021;
                    break;
                default:
                    break;
            }
        }
    }

    void OnPause (InputAction.CallbackContext context)
    {
        pauseCanvas.SetActive(!pauseCanvas.activeSelf);
        bool checkPause = catUI.PressedPause();
        switch (checkPause)
        {
            case true:
                playerInputs.MenuMap.Enable();
                playerInputs.MovementMap.Disable();
                break;
            case false:
                playerInputs.MenuMap.Disable();
                playerInputs.MovementMap.Enable();
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 9)
        {
            if (isAlive)
            {
                KillCat();
            }
        }

        if (other.gameObject.layer == 4)
        {
            animator.SetBool("catWet", true);
            takingWaterEffects = true;
            wetCount = 3f;
        }

        if (other.gameObject.layer == 11)
        {
            winCanvas.SetActive(!pauseCanvas.activeSelf);
            bool checkPause = catUI.PressedPause();
            playerInputs.MenuMap.Enable();
            playerInputs.MovementMap.Disable();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == 4)
        {
            animator.SetBool("catWet", true);
            wetCount = 3f;
        }
    }

    void RespawnKitten()
    {
        catUI.catsUpdate();
        isAlive = true;
    }

    public void KillCat()
    {
        isAlive = false;
        Move_X = 0;
        GetComponent<Collider>().enabled = false;
        RespawnKitten();
        isRespawned = true;
        wetCount = 3;
    }
}