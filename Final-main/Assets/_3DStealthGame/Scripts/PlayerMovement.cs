using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input Actions")]
    public InputAction MoveAction;
    public InputAction SprintAction;

    [Header("Movement Settings")]
    public float walkSpeed = 1.0f;
    public float turnSpeed = 20f;
    public float sprintSpeed = 3.0f;   // Speed while sprinting

    [Header("Sprint (Major Mod)")]
    public float sprintDuration = 2.5f;    // How long sprint lasts
    public float sprintCooldown = 4f;      // Recharge time

    [Header("UI Toolkit")]
    public UIDocument uiDocument;          // Drag your UI Document here
    private VisualElement sprintIcon;       // UI square to recolor

    Animator m_Animator;
    Rigidbody m_Rigidbody;

    Vector3 m_Movement;
    Quaternion m_Rotation = Quaternion.identity;

    // Internal sprint logic
    private float sprintTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isSprinting = false;
    private bool canSprint = true;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();

        MoveAction.Enable();
        SprintAction.Enable();

        // FIND SprintIcon inside UI Toolkit
        sprintIcon = uiDocument.rootVisualElement.Q<VisualElement>("SprintIcon");
    }

    void FixedUpdate()
    {
        HandleSprintLogic();   // Major mod logic

        Vector2 input = MoveAction.ReadValue<Vector2>();
        float horizontal = input.x;
        float vertical = input.y;

        m_Movement.Set(horizontal, 0f, vertical);
        m_Movement.Normalize();

        // Rotate toward movement direction
        if (m_Movement.sqrMagnitude > 0.01f)
        {
            Vector3 desiredForward = Vector3.RotateTowards(
                transform.forward,
                m_Movement,
                turnSpeed * Time.deltaTime,
                0f
            );

            m_Rotation = Quaternion.LookRotation(desiredForward);
            m_Rigidbody.MoveRotation(m_Rotation);
        }

        // Choose speed
        float currentSpeed = walkSpeed;

        if (isSprinting && canSprint)
        {
            currentSpeed = sprintSpeed;  // Minor + major mod
        }

        // Move
        m_Rigidbody.MovePosition(
            m_Rigidbody.position +
            m_Movement * currentSpeed * Time.deltaTime
        );

        // Animator
        bool isWalking = m_Movement.sqrMagnitude > 0.01f;
        m_Animator.SetBool("IsWalking", isWalking);
    }

    // --------------------------------------------------------------
    //                    SPRINT LOGIC (MAJOR MOD)
    // --------------------------------------------------------------
    void HandleSprintLogic()
    {
        // Start sprint when pressing Shift AND sprint is ready
        if (SprintAction.IsPressed() && canSprint && !isSprinting)
        {
            isSprinting = true;
            sprintTimer = sprintDuration;
        }

        // Sprint active timer
        if (isSprinting)
        {
            sprintTimer -= Time.deltaTime;

            if (sprintTimer <= 0f)
            {
                isSprinting = false;
                canSprint = false;
                cooldownTimer = sprintCooldown;
            }
        }

        // Cooldown timer
        if (!canSprint && !isSprinting)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                canSprint = true;
            }
        }

        UpdateSprintIcon(); // UI Toolkit recolor
    }

    // --------------------------------------------------------------
    //                UI TOOLKIT ICON COLOR UPDATE
    // --------------------------------------------------------------
    void UpdateSprintIcon()
    {
        if (sprintIcon == null) return;

        Color borderColor;

        if (isSprinting)
            borderColor = Color.green;        // sprint active
        else if (!canSprint)
            borderColor = Color.red;          // cooling down
        else
            borderColor = Color.white;        // ready

        sprintIcon.style.borderTopColor = borderColor;
        sprintIcon.style.borderRightColor = borderColor;
        sprintIcon.style.borderBottomColor = borderColor;
        sprintIcon.style.borderLeftColor = borderColor;
    }

}
