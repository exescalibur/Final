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
    private VisualElement sprintIcon;      // Border color shows sprint state

    Rigidbody m_Rigidbody;
    Animator m_Animator;
    Vector3 m_Movement;
    Quaternion m_Rotation = Quaternion.identity;

    // Sprint internal state
    float sprintTimer = 0f;
    float cooldownTimer = 0f;
    bool isSprinting = false;
    bool canSprint = true;

    [Header("Fear Freeze (Minor Mod)")]
    public float minFreezeInterval = 10f;      // minimum seconds between freezes
    public float maxFreezeInterval = 20f;      // maximum seconds between freezes
    public int requiredMashCount = 5;          // presses needed to unfreeze
    public KeyCode unfreezeKey = KeyCode.E;    // key to mash

    bool isFrozen = false;
    int mashCount = 0;
    float nextFreezeTime = 0f;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();

        MoveAction.Enable();
        SprintAction.Enable();

        if (uiDocument != null)
        {
            sprintIcon = uiDocument.rootVisualElement.Q<VisualElement>("SprintIcon");
        }

        ScheduleNextFreeze();
    }

    void ScheduleNextFreeze()
    {
        nextFreezeTime = Time.time + Random.Range(minFreezeInterval, maxFreezeInterval);
    }

    void Update()
    {
        HandleFreezeLogic();
    }

    void FixedUpdate()
    {
        HandleSprintLogic();

        // Block all movement while frozen
        if (isFrozen)
        {
            m_Movement = Vector3.zero;
            m_Animator.SetBool("IsWalking", false);
            return;
        }

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

        if (isSprinting && canSprint && !isFrozen)
        {
            currentSpeed = sprintSpeed;
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

    // ---------------------- SPRINT (MAJOR MOD) ----------------------
    void HandleSprintLogic()
    {
        // No sprint while frozen
        if (isFrozen)
        {
            isSprinting = false;
            UpdateSprintIcon();
            return;
        }

        // Start sprint
        if (SprintAction.IsPressed() && canSprint && !isSprinting)
        {
            isSprinting = true;
            sprintTimer = sprintDuration;
        }

        // Sprint timer
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

        UpdateSprintIcon();
    }

    void UpdateSprintIcon()
    {
        if (sprintIcon == null) return;

        Color borderColor;

        if (isSprinting)
            borderColor = Color.green;      // sprinting
        else if (!canSprint)
            borderColor = Color.red;        // cooldown
        else
            borderColor = Color.white;      // ready

        sprintIcon.style.borderTopColor = borderColor;
        sprintIcon.style.borderRightColor = borderColor;
        sprintIcon.style.borderBottomColor = borderColor;
        sprintIcon.style.borderLeftColor = borderColor;
    }

    // ---------------------- FEAR FREEZE (MINOR MOD) ----------------------
    void HandleFreezeLogic()
    {
        // Already frozen: wait for mash input
        if (isFrozen)
        {
            if (Input.GetKeyDown(unfreezeKey))
            {
                mashCount++;

                if (mashCount >= requiredMashCount)
                {
                    isFrozen = false;
                    mashCount = 0;
                    ScheduleNextFreeze();
                }
            }
            return;
        }

        // Not frozen: check if it's time to trigger freeze
        if (Time.time >= nextFreezeTime)
        {
            isFrozen = true;
            mashCount = 0;
            // You can add sound / VFX here if you want
        }
    }
}
